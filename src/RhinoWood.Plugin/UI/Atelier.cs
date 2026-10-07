using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;

namespace RhinoWood.Plugin.UI.Atelier
{
    /// <summary>Atelier design tokens (light + dark). Dark follows the host theme (detected from the system control background).</summary>
    internal static class Tk
    {
        public static bool Dark { get; private set; }
        public static Color Surface, Raised, Sunken, Line, LineStrong, Ink, InkMuted, Accent, OnAccent, AccentSoft, Economa, Standard, Premium, OnTier, Waste, Ok, Warn, Danger, Focus;
        public static Font Label, LabelStrong, Caption, Section, Mono, PanelTitle, DimTotal;

        private static Color C(string h) => Color.Parse(h);

        public static void Init()
        {
            var bg = SystemColors.ControlBackground;
            Dark = 0.299 * bg.R + 0.587 * bg.G + 0.114 * bg.B < 0.45;
            if (!Dark)
            {
                Surface = C("#f3f1ed"); Raised = C("#ffffff"); Sunken = C("#e7e3dc"); Line = C("#d3cdc3"); LineStrong = C("#8c8376");
                Ink = C("#1f1b16"); InkMuted = C("#5f574c"); Accent = C("#9c5410"); OnAccent = C("#ffffff"); AccentSoft = C("#f2e3d1");
                Economa = C("#5f6b72"); Standard = C("#2e6e62"); Premium = C("#5a3a22"); OnTier = C("#ffffff");
                Waste = C("#c23a22"); Ok = C("#1f5f8f"); Warn = C("#8a5d00"); Danger = C("#b3261e"); Focus = C("#1f5f8f");
            }
            else
            {
                Surface = C("#232120"); Raised = C("#2c2a28"); Sunken = C("#1a1918"); Line = C("#3d3a37"); LineStrong = C("#77706a");
                Ink = C("#ece7e0"); InkMuted = C("#a8a097"); Accent = C("#e09a4f"); OnAccent = C("#1f1b16"); AccentSoft = C("#3d2e20");
                Economa = C("#9fb0b9"); Standard = C("#6fc0ad"); Premium = C("#c9a27a"); OnTier = C("#1a1918");
                Waste = C("#ff7a5c"); Ok = C("#7cc0e6"); Warn = C("#e8b84a"); Danger = C("#ff8a7a"); Focus = C("#7cc0e6");
            }
            // 12 px = 9 pt, 11 px = 8.25 pt, 13 px = 9.75 pt, 15 px = 11.25 pt
            Label = new Font("Segoe UI", 9f); LabelStrong = new Font("Segoe UI", 9f, FontStyle.Bold);
            Caption = new Font("Segoe UI", 8.25f); Section = new Font("Segoe UI", 8.25f, FontStyle.Bold);
            Mono = new Font("Consolas", 9f); PanelTitle = new Font("Segoe UI", 9.75f, FontStyle.Bold); DimTotal = new Font("Consolas", 11.25f, FontStyle.Bold);
        }

        public static Color TierColor(string tier) => tier == "ECONOMA" ? Economa : tier == "PREMIUM" ? Premium : Standard;
    }

    internal enum BtnVariant { Primary, Secondary, Quiet, Danger }

    /// <summary>Button, 24 px high. At most one Primary per panel.</summary>
    internal sealed class AtButton : Drawable
    {
        private string _text; private bool _hover, _down;
        public BtnVariant Variant { get; set; } = BtnVariant.Secondary;
        public event EventHandler Click;
        public string Text { get => _text; set { _text = value; Resize(); Invalidate(); } }

