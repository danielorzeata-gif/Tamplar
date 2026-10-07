using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RhinoWood.Core.Domain;
using RhinoWood.Core.Libraries;
using RhinoWood.Core.Manufacturing;
using RhinoWood.Core.Optimization;
using RhinoWood.Core.Rules;

namespace RhinoWood.Core.Costing
{
    public sealed class BomLine
    {
        public string Category { get; set; }   // Part, Material, Hardware, Fastener, Adhesive, Finish
        public string Id { get; set; }
        public string Description { get; set; }
        public double Quantity { get; set; }
        public string Unit { get; set; }
        public double UnitCost { get; set; }
        public double Total => Math.Round(Quantity * UnitCost, 2);
        public string Notes { get; set; }
    }

    public sealed class CostSummary
    {
        public double RawMaterial { get; set; }
        public double Hardware { get; set; }
        public double Consumables { get; set; }
        public double Labor { get; set; }
        public double EstimatedWasteCost { get; set; }   // informational: portion of raw material that is not in parts
        public double Total => Math.Round(RawMaterial + Hardware + Consumables + Labor, 2);
        public string Currency { get; set; }
    }

    public sealed class Bom
    {
        public List<BomLine> Lines { get; set; } = new List<BomLine>();
    }

    public sealed class CostEngine
    {
        private readonly WoodLibrary _lib; private readonly ProjectSettings _settings;
        public CostEngine(WoodLibrary lib, ProjectSettings settings) { _lib = lib; _settings = settings; }

        public (Bom bom, CostSummary cost) Calculate(FurnitureModel model, OptimizationResult opt, ManufacturingPlan mfg)
        {
            var bom = new Bom(); var ci = CultureInfo.InvariantCulture;
            var cost = new CostSummary { Currency = _settings.Currency };

            // parts: cost allocated from the REAL optimized purchase by rough volume share
            double totalRough = Math.Max(1e-12, opt.Boards.Where(b => !b.IsReserve).Sum(b => b.Pieces.Sum(p => p.RoughVolumeM3)));
            double purchaseCost = opt.Purchase.Sum(p => p.Total);
            foreach (var fam in model.Families)
            {
                double famRough = opt.Boards.SelectMany(b => b.Pieces).Where(p => p.FamilyId == fam.Id).Sum(p => p.RoughVolumeM3);
                fam.UnitCost = fam.Quantity == 0 ? 0 : Math.Round(purchaseCost * famRough / totalRough / fam.Quantity, 2);
                bom.Lines.Add(new BomLine { Category = "Part", Id = fam.Id, Description = fam.Name + " (" + fam.SpeciesId + ") " + fam.Finished.ToString(), Quantity = fam.Quantity, Unit = "pcs", UnitCost = fam.UnitCost, Notes = "Cost = share of optimized purchase" });
            }
            foreach (var p in opt.Purchase)
                bom.Lines.Add(new BomLine { Category = "Material", Id = p.Id, Description = p.Item.Label + " x " + p.Length.ToString("0", ci) + " mm" + (p.ReserveQuantity > 0 ? " (incl. " + p.ReserveQuantity + " reserve)" : ""), Quantity = p.Quantity, Unit = "pcs", UnitCost = p.UnitPrice });
            cost.RawMaterial = opt.Purchase.Sum(p => p.Total);

            // hardware and fasteners
            foreach (var g in model.HardwareInstalls.GroupBy(h => h.HardwareId))
            {
                var hw = _lib.Hardware[g.Key]; int qty = g.Sum(x => x.Quantity);
                bom.Lines.Add(new BomLine { Category = "Hardware", Id = hw.Id, Description = hw.Category + ": " + hw.Model, Quantity = qty, Unit = "pcs", UnitCost = hw.UnitPrice });
                foreach (var fs in hw.FastenersPerUnit)
                {
                    var f = _lib.Hardware[fs.Key];
                    bom.Lines.Add(new BomLine { Category = "Fastener", Id = f.Id, Description = f.Model, Quantity = qty * fs.Value, Unit = "pcs", UnitCost = f.UnitPrice });
                }
            }
            cost.Hardware = bom.Lines.Where(l => l.Category == "Hardware" || l.Category == "Fastener").Sum(l => l.Total);

            // adhesives and finish
            double glueArea = GlueAreaM2(model);
            double finishArea = model.AllParts.Sum(p => { var d = p.Finished; return 2 * (d.Length * d.Width + d.Length * d.Thickness + d.Width * d.Thickness) / 1e6; });
            bom.Lines.Add(new BomLine { Category = "Adhesive", Id = "GLUE-PVAC", Description = "PVA D3 wood glue (glue line area)", Quantity = Math.Round(glueArea, 3), Unit = "m2", UnitCost = _settings.Rules.GluePricePerM2 });
            bom.Lines.Add(new BomLine { Category = "Finish", Id = "FINISH-OIL", Description = "Hardwax oil (2 coats, surface area)", Quantity = Math.Round(finishArea, 3), Unit = "m2", UnitCost = _settings.Rules.FinishPricePerM2 * 2 });
            cost.Consumables = bom.Lines.Where(l => l.Category == "Adhesive" || l.Category == "Finish").Sum(l => l.Total);

            cost.Labor = Math.Round(mfg.TotalMinutes / 60.0 * _settings.Rules.LaborRatePerHour, 2);
            double wasteShare = opt.PurchasedM3 <= 0 ? 0 : 1 - opt.Boards.SelectMany(b => b.Pieces).Sum(p => p.FinishedVolumeM3) / opt.PurchasedM3;
            cost.EstimatedWasteCost = Math.Round(cost.RawMaterial * wasteShare, 2);
            return (bom, cost);
        }

        public static double GlueAreaM2(FurnitureModel model)
        {
            double mm2 = 0;
            foreach (var p in model.AllParts)
                foreach (var f in p.Features)
                {
                    if (f.Kind == FeatureKind.Tenon && f.HasBox) { var s = f.Box.Size; mm2 += 2 * s.X * s.Y + 2 * s.X * s.Z + s.Y * s.Z; }
                    else if (f.Kind == FeatureKind.DowelHole) mm2 += Math.PI * f.Diameter * f.Depth;
                    else if (f.HasBox && (f.Kind == FeatureKind.Dado || f.Kind == FeatureKind.Rabbet || f.Kind == FeatureKind.Lap)) { var s = f.Box.Size; mm2 += s.X * s.Y; }
                }
            foreach (var fam in model.Families)
            {
                int pieces = fam.RoughPieces.Sum(r => r.CountPerPart);
                if (pieces > 1) mm2 += (pieces - 1) * fam.Finished.Length * fam.Finished.Thickness * fam.Quantity;
            }
            return mm2 / 1e6;
        }
    }
}
