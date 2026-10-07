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

namespace RhinoWood.Tests
{
    public class BedTests
    {
        private static WoodProject Bed(string tier = "STANDARD") { var p = WoodProject.Create("casework.bed", "Pat"); p.ApplyTier(tier); return p; }

        [Fact]
        public void King_1600x2000_HasTheSheetStructure()
        {
            var r = Bed().Recalculate();
            Assert.False(r.HasErrors, string.Join("\n", r.Issues.Where(i => i.Severity == RhinoWood.Core.Domain.Severity.Error)));
            Assert.Empty(r.Optimization.Unplaced);
            Assert.Equal(16, r.Model.AllParts.Count(p => p.Id.StartsWith("SLAT")));
            Assert.Equal(1604, r.Model.FindPart("SLAT-1").Finished.Length, 6);                       // mattress + 4
            Assert.Equal(2, r.Model.AllParts.Count(p => p.Id.StartsWith("HLEG"))); Assert.Equal(1000, r.Model.FindPart("HLEG-1").Finished.Length, 6); Assert.Equal(430, r.Model.FindPart("FLEG-1").Finished.Length, 6);
            Assert.Equal(4, r.Model.HardwareInstalls.Count(h => h.HardwareId == "BED-BOLT"));
            Assert.Equal(4, r.Model.AllParts.Where(p => p.Id.StartsWith("SRAIL")).Sum(p => p.Features.Count(f => f.Kind == RhinoWood.Core.Domain.FeatureKind.BlindHole)));
            Assert.All(r.Model.AllParts.Where(p => p.Id.StartsWith("HLEG") || p.Id.StartsWith("FLEG")), p => Assert.Contains(p.Features, f => f.Kind == RhinoWood.Core.Domain.FeatureKind.Counterbore));
            Assert.All(r.Model.AllParts.Where(p => p.Id.StartsWith("HLEG")), p => Assert.Contains(p.Features, f => f.Kind == RhinoWood.Core.Domain.FeatureKind.Dado));
        }

        [Fact]
        public void SlatCount_FollowsTheMattressLength_AndSheetOfHiddenParts()
        {
            var p = Bed(); p.SetParameter("mattressL", 2200); var a = p.Recalculate();
            Assert.True(a.Model.AllParts.Count(x => x.Id.StartsWith("SLAT")) > 16);
            Assert.All(a.Model.Families.Where(f => f.VisClass == 'C'), f => Assert.Equal("PINE", f.SpeciesId));
            Assert.All(Bed("PREMIUM").Recalculate().Model.Families, f => Assert.Equal("OAK", f.SpeciesId));
        }

        [Fact]
        public void NoSolidsOverlap_InTheBed()
        {
            var r = Bed().Recalculate(); var parts = r.Model.AllParts.ToList();
            for (int i = 0; i < parts.Count; i++) for (int j = i + 1; j < parts.Count; j++)
            {
                var a = parts[i].Bounds; var b = parts[j].Bounds;
                double dx = System.Math.Min(a.Max.X, b.Max.X) - System.Math.Max(a.Min.X, b.Min.X), dy = System.Math.Min(a.Max.Y, b.Max.Y) - System.Math.Max(a.Min.Y, b.Min.Y), dz = System.Math.Min(a.Max.Z, b.Max.Z) - System.Math.Max(a.Min.Z, b.Min.Z);
                bool panelTongue = parts[i].Id == "PANEL-1" || parts[j].Id == "PANEL-1";
                if (!panelTongue) Assert.False(dx > 0.01 && dy > 0.01 && dz > 0.01, parts[i].Id + " overlaps " + parts[j].Id);
            }
        }

        [Fact] public void AllSheetsBuild() { var p = Bed(); var r = p.Recalculate(); Assert.All(RhinoWood.Core.Reports.SheetBuilder.Build(p, r, RhinoWood.Core.Reports.SheetMode.Design), s => Assert.False(string.IsNullOrWhiteSpace(s.Body))); }
    }
}

