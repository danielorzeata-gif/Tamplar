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
        /// <summary>Workshop labour, lei/hour. Romanian joiners ask ~75-100 lei/h (100-120 in big cities, 150+ for complex work) [REF brig.ro]; default 90.</summary>
        public double LaborRatePerHour { get; set; } = 90;
        public double GluePricePerM2 { get; set; } = 1.2;
        public double BiscuitPrice { get; set; } = 0.25;
        public double HdfPricePerM2 { get; set; } = 12;
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
        /// <summary>BASE margin ON THE PRICE (not a markup on cost) for the simplest work; custom high-end woodwork is 25-35 %, production 15-20 % [REF].</summary>
        public double SalesMarginPercent { get; set; } = 25;
        /// <summary>Extra margin points reached at full complexity (many joinery operations per part): 25 % + 10 = 35 %.</summary>
        public double ComplexityMarginPercent { get; set; } = 10;
        /// <summary>Workshop overhead (rent, power, tools, consumables) as a percentage of the direct cost; the bedroom sheet uses 10 %.</summary>
        public double OverheadPercent { get; set; } = 10;
        /// <summary>VAT (Romania: standard rate 21 % from 1 Aug 2025).</summary>
        public double VatPercent { get; set; } = 21;
        /// <summary>The price without VAT is rounded UP to this step (0 = no rounding).</summary>
        public double PriceRounding { get; set; } = 10;
        /// <summary>Quick order estimate = finished volume x yield factor. Edged (tivit) class B 1.7; unedged (netivit) 1.8-2.2 (2.0); blanks (semifabricate) 1.2.</summary>
        public string LumberForm { get; set; } = "Edged";
        public double YieldEdged { get; set; } = 1.7;
        public double YieldUnedged { get; set; } = 2.0;
        public double YieldBlanks { get; set; } = 1.2;
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
