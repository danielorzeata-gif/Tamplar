using System;
using System.Collections.Generic;
using System.Linq;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using RhinoWood.Core.Display;
using RhinoWood.Core.Domain;
using RhinoWood.Core.Geometry;
using RhinoWood.Core.Projects;

namespace RhinoWood.Plugin
{
    /// <summary>
    /// "Export piese de debitare": every part laid flat next to the model, in three layers that can be switched on and off:
    /// 01 Piesă brută (rough block), 02 După rindeluire (finished-size block), 03 Debitare (the part with joinery/holes + the removed material in red).
    /// The three stages sit in three columns, one row per part.
    /// </summary>
    public static class CutPartsExport
    {
        private const string Parent = "Debitare";
        private const string L1 = "01 Piesă brută", L2 = "02 După rindeluire", L3 = "03 Debitare (piesă cu găuri + deșeu roșu)", L4 = "04 Lamele (biscuiți)";
        private const string KExport = "rw.export";

        private static int Layer(RhinoDoc doc, string name, System.Drawing.Color color)
        {
            int parent = doc.Layers.FindByFullPath(Parent, -1);
            if (parent < 0) parent = doc.Layers.Add(new Layer { Name = Parent, Color = System.Drawing.Color.SaddleBrown });
            int idx = doc.Layers.FindByFullPath(Parent + "::" + name, -1);
            if (idx >= 0) return idx;
            return doc.Layers.Add(new Layer { Name = name, Color = color, ParentLayerId = doc.Layers[parent].Id });
        }

        public static string Run(RhinoDoc doc, WoodProject project, ProjectResult result)
        {
            if (doc == null || project == null || result == null) return "Nu există proiect sau document.";
            double s = RhinoMath.UnitScale(UnitSystem.Millimeters, doc.ModelUnitSystem);
            // replace a previous export of this project
            foreach (var o in doc.Objects.GetObjectList(new ObjectEnumeratorSettings { NormalObjects = true, LockedObjects = true, HiddenObjects = true }).ToList())
                if (o.Attributes.GetUserString(KExport) == project.Id) doc.Objects.Delete(o, true);

            int l1 = Layer(doc, L1, System.Drawing.Color.FromArgb(150, 150, 150));
            int l2 = Layer(doc, L2, System.Drawing.Color.FromArgb(192, 132, 90));
            int l3 = Layer(doc, L3, System.Drawing.Color.FromArgb(160, 100, 60));
            int l4 = Layer(doc, L4, System.Drawing.Color.FromArgb(230, 200, 140));

            var model = result.Model;
            var rules = project.Settings.Rules;
            // one item per solid: ordinary parts, and every strip of an edge-glued panel (each strip is cut from its own board)
            var items = new List<(PartInstance part, PartFamily fam, Box3 region, Dims fin, Dims rough, string name)>();
            foreach (var part in model.AllParts.OrderBy(x => x.Id, StringComparer.Ordinal))
            {
                var fam = model.FamilyOf(part.Id);
                var strips = PartSolids.StripBoxes(part, fam);
                for (int i = 0; i < strips.Count; i++)
                {
                    var sb = strips[i];
                    var fin = new Dims(sb.Size.Get(part.LengthAxis), sb.Size.Get(part.WidthAxis), sb.Size.Get(part.ThicknessAxis));
                    var rough = strips.Count > 1 ? fam.RoughPieces[0].Rough : PartSolids.RoughOf(part, fam, rules.GluedPanelTrim);
                    items.Add((part, fam, sb, fin, new Dims(Math.Max(rough.Length, fin.Length), Math.Max(rough.Width, fin.Width), Math.Max(rough.Thickness, fin.Thickness)),
                        (fam?.Name ?? part.Id) + " · " + part.Id + (strips.Count > 1 ? " lamela " + (i + 1) + "/" + strips.Count : "")));
                }
            }
            double modelMaxX = model.AllParts.Max(p => p.Bounds.Max.X);
            double colW = items.Max(i => i.rough.Length) + 300;
            double x0 = modelMaxX + 600;
            double y = 0; int failed = 0, n = 0;
            var red = System.Drawing.Color.FromArgb(200, 40, 30);

            foreach (var it in items)
            {
                var part = it.part; var R = it.rough; var F = it.fin;
                var d = new Vec3((R.Length - F.Length) / 2, (R.Width - F.Width) / 2, (R.Thickness - F.Thickness) / 2);   // finished block centred in the rough block
                var rowOrigin = new Vec3(x0, y, 0);
                // local frame of this solid: the part frame moved to the strip's min corner
                var origin = part.WorldToLocal(it.region.Min);

                var rough = CutSolids.BoxBrep(new Box3(new Vec3(0, 0, 0), new Vec3(R.Length, R.Width, R.Thickness)), s, rowOrigin);
                var fin = CutSolids.BoxBrep(new Box3(d, d + new Vec3(F.Length, F.Width, F.Thickness)), s, rowOrigin + new Vec3(colW, 0, 0));
                var shift3 = rowOrigin + new Vec3(2 * colW, 0, 0) + d - origin;
                var cutters = PartSolids.CutsIn(PartSolids.CutsFor(part), it.region).Select(c => ToLocal(part, c)).Select(c => CutSolids.CutterBrep(Shift(c, shift3), s)).ToList();
                var finIn3 = CutSolids.BoxBrep(new Box3(d, d + new Vec3(F.Length, F.Width, F.Thickness)), s, rowOrigin + new Vec3(2 * colW, 0, 0));
                var roughIn3 = CutSolids.BoxBrep(new Box3(new Vec3(0, 0, 0), new Vec3(R.Length, R.Width, R.Thickness)), s, rowOrigin + new Vec3(2 * colW, 0, 0));
                var cutPart = CutSolids.Subtract(finIn3, cutters, out var ok);
                if (!ok) failed++;

                string label = it.name;
                Add(doc, rough, l1, part, label + " · brut " + R, null);
                doc.Objects.AddTextDot(new TextDot(label, new Point3d((rowOrigin.X + R.Length / 2) * s, (rowOrigin.Y + R.Width / 2) * s, R.Thickness * s)), Attr(doc, l1, part, label, null));
                Add(doc, fin, l2, part, label + " · după rindeluire " + F, null);
                Add(doc, cutPart, l3, part, label + " · piesa cu îmbinări", null);

                // removed material in red: planing allowance shell + chips from joinery (cutters inside the finished block)
                var shell = Brep.CreateBooleanDifference(new[] { roughIn3 }, new[] { finIn3 }, CutSolids.Tolerance);
                if (shell != null) foreach (var b in shell) Add(doc, b, l3, part, label + " · deșeu rindeluire", red);
                foreach (var c in cutters.Where(c => c != null))
                {
                    var chips = Brep.CreateBooleanIntersection(c, finIn3, CutSolids.Tolerance);
                    if (chips != null) foreach (var b in chips) Add(doc, b, l3, part, label + " · deșeu îmbinare", red);
                }
                y += R.Width + 120; n++;
            }

            // biscuits, in a row after the parts
            int bi = 0; double by = y + 100;
            foreach (var bsc in model.Biscuits)
            {
                var sz = bsc.Box.Size; var center = new Vec3(x0 + 30 + (bi % 40) * 70, by + (bi / 40) * 40, 0);
                var box = new Box3(center, center + new Vec3(sz.X, sz.Y, sz.Z));
                var br = CutSolids.BiscuitBrep(box, s);
                var at = Attr(doc, l4, model.FindPart(bsc.PartId), "Lamelă " + bsc.Size + " " + bsc.Id, null);
                doc.Objects.AddBrep(br, at); bi++;
            }
            doc.Views.Redraw();
            RhinoApp.RunScript("-_Zoom _Extents", false);
            return n + " piese și " + model.Biscuits.Count + " lamele exportate în layerele „" + Parent + "”" + (failed > 0 ? " (" + failed + " piese: decuparea booleană a eșuat, s-a păstrat blocul)" : "") + ".";
        }

