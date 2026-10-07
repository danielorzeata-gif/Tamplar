using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RhinoWood.Core.Display;
using RhinoWood.Core.Domain;
using RhinoWood.Core.Furniture;
using RhinoWood.Core.Geometry;
using RhinoWood.Core.Joinery;
using RhinoWood.Core.Libraries;
using RhinoWood.Core.Optimization;
using RhinoWood.Core.Parametric;
using RhinoWood.Core.Projects;
using RhinoWood.Core.Reports;
using Xunit;

namespace RhinoWood.Tests
{
    public class DependencyGraphTests
    {
        [Fact]
        public void OnlyAffectedNodes_AreRecomputed()
        {
            var g = new DependencyGraph();
            g.AddInput("a", 2.0); g.AddInput("b", 3.0);
            g.AddComputed("sumA", new[] { "a" }, r => r.Get<double>("a") + 1);
            g.AddComputed("sumB", new[] { "b" }, r => r.Get<double>("b") + 1);
            g.AddComputed("total", new[] { "sumA", "sumB" }, r => r.Get<double>("sumA") + r.Get<double>("sumB"));
            Assert.Equal(7.0, g.Get<double>("total"));
            g.ClearLog();
            g.Set("a", 10.0);
            Assert.Equal(15.0, g.Get<double>("total"));
            Assert.Equal(new[] { "sumA", "total" }, g.EvaluationLog.ToArray());   // sumB untouched
        }

        [Fact]
        public void EarlyCutoff_StopsPropagation_WhenValueUnchanged()
        {
            var g = new DependencyGraph();
            g.AddInput("x", 4.0);
            g.AddComputed("clamped", new[] { "x" }, r => Math.Min(r.Get<double>("x"), 5.0));
            g.AddComputed("down", new[] { "clamped" }, r => r.Get<double>("clamped") * 2);
            g.Get<double>("down"); g.Set("x", 4.5); g.Get<double>("down"); g.ClearLog();
            g.Set("x", 9.0); g.Get<double>("down"); g.ClearLog();
            g.Set("x", 12.0);                   // clamped stays 5 -> 'down' must not re-run
            Assert.Equal(10.0, g.Get<double>("down"));
            Assert.DoesNotContain("down", g.EvaluationLog);
        }

        [Fact]
        public void UndeclaredDependency_IsRejected_AndOverridesApply()
        {
            var g = new DependencyGraph();
            g.AddInput("a", 1.0); g.AddInput("b", 2.0);
            g.AddComputed("c", new[] { "a" }, r => r.Get<double>("b"));
            Assert.Throws<InvalidOperationException>(() => g.Get<double>("c"));
            g.AddComputed("d", new[] { "a" }, r => r.Get<double>("a") * 350);
            g.SetOverride("d", new NumericOverride { Mode = OverrideMode.Add, Value = 30 });
            var info = g.GetOverrideInfo("d");
            Assert.Equal(350, info.Calculated); Assert.Equal(380, info.Final);
            g.Set("a", 2.0);                    // rule keeps working underneath the override
            Assert.Equal(730, g.Get<double>("d"));
        }

        [Fact]
        public void UnknownDependency_ThrowsAtDefinition() =>
            Assert.Throws<InvalidOperationException>(() => new DependencyGraph().AddComputed("x", new[] { "nope" }, r => 1.0));
    }

    public class TableProjectTests
    {
        private static WoodProject Table(string sp = "OAK") => WoodProject.CreateTable("Test table", sp);

        [Fact]
        public void DefaultTable_HasExpectedStructure()
        {
            var r = Table().Recalculate();
            Assert.Equal(4, r.Model.Families.Count);
            Assert.Equal(4, r.Model.Families.First(f => f.Id == "F-LEG").Quantity);   // part family, quantity 4
            Assert.Equal(9, r.Model.AllParts.Count());
            Assert.Equal(8, r.Model.Joints.Count);
            var leg = r.Model.Families.First(f => f.Id == "F-LEG");
            Assert.Equal(80, leg.Finished.Width);                                      // rule: 1800/22.5 = 80
            Assert.Equal(725, leg.Finished.Length);                                    // 760 - 35
            Assert.Equal(new Dims(755, 90, 90), leg.RoughPieces[0].Rough);             // +30 length, +10 section
            Assert.False(r.HasErrors, string.Join("\n", r.Issues));
        }

