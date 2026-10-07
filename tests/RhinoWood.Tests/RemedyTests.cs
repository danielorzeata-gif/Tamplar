using System.Linq;
using RhinoWood.Core.Display;
using RhinoWood.Core.Domain;
using RhinoWood.Core.Projects;
using Xunit;

namespace RhinoWood.Tests
{
    public class RemedyTests
    {
        private static WoodProject Premium() { var p = WoodProject.CreateTable("t"); p.SetChoice("jointApronLong", "japanese-kusabi"); p.SetChoice("jointApronShort", "japanese-kusabi"); return p; }

        [Fact]
        public void ThroughConflict_HasRankedSolutions_AndApplyingTheBestClearsIt()
        {
            var p = Premium(); var r = p.Recalculate();
            Assert.Contains(r.Issues, i => i.Code == "THROUGH_CONFLICT");
            var fixes = RemedyEngine.Suggest(p, r);
            Assert.NotEmpty(fixes);
            Assert.True(fixes[0].Recommended); Assert.Single(fixes, f => f.Recommended);
            Assert.True(fixes.Zip(fixes.Skip(1), (a, b) => a.Score <= b.Score + 1e-9).All(x => x));
            Assert.Equal("japanese-kusabi", fixes[0].FromOption);
            // suggesting must not change the project
            Assert.Equal("japanese-kusabi", p.Choices["jointApronLong"]);
            RemedyEngine.Apply(p, fixes[0]);
            Assert.DoesNotContain(p.Recalculate().Issues, i => i.Code == "THROUGH_CONFLICT");
        }

        [Fact]
        public void NoConflict_NoSolutions() => Assert.Empty(RemedyEngine.Suggest(WoodProject.CreateTable("t")));

        [Fact]
        public void Ranking_FollowsStrategy()
        {
            var p = Premium(); p.Settings.Strategy = OptimizationStrategy.MinCost;
            var best = RemedyEngine.Suggest(p)[0];
            Assert.All(RemedyEngine.Suggest(p), f => Assert.True(f.Score >= best.Score - 1e-9));
        }
    }

    public class PartSolidTests
    {
        [Fact]
        public void Parts_CarryCutVolumes_InNormalModeOnly()
        {
            var p = WoodProject.CreateTable("t"); p.Recalculate();
            var normal = p.GenerateGeometry(DisplayMode.Normal).Where(x => x.Category == PrimCategory.Part).ToList();
            Assert.All(normal.Where(x => x.PartId.StartsWith("LEG")), x => Assert.NotEmpty(x.Cuts));
            Assert.All(normal.Where(x => x.PartId.StartsWith("APR")), x => Assert.NotEmpty(x.Cuts));   // tenon cheeks + shoulders
            Assert.All(p.GenerateGeometry(DisplayMode.Performance).Where(x => x.Category == PrimCategory.Part), x => Assert.Null(x.Cuts));
        }

        [Fact]
        public void CutVolumes_AreExtendedPastTouchedFaces_ToAvoidCoplanarBooleans()
        {
            var p = WoodProject.CreateTable("t"); var r = p.Recalculate();
            var rail = r.Model.FindPart("APR-L-1");
            var cuts = PartSolids.CutsFor(rail);
            Assert.Contains(cuts, c => c.Kind == PrimKind.Box && c.Box.Min.Z < rail.Bounds.Min.Z || c.Box.Max.Z > rail.Bounds.Max.Z);
            Assert.True(PartSolids.RemovedVolumeMm3(rail) > 0);
            Assert.True(PartSolids.RemovedVolumeMm3(rail) < rail.Bounds.Size.X * rail.Bounds.Size.Y * rail.Bounds.Size.Z);
        }

        [Fact]
        public void Rough_IsNotSmallerThanFinished()
        {
            var p = WoodProject.CreateTable("t"); var r = p.Recalculate();
            foreach (var part in r.Model.AllParts)
            {
                var rough = PartSolids.RoughOf(part, r.Model.FamilyOf(part.Id), p.Settings.Rules.GluedPanelTrim); var fin = part.Finished;
                Assert.True(rough.Length >= fin.Length && rough.Width >= fin.Width && rough.Thickness >= fin.Thickness, part.Id);
            }
        }
    }
}

