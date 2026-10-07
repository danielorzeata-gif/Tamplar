using System;
using System.Collections.Generic;
using System.Linq;
using RhinoWood.Core.Domain;
using RhinoWood.Core.Libraries;
using RhinoWood.Core.Optimization;
using RhinoWood.Core.Rules;
using Xunit;

namespace RhinoWood.Tests
{
    public class OptimizerTests
    {
        private static WoodLibrary Lib(params double[] lengths)
        {
            var lib = new WoodLibrary();
            lib.Species["OAK"] = new WoodSpecies { Id = "OAK", Name = "Oak", PricePerM3 = 1400, SupplierId = "S" };
            lib.Stock.Add(new StockItem { Id = "OAK-90x90", SpeciesId = "OAK", Width = 90, Thickness = 90, Lengths = lengths.ToList(), SupplierId = "S" });
            return lib;
        }

        private static CutDemand Leg(int i) => new CutDemand { Id = "D" + i, PartId = "LEG-" + i, FamilyId = "F-LEG", FamilyName = "Leg", SpeciesId = "OAK", Length = 780, SecA = 90, SecB = 90, FinishedLength = 750, FinishedVolumeM3 = 750 * 80 * 80 / 1e9, Role = "Leg" };
        private static CutDemand Rail(int id, double len) => new CutDemand { Id = "D" + id, PartId = "RAIL-" + id, FamilyId = "F-RAIL", FamilyName = "Rail", SpeciesId = "OAK", Length = len, SecA = 90, SecB = 90, FinishedLength = len - 30, FinishedVolumeM3 = (len - 30) * 80 * 80 / 1e9, Role = "Rail" };

        private static ProjectSettings Settings(OptimizationStrategy s = OptimizationStrategy.MinPurchase, double reserve = 0)
            => new ProjectSettings { Strategy = s, GlobalReservePercent = reserve };

        /// <summary>MANDATORY end-to-end test (spec section 59).</summary>
        [Fact]
        public void FourLegs_Plus600_AreGloballyPacked_NotFourIndependentBeams()
        {
            var lib = Lib(3000);
            var demands = new[] { Leg(1), Leg(2), Leg(3), Leg(4), Rail(5, 600) };
            var res = new StockOptimizer(lib, Settings()).Optimize(demands);

            // 4 x 780 = 3120 > 3000, so legs cannot be on a single 3000 board, but they must NOT use 4 boards:
            Assert.Equal(2, res.BoardCount);
            // board with 780,780,780,600 (3 legs + the 600 rail), as specified
            var first = res.Boards.OrderByDescending(b => b.Cuts.Count).First();
            Assert.Equal(new[] { 780.0, 780.0, 780.0, 600.0 }, first.Cuts.Select(c => c.Length).OrderByDescending(x => x).ToArray());
            // the 4th leg sits on the second board, and its remnant is classified reusable
            Assert.Equal(1, res.Boards.Count(b => b.Cuts.Count == 1));
            Assert.All(res.Boards, b => Assert.True(b.Cuts.Sum(c => c.Length) + 3 * (b.Cuts.Count - 1) <= b.Length));
            Assert.Equal(2, res.Purchase.Sum(p => p.Quantity));
            Assert.Contains(res.Remnants, r => r.Class == RemnantClass.ProjectRemnant);
        }

        [Fact]
        public void FourLegs_Plus600_WithLongerCommercialLength_BuysSingleBoard()
        {
            var lib = Lib(3000, 4000, 5000);
            var res = new StockOptimizer(lib, Settings()).Optimize(new[] { Leg(1), Leg(2), Leg(3), Leg(4), Rail(5, 600) });
            Assert.Equal(1, res.BoardCount);
            Assert.Equal(4000, res.Boards[0].Length);
            Assert.Equal(5, res.Boards[0].Cuts.Count);
        }

        [Fact]
        public void FourLegs_Only_NeverFourIndependentBoards()
        {
            var res = new StockOptimizer(Lib(3000), Settings()).Optimize(new[] { Leg(1), Leg(2), Leg(3), Leg(4) });
            Assert.Equal(2, res.BoardCount);   // 3 + 1, and with 3000 remnant 660 reusable
        }

        [Fact]
        public void Reserve_IsAppliedOnce_AfterOptimization_AndCoveredByRemnants()
        {
            var lib = Lib(3000);
            var demands = new[] { Leg(1), Leg(2), Leg(3), Leg(4), Rail(5, 600) };
            var res10 = new StockOptimizer(lib, Settings(reserve: 10)).Optimize(demands);
            var res0 = new StockOptimizer(lib, Settings(reserve: 0)).Optimize(demands);
            // 10% of ~0.0229 m3 is far below the usable remnant volume -> no extra boards
            Assert.Equal(res0.BoardCount, res10.BoardCount);
            Assert.Equal(0, res10.Purchase.Sum(p => p.ReserveQuantity));
        }

        [Fact]
        public void Reserve_AddsBoards_WhenRemnantsDoNotCoverIt()
        {
            var lib = Lib(3000);
            // 3 x 1500 fill exactly 2 boards -> no remnant at all
            var demands = new[] { Rail(1, 1480), Rail(2, 1480), Rail(3, 1480), Rail(4, 1480) };
            var res = new StockOptimizer(lib, Settings(reserve: 10)).Optimize(demands);
            Assert.True(res.Purchase.Sum(p => p.ReserveQuantity) >= 1);
            Assert.Equal(res.PurchasedM3, res.Boards.Sum(b => b.VolumeM3), 9);
        }

