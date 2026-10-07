using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using Rhino;
using RhinoWood.Core.Domain;
using RhinoWood.Core.Optimization;
using RhinoWood.Core.Parametric;
using RhinoWood.Core.Projects;
using RhinoWood.Core.Rules;
using RhinoWood.Core.Reports;
using RhinoWood.Core.Validation;
using RhinoWood.Plugin.UI.Atelier;

namespace RhinoWood.Plugin.UI
{
    /// <summary>Base of an independent WoodStart tab: scrollable Atelier-styled stack that is rebuilt when the project changes.</summary>
    internal abstract class TabBase : Scrollable
    {
        protected static WoodPlugin P => WoodPlugin.Instance;
        protected static RhinoWood.Core.Projects.WoodProject Prj => P?.Project;
        private readonly StackLayout _stack = new StackLayout { Orientation = Orientation.Vertical, Spacing = 8, Padding = new Padding(12), HorizontalContentAlignment = HorizontalAlignment.Stretch };
        private bool _building;

        protected TabBase() { Tk.Init(); BackgroundColor = Tk.Surface; Border = BorderType.None; Content = _stack; }

        /// <summary>Fills the stack (called on every project change).</summary>
        protected abstract void Build();

        public void Rebuild()
        {
            if (_building) return;
            _building = true;
            try { _stack.Items.Clear(); Build(); }
            catch (Exception ex) { _stack.Items.Clear(); Muted("Eroare la afișare: " + ex.Message); }
            finally { _building = false; }
        }

        protected void Add(Control c) => _stack.Items.Add(new StackLayoutItem(c, HorizontalAlignment.Stretch));
        protected void Title(string t) => Add(new Label { Text = t, Font = Tk.PanelTitle, TextColor = Tk.Ink });
        protected void Heading(string t) => Add(new Label { Text = t.ToUpperInvariant(), Font = Tk.Section, TextColor = Tk.InkMuted });
        protected void Text(string t) => Add(new Label { Text = t, Font = Tk.Label, TextColor = Tk.Ink, Wrap = WrapMode.Word });
        protected void Muted(string t) => Add(new Label { Text = t, Font = Tk.Caption, TextColor = Tk.InkMuted, Wrap = WrapMode.Word });
        protected void Mono(string t) => Add(new Label { Text = t, Font = Tk.Mono, TextColor = Tk.Ink, Wrap = WrapMode.None });
        protected void Row(params Control[] cs) => Add(new StackLayout { Orientation = Orientation.Horizontal, Spacing = 6, Items = { } }.With(cs));
        protected bool NeedProject() { if (Prj != null) return false; Muted("Nu există un proiect activ. Creează unul în tabul Configurare sau deschide un proiect în tabul Proiect."); return true; }
        protected static string Money(double v) => v.ToString("0.00", CultureInfo.InvariantCulture) + " " + (Prj?.Settings.Currency ?? "");
        protected static string Num(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);
        protected static bool TryNum(string s, out double v) => double.TryParse((s ?? "").Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out v);

        protected AtButton Button(string text, Action a, BtnVariant v = BtnVariant.Secondary) { var b = new AtButton(text, v); b.Click += (s, e) => a(); return b; }
        protected static void Refresh() { var doc = RhinoDoc.ActiveDoc; if (doc != null) WoodActions.Refresh(doc, false); else P.Recalculate(); }
        protected TextBox Field(string value, int width = 90) => new TextBox { Text = value, Width = width, Font = Tk.Label };
        protected Control Labeled(string label, Control c, string unit = null)
        {
            var row = new StackLayout { Orientation = Orientation.Horizontal, Spacing = 8, VerticalContentAlignment = VerticalAlignment.Center };
            row.Items.Add(new StackLayoutItem(new Label { Text = label, Font = Tk.Label, TextColor = Tk.Ink, Width = 190 }));
            row.Items.Add(new StackLayoutItem(c));
            if (unit != null) row.Items.Add(new StackLayoutItem(new Label { Text = unit, Font = Tk.Caption, TextColor = Tk.InkMuted }));
            return row;
        }
    }