namespace RhinoWood.Tests
{
    public class TopStripsAndBiscuitTests
    {
        private static int Strips(RhinoWood.Core.Projects.ProjectResult r) => r.Model.FamilyOf("TOP-1").RoughPieces.Sum(x => x.CountPerPart);

        [Fact]
        public void TopIsDrawnAsSeparateStrips_WithBiscuitsBetweenThem()
        {
            var p = WoodProject.CreateTable("t"); var r = p.Recalculate();
            int n = Strips(r); Assert.True(n > 1);
            var prims = p.GenerateGeometry(DisplayMode.Engineering);
            Assert.Equal(n, prims.Count(x => x.Category == PrimCategory.Part && x.PartId == "TOP-1"));
            Assert.Equal(n * 0 + r.Model.Biscuits.Count, prims.Count(x => x.Category == PrimCategory.Hardware && x.Key.StartsWith("BSC")));
            Assert.Equal(r.Model.Biscuits.Count * 1, r.Model.AllParts.Single(x => x.Id == "TOP-1").Features.Count(f => f.Kind == FeatureKind.BiscuitSlot));
            Assert.Equal(2, r.Model.Biscuits.Select(b => b.Edge).Min() + 1);          // edges are 1..n-1
            Assert.Equal(n - 1, r.Model.Biscuits.Select(b => b.Edge).Distinct().Count());
        }

        [Fact]
        public void BiscuitCountAndPositions_FollowLengthWidthAndPitch()
        {
            var p = WoodProject.CreateTable("t"); var r0 = p.Recalculate(); int n0 = r0.Model.Biscuits.Count;
            p.SetParameter("length", 2400); var r1 = p.Recalculate();
            Assert.True(r1.Model.Biscuits.Count > n0);                                  // longer top -> more biscuits per edge
            p.SetParameter("biscuitPitch", 120); Assert.True(p.Recalculate().Model.Biscuits.Count > r1.Model.Biscuits.Count);
            p = WoodProject.CreateTable("t"); p.SetParameter("width", 600);
            var r2 = p.Recalculate(); Assert.True(r2.Model.Biscuits.Select(b => b.Edge).Distinct().Count() == Strips(r2) - 1);
            // every biscuit lies inside the top, 60 mm from the ends at the nearest, centred in the thickness
            var top = r2.Model.FindPart("TOP-1");
            Assert.All(r2.Model.Biscuits, b => { Assert.True(b.Box.Min.X >= top.Bounds.Min.X + 30 && b.Box.Max.X <= top.Bounds.Max.X - 30); Assert.Equal(top.Bounds.Center.Z, b.Box.Center.Z, 6); });
        }

        [Fact]
        public void Biscuits_AreInTheBom_AndSlotsAreCutInTheStrips()
        {
            var p = WoodProject.CreateTable("t"); var r = p.Recalculate();
            Assert.Contains(r.Cost == null ? null : r.Bom.Lines, l => l.Id.StartsWith("BISCUIT") && l.Quantity == r.Model.Biscuits.Count);
            var strip = p.GenerateGeometry(DisplayMode.Normal).Where(x => x.PartId == "TOP-1" && x.Category == PrimCategory.Part).ToList();
            Assert.All(strip, s => Assert.NotEmpty(s.Cuts));
        }
    }
}

namespace RhinoWood.Tests
{
    public class TopFastenerCountTests
    {
        private static int OnApron(RhinoWood.Core.Projects.ProjectResult r, string id) => r.Model.HardwareInstalls.Count(h => h.HostPartId == id);