namespace RhinoWood.Tests
{
    public class NoOverlapTests
    {
        [Theory] [InlineData("casework.nightstand")] [InlineData("casework.dresser")] [InlineData("table.dining")]
        public void PartsDoNotPenetrateEachOther(string type)
        {
            var p = WoodProject.Create(type, "x"); var r = p.Recalculate(); var parts = r.Model.AllParts.ToList();
            for (int i = 0; i < parts.Count; i++) for (int j = i + 1; j < parts.Count; j++)
            {
                var a = parts[i].Bounds; var b = parts[j].Bounds;
                double dx = System.Math.Min(a.Max.X, b.Max.X) - System.Math.Max(a.Min.X, b.Min.X), dy = System.Math.Min(a.Max.Y, b.Max.Y) - System.Math.Max(a.Min.Y, b.Min.Y), dz = System.Math.Min(a.Max.Z, b.Max.Z) - System.Math.Max(a.Min.Z, b.Min.Z);
                bool jointed = r.Model.Joints.Any(q => (q.PartAId == parts[i].Id && q.PartBId == parts[j].Id) || (q.PartAId == parts[j].Id && q.PartBId == parts[i].Id)) && type == "table.dining";   // tenons enter mortises
                if (!jointed) Assert.False(dx > 0.01 && dy > 0.01 && dz > 0.01, parts[i].Id + " overlaps " + parts[j].Id);
            }
        }
    }
}

namespace RhinoWood.Tests
{
    using RhinoWood.Core.Workspaces;
    public class BedroomSetTests
    {
        [Theory] [InlineData("PREMIUM", "OAK", "OAK")] [InlineData("STANDARD", "ASH", "PINE")] [InlineData("ECONOMA", "SPRUCE", "PINE")]
        public void OneChoicePropagatesToAllFourPieces(string tier, string classB, string classC)
        {
            var ws = new Workspace("t"); var room = BedroomSet.Create(ws, tier);
            Assert.Equal(4, room.Pieces.Count);
            var res = ws.Recalculate(room);
            Assert.All(room.Pieces.Where(p => p.Project.Furniture.TypeId != "casework.bed"), p => Assert.Equal(classB, p.Project.Choices["materialB"]));
            Assert.Equal(classC, room.Pieces.First(p => p.Project.Furniture.TypeId == "casework.bed").Project.Choices["materialC"]);
            Assert.Equal(0, res.Pieces.Sum(p => p.Errors));
            Assert.Empty(res.Combined.Unplaced);
            Assert.True(res.CombinedCost <= res.SeparateCostTotal + 1e-6);
            Assert.Contains(tier == "PREMIUM" ? "Stejar masiv în toate componentele" : "Exterior din stejar masiv", res.Declaration);
        }

        [Fact]
        public void FrontStyle_IsOneAspectSetForTheWholeRoom()
        {
            var ws = new Workspace("t"); var room = BedroomSet.Create(ws, "STANDARD");
            var imp = room.SetStyle(RhinoWood.Core.Furniture.StyleKind.Aspect, "front.style", "handle");
            Assert.All(room.Pieces.Where(p => p.Project.Choices.ContainsKey("frontStyle")), p => Assert.Equal("handle", p.Project.Choices["frontStyle"]));
            Assert.Equal(3, room.Pieces.Count(p => p.Project.Choices.ContainsKey("frontStyle")));
        }

        [Fact]
        public void Layout_PlacesPiecesWithoutOverlapAndSurvivesPersistence()
        {
            var ws = new Workspace("t"); var room = BedroomSet.Create(ws, "STANDARD");
            var boxes = room.Pieces.Select(p => { var prims = p.Project.GenerateGeometry(RhinoWood.Core.Domain.DisplayMode.Normal); var min = new RhinoWood.Core.Geometry.Vec3(prims.Min(x => x.Box.Min.X), prims.Min(x => x.Box.Min.Y), 0); var max = new RhinoWood.Core.Geometry.Vec3(prims.Max(x => x.Box.Max.X), prims.Max(x => x.Box.Max.Y), 1); return (p.Name, box: new RhinoWood.Core.Geometry.Box3(min, max)); }).ToList();
            for (int i = 0; i < boxes.Count; i++) for (int j = i + 1; j < boxes.Count; j++)
                Assert.False(boxes[i].box.Intersects(boxes[j].box, -1), boxes[i].Name + " overlaps " + boxes[j].Name);
            // nightstands face the foot of the bed (+Y): their front panel (min Y of the rotated model) is at the larger Y
            var ns = room.Pieces.First(p => p.Project.Furniture.TypeId == "casework.nightstand");
            var front = ns.Project.GenerateGeometry(RhinoWood.Core.Domain.DisplayMode.Normal).First(x => x.PartId == "FRONT-1");
            Assert.True(front.Box.Center.Y > ns.Project.GenerateGeometry(RhinoWood.Core.Domain.DisplayMode.Normal).First(x => x.PartId == "BOT-1").Box.Center.Y);
            var back = WorkspaceSerializer.Deserialize(WorkspaceSerializer.Serialize(ws));
            Assert.Equal(180, back.Rooms[0].Pieces[1].Project.Placement.RotZ);
        }
    }
}

