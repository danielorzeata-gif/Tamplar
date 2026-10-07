using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using RhinoWood.Core.Domain;
using RhinoWood.Core.Libraries;
using RhinoWood.Core.Rules;

namespace RhinoWood.Core.Optimization
{
    /// <summary>One rough piece that has to be cut from stock. Traceable to its Part Id.</summary>
    public sealed class CutDemand
    {
        public string Id { get; set; }
        public string PartId { get; set; }
        public string FamilyId { get; set; }
        public string FamilyName { get; set; }
        public string SpeciesId { get; set; }
        public double Length { get; set; }
        /// <summary>Rough cross-section, SecA &gt;= SecB.</summary>
        public double SecA { get; set; }
        public double SecB { get; set; }
        public string GrainGroup { get; set; }
        public double FinishedVolumeM3 { get; set; }
        public double FinishedLength { get; set; }
        public string Role { get; set; }
        public double RoughVolumeM3 => Length * SecA * SecB / 1e9;
    }

    public sealed class CutPlacement
    {
        public string CutId { get; set; }
        public CutDemand Demand { get; set; }
        public double Start { get; set; }
        public double Length => Demand.Length;
    }

    public sealed class Board
    {
        public string Id { get; set; }
        public StockItem Item { get; set; }
        public double Length { get; set; }
        public bool IsReserve { get; set; }
        public double Price { get; set; }
        public List<CutDemand> Pieces { get; set; } = new List<CutDemand>();
        public List<CutPlacement> Cuts { get; set; } = new List<CutPlacement>();
        public double TailLength { get; set; }
        public RemnantClass TailClass { get; set; }
        public double KerfLoss { get; set; }
        public double TrimLoss { get; set; }
        public double VolumeM3 => Item.Volume(Length);
    }

    public sealed class RemnantInfo
    {
        public string BoardId { get; set; }
        public string StockItemId { get; set; }
        public double Length { get; set; }
        public double VolumeM3 { get; set; }
        public RemnantClass Class { get; set; }
    }

    public sealed class PurchaseLine
    {
        public string Id { get; set; }
        public StockItem Item { get; set; }
        public string SupplierId { get; set; }
        public double Length { get; set; }
        public int Quantity { get; set; }
        public int ReserveQuantity { get; set; }
        public double UnitPrice { get; set; }
        public double Total => Math.Round(UnitPrice * Quantity, 2);
        public List<string> BoardIds { get; set; } = new List<string>();
        public double ExpectedUsableRemnantM3 { get; set; }
        public double ExpectedWasteM3 { get; set; }
    }

    public sealed class CandidateSummary
    {
        public string Policy { get; set; }
        public int Boards { get; set; }
        public double PurchasedM3 { get; set; }
        public double Cost { get; set; }
        public double WasteM3 { get; set; }
        public bool Selected { get; set; }
    }

    public sealed class OptimizationResult
    {
        public OptimizationStrategy Strategy { get; set; }
        public List<Board> Boards { get; set; } = new List<Board>();
        public List<PurchaseLine> Purchase { get; set; } = new List<PurchaseLine>();
        public List<RemnantInfo> Remnants { get; set; } = new List<RemnantInfo>();
        public List<CutDemand> Unplaced { get; set; } = new List<CutDemand>();
        public List<Issue> Issues { get; set; } = new List<Issue>();
        public List<string> Explanation { get; set; } = new List<string>();
        public List<CandidateSummary> Candidates { get; set; } = new List<CandidateSummary>();

        public double TheoreticalFinishedM3 { get; set; }
        public double TheoreticalLinearM { get; set; }
        public double RoughRequiredM3 { get; set; }          // sum of rough pieces (with allowances)
        public double OptimizedManufacturingM3 { get; set; } // rough + kerf + trims (what must physically be consumed)
        public double PurchasedM3 { get; set; }              // including reserve boards
        public double PurchasedBeforeReserveM3 { get; set; }
        public double ReserveM3 { get; set; }
        public double ReusableRemnantM3 { get; set; }
        public double ProcessWasteM3 { get; set; }           // kerf + trim + section excess + planing/machining allowance
        public double KerfM3 { get; set; }
        public double ScrapM3 { get; set; }
        public double TotalCost { get; set; }
        public int BoardCount => Boards.Count;
        public double Utilization => PurchasedM3 <= 0 ? 0 : RoughRequiredM3 / PurchasedM3;
        public Dictionary<string, double> ReservePercentBySpecies { get; set; } = new Dictionary<string, double>();
    }