        [Fact]
        public void ShortAprons_GetMoreFasteners_WhenTheTableGrows_AndTheCountIsOddWithACentreOne()
        {
            var small = WoodProject.CreateTable("t"); small.SetParameter("length", 1400); small.SetParameter("width", 700); var rs = small.Recalculate();
            var wide = WoodProject.CreateTable("t"); wide.SetParameter("length", 1400); wide.SetParameter("width", 1400); var rw = wide.Recalculate();
            Assert.True(OnApron(rw, "APR-S-1") > OnApron(rs, "APR-S-1"));
            Assert.Equal(OnApron(rw, "APR-S-1"), OnApron(rw, "APR-S-2"));
            Assert.Equal(1, OnApron(rw, "APR-S-1") % 2);
            var ys = rw.Model.HardwareInstalls.Where(h => h.HostPartId == "APR-S-1").Select(h => h.Point.Y).OrderBy(v => v).ToList();
            Assert.Equal(rw.Model.FindPart("TOP-1").Bounds.Center.Y, ys[ys.Count / 2], 3);           // one on the centre line
            Assert.Equal(ys.First() - rw.Model.FindPart("TOP-1").Bounds.Min.Y, rw.Model.FindPart("TOP-1").Bounds.Max.Y - ys.Last(), 3);   // symmetric
        }

        [Fact]
        public void Square1400_HasSeveralFastenersOnAllFourAprons() { var p = WoodProject.CreateTable("t"); p.SetParameter("length", 1400); p.SetParameter("width", 1400); var r = p.Recalculate(); Assert.All(new[] { "APR-L-1", "APR-L-2", "APR-S-1", "APR-S-2" }, a => Assert.True(OnApron(r, a) >= 3, a)); Assert.DoesNotContain(r.Issues, i => i.Code == "MOVEMENT_FASTENER" && i.Message.Contains("Slotted")); }

        [Fact]
        public void ClipTravelWarning_GetsAFixThatChangesTheTopFixing()
        {
            var p = WoodProject.CreateTable("t"); p.SetParameter("length", 1600); p.SetParameter("width", 1150); var r = p.Recalculate();
            Assert.Contains(r.Issues, i => i.Code == "MOVEMENT_FASTENER");
            var fixes = RemedyEngine.Suggest(p, r);
            Assert.NotEmpty(fixes); Assert.All(fixes, f => Assert.Equal("topFixing", f.ChoiceKey));
            RemedyEngine.Apply(p, fixes[0]); Assert.DoesNotContain(p.Recalculate().Issues, i => i.Code == "MOVEMENT_FASTENER");
        }
    }
}

namespace RhinoWood.Tests
{
    public class NightstandTests
    {
        private static WoodProject N(string tier = "STANDARD") { var p = WoodProject.Create("casework.nightstand", "Noptieră"); p.ApplyTier(tier); return p; }

        [Fact]
        public void Nightstand_HasAllParts_NoErrors_AndAPurchasePlan()
        {
            var r = N().Recalculate();
            Assert.Equal(new[] { "CAP-1", "DBACK-1", "DFRONT-1", "DSIDE-1", "DSIDE-2", "FRONT-1", "LEG-1", "LEG-2", "LEG-3", "LEG-4", "SHELF-1", "SIDE-1", "SIDE-2", "BOT-1" }.OrderBy(x => x), r.Model.AllParts.Select(p => p.Id).OrderBy(x => x));
            Assert.Equal(2, r.Model.SheetParts.Count);
            Assert.False(r.HasErrors, string.Join("\n", r.Issues.Where(i => i.Severity == RhinoWood.Core.Domain.Severity.Error)));
            Assert.Empty(r.Optimization.Unplaced); Assert.True(r.Cost.Total > 0);
            Assert.Contains(r.Bom.Lines, l => l.Id.StartsWith("SHEET-HDF"));
            Assert.Equal(4, r.Model.HardwareInstalls.Count(h => h.HardwareId == "FOOT-LEVEL"));
        }

