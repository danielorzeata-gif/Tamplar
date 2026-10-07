using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Eto.Drawing;
using Eto.Forms;
using Rhino;
using RhinoWood.Core.Domain;
using RhinoWood.Core.Furniture;
using RhinoWood.Core.Optimization;
using RhinoWood.Core.Projects;
using RhinoWood.Core.Reports;
using RhinoWood.Plugin.UI.Atelier;

namespace RhinoWood.Plugin.UI
{
    /// <summary>
    /// The Atelier panel: ONE docked panel (320 px), no windows. Header (title + DESIGN/VÂNZARE) → ELEMENT → DIMENSIUNI → VARIANTĂ →
    /// ÎMBINĂRI → VERIFICĂRI → DEBITARE (DESIGN) → COST / OFERTĂ → DOCUMENTE, and a footer with [Previzualizare] (secondary, left) and
    /// [Generează] (the single primary action, right). Business logic stays in RhinoWood.Core.
    /// </summary>
    [Guid("a7d2c1f4-93b0-4c1e-8f65-5b0f6d3e2a10")]
    public class WoodPanel : Panel
    {
        private static WoodPlugin P => WoodPlugin.Instance;
        private readonly AtSegmented _mode = new AtSegmented("DESIGN", "VÂNZARE");
        private bool _internal;
        private AtSection _verif, _debit, _cost;
        private Label _tierNote;
        private AtButton _previewBtn;

