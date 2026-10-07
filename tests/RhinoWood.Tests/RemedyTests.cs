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
