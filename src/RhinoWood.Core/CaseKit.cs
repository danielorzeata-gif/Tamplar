using System;
using System.Collections.Generic;
using System.Linq;
using RhinoWood.Core.Domain;
using RhinoWood.Core.Geometry;
using RhinoWood.Core.HardwareSystem;
using RhinoWood.Core.Joinery;
using RhinoWood.Core.Libraries;
using RhinoWood.Core.Rules;

namespace RhinoWood.Core.Furniture
{
    /// <summary>Shared building blocks of the casework / bed definitions: part families (single board or edge-glued strips), front styles, anti-tip kit.</summary>
    public static class CaseKit
    {
        public static readonly string[] FrontStyles = { "scoop", "handle", "push", "jrabbet" };

        public static Box3 B(double x0, double y0, double z0, double x1, double y1, double z1) => new Box3(new Vec3(x0, y0, z0), new Vec3(x1, y1, z1));

        public static void Add(PartFamily fam, string id, Box3 b, Axis len, Axis wid, Axis thk) =>
            fam.Instances.Add(new PartInstance { Id = id, FamilyId = fam.Id, Index = fam.Instances.Count, Bounds = b, LengthAxis = len, WidthAxis = wid, ThicknessAxis = thk });

        /// <summary>A family of identical parts. A part wider than any commercial board is edge-glued from equal strips (joined with biscuits), otherwise it is cut from one board.</summary>
        public static PartFamily Family(ProjectContext ctx, string id, string name, PartType type, string species, Dims fin, Axis grain, char cls, bool glued, string assembly = "Body")
        {
            var rules = ctx.Rules;
            var roughFull = rules.Rough(fin);
            var fam = new PartFamily
            {
                Id = id, Name = name, Type = type, Assembly = assembly, SpeciesId = species, Finished = fin, GrainAxis = grain, VisClass = cls,
                RequiresGrainContinuity = !glued, VisualGrainRequired = cls == 'A', GrainGroup = cls == 'A' ? "CASE-A" : null,
                RoughPieces = { new RoughPieceSpec { CountPerPart = 1, Rough = roughFull, Role = name + " blank" } }
            };
            if (glued && fin.Width > 100)
            {
                var stocks = ctx.Library.StockFor(species).ToList();
                bool single = stocks.Any(s => Math.Min(s.Width, s.Thickness) >= roughFull.Thickness - 1e-6 && Math.Max(s.Width, s.Thickness) >= roughFull.Width - 1e-6);
                if (!single)
                {
                    var stock = ctx.Library.PanelStripStock(species, roughFull.Thickness);
                    double stockW = stock == null ? 140 : Math.Max(stock.Width, stock.Thickness);
                    double maxStrip = stockW - rules.PanelStripWidthAllowance;
                    int n = Math.Max(2, (int)Math.Ceiling((fin.Width + rules.GluedPanelTrim) / maxStrip));
                    double stripFinished = (fin.Width + rules.GluedPanelTrim) / n;
                    fam.EdgeGlued = true; fam.RequiresGrainContinuity = false;
                    fam.Notes = "Edge-glued from " + n + " strips with biscuits; alternate growth-ring orientation.";
                    fam.RoughPieces[0] = new RoughPieceSpec { CountPerPart = n, Rough = new Dims(roughFull.Length, stripFinished + rules.PanelStripWidthAllowance, roughFull.Thickness), Role = name + " strip" };
                }
            }
            return fam;
        }

        // ------------------------------------------------------------------------------------------- front styles
        /// <summary>Hardware for the style: handle (2 through holes, 128 mm) or push-to-open latch. Scoop / J-rabbet are routed (see AddFrontFeatures).</summary>
        public static void FrontHardware(List<HardwareInstall> installs, string style, string frontId, Box3 front)
        {
            if (style != "handle" && style != "push") return;
            var c = front.Center;
            installs.Add(new HardwareInstall
            {
                HardwareId = style == "handle" ? "HANDLE-128" : "PUSH-OPEN", HostPartId = frontId, MatePartId = "",
                Point = new Vec3(c.X, front.Min.Y, c.Z), Normal = new Vec3(0, 1, 0), AxisU = new Vec3(1, 0, 0), AxisV = new Vec3(0, 0, 1),
                MatePoint = c, MateNormal = new Vec3(0, -1, 0), MateAxisV = new Vec3(0, 0, 1)
            });
        }

        /// <summary>Routed finger pulls on the rear of the front (visible from above when the drawer is closed? no: reached from above/below): scoop 120 mm or full-length J-rabbet.</summary>
        public static void AddFrontFeatures(FurnitureModel model, string style, string prefix = "FRONT")
        {
            if (style != "scoop" && style != "jrabbet") return;
            int n = 0;
            foreach (var part in model.AllParts.Where(p => p.Id.StartsWith(prefix, StringComparison.Ordinal)))
            {
                var f = part.Finished; double t = f.Thickness;      // local x = along the front, y = height, z = thickness (0 = exterior face)
                Box3 box = style == "scoop"
                    ? new Box3(new Vec3(f.Length / 2 - 60, f.Width - 30, t - 10), new Vec3(f.Length / 2 + 60, f.Width, t))
                    : new Box3(new Vec3(0, f.Width - 22, t - 10), new Vec3(f.Length, f.Width, t));
                part.Features.Add(JointGeometry.Rect(part, box, style == "scoop" ? FeatureKind.RoutPocket : FeatureKind.Rabbet, 10,
                    style == "scoop" ? "Finger scoop" : "J finger rabbet", "ROUT-8", "STY", "STY" + (++n).ToString("000")));
            }
        }

        /// <summary>Anti-tip kit when EN 14749 thresholds are exceeded (R10): height &gt; 900 &amp; mass ≥ 10 kg, or height &gt; 350 &amp; mass ≥ 35 kg.</summary>
        public static void AntiTip(List<HardwareInstall> installs, FurnitureModel model, WoodLibrary lib, string hostPartId, Vec3 point)
        {
            double mass = model.Families.Sum(f => f.Instances.Count * f.Finished.VolumeM3 * (lib.Species.TryGetValue(f.SpeciesId, out var sp) && sp.DensityKgM3 > 0 ? sp.DensityKgM3 : 600))
                + model.SheetParts.Sum(s => s.AreaM2 * s.Thickness / 1000 * 850);
            if (!SafetyRules.RequiresStabilityCheck(model.Bounds.Size.Z, mass)) return;
            installs.Add(new HardwareInstall { HardwareId = "ANTITIP-KIT", HostPartId = hostPartId, MatePartId = "", Point = point, Normal = new Vec3(0, 1, 0), AxisU = new Vec3(1, 0, 0), AxisV = new Vec3(0, 0, 1), MatePoint = point, MateNormal = new Vec3(0, -1, 0), MateAxisV = new Vec3(0, 0, 1) });
        }

        public static string Fingerprint(IEnumerable<PartFamily> fams, IEnumerable<JointEngine.Request> reqs, IEnumerable<SheetPart> sheets) =>
            string.Join("|", fams.Select(f => f.Id + ":" + f.SpeciesId + ":" + f.Finished + ":" + string.Join(";", f.RoughPieces.Select(x => x.CountPerPart + "x" + x.Rough)) + ":" + string.Join(";", f.Instances.Select(i => i.Id + "@" + i.Bounds))))
            + "|" + string.Join(";", reqs.Select(q => q.JointTypeId + q.PartAId + q.PartBId + q.AAtStart)) + "|" + string.Join(";", sheets.Select(s => s.Id + s.Bounds));
    }
}