        public AtButton(string text, BtnVariant v = BtnVariant.Secondary, int minWidth = 0)
        {
            Variant = v; _text = text; CanFocus = true; Cursor = Cursors.Pointer; _minW = minWidth; Resize();
            MouseEnter += (s, e) => { _hover = true; Invalidate(); };
            MouseLeave += (s, e) => { _hover = false; _down = false; Invalidate(); };
            MouseDown += (s, e) => { if (Enabled) { _down = true; Focus(); Invalidate(); } };
            MouseUp += (s, e) => { var was = _down; _down = false; Invalidate(); if (was && _hover && Enabled) Click?.Invoke(this, EventArgs.Empty); };
            KeyDown += (s, e) => { if (Enabled && (e.Key == Keys.Enter || e.Key == Keys.Space)) { Click?.Invoke(this, EventArgs.Empty); e.Handled = true; } };
            GotFocus += (s, e) => Invalidate(); LostFocus += (s, e) => Invalidate();
        }
        private readonly int _minW;
        private void Resize()
        {
            var w = (int)Math.Ceiling(Tk.LabelStrong.MeasureString(_text ?? "").Width) + 24;
            Size = new Size(Math.Max(w, _minW), 24);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; var r = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
            Color fill = Variant == BtnVariant.Primary ? Tk.Accent : Variant == BtnVariant.Quiet ? Tk.Surface : Tk.Raised;
            Color txt = Variant == BtnVariant.Primary ? Tk.OnAccent : Variant == BtnVariant.Danger ? Tk.Danger : Tk.Ink;
            if (_hover && Variant != BtnVariant.Primary) fill = Tk.AccentSoft;
            if (_down) fill = Variant == BtnVariant.Primary ? Tk.Accent : Tk.Sunken;
            g.FillRectangle(fill, r);
            if (Variant != BtnVariant.Primary && Variant != BtnVariant.Quiet) g.DrawRectangle(Variant == BtnVariant.Danger ? Tk.Danger : Tk.LineStrong, r);
            if (HasFocus) g.DrawRectangle(new Pen(Tk.Focus, 2), new RectangleF(1, 1, Width - 2, Height - 2));
            var sz = g.MeasureString(Tk.LabelStrong, _text ?? "");
            g.DrawText(Tk.LabelStrong, Enabled ? txt : new Color(txt, 0.45f), (Width - sz.Width) / 2, (Height - sz.Height) / 2, _text ?? "");
        }
    }

    /// <summary>Segmented switch for 2-4 exclusive options (DESIGN / VÂNZARE).</summary>
    internal sealed class AtSegmented : Drawable
    {
        private readonly string[] _opts; private int _sel;
        public event EventHandler SelectedChanged;
        public int SelectedIndex { get => _sel; set { _sel = value; Invalidate(); } }
        public string Selected => _opts[_sel];
        public AtSegmented(params string[] options)
        {
            _opts = options; Cursor = Cursors.Pointer;
            Size = new Size(options.Sum(o => (int)Tk.LabelStrong.MeasureString(o).Width + 20) + 4, 24);
            MouseUp += (s, e) =>
            {
                float x = 2; for (int i = 0; i < _opts.Length; i++) { float w = Tk.LabelStrong.MeasureString(_opts[i]).Width + 20; if (e.Location.X >= x && e.Location.X < x + w) { if (_sel != i) { _sel = i; Invalidate(); SelectedChanged?.Invoke(this, EventArgs.Empty); } return; } x += w; }
            };
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.FillRectangle(Tk.Sunken, new RectangleF(0, 0, Width, Height)); g.DrawRectangle(Tk.LineStrong, new RectangleF(0.5f, 0.5f, Width - 1, Height - 1));
            float x = 2;
            for (int i = 0; i < _opts.Length; i++)
            {
                float w = Tk.LabelStrong.MeasureString(_opts[i]).Width + 20; bool on = i == _sel;
                if (on) g.FillRectangle(Tk.Accent, new RectangleF(x, 2, w, Height - 4));
                var sz = g.MeasureString(Tk.LabelStrong, _opts[i]);
                g.DrawText(on ? Tk.LabelStrong : Tk.Label, on ? Tk.OnAccent : Tk.InkMuted, x + (w - sz.Width) / 2, (Height - sz.Height) / 2, _opts[i]);
                x += w;
            }
        }
    }

