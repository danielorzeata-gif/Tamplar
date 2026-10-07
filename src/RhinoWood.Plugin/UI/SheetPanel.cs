using System;
using System.Runtime.InteropServices;
using Eto.Drawing;
using Eto.Forms;
using RhinoWood.Core.Reports;

namespace RhinoWood.Plugin.UI
{
    /// <summary>
    /// "Fișă tehnică" preview: the technical sheet (3 views + materials) rendered live and refreshed after every change.
    /// It is a separate dockable panel so it can be widened or floated next to the 320 px Atelier panel; the other sheets
    /// (joint dimensions, cutting plan, assembly notes) are only produced by the PDF export.
    /// </summary>
    [Guid("c4b2f8d1-5e7a-4a31-9a6e-2d1f0b8c7e22")]
    public class SheetPanel : Panel
    {
        private readonly WebView _web = new WebView();
        private readonly Label _status = new Label { Text = "Nu există un proiect activ.", TextColor = Colors.Gray };
        private static WoodPlugin P => WoodPlugin.Instance;

        public SheetPanel()
        {
            Content = new TableLayout { Rows = { new TableRow(_status), new TableRow(_web) { ScaleHeight = true } } };
            P.ProjectChanged += (s, e) => Application.Instance.AsyncInvoke(Refresh);
            Refresh();
        }

        private void Refresh()
        {
            var p = P?.Project; var r = P?.LastResult;
            if (p == null || r == null) { _status.Text = "Nu există un proiect activ."; _web.LoadHtml("<html><body style='font-family:Segoe UI;color:#5f574c;background:#f3f1ed'></body></html>"); return; }
            try
            {
                var sheet = SheetBuilder.TechnicalSheet(p, r, SheetMode.Design, null);
                _web.LoadHtml(SheetDocument.PreviewHtml(sheet, SheetBuilderSubtitle(p)));
                _status.Text = p.Name + " · fișă tehnică (previzualizare)";
            }
            catch (Exception ex) { _status.Text = "Previzualizarea a eșuat: " + ex.Message; }
        }

        private static string SheetBuilderSubtitle(RhinoWood.Core.Projects.WoodProject p) =>
            PdfExporter.SheetDocumentSubtitle(p, SheetMode.Design, null);
    }
}