        [Fact]
        public void LegsStandOnFloor_TopAtHeight_AndPartsDoNotOverlapExceptJoints()
        {
            var m = Table().Recalculate().Model;
            Assert.Equal(0, m.AllParts.Where(p => p.Id.StartsWith("LEG")).Min(p => p.Bounds.Min.Z), 6);
            Assert.Equal(760, m.FindPart("TOP-1").Bounds.Max.Z, 6);
            Assert.Equal(760, m.Bounds.Max.Z, 6);
            // apron tenons overlap legs by tenon length, but aprons never overlap each other
            var aprons = m.AllParts.Where(p => p.Id.StartsWith("APR")).ToList();
            for (int i = 0; i < aprons.Count; i++) for (int j = i + 1; j < aprons.Count; j++) Assert.False(aprons[i].Bounds.Intersects(aprons[j].Bounds), aprons[i].Id + " vs " + aprons[j].Id);
        }

        [Fact]
        public void LengthChange_UpdatesDependents_AndOnlyAffectedNodes()
        {
            var p = Table(); p.Recalculate();
            p.SetParameter("length", 2200);
            var r = p.Recalculate();
            Assert.Equal(2200, r.Model.FindPart("TOP-1").Finished.Length);
            Assert.Equal(2020, r.Model.FindPart("APR-L-1").Finished.Length, 6);   // (2200-80) - 2x100 leg + 2x50 tenons
            Assert.Equal(100, r.Model.FamilyOf("LEG-1").Finished.Width);                           // 2200/22.5 -> 100 (rule)
            // short apron geometry unaffected by length except via leg section; width-only nodes not re-run
            Assert.DoesNotContain("span.y", r.RecomputedNodes);
            Assert.DoesNotContain("stock.top.width", r.RecomputedNodes);
            Assert.Contains("comp.top", r.RecomputedNodes);
            Assert.False(r.OptimizationReused);
            // downstream results all changed: BOM / procurement
            Assert.Contains(r.Optimization.Boards.SelectMany(b => b.Cuts), c => Math.Abs(c.Length - 2230) < 1e-9);
        }

        [Fact]
        public void NothingChanged_ReusesOptimization_AndGeometryCache()
        {
            var p = Table(); var r1 = p.Recalculate(); var r2 = p.Recalculate();
            Assert.True(r2.OptimizationReused);
            Assert.Same(r1.Optimization, r2.Optimization);
            p.GenerateGeometry(DisplayMode.Engineering);
            p.GeometryCache.ResetCounters();
            p.GenerateGeometry(DisplayMode.Engineering);
            Assert.Equal(0, p.GeometryCache.Misses);
            Assert.True(p.GeometryCache.Hits > 0);
        }

        [Fact]
        public void ChangingWidth_RegeneratesOnlyChangedPartGeometry()
        {
            var p = Table(); p.GenerateGeometry(DisplayMode.Normal);
            p.GeometryCache.ResetCounters();
            p.SetParameter("width", 1000);
            p.GenerateGeometry(DisplayMode.Normal);
            // legs 1/2 and the long apron near y=o keep identical geometry only if untouched; legs 3/4 and aprons move -> some hits AND some misses
            Assert.True(p.GeometryCache.Hits > 0);
            Assert.True(p.GeometryCache.Misses > 0);
            Assert.True(p.GeometryCache.Misses < 9);
        }

        [Fact]
        public void DisplayModes_ProvideIncreasingDetail()
        {
            var p = Table(); p.Recalculate();
            int perf = p.GenerateGeometry(DisplayMode.Performance).Count;
            int normal = p.GenerateGeometry(DisplayMode.Normal).Count;
            int eng = p.GenerateGeometry(DisplayMode.Engineering).Count;
            int man = p.GenerateGeometry(DisplayMode.Manufacturing).Count;
            Assert.Equal(9, perf); Assert.Equal(9, normal);
            Assert.True(eng > normal); Assert.True(man > eng);
            Assert.DoesNotContain(p.GenerateGeometry(DisplayMode.Normal), g => g.Category == PrimCategory.Feature || g.Category == PrimCategory.Grain);
            Assert.Contains(p.GenerateGeometry(DisplayMode.Engineering), g => g.Category == PrimCategory.Grain);
        }

