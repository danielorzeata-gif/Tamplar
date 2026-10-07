using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Eto.Drawing;
using Eto.Forms;
using Rhino;
using RhinoWood.Core.Domain;
using RhinoWood.Core.Projects;
using RhinoWood.Core.Optimization;
using RhinoWood.Core.Reports;

namespace RhinoWood.Plugin.UI
{
    /// <summary>
    /// Dockable Rhino panel with the 12 sections of the system. Business logic lives in RhinoWood.Core; this class only presents it.
    /// Progressive disclosure: advanced parameters sit in a collapsed expander.
    /// </summary>
    [Guid("a7d2c1f4-93b0-4c1e-8f65-5b0f6d3e2a10")]
    public class WoodPanel : Panel
    {
        private readonly TabControl _tabs = new TabControl { Size = new Size(380, 600) };
        private static WoodPlugin P => WoodPlugin.Instance;

        public WoodPanel()
        {
            Content = _tabs;
            P.ProjectChanged += (s, e) => Application.Instance.AsyncInvoke(Rebuild);
            Rebuild();
        }

        public static System.Drawing.Icon CreateIcon()
        {
            using (var bmp = new System.Drawing.Bitmap(32, 32))
            {
                using (var g = System.Drawing.Graphics.FromImage(bmp))
                {
                    g.Clear(System.Drawing.Color.Transparent);
                    g.FillRectangle(System.Drawing.Brushes.Sienna, 3, 6, 26, 6);
                    g.FillRectangle(System.Drawing.Brushes.Peru, 5, 12, 4, 16); g.FillRectangle(System.Drawing.Brushes.Peru, 23, 12, 4, 16);
                }
                return System.Drawing.Icon.FromHandle(bmp.GetHicon());
            }
        }

        private void Rebuild()
        {
            int sel = _tabs.SelectedIndex;
            _tabs.Pages.Clear();
            var p = P.Project; var r = P.LastResult;
            _tabs.Pages.Add(new TabPage { Text = "PROJECT", Content = ProjectPage(p, r) });
            if (p != null && r != null)
            {
                _tabs.Pages.Add(new TabPage { Text = "FURNITURE", Content = FurniturePage(p) });
                _tabs.Pages.Add(new TabPage { Text = "COMPONENTS", Content = Grid(new[] { "Id", "Name", "Qty", "Finished", "Rough", "Species" }, r.Model.Families.Select(f => new[] { f.Id, f.Name, f.Quantity.ToString(), f.Finished.ToString(), string.Join(" + ", f.RoughPieces.Select(x => x.CountPerPart + "x " + x.Rough)), f.SpeciesId })) });
                _tabs.Pages.Add(new TabPage { Text = "MATERIALS", Content = MaterialsPage(p) });
                _tabs.Pages.Add(new TabPage { Text = "JOINERY", Content = Grid(new[] { "Joint", "Type", "A", "B", "Detail" }, r.Model.Joints.Select(j => new[] { j.Id, j.JointTypeId, j.PartAId, j.PartBId, j.Description + (j.Mitred ? " [mitred]" : "") })) });
                _tabs.Pages.Add(new TabPage { Text = "HARDWARE", Content = Grid(new[] { "Id", "Hardware", "Host", "Mate", "Features" }, r.Model.HardwareInstalls.Select(h => new[] { h.Id, h.HardwareId, h.HostPartId, h.MatePartId, h.FeatureIds.Count.ToString() })) });
                _tabs.Pages.Add(new TabPage { Text = "ENGINEERING", Content = EngineeringPage(p, r) });
                _tabs.Pages.Add(new TabPage { Text = "MANUFACTURING", Content = Grid(new[] { "Op", "Part", "Type", "Description", "Min" }, r.Manufacturing.Operations.Select(o => new[] { o.Id, o.PartId, o.Type.ToString(), o.Description, o.Minutes.ToString("0.#", CultureInfo.InvariantCulture) })) });
                _tabs.Pages.Add(new TabPage { Text = "OPTIMIZATION", Content = OptimizationPage(p, r) });
                _tabs.Pages.Add(new TabPage { Text = "PROCUREMENT", Content = ProcurementPage(r) });
                _tabs.Pages.Add(new TabPage { Text = "DOCUMENTATION", Content = DocumentationPage() });
                _tabs.Pages.Add(new TabPage { Text = "SETTINGS", Content = SettingsPage(p) });
            }
            if (sel >= 0 && sel < _tabs.Pages.Count) _tabs.SelectedIndex = sel;
        }