    public sealed class StockOptimizer
    {
        private readonly WoodLibrary _lib;
        private readonly ProjectSettings _settings;
        private ManufacturingRules R => _settings.Rules;

        public StockOptimizer(WoodLibrary lib, ProjectSettings settings) { _lib = lib; _settings = settings; }

        private enum OrderMode { LongestFirst, ConstrainedFirst }
        private enum LenMode { Utilization, Smallest, Largest, Fixed }
        private sealed class Policy { public OrderMode Order; public LenMode Len; public double Fixed; public string Name; }

        // ------------------------------------------------------------ board arithmetic
        private double Needed(IEnumerable<CutDemand> pieces)
        {
            int n = 0; double sum = 0;
            foreach (var p in pieces) { n++; sum += p.Length; }
            return n == 0 ? 0 : sum + R.SawKerf * (n - 1) + 2 * R.EndTrim;
        }
        private bool Fits(StockItem item, double len, IEnumerable<CutDemand> pieces) =>
            Needed(pieces) <= len + 1e-9 && pieces.All(p => p.SpeciesId == item.SpeciesId && item.SectionFits(p.SecA, p.SecB));

        private sealed class Draft { public StockItem Item; public double Length; public List<CutDemand> Pieces = new List<CutDemand>(); }

        private double Price(StockItem i, double len) => _lib.PiecePrice(i, len);

        private IEnumerable<(StockItem item, double len)> Options(string species) =>
            _lib.StockFor(species).SelectMany(i => i.Lengths.Select(l => (i, l)));

        // ------------------------------------------------------------ main entry
        /// <summary>
        /// Global optimization of all demands. <paramref name="seedGroups"/> (e.g. the pieces of a room) lets the optimizer also start from the best
        /// plan of each group and then merge across groups, so the global plan is never worse than the groups planned separately.
        /// </summary>
        public OptimizationResult Optimize(IList<CutDemand> demands, IEnumerable<IList<CutDemand>> seedGroups = null)
        {
            var res = new OptimizationResult { Strategy = _settings.Strategy };
            var placeable = new List<CutDemand>();
            foreach (var d in demands)
            {
                bool ok = Options(d.SpeciesId).Any(o => o.item.SectionFits(d.SecA, d.SecB) && d.Length + 2 * R.EndTrim <= o.len + 1e-9);
                if (ok) placeable.Add(d);
                else
                {
                    res.Unplaced.Add(d);
                    res.Issues.Add(new Issue { Severity = Severity.Error, Code = "STOCK_CANNOT_PRODUCE", SubjectId = d.PartId, Message = "Commercial stock cannot produce " + d.PartId + " (" + d.Role + "): rough " + d.Length.ToString("0.#", CultureInfo.InvariantCulture) + " x " + d.SecA.ToString("0.#", CultureInfo.InvariantCulture) + " x " + d.SecB.ToString("0.#", CultureInfo.InvariantCulture) + " " + d.SpeciesId + "." });
                }
            }

            // theoretical numbers (before allowances)
            res.TheoreticalFinishedM3 = demands.Sum(d => d.FinishedVolumeM3);
            res.TheoreticalLinearM = demands.Sum(d => d.FinishedLength) / 1000.0;
            res.RoughRequiredM3 = demands.Sum(d => d.RoughVolumeM3);

            var placeableIds = new HashSet<string>(placeable.Select(d => d.Id));
            var groups = seedGroups?.Select(g => g.Where(d => placeableIds.Contains(d.Id)).ToList()).Where(g => g.Count > 0).ToList();
            var best = ChoosePlan(placeable, res, groups);
            Finalize(res, best);
            return res;
        }