    /// <summary>Collapsible group: header on surface-sunken with UPPERCASE title and a summary on the right.</summary>
    internal sealed class AtSection : Panel
    {
        private readonly Drawable _head; private readonly StackLayout _body = new StackLayout { Orientation = Orientation.Vertical, Spacing = 8, Padding = new Padding(12), HorizontalContentAlignment = HorizontalAlignment.Stretch };
        private bool _open; private readonly string _title; private string _meta;
        public string Meta { get => _meta; set { _meta = value; _head.Invalidate(); } }
        public bool Expanded { get => _open; set { _open = value; _body.Visible = value; _head.Invalidate(); } }

        public AtSection(string title, string meta = null, bool open = true)
        {
            _title = title.ToUpperInvariant(); _meta = meta; _open = open;
            _head = new Drawable { Height = 24, Cursor = Cursors.Pointer };
            _head.Paint += (s, e) =>
            {
                var g = e.Graphics; g.FillRectangle(Tk.Sunken, new RectangleF(0, 0, _head.Width, 24));
                g.DrawText(Tk.Section, Tk.Accent, 10, 5, _open ? "▾" : "▸");
                g.DrawText(Tk.Section, Tk.Ink, 24, 5, _title);
                if (!string.IsNullOrEmpty(_meta)) { var sz = g.MeasureString(Tk.Caption, _meta); g.DrawText(Tk.Caption, Tk.InkMuted, _head.Width - sz.Width - 10, 5, _meta); }
                g.DrawLine(Tk.Line, 0, 23.5f, _head.Width, 23.5f);
            };
            _head.MouseUp += (s, e) => Expanded = !_open;
            _body.Visible = open;
            Content = new StackLayout { Orientation = Orientation.Vertical, Spacing = 0, HorizontalContentAlignment = HorizontalAlignment.Stretch, Items = { _head, _body } };
        }
        public void Add(Control c) => _body.Items.Add(new StackLayoutItem(c, HorizontalAlignment.Stretch));
        public void Clear() => _body.Items.Clear();
    }

    /// <summary>Numeric parameter: label, −/+ buttons, monospaced right-aligned value, unit, limits hint and error text. Out-of-limit values are shown, not blocked.</summary>
    internal sealed class AtParam : Panel
    {
        private readonly TextBox _box = new TextBox { TextAlignment = TextAlignment.Right, Font = Tk.Mono, BackgroundColor = Tk.Raised, TextColor = Tk.Ink, Height = 24 };
        private readonly Label _msg = new Label { Font = Tk.Caption, TextColor = Tk.InkMuted };
        private readonly double _min, _max; private readonly int _step;
        public event EventHandler<int> Committed;
        public int Value { get; private set; }

        public AtParam(string label, int value, int min, int max, int step = 10, string unit = "mm", string hint = null)
        {
            _min = min; _max = max; _step = step; Value = value;
            _box.Text = value.ToString();
            var minus = new AtButton("−", BtnVariant.Secondary, 24); var plus = new AtButton("+", BtnVariant.Secondary, 24);
            minus.Click += (s, e) => Commit(Value - _step); plus.Click += (s, e) => Commit(Value + _step);
            _box.LostFocus += (s, e) => TryParse(); _box.KeyDown += (s, e) => { if (e.Key == Keys.Enter) { TryParse(); e.Handled = true; } };
            _msg.Text = hint ?? ("min " + min + " · max " + max + " " + unit);
            var row = new TableLayout { Spacing = new Size(4, 0), Rows = { new TableRow(minus, new TableCell(_box, true), new Label { Text = unit, Font = Tk.Label, TextColor = Tk.InkMuted, VerticalAlignment = VerticalAlignment.Center, Width = 28 }, plus) } };
            Content = new StackLayout { Orientation = Orientation.Vertical, Spacing = 4, HorizontalContentAlignment = HorizontalAlignment.Stretch, Items = { new Label { Text = label, Font = Tk.Label, TextColor = Tk.Ink }, row, _msg } };
        }

        private void TryParse() { if (int.TryParse(_box.Text.Trim(), out var v)) Commit(v); else { _msg.Text = "Introdu un număr întreg (mm)."; _msg.TextColor = Tk.Danger; } }

