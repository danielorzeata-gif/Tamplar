using System;
using System.Linq;
using RhinoWood.Core.Domain;
using RhinoWood.Core.Furniture;
using RhinoWood.Core.Libraries;
using RhinoWood.Core.Projects;
using RhinoWood.Core.Rules;
using RhinoWood.Core.Workspaces;
using Xunit;

namespace RhinoWood.Tests
{
    public class WorkspaceTests
    {
        private static (Workspace ws, Room room, PieceEntry a, PieceEntry b) TwoTables(string speciesA = "OAK")
        {
            var ws = new Workspace("Casa");
            var room = ws.AddRoom("Dormitor");
            var a = room.AddPiece("Masa 1", WoodProject.CreateTable("Masa 1", speciesA, ws.Library));
            var bp = WoodProject.CreateTable("Masa 2", "BEECH", ws.Library);
            bp.SetParameter("length", 1200); bp.SetParameter("width", 800);
            var b = room.AddPiece("Masa 2", bp);
            return (ws, room, a, b);
        }

        [Fact]
        public void FirstPiece_InitialisesSets_NextPiecesFollowThem()
        {
            var (_, room, a, b) = TwoTables();
            Assert.Equal("OAK", room.Aspect.Values["species"]);
            Assert.Equal("mortise-tenon", room.Structure.Values["joint.apron-long"]);
            Assert.Equal("OAK", b.Project.SpeciesId);                      // was BEECH, now follows the room set
            Assert.Equal(1200, b.Project.Parameters["length"]);            // dimensions are NOT part of a set
        }

        [Fact]
        public void ChangingTheSet_UpdatesAllLinkedPieces_AndReportsImpact()
        {
            var (_, room, a, b) = TwoTables();
            var impact = room.SetStyle(StyleKind.Aspect, "species", "ASH");
            Assert.Equal(2, impact.PiecesAffected);
            Assert.Equal("ASH", a.Project.SpeciesId); Assert.Equal("ASH", b.Project.SpeciesId);
            var impact2 = room.SetStyle(StyleKind.Structure, "joint.apron-long", "loose-tenon");
            Assert.Equal(2, impact2.PiecesAffected);
            Assert.Equal("loose-tenon", a.Project.Choices["jointApronLong"]);
            Assert.Equal("loose-tenon", b.Project.Choices["jointApronLong"]);
            Assert.Equal("mortise-tenon", b.Project.Choices["jointApronShort"]);       // separate key per role
        }

        [Fact]
        public void OnlyHere_DetachesJustThatField_WholeSet_UpdatesEveryone()
        {
            var (_, room, a, b) = TwoTables();
            var imp = room.ChangeField(a, "jointApronLong", "dowel", ChangeScope.OnlyHere);       // default behaviour
            Assert.Equal(new[] { "P1" }, imp.PieceIds);
            Assert.Equal("dowel", a.Project.Choices["jointApronLong"]);
            Assert.Equal("mortise-tenon", b.Project.Choices["jointApronLong"]);
            Assert.Equal("mortise-tenon", room.Structure.Values["joint.apron-long"]);              // set untouched
            Assert.Equal(ValueSource.PieceOverride, room.Fields(a).First(f => f.Key == "jointApronLong").Source);
            Assert.Equal(ValueSource.Set, room.Fields(a).First(f => f.Key == "jointApronShort").Source);   // other field still linked

            // later set change must NOT overwrite the detached field
            room.SetStyle(StyleKind.Structure, "joint.apron-long", "bridle");
            room.SetStyle(StyleKind.Structure, "joint.apron-short", "loose-tenon");
            Assert.Equal("dowel", a.Project.Choices["jointApronLong"]);
            Assert.Equal("loose-tenon", a.Project.Choices["jointApronShort"]);
            Assert.Equal("bridle", b.Project.Choices["jointApronLong"]);

            // "apply to the whole set" from piece B
            var whole = room.ChangeField(b, "topFixing", "TOP-BUTTON", ChangeScope.WholeSet);
            Assert.Equal(2, whole.PiecesAffected);
            Assert.Equal("TOP-BUTTON", a.Project.Choices["topFixing"]);
            Assert.Equal("TOP-BUTTON", room.Structure.Values["top.fixing"]);
        }