        private List<(Policy pol, List<Draft> plan)> GeneratePlans(List<CutDemand> demands)
        {
            var policies = new List<Policy>();
            foreach (var order in new[] { OrderMode.LongestFirst, OrderMode.ConstrainedFirst })
            {
                policies.Add(new Policy { Order = order, Len = LenMode.Utilization, Name = order + "/utilization" });
                policies.Add(new Policy { Order = order, Len = LenMode.Smallest, Name = order + "/smallest-fit" });
                policies.Add(new Policy { Order = order, Len = LenMode.Largest, Name = order + "/largest-stock" });
                foreach (var l in demands.SelectMany(d => Options(d.SpeciesId)).Select(o => o.len).Distinct().OrderBy(x => x))
                    policies.Add(new Policy { Order = order, Len = LenMode.Fixed, Fixed = l, Name = order + "/fixed-" + l.ToString("0", CultureInfo.InvariantCulture) });
            }
            var plans = new List<(Policy pol, List<Draft> plan)>();
            foreach (var p in policies)
            {
                if (demands.Count == 0) break;
                var plan = Pack(demands, p);
                Improve(plan);
                plans.Add((p, plan));
            }
            return plans;
        }

        private List<Draft> ChoosePlan(List<CutDemand> demands, OptimizationResult res, List<List<CutDemand>> seedGroups = null)
        {
            var plans = GeneratePlans(demands);
            if (plans.Count == 0) return new List<Draft>();

            if (seedGroups != null && seedGroups.Count > 1)
            {
                // best plan of every group planned alone, concatenated, then improved across groups
                var seeded = new List<Draft>();
                foreach (var g in seedGroups)
                {
                    var gp = GeneratePlans(g);
                    var gm = gp.Select(x => Metrics(x.plan)).ToList();
                    seeded.AddRange(gp[SelectBest(gm)].plan.Select(d => new Draft { Item = d.Item, Length = d.Length, Pieces = d.Pieces.ToList() }));
                }
                Improve(seeded);
                plans.Add((new Policy { Name = "seed:groups+merge" }, seeded));
            }

            var metrics = plans.Select(x => Metrics(x.plan)).ToList();
            int bestIdx = SelectBest(metrics);
            for (int i = 0; i < plans.Count; i++)
                res.Candidates.Add(new CandidateSummary { Policy = plans[i].pol.Name, Boards = plans[i].plan.Count, PurchasedM3 = metrics[i].Purchased, Cost = metrics[i].Cost, WasteM3 = metrics[i].Waste, Selected = i == bestIdx });
            // de-duplicate identical candidates for readability
            res.Candidates = res.Candidates.GroupBy(c => c.Boards + "|" + c.PurchasedM3.ToString("R", CultureInfo.InvariantCulture) + "|" + c.Cost.ToString("R", CultureInfo.InvariantCulture))
                .Select(g => g.OrderByDescending(c => c.Selected).First()).ToList();
            return plans[bestIdx].plan;
        }

        // ------------------------------------------------------------ packing
        private List<Draft> Pack(List<CutDemand> demands, Policy pol)
        {
            var eligibleCount = demands.ToDictionary(d => d.Id, d => Options(d.SpeciesId).Count(o => o.item.SectionFits(d.SecA, d.SecB)));
            IEnumerable<CutDemand> ordered = pol.Order == OrderMode.LongestFirst
                ? demands.OrderByDescending(d => d.Length).ThenBy(d => d.Id, StringComparer.Ordinal)
                : demands.OrderBy(d => eligibleCount[d.Id]).ThenByDescending(d => d.Length).ThenBy(d => d.Id, StringComparer.Ordinal);
            var pending = ordered.ToList();
            var plan = new List<Draft>();

            while (pending.Count > 0)
            {
                var d = pending[0]; pending.RemoveAt(0);
                var target = FindExisting(plan, d);
                if (target != null) { target.Pieces.Add(d); continue; }
                var (item, len) = ChooseNew(d, pending, pol);
                var nb = new Draft { Item = item, Length = len };
                nb.Pieces.Add(d);
                plan.Add(nb);
            }
            return plan;
        }

