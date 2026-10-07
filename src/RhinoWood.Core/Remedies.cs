using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RhinoWood.Core.Domain;
using RhinoWood.Core.Joinery;
using RhinoWood.Core.Reports;

namespace RhinoWood.Core.Projects
{
    /// <summary>A concrete, applicable solution to a validation warning (e.g. change one joint type), ranked by the project's optimization strategy.</summary>
    public sealed class Remedy
    {
        public string IssueCode { get; set; }
        public string ChoiceKey { get; set; }
        public string FromOption { get; set; }
        public string ToOption { get; set; }
        public string Title { get; set; }
        public string Detail { get; set; }
        public double CostDelta { get; set; }
        public double PurchaseDeltaM3 { get; set; }
        public double Score { get; set; }
        public int Strength { get; set; }
        public bool Recommended { get; set; }
    }

    /// <summary>
    /// Turns warnings into solutions. Each candidate is tried on the real project (set choice → recalculate → check that the warning is gone and
    /// no new error appears → revert), then ranked by the active optimization strategy; the first one is the recommended solution.
    /// </summary>
    public static class RemedyEngine
    {
        public const string ThroughConflict = "THROUGH_CONFLICT";

        public static double Score(OptimizationStrategy s, ProjectResult r)
        {
            var o = r.Optimization;
            switch (s)
            {
                case OptimizationStrategy.MinPurchase: return o.PurchasedM3;
                case OptimizationStrategy.MinWaste: return o.PurchasedM3 - o.TheoreticalFinishedM3;
                case OptimizationStrategy.MinBoards: return o.Purchase.Sum(p => p.Quantity + p.ReserveQuantity);
                default: return r.Cost.Total;      // MinCost, GrainFirst, Balanced
            }
        }

        public static List<Remedy> Suggest(WoodProject p, ProjectResult current = null)
        {
            var res = new List<Remedy>();
            var r0 = current ?? p.Recalculate();
            if (!r0.Issues.Any(i => i.Code == ThroughConflict)) return res;

            var strategy = p.Settings.Strategy;
            double baseScore = Score(strategy, r0);
            int errors0 = r0.Issues.Count(i => i.Severity == Severity.Error);
            var saved = new Dictionary<string, string>(p.Choices);
            try
            {
                foreach (var ch in p.Furniture.Choices.Where(c => c.Kind == "joint"))
                {
                    var cur = p.Choices[ch.Key];
                    if (!(p.Joints.Get(cur) is MortiseTenonJoint mt && mt.IsThroughJoint)) continue;   // only the through joint is the cause
                    foreach (var opt in ch.Options.Where(o => o != cur))
                    {
                        var def = p.Joints.Get(opt);
                        if (def is MortiseTenonJoint m2 && m2.IsThroughJoint) continue;
                        if (def.Info.Strength < 3) continue;                                              // a fix must not weaken the joint to a poor one
                        p.SetChoice(ch.Key, opt);
                        var r = p.Recalculate();
                        bool ok = !r.Issues.Any(i => i.Code == ThroughConflict) && r.Issues.Count(i => i.Severity == Severity.Error) <= errors0;
                        if (ok)
                            res.Add(new Remedy
                            {
                                IssueCode = ThroughConflict, ChoiceKey = ch.Key, FromOption = cur, ToOption = opt, Strength = def.Info.Strength,
                                CostDelta = r.Cost.Total - r0.Cost.Total, PurchaseDeltaM3 = r.Optimization.PurchasedM3 - r0.Optimization.PurchasedM3,
                                Score = Score(strategy, r) - baseScore,
                                Title = Ro.Choice(ch.Key, ch.Label) + ": " + Ro.Joint(cur, cur) + " → " + Ro.Joint(opt, opt)
                            });
                        p.SetChoice(ch.Key, cur);
                    }
                }
            }
            finally
            {
                foreach (var kv in saved) if (p.Choices[kv.Key] != kv.Value) p.SetChoice(kv.Key, kv.Value);
                p.Recalculate();
            }
            res = res.OrderBy(x => Math.Round(x.Score, 6)).ThenByDescending(x => x.Strength).ThenBy(x => x.ToOption, StringComparer.Ordinal).ToList();
            foreach (var x in res)
                x.Detail = string.Format(CultureInfo.InvariantCulture, "cost {0:+0.00;-0.00;±0.00} {1} · material cumpărat {2:+0.000;-0.000;±0.000} m³ · rezistență {3}/5",
                    x.CostDelta, p.Settings.Currency, x.PurchaseDeltaM3, x.Strength);
            if (res.Count > 0) res[0].Recommended = true;
            return res;
        }

        public static void Apply(WoodProject p, Remedy r) => p.SetChoice(r.ChoiceKey, r.ToOption);
    }
}
