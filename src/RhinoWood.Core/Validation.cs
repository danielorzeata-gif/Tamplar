using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RhinoWood.Core.Domain;
using RhinoWood.Core.Geometry;
using RhinoWood.Core.Libraries;
using RhinoWood.Core.Optimization;
using RhinoWood.Core.Rules;

namespace RhinoWood.Core.Validation
{
    public interface IValidationRule
    {
        string Code { get; }
        IEnumerable<Issue> Check(ValidationContext ctx);
    }

    public sealed class ValidationContext
    {
        public FurnitureModel Model { get; set; }
        public OptimizationResult Optimization { get; set; }
        public WoodLibrary Library { get; set; }
        public ProjectSettings Settings { get; set; }
    }

    public sealed class Validator
    {
        private readonly List<IValidationRule> _rules = new List<IValidationRule>();
        public void Register(IValidationRule r) => _rules.Add(r);
        public static Validator CreateDefault()
        {
            var v = new Validator();
            v.Register(new AllowanceRule()); v.Register(new CrossGrainRule()); v.Register(new MovementRule());
            v.Register(new WasteRule()); v.Register(new SlendernessRule()); v.Register(new StockRule());
            return v;
        }

        public List<Issue> Run(ValidationContext ctx)
        {
            var list = new List<Issue>();
            list.AddRange(ctx.Model.Issues);
            foreach (var r in _rules) list.AddRange(r.Check(ctx));
            return list.GroupBy(i => i.Code + "|" + i.SubjectId + "|" + i.Message).Select(g => g.First())
                .OrderByDescending(i => i.Severity).ThenBy(i => i.Code, StringComparer.Ordinal).ToList();
        }
    }

    internal sealed class AllowanceRule : IValidationRule
    {
        public string Code => "ALLOWANCE";
        public IEnumerable<Issue> Check(ValidationContext c)
        {
            foreach (var f in c.Model.Families)
                foreach (var rp in f.RoughPieces)
                {
                    double dl = rp.Rough.Length - f.Finished.Length;
                    double dt = rp.Rough.Thickness - f.Finished.Thickness;
                    bool strip = f.RoughPieces.Sum(x => x.CountPerPart) > 1;
                    double dw = rp.Rough.Width - (strip ? 0 : f.Finished.Width);
                    if (dl < 10 || dt < 3 || (!strip && dw < 3))
                        yield return new Issue { Severity = Severity.Warning, Code = "ALLOWANCE", SubjectId = f.Id, Message = "Insufficient machining allowance for " + f.Name + " (length +" + dl.ToString("0.#", CultureInfo.InvariantCulture) + ", thickness +" + dt.ToString("0.#", CultureInfo.InvariantCulture) + ")." };
                }
        }
    }

    internal sealed class CrossGrainRule : IValidationRule
    {
        public string Code => "CROSS_GRAIN";
        private static readonly HashSet<string> GlueOnly = new HashSet<string> { "dowel", "biscuit", "finger", "box-joint", "dado", "rabbet" };
        public IEnumerable<Issue> Check(ValidationContext c)
        {
            foreach (var j in c.Model.Joints)
            {
                if (!GlueOnly.Contains(j.JointTypeId)) continue;
                var fa = c.Model.FamilyOf(j.PartAId); var fb = c.Model.FamilyOf(j.PartBId);
                if (fa == null || fb == null || fa.GrainAxis == fb.GrainAxis) continue;
                var pa = c.Model.FindPart(j.PartAId);
                if (pa.Finished.Width > 60)
                    yield return new Issue { Severity = Severity.Warning, Code = "CROSS_GRAIN", SubjectId = j.Id, Message = "Cross-grain glued connection detected between " + j.PartAId + " and " + j.PartBId + " (" + j.JointTypeId + ", width " + pa.Finished.Width.ToString("0.#", CultureInfo.InvariantCulture) + " mm): seasonal movement may crack the joint." };
            }
        }
    }