        private Draft FindExisting(List<Draft> plan, CutDemand d)
        {
            Draft best = null; double bestScore = double.MaxValue;
            foreach (var b in plan)
            {
                if (b.Item.SpeciesId != d.SpeciesId || !b.Item.SectionFits(d.SecA, d.SecB)) continue;
                double need = Needed(b.Pieces.Concat(new[] { d }));
                if (need > b.Length + 1e-9) continue;
                double leftover = b.Length - need;
                double score = leftover + b.Item.SectionArea * 1e-3;   // best-fit, prefer tight sections
                if (_settings.Strategy == OptimizationStrategy.GrainFirst && d.GrainGroup != null && b.Pieces.Any(p => p.GrainGroup == d.GrainGroup)) score -= 1e6;
                if (score < bestScore) { bestScore = score; best = b; }
            }
            return best;
        }

        private (StockItem, double) ChooseNew(CutDemand d, List<CutDemand> pending, Policy pol)
        {
            var cands = Options(d.SpeciesId)
                .Where(o => o.item.SectionFits(d.SecA, d.SecB) && d.Length + 2 * R.EndTrim <= o.len + 1e-9).ToList();
            switch (pol.Len)
            {
                case LenMode.Smallest:
                    return cands.OrderBy(o => Price(o.item, o.len)).ThenBy(o => o.item.SectionArea).ThenBy(o => o.item.Id, StringComparer.Ordinal).First();
                case LenMode.Largest:
                {
                    double minArea = cands.Min(o => o.item.SectionArea);
                    return cands.Where(o => o.item.SectionArea <= minArea + 1e-9).OrderByDescending(o => o.len).ThenBy(o => o.item.Id, StringComparer.Ordinal).First();
                }
                case LenMode.Fixed:
                {
                    var exact = cands.Where(o => Math.Abs(o.len - pol.Fixed) < 1e-9).ToList();
                    if (exact.Count > 0) return exact.OrderBy(o => o.item.SectionArea).ThenBy(o => o.item.Id, StringComparer.Ordinal).First();
                    return cands.OrderBy(o => Price(o.item, o.len)).ThenBy(o => o.item.SectionArea).ThenBy(o => o.item.Id, StringComparer.Ordinal).First();
                }
                default:
                    return cands.OrderBy(o => SimScore(o.item, o.len, d, pending)).ThenBy(o => Price(o.item, o.len))
                        .ThenBy(o => o.item.Id, StringComparer.Ordinal).First();
            }
        }

        /// <summary>Cost per usefully filled volume if we open this board and greedily fill it with pending pieces.</summary>
        private double SimScore(StockItem item, double len, CutDemand first, List<CutDemand> pending)
        {
            var fill = new List<CutDemand> { first };
            foreach (var p in pending.OrderByDescending(x => x.Length))
            {
                if (p.SpeciesId != item.SpeciesId || !item.SectionFits(p.SecA, p.SecB)) continue;
                fill.Add(p);
                if (Needed(fill) > len + 1e-9) fill.RemoveAt(fill.Count - 1);
            }
            double vol = fill.Sum(x => x.RoughVolumeM3);
            return vol <= 0 ? double.MaxValue : Price(item, len) / vol;
        }

        // ------------------------------------------------------------ improvement
        private void Improve(List<Draft> plan)
        {
            for (int iter = 0; iter < 6; iter++)
            {
                bool changed = false;
                changed |= Retype(plan);
                changed |= MergePairs(plan);
                changed |= EmptyBoards(plan);
                if (!changed) break;
            }
            Retype(plan);
        }

        private bool Retype(List<Draft> plan)
        {
            bool changed = false;
            foreach (var b in plan)
            {
                var sp = b.Item.SpeciesId;
                var best = Options(sp).Where(o => Fits(o.item, o.len, b.Pieces))
                    .OrderBy(o => Price(o.item, o.len)).ThenBy(o => o.item.SectionArea).ThenBy(o => o.item.Id, StringComparer.Ordinal).FirstOrDefault();
                if (best.item != null && Price(best.item, best.len) < Price(b.Item, b.Length) - 1e-9)
                { b.Item = best.item; b.Length = best.len; changed = true; }
            }
            return changed;
        }

