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
        private const string L1 = "01 Piesă brută", L2 = "02 După rindeluire", L3 = "03 Debitare (piesă cu găuri + deșeu roșu)";
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

            var model = result.Model;
            var rules = project.Settings.Rules;
            var items = model.AllParts.Select(p => (part: p, fam: model.FamilyOf(p.Id), rough: PartSolids.RoughOf(p, model.FamilyOf(p.Id), rules.GluedPanelTrim))).ToList();
            double modelMaxX = model.AllParts.Max(p => p.Bounds.Max.X);
            double colW = items.Max(i => i.rough.Length) + 300;
            double x0 = modelMaxX + 600;
            double y = 0; int failed = 0, n = 0;

            foreach (var it in items.OrderBy(i => i.part.Id, StringComparer.Ordinal))
            {
                var part = it.part; var R = it.rough; var F = part.Finished;
                var d = new Vec3((R.Length - F.Length) / 2, (R.Width - F.Width) / 2, (R.Thickness - F.Thickness) / 2);   // finished block centred in the rough block
                var rowOrigin = new Vec3(x0, y, 0);

                var rough = CutSolids.BoxBrep(new Box3(new Vec3(0, 0, 0), new Vec3(R.Length, R.Width, R.Thickness)), s, rowOrigin);
                var fin = CutSolids.BoxBrep(new Box3(d, d + new Vec3(F.Length, F.Width, F.Thickness)), s, rowOrigin + new Vec3(colW, 0, 0));

                // cutters in the part's local frame, then into column 3
                var shift3 = rowOrigin + new Vec3(2 * colW, 0, 0) + d;
                var cutters = PartSolids.CutsFor(part).Select(c => ToLocal(part, c)).Select(c => CutSolids.CutterBrep(c, s) == null ? null : CutSolids.CutterBrep(Shift(c, shift3), s)).ToList();
                var finIn3 = CutSolids.BoxBrep(new Box3(d, d + new Vec3(F.Length, F.Width, F.Thickness)), s, rowOrigin + new Vec3(2 * colW, 0, 0));
                var roughIn3 = CutSolids.BoxBrep(new Box3(new Vec3(0, 0, 0), new Vec3(R.Length, R.Width, R.Thickness)), s, rowOrigin + new Vec3(2 * colW, 0, 0));
                var cutPart = CutSolids.Subtract(finIn3, cutters, out var ok);
                if (!ok) failed++;

                string label = (it.fam?.Name ?? part.Id) + " · " + part.Id;
                Add(doc, rough, l1, part, label + " · brut " + R, null);
                doc.Objects.AddTextDot(new TextDot(part.Id, new Point3d((rowOrigin.X + R.Length / 2) * s, (rowOrigin.Y + R.Width / 2) * s, R.Thickness * s)) { }, Attr(doc, l1, part, label, null));
                Add(doc, fin, l2, part, label + " · după rindeluire " + F, null);
                Add(doc, cutPart, l3, part, label + " · piesa cu îmbinări", null);

                // removed material in red: planing allowance shell + chips from joinery (cutters inside the finished block)
                var red = System.Drawing.Color.FromArgb(200, 40, 30);
                var shell = Brep.CreateBooleanDifference(new[] { roughIn3 }, new[] { finIn3 }, CutSolids.Tolerance);
                if (shell != null) foreach (var b in shell) Add(doc, b, l3, part, label + " · deșeu rindeluire", red);
                foreach (var c in cutters.Where(c => c != null))
                {
                    var chips = Brep.CreateBooleanIntersection(c, finIn3, CutSolids.Tolerance);
                    if (chips != null) foreach (var b in chips) Add(doc, b, l3, part, label + " · deșeu îmbinare", red);
                }
                y += R.Width + 120; n++;
            }
            doc.Views.Redraw();
            RhinoApp.RunScript("-_Zoom _Extents", false);
            return n + " piese exportate în layerele „" + Parent + "”" + (failed > 0 ? " (" + failed + " piese: decuparea booleană a eșuat, s-a păstrat blocul)" : "") + ".";
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