        [Fact]
        public void MaterialSpecificReserve_AndProjectOverride_Win()
        {
            var lib = Lib(3000);
            lib.Species["OAK"].ReservePercent = 15;
            var s = Settings(reserve: 10);
            Assert.Equal(15, s.ReserveFor(lib.Species["OAK"], lib.Stock[0]));
            s.ReserveOverrides["OAK"] = 5;
            Assert.Equal(5, s.ReserveFor(lib.Species["OAK"], lib.Stock[0]));
        }

        [Fact]
        public void StockCannotProducePart_IsReportedAsError()
        {
            var res = new StockOptimizer(Lib(3000), Settings()).Optimize(new[] { Rail(1, 3500) });
            Assert.Single(res.Unplaced);
            Assert.Contains(res.Issues, i => i.Severity == Severity.Error && i.Code == "STOCK_CANNOT_PRODUCE");
        }

        [Fact]
        public void EveryPiece_IsPlacedExactlyOnce_AndTraceable()
        {
            var lib = Lib(3000, 4000);
            var demands = Enumerable.Range(1, 12).Select(i => Rail(i, 400 + 137 * i)).ToList();
            var res = new StockOptimizer(lib, Settings()).Optimize(demands);
            var placed = res.Boards.SelectMany(b => b.Cuts).Select(c => c.Demand.Id).OrderBy(x => x).ToList();
            Assert.Equal(demands.Select(d => d.Id).OrderBy(x => x).ToList(), placed);
            Assert.All(res.Boards, b => Assert.StartsWith("B", b.Id));
            Assert.Equal(res.Purchase.Sum(p => p.Quantity), res.BoardCount);
        }

        [Fact]
        public void Optimization_IsDeterministic()
        {
            var lib = Lib(3000, 4000, 5000);
            var demands = Enumerable.Range(1, 9).Select(i => Rail(i, 500 + 91 * i)).ToList();
            string Sig(OptimizationResult r) => string.Join("|", r.Boards.Select(b => b.Id + b.Length + string.Join(",", b.Cuts.Select(c => c.Demand.Id))));
            Assert.Equal(Sig(new StockOptimizer(lib, Settings()).Optimize(demands)), Sig(new StockOptimizer(lib, Settings()).Optimize(demands)));
        }

        [Theory]
        [InlineData(OptimizationStrategy.MinPurchase)]
        [InlineData(OptimizationStrategy.MinWaste)]
        [InlineData(OptimizationStrategy.MinCost)]
        [InlineData(OptimizationStrategy.MinBoards)]
        [InlineData(OptimizationStrategy.GrainFirst)]
        [InlineData(OptimizationStrategy.Balanced)]
        public void AllStrategies_ProduceValidPlans(OptimizationStrategy s)
        {
            var lib = Lib(3000, 4000, 5000);
            var demands = new[] { Leg(1), Leg(2), Leg(3), Leg(4), Rail(5, 600), Rail(6, 1700), Rail(7, 1700) };
            var res = new StockOptimizer(lib, Settings(s)).Optimize(demands);
            Assert.Equal(demands.Length, res.Boards.Sum(b => b.Cuts.Count));
            Assert.All(res.Boards, b => Assert.True(b.TailLength >= 0));
            Assert.True(res.TotalCost > 0);
        }

        [Fact]
        public void MinPurchase_NeverBuysMoreThanMinCostStrategy()
        {
            var lib = Lib(3000, 4000, 5000);
            var demands = new[] { Leg(1), Leg(2), Leg(3), Leg(4), Rail(5, 600), Rail(6, 1700), Rail(7, 1700) };
            var a = new StockOptimizer(lib, Settings(OptimizationStrategy.MinPurchase)).Optimize(demands);
            var b = new StockOptimizer(lib, Settings(OptimizationStrategy.MinCost)).Optimize(demands);
            Assert.True(a.PurchasedM3 <= b.PurchasedM3 + 1e-9);
        }

        [Fact]
        public void Kerf_IsAccountedFor()
        {
            var lib = Lib(3000);
            // 2 x 1498 + kerf 3 = 2999 fits; 2 x 1499 + 3 = 3001 does not
            var fit = new StockOptimizer(lib, Settings()).Optimize(new[] { Rail(1, 1498), Rail(2, 1498) });
            Assert.Equal(1, fit.BoardCount);
            var nofit = new StockOptimizer(lib, Settings()).Optimize(new[] { Rail(1, 1499), Rail(2, 1499) });
            Assert.Equal(2, nofit.BoardCount);
        }

        [Fact]
        public void ThinParts_ShareBoardsWithLargerSections_ButPreferTightProfiles()
        {
            var lib = Lib(3000);
            lib.Stock.Add(new StockItem { Id = "OAK-40x140", SpeciesId = "OAK", Width = 40, Thickness = 140, Lengths = new List<double> { 3000 }, SupplierId = "S" });
            var apron = new CutDemand { Id = "D1", PartId = "APR", FamilyId = "F-A", SpeciesId = "OAK", Length = 1000, SecA = 90, SecB = 30, FinishedVolumeM3 = 0.001 };
            var res = new StockOptimizer(lib, Settings()).Optimize(new[] { apron });
            Assert.Equal("OAK-40x140", res.Boards[0].Item.Id);   // cheaper tight profile than 90x90
        }
    }
}