        private bool MergePairs(List<Draft> plan)
        {
            for (int i = 0; i < plan.Count; i++)
                for (int j = i + 1; j < plan.Count; j++)
                {
                    if (plan[i].Item.SpeciesId != plan[j].Item.SpeciesId) continue;
                    var union = plan[i].Pieces.Concat(plan[j].Pieces).ToList();
                    var best = Options(plan[i].Item.SpeciesId).Where(o => Fits(o.item, o.len, union))
                        .OrderBy(o => Price(o.item, o.len)).ThenBy(o => o.item.Id, StringComparer.Ordinal).FirstOrDefault();
                    if (best.item == null) continue;
                    double before = Price(plan[i].Item, plan[i].Length) + Price(plan[j].Item, plan[j].Length);
                    double volBefore = plan[i].Item.Volume(plan[i].Length) + plan[j].Item.Volume(plan[j].Length);
                    if (Price(best.item, best.len) < before - 1e-9 || (Math.Abs(Price(best.item, best.len) - before) < 1e-9 && best.item.Volume(best.len) < volBefore - 1e-12))
                    {
                        plan[i].Item = best.item; plan[i].Length = best.len; plan[i].Pieces = union;
                        plan.RemoveAt(j);
                        return true;
                    }
                }
            return false;
        }

        private bool EmptyBoards(List<Draft> plan)
        {
            foreach (var victim in plan.OrderBy(b => b.Pieces.Sum(p => p.Length)).ToList())
            {
                var snapshot = plan.Where(b => b != victim).ToDictionary(b => b, b => b.Pieces.ToList());
                var moved = true;
                foreach (var piece in victim.Pieces.OrderByDescending(p => p.Length))
                {
                    var target = FindExisting(plan.Where(b => b != victim).ToList(), piece);
                    if (target == null) { moved = false; break; }
                    target.Pieces.Add(piece);
                }
                if (moved) { plan.Remove(victim); return true; }
                foreach (var kv in snapshot) kv.Key.Pieces = kv.Value;
            }
            return false;
        }

        // ------------------------------------------------------------ metrics / selection
        private sealed class PlanMetrics { public double Purchased, Cost, Waste; public int Boards; public double Grain; }

        private PlanMetrics Metrics(List<Draft> plan)
        {
            var m = new PlanMetrics { Boards = plan.Count };
            foreach (var b in plan)
            {
                m.Purchased += b.Item.Volume(b.Length);
                m.Cost += Price(b.Item, b.Length);
                double used = b.Pieces.Sum(p => p.Length) * b.Item.SectionArea / 1e9;
                m.Waste += b.Item.Volume(b.Length) - used;
            }
            foreach (var g in plan.SelectMany(b => b.Pieces.Select(p => (b, p))).Where(x => x.p.GrainGroup != null).GroupBy(x => x.p.GrainGroup))
                m.Grain += g.Select(x => x.b).Distinct().Count();
            // the reserve is part of what is really bought, so candidate plans are compared WITH it
            foreach (var rp in ComputeReserve(plan.Select(b => (b.Item, b.Length, b.Pieces))))
                foreach (var (item, length) in rp.Add) { m.Purchased += item.Volume(length); m.Cost += Price(item, length); m.Boards++; }
            return m;
        }

        private int SelectBest(List<PlanMetrics> ms)
        {
            const double eps = 1e-9;
            Func<PlanMetrics, PlanMetrics, int> cmp;
            switch (_settings.Strategy)
            {
                case OptimizationStrategy.MinWaste: cmp = (a, b) => Chain(Cmp(a.Waste, b.Waste, eps), Cmp(a.Purchased, b.Purchased, eps), Cmp(a.Cost, b.Cost, 1e-6)); break;
                case OptimizationStrategy.MinCost: cmp = (a, b) => Chain(Cmp(a.Cost, b.Cost, 1e-6), Cmp(a.Purchased, b.Purchased, eps), a.Boards.CompareTo(b.Boards)); break;
                case OptimizationStrategy.MinBoards: cmp = (a, b) => Chain(a.Boards.CompareTo(b.Boards), Cmp(a.Purchased, b.Purchased, eps), Cmp(a.Cost, b.Cost, 1e-6)); break;
                case OptimizationStrategy.GrainFirst: cmp = (a, b) => Chain(a.Grain.CompareTo(b.Grain), Cmp(a.Purchased, b.Purchased, eps), Cmp(a.Cost, b.Cost, 1e-6)); break;
                case OptimizationStrategy.Balanced:
                {
                    double minC = ms.Min(x => x.Cost) + 1e-9, minP = ms.Min(x => x.Purchased) + 1e-12, minW = ms.Min(x => x.Waste) + 1e-12, minB = ms.Min(x => x.Boards);
                    Func<PlanMetrics, double> score = x => 0.35 * x.Cost / minC + 0.25 * x.Purchased / minP + 0.25 * (x.Waste + 1e-12) / minW + 0.15 * x.Boards / minB;
                    cmp = (a, b) => score(a).CompareTo(score(b));
                    break;
                }
                default: cmp = (a, b) => Chain(Cmp(a.Purchased, b.Purchased, eps), Cmp(a.Cost, b.Cost, 1e-6), a.Boards.CompareTo(b.Boards)); break;
            }
            int best = 0;
            for (int i = 1; i < ms.Count; i++) if (cmp(ms[i], ms[best]) < 0) best = i;
            return best;
        }
        private static int Cmp(double a, double b, double eps) => Math.Abs(a - b) <= eps ? 0 : a.CompareTo(b);
        private static int Chain(params int[] cs) { foreach (var c in cs) if (c != 0) return c; return 0; }