        [Fact]
        public void Joinery_MortiseTenon_FeaturesMatchAndFitInsideParts()
        {
            var m = Table().Recalculate().Model;
            var apron = m.FindPart("APR-L-1");
            var tenons = apron.Features.Where(f => f.Kind == FeatureKind.Tenon).ToList();
            Assert.Equal(2, tenons.Count);
            Assert.All(tenons, t => { Assert.Equal(8, t.Box.Size.Z, 6); Assert.Equal(60, t.Box.Size.Y, 6); Assert.Equal(40, t.Box.Size.X, 6); });
            var leg = m.FindPart("LEG-1");
            var mortises = leg.Features.Where(f => f.Kind == FeatureKind.Mortise).ToList();
            Assert.Equal(2, mortises.Count);
            foreach (var mo in mortises)
            {
                var w = leg.LocalBoxToWorld(mo.Box);
                Assert.True(w.Min.X >= leg.Bounds.Min.X - 1e-6 && w.Max.X <= leg.Bounds.Max.X + 1e-6);
                Assert.True(w.Min.Y >= leg.Bounds.Min.Y - 1e-6 && w.Max.Y <= leg.Bounds.Max.Y + 1e-6);
                Assert.True(w.Min.Z >= leg.Bounds.Min.Z - 1e-6 && w.Max.Z <= leg.Bounds.Max.Z + 1e-6);
                Assert.Equal(43, mo.Depth, 6);                     // tenon 40 + 3 glue pocket
            }
            // the mortise must contain the tenon of the apron (world)
            var tw = apron.LocalBoxToWorld(tenons[0].Box);
            Assert.Contains(mortises, mo => { var w = leg.LocalBoxToWorld(mo.Box); return w.Min.Y <= tw.Min.Y && w.Max.Y >= tw.Max.Y && w.Min.Z <= tw.Min.Z && w.Max.Z >= tw.Max.Z; });
        }

        [Fact]
        public void HardwareGeneratesHolesAndSlots_AndFollowsChanges()
        {
            var p = Table(); var r = p.Recalculate();
            int clips = r.Model.HardwareInstalls.Count(h => h.HardwareId == "TOP-ZCLIP");
            Assert.True(clips >= 4);
            Assert.Contains(r.Model.FindPart("APR-L-1").Features, f => f.Kind == FeatureKind.Slot);
            Assert.Contains(r.Model.FindPart("TOP-1").Features, f => f.Kind == FeatureKind.ScrewHole && f.Depth == 12);
            Assert.Contains(r.Model.FindPart("APR-S-1").Features, f => f.Kind == FeatureKind.ElongatedHole);
            // Z-clip slot is on the inner apron face, 12mm below the apron top
            var slot = r.Model.FindPart("APR-L-1").Features.First(f => f.Kind == FeatureKind.Slot);
            var wb = r.Model.FindPart("APR-L-1").FeatureWorldBox(slot);
            Assert.Equal(r.Model.FindPart("APR-L-1").Bounds.Max.Z - 12, (wb.Min.Z + wb.Max.Z) / 2, 3);
            p.SetParameter("clipSpacing", 200);
            Assert.True(p.Recalculate().Model.HardwareInstalls.Count(h => h.HardwareId == "TOP-ZCLIP") > clips);   // more clips -> more holes
            Assert.Equal(r.Bom.Lines.First(l => l.Id == "TOP-ZCLIP").Quantity, clips);
        }