    internal static class StackExt
    {
        public static StackLayout With(this StackLayout s, params Control[] cs) { foreach (var c in cs) s.Items.Add(new StackLayoutItem(c)); return s; }
    }

    // ------------------------------------------------------------------------------------------------ Proiect (WoodNewTable/Open/Save)
    internal sealed class ProjectTab : TabBase
    {
        public ProjectTab() { Rebuild(); }
        protected override void Build()
        {
            Title("Proiect");
            if (Prj == null) Muted("Nu există un proiect activ.");
            else
            {
                Text(Prj.Name + " · " + Ro.FurnitureName(Prj.Furniture.TypeId, Prj.Furniture.Name));
                Muted((P.Generated ? "Geometria este generată în document." : "Ciornă: doar previzualizare în viewport (apasă Generează în Configurare)."));
            }
            var types = WoodActions.FurnitureTypes().ToList();
            var dd = new DropDown { Font = Tk.Label };
            foreach (var ty in types) dd.Items.Add(new ListItem { Text = ty.name, Key = ty.id });
            dd.SelectedIndex = 0;
            Add(Labeled("Piesă nouă", dd));
            Row(Button("Creează ciornă", () => WoodActions.NewProject(dd.SelectedKey), BtnVariant.Primary), Button("Deschide…", Open), Button("Salvează…", Save));
        }

        private void Open()
        {
            var dlg = new OpenFileDialog { Title = "Deschide proiect Rhino Wood" };
            dlg.Filters.Add(new FileFilter("Rhino Wood", ".json"));
            if (dlg.ShowDialog(this) != DialogResult.Ok) return;
            try
            {
                var (p, ok) = ProjectSerializer.Open(File.ReadAllText(dlg.FileName), P.Library);
                P.SetProject(p, RhinoDoc.ActiveDoc != null);
                if (RhinoDoc.ActiveDoc != null) WoodActions.Refresh(RhinoDoc.ActiveDoc, false);
                RhinoApp.WriteLine(ok ? "Rhino Wood: proiect redeschis; planul verificat." : "Rhino Wood: proiect redeschis; planul diferă de cel salvat (reguli/bibliotecă schimbate).");
            }
            catch (Exception ex) { MessageBox.Show(this, "Proiectul nu a putut fi deschis: " + ex.Message, MessageBoxType.Error); }
        }

        private void Save()
        {
            if (Prj == null) return;
            var doc = RhinoDoc.ActiveDoc; if (doc != null) P.SaveToDocument(doc);
            var dlg = new SaveFileDialog { Title = "Salvează proiectul", FileName = Prj.Name + ".rhinowood.json" };
            if (dlg.ShowDialog(this) == DialogResult.Ok)
            { File.WriteAllText(dlg.FileName, ProjectSerializer.Serialize(Prj, P.LastResult)); RhinoApp.WriteLine("Rhino Wood: salvat " + dlg.FileName); }
        }
    }

    // ------------------------------------------------------------------------------------------------ Afișare (WoodDisplay)
    internal sealed class DisplayTab : TabBase
    {
        public DisplayTab() { Rebuild(); }
        protected override void Build()
        {
            Title("Afișare");
            if (NeedProject()) return;
            var modes = Enum.GetValues(typeof(DisplayMode)).Cast<DisplayMode>().ToArray();
            var seg = new AtSegmented(modes.Select(Ro.DisplayMode).ToArray()) { SelectedIndex = Array.IndexOf(modes, Prj.Settings.Display) };
            var info = new Label { Text = Ro.DisplayModeInfo(Prj.Settings.Display), Font = Tk.Caption, TextColor = Tk.InkMuted, Wrap = WrapMode.Word };
            seg.SelectedChanged += (s, e) => { Prj.Settings.Display = modes[seg.SelectedIndex]; info.Text = Ro.DisplayModeInfo(Prj.Settings.Display); Refresh(); P.Raise(); };
            Add(seg); Add(info);
            var grain = new CheckBox { Text = "Arată direcția fibrei", Checked = Prj.Settings.ShowGrain, Font = Tk.Label, TextColor = Tk.Ink };
            grain.CheckedChanged += (s, e) => { Prj.Settings.ShowGrain = grain.Checked == true; Refresh(); P.Raise(); };
            Add(grain);
        }
    }