        // ------------------------------------------------------------ finalisation: ids, layout, reserve, purchase list
        private void Finalize(OptimizationResult res, List<Draft> plan)
        {
            // stable ordering and ids
            var ordered = plan.OrderBy(b => b.Item.SpeciesId, StringComparer.Ordinal).ThenBy(b => b.Item.Id, StringComparer.Ordinal)
                .ThenByDescending(b => b.Length).ThenByDescending(b => b.Pieces.Count)
                .ThenBy(b => b.Pieces.Min(p => p.Id), StringComparer.Ordinal).ToList();
            int bi = 0, ci = 0;
            foreach (var d in ordered)
            {
                var board = new Board { Id = "B" + (++bi).ToString("000", CultureInfo.InvariantCulture), Item = d.Item, Length = d.Length, Price = Price(d.Item, d.Length) };
                double pos = R.EndTrim;
                int n = 0;
                foreach (var p in d.Pieces.OrderByDescending(x => x.Length).ThenBy(x => x.Id, StringComparer.Ordinal))
                {
                    board.Cuts.Add(new CutPlacement { CutId = "C" + (++ci).ToString("000", CultureInfo.InvariantCulture), Demand = p, Start = pos });
                    board.Pieces.Add(p);
                    pos += p.Length + R.SawKerf; n++;
                }
                double rawTail = d.Length - 2 * R.EndTrim - board.Pieces.Sum(p => p.Length) - R.SawKerf * n;
                if (rawTail >= -1e-9) { board.TailLength = Math.Max(0, rawTail); board.KerfLoss = R.SawKerf * n; }
                else { board.TailLength = 0; board.KerfLoss = R.SawKerf * (n - 1); }
                board.TrimLoss = 2 * R.EndTrim;
                board.TailClass = board.TailLength >= R.MinReusableRemnant ? RemnantClass.ProjectRemnant : RemnantClass.Scrap;
                res.Boards.Add(board);
            }
            res.PurchasedBeforeReserveM3 = res.Boards.Sum(b => b.VolumeM3);

            AccountWaste(res);
            ApplyReserve(res);
            BuildPurchase(res);
            Explain(res);
            res.PurchasedM3 = res.Boards.Sum(b => b.VolumeM3);
            res.TotalCost = res.Purchase.Sum(p => p.Total);
        }

        private void AccountWaste(OptimizationResult res)
        {
            double kerf = 0, trim = 0, excess = 0, scrap = 0, remn = 0, pieces = 0;
            foreach (var b in res.Boards.Where(x => !x.IsReserve))
            {
                double area = b.Item.SectionArea / 1e9;
                kerf += b.KerfLoss * area; trim += b.TrimLoss * area;
                foreach (var p in b.Pieces) { excess += (b.Item.SectionArea - p.SecA * p.SecB) * p.Length / 1e9; pieces += p.RoughVolumeM3; }
                if (b.TailClass == RemnantClass.ProjectRemnant) remn += b.TailLength * area; else scrap += b.TailLength * area;
                if (b.TailLength > 0)
                    res.Remnants.Add(new RemnantInfo { BoardId = b.Id, StockItemId = b.Item.Id, Length = b.TailLength, VolumeM3 = b.TailLength * area, Class = b.TailClass });
            }
            double machining = Math.Max(0, pieces - res.TheoreticalFinishedM3);
            res.KerfM3 = kerf;
            res.ScrapM3 = scrap;
            res.ReusableRemnantM3 = remn;
            res.ProcessWasteM3 = kerf + trim + excess + machining;
            res.OptimizedManufacturingM3 = pieces + kerf + trim;
        }