        private void Commit(int v)
        {
            _box.Text = v.ToString();
            if (v < _min || v > _max) { _msg.Text = "În afara limitelor: " + (int)_min + "–" + (int)_max + " mm"; _msg.TextColor = Tk.Danger; _box.TextColor = Tk.Danger; return; }
            _msg.Text = "min " + (int)_min + " · max " + (int)_max + " mm"; _msg.TextColor = Tk.InkMuted; _box.TextColor = Tk.Ink;
            if (v == Value) return;
            Value = v; Committed?.Invoke(this, v);
        }
    }

    /// <summary>Three execution variants, always in the order ECONOMA → STANDARD → PREMIUM, each with its colour AND its written name.</summary>
    internal sealed class AtTierSelector : Drawable
    {
        private readonly string[] _ids; private readonly string[] _meta; private int _sel;
        public event EventHandler SelectedChanged;
        public string Selected => _ids[_sel];
        public AtTierSelector(string[] ids, string[] meta, string selected)
        {
            _ids = ids; _meta = meta; _sel = Math.Max(0, Array.IndexOf(ids, selected)); Size = new Size(-1, 70); Cursor = Cursors.Pointer;
            MouseUp += (s, e) => { int i = (int)(e.Location.X / (Width / (float)_ids.Length)); i = Math.Max(0, Math.Min(_ids.Length - 1, i)); if (i != _sel) { _sel = i; Invalidate(); SelectedChanged?.Invoke(this, EventArgs.Empty); } };
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; float gap = 4, w = (Width - gap * (_ids.Length - 1)) / _ids.Length;
            for (int i = 0; i < _ids.Length; i++)
            {
                float x = i * (w + gap); bool on = i == _sel; var tc = Tk.TierColor(_ids[i]);
                g.FillRectangle(on ? tc : Tk.Raised, new RectangleF(x, 0, w, Height - 1));
                g.DrawRectangle(on ? tc : Tk.LineStrong, new RectangleF(x + 0.5f, 0.5f, w - 1, Height - 2));
                var txt = on ? Tk.OnTier : Tk.Ink; var muted = on ? Tk.OnTier : Tk.InkMuted;
                g.FillRectangle(on ? Tk.OnTier : tc, new RectangleF(x + 6, 9, 7, 7));
                g.DrawText(Tk.LabelStrong, txt, x + 17, 5, _ids[i]);
                float y = 22;
                foreach (var line in (_meta[i] ?? "").Split('·').Select(t => t.Trim()))
                { g.DrawText(Tk.Caption, muted, x + 6, y, line); y += 14; }
            }
        }
    }