        // ------------------------------------------------------------------ helpers
        private static GridView Grid(string[] headers, IEnumerable<string[]> rows)
        {
            var g = new GridView { DataStore = rows.ToList(), AllowMultipleSelection = false };
            for (int i = 0; i < headers.Length; i++)
            {
                int col = i;
                g.Columns.Add(new GridColumn { HeaderText = headers[i], DataCell = new TextBoxCell { Binding = Binding.Delegate<string[], string>(x => x[col]) }, AutoSize = true });
            }
            return g;
        }

        private static Button Btn(string text, Action a) { var b = new Button { Text = text }; b.Click += (s, e) => { try { a(); } catch (Exception ex) { MessageBox.Show(ex.Message, "Rhino Wood"); } }; return b; }

        private static void Run(string cmd) => RhinoApp.RunScript("_" + cmd, true);

        private Control ProjectPage(WoodProject p, ProjectResult r)
        {
            var l = new DynamicLayout { Padding = new Padding(8), Spacing = new Size(6, 6) };
            l.AddRow(new Label { Text = p == null ? "No active project" : p.Name, Font = new Font(SystemFont.Bold, 11) });
            if (p != null && r != null)
            {
                l.AddRow(new Label { Text = "Type: " + p.Furniture.Name });
                l.AddRow(new Label { Text = "Species: " + p.SpeciesId + "   Project ID: " + p.Id });
                l.AddRow(new Label { Text = string.Format("Parts: {0}   Families: {1}   Joints: {2}   Hardware: {3}", r.Model.AllParts.Count(), r.Model.Families.Count, r.Model.Joints.Count, r.Model.HardwareInstalls.Count) });
                l.AddRow(new Label { Text = string.Format("Purchase: {0}", string.Join(", ", r.Optimization.Purchase.Select(x => x.Quantity + "x " + x.Item.Label + "x" + x.Length))) });
                l.AddRow(new Label { Text = string.Format("Total cost: {0:0.00} {1}", r.Cost.Total, r.Cost.Currency) });
                l.AddRow(new Label { Text = string.Format("Issues: {0} errors, {1} warnings", r.Issues.Count(i => i.Severity == Severity.Error), r.Issues.Count(i => i.Severity == Severity.Warning)) });
            }
            l.AddRow(Btn("New dining table...", () => Run("WoodNewTable")));
            l.AddRow(Btn("Open project file...", () => Run("WoodOpen")), Btn("Save project file...", () => Run("WoodSave")));
            l.AddRow(Btn("Recalculate", () => { if (RhinoDoc.ActiveDoc != null && P.Project != null) WoodActions.Refresh(RhinoDoc.ActiveDoc); }));
            l.Add(null);
            return l;
        }

        private Control FurniturePage(WoodProject p)
        {
            var l = new DynamicLayout { Padding = new Padding(8), Spacing = new Size(6, 6) };
            var advanced = new DynamicLayout { Spacing = new Size(6, 6) };
            foreach (var def in p.Furniture.Parameters)
            {
                var d = def;
                var ns = new NumericStepper { MinValue = d.Min, MaxValue = d.Max, DecimalPlaces = 0, Increment = 5, Value = p.Parameters[d.Key] };
                ns.Value = Math.Max(d.Min, Math.Min(d.Max, p.Parameters[d.Key]));
                ns.LostFocus += (s, e) =>
                {
                    if (Math.Abs(ns.Value - p.Parameters[d.Key]) < 1e-9) return;
                    try { p.SetParameter(d.Key, ns.Value); WoodActions.Refresh(RhinoDoc.ActiveDoc, false); }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Rhino Wood"); }
                };
                (d.Advanced ? advanced : l).AddRow(new Label { Text = d.Label + " (" + d.Unit + ")", ToolTip = d.Description }, ns);
            }
            l.AddRow(new Expander { Header = "Advanced parameters", Expanded = false, Content = advanced });
            foreach (var grp in p.Furniture.Choices.GroupBy(c => c.Group))
            {
                l.AddRow(new Label { Text = grp.Key.ToUpperInvariant(), Font = new Font(SystemFont.Bold) });
                foreach (var ch in grp)
                {
                    var choice = ch; var dd = new DropDown();
                    foreach (var o in choice.Options)
                        dd.Items.Add(new ListItem { Text = choice.Kind == "joint" ? p.Joints.Get(o).Name + "  (strength " + p.Joints.Get(o).Info.Strength + "/5)" : (p.Library.Hardware.TryGetValue(o, out var h) ? h.Model : o), Key = o });
                    dd.SelectedKey = p.Choices[choice.Key];
                    dd.SelectedKeyChanged += (s, e) =>
                    {
                        if (dd.SelectedKey == p.Choices[choice.Key]) return;
                        try { p.SetChoice(choice.Key, dd.SelectedKey); WoodActions.Refresh(RhinoDoc.ActiveDoc, true); }
                        catch (Exception ex) { MessageBox.Show(ex.Message, "Rhino Wood"); }
                    };
                    l.AddRow(new Label { Text = choice.Label, ToolTip = choice.Description }, dd);
                }
            }
            l.AddRow(Btn("Manual override...", () => Run("WoodOverride")));
            foreach (var o in p.Overrides)
            {
                var info = p.GetOverride(o.Key);
                l.AddRow(new Label { Text = string.Format("{0}: rule {1:0.#} {2} {3:0.#} = {4:0.#}", o.Key, info.Calculated, o.Value.Mode == RhinoWood.Core.Parametric.OverrideMode.Add ? "+" : "=>", o.Value.Value, info.Final) });
            }
            l.Add(null);
            return l;
        }

