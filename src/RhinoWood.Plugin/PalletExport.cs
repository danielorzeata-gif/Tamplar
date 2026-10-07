using System;
using System.Collections.Generic;
using System.Linq;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using RhinoWood.Core.Geometry;
using RhinoWood.Core.Logistics;

namespace RhinoWood.Plugin
{
    /// <summary>"Paletat": draws the packing plan in 3D next to the model: layer {name}::Paletat::Palet n, sub-layer per carton (carton shell + the parts inside).</summary>
    public static class PalletExport
    {
        private const string KPack = "rw.pack";

        public static string Run(RhinoDoc doc, PackingPlan plan, string name, string id, double originX, double originY)
        {
            double s = RhinoMath.UnitScale(UnitSystem.Millimeters, doc.ModelUnitSystem);
            foreach (var o in doc.Objects.GetObjectList(new ObjectEnumeratorSettings { NormalObjects = true, LockedObjects = true, HiddenObjects = true }).ToList())
                if (o.Attributes.GetUserString(KPack) == id) doc.Objects.Delete(o, true);

            string root = (string.IsNullOrWhiteSpace(name) ? "Piesa" : name.Replace("::", " ").Trim()) + "::Paletat";
            double x = originX;
            foreach (var pal in plan.Pallets)
            {
                string pl = root + "::" + pal.Id;
                int lp = RhinoSync.EnsureLayer(doc, pl, System.Drawing.Color.FromArgb(210, 180, 140));
                var at = Attr(lp, pal.Id + " · " + pal.Spec.Name, id);
                // deck (simplified EPAL: top boards, 3 blocks rows, bottom boards as one slab 144 mm high)
                doc.Objects.AddBrep(CutSolids.BoxBrep(new Box3(new Vec3(0, 0, 0), new Vec3(pal.Spec.L, pal.Spec.W, pal.Spec.Deck)), s, new Vec3(x, originY, 0)), at);
                doc.Objects.AddTextDot(new TextDot(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} · {1:0} kg · {2:0} mm înălțime", pal.Id, pal.MassKg, pal.Height), new Point3d((x + pal.Spec.L / 2) * s, (originY - 80) * s, 0)), at);
                foreach (var pc in pal.Cartons)
                {
                    var c = pc.Carton;
                    string cl = pl + "::" + c.Id + " [" + (c.Piece ?? "") + "]";
                    int lc = RhinoSync.EnsureLayer(doc, cl, System.Drawing.Color.FromArgb(196, 160, 110));
                    var ca = Attr(lc, c.Id + " · " + c.Spec.Name, id);
                    var shift = new Vec3(x + pc.Box.Min.X, originY + pc.Box.Min.Y, pal.Spec.Deck + pc.Box.Min.Z);
                    // translucent-looking shell: the outer box (edges visible through the parts)
                    var shell = CutSolids.BoxBrep(new Box3(new Vec3(0, 0, 0), new Vec3(c.OuterL, c.OuterW, c.OuterH)), s, shift);
                    var shellAttr = Attr(lc, c.Id + " · carton", id); shellAttr.ObjectColor = System.Drawing.Color.FromArgb(150, 110, 70); shellAttr.ColorSource = ObjectColorSource.ColorFromObject; shellAttr.Mode = ObjectMode.Normal;
                    doc.Objects.AddBrep(shell, shellAttr);
                    double wall = (c.OuterL - c.InnerL) / 2;
                    foreach (var it in c.Items)
                    {
                        var b = new Box3(new Vec3(it.Box.Min.X + wall, it.Box.Min.Y + wall, it.Box.Min.Z + wall), new Vec3(it.Box.Max.X + wall, it.Box.Max.Y + wall, it.Box.Max.Z + wall));
                        var pa = Attr(lc, it.Item.Label, id); pa.ObjectColor = ColorOf(it.Item.Group); pa.ColorSource = ObjectColorSource.ColorFromObject;
                        doc.Objects.AddBrep(CutSolids.BoxBrep(b, s, shift), pa);
                    }
                    doc.Objects.AddTextDot(new TextDot(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} · {1:0.0} kg · {2:0}×{3:0}×{4:0} · {5}", c.Id, c.Mass, c.OuterL, c.OuterW, c.OuterH, c.Courier), new Point3d((shift.X + c.OuterL / 2) * s, (shift.Y + c.OuterW / 2) * s, (shift.Z + c.OuterH) * s)), ca);
                }
                x += pal.Spec.L + 400;
            }
            doc.Views.Redraw(); RhinoApp.RunScript("-_Zoom _Extents", false);
            return plan.Pallets.Count + " paleți, " + plan.Cartons.Count + " cutii, " + plan.TotalKg.ToString("0") + " kg — layerul „" + root + "”.";
        }

        private static ObjectAttributes Attr(int layer, string name, string id)
        {
            var a = new ObjectAttributes { LayerIndex = layer, Name = name };
            a.SetUserString(KPack, id);
            return a;
        }

        private static System.Drawing.Color ColorOf(string group)
        {
            int h = Math.Abs((group ?? "").GetHashCode());
            var tones = new[] { System.Drawing.Color.FromArgb(192, 132, 90), System.Drawing.Color.FromArgb(176, 120, 78), System.Drawing.Color.FromArgb(205, 150, 100), System.Drawing.Color.FromArgb(160, 105, 70), System.Drawing.Color.FromArgb(214, 168, 120), System.Drawing.Color.FromArgb(150, 100, 64) };
            return tones[h % tones.Length];
        }
    }
}