        [Fact]
        public void BomCostAndProcurement_UseActualOptimizedPurchase()
        {
            var r = Table().Recalculate();
            Assert.Equal(r.Optimization.Purchase.Sum(x => x.Total), r.Cost.RawMaterial, 2);
            Assert.True(r.Cost.RawMaterial > 0 && r.Cost.Hardware > 0 && r.Cost.Consumables > 0 && r.Cost.Labor > 0);
            Assert.Equal(r.Cost.RawMaterial + r.Cost.Hardware + r.Cost.Consumables + r.Cost.Labor, r.Cost.Total, 2);
            // purchase is commercial quantities, never fractional
            Assert.All(r.Optimization.Purchase, p => Assert.True(p.Quantity >= 1 && p.Length >= 3000));
            // part cost allocation adds up to the purchase
            Assert.Equal(r.Cost.RawMaterial - r.Optimization.Purchase.Where(x => x.ReserveQuantity > 0).Sum(x => x.ReserveQuantity * x.UnitPrice),
                r.Model.Families.Sum(f => f.UnitCost * f.Quantity), 0);
            Assert.Contains(r.Bom.Lines, l => l.Category == "Adhesive");
            Assert.Contains(r.Bom.Lines, l => l.Category == "Finish");
        }

        [Fact]
        public void Traceability_PartToPurchaseBoardCutOperations()
        {
            var r = Table().Recalculate();
            foreach (var part in r.Model.AllParts)
            {
                var t = r.Manufacturing.Traces[part.Id];
                Assert.NotEmpty(t.CutIds); Assert.NotEmpty(t.BoardIds); Assert.NotEmpty(t.PurchaseLineIds); Assert.NotEmpty(t.OperationIds);
                foreach (var b in t.BoardIds) Assert.Contains(r.Optimization.Boards, x => x.Id == b && x.Cuts.Any(c => c.Demand.PartId == part.Id));
            }
            Assert.Contains(r.Manufacturing.Operations, o => o.Type == OperationType.Mortise && o.PartId == "LEG-1");
            Assert.Contains(r.Manufacturing.Operations, o => o.Type == OperationType.Tenon && o.PartId == "APR-L-1");
            Assert.Contains(r.Manufacturing.Operations, o => o.Type == OperationType.Glue && o.PartId == "TOP-1");
        }

        [Fact]
        public void EveryDemandIsCoveredByCuts()
        {
            var r = Table().Recalculate();
            Assert.Equal(r.Demands.Count, r.Optimization.Boards.Sum(b => b.Cuts.Count));
            Assert.Equal(7 + 4 + 4, r.Demands.Count);       // 7 top strips + 4 legs + 4 aprons
        }

        [Fact]
        public void CustomSpecies_CanBeAddedAndUsed()
        {
            var lib = WoodLibrary.CreateDefault();
            lib.Species["IROKO"] = new WoodSpecies { Id = "IROKO", Name = "Iroko", PricePerM3 = 1700, TangentialMovementPerPercent = 0.0028, SupplierId = "SUP-TIMBER", IsUserDefined = true };
            foreach (var s in lib.StockFor("OAK").ToList()) lib.Stock.Add(new StockItem { Id = "IROKO-" + s.Width + "x" + s.Thickness, SpeciesId = "IROKO", Width = s.Width, Thickness = s.Thickness, Lengths = s.Lengths.ToList() });
            var p = WoodProject.CreateTable("Iroko", "IROKO", lib);
            var r = p.Recalculate();
            Assert.All(r.Optimization.Purchase, x => Assert.Equal("IROKO", x.Item.SpeciesId));
        }

        [Theory]
        [InlineData("OAK")] [InlineData("PINE")] [InlineData("WALNUT")] [InlineData("BEECH")]
        public void AllSpecies_ProduceCompletePlans(string sp)
        {
            var r = Table(sp).Recalculate();
            Assert.False(r.HasErrors, string.Join("\n", r.Issues.Where(i => i.Severity == Severity.Error)));
            Assert.True(r.Cost.Total > 0);
        }

        [Fact]
        public void SpeciesReserve_Walnut15Percent_IsReported()
        {
            var r = Table("WALNUT").Recalculate();
            Assert.Equal(15, r.Optimization.ReservePercentBySpecies["WALNUT"]);
        }