        [Fact]
        public void Relink_RestoresTheSetValue()
        {
            var (_, room, a, _) = TwoTables();
            room.ChangeField(a, "jointApronLong", "dowel");
            Assert.True(room.Relink(a, "jointApronLong"));
            Assert.Equal("mortise-tenon", a.Project.Choices["jointApronLong"]);
            Assert.Equal(ValueSource.Set, room.Fields(a).First(f => f.Key == "jointApronLong").Source);
        }

        [Fact]
        public void ValueNotAllowedForAPiece_IsAConflict_NotSilentlyApplied()
        {
            var (_, room, a, b) = TwoTables();
            var imp = room.SetStyle(StyleKind.Structure, "joint.apron-long", "dovetail");       // not an allowed apron joint
            Assert.NotEmpty(imp.Conflicts);
            Assert.Equal(0, imp.PiecesAffected);
            Assert.Equal("mortise-tenon", a.Project.Choices["jointApronLong"]);
            var imp2 = room.ChangeField(a, "jointApronLong", "dovetail", ChangeScope.WholeSet);
            Assert.NotEmpty(imp2.Conflicts);
        }

        [Fact]
        public void RoomOptimization_IsGlobal_NeverWorseThanSeparatePlans()
        {
            var (ws, room, a, b) = TwoTables();
            var r = ws.Recalculate(room);
            Assert.Equal(2, r.Pieces.Count);
            Assert.True(r.CombinedCost <= r.SeparateCostTotal + 1e-6, "combined " + r.CombinedCost + " vs separate " + r.SeparateCostTotal);
            Assert.True(r.CombinedBoards <= r.SeparateBoardsTotal);
            // every demand of every piece is placed exactly once and traceable to its piece
            var cuts = r.Combined.Boards.SelectMany(x => x.Cuts).Select(c => c.Demand.PartId).ToList();
            Assert.Equal(cuts.Count, cuts.Count(c => c.StartsWith("P1/") || c.StartsWith("P2/")));
            Assert.Contains(cuts, c => c.StartsWith("P1/LEG-")); Assert.Contains(cuts, c => c.StartsWith("P2/LEG-"));
            Assert.Equal(cuts.Count, a.Project.Recalculate().Demands.Count + b.Project.Recalculate().Demands.Count);
        }

        [Fact]
        public void Workspace_RoundTrips_WithSetsUnlinksAndDimensions()
        {
            var (ws, room, a, b) = TwoTables();
            room.ChangeField(a, "jointApronLong", "dowel");
            room.SetStyle(StyleKind.Aspect, "species", "ASH");
            var json = WorkspaceSerializer.Serialize(ws);
            var ws2 = WorkspaceSerializer.Deserialize(json);
            var room2 = ws2.Rooms.Single();
            var a2 = room2.Pieces.First(p => p.Id == "P1"); var b2 = room2.Pieces.First(p => p.Id == "P2");
            Assert.Equal("ASH", room2.Aspect.Values["species"]);
            Assert.Equal("ASH", a2.Project.SpeciesId);
            Assert.Equal("dowel", a2.Project.Choices["jointApronLong"]);
            Assert.Contains("jointApronLong", a2.Unlinked);
            Assert.Equal(1200, b2.Project.Parameters["length"]);
            Assert.Equal(ws.Recalculate(room).CombinedCost, ws2.Recalculate(room2).CombinedCost, 2);
            var added = room2.AddPiece("Masa 3", WoodProject.CreateTable("m3", "OAK", ws2.Library));
            Assert.Equal("P3", added.Id);
        }

        [Fact]
        public void TwoRooms_HaveIndependentSets()
        {
            var ws = new Workspace("Casa");
            var r1 = ws.AddRoom("Dormitor"); var r2 = ws.AddRoom("Bucatarie");
            r1.AddPiece("a", WoodProject.CreateTable("a", "OAK", ws.Library));
            r2.AddPiece("b", WoodProject.CreateTable("b", "BEECH", ws.Library));
            Assert.Equal("OAK", r1.Aspect.Values["species"]); Assert.Equal("BEECH", r2.Aspect.Values["species"]);
            r1.SetStyle(StyleKind.Aspect, "species", "ASH");
            Assert.Equal("BEECH", r2.Pieces[0].Project.SpeciesId);
        }
    }