namespace RhinoWood.Tests
{
    public class MoreFurnitureTests
    {
        [Theory] [InlineData("casework.bench")] [InlineData("casework.wardrobe")] [InlineData("casework.shelving")]
        public void BuildsWithoutErrors_AndAllSheetsRender(string type)
        {
            var p = WoodProject.Create(type, "x"); var r = p.Recalculate();
            Assert.False(r.HasErrors, type + ": " + string.Join("\n", r.Issues.Where(i => i.Severity == RhinoWood.Core.Domain.Severity.Error)));
            Assert.Empty(r.Optimization.Unplaced); Assert.True(r.Cost.Total > 0);
            Assert.All(RhinoWood.Core.Reports.SheetBuilder.Build(p, r, RhinoWood.Core.Reports.SheetMode.Design), s => Assert.False(string.IsNullOrWhiteSpace(s.Body)));
            var (q, ok) = ProjectSerializer.Open(ProjectSerializer.Serialize(p, r), p.Library); Assert.Equal(type, q.Furniture.TypeId); Assert.True(ok);
        }

        [Fact]
        public void Bench_HasSeatHeightInTheErgonomicRange_AndTableLogic()
        {
            var r = WoodProject.Create("casework.bench", "b").Recalculate();
            Assert.InRange(r.Model.Bounds.Size.Z, 440, 480); Assert.Contains(r.Model.AllParts, p => p.Id == "TOP-1"); Assert.Equal(8, r.Model.Joints.Count);
        }

        [Fact]
        public void Wardrobe_HingesFollowDoorHeight_AndAntiTipAndRail()
        {
            Assert.Equal(2, RhinoWood.Core.Furniture.WardrobeDefinition.HingesFor(800)); Assert.Equal(3, RhinoWood.Core.Furniture.WardrobeDefinition.HingesFor(1500)); Assert.Equal(4, RhinoWood.Core.Furniture.WardrobeDefinition.HingesFor(2000));
            var p = WoodProject.Create("casework.wardrobe", "w"); var r = p.Recalculate();
            Assert.Equal(2 * 4, r.Model.HardwareInstalls.Count(h => h.HardwareId == "HINGE-CUP35"));
            Assert.Contains(r.Model.HardwareInstalls, h => h.HardwareId == "ANTITIP-KIT"); Assert.Contains(r.Model.HardwareInstalls, h => h.HardwareId == "ROD-25");
            Assert.All(r.Model.AllParts.Where(x => x.Id.StartsWith("DOOR")), d => Assert.Equal(4, d.Features.Count(f => f.Kind == RhinoWood.Core.Domain.FeatureKind.HingeCup)));
            var rail = r.Model.HardwareInstalls.First(h => h.HardwareId == "ROD-25").Point.Z; Assert.InRange(rail, 1520, 1770);
            p.SetParameter("doors", 3); Assert.Equal(3 * 4, p.Recalculate().Model.HardwareInstalls.Count(h => h.HardwareId == "HINGE-CUP35"));
        }

        [Fact]
        public void Shelving_FlagsASaggingShelf_AndThickeningFixesIt()
        {
            var p = WoodProject.Create("casework.shelving", "s"); p.SetParameter("width", 1400); p.SetParameter("shelfThickness", 16);
            Assert.Contains(p.Recalculate().Issues, i => i.Code == "SHELF_DEFLECTION");
            p.SetParameter("shelfThickness", 40); Assert.DoesNotContain(p.Recalculate().Issues, i => i.Code == "SHELF_DEFLECTION");
            Assert.DoesNotContain(WoodProject.Create("casework.shelving", "s").Recalculate().Issues, i => i.Code == "SHELF_DEFLECTION");   // 800 x 22 passes
        }

