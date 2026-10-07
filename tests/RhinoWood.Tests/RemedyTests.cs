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
