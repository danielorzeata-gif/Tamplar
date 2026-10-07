using System.Linq;
using Rhino;
using RhinoWood.Core.Domain;

namespace RhinoWood.Plugin.UI
{
    /// <summary>Viewport preview of the project BEFORE it is generated: a display conduit, nothing is added to the document.</summary>
    public static class PreviewService
    {
        private static readonly PreviewConduit Conduit = new PreviewConduit();
        private static bool _zoomed;

        public static void Refresh()
        {
            var plugin = WoodPlugin.Instance; var doc = RhinoDoc.ActiveDoc;
            if (plugin == null || doc == null) { Conduit.Enabled = false; return; }
            var p = plugin.Project;
            bool show = p != null && plugin.PreviewOn && !plugin.Generated;
            if (!show) { if (Conduit.Enabled) { Conduit.Enabled = false; doc.Views.Redraw(); } _zoomed = false; return; }
            Conduit.Scale = RhinoMath.UnitScale(UnitSystem.Millimeters, doc.ModelUnitSystem);
            Conduit.Prims = p.GenerateGeometry(DisplayMode.Engineering);
            Conduit.Enabled = true;
            if (!_zoomed && Conduit.Bounds.IsValid && doc.Views.ActiveView != null) { doc.Views.ActiveView.ActiveViewport.ZoomBoundingBox(Conduit.Bounds); _zoomed = true; }
            doc.Views.Redraw();
        }
    }
}