    public class RuleTests
    {
        [Fact]
        public void Catalog_HasAll19Rules_WithForbiddenAndTypologyLevels()
        {
            Assert.Equal(19, RuleCatalog.All.Count(r => System.Text.RegularExpressions.Regex.IsMatch(r.Id, @"^R\d+$")));
            Assert.Contains(RuleCatalog.All, r => r.Id == "MIX-003");
            Assert.Equal(RuleConfidence.Forbidden, RuleCatalog.Get("R18").Confidence);
            Assert.Equal(RuleConfidence.TypologyOnly, RuleCatalog.Get("R19").Confidence);
            Assert.Contains("Janka", RuleCatalog.Get("R17").Note);
        }

        [Fact]
        public void StabilityThresholds_EN14749_EN12521()
        {
            Assert.True(SafetyRules.RequiresStabilityCheck(901, 10)); Assert.False(SafetyRules.RequiresStabilityCheck(900, 40 - 31));
            Assert.True(SafetyRules.RequiresStabilityCheck(351, 35)); Assert.False(SafetyRules.RequiresStabilityCheck(350, 100));
            Assert.True(SafetyRules.IsDelicateTable(0.30, 600, 10.1)); Assert.False(SafetyRules.IsDelicateTable(0.31, 700, 20));
        }

        [Fact]
        public void FingerTrapAndShearGap()
        {
            Assert.True(SafetyRules.IsFingerTrap(8, 10)); Assert.False(SafetyRules.IsFingerTrap(6, 30)); Assert.False(SafetyRules.IsFingerTrap(8, 9));
            Assert.True(SafetyRules.IsShearGap(20, true)); Assert.False(SafetyRules.IsShearGap(20, false)); Assert.False(SafetyRules.IsShearGap(5, true));
        }

        [Fact]
        public void PanelMovement_MatchesReportTable()
        {
            Assert.Equal(12.3, SafetyRules.PanelMovementMm(600, 0.41), 1);     // beech
            Assert.Equal(10.8, SafetyRules.PanelMovementMm(600, 0.36), 1);     // oak
            Assert.Equal(6.3, SafetyRules.PanelMovementMm(600, 0.21), 1);      // chestnut low end
        }

        [Fact]
        public void ChairCapacities_AndTenonE1_MatchReportExamples()
        {
            Assert.Equal(2384, SafetyRules.RequiredStaticChairCapacityN(1335), 0);
            Assert.Equal(2780, SafetyRules.RequiredStaticChairCapacityN(1557), 0);
            Assert.Equal(3575, SafetyRules.RequiredStaticChairCapacityN(2002), 0);
            var m = SafetyRules.TenonMomentE1(40, 40, 10, 10.3, tension: true, pva: true);      // beech, PVAc
            Assert.InRange(m.MeanNm, 155, 160);                                                 // report: ~158 N*m
            Assert.True(m.InDomain);
            Assert.Equal(0.68 * m.MeanNm, m.DesignNm, 6);
            Assert.False(SafetyRules.TenonMomentE1(60, 40, 10, 10.3).InDomain);                 // outside the validated domain
            Assert.Contains("needs testing", SafetyRules.TenonMomentE1(60, 40, 10, 10.3).Note);
        }

        [Fact]
        public void TenonGeometry_R5()
        {
            Assert.True(SafetyRules.CheckTenonGeometry(30, 20, 8, 25).LengthOk);
            Assert.False(SafetyRules.CheckTenonGeometry(50, 20, 8, 25).LengthOk);
            Assert.True(SafetyRules.CheckTenonGeometry(30, 20, 8, 25).ThicknessOk);
            Assert.False(SafetyRules.CheckTenonGeometry(30, 20, 3, 25).ThicknessOk);
        }

        [Fact]
        public void SpeciesData_UseVerifiedSources_AndNeverJanka()
        {
            var lib = WoodLibrary.CreateDefault();
            var oak = lib.GetSpecies("OAK");
            Assert.Equal(0.0036, oak.TangentialMovementPerPercent, 6);            // DIN 68100 diff_T 0.36 %/%
            Assert.Equal("[V-DATA]", oak.DataLabel); Assert.Equal(710, oak.DensityKgM3);
            Assert.Equal(10.3, lib.GetSpecies("BEECH").ShearStrengthMPa);
            Assert.All(lib.Species.Values, s => Assert.Equal(0, s.JankaHardnessN));  // R17: Janka must not be loaded
            Assert.Equal("[UNVERIFIED]", lib.GetSpecies("MAHOGANY").DataLabel);
            Assert.Equal(5, new RhinoWood.Core.Rules.ManufacturingRules().SeasonalMoisturePercent);
        }