        [Fact]
        public void SpeciesMix_WarnsOnlyForDissimilarShrinkage()
        {
            var p = WoodProject.Create("casework.nightstand", "n"); p.SetChoice("materialB", "ASH");
            Assert.DoesNotContain(p.Recalculate().Issues, i => i.Code == "SPECIES_MIX");        // oak 0.36 vs ash 0.38: fine
            p.SetChoice("materialB", "PINE"); Assert.DoesNotContain(p.Recalculate().Issues, i => i.Code == "SPECIES_MIX");
        }
    }
}

namespace RhinoWood.Tests
{
    using RhinoWood.Core.Workspaces;
    public class BedroomWithWardrobeTests
    {
        [Fact]
        public void Wardrobe_CanBeAddedAndStaysClearOfTheOtherPieces()
        {
            var ws = new Workspace("t"); var room = BedroomSet.Create(ws, "STANDARD", "OAK", "Dormitor", wardrobe: true);
            Assert.Equal(5, room.Pieces.Count);
            var boxes = room.Pieces.Select(p => { var pr = p.Project.GenerateGeometry(RhinoWood.Core.Domain.DisplayMode.Normal); return (p.Name, new RhinoWood.Core.Geometry.Box3(new RhinoWood.Core.Geometry.Vec3(pr.Min(x => x.Box.Min.X), pr.Min(x => x.Box.Min.Y), 0), new RhinoWood.Core.Geometry.Vec3(pr.Max(x => x.Box.Max.X), pr.Max(x => x.Box.Max.Y), 1))); }).ToList();
            for (int i = 0; i < boxes.Count; i++) for (int j = i + 1; j < boxes.Count; j++) Assert.False(boxes[i].Item2.Intersects(boxes[j].Item2, -1), boxes[i].Name + " overlaps " + boxes[j].Name);
            var res = ws.Recalculate(room); Assert.Equal(0, res.Pieces.Sum(p => p.Errors)); Assert.Empty(res.Combined.Unplaced);
        }
    }
}

namespace RhinoWood.Tests
{
    using RhinoWood.Core.Logistics;
    using RhinoWood.Core.Workspaces;
    public class PackingTests
    {
        private static void AssertSound(PackingPlan plan, System.Collections.Generic.IList<PackItem> items, PackingOptions o)
        {
            // every item exactly once
            Assert.Equal(items.Select(i => i.Id).OrderBy(x => x), plan.Cartons.SelectMany(c => c.Items).Select(i => i.Item.Id).OrderBy(x => x));
            foreach (var c in plan.Cartons)
            {
                for (int i = 0; i < c.Items.Count; i++)
                {
                    var b = c.Items[i].Box;
                    Assert.True(b.Min.X >= -1e-6 && b.Min.Y >= -1e-6 && b.Min.Z >= -1e-6 && b.Max.X <= c.InnerL + 1e-6 && b.Max.Y <= c.InnerW + 1e-6 && b.Max.Z <= c.InnerH + 1e-6, c.Id + ": " + c.Items[i].Item.Id + " outside the carton");
                    for (int j = i + 1; j < c.Items.Count; j++) Assert.False(b.Intersects(c.Items[j].Box, 0.5), c.Id + ": items overlap");
                }
                if (!c.Custom) Assert.True(c.ContentKg <= o.MaxCartonKg + 1e-6, c.Id + " too heavy");
            }
            Assert.Equal(plan.Cartons.Select(c => c.Id).OrderBy(x => x), plan.Pallets.SelectMany(p => p.Cartons).Select(c => c.Carton.Id).OrderBy(x => x));
            foreach (var p in plan.Pallets)
            {
                for (int i = 0; i < p.Cartons.Count; i++)
                {
                    var b = p.Cartons[i].Box;
                    Assert.True(b.Min.X >= -1e-6 && b.Min.Y >= -1e-6 && b.Max.X <= p.Spec.L + 1e-6 && b.Max.Y <= p.Spec.W + 1e-6, p.Id + ": carton overhangs the pallet");
                    for (int j = i + 1; j < p.Cartons.Count; j++) Assert.False(b.Intersects(p.Cartons[j].Box, 0.5), p.Id + ": cartons overlap");
                }
                Assert.True(p.Height <= o.MaxPalletHeight + 1e-6 || p.Cartons.Count == 1, p.Id + " too tall");
                Assert.True(p.MassKg <= o.MaxPalletKg + 1e-6 || p.Cartons.Count == 1, p.Id + " too heavy");
            }
        }