        [Fact]
        public void ParameterBounds_AreEnforced()
        {
            var p = Table();
            Assert.Throws<ArgumentOutOfRangeException>(() => p.SetParameter("height", 5000));
            Assert.Throws<ArgumentException>(() => p.SetParameter("nonsense", 1));
        }

        [Fact]
        public void SlenderLeg_ProducesWarning()
        {
            var p = Table(); p.SetParameter("height", 1100); p.SetParameter("length", 700); p.SetParameter("legSectionUser", 60);
            Assert.Contains(p.Recalculate().Issues, i => i.Code == "SLENDER");
        }

        [Fact]
        public void SmallLeg_TriggersMitredTenons_WhenMortisesCollide()
        {
            var p = Table(); p.SetParameter("legSectionUser", 60); p.SetParameter("apronThickness", 40);
            var r = p.Recalculate();
            Assert.Contains(r.Model.Joints, j => j.Mitred);
            Assert.Contains(r.Issues, i => i.Code == "TENON_MITRED");
        }
    }

    public class OverrideTests
    {
        [Fact]
        public void Override_PreservesRule_AndCanBeResolved()
        {
            var p = WoodProject.CreateTable("t");
            var before = p.Recalculate().Model.FamilyOf("LEG-1").Finished.Width;     // 80
            p.SetOverride("leg.section", OverrideMode.Add, 10);
            var r = p.Recalculate();
            Assert.Equal(before + 10, r.Model.FamilyOf("LEG-1").Finished.Width);
            var info = p.GetOverride("leg.section");
            Assert.Equal(80, info.Calculated); Assert.Equal(90, info.Final);

            p.SetParameter("length", 2200);                                            // rule recalculates underneath (100) + override 10
            Assert.Equal(110, p.Recalculate().Model.FamilyOf("LEG-1").Finished.Width);

            p.ResolveOverride("leg.section", OverrideResolution.Recalculate);
            Assert.Equal(100, p.Recalculate().Model.FamilyOf("LEG-1").Finished.Width);
        }

        [Fact]
        public void ConvertToCustom_FreezesValue_NothingLost()
        {
            var p = WoodProject.CreateTable("t");
            p.SetOverride("overhang", OverrideMode.Add, 15);
            p.ResolveOverride("overhang", OverrideResolution.ConvertToCustomComponent);
            Assert.Contains("overhang", p.CustomComponents);
            Assert.Equal(55, p.GetOverride("overhang").Final);
            var json = ProjectSerializer.Serialize(p);
            var p2 = ProjectSerializer.Deserialize(json);
            Assert.Contains("overhang", p2.CustomComponents);
            Assert.Equal(55, p2.GetOverride("overhang").Final);
        }

        [Fact]
        public void NonOverridableNode_Throws() =>
            Assert.Throws<ArgumentException>(() => WoodProject.CreateTable("t").SetOverride("comp.legs", OverrideMode.Add, 1));
    }

    public class PersistenceTests
    {
        [Fact]
        public void SaveClose_Reopen_RestoresParametersOverridesAndPlan()
        {
            var p = WoodProject.CreateTable("Round trip", "OAK");
            p.SetParameter("length", 2000); p.SetParameter("width", 950); p.SetParameter("apronHeight", 90);
            p.SetOverride("overhang", OverrideMode.Add, 10);
            p.Settings.Strategy = OptimizationStrategy.MinCost; p.Settings.GlobalReservePercent = 12;
            p.Settings.ReserveOverrides["OAK"] = 8;
            var r1 = p.Recalculate();
            var json = ProjectSerializer.Serialize(p, r1);

            var (p2, match) = ProjectSerializer.Open(json);
            Assert.True(match);
            Assert.Equal(p.Id, p2.Id);
            Assert.Equal(2000, p2.Parameters["length"]);
            Assert.Equal(OptimizationStrategy.MinCost, p2.Settings.Strategy);
            Assert.Equal(8, p2.Settings.ReserveOverrides["OAK"]);
            Assert.Equal(OverrideMode.Add, p2.Overrides.Single().Value.Mode);
            var r2 = p2.Recalculate();
            Assert.Equal(r1.Fingerprint, r2.Fingerprint);
            Assert.Equal(r1.Cost.Total, r2.Cost.Total);
            // relationships survive: dependency graph still reacts after reopening
            p2.SetParameter("length", 2400);
            Assert.Equal(2400, p2.Recalculate().Model.FindPart("TOP-1").Finished.Length);
        }