    /// <summary>Cutting list: Piesă, Secț., L, Buc, Bară, Deșeu (red). Rows are 22 px; click selects the part in the viewport.</summary>
    internal sealed class AtCutList : Drawable
    {
        public sealed class Row { public string Name, Section, Length, Source, Waste, PartId; public int Qty; public double WasteMm; }
        private readonly List<Row> _rows; private int _sel = -1;
        public event EventHandler<Row> RowSelected;
        public AtCutList(List<Row> rows) { _rows = rows; Size = new Size(-1, (rows.Count + 2) * 22); Cursor = Cursors.Pointer; MouseUp += (s, e) => { int i = (int)(e.Location.Y / 22) - 1; if (i >= 0 && i < _rows.Count) { _sel = i; Invalidate(); RowSelected?.Invoke(this, _rows[i]); } }; }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; float W = Width;
            float[] cx = { 6, W * 0.30f, W * 0.50f, W * 0.62f, W * 0.82f }; // left edges; numeric columns are right-aligned to the next edge
            void Cell(string t, float left, float right, Font f, Color c, bool rightAlign)
            { var sz = g.MeasureString(f, t); g.DrawText(f, c, rightAlign ? right - sz.Width - 4 : left, 3, t); }
            g.FillRectangle(Tk.Raised, new RectangleF(0, 0, W, Height)); g.DrawRectangle(Tk.Line, new RectangleF(0.5f, 0.5f, W - 1, Height - 1));
            var head = new[] { "Piesă", "Secț.", "L", "Buc", "Bară", "Deșeu" };
            g.FillRectangle(Tk.Sunken, new RectangleF(0, 0, W, 22));
            float[] lefts = { 6, W * 0.27f, W * 0.52f, W * 0.62f, W * 0.72f, W * 0.86f }; float[] rights = { W * 0.27f, W * 0.52f, W * 0.62f, W * 0.72f, W * 0.86f, W };
            for (int c = 0; c < 6; c++) { var sz = g.MeasureString(Tk.Caption, head[c]); g.DrawText(Tk.Caption, Tk.InkMuted, (c >= 1 && c <= 3) || c == 5 ? rights[c] - sz.Width - 4 : lefts[c], 4, head[c]); }
            for (int i = 0; i < _rows.Count; i++)
            {
                float y = (i + 1) * 22; var r = _rows[i];
                if (i == _sel) g.FillRectangle(Tk.AccentSoft, new RectangleF(0, y, W, 22));
                g.DrawLine(Tk.Line, 0, y, W, y);
                var cells = new[] { r.Name, r.Section, r.Length, r.Qty.ToString(), r.Source, r.Waste };
                for (int c = 0; c < 6; c++)
                {
                    bool num = (c >= 1 && c <= 3) || c == 5; var f = c == 0 ? Tk.Label : Tk.Mono; var col = c == 5 && r.WasteMm > 0 ? Tk.Waste : Tk.Ink;
                    var sz = g.MeasureString(f, cells[c] ?? ""); g.DrawText(f, col, num ? rights[c] - sz.Width - 4 : lefts[c], y + 3, cells[c] ?? "");
                }
            }
            float ty = (_rows.Count + 1) * 22; g.FillRectangle(Tk.Sunken, new RectangleF(0, ty, W, 22)); g.DrawLine(Tk.LineStrong, 0, ty, W, ty);
            g.DrawText(Tk.LabelStrong, Tk.Ink, 6, ty + 3, _rows.Sum(x => 1) + " repere");
            var q = _rows.Sum(x => x.Qty).ToString(); var qs = g.MeasureString(Tk.Mono, q); g.DrawText(Tk.Mono, Tk.Ink, rights[3] - qs.Width - 4, ty + 3, q);
            var wt = ((int)_rows.Sum(x => x.WasteMm)).ToString(); var ws = g.MeasureString(Tk.Mono, wt); g.DrawText(Tk.Mono, Tk.Waste, W - ws.Width - 4, ty + 3, wt);
        }
    }

    /// <summary>Cost or price lines with a separated total in monospaced digits.</summary>
    internal static class AtPrice
    {
        public static Control Build(IList<(string label, string value)> lines, string totalLabel, string total)
        {
            var rows = new List<TableRow>();
            foreach (var (l, v) in lines) rows.Add(new TableRow(new Label { Text = l, Font = Tk.Label, TextColor = Tk.InkMuted }, new TableCell(new Label { Text = v, Font = Tk.Mono, TextColor = Tk.Ink, TextAlignment = TextAlignment.Right }, true)));
            var line = new Drawable { Height = 1 }; line.Paint += (s, e) => e.Graphics.DrawLine(Tk.LineStrong, 0, 0.5f, line.Width, 0.5f);
            rows.Add(new TableRow(new Label { Text = totalLabel, Font = Tk.LabelStrong, TextColor = Tk.Ink, VerticalAlignment = VerticalAlignment.Center }, new TableCell(new Label { Text = total, Font = Tk.DimTotal, TextColor = Tk.Ink, TextAlignment = TextAlignment.Right }, true)));
            return new StackLayout { Orientation = Orientation.Vertical, Spacing = 4, HorizontalContentAlignment = HorizontalAlignment.Stretch, Items = { new TableLayout { Spacing = new Size(8, 2), Rows = { } }.With(rows.Take(rows.Count - 1)), line, new TableLayout { Spacing = new Size(8, 0), Rows = { rows.Last() } } } };
        }
        private static TableLayout With(this TableLayout t, IEnumerable<TableRow> rows) { foreach (var r in rows) t.Rows.Add(r); return t; }
    }
}