    // ------------------------------------------------------------------------------------------------ Optimizare (WoodOptimize)
    internal sealed class OptimizeTab : TabBase
    {
        public OptimizeTab() { Rebuild(); }
        protected override void Build()
        {
            Title("Optimizare materiale");
            if (NeedProject()) return;
            var strategies = Enum.GetValues(typeof(OptimizationStrategy)).Cast<OptimizationStrategy>().ToArray();
            var dd = new DropDown { Font = Tk.Label };
            foreach (var s in strategies) dd.Items.Add(Ro.Strategy(s));
            dd.SelectedIndex = Array.IndexOf(strategies, Prj.Settings.Strategy);
            dd.SelectedIndexChanged += (s, e) => { Prj.Settings.Strategy = strategies[dd.SelectedIndex]; Refresh(); };
            Add(Labeled("Strategie", dd));
            var r = P.LastResult; if (r == null) return;
            var o = r.Optimization;
            Heading("Rezultat");
            Mono(string.Format(CultureInfo.InvariantCulture, "Necesar piese   {0:0.000} m³\nCumpărat        {1:0.000} m³\nCost material   {2}",
                o.OptimizedManufacturingM3, o.PurchasedM3, Money(r.Cost.RawMaterial)));
            Heading("De ce acest plan");
            foreach (var line in SheetBuilder.OrderExplanation(Prj, r)) Muted(line);
            Heading("Lista de comandă");
            foreach (var l in o.Purchase) Mono(string.Format(CultureInfo.InvariantCulture, "{0,2} × {1} × {2:0} mm{3}", l.Quantity, l.Item.Label, l.Length, l.ReserveQuantity > 0 ? " (din care rezervă " + l.ReserveQuantity + ")" : ""));
        }
    }

    // ------------------------------------------------------------------------------------------------ Suprascrieri (WoodOverride)
    internal sealed class OverrideTab : TabBase
    {
        public OverrideTab() { Rebuild(); }
        protected override void Build()
        {
            Title("Suprascrieri");
            if (NeedProject()) return;
            Muted("Regula rămâne activă dedesubt. „Adaugă” modifică valoarea calculată cu un decalaj; poți reveni oricând la valoarea calculată.");
            foreach (var node in Prj.Furniture.OverridableNodes)
            {
                var info = Prj.GetOverride(node);
                var box = Field("0", 70);
                var has = info.Override != null;
                if (has) box.Text = Num(info.Override.Value);
                Add(Labeled(Ro.OverrideNode(node), box, "mm"));
                Muted(string.Format(CultureInfo.InvariantCulture, "calculat {0:0.##} · final {1:0.##}{2}", info.Calculated, info.Final, has ? " · suprascris manual" : ""));
                var n = node;
                Row(Button("Aplică decalaj", () => { if (TryNum(box.Text, out var v)) { Prj.SetOverride(n, OverrideMode.Add, v); Refresh(); } }),
                    Button("Revino la calculat", () => { Prj.ResolveOverride(n, OverrideResolution.Recalculate); Refresh(); }),
                    Button("Convertește în piesă personalizată", () => { Prj.ResolveOverride(n, OverrideResolution.ConvertToCustomComponent); Refresh(); }, BtnVariant.Quiet));
            }
        }
    }