        private sealed class ReservePlan
        {
            public string SpeciesId; public double Percent, Target, Have;
            public List<(StockItem item, double length)> Add = new List<(StockItem, double)>();
        }

        /// <summary>
        /// Reserve is applied ONCE, after optimization: target = reserve% x rough volume of a species, first covered by the usable remnants
        /// of the plan; only a deficit adds boards, and then the CHEAPEST commercial option that covers it (not a whole board of the dominant profile).
        /// Used both when comparing candidate plans and when finalising, so a tighter plan is never penalised for needing less spare wood.
        /// </summary>
        private List<ReservePlan> ComputeReserve(IEnumerable<(StockItem item, double length, List<CutDemand> pieces)> boards)
        {
            var list = new List<ReservePlan>();
            foreach (var grp in boards.GroupBy(b => b.item.SpeciesId))
            {
                var sp = _lib.GetSpecies(grp.Key);
                var dominant = grp.GroupBy(b => b.item).OrderByDescending(g => g.Sum(b => b.item.Volume(b.length))).First().Key;
                var rp = new ReservePlan { SpeciesId = grp.Key, Percent = _settings.ReserveFor(sp, dominant) };
                rp.Target = rp.Percent / 100.0 * grp.Sum(b => b.pieces.Sum(p => p.RoughVolumeM3));
                foreach (var b in grp)
                {
                    double tail = b.length - 2 * R.EndTrim - b.pieces.Sum(p => p.Length) - R.SawKerf * b.pieces.Count;
                    if (tail >= R.MinReusableRemnant) rp.Have += tail * b.item.SectionArea / 1e9;
                }
                double deficit = rp.Target - rp.Have;
                var opts = _lib.StockFor(grp.Key).SelectMany(i => i.Lengths.Select(l => (item: i, length: l))).ToList();
                int guard = 0;
                while (deficit > 1e-9 && guard++ < 50 && opts.Count > 0)
                {
                    var covering = opts.Where(o => o.item.Volume(o.length) >= deficit - 1e-12).OrderBy(o => Price(o.item, o.length)).ThenBy(o => o.item.Volume(o.length)).ThenBy(o => o.item.Id, StringComparer.Ordinal).ToList();
                    var pick = covering.Count > 0 ? covering[0] : opts.OrderByDescending(o => o.item.Volume(o.length)).First();
                    rp.Add.Add((pick.item, pick.length));
                    deficit -= pick.item.Volume(pick.length);
                }
                list.Add(rp);
            }
            return list;
        }

        private void ApplyReserve(OptimizationResult res)
        {
            var real = res.Boards.Where(b => !b.IsReserve).ToList();
            foreach (var rp in ComputeReserve(real.Select(b => (b.Item, b.Length, b.Pieces))))
            {
                var sp = _lib.GetSpecies(rp.SpeciesId);
                res.ReservePercentBySpecies[sp.Id] = rp.Percent;
                foreach (var (item, length) in rp.Add)
                {
                    var rb = new Board { Id = "R" + (res.Boards.Count(b => b.IsReserve) + 1).ToString("00", CultureInfo.InvariantCulture), Item = item, Length = length, IsReserve = true, Price = Price(item, length), TailLength = length, TailClass = RemnantClass.ProjectRemnant };
                    res.Boards.Add(rb);
                    res.ReserveM3 += rb.VolumeM3;
                }
                res.Explanation.Add(string.Format(CultureInfo.InvariantCulture,
                    "Reserve {0:0.#}% of {1}: target {2:0.0000} m3, covered by {3:0.0000} m3 of reusable remnants{4}.",
                    rp.Percent, sp.Name, rp.Target, Math.Min(rp.Have, rp.Target),
                    rp.Add.Count > 0 ? "; " + rp.Add.Count + " extra reserve board(s) added (" + string.Join(", ", rp.Add.Select(a => a.item.Label + " x " + a.length.ToString("0", CultureInfo.InvariantCulture))) + ")" : " (no extra boards needed - reserve applied once, after optimization)"));
            }
        }

