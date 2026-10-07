using System;
using System.Linq;
using RhinoWood.Core.Domain;
using RhinoWood.Core.Optimization;
using RhinoWood.Core.Reports;
using RhinoWood.Core.Rules;

namespace RhinoWood.Core.Projects
{
    /// <summary>The sale price of a piece, built the way a workshop quotes: direct cost + overhead, then a margin ON THE PRICE that grows with the number of operations.</summary>
    public sealed class PriceBreakdown
    {
        public double Material { get; set; }
        public double Hardware { get; set; }
        public double Consumables { get; set; }
        public double Labor { get; set; }
        public double LaborHours { get; set; }
        public double DirectCost => Material + Hardware + Consumables + Labor;
        public double Overhead { get; set; }
        public double ProductionCost => DirectCost + Overhead;
        public int Parts { get; set; }
        /// <summary>Joinery / machining operations (one per hole, mortise, tenon, slot, rout).</summary>
        public int Operations { get; set; }
        /// <summary>Minutes of those operations (a mortise costs far more than a dowel hole).</summary>
        public double OperationMinutes { get; set; }
        public double MinutesPerPart => Parts == 0 ? 0 : OperationMinutes / Parts;
        public double OperationsPerPart => Parts == 0 ? 0 : (double)Operations / Parts;
        /// <summary>0..1: joinery/machining minutes per part / 12 (one mortise-and-tenon pair per part is about 12 min).</summary>
        public double Complexity { get; set; }
        public double BaseMarginPercent { get; set; }
        public double ComplexityMarginPercent { get; set; }
        public double MarginPercent => BaseMarginPercent + ComplexityMarginPercent;
        public double Margin => PriceExVat - ProductionCost;
        public double PriceExVat { get; set; }
        public double VatPercent { get; set; }
        public double Vat => PriceExVat * VatPercent / 100.0;
        public double PriceIncVat => PriceExVat + Vat;
        public string Currency { get; set; }
        /// <summary>Species whose price is an estimate / placeholder: confirm with a supplier before quoting.</summary>
        public System.Collections.Generic.List<string> UnverifiedPrices { get; set; } = new System.Collections.Generic.List<string>();
    }

    /// <summary>Quick order estimate: finished volume x yield factor (depends on the lumber form), per species.</summary>
    public sealed class OrderEstimate
    {
        public sealed class Line { public string SpeciesId; public double FinishedM3; public double Factor; public double OrderM3; public double PricePerM3; public double Cost; }
        public System.Collections.Generic.List<Line> Lines { get; } = new System.Collections.Generic.List<Line>();
        public double OrderM3 => Lines.Sum(l => l.OrderM3);
        public double Cost => Lines.Sum(l => l.Cost);
        public double Factor { get; set; }
        public string Form { get; set; }

        public static double FactorOf(ProjectSettings s) => s.LumberForm == "Blanks" ? s.YieldBlanks : s.LumberForm == "Unedged" ? s.YieldUnedged : s.YieldEdged;

        public static OrderEstimate Compute(WoodProject p, ProjectResult r)
        {
            var e = new OrderEstimate { Factor = FactorOf(p.Settings), Form = p.Settings.LumberForm };
            foreach (var g in r.Model.Families.GroupBy(f => f.SpeciesId))
            {
                double fin = g.Sum(f => f.Quantity * f.Finished.VolumeM3);
                double price = p.Library.Species.TryGetValue(g.Key, out var sp) ? sp.PricePerM3 : 0;
                e.Lines.Add(new Line { SpeciesId = g.Key, FinishedM3 = fin, Factor = e.Factor, OrderM3 = fin * e.Factor, PricePerM3 = price, Cost = fin * e.Factor * price });
            }
            return e;
        }
    }

    public static class PanelPrice
    {
        /// <summary>Price of one m² of solid-wood panel of the given thickness: lei/m³ x thickness (e.g. oak 2800 x 0.029 = 81 lei/m²).</summary>
        public static double PerM2(double pricePerM3, double thicknessMm) => pricePerM3 * thicknessMm / 1000.0;
    }

    public static class Pricing
    {
        public static PriceBreakdown Compute(WoodProject p, ProjectResult r) => Compute(p.Settings, r, p);

        public static PriceBreakdown Compute(ProjectSettings s, ProjectResult r, WoodProject p0 = null)
        {
            var c = r.Cost;
            var b = new PriceBreakdown
            {
                Material = c.RawMaterial, Hardware = c.Hardware, Consumables = c.Consumables, Labor = c.Labor, Currency = c.Currency,
                LaborHours = s.Rules.LaborRatePerHour > 0 ? c.Labor / s.Rules.LaborRatePerHour : 0, Parts = r.Model.AllParts.Count(), VatPercent = s.VatPercent
            };
            foreach (var sid in r.Model.Families.Select(f => f.SpeciesId).Distinct())
                if (p0 != null && p0.Library.Species.TryGetValue(sid, out var spp) && spp.PriceLabel != "[REF]") b.UnverifiedPrices.Add(Ro.SpeciesName(sid) + " " + spp.PriceLabel);
            b.Overhead = Math.Round((b.Material + b.Hardware + b.Consumables + b.Labor) * s.OverheadPercent / 100.0, 2);
            b.Operations = r.Manufacturing == null ? 0 : r.Manufacturing.Operations.Count(o => o.FeatureId != null && (o.Type == OperationType.Mortise || o.Type == OperationType.Tenon || o.Type == OperationType.Slot || o.Type == OperationType.Rout || o.Type == OperationType.Drill));
            b.OperationMinutes = r.Manufacturing == null ? 0 : r.Manufacturing.Operations.Where(o => o.FeatureId != null).Sum(o => o.Minutes);
            b.Complexity = Math.Max(0, Math.Min(1, b.MinutesPerPart / 12.0));
            b.BaseMarginPercent = s.SalesMarginPercent;
            b.ComplexityMarginPercent = Math.Round(s.ComplexityMarginPercent * b.Complexity, 1);
            double m = Math.Min(60, b.MarginPercent) / 100.0;
            double price = b.ProductionCost / (1 - m);
            b.PriceExVat = s.PriceRounding > 0 ? Math.Ceiling(price / s.PriceRounding) * s.PriceRounding : Math.Round(price, 2);
            return b;
        }
    }
}
