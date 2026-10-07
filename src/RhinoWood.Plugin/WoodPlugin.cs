using System;
using System.IO;
using System.Runtime.InteropServices;
using Rhino;
using Rhino.PlugIns;
using RhinoWood.Core.Libraries;
using RhinoWood.Core.Projects;

[assembly: Guid("6b8c9a1e-4f2d-4e6a-9d57-3a1c2b7f0e11")]
[assembly: PlugInDescription(DescriptionType.Address, "Rhino Wood")]
[assembly: PlugInDescription(DescriptionType.Country, "Romania")]
[assembly: PlugInDescription(DescriptionType.Organization, "Rhino Wood")]
[assembly: PlugInDescription(DescriptionType.UpdateUrl, "")]
[assembly: PlugInDescription(DescriptionType.WebSite, "")]

namespace RhinoWood.Plugin
{
    /// <summary>
    /// Rhino 8 plugin entry point. Holds the active <see cref="WoodProject"/> (the source of truth); the Rhino document only contains
    /// lightweight geometry tagged with project id / object key / part id / version / type, plus ONE project string in the document.
    /// </summary>
    public sealed class WoodPlugin : PlugIn
    {
        public const string DocSection = "RhinoWood";
        public const string DocEntry = "project";

        public static WoodPlugin Instance { get; private set; }
        public WoodProject Project { get; private set; }
        public ProjectResult LastResult { get; private set; }
        public WoodLibrary Library { get; private set; }
        public event EventHandler ProjectChanged;
        /// <summary>True once geometry for the active project has been generated into the document (until then only the viewport preview exists).</summary>
        public bool Generated { get; set; }
        public bool PreviewOn { get; set; } = true;

        public WoodPlugin() { Instance = this; }

        public string UserLibraryPath => Path.Combine(SettingsDirectory, "user-library.json");

        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            try
            {
                LoadLibrary();
                Rhino.UI.Panels.RegisterPanel(this, typeof(RhinoWood.Plugin.UI.WoodPanel), "Rhino Wood", RhinoWood.Plugin.UI.WoodPanel.CreateIcon());
                Rhino.UI.Panels.RegisterPanel(this, typeof(RhinoWood.Plugin.UI.SheetPanel), "Rhino Wood · Fișă tehnică", RhinoWood.Plugin.UI.WoodPanel.CreateIcon());
                RhinoDoc.EndOpenDocument += OnEndOpenDocument;
                RhinoDoc.BeginSaveDocument += OnBeginSaveDocument;
                RhinoDoc.CloseDocument += (s, e) => { if (RhinoDoc.ActiveDoc == null) { Project = null; LastResult = null; Raise(); } };
                return LoadReturnCode.Success;
            }
            catch (Exception ex)
            {
                errorMessage = "Rhino Wood failed to initialise: " + ex.Message;
                return LoadReturnCode.ErrorShowDialog;
            }
        }

        public void LoadLibrary()
        {
            Library = WoodLibrary.CreateDefault();
            try
            {
                if (File.Exists(UserLibraryPath)) Library.Merge(LibrarySerializer.DeserializeUser(File.ReadAllText(UserLibraryPath)));
            }
            catch (Exception ex) { RhinoApp.WriteLine("Rhino Wood: user library could not be read (" + ex.Message + "); using system library only."); }
        }

        public void SaveUserLibrary(WoodLibrary user)
        {
            Directory.CreateDirectory(SettingsDirectory);
            File.WriteAllText(UserLibraryPath, LibrarySerializer.SerializeUser(user));
        }

        public void SetProject(WoodProject p, bool generated = false)
        {
            Project = p; Generated = generated; LastResult = p?.Recalculate();
            Raise();
        }

        public ProjectResult Recalculate()
        {
            if (Project == null) return null;
            LastResult = Project.Recalculate();
            Raise();
            return LastResult;
        }

        public void Raise() { RhinoWood.Plugin.UI.PreviewService.Refresh(); ProjectChanged?.Invoke(this, EventArgs.Empty); }

        // ---------------------------------------------------------------- persistence inside the .3dm (single string, no per-object JSON)
        public void SaveToDocument(RhinoDoc doc)
        {
            if (doc == null) return;
            if (Project == null) { doc.Strings.Delete(DocSection, DocEntry); return; }
            doc.Strings.SetString(DocSection, DocEntry, ProjectSerializer.Serialize(Project, LastResult));
        }

        private void OnBeginSaveDocument(object sender, DocumentSaveEventArgs e) => SaveToDocument(e.Document);

        private void OnEndOpenDocument(object sender, DocumentOpenEventArgs e)
        {
            if (e.Merge) return;
            var json = e.Document.Strings.GetValue(DocSection, DocEntry);
            if (string.IsNullOrEmpty(json)) { Project = null; LastResult = null; Raise(); return; }
            try
            {
                var (p, ok) = ProjectSerializer.Open(json, Library);
                Project = p; Generated = true; LastResult = p.Recalculate();
                RhinoApp.WriteLine("Rhino Wood: project '" + p.Name + "' reopened" + (ok ? " - plan verified." : " - WARNING: recalculated plan differs from the saved snapshot (library or rules changed)."));
                Raise();
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine("Rhino Wood: could not reopen project data: " + ex.Message + " (project data kept untouched in the document).");
            }
        }
    }
}