        [Theory]
        [InlineData("PREMIUM", "OAK")] [InlineData("STANDARD", "ASH")] [InlineData("ECONOMA", "SPRUCE")]
        public void SpeciesFollowsVisibilityClass_AndTheVariant(string tier, string classB)
        {
            var r = N(tier).Recalculate();
            Assert.All(r.Model.Families.Where(f => f.VisClass == 'A'), f => Assert.Equal("OAK", f.SpeciesId));
            Assert.All(r.Model.Families.Where(f => f.VisClass == 'B'), f => Assert.Equal(classB, f.SpeciesId));
            Assert.Contains(r.Model.Families, f => f.VisClass == 'B');
        }

        [Fact]
        public void Variants_ChangeMaterialCost_Monotonically()
        {
            double c(string t) => N(t).Recalculate().Cost.RawMaterial;
            Assert.True(c("PREMIUM") >= c("STANDARD") - 1e-6); Assert.True(c("STANDARD") >= c("ECONOMA") - 1e-6);
        }

        [Fact]
        public void WiderNightstand_ChangesStripsAndBiscuits_AndLegsAreDowelled()
        {
            var p = N(); var a = p.Recalculate(); int b0 = a.Model.Biscuits.Count;
            p.SetParameter("depth", 550); p.SetParameter("width", 800); var b = p.Recalculate();
            Assert.True(b.Model.Biscuits.Count >= b0); Assert.False(b.HasErrors);
            Assert.Equal(4, b.Model.Joints.Count(j => j.JointTypeId == "dowel" && j.PartAId.StartsWith("LEG")));
            Assert.True(b.Model.AllParts.All(x => x.Bounds.Size.X > 0 && x.Bounds.Size.Y > 0 && x.Bounds.Size.Z > 0));
        }

        [Fact]
        public void Geometry_IncludesSheetParts_AndCutPanels()
        {
            var p = N(); p.Recalculate();
            var prims = p.GenerateGeometry(RhinoWood.Core.Domain.DisplayMode.Normal);
            Assert.Contains(prims, x => x.Key == "BACK-1"); Assert.Contains(prims, x => x.Key == "DBOT-1");
            Assert.True(prims.Any(x => x.PartId == "SIDE-1" && x.Cuts != null && x.Cuts.Count > 0));
        }

        [Fact]
        public void PersistsAndReopens()
        {
            var p = N("PREMIUM"); var r = p.Recalculate();
            var (q, ok) = ProjectSerializer.Open(ProjectSerializer.Serialize(p, r), p.Library);
            Assert.Equal("casework.nightstand", q.Furniture.TypeId); Assert.Equal("OAK", q.Choices["materialB"]); Assert.True(ok);
        }
    }
}

namespace RhinoWood.Tests
{
    public class NightstandSheetTests
    {
        [Theory] [InlineData(RhinoWood.Core.Reports.SheetMode.Design)] [InlineData(RhinoWood.Core.Reports.SheetMode.Sale)]
        public void AllSheets_BuildForTheNightstand(RhinoWood.Core.Reports.SheetMode mode)
        {
            var p = WoodProject.Create("casework.nightstand", "n"); var r = p.Recalculate();
            var sheets = RhinoWood.Core.Reports.SheetBuilder.Build(p, r, mode, "STANDARD");
            Assert.NotEmpty(sheets); Assert.All(sheets, s => Assert.False(string.IsNullOrWhiteSpace(s.Body)));
            var html = RhinoWood.Core.Reports.SheetDocument.Html(sheets, "n");
            System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ns_" + mode + ".html"), html);
        }
    }
}

namespace RhinoWood.Tests
{
    public class DresserTests
    {
        private static WoodProject D(string tier = "STANDARD") { var p = WoodProject.Create("casework.dresser", "Comodă"); p.ApplyTier(tier); return p; }

