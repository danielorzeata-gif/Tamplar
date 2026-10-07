using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Display;
using Rhino.Geometry;
using RhinoWood.Core.Display;

namespace RhinoWood.Plugin.UI
{
    /// <summary>Shows the previewed furniture in the Rhino viewport while the dialog is open (nothing is added to the document).</summary>
    public sealed class PreviewConduit : DisplayConduit
    {
        public List<GeometryPrimitive> Prims { get; set; } = new List<GeometryPrimitive>();
        public double Scale { get; set; } = 1;

        public BoundingBox Bounds
        {
            get
            {
                var bb = BoundingBox.Empty;
                foreach (var p in Prims.Where(x => x.Kind == PrimKind.Box))
                {
                    bb.Union(new Point3d(p.Box.Min.X * Scale, p.Box.Min.Y * Scale, p.Box.Min.Z * Scale));
                    bb.Union(new Point3d(p.Box.Max.X * Scale, p.Box.Max.Y * Scale, p.Box.Max.Z * Scale));
                }
                return bb;
            }
        }

        protected override void CalculateBoundingBox(CalculateBoundingBoxEventArgs e) { var b = Bounds; if (b.IsValid) e.IncludeBoundingBox(b); }

        protected override void PostDrawObjects(DrawEventArgs e)
        {
            foreach (var p in Prims)
            {
                if (p.Kind == PrimKind.Box)
                {
                    var box = new Box(Plane.WorldXY, new Interval(p.Box.Min.X * Scale, p.Box.Max.X * Scale), new Interval(p.Box.Min.Y * Scale, p.Box.Max.Y * Scale), new Interval(p.Box.Min.Z * Scale, p.Box.Max.Z * Scale));
                    bool part = p.Category == PrimCategory.Part;
                    var col = part ? System.Drawing.Color.FromArgb(192, 132, 90) : p.Category == PrimCategory.Hardware ? System.Drawing.Color.SteelBlue : System.Drawing.Color.Firebrick;
                    if (part) e.Display.DrawBrepShaded(box.ToBrep(), new DisplayMaterial(col, 0.35));
                    e.Display.DrawBox(box, col, part ? 2 : 1);
                }
                else if (p.Kind == PrimKind.Line)
                    e.Display.DrawLine(new Line(new Point3d(p.P0.X * Scale, p.P0.Y * Scale, p.P0.Z * Scale), new Point3d(p.P1.X * Scale, p.P1.Y * Scale, p.P1.Z * Scale)), System.Drawing.Color.SaddleBrown, 2);
            }
        }
    }
}
