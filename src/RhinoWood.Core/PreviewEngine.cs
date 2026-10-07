using System;
using System.Collections.Generic;
using System.Linq;
using RhinoWood.Core.Display;
using RhinoWood.Core.Domain;
using RhinoWood.Core.Geometry;

namespace RhinoWood.Core.Display
{
    public sealed class PreviewPolygon
    {
        public double[] X { get; set; }
        public double[] Y { get; set; }
        public string Fill { get; set; }      // #rrggbb or null
        public string Stroke { get; set; }
        public double Depth { get; set; }     // larger = further away (draw first)
        public bool IsFeature { get; set; }
        public string PartId { get; set; }
    }

    /// <summary>
    /// Lightweight 2D oblique preview of a furniture model (used by the "before you generate" preview in the new-project dialog and by the
    /// SVG exports). Works on the Core model only - no Rhino needed.
    /// </summary>
    public static class PreviewEngine
    {
        public static List<PreviewPolygon> Build(FurnitureModel model, DisplayMode mode, bool exploded)
        {
            var polys = new List<PreviewPolygon>();
            var center = model.Bounds.Center;
            foreach (var part in model.AllParts)
            {
                var fam = model.FamilyOf(part.Id);
                Vec3 off = exploded ? ExplodeOffset(fam.Type, part, center) : Vec3.Zero;
                bool top = fam.Type == PartType.Top;
                AddBox(polys, part.Bounds.Offset(off), top ? "#e4cfae" : fam.Type == PartType.Leg ? "#c0845a" : "#b87a52", "#222", false, part.Id);
                if (mode >= DisplayMode.Engineering)
                    foreach (var f in part.Features)
                    {
                        var wb = part.FeatureWorldBox(f).Offset(off);
                        AddBox(polys, wb, null, f.Kind == FeatureKind.Tenon ? "#1b6ca8" : "#c0392b", true, part.Id);
                    }
            }
            return polys.OrderByDescending(p => p.IsFeature ? 0 : 1).ThenByDescending(p => p.Depth).ToList().OrderBy(p => p.IsFeature ? 1 : 0).ThenByDescending(p => p.Depth).ToList();
        }

        public static (double minX, double minY, double maxX, double maxY) Extent(IEnumerable<PreviewPolygon> polys)
        {
            var l = polys.ToList();
            if (l.Count == 0) return (0, 0, 1, 1);
            return (l.Min(p => p.X.Min()), l.Min(p => p.Y.Min()), l.Max(p => p.X.Max()), l.Max(p => p.Y.Max()));
        }

        private static Vec3 ExplodeOffset(PartType t, PartInstance p, Vec3 c)
        {
            if (t == PartType.Top) return new Vec3(0, 0, 220);
            if (t == PartType.Leg) return new Vec3((p.Bounds.Center.X < c.X ? -1 : 1) * 120, (p.Bounds.Center.Y < c.Y ? -1 : 1) * 120, -120);
            return new Vec3(0, 0, 40);
        }

        private static (double, double) Proj(Vec3 v) => (v.X + 0.5 * v.Y, -(v.Z + 0.35 * v.Y));

        private static void AddBox(List<PreviewPolygon> list, Box3 b, string fill, string stroke, bool feature, string partId)
        {
            Vec3 a = b.Min, c = b.Max;
            double d = (a.Y + c.Y) / 2 - 0.1 * (a.Z + c.Z) / 2 - 0.0005 * (a.X + c.X);
            void Face(Vec3[] vs, string col, double dd)
            {
                var pts = vs.Select(Proj).ToList();
                list.Add(new PreviewPolygon { X = pts.Select(q => q.Item1).ToArray(), Y = pts.Select(q => q.Item2).ToArray(), Fill = col, Stroke = stroke, Depth = dd, IsFeature = feature, PartId = partId });
            }
            Func<string, string, string> shade = (f, s) => f == null ? null : s;
            Face(new[] { new Vec3(a.X, a.Y, a.Z), new Vec3(c.X, a.Y, a.Z), new Vec3(c.X, a.Y, c.Z), new Vec3(a.X, a.Y, c.Z) }, fill, d);
            Face(new[] { new Vec3(a.X, a.Y, c.Z), new Vec3(c.X, a.Y, c.Z), new Vec3(c.X, c.Y, c.Z), new Vec3(a.X, c.Y, c.Z) }, fill == null ? null : Lighten(fill), d + 0.01);
            Face(new[] { new Vec3(c.X, a.Y, a.Z), new Vec3(c.X, c.Y, a.Z), new Vec3(c.X, c.Y, c.Z), new Vec3(c.X, a.Y, c.Z) }, fill == null ? null : Darken(fill), d + 0.02);
        }

        private static string Lighten(string hex) => Mix(hex, 255, 0.35);
        private static string Darken(string hex) => Mix(hex, 0, 0.22);
        private static string Mix(string hex, int target, double k)
        {
            int r = Convert.ToInt32(hex.Substring(1, 2), 16), g = Convert.ToInt32(hex.Substring(3, 2), 16), bl = Convert.ToInt32(hex.Substring(5, 2), 16);
            Func<int, int> m = v => (int)Math.Round(v + (target - v) * k);
            return "#" + m(r).ToString("x2") + m(g).ToString("x2") + m(bl).ToString("x2");
        }
    }
}