        private Control MaterialsPage(WoodProject p)
        {
            var l = new DynamicLayout { Padding = new Padding(8), Spacing = new Size(6, 6) };
            var dd = new DropDown();
            foreach (var s in p.Library.Species.Values.OrderBy(s => s.Name)) dd.Items.Add(new ListItem { Text = s.Name, Key = s.Id });
            dd.SelectedKey = p.SpeciesId;
            dd.SelectedKeyChanged += (s, e) => { if (dd.SelectedKey != p.SpeciesId) { p.SetSpecies(dd.SelectedKey); WoodActions.Refresh(RhinoDoc.ActiveDoc, false); } };
            l.AddRow(new Label { Text = "Species" }, dd);
            var sp = p.Library.GetSpecies(p.SpeciesId);
            l.AddRow(new Label { Text = string.Format(CultureInfo.InvariantCulture, "Density {0} kg/m3, Brinell perp {1}-{2} N/mm2, tangential movement {3:0.0000}/%MC, price {4:0}/m3 {5}", sp.DensityKgM3, sp.BrinellPerpMin, sp.BrinellPerpMax, sp.TangentialMovementPerPercent, sp.PricePerM3, sp.DataLabel) });
            l.AddRow(Btn("Add custom species...", () => Run("WoodAddSpecies")));
            l.Add(Grid(new[] { "Profile", "W x T", "Lengths" }, p.Library.StockFor(p.SpeciesId).Select(s => new[] { s.Id, s.Width + " x " + s.Thickness, string.Join(", ", s.Lengths) })));
            return l;
        }

        private Control EngineeringPage(WoodProject p, ProjectResult r)
        {
            var l = new DynamicLayout { Padding = new Padding(8), Spacing = new Size(6, 6) };
            var mode = new DropDown();
            foreach (var m in Enum.GetValues(typeof(DisplayMode))) mode.Items.Add(m.ToString());
            mode.SelectedKey = p.Settings.Display.ToString();
            mode.SelectedKeyChanged += (s, e) => { p.Settings.Display = (DisplayMode)Enum.Parse(typeof(DisplayMode), mode.SelectedKey); WoodActions.Refresh(RhinoDoc.ActiveDoc, false); };
            var grain = new CheckBox { Text = "Show grain direction", Checked = p.Settings.ShowGrain };
            grain.CheckedChanged += (s, e) => { p.Settings.ShowGrain = grain.Checked == true; WoodActions.Refresh(RhinoDoc.ActiveDoc, false); };
            l.AddRow(new Label { Text = "Display mode" }, mode); l.AddRow(grain);
            l.Add(Grid(new[] { "Severity", "Code", "Message" }, r.Issues.Select(i => new[] { i.Severity.ToString(), i.Code, i.Message })), yscale: true);
            return l;
        }

        private Control OptimizationPage(WoodProject p, ProjectResult r)
        {
            var o = r.Optimization;
            var l = new DynamicLayout { Padding = new Padding(8), Spacing = new Size(6, 6) };
            var strat = new DropDown();
            foreach (var s in Enum.GetValues(typeof(OptimizationStrategy))) strat.Items.Add(s.ToString());
            strat.SelectedKey = p.Settings.Strategy.ToString();
            strat.SelectedKeyChanged += (s, e) => { p.Settings.Strategy = (OptimizationStrategy)Enum.Parse(typeof(OptimizationStrategy), strat.SelectedKey); P.Recalculate(); };
            l.AddRow(new Label { Text = "Strategy" }, strat);
            l.Add(new TextArea { ReadOnly = true, Text = ReportBuilder.OptimizationText(r), Size = new Size(360, 220), Font = new Font(FontFamilies.Monospace, 8) });
            l.Add(new CutPlanView(o, r.Model.Families.Select(f => f.Id).ToList()) { Size = new Size(360, Math.Max(80, o.Boards.Count * 34 + 10)) });
            return new Scrollable { Content = l };
        }