        [Fact]
        public void TamperedProject_IsDetectedBySnapshot()
        {
            var p = WoodProject.CreateTable("x"); var json = ProjectSerializer.Serialize(p);
            var tampered = json.Replace("\"length\": 1800", "\"length\": 1900");
            Assert.NotEqual(json, tampered);
            Assert.False(ProjectSerializer.Open(tampered).snapshotMatches);
        }

        [Fact]
        public void NewerSchema_IsRejectedClearly()
        {
            var json = ProjectSerializer.Serialize(WoodProject.CreateTable("x")).Replace("\"SchemaVersion\": 1", "\"SchemaVersion\": 99");
            Assert.Throws<NotSupportedException>(() => ProjectSerializer.Deserialize(json));
        }

        [Fact]
        public void UserLibrary_RoundTrips()
        {
            var user = new WoodLibrary();
            user.Species["TEAK"] = new WoodSpecies { Id = "TEAK", Name = "Teak", PricePerM3 = 4200, SupplierId = "SUP-TIMBER", ReservePercent = 12 };
            user.Stock.Add(new StockItem { Id = "TEAK-90x90", SpeciesId = "TEAK", Width = 90, Thickness = 90, Lengths = { 3000, 4000 } });
            var back = LibrarySerializer.DeserializeUser(LibrarySerializer.SerializeUser(user));
            var lib = WoodLibrary.CreateDefault(); lib.Merge(back);
            Assert.Equal(12, lib.GetSpecies("TEAK").ReservePercent);
            Assert.Contains(lib.Stock, s => s.Id == "TEAK-90x90");
        }
    }

    public class ReportTests
    {
        [Fact]
        public void AllReports_AreWritten_AndConsistent()
        {
            var p = WoodProject.CreateTable("Report table"); var r = p.Recalculate();
            var dir = Path.Combine(Path.GetTempPath(), "rw_" + Guid.NewGuid().ToString("N"));
            var files = ReportWriter.WriteAll(dir, p, r);
            try
            {
                Assert.True(files.Count >= 18);
                Assert.All(files, f => Assert.True(new FileInfo(f).Length > 20, f));
                var cut = File.ReadAllLines(Path.Combine(dir, "cutlist.csv"));
                Assert.Equal(r.Demands.Count + 1, cut.Length);
                var proc = File.ReadAllText(Path.Combine(dir, "procurement.csv"));
                Assert.Contains("OAK", proc);
                Assert.Contains("<svg", File.ReadAllText(Path.Combine(dir, "cutplan.svg")));
                Assert.Contains("WHY THIS PURCHASE", File.ReadAllText(Path.Combine(dir, "optimization.txt")));
                var html = File.ReadAllText(Path.Combine(dir, "project_summary.html"));
                Assert.Contains("Visual cut plan", html);
                // saved project reopens to the same plan
                var (_, ok) = ProjectSerializer.Open(File.ReadAllText(Path.Combine(dir, "project.rhinowood.json")));
                Assert.True(ok);
            }
            finally { Directory.Delete(dir, true); }
        }
    }

    public class JointTests
    {
        private static PartInstance Part(string id, Box3 b, Axis len, Axis wid, Axis th) => new PartInstance { Id = id, FamilyId = "F", Bounds = b, LengthAxis = len, WidthAxis = wid, ThicknessAxis = th };

        [Fact]
        public void Registry_ContainsInitialLibrary()
        {
            var reg = JointRegistry.CreateDefault();
            foreach (var id in new[] { "mortise-tenon", "loose-tenon", "dowel", "biscuit", "dovetail", "finger", "box-joint", "dado", "rabbet", "half-lap", "bridle", "scarf", "japanese-kusabi" })
                Assert.True(reg.Has(id), id);
        }

