using System;
using System.Collections.Generic;
using System.Linq;
using RhinoWood.Core.Domain;
using RhinoWood.Core.Geometry;

namespace RhinoWood.Core.Display
{
    /// <summary>Renderer-independent description of the material removed from a part, used for boolean-cut solids and for the cutting-piece export.</summary>
    public static class PartSolids
    {
        private const double Ext = 1.0;     // cutters are extended past faces they touch so booleans never meet coplanar faces

        /// <summary>All volumes removed from the finished-size block of the part (mortises, slots, holes, tenon cheeks and shoulders), world coordinates.</summary>
        public static List<CutVolume> CutsFor(PartInstance part)
        {
            var list = new List<CutVolume>();
            var b = part.Bounds;
            foreach (var f in part.Features)
            {
                if (f.HasBox)
                {
                    if (f.Kind == FeatureKind.Tenon)
                    {
                        foreach (var lb in TenonWaste(part, f.Box)) list.Add(BoxCut(part, lb, "Cep: " + f.Id));
                    }
                    else list.Add(BoxCut(part, f.Box, f.Describe()));
                }
                else
                {
                    var a = part.LocalToWorld(f.Position);
                    var e = part.LocalToWorld(f.Position + f.Direction * f.Depth);
                    var d = e - a; double len = Math.Sqrt(d.X * d.X + d.Y * d.Y + d.Z * d.Z);
                    if (len < 1e-9 || f.Diameter <= 0) continue;
                    var u = new Vec3(d.X / len, d.Y / len, d.Z / len);
                    var a2 = a - u * Ext;
                    bool through = IsOnOrPast(e, b);
                    var e2 = through ? e + u * Ext : e;
                    list.Add(new CutVolume { Kind = PrimKind.Cylinder, P0 = a2, P1 = e2, Radius = f.Diameter / 2, Label = f.Describe() });
                }
            }
            return list;
        }

        private static bool IsOnOrPast(Vec3 p, Box3 b) =>
            p.X <= b.Min.X + 1e-6 || p.X >= b.Max.X - 1e-6 || p.Y <= b.Min.Y + 1e-6 || p.Y >= b.Max.Y - 1e-6 || p.Z <= b.Min.Z + 1e-6 || p.Z >= b.Max.Z - 1e-6;

        private static CutVolume BoxCut(PartInstance part, Box3 localBox, string label)
        {
            var w = part.LocalBoxToWorld(localBox); var b = part.Bounds;
            Vec3 lo = w.Min, hi = w.Max;
            foreach (var ax in new[] { Axis.X, Axis.Y, Axis.Z })
            {
                if (lo.Get(ax) <= b.Min.Get(ax) + 1e-6) lo = lo.With(ax, b.Min.Get(ax) - Ext);
                if (hi.Get(ax) >= b.Max.Get(ax) - 1e-6) hi = hi.With(ax, b.Max.Get(ax) + Ext);
            }
            return new CutVolume { Kind = PrimKind.Box, Box = new Box3(lo, hi), Label = label };
        }

        /// <summary>The tenon feature is the tenon itself; what is removed is the end zone of the rail (full section, tenon length) around it: cheeks and shoulders, in LOCAL coordinates.</summary>
        private static IEnumerable<Box3> TenonWaste(PartInstance part, Box3 t)
        {
            var fin = part.Finished;
            double x0 = t.Min.X, x1 = t.Max.X, W = fin.Width, T = fin.Thickness;
            var boxes = new[]
            {
                new Box3(new Vec3(x0, 0, 0), new Vec3(x1, W, t.Min.Z)),                    // below
                new Box3(new Vec3(x0, 0, t.Max.Z), new Vec3(x1, W, T)),                    // above
                new Box3(new Vec3(x0, 0, t.Min.Z), new Vec3(x1, t.Min.Y, t.Max.Z)),        // shoulder left
                new Box3(new Vec3(x0, t.Max.Y, t.Min.Z), new Vec3(x1, W, t.Max.Z)),       // shoulder right
            };
            return boxes.Where(bx => bx.Size.X > 1e-6 && bx.Size.Y > 1e-6 && bx.Size.Z > 1e-6);
        }

        /// <summary>Volume (mm³) removed by box cutters inside the part bounds (cylinders approximated by π r² L); used for checks and the waste report.</summary>
        public static double RemovedVolumeMm3(PartInstance part)
        {
            double v = 0; var b = part.Bounds;
            foreach (var c in CutsFor(part))
            {
                if (c.Kind == PrimKind.Box)
                {
                    double dx = Math.Min(c.Box.Max.X, b.Max.X) - Math.Max(c.Box.Min.X, b.Min.X);
                    double dy = Math.Min(c.Box.Max.Y, b.Max.Y) - Math.Max(c.Box.Min.Y, b.Min.Y);
                    double dz = Math.Min(c.Box.Max.Z, b.Max.Z) - Math.Max(c.Box.Min.Z, b.Min.Z);
                    if (dx > 0 && dy > 0 && dz > 0) v += dx * dy * dz;
                }
                else
                {
                    var d = c.P1 - c.P0; double len = Math.Sqrt(d.X * d.X + d.Y * d.Y + d.Z * d.Z);
                    v += Math.PI * c.Radius * c.Radius * len;
                }
            }
            return v;
        }

        /// <summary>Rough (pre-machining) block of a part, in the part's local frame (length, width, thickness); falls back to finished + trim for glued panels.</summary>
        public static Dims RoughOf(PartInstance part, PartFamily fam, double gluedTrim)
        {
            var fin = part.Finished;
            var rp = fam?.RoughPieces.FirstOrDefault();
            if (rp == null) return fin;
            if (rp.CountPerPart != 1) return new Dims(fin.Length + gluedTrim, fin.Width + gluedTrim, fin.Thickness + gluedTrim);
            var r = rp.Rough;
            // rough is stored per family in the same orientation as Finished (length, width, thickness)
            return new Dims(Math.Max(r.Length, fin.Length), Math.Max(r.Width, fin.Width), Math.Max(r.Thickness, fin.Thickness));
        }
    }
}
