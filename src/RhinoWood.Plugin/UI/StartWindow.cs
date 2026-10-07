using System;
using Eto.Drawing;
using Eto.Forms;
using RhinoWood.Plugin.UI.Atelier;

namespace RhinoWood.Plugin.UI
{
    /// <summary>
    /// WoodStart: one window with independent tabs. First tab = "Configurare" (the Atelier panel); the others are the former
    /// command-line commands (Proiect, Afișare, Optimizare, Suprascrieri, Fișă tehnică, Documente, Verificări, Setări, Materiale, Despre).
    /// The commands keep working from the command line as well.
    /// </summary>
    public sealed class StartWindow : Form
    {
        private static StartWindow _instance;
        private static WoodPlugin P => WoodPlugin.Instance;
        private readonly EventHandler _changed;

        public static void Open()
        {
            if (_instance != null && !_instance.IsDisposed) { _instance.Show(); _instance.Focus(); return; }
            _instance = new StartWindow();
            _instance.Show();
        }

        private StartWindow()
        {
            Tk.Init();
            Title = "Rhino Wood · Atelier";
            ClientSize = new Size(560, 820);
            MinimumSize = new Size(480, 520);
            BackgroundColor = Tk.Surface;
            try { Owner = Rhino.UI.RhinoEtoApp.MainWindow; } catch { }

            var tabs = new TabControl();
            tabs.Pages.Add(new TabPage { Text = "Configurare", Content = new WoodPanel() });
            Add(tabs, "Proiect", new ProjectTab());
            Add(tabs, "Afișare", new DisplayTab());
            Add(tabs, "Optimizare", new OptimizeTab());
            Add(tabs, "Suprascrieri", new OverrideTab());
            Add(tabs, "Fișă tehnică", new SheetPanel());
            Add(tabs, "Documente", new DocumentsTab());
            Add(tabs, "Verificări", new ValidateTab());
            Add(tabs, "Setări", new SettingsTab());
            Add(tabs, "Materiale", new MaterialsTab());
            Add(tabs, "Despre", new AboutTab());
            Content = tabs;

            _changed = (s, e) => Application.Instance.AsyncInvoke(() => { foreach (var t in Refreshables) t.Rebuild(); });
            P.ProjectChanged += _changed;
            Closed += (s, e) => { P.ProjectChanged -= _changed; _instance = null; };
        }

        private readonly System.Collections.Generic.List<TabBase> Refreshables = new System.Collections.Generic.List<TabBase>();
        private void Add(TabControl tabs, string title, Control c)
        {
            if (c is TabBase tb) Refreshables.Add(tb);
            tabs.Pages.Add(new TabPage { Text = title, Content = c });
        }
    }
}