        private static (PartInstance a, PartInstance b) Corner()
        {
            // A: 600 long along X, 100 wide (Y), 20 thick (Z); butts to B (vertical post 60x60x800) at X=0
            var b = Part("B", new Box3(new Vec3(-60, 0, 0), new Vec3(0, 60, 800)), Axis.Z, Axis.X, Axis.Y);
            var a = Part("A", new Box3(new Vec3(0, 0, 100), new Vec3(600, 60, 160)), Axis.X, Axis.Y, Axis.Z);
            return (a, b);
        }

        [Theory]
        [InlineData("dowel")] [InlineData("biscuit")] [InlineData("loose-tenon")] [InlineData("finger")] [InlineData("box-joint")] [InlineData("dovetail")] [InlineData("dado")] [InlineData("bridle")]
        public void EveryJoint_GeneratesFeaturesOnBothParts(string id)
        {
            var (a, b) = Corner();
            var def = JointRegistry.CreateDefault().Get(id);
            var res = def.Generate(new JointContext { JointId = "J1", A = a, B = b, AAtStart = true, Library = WoodLibrary.CreateDefault() });
            Assert.NotEmpty(res.Features);
            Assert.Contains(res.Features, f => f.PartId == "B");
            Assert.True(res.Features.All(f => f.SourceId == "J1"));
            Assert.False(string.IsNullOrEmpty(res.Description));
        }

        [Fact]
        public void Dowels_AreCoaxialInBothParts()
        {
            var (a, b) = Corner();
            var res = new DowelJoint().Generate(new JointContext { JointId = "J1", A = a, B = b, AAtStart = true });
            var ha = res.Features.Where(f => f.PartId == "A").ToList(); var hb = res.Features.Where(f => f.PartId == "B").ToList();
            Assert.Equal(ha.Count, hb.Count);
            for (int i = 0; i < ha.Count; i++)
            {
                var pa = a.LocalToWorld(ha[i].Position); var pb = b.LocalToWorld(hb[i].Position);
                Assert.True(pa.Equals(pb), pa + " vs " + pb);
                var da = a.LocalToWorld(ha[i].Direction) - a.LocalToWorld(Vec3.Zero); var db = b.LocalToWorld(hb[i].Direction) - b.LocalToWorld(Vec3.Zero);
                Assert.Equal(-1, da.Normalized().Dot(db.Normalized()), 6);          // opposite directions into each part
            }
        }

        [Fact]
        public void MortiseDepthExceedingMaterial_IsError()
        {
            var b = Part("B", new Box3(new Vec3(-30, 0, 0), new Vec3(0, 30, 800)), Axis.Z, Axis.X, Axis.Y);    // 30 mm post
            var a = Part("A", new Box3(new Vec3(-45, 0, 100), new Vec3(600, 30, 160)), Axis.X, Axis.Y, Axis.Z);
            var res = new MortiseTenonJoint().Generate(new JointContext { JointId = "J", A = a, B = b, AAtStart = true, P = { ["tenonLengthMax"] = 100, ["tenonLengthRatio"] = 0.9 } });
            Assert.Contains(res.Issues, i => i.Severity == Severity.Error && i.Code == "MORTISE_DEPTH");
        }

        [Fact]
        public void HalfLap_SplitsOverlapBetweenParts()
        {
            var a = Part("A", new Box3(new Vec3(0, 0, 0), new Vec3(500, 50, 20)), Axis.X, Axis.Y, Axis.Z);
            var b = Part("B", new Box3(new Vec3(200, -100, 0), new Vec3(250, 200, 20)), Axis.Y, Axis.X, Axis.Z);
            var res = new HalfLapJoint().Generate(new JointContext { JointId = "J", A = a, B = b, AAtStart = true });
            Assert.Equal(2, res.Features.Count);
            Assert.All(res.Features, f => Assert.Equal(10, f.Depth, 6));
        }