        private static CutVolume ToLocal(PartInstance part, CutVolume c)
        {
            if (c.Kind == PrimKind.Box) return new CutVolume { Kind = PrimKind.Box, Box = part.WorldBoxToLocal(c.Box), Label = c.Label };
            return new CutVolume { Kind = PrimKind.Cylinder, P0 = part.WorldToLocal(c.P0), P1 = part.WorldToLocal(c.P1), Radius = c.Radius, Label = c.Label };
        }

        private static CutVolume Shift(CutVolume c, Vec3 d)
        {
            if (c.Kind == PrimKind.Box) return new CutVolume { Kind = PrimKind.Box, Box = new Box3(c.Box.Min + d, c.Box.Max + d), Label = c.Label };
            return new CutVolume { Kind = PrimKind.Cylinder, P0 = c.P0 + d, P1 = c.P1 + d, Radius = c.Radius, Label = c.Label };
        }

        private static ObjectAttributes Attr(RhinoDoc doc, int layer, PartInstance part, string name, System.Drawing.Color? color)
        {
            var a = new ObjectAttributes { LayerIndex = layer, Name = name };
            a.SetUserString(KExport, WoodPlugin.Instance.Project.Id); a.SetUserString("rw.part", part.Id);
            if (color.HasValue) { a.ObjectColor = color.Value; a.ColorSource = ObjectColorSource.ColorFromObject; }
            return a;
        }

        private static void Add(RhinoDoc doc, Brep b, int layer, PartInstance part, string name, System.Drawing.Color? color)
        {
            if (b != null) doc.Objects.AddBrep(b, Attr(doc, layer, part, name, color));
        }
    }
}