    /// <summary>Seasonal movement of solid-wood panels vs. the travel allowed by their fasteners.</summary>
    internal sealed class MovementRule : IValidationRule
    {
        public string Code => "MOVEMENT";
        public IEnumerable<Issue> Check(ValidationContext c)
        {
            double dMc = c.Settings.Rules.SeasonalMoisturePercent;
            foreach (var fam in c.Model.Families.Where(f => f.VisualGrainRequired || f.Type == PartType.Top || f.Type == PartType.Panel))
            {
                var sp = c.Library.GetSpecies(fam.SpeciesId);
                foreach (var part in fam.Instances)
                {
                    // movement is across the grain, in the width direction when grain runs along the length
                    var axis = part.WidthAxis;
                    double width = part.Finished.Width;
                    double total = width * sp.TangentialMovementPerPercent * dMc;
                    yield return new Issue { Severity = Severity.Info, Code = "MOVEMENT_INFO", SubjectId = part.Id, Message = string.Format(CultureInfo.InvariantCulture, "{0}: expected seasonal movement across grain {1:0.0} mm total (±{2:0.0} mm) for a {3}% moisture swing - leave expansion gaps / use slotted fasteners.", part.Id, total, total / 2, dMc) };
                    double centre = part.Bounds.Center.Get(axis);
                    foreach (var grp in c.Model.HardwareInstalls.Where(x => x.MatePartId == part.Id).GroupBy(x => x.HardwareId + "|" + x.HostPartId))
                    {
                        var hw = c.Library.Hardware[grp.First().HardwareId];
                        double disp = grp.Max(h => Math.Abs(h.MatePoint.Get(axis) - centre)) * sp.TangentialMovementPerPercent * dMc;
                        if (disp > hw.TravelAllowance + 1e-9)
                            yield return new Issue { Severity = Severity.Warning, Code = "MOVEMENT_FASTENER", SubjectId = grp.First().Id, Message = string.Format(CultureInfo.InvariantCulture, "{0} on {1}: expected movement {2:0.0} mm exceeds fastener travel {3:0.0} mm; use slotted holes or more flexible fasteners.", hw.Model, grp.First().HostPartId, disp, hw.TravelAllowance) };
                    }
                    if (!c.Model.HardwareInstalls.Any(x => x.MatePartId == part.Id) && part.Finished.Width > 150)
                        yield return new Issue { Severity = Severity.Warning, Code = "MOVEMENT_UNFASTENED", SubjectId = part.Id, Message = part.Id + ": wide solid-wood panel has no movement-compatible fasteners defined." };
                }
            }
        }
    }

    internal sealed class WasteRule : IValidationRule
    {
        public string Code => "WASTE";
        public IEnumerable<Issue> Check(ValidationContext c)
        {
            var o = c.Optimization; if (o == null || o.PurchasedM3 <= 0) yield break;
            double lost = (o.PurchasedM3 - o.ReserveM3 - c.Model.AllParts.Sum(p => p.Finished.VolumeM3 * 0 ) - o.RoughRequiredM3) / Math.Max(1e-9, o.PurchasedM3 - o.ReserveM3);
            double wasteShare = (o.ProcessWasteM3 + o.ScrapM3) / Math.Max(1e-9, o.PurchasedM3 - o.ReserveM3);
            if (wasteShare > 0.45)
                yield return new Issue { Severity = Severity.Warning, Code = "WASTE", Message = string.Format(CultureInfo.InvariantCulture, "Material optimization contains excessive waste ({0:0}% of purchased wood is process waste or scrap). Consider other stock profiles or a different strategy.", wasteShare * 100) };
        }
    }

    internal sealed class SlendernessRule : IValidationRule
    {
        public string Code => "SLENDER";
        public IEnumerable<Issue> Check(ValidationContext c)
        {
            foreach (var f in c.Model.Families.Where(f => f.Type == PartType.Leg))
            {
                double ratio = f.Finished.Length / Math.Min(f.Finished.Width, f.Finished.Thickness);
                if (ratio > 14)
                    yield return new Issue { Severity = Severity.Warning, Code = "SLENDER", SubjectId = f.Id, Message = "Leg is slender (length/section = " + ratio.ToString("0.#", CultureInfo.InvariantCulture) + "); consider a thicker section or stretchers." };
            }
        }
    }

    internal sealed class StockRule : IValidationRule
    {
        public string Code => "STOCK";
        public IEnumerable<Issue> Check(ValidationContext c)
        {
            if (c.Optimization == null) yield break;
            foreach (var i in c.Optimization.Issues) yield return i;
            foreach (var pl in c.Optimization.Purchase)
                if (c.Library.StockFor(pl.Item.SpeciesId).All(s => s.Id != pl.Item.Id))
                    yield return new Issue { Severity = Severity.Error, Code = "STOCK_MISSING", Message = "Stock item " + pl.Item.Id + " is not in the library." };
        }
    }
}
