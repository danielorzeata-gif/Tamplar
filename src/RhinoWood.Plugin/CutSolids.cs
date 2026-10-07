using System;
using System.Collections.Generic;
using System.Linq;
using Rhino;
using Rhino.Geometry;
using RhinoWood.Core.Display;
using RhinoWood.Core.Geometry;

namespace RhinoWood.Plugin
{
    /// <summary>Boolean helpers: parts are real cut solids (mortises, holes, tenon cheeks subtracted), not bounding boxes.</summary>
    public static class CutSolids
    {
        public static double Tolerance => RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? 0.01;

        public static Brep BoxBrep(Box3 b, double s, Vec3? shift = null)
        {
            var d = shift ?? new Vec3(0, 0, 0);
            return new Box(Plane.WorldXY, new Interval((b.Min.X + d.X) * s, (b.Max.X + d.X) * s), new Interval((b.Min.Y + d.Y) * s, (b.Max.Y + d.Y) * s), new Interval((b.Min.Z + d.Z) * s, (b.Max.Z + d.Z) * s)).ToBrep();
        }

        public static Brep CylinderBrep(Vec3 p0, Vec3 p1, double radius, double s)
        {
            var a = new Point3d(p0.X * s, p0.Y * s, p0.Z * s); var b = new Point3d(p1.X * s, p1.Y * s, p1.Z * s);
            var dir = b - a; double h = dir.Length;
            return h < 1e-9 ? null : new Cylinder(new Circle(new Plane(a, dir), Math.Max(radius * s, 1e-6)), h).ToBrep(true, true);
        }

        public static Brep CutterBrep(CutVolume c, double s) =>
            c.Kind == PrimKind.Box ? BoxBrep(c.Box, s) : CylinderBrep(c.P0, c.P1, c.Radius, s);

        /// <summary>Subtracts all cutters from the stock. Returns the stock unchanged (and ok=false) if the boolean fails, so geometry is never lost.</summary>
        public static Brep Subtract(Brep stock, IEnumerable<Brep> cutters, out bool ok)
        {
            ok = true;
            var list = cutters.Where(c => c != null).ToList();
            if (list.Count == 0) return stock;
            var res = Brep.CreateBooleanDifference(new[] { stock }, list, Tolerance);
            if (res == null || res.Length == 0) { ok = false; return stock; }
            return res.Length == 1 ? res[0] : res.OrderByDescending(b => b.GetVolume()).First();
        }

        /// <summary>The part's cut solid for the document (scale s = model units per mm).</summary>
        public static Brep CutPart(GeometryPrimitive p, double s)
        {
            var stock = BoxBrep(p.Box, s);
            if (p.Cuts == null || p.Cuts.Count == 0) return stock;
            var cut = Subtract(stock, p.Cuts.Select(c => CutterBrep(c, s)), out var ok);
            if (!ok) RhinoApp.WriteLine("Rhino Wood: decuparea piesei " + p.PartId + " a eșuat; se afișează blocul fără decupări.");
            return cut;
        }
    }
}