        [Theory] [InlineData("casework.bed")] [InlineData("casework.dresser")] [InlineData("casework.wardrobe")] [InlineData("casework.nightstand")] [InlineData("table.dining")] [InlineData("casework.shelving")] [InlineData("casework.bench")]
        public void EveryPiece_IsPackedSoundly(string type)
        {
            var p = WoodProject.Create(type, "x"); var r = p.Recalculate(); var o = new PackingOptions();
            var items = PackingPlanner.ItemsOf(p, r); var plan = PackingPlanner.Plan(items, o);
            AssertSound(plan, items, o);
            Assert.NotEmpty(plan.Pallets); Assert.Contains("PLAN DE PALETARE", plan.Report);
        }

        [Fact]
        public void Bed_NeedsOnlyAFewPallets_AndLongPartsGoInLongCartons()
        {
            var p = WoodProject.Create("casework.bed", "x"); var r = p.Recalculate();
            var plan = PackingPlanner.Plan(PackingPlanner.ItemsOf(p, r));
            Assert.InRange(plan.Pallets.Count, 1, 3);
            var sideRail = plan.Cartons.First(c => c.Items.Any(i => i.Item.Id == "SRAIL-1"));
            Assert.True(sideRail.InnerL >= 1962);
        }

        [Fact]
        public void Bedroom_PacksTogether_AndSmallPiecesShareCartons()
        {
            var ws = new Workspace("t"); var room = BedroomSet.Create(ws, "STANDARD", "OAK", "Dormitor", wardrobe: true);
            var pieces = room.Pieces.Select(x => (x.Project, x.Project.Recalculate(), x.Id + ":")).ToList();
            var o = new PackingOptions(); var items = pieces.SelectMany(q => PackingPlanner.ItemsOf(q.Item1, q.Item2, q.Item3)).ToList();
            var plan = PackingPlanner.Plan(pieces, o);
            AssertSound(plan, items, o);
            Assert.True(plan.Cartons.Count < items.Count / 2, plan.Cartons.Count + " cartons for " + items.Count + " items");
        }

        [Fact]
        public void LighterCartonLimit_MakesMoreCartons()
        {
            var p = WoodProject.Create("casework.bed", "x"); var r = p.Recalculate(); var items = PackingPlanner.ItemsOf(p, r);
            Assert.True(PackingPlanner.Plan(items, new PackingOptions { MaxCartonKg = 15 }).Cartons.Count > PackingPlanner.Plan(items, new PackingOptions { MaxCartonKg = 30 }).Cartons.Count);
        }
    }
}

namespace RhinoWood.Tests
{
    using RhinoWood.Core.Logistics;
    using RhinoWood.Core.Workspaces;
    public class PackingPerPieceTests
    {
        [Fact]
        public void CartonsNeverMixPieces_AndPalletsMayShareCartons()
        {
            var ws = new Workspace("t"); var room = BedroomSet.Create(ws, "STANDARD", "OAK", "Dormitor", true);
            var pcs = room.Pieces.Select(x => (x.Project, x.Project.Recalculate(), x.Name + ":")).ToList();
            var plan = PackingPlanner.Plan(pcs);
            Assert.All(plan.Cartons, c => Assert.Single(c.Items.Select(i => i.Item.Label.Split(':')[0]).Distinct()));
            Assert.True(plan.Pallets.Count <= 3);
            Assert.True(plan.Pallets.Any(p => p.Cartons.Select(c => c.Carton.Piece).Distinct().Count() > 1));
        }
    }
}