    // ------------------------------------------------------------------------------------------------ Documente (WoodPdf / WoodReport)
    internal sealed class DocumentsTab : TabBase
    {
        private readonly Label _msg = new Label { Font = Tk.Caption, TextColor = Tk.InkMuted, Wrap = WrapMode.Word };
        public DocumentsTab() { Rebuild(); }
        protected override void Build()
        {
            Title("Documente");
            if (NeedProject()) return;
            Muted("Fișa tehnică este în tabul „Fișă tehnică”. Restul planșelor (cote de îmbinare, plan de debitare, note de montaj) se exportă într-un singur PDF.");
            Row(Button("PDF Design", () => _msg.Text = WoodActions.ExportPdf(SheetMode.Design), BtnVariant.Primary),
                Button("PDF Vânzare (ofertă)", () => _msg.Text = WoodActions.ExportPdf(SheetMode.Sale)));
            Row(Button("Exportă piese de debitare (3D)", () => _msg.Text = WoodActions.ExportCutParts()));
            Muted("Piesele se așază plat lângă model, în layerele Debitare: 01 piesă brută · 02 după rindeluire · 03 piesă cu găuri + deșeu roșu.");
            Row(Button("Exportă documentația (CSV/HTML)…", ExportReport));
            Add(_msg);
        }

