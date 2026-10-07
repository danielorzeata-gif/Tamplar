using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Eto.Drawing;
using Eto.Forms;
using Rhino;
using Rhino.Display;
using Rhino.Geometry;
using RhinoWood.Core.Display;
using RhinoWood.Core.Domain;
using RhinoWood.Core.Furniture;
using RhinoWood.Core.Joinery;
using RhinoWood.Core.Libraries;
using RhinoWood.Core.Projects;

namespace RhinoWood.Plugin.UI
{
    /// <summary>2D oblique preview drawn from the Core model (no Rhino geometry is created until the user presses Generate).</summary>
    internal sealed class PreviewDrawable : Drawable
    {
        public List<PreviewPolygon> Polys { get; set; } = new List<PreviewPolygon>();

        public PreviewDrawable() { Size = new Size(420, 300); BackgroundColor = Colors.White; }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            if (Polys.Count == 0) { g.DrawText(SystemFonts.Default(), Colors.Gray, 10, 10, "No preview"); return; }
            var (minX, minY, maxX, maxY) = PreviewEngine.Extent(Polys);
            float pad = 14;
            double w = Math.Max(1, Width - 2 * pad), h = Math.Max(1, Height - 2 * pad);
            float s = (float)Math.Min(w / Math.Max(1e-6, maxX - minX), h / Math.Max(1e-6, maxY - minY));
            float ox = pad + (float)((w - (maxX - minX) * s) / 2), oy = pad + (float)((h - (maxY - minY) * s) / 2);
            foreach (var p in Polys)
            {
                var pts = new PointF[p.X.Length];
                for (int i = 0; i < pts.Length; i++) pts[i] = new PointF(ox + (float)(p.X[i] - minX) * s, oy + (float)(p.Y[i] - minY) * s);
                if (p.Fill != null) g.FillPolygon(Color.Parse(p.Fill), pts);
                if (p.Stroke != null) g.DrawPolygon(Color.Parse(p.Stroke), pts);
            }
        }
    }

    /// <summary>Shows the previewed furniture in the Rhino viewport while the dialog is open (nothing is added to the document).</summary>
    internal sealed class PreviewConduit : DisplayConduit
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

    /// <summary>
    /// "New project" dialog: parameters, material, joint / hardware choices - with a live preview and live plan summary BEFORE anything
    /// is generated in the Rhino document. OK returns the configured <see cref="WoodProject"/>.
    /// </summary>
    internal sealed class NewProjectDialog : Dialog<WoodProject>
    {
        private readonly WoodProject _p;
        private readonly RhinoDoc _doc;
        private readonly PreviewDrawable _preview = new PreviewDrawable();
        private readonly PreviewConduit _conduit = new PreviewConduit();
        private readonly TextArea _summary = new TextArea { ReadOnly = true, Size = new Size(420, 170), Font = new Font(FontFamilies.Monospace, 8) };
        private readonly CheckBox _joinery = new CheckBox { Text = "Show joinery / holes / hardware" };
        private readonly CheckBox _exploded = new CheckBox { Text = "Exploded" };
        private readonly CheckBox _viewport = new CheckBox { Text = "Show preview in Rhino viewport", Checked = true };
        private bool _loading = true, _zoomed;

        public NewProjectDialog(RhinoDoc doc, WoodProject project)
        {
            _doc = doc; _p = project;
            Title = "Rhino Wood - New " + project.Furniture.Name;
            Padding = new Padding(10);
            Resizable = true;

            var left = new DynamicLayout { Spacing = new Size(6, 6), Padding = new Padding(0, 0, 10, 0) };
            var sp = new DropDown();
            foreach (var s in project.Library.Species.Values.OrderBy(s => s.Name)) sp.Items.Add(new ListItem { Text = s.Name + " (" + s.PricePerM3.ToString("0") + "/m3)", Key = s.Id });
            sp.SelectedKey = project.SpeciesId;
            sp.SelectedKeyChanged += (s, e) => Apply(() => project.SetSpecies(sp.SelectedKey));
            left.AddRow(new Label { Text = "Species", Font = new Font(SystemFont.Bold) }, sp);

            var advanced = new DynamicLayout { Spacing = new Size(6, 6) };
            foreach (var d in project.Furniture.Parameters)
            {
                var def = d;
                var ns = new NumericStepper { MinValue = def.Min, MaxValue = def.Max, DecimalPlaces = 0, Increment = 5, Value = project.Parameters[def.Key], ToolTip = def.Description };
                ns.ValueChanged += (s, e) => Apply(() => project.SetParameter(def.Key, ns.Value));
                (def.Advanced ? advanced : left).AddRow(new Label { Text = def.Label + " (" + def.Unit + ")", ToolTip = def.Description }, ns);
            }
            left.AddRow(new Expander { Header = "Advanced dimensions", Expanded = false, Content = advanced });

            // joints & hardware: the user decides
            foreach (var grp in project.Furniture.Choices.GroupBy(c => c.Group))
            {
                left.AddRow(new Label { Text = grp.Key.ToUpperInvariant(), Font = new Font(SystemFont.Bold) });
                foreach (var ch in grp)
                {
                    var choice = ch;
                    var dd = new DropDown();
                    foreach (var o in choice.Options) dd.Items.Add(new ListItem { Text = OptionLabel(choice, o), Key = o });
                    dd.SelectedKey = project.Choices[choice.Key];
                    var info = new Label { Wrap = WrapMode.Word, Width = 330, TextColor = Colors.DimGray };
                    info.Text = OptionInfo(choice, dd.SelectedKey);
                    dd.SelectedKeyChanged += (s, e) => { info.Text = OptionInfo(choice, dd.SelectedKey); Apply(() => project.SetChoice(choice.Key, dd.SelectedKey)); };
                    left.AddRow(new Label { Text = choice.Label, ToolTip = choice.Description }, dd);
                    left.AddRow(null, info);
                }
            }

            _joinery.CheckedChanged += (s, e) => Refresh();
            _exploded.CheckedChanged += (s, e) => Refresh();
            _viewport.CheckedChanged += (s, e) => { _conduit.Enabled = _viewport.Checked == true; _doc.Views.Redraw(); };

            var right = new DynamicLayout { Spacing = new Size(6, 6) };
            right.AddRow(new Label { Text = "PREVIEW (nothing is created in the document yet)", Font = new Font(SystemFont.Bold) });
            right.Add(_preview);
            right.AddRow(_joinery, _exploded);
            right.AddRow(_viewport);
            right.AddRow(new Label { Text = "PLAN", Font = new Font(SystemFont.Bold) });
            right.Add(_summary);

            var ok = new Button { Text = "Generate" };
            var cancel = new Button { Text = "Cancel" };
            ok.Click += (s, e) => { Cleanup(); Close(_p); };
            cancel.Click += (s, e) => { Cleanup(); Close(null); };
            DefaultButton = ok; AbortButton = cancel;
            Closed += (s, e) => Cleanup();

            var root = new DynamicLayout { Spacing = new Size(8, 8) };
            root.AddRow(new Scrollable { Content = left, Size = new Size(470, 560), Border = BorderType.None }, right);
            root.AddRow(null, new StackLayout { Orientation = Orientation.Horizontal, Spacing = 8, Items = { cancel, ok } });
            Content = root;

            _conduit.Enabled = true;
            _loading = false;
            Refresh();
        }

        private string OptionLabel(ChoiceDef c, string id)
        {
            if (c.Kind == "joint")
            {
                var d = _p.Joints.Get(id);
                return d.Name + "   " + Stars(d.Info.Strength);
            }
            return _p.Library.Hardware.TryGetValue(id, out var h) ? h.Model : id;
        }

        private static string Stars(int n) => new string('*', Math.Max(0, Math.Min(5, n))) + new string('-', 5 - Math.Max(0, Math.Min(5, n)));

        private string OptionInfo(ChoiceDef c, string id)
        {
            var sb = new StringBuilder();
            if (c.Kind == "joint")
            {
                var i = _p.Joints.Get(id).Info;
                sb.Append(i.Summary).Append("  Strength ").Append(Stars(i.Strength)).Append(", difficulty ").Append(Stars(i.Difficulty)).Append(i.VisibleFromOutside ? ", visible." : ", hidden.");
                if (i.Pros.Count > 0) sb.Append("  + ").Append(string.Join("; ", i.Pros)).Append('.');
                if (i.Cons.Count > 0) sb.Append("  - ").Append(string.Join("; ", i.Cons)).Append('.');
            }
            else if (_p.Library.Hardware.TryGetValue(id, out var h))
            {
                sb.Append(h.InstallationNotes).Append("  Travel allowance ").Append(h.TravelAllowance.ToString("0.#", CultureInfo.InvariantCulture)).Append(" mm.");
            }
            return sb.ToString();
        }

        private void Apply(Action change)
        {
            if (_loading) return;
            try { change(); Refresh(); }
            catch (Exception ex) { _summary.Text = "Cannot apply: " + ex.Message; }
        }

        private void Refresh()
        {
            if (_loading) return;
            try
            {
                var r = _p.Recalculate();
                var mode = _joinery.Checked == true ? RhinoWood.Core.Domain.DisplayMode.Engineering : RhinoWood.Core.Domain.DisplayMode.Normal;
                _preview.Polys = PreviewEngine.Build(r.Model, mode, _exploded.Checked == true);
                _preview.Invalidate();

                var sb = new StringBuilder();
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0} parts in {1} families, {2} joints, {3} hardware items",
                    r.Model.AllParts.Count(), r.Model.Families.Count, r.Model.Joints.Count, r.Model.HardwareInstalls.Count));
                foreach (var f in r.Model.Families) sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  {0} x {1}: {2}", f.Quantity, f.Name, f.Finished));
                sb.AppendLine("Purchase (optimized, all parts together):");
                foreach (var l in r.Optimization.Purchase) sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  {0} x {1} x {2:0}  = {3:0.00}", l.Quantity, l.Item.Label, l.Length, l.Total));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "Total cost {0:0.00} {1}  (material {2:0.00}, hardware {3:0.00})", r.Cost.Total, r.Cost.Currency, r.Cost.RawMaterial, r.Cost.Hardware));
                var issues = r.Issues.Where(i => i.Severity != Severity.Info).ToList();
                sb.AppendLine(issues.Count == 0 ? "Validation: OK" : "Validation:");
                foreach (var i in issues) sb.AppendLine("  " + i.Severity.ToString().ToUpperInvariant() + ": " + i.Message);
                _summary.Text = sb.ToString();

                _conduit.Scale = RhinoMath.UnitScale(UnitSystem.Millimeters, _doc.ModelUnitSystem);
                _conduit.Prims = _p.GenerateGeometry(mode);
                _conduit.Enabled = _viewport.Checked == true;
                if (!_zoomed && _conduit.Bounds.IsValid && _doc.Views.ActiveView != null) { _doc.Views.ActiveView.ActiveViewport.ZoomBoundingBox(_conduit.Bounds); _zoomed = true; }
                _doc.Views.Redraw();
            }
            catch (Exception ex) { _summary.Text = "Preview failed: " + ex.Message; }
        }

        private void Cleanup()
        {
            _conduit.Enabled = false;
            try { _doc.Views.Redraw(); } catch { }
        }
    }
}