        public WoodPanel()
        {
            Tk.Init();
            BackgroundColor = Tk.Surface;
            MinimumSize = new Size(300, 240);
            _mode.SelectedChanged += (s, e) => Rebuild();
            P.ProjectChanged += (s, e) => { if (_internal) return; Application.Instance.AsyncInvoke(Rebuild); };
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

        private bool Sale => _mode.SelectedIndex == 1;
        private static void Run(string cmd) => RhinoApp.RunScript("_" + cmd, true);

        // ----------------------------------------------------------------------------------------- build
        private void Rebuild()
        {
            Tk.Init(); BackgroundColor = Tk.Surface;
            var p = P.Project;
            var header = new TableLayout
            {
                Padding = new Padding(12, 8, 12, 8), Spacing = new Size(8, 0), BackgroundColor = Tk.Surface,
                Rows = { new TableRow(new Label { Text = "Atelier", Font = Tk.PanelTitle, TextColor = Tk.Ink, VerticalAlignment = VerticalAlignment.Center }, new TableCell(null, true), _mode) }
            };

            var body = new StackLayout { Orientation = Orientation.Vertical, Spacing = 0, HorizontalContentAlignment = HorizontalAlignment.Stretch };
            if (p == null) body.Items.Add(new StackLayoutItem(EmptySection(), HorizontalAlignment.Stretch));
            else
            {
                body.Items.Add(new StackLayoutItem(ElementSection(p), HorizontalAlignment.Stretch));
                body.Items.Add(new StackLayoutItem(DimensionsSection(p), HorizontalAlignment.Stretch));
                body.Items.Add(new StackLayoutItem(TierSection(p), HorizontalAlignment.Stretch));
                body.Items.Add(new StackLayoutItem(JointsSection(p), HorizontalAlignment.Stretch));
                _verif = new AtSection("Verificări", null, true); body.Items.Add(new StackLayoutItem(_verif, HorizontalAlignment.Stretch));
                if (!Sale) { _debit = new AtSection("Debitare", "necesar lucrare", true); body.Items.Add(new StackLayoutItem(_debit, HorizontalAlignment.Stretch)); }
                else _debit = null;
                _cost = new AtSection(Sale ? "Ofertă" : "Cost", null, true); body.Items.Add(new StackLayoutItem(_cost, HorizontalAlignment.Stretch));
                body.Items.Add(new StackLayoutItem(DocumentsSection(), HorizontalAlignment.Stretch));
                UpdateResults();
            }

            _previewBtn = new AtButton(P.PreviewOn ? "Ascunde previzualizarea" : "Previzualizare", BtnVariant.Secondary);
            _previewBtn.Click += (s, e) => { P.PreviewOn = !P.PreviewOn; _previewBtn.Text = P.PreviewOn ? "Ascunde previzualizarea" : "Previzualizare"; PreviewService.Refresh(); };
            var gen = new AtButton(P.Generated ? "Actualizează" : "Generează", BtnVariant.Primary);
            gen.Click += (s, e) => { if (RhinoDoc.ActiveDoc != null && P.Project != null) { WoodActions.Generate(RhinoDoc.ActiveDoc); Application.Instance.AsyncInvoke(Rebuild); } };
            var footer = new TableLayout
            {
                Padding = new Padding(12, 8), Spacing = new Size(8, 0), BackgroundColor = Tk.Surface,
                Rows = { new TableRow(_previewBtn, new TableCell(null, true), gen) }
            };
            var version = new Label { Text = "Rhino Wood " + BuildInfo.Text, Font = Tk.Caption, TextColor = Tk.InkMuted };
            if (p == null) { _previewBtn.Enabled = false; gen.Enabled = false; }

            Content = new TableLayout
            {
                Rows =
                {
                    new TableRow(header),
                    new TableRow(new Scrollable { Content = body, Border = BorderType.None, ExpandContentWidth = true, BackgroundColor = Tk.Surface }) { ScaleHeight = true },
                    new TableRow(footer),
                    new TableRow(new StackLayout { Padding = new Padding(12, 0, 12, 6), Items = { version } })
                }
            };
        }

        private Control EmptySection()
        {
            var s = new AtSection("Element", "niciunul", true);
            s.Add(new Label { Text = "Niciun element activ. Creează o masă din lemn masiv; vei vedea previzualizarea în viewport înainte de generare.", Font = Tk.Label, TextColor = Tk.InkMuted, Wrap = WrapMode.Word });
            var b = new AtButton("Masă de sufragerie", BtnVariant.Secondary); b.Click += (s2, e) => Run("WoodNewTable");
            s.Add(b);
            return s;
        }

        private Control ElementSection(WoodProject p)
        {
            var s = new AtSection("Element", Ro.FurnitureName(p.Furniture.TypeId, p.Furniture.Name), true);
            s.Add(new Label { Text = "Mobilier › " + p.Furniture.Category.Replace("Tables", "Mese") + " › " + Ro.FurnitureName(p.Furniture.TypeId, p.Furniture.Name), Font = Tk.Label, TextColor = Tk.InkMuted });
            var dd = new DropDown { Font = Tk.Label, BackgroundColor = Tk.Raised, TextColor = Tk.Ink };
            foreach (var sp in p.Library.Species.Values.OrderBy(x => Ro.SpeciesName(x.Id, x.Name))) dd.Items.Add(new ListItem { Text = Ro.SpeciesName(sp.Id, sp.Name) + (sp.DataLabel == "[UNVERIFIED]" ? " (date neverificate)" : ""), Key = sp.Id });
            dd.SelectedKey = p.SpeciesId;
            dd.SelectedKeyChanged += (s2, e) => { if (dd.SelectedKey != p.SpeciesId) { p.SetSpecies(dd.SelectedKey); AfterEdit(); } };
            s.Add(Field("Esență", dd));
            return s;
        }

        private Control DimensionsSection(WoodProject p)
        {
            var main = p.Furniture.Parameters.Where(d => !d.Advanced).ToList(); var adv = p.Furniture.Parameters.Where(d => d.Advanced).ToList();
            var s = new AtSection("Dimensiuni", main.Count + " parametri", true);
            foreach (var d in main) s.Add(ParamControl(p, d));
            if (adv.Count > 0)
            {
                var a = new AtSection("Avansate", adv.Count + " parametri", false);
                foreach (var d in adv) a.Add(ParamControl(p, d));
                s.Add(a);
            }
            return s;
        }

        private Control ParamControl(WoodProject p, ParameterDef d)
        {
            var f = new AtParam(Ro.Param(d.Key, d.Label), (int)Math.Round(p.Parameters[d.Key]), (int)d.Min, (int)d.Max, d.Key == "topThickness" ? 5 : 10);
            f.Committed += (s, v) => { try { p.SetParameter(d.Key, v); AfterEdit(); } catch (Exception ex) { MessageBox.Show(ex.Message, "Rhino Wood"); } };
            return f;
        }

        private Control TierSection(WoodProject p)
        {
            var tiers = p.Furniture.Tiers;
            var s = new AtSection("Variantă", p.Tier, true);
            if (tiers.Count == 0) { s.Add(new Label { Text = "Acest element nu are variante.", Font = Tk.Label, TextColor = Tk.InkMuted }); return s; }
            var sel = new AtTierSelector(tiers.Select(t => t.Id).ToArray(), tiers.Select(t => t.Meta).ToArray(), p.Tier);
            sel.SelectedChanged += (s2, e) => { try { p.ApplyTier(sel.Selected); AfterEdit(); } catch (Exception ex) { MessageBox.Show(ex.Message, "Rhino Wood"); } Application.Instance.AsyncInvoke(Rebuild); };
            s.Add(sel);
            _tierNote = new Label { Font = Tk.Caption, TextColor = Tk.Warn };
            s.Add(_tierNote);
            return s;
        }

        private Control JointsSection(WoodProject p)
        {
            var s = new AtSection("Îmbinări", p.Furniture.Choices.Count + " alegeri", true);
            foreach (var ch in p.Furniture.Choices)
            {
                var choice = ch;
                var dd = new DropDown { Font = Tk.Label, BackgroundColor = Tk.Raised, TextColor = Tk.Ink };
                foreach (var o in choice.Options) dd.Items.Add(new ListItem { Text = OptionLabel(p, choice, o), Key = o });
                dd.SelectedKey = p.Choices[choice.Key];
                var note = new Label { Font = Tk.Caption, TextColor = Tk.InkMuted, Wrap = WrapMode.Word, Text = OptionNote(p, choice, dd.SelectedKey) };
                dd.SelectedKeyChanged += (s2, e) =>
                {
                    if (dd.SelectedKey == p.Choices[choice.Key]) return;
                    try { p.SetChoice(choice.Key, dd.SelectedKey); note.Text = OptionNote(p, choice, dd.SelectedKey); AfterEdit(); }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Rhino Wood"); }
                };
                s.Add(Field(Ro.Choice(choice.Key, choice.Label), dd));
                s.Add(note);
            }
            return s;
        }

        private static string OptionLabel(WoodProject p, ChoiceDef c, string id) =>
            c.Kind == "joint" ? Ro.Joint(p.Joints.Get(id)) : Ro.HardwareName(id, p.Library.Hardware.TryGetValue(id, out var h) ? h.Model : id);

        private static string OptionNote(WoodProject p, ChoiceDef c, string id)
        {
            if (c.Kind != "joint") return p.Library.Hardware.TryGetValue(id, out var h) ? "cursă " + h.TravelAllowance.ToString("0.#", CultureInfo.InvariantCulture) + " mm" : "";
            var i = p.Joints.Get(id).Info;
            return "rezistență " + Dots(i.Strength) + " · dificultate " + Dots(i.Difficulty) + " · " + (i.VisibleFromOutside ? "aparentă" : "ascunsă");
        }
        private static string Dots(int n) => new string('●', Math.Max(0, Math.Min(5, n))) + new string('○', 5 - Math.Max(0, Math.Min(5, n)));

        private Control DocumentsSection()
        {
            var s = new AtSection("Documente", "PDF", false);
            var a = new AtButton("Fișă tehnică (previzualizare)", BtnVariant.Quiet); a.Click += (x, e) => Run("WoodSheet");
            var b = new AtButton(Sale ? "Exportă oferta PDF" : "Exportă planșe PDF", BtnVariant.Secondary); b.Click += (x, e) => Run("WoodPdf");
            var c = new AtButton("Exportă toate documentele…", BtnVariant.Quiet); c.Click += (x, e) => Run("WoodReport");
            s.Add(a); s.Add(b); s.Add(c);
            return s;
        }

        private static Control Field(string label, Control c) =>
            new StackLayout { Orientation = Orientation.Vertical, Spacing = 4, HorizontalContentAlignment = HorizontalAlignment.Stretch, Items = { new Label { Text = label, Font = Tk.Label, TextColor = Tk.Ink }, c } };

        // ----------------------------------------------------------------------------------------- editing
        private void AfterEdit()
        {
            _internal = true;
            try
            {
                var doc = RhinoDoc.ActiveDoc;
                if (P.Generated && doc != null) WoodActions.Refresh(doc, false); else P.Recalculate();
            }
            finally { _internal = false; }
            UpdateResults();
        }

        /// <summary>Refreshes the result sections (checks, cutting list, cost) without rebuilding the inputs, so typing is never interrupted.</summary>
        private void UpdateResults()
        {
            var p = P.Project; var r = P.LastResult;
            if (p == null || r == null || _verif == null) return;
            if (_tierNote != null) _tierNote.Text = p.Tier != null && !p.TierMatches ? "Variantă modificată: alegi altceva decât presetarea " + p.Tier + "." : "";

            _verif.Clear();
            var issues = r.Issues.Where(i => i.Severity != Severity.Info).ToList();
            _verif.Meta = issues.Count == 0 ? "în regulă" : issues.Count + " de verificat";
            if (issues.Count == 0) _verif.Add(new Label { Text = "● Nicio problemă de construcție găsită.", Font = Tk.Label, TextColor = Tk.Ok });
            foreach (var i in issues.Take(8))
                _verif.Add(new Label { Text = (i.Severity == Severity.Error ? "○ " : "▲ ") + Ro.Issue(i), Font = Tk.Caption, TextColor = i.Severity == Severity.Error ? Tk.Danger : Tk.Warn, Wrap = WrapMode.Word });
            if (issues.Count > 8) _verif.Add(new Label { Text = "… încă " + (issues.Count - 8) + " (vezi planșele PDF).", Font = Tk.Caption, TextColor = Tk.InkMuted });

            if (_debit != null)
            {
                _debit.Clear();
                var rows = CutRows(r);
                _debit.Meta = rows.Count + " repere · " + r.Optimization.Purchase.Sum(x => x.Quantity) + " bare";
                var list = new AtCutList(rows);
                list.RowSelected += (s, row) => WoodActions.SelectPart(RhinoDoc.ActiveDoc, row.PartId);
                _debit.Add(list);
                _debit.Add(new Label { Text = "Deșeul este colorat roșu (aceeași culoare ca în viewport).", Font = Tk.Caption, TextColor = Tk.InkMuted });
            }

            _cost.Clear();
            var cur = r.Cost.Currency;
            if (Sale) { _cost.Meta = "preț client"; _cost.Add(AtPrice.Build(new List<(string, string)>(), "Preț", Money.Format(SheetBuilder.SalePrice(p, r), cur))); }
            else
            {
                _cost.Meta = "producție";
                _cost.Add(AtPrice.Build(new List<(string, string)>
                {
                    ("Material", Money.Format(r.Cost.RawMaterial, cur)),
                    ("Feronerie și mărunțișuri", Money.Format(r.Cost.Hardware + r.Cost.Consumables, cur)),
                    ("Manoperă", Money.Format(r.Cost.Labor, cur))
                }, "Cost producție", Money.Format(r.Cost.Total, cur)));
            }
        }

        private static List<AtCutList.Row> CutRows(ProjectResult r)
        {
            var groups = new Dictionary<string, AtCutList.Row>(); var boards = new Dictionary<string, List<string>>();
            foreach (var b in r.Optimization.Boards.Where(x => !x.IsReserve))
                for (int i = 0; i < b.Cuts.Count; i++)
                {
                    var c = b.Cuts[i]; var fam = r.Model.Families.First(f => f.Id == c.Demand.FamilyId);
                    string key = c.Demand.FamilyId + "|" + c.Length.ToString("0.#", CultureInfo.InvariantCulture) + "|" + c.Demand.SecA + "|" + c.Demand.SecB;
                    if (!groups.TryGetValue(key, out var row))
                    {
                        row = new AtCutList.Row { Name = Ro.Family(fam), Section = c.Demand.SecB.ToString("0") + "×" + c.Demand.SecA.ToString("0"), Length = c.Length.ToString("0"), PartId = c.Demand.PartId };
                        groups[key] = row; boards[key] = new List<string>();
                    }
                    row.Qty++;
                    if (!boards[key].Contains(b.Id)) boards[key].Add(b.Id);
                    if (i == b.Cuts.Count - 1 && b.TailClass == RemnantClass.Scrap) row.WasteMm += b.TailLength;
                }
            foreach (var kv in groups)
            {
                var bl = boards[kv.Key]; kv.Value.Source = bl[0] + (bl.Count > 1 ? " +" + (bl.Count - 1) : "");
                kv.Value.Waste = kv.Value.WasteMm > 0 ? kv.Value.WasteMm.ToString("0") : "–";
            }
            return groups.Values.ToList();
        }
    }
}