        private Control ProcurementPage(ProjectResult r)
        {
            var l = new DynamicLayout { Padding = new Padding(8), Spacing = new Size(6, 6) };
            l.Add(Grid(new[] { "Line", "Material", "Length", "Qty", "Reserve", "Unit", "Total", "Remnant m3", "Waste m3" }, r.Optimization.Purchase.Select(x => new[] { x.Id, x.Item.Label, x.Length.ToString("0"), x.Quantity.ToString(), x.ReserveQuantity.ToString(), x.UnitPrice.ToString("0.00"), x.Total.ToString("0.00"), x.ExpectedUsableRemnantM3.ToString("0.0000"), x.ExpectedWasteM3.ToString("0.0000") })), yscale: true);
            l.AddRow(new Label { Text = string.Format("Material {0:0.00}  Hardware {1:0.00}  Consumables {2:0.00}  Labor {3:0.00}  TOTAL {4:0.00} {5}", r.Cost.RawMaterial, r.Cost.Hardware, r.Cost.Consumables, r.Cost.Labor, r.Cost.Total, r.Cost.Currency) });
            return l;
        }

        private Control DocumentationPage()
        {
            var l = new DynamicLayout { Padding = new Padding(8), Spacing = new Size(6, 6) };
            l.AddRow(new Label { Text = "Exports BOM, cut list, procurement list, manufacturing operations, traceability, joinery and hardware details, assembly sequence, drawings (SVG), exploded view and an HTML project summary." });
            l.AddRow(Btn("Fișă tehnică (previzualizare)", () => Run("WoodSheet")));
            l.AddRow(Btn("Exportă planșe PDF", () => Run("WoodPdf")));
            l.AddRow(Btn("Exportă toate documentele...", () => Run("WoodReport")));
            l.Add(null);
            return l;
        }

        private Control SettingsPage(WoodProject p)
        {
            var l = new DynamicLayout { Padding = new Padding(8), Spacing = new Size(6, 6) };
            l.AddRow(new Label { Text = string.Format("Reserve {0}%, kerf {1} mm, length allowance {2} mm, min remnant {3} mm, labor {4}/h {5}", p.Settings.GlobalReservePercent, p.Settings.Rules.SawKerf, p.Settings.Rules.LengthAllowance, p.Settings.Rules.MinReusableRemnant, p.Settings.Rules.LaborRatePerHour, p.Settings.Currency) });
            l.AddRow(Btn("Edit settings...", () => Run("WoodSettings")));
            l.Add(null);
            return l;
        }
    }

    /// <summary>Visual cut plan: each commercial board as a bar with parts, remnants and scrap.</summary>
    public sealed class CutPlanView : Drawable
    {
        private readonly OptimizationResult _o; private readonly List<string> _families;
        private static readonly Color[] Palette = { Color.FromArgb(192, 132, 90), Color.FromArgb(122, 158, 126), Color.FromArgb(111, 143, 181), Color.FromArgb(181, 143, 176), Color.FromArgb(209, 180, 92), Color.FromArgb(141, 141, 141) };
        public CutPlanView(OptimizationResult o, List<string> families) { _o = o; _families = families; }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_o.Boards.Count == 0) return;
            float w = Width - 20; double max = _o.Boards.Max(b => b.Length); float y = 4;
            foreach (var b in _o.Boards)
            {
                float k = (float)(w / max);
                e.Graphics.DrawText(SystemFonts.Default(7), Brushes.Black, 2, y, b.Id + " " + b.Item.Label + " x " + b.Length);
                float by = y + 12;
                e.Graphics.FillRectangle(Color.FromArgb(243, 236, 226), 4, by, (float)b.Length * k, 14);
                foreach (var c in b.Cuts)
                {
                    var col = Palette[Math.Max(0, _families.IndexOf(c.Demand.FamilyId)) % Palette.Length];
                    e.Graphics.FillRectangle(col, 4 + (float)c.Start * k, by, (float)c.Length * k, 14);
                    e.Graphics.DrawRectangle(Colors.Black, 4 + (float)c.Start * k, by, (float)c.Length * k, 14);
                }
                if (b.TailLength > 0) e.Graphics.FillRectangle(b.TailClass == RemnantClass.ProjectRemnant ? Color.FromArgb(200, 230, 190) : Colors.LightGrey, 4 + (float)(b.Length - b.TailLength) * k, by, (float)b.TailLength * k, 14);
                y += 34;
            }
        }
    }
}