        [Fact]
        public void Kusabi_HasWedgeSlotsWithSlopeAngle()
        {
            var (a, b) = Corner();
            var res = new WedgedThroughTenonJoint().Generate(new JointContext { JointId = "J", A = a, B = b, AAtStart = true, Library = WoodLibrary.CreateDefault() });
            var wedges = res.Features.Where(f => f.Kind == FeatureKind.WedgeSlot).ToList();
            Assert.Equal(2, wedges.Count);
            Assert.Equal(Math.Atan(1.0 / 8) * 180 / Math.PI, wedges[0].Angle, 6);
            Assert.Contains(res.Features, f => f.Kind == FeatureKind.Mortise && f.IsThrough);
        }

        [Fact]
        public void CustomJoint_BuiltFromTemplates()
        {
            var (a, b) = Corner();
            var cj = new CustomJoint("my-joint", "My joint", new[] { new CustomJointTemplate { Target = "A", Kind = FeatureKind.Pocket }, new CustomJointTemplate { Target = "B", Kind = FeatureKind.Pocket, Depth = 10 } });
            var reg = JointRegistry.CreateDefault(); reg.Register(cj);
            var res = reg.Get("my-joint").Generate(new JointContext { JointId = "J", A = a, B = b, AAtStart = true });
            Assert.Equal(2, res.Features.Count);
        }
    }

    public class ExtensibilityTests
    {
        private sealed class BenchDefinition : IFurnitureDefinition
        {
            public string TypeId => "bench.simple"; public string Name => "Simple bench"; public string Category => "Benches";
            public IReadOnlyList<ParameterDef> Parameters { get; } = new[] { new ParameterDef { Key = "length", Default = 1200, Min = 400, Max = 3000, Label = "Length" } };
            public IReadOnlyList<string> OverridableNodes { get; } = new string[0];
            public string NodeFor(string k) => k;
            public DependencyGraph CreateGraph(ProjectContext ctx, IDictionary<string, double> v, string sp)
            {
                var g = new DependencyGraph(); g.AddInput("length", v["length"]);
                g.AddComputed("seat.length", new[] { "length" }, r => r.Get<double>("length"));
                return g;
            }
            public FurnitureModel Assemble(DependencyGraph g, ProjectContext ctx)
            {
                var fin = new Dims(g.Get<double>("seat.length"), 80, 40);
                var fam = new PartFamily { Id = "F-SEAT", Name = "Seat", Type = PartType.Panel, SpeciesId = "OAK", Finished = fin, RoughPieces = { new RoughPieceSpec { Rough = ctx.Rules.Rough(fin) } } };
                fam.Instances.Add(new PartInstance { Id = "SEAT-1", FamilyId = "F-SEAT", Bounds = Box3.FromMinSize(Vec3.Zero, new Vec3(fin.Length, 80, 40)), LengthAxis = Axis.X, WidthAxis = Axis.Y, ThicknessAxis = Axis.Z });
                return new FurnitureModel { TypeId = TypeId, Name = Name, Families = { fam } };
            }
        }

        [Fact]
        public void NewFurnitureType_WorksWithoutChangingTheCore()
        {
            var lib = WoodLibrary.CreateDefault(); var types = FurnitureRegistry.CreateDefault(); types.Register(new BenchDefinition());
            var p = new WoodProject("bench", types.Get("bench.simple"), "OAK", lib, JointRegistry.CreateDefault(), types);
            var r = p.Recalculate();
            Assert.Single(r.Model.AllParts);
            Assert.NotEmpty(r.Optimization.Purchase);
            Assert.True(r.Cost.RawMaterial > 0);
        }

        [Fact]
        public void NewValidationRule_CanBeRegistered()
        {
            var p = WoodProject.CreateTable("t");
            p.Validator.Register(new AlwaysWarn());
            Assert.Contains(p.Recalculate().Issues, i => i.Code == "CUSTOM");
        }
        private sealed class AlwaysWarn : RhinoWood.Core.Validation.IValidationRule
        {
            public string Code => "CUSTOM";
            public IEnumerable<Issue> Check(RhinoWood.Core.Validation.ValidationContext c) { yield return new Issue { Severity = Severity.Warning, Code = "CUSTOM", Message = "x" }; }
        }
    }
}