        private void ExportReport()
        {
            var dlg = new SelectFolderDialog { Title = "Exportă documentația în…" };
            if (dlg.ShowDialog(this) != DialogResult.Ok) return;
            var files = ReportWriter.WriteAll(dlg.Directory, Prj, P.Recalculate());
            _msg.Text = files.Count + " documente scrise în " + dlg.Directory;
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Path.Combine(dlg.Directory, "project_summary.html")) { UseShellExecute = true }); } catch { }
        }
    }

    // ------------------------------------------------------------------------------------------------ Verificări (WoodValidate) + catalog reguli
    internal sealed class ValidateTab : TabBase
    {
        public ValidateTab() { Rebuild(); }
        protected override void Build()
        {
            Title("Verificări");
            if (NeedProject()) { RuleTable(); return; }
            var issues = P.LastResult?.Issues ?? new List<Issue>();
            Muted(string.Format("{0} erori · {1} avertismente · {2} informări",
                issues.Count(i => i.Severity == Severity.Error), issues.Count(i => i.Severity == Severity.Warning), issues.Count(i => i.Severity == Severity.Info)));
            foreach (var i in issues.OrderBy(i => i.Severity == Severity.Error ? 0 : i.Severity == Severity.Warning ? 1 : 2))
            {
                var col = i.Severity == Severity.Error ? Tk.Danger : i.Severity == Severity.Warning ? Tk.Warn : Tk.InkMuted;
                var tag = i.Severity == Severity.Error ? "EROARE" : i.Severity == Severity.Warning ? "AVERTISMENT" : "INFO";
                Add(new Label { Text = tag + " · " + Ro.Issue(i), Font = Tk.Label, TextColor = col, Wrap = WrapMode.Word });
            }
            var holder = new StackLayout { Orientation = Orientation.Vertical, Spacing = 6, HorizontalContentAlignment = HorizontalAlignment.Stretch };
            Remedies.AddTo(new RemedyHost(holder), Prj, P.LastResult, null);
            Add(holder);
            RuleTable();
        }

        private void RuleTable()
        {
            Heading("Catalog de reguli (R1–R19)");
            foreach (var r in RuleCatalog.All)
                Muted(r.Id + " · " + r.Title + " — " + r.Confidence + " " + r.Label);
        }
    }

    // ------------------------------------------------------------------------------------------------ Setări (WoodSettings)
    internal sealed class SettingsTab : TabBase
    {
        public SettingsTab() { Rebuild(); }
        protected override void Build()
        {
            Title("Setări proiect");
            if (NeedProject()) return;
            var s = Prj.Settings;
            var reserve = Field(Num(s.GlobalReservePercent)); var kerf = Field(Num(s.Rules.SawKerf)); var allow = Field(Num(s.Rules.LengthAllowance));
            var minRem = Field(Num(s.Rules.MinReusableRemnant)); var labor = Field(Num(s.Rules.LaborRatePerHour)); var margin = Field(Num(s.SalesMarginPercent));
            var cur = Field(s.Currency, 60);
            Add(Labeled("Rezervă globală (aplicată o dată)", reserve, "%"));
            Add(Labeled("Grosime tăietură (kerf)", kerf, "mm"));
            Add(Labeled("Adaos lungime", allow, "mm"));
            Add(Labeled("Rest minim reutilizabil", minRem, "mm"));
            Add(Labeled("Tarif manoperă", labor, "/oră"));
            Add(Labeled("Marjă vânzare", margin, "%"));
            Add(Labeled("Monedă", cur));
            Add(Button("Aplică", () =>
            {
                if (TryNum(reserve.Text, out var a)) s.GlobalReservePercent = a;
                if (TryNum(kerf.Text, out var b)) s.Rules.SawKerf = b;
                if (TryNum(allow.Text, out var c)) s.Rules.LengthAllowance = c;
                if (TryNum(minRem.Text, out var d)) s.Rules.MinReusableRemnant = d;
                if (TryNum(labor.Text, out var e)) s.Rules.LaborRatePerHour = e;
                if (TryNum(margin.Text, out var f)) s.SalesMarginPercent = f;
                if (!string.IsNullOrWhiteSpace(cur.Text)) s.Currency = cur.Text.Trim();
                Prj.Rebuild(); Refresh();
            }, BtnVariant.Primary));
        }
    }

    // ------------------------------------------------------------------------------------------------ Materiale (WoodAddSpecies)
    internal sealed class MaterialsTab : TabBase
    {
        private readonly Label _msg = new Label { Font = Tk.Caption, TextColor = Tk.InkMuted, Wrap = WrapMode.Word };
        public MaterialsTab() { Rebuild(); }
        protected override void Build()
        {
            Title("Materiale");
            Heading("Esențe în bibliotecă");
            foreach (var sp in P.Library.Species.Values.OrderBy(x => x.Name))
                Mono(string.Format(CultureInfo.InvariantCulture, "{0,-16} {1,5:0} kg/m³  {2,8:0.00}/m³  {3}", sp.Name, sp.DensityKgM3, sp.PricePerM3, sp.IsUserDefined ? "(utilizator)" : sp.DataLabel));
            Heading("Adaugă esență");
            var name = Field("", 160); var price = Field("1500"); var dens = Field("650"); var tang = Field("0.0040");
            Add(Labeled("Nume", name)); Add(Labeled("Preț", price, "/m³")); Add(Labeled("Densitate", dens, "kg/m³"));
            Add(Labeled("Mișcare tangențială", tang, "fracție / 1% UM"));
            Add(Button("Adaugă", () =>
            {
                if (TryNum(price.Text, out var p) && TryNum(dens.Text, out var d) && TryNum(tang.Text, out var t)) { _msg.Text = WoodActions.AddSpecies(name.Text, p, d, t); Rebuild(); Add(_msg); }
                else _msg.Text = "Valori numerice invalide.";
            }, BtnVariant.Primary));
            Add(_msg);
        }
    }

    // ------------------------------------------------------------------------------------------------ Despre (WoodAbout)
    internal sealed class AboutTab : TabBase
    {
        public AboutTab() { Rebuild(); }
        protected override void Build()
        {
            Title("Despre Rhino Wood");
            Text(BuildInfo.Text);
            Muted("Fișier încărcat: " + BuildInfo.Path);
            Muted("Dacă versiunea sau calea nu corespund cu ce ai instalat, închide Rhino și rulează din nou scripts\\install.ps1.");
        }
    }
}