        private void BuildPurchase(OptimizationResult res)
        {
            int n = 0;
            foreach (var g in res.Boards.GroupBy(b => (b.Item.Id, b.Length)).OrderBy(g => g.First().Item.SpeciesId, StringComparer.Ordinal).ThenBy(g => g.Key.Id, StringComparer.Ordinal).ThenByDescending(g => g.Key.Length))
            {
                var first = g.First();
                res.Purchase.Add(new PurchaseLine
                {
                    Id = "PUR" + (++n).ToString("000", CultureInfo.InvariantCulture), Item = first.Item, SupplierId = first.Item.SupplierId ?? _lib.GetSpecies(first.Item.SpeciesId).SupplierId,
                    Length = first.Length, Quantity = g.Count(), ReserveQuantity = g.Count(b => b.IsReserve), UnitPrice = first.Price,
                    BoardIds = g.Select(b => b.Id).ToList(),
                    ExpectedUsableRemnantM3 = g.Where(b => b.TailClass == RemnantClass.ProjectRemnant).Sum(b => b.TailLength * b.Item.SectionArea / 1e9),
                    ExpectedWasteM3 = g.Where(b => !b.IsReserve).Sum(b => (b.KerfLoss + b.TrimLoss + (b.TailClass == RemnantClass.Scrap ? b.TailLength : 0)) * b.Item.SectionArea / 1e9)
                });
            }
        }

        private void Explain(OptimizationResult res)
        {
            var ex = res.Explanation;
            var ci = CultureInfo.InvariantCulture;
            var reserveLines = ex.ToList(); ex.Clear();
            ex.Add(string.Format(ci, "1. THEORETICAL requirement: {0:0.0000} m3 finished volume, {1:0.00} linear m of finished parts.", res.TheoreticalFinishedM3, res.TheoreticalLinearM));
            ex.Add(string.Format(ci, "2. After manufacturing allowances (planing/jointing/trim): {0:0.0000} m3 of rough pieces ({1} pieces).", res.RoughRequiredM3, res.Boards.Where(b => !b.IsReserve).Sum(b => b.Pieces.Count)));
            ex.Add(string.Format(ci, "3. OPTIMIZED manufacturing requirement (all parts packed globally, kerf {0} mm, strategy {1}): {2:0.0000} m3 on {3} board(s).",
                R.SawKerf, res.Strategy, res.OptimizedManufacturingM3, res.Boards.Count(b => !b.IsReserve)));
            int reuse = res.Boards.Where(b => !b.IsReserve).Sum(b => Math.Max(0, b.Pieces.Count - 1));
            ex.Add(string.Format(ci, "   {0} piece(s) are cut from material left over by an earlier cut on the same board (global remnant reuse).", reuse));
            var buy = string.Join(", ", res.Boards.GroupBy(b => (b.Item.Id, b.Length)).OrderBy(g => g.Key.Id, StringComparer.Ordinal).Select(g => g.Count() + " x " + g.First().Item.Label + " x " + g.Key.Length.ToString("0", ci)));
            ex.Add(string.Format(ci, "4. COMMERCIAL purchase (stock sizes, incl. reserve boards): {0} = {1:0.0000} m3. Utilisation of purchased wood by rough pieces: {2:0.0}%.", buy, res.PurchasedM3 > 0 ? res.PurchasedM3 : res.Boards.Sum(b => b.VolumeM3), 100 * res.RoughRequiredM3 / Math.Max(1e-9, res.Boards.Sum(b => b.VolumeM3))));
            ex.Add(string.Format(ci, "5. Usable remnants {0:0.0000} m3, scrap {1:0.0000} m3, process waste (kerf, trims, section excess, planing) {2:0.0000} m3.", res.ReusableRemnantM3, res.ScrapM3, res.ProcessWasteM3));
            ex.AddRange(reserveLines);
        }
    }
}
