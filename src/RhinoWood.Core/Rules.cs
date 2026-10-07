using System;
using System.Collections.Generic;
using System.Linq;
using RhinoWood.Core.Domain;

namespace RhinoWood.Core.Rules
{
    public sealed class AllowanceBand { public double MaxDim { get; set; } public double Allowance { get; set; } }

    /// <summary>Rule-driven manufacturing allowances and shop constants.</summary>
    public sealed class ManufacturingRules
    {
        /// <summary>(max finished dimension, allowance added to that dimension). Last band catches the rest.</summary>
        public List<AllowanceBand> SectionBands { get; set; } = new List<AllowanceBand>
        {
            new AllowanceBand { MaxDim = 50, Allowance = 5 }, new AllowanceBand { MaxDim = 100, Allowance = 10 }, new AllowanceBand { MaxDim = 1e9, Allowance = 15 }
        };
        public double LengthAllowance { get; set; } = 30;
        public double SawKerf { get; set; } = 3;
        public double EndTrim { get; set; } = 0;
        public double MinReusableRemnant { get; set; } = 300;
        public double GluedPanelTrim { get; set; } = 10;
        public double PanelStripWidthAllowance { get; set; } = 10;
        /// <summary>Moisture swing for movement checks; 5 points = indoor heated apartment (RULES R9, [ESTIMARE]).</summary>
        public double SeasonalMoisturePercent { get; set; } = 5;

        public double SectionAllowance(double finishedDim)
        {
            foreach (var b in SectionBands.OrderBy(x => x.MaxDim)) if (finishedDim <= b.MaxDim) return b.Allowance;
            return SectionBands.Last().Allowance;
        }

        public Dims Rough(Dims finished) => new Dims(
            finished.Length + LengthAllowance,
            finished.Width + SectionAllowance(finished.Width),
            finished.Thickness + SectionAllowance(finished.Thickness));

        // shop economics
        public double LaborRatePerHour { get; set; } = 25;
        public double GluePricePerM2 { get; set; } = 1.2;
        public double BiscuitPrice { get; set; } = 0.25;
        public double FinishPricePerM2 { get; set; } = 4.5;
        public double GlueLinesPerJointM2 { get; set; } = 1.0;
    }

    public sealed class ProjectSettings
    {
        public ManufacturingRules Rules { get; set; } = new ManufacturingRules();
        public OptimizationStrategy Strategy { get; set; } = OptimizationStrategy.MinPurchase;
        public double GlobalReservePercent { get; set; } = 10;
        /// <summary>Project-specific overrides, species id -> percent.</summary>
        public Dictionary<string, double> ReserveOverrides { get; set; } = new Dictionary<string, double>();
        public string Currency { get; set; } = "lei";
        /// <summary>Sale price = production cost x (1 + margin). Only used in SALE (VANZARE) documents, never shown with internal costs.</summary>
        public double SalesMarginPercent { get; set; } = 35;
        public DisplayMode Display { get; set; } = DisplayMode.Normal;
        public bool ShowGrain { get; set; }

        public double ReserveFor(WoodSpecies sp, StockItem stock)
        {
            if (sp != null && ReserveOverrides.TryGetValue(sp.Id, out var o)) return o;
            if (stock?.ReservePercent != null) return stock.ReservePercent.Value;
            if (sp?.ReservePercent != null) return sp.ReservePercent.Value;
            return GlobalReservePercent;
        }
    }
}
