using System;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using RhinoWood.Core.Projects;
using RhinoWood.Core.Reports;
using RhinoWood.Plugin.UI.Atelier;

namespace RhinoWood.Plugin.UI
{
    /// <summary>Anything that can hold remedy rows (an AtSection or a plain stack).</summary>
    internal interface IRemedyHost { void Add(Control c); }
    internal sealed class RemedyHost : IRemedyHost
    {
        private readonly StackLayout _s; public RemedyHost(StackLayout s) { _s = s; }
        public void Add(Control c) => _s.Items.Add(new StackLayoutItem(c, HorizontalAlignment.Stretch));
    }

    /// <summary>"Aplică soluția": a warning with a fix gets a recommended solution (best for the active optimization strategy) and alternatives.</summary>
    internal static class Remedies
    {
        public static void AddTo(AtSection s, RhinoWood.Core.Projects.WoodProject p, RhinoWood.Core.Projects.ProjectResult r, Action after) => AddTo(new SectionHost(s), p, r, after);
        public static void AddTo(RemedyHost h, RhinoWood.Core.Projects.WoodProject p, RhinoWood.Core.Projects.ProjectResult r, Action after) => AddTo((IRemedyHost)h, p, r, after);

        private sealed class SectionHost : IRemedyHost { private readonly AtSection _s; public SectionHost(AtSection s) { _s = s; } public void Add(Control c) => _s.Add(c); }

        private static void AddTo(IRemedyHost host, RhinoWood.Core.Projects.WoodProject p, RhinoWood.Core.Projects.ProjectResult r, Action after)
        {
            if (p == null || r == null) return;
            System.Collections.Generic.List<Remedy> fixes;
            try { fixes = RemedyEngine.Suggest(p, r); } catch (Exception ex) { host.Add(new Label { Text = "Soluțiile nu au putut fi calculate: " + ex.Message, Font = Tk.Caption, TextColor = Tk.InkMuted }); return; }
            if (fixes.Count == 0) return;
            host.Add(new Label { Text = "SOLUȚII (ordonate după „" + Ro.Strategy(p.Settings.Strategy) + "”)", Font = Tk.Section, TextColor = Tk.InkMuted });
            foreach (var f in fixes.Take(4))
            {
                host.Add(new Label { Text = (f.Recommended ? "★ Recomandat: " : "") + f.Title, Font = f.Recommended ? Tk.LabelStrong : Tk.Label, TextColor = Tk.Ink, Wrap = WrapMode.Word });
                host.Add(new Label { Text = f.Detail, Font = Tk.Caption, TextColor = Tk.InkMuted, Wrap = WrapMode.Word });
                var b = new AtButton(f.Recommended ? "Aplică soluția recomandată" : "Aplică această soluție", f.Recommended ? BtnVariant.Primary : BtnVariant.Secondary);
                var fix = f;
                b.Click += (x, e) => { WoodActions.ApplyRemedy(fix); after?.Invoke(); };
                host.Add(b);
            }
        }
    }
}