        [Fact]
        public void StabilityRule_FlagsSmallTable_ButNotTheStandardOne()
        {
            var big = WoodProject.CreateTable("big").Recalculate();
            Assert.DoesNotContain(big.Issues, i => i.Code == "STABILITY_TABLE");
            var p = WoodProject.CreateTable("small"); p.SetParameter("length", 600); p.SetParameter("width", 400);
            Assert.Contains(p.Recalculate().Issues, i => i.Code == "STABILITY_TABLE");
        }
    }
}

namespace RhinoWood.Tests
{
    public class TierTests
    {
        [Theory]
        [InlineData("ECONOMA", "dowel", "dowel")] [InlineData("STANDARD", "mortise-tenon", "mortise-tenon")] [InlineData("PREMIUM", "japanese-kusabi", "mortise-tenon")]
        public void Tier_SetsJointsPerRole_AndProducesAValidTable(string tier, string longJoint, string shortJoint)
        {
            var p = WoodProject.CreateTable("t");
            p.ApplyTier(tier);
            Assert.Equal(longJoint, p.Choices["jointApronLong"]); Assert.Equal(shortJoint, p.Choices["jointApronShort"]);
            var r = p.Recalculate();
            Assert.False(r.HasErrors, string.Join("\n", r.Issues.Where(i => i.Severity == Severity.Error)));
            Assert.DoesNotContain(r.Issues, i => i.Code == "THROUGH_CONFLICT");   // premium: through tenon only on one rail family
            Assert.True(p.TierMatches);
        }

        [Fact]
        public void TiersAreOrdered_AndEveryTierHasAMetaLine()
        {
            var tiers = WoodProject.CreateTable("t").Furniture.Tiers;
            Assert.Equal(new[] { "ECONOMA", "STANDARD", "PREMIUM" }, tiers.Select(x => x.Id).ToArray());
            Assert.All(tiers, x => Assert.False(string.IsNullOrWhiteSpace(x.Meta)));
        }

        [Fact]
        public void ChangingAJointAfterATier_MarksItModified_AndPersists()
        {
            var p = WoodProject.CreateTable("t"); p.ApplyTier("PREMIUM");
            p.SetChoice("jointApronShort", "dowel");
            Assert.False(p.TierMatches);
            Assert.Equal("PREMIUM (modificat)", RhinoWood.Core.Reports.SheetBuilder.TierName(p));
            var (p2, ok) = ProjectSerializer.Open(ProjectSerializer.Serialize(p));
            Assert.True(ok); Assert.Equal("PREMIUM", p2.Tier); Assert.False(p2.TierMatches);
        }

        [Fact]
        public void Economa_HasNoTenons_AndShorterRails()
        {
            var std = WoodProject.CreateTable("a").Recalculate().Model.FindPart("APR-L-1").Finished.Length;
            var p = WoodProject.CreateTable("b"); p.ApplyTier("ECONOMA");
            var r = p.Recalculate();
            Assert.DoesNotContain(r.Model.FindPart("APR-L-1").Features, f => f.Kind == FeatureKind.Tenon);
            Assert.True(r.Model.FindPart("APR-L-1").Finished.Length < std);
        }

        [Fact]
        public void RoomTier_DrivesTheStructureSet_ForEveryLinkedPiece()
        {
            var ws = new Workspace("c"); var room = ws.AddRoom("Dormitor");
            var a = room.AddPiece("a", WoodProject.CreateTable("a", "OAK", ws.Library));
            var b = room.AddPiece("b", WoodProject.CreateTable("b", "OAK", ws.Library));
            room.ChangeField(b, "topFixing", "TOP-FIGURE8");                    // b deliberately differs
            var imp = room.ApplyTier("PREMIUM");
            Assert.Equal("japanese-kusabi", a.Project.Choices["jointApronLong"]); Assert.Equal("japanese-kusabi", b.Project.Choices["jointApronLong"]);
            Assert.Equal("TOP-BUTTON", a.Project.Choices["topFixing"]);
            Assert.Equal("TOP-FIGURE8", b.Project.Choices["topFixing"]);       // detached field is respected
            Assert.Equal("PREMIUM", room.Tier); Assert.Equal("PREMIUM", a.Project.Tier);
            var back = WorkspaceSerializer.Deserialize(WorkspaceSerializer.Serialize(ws));
            Assert.Equal("PREMIUM", back.Rooms.Single().Tier);
        }
    }
}