        [Fact]
        public void Dresser_MatchesTheBedroomSheet()
        {
            var r = D().Recalculate();
            Assert.False(r.HasErrors, string.Join("\n", r.Issues.Where(i => i.Severity == RhinoWood.Core.Domain.Severity.Error)));
            Assert.Empty(r.Optimization.Unplaced);
            Assert.Equal(6, r.Model.AllParts.Count(p => p.Id.StartsWith("LEG")));                 // 4 corners + 2 under the separator
            Assert.Equal(8, r.Model.AllParts.Count(p => p.Id.StartsWith("FRONT")));                // 2 columns x 4 drawers
            Assert.Equal(8, r.Model.HardwareInstalls.Count(h => h.HardwareId == "SLIDE-SC"));
            Assert.Contains(r.Model.HardwareInstalls, h => h.HardwareId == "ANTITIP-KIT");       // ~87 kg, H 800 -> EN 14749
            var heights = r.Model.AllParts.Where(p => p.Id.StartsWith("FRONT-R") && p.Id.EndsWith("C1")).OrderBy(p => p.Bounds.Min.Z).Select(p => p.Finished.Width).ToList();
            Assert.Equal(new[] { 197.0, 174.0, 151.0, 128.0 }, heights.Select(h => System.Math.Round(h)).ToArray());   // graded fronts: 197 / 174 / 151 / 128
            Assert.Equal(897, r.Model.FindPart("FRONT-R1C1").Finished.Length, 6);                    // 1800/2 - 3
            Assert.Equal(661, r.Model.FindPart("SIDE-1").Finished.Length, 6);
        }

        [Fact]
        public void Columns_And_Drawers_ChangeTheGrid()
        {
            var p = D(); p.SetParameter("columns", 3); p.SetParameter("drawers", 3); var r = p.Recalculate();
            Assert.Equal(9, r.Model.AllParts.Count(x => x.Id.StartsWith("FRONT"))); Assert.Equal(8, r.Model.AllParts.Count(x => x.Id.StartsWith("LEG")));
            Assert.Equal(2, r.Model.AllParts.Count(x => x.Id.StartsWith("SEP"))); Assert.False(r.HasErrors);
        }

        [Theory] [InlineData("scoop")] [InlineData("handle")] [InlineData("push")] [InlineData("jrabbet")]
        public void FrontStyle_AddsTheMatchingFeaturesOrHardware(string style)
        {
            var p = D(); p.SetChoice("frontStyle", style); var r = p.Recalculate();
            var fr = r.Model.AllParts.Where(x => x.Id.StartsWith("FRONT")).ToList();
            if (style == "scoop") Assert.All(fr, f => Assert.Contains(f.Features, q => q.Kind == RhinoWood.Core.Domain.FeatureKind.RoutPocket));
            if (style == "jrabbet") Assert.All(fr, f => Assert.Contains(f.Features, q => q.Kind == RhinoWood.Core.Domain.FeatureKind.Rabbet));
            if (style == "handle") { Assert.Equal(8, r.Model.HardwareInstalls.Count(h => h.HardwareId == "HANDLE-128")); Assert.All(fr, f => Assert.Equal(2, f.Features.Count(q => q.Kind == RhinoWood.Core.Domain.FeatureKind.ThroughHole))); }
            if (style == "push") Assert.Equal(8, r.Model.HardwareInstalls.Count(h => h.HardwareId == "PUSH-OPEN"));
            Assert.False(r.HasErrors);
        }

        [Fact]
        public void VariantsKeepClassA_AndSaveMaterial()
        {
            double cost(string t) => D(t).Recalculate().Cost.RawMaterial;
            Assert.True(cost("PREMIUM") >= cost("STANDARD") - 1e-6 && cost("STANDARD") >= cost("ECONOMA") - 1e-6);
            Assert.All(D("ECONOMA").Recalculate().Model.Families.Where(f => f.VisClass == 'A'), f => Assert.Equal("OAK", f.SpeciesId));
        }

        [Fact]
        public void AllSheetsBuild()
        {
            var p = D(); var r = p.Recalculate();
            Assert.All(RhinoWood.Core.Reports.SheetBuilder.Build(p, r, RhinoWood.Core.Reports.SheetMode.Design), s => Assert.False(string.IsNullOrWhiteSpace(s.Body)));
        }
    }
}
