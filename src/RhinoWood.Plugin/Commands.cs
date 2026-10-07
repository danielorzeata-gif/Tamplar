using System;
using System.IO;
using System.Linq;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using RhinoWood.Core.Domain;
using RhinoWood.Core.Parametric;
using RhinoWood.Core.Projects;
using RhinoWood.Core.Reports;

namespace RhinoWood.Plugin
{
    public static class WoodActions
    {
        public static WoodPlugin P => WoodPlugin.Instance;

        public static bool RequireProject()
        {
            if (P.Project != null) return true;
            RhinoApp.WriteLine("Rhino Wood: no active project. Run WoodNewTable first.");
            return false;
        }

        /// <summary>Recalculate everything, then update Rhino geometry for the current display mode.</summary>
        public static void Refresh(RhinoDoc doc, bool announce = true)
        {
            if (P.Room != null && P.RoomGenerated) { P.Recalculate(); SyncRoom(doc); return; }
            if (!P.Generated) { P.Recalculate(); return; }      // draft: only the viewport preview exists
            var r = P.Recalculate();
            var prims = P.Project.GenerateGeometry();
            var rep = RhinoSync.Sync(doc, P.Project, prims, P.Project.CustomComponents);
            P.SaveToDocument(doc);
            if (announce)
            {
                RhinoApp.WriteLine(string.Format("Rhino Wood: {0} nodes recalculated; geometry +{1} ~{2} -{3} ={4}; purchase: {5}; total cost {6:0.00} {7}",
                    r.RecomputedNodes.Count, rep.Added, rep.Replaced, rep.Deleted, rep.Unchanged,
                    string.Join(", ", r.Optimization.Purchase.Select(p => p.Quantity + "x " + p.Item.Label + "x" + p.Length)), r.Cost.Total, r.Cost.Currency));
                foreach (var i in r.Issues.Where(i => i.Severity != Severity.Info)) RhinoApp.WriteLine("  " + i);
            }
            if (rep.ManualEdits.Count > 0) ResolveManualEdits(doc, rep);
        }

        /// <summary>Writes the draft into the document (idempotent) and stops the viewport preview.</summary>
        public static void Generate(RhinoDoc doc)
        {
            if (!RequireProject()) return;
            if (P.Room != null) { GenerateRoom(doc); return; }     // a room is always generated as a whole (otherwise the rest stays a preview "ghost")
            P.Generated = true;
            int gone = RhinoSync.RemoveOtherProjects(doc, new[] { P.Project.Id });
            if (gone > 0) RhinoApp.WriteLine("Rhino Wood: au fost șterse " + gone + " obiecte rămase de la alte proiecte.");
            Refresh(doc, true);
            RhinoApp.RunScript("-_Zoom _Extents", false);
        }

        /// <summary>New bedroom set (bed + 2 nightstands + dresser) with one set of choices; draft preview until Generează.</summary>
        public static void NewBedroom(string tier, bool wardrobe = false)
        {
            var ws = new RhinoWood.Core.Workspaces.Workspace("Dormitor", P.Library);
            var room = RhinoWood.Core.Projects.BedroomSet.Create(ws, tier, "OAK", "Dormitor", wardrobe);
            P.PreviewOn = true;
            P.SetRoom(ws, room, generated: false);
            CleanOtherProjects();
        }

        /// <summary>Writes every piece of the active room into the document (idempotent).</summary>
        public static void SyncRoom(RhinoDoc doc)
        {
            if (P.Room == null || doc == null) return;
            int gone = RhinoSync.RemoveOtherProjects(doc, P.Room.Pieces.Select(x => x.Project.Id).ToList());
            if (gone > 0) RhinoApp.WriteLine("Rhino Wood: au fost șterse " + gone + " obiecte rămase de la alte proiecte.");
            foreach (var piece in P.Room.Pieces)
                RhinoSync.Sync(doc, piece.Project, piece.Project.GenerateGeometry(), piece.Project.CustomComponents);
            P.SaveToDocument(doc);
        }

        public static void GenerateRoom(RhinoDoc doc)
        {
            if (P.Room == null) return;
            P.RoomGenerated = true; P.Generated = true;
            P.Recalculate(); SyncRoom(doc);
            RhinoApp.RunScript("-_Zoom _Extents", false);
        }

        /// <summary>Creates a draft of any furniture type (preview in the viewport until Generează).</summary>
        public static void NewProject(string typeId)
        {
            var types = RhinoWood.Core.Furniture.FurnitureRegistry.CreateDefault();
            var def = types.Get(typeId);
            var name = RhinoWood.Core.Reports.Ro.FurnitureName(typeId, def.Name);
            P.PreviewOn = true;
            P.SetProject(RhinoWood.Core.Projects.WoodProject.Create(typeId, name, "OAK", P.Library), generated: false);
            CleanOtherProjects();
        }

        /// <summary>A new draft replaces the previous work: its generated objects would otherwise stay in the viewport next to the new preview (ghosts). Undoable.</summary>
        public static void CleanOtherProjects()
        {
            var doc = RhinoDoc.ActiveDoc; if (doc == null || P.Project == null) return;
            var keep = P.Room != null ? P.Room.Pieces.Select(x => x.Project.Id).ToList() : new System.Collections.Generic.List<string> { P.Project.Id };
            int n = RhinoSync.RemoveOtherProjects(doc, keep);
            if (n > 0) RhinoApp.WriteLine("Rhino Wood: " + n + " obiecte ale proiectului anterior au fost șterse (Ctrl+Z le readuce).");
        }

        public static System.Collections.Generic.IEnumerable<(string id, string name)> FurnitureTypes() =>
            RhinoWood.Core.Furniture.FurnitureRegistry.CreateDefault().All.Select(d => (d.TypeId, RhinoWood.Core.Reports.Ro.FurnitureName(d.TypeId, d.Name)));

        /// <summary>Exports the PDF sheets (DESIGN or SALE); asks where to save. Returns a short message for the UI.</summary>
        public static string ExportPdf(RhinoWood.Core.Reports.SheetMode sm)
        {
            if (P.Project == null) return "Nu există un proiect activ.";
            var p = P.Project; var r = P.Recalculate();
            var dlg = new Eto.Forms.SaveFileDialog { Title = "Salvează PDF-ul", FileName = p.Name + (sm == RhinoWood.Core.Reports.SheetMode.Sale ? " - oferta.pdf" : " - planse.pdf") };
            dlg.Filters.Add(new Eto.Forms.FileFilter("PDF", ".pdf"));
            if (dlg.ShowDialog(Rhino.UI.RhinoEtoApp.MainWindow) != Eto.Forms.DialogResult.Ok) return "Export anulat.";
            var (ok, msg) = RhinoWood.Core.Reports.PdfExporter.ExportSheets(p, r, dlg.FileName, sm, null);
            if (ok) { try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true }); } catch { } }
            return ok ? "PDF salvat: " + msg : msg;
        }

        /// <summary>Adds a custom species (with the standard commercial profiles) to the user library.</summary>
        public static string AddSpecies(string name, double price, double density, double tangentialPerPercent)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Numele esenței este obligatoriu.";
            var id = name.Trim().ToUpperInvariant().Replace(" ", "");
            var plugin = P;
            var user = File.Exists(plugin.UserLibraryPath) ? RhinoWood.Core.Projects.LibrarySerializer.DeserializeUser(File.ReadAllText(plugin.UserLibraryPath)) : new RhinoWood.Core.Libraries.WoodLibrary();
            user.Species[id] = new WoodSpecies { Id = id, Name = name.Trim(), PricePerM3 = price, DensityKgM3 = density, TangentialMovementPerPercent = tangentialPerPercent, DiffShrinkTangentialPct = tangentialPerPercent * 100, SupplierId = "SUP-TIMBER", IsUserDefined = true, DataLabel = "[UNVERIFIED]", DataSource = "introdus de utilizator" };
            foreach (var st in plugin.Library.StockFor("OAK")) user.Stock.Add(new StockItem { Id = id + "-" + st.Width + "x" + st.Thickness, SpeciesId = id, Width = st.Width, Thickness = st.Thickness, Lengths = st.Lengths.ToList(), SupplierId = "SUP-TIMBER" });
            plugin.SaveUserLibrary(user); plugin.LoadLibrary();
            return "Esența „" + name.Trim() + "” a fost adăugată cu profilele comerciale standard (editează user-library.json pentru dimensiuni și prețuri exacte).";
        }

        /// <summary>Lays every part flat in the document as rough block, finished block and cut part + red waste (three layers).</summary>
        public static string ExportCutParts(bool wholeRoom = false)
        {
            if (P.Project == null) return "Nu există un proiect activ.";
            if (wholeRoom && P.Room != null) return CutPartsExport.RunMany(RhinoDoc.ActiveDoc, P.Room.Pieces.Select(x => x.Project).ToList());
            var r = P.Recalculate();
            return CutPartsExport.Run(RhinoDoc.ActiveDoc, P.Project, r);
        }

        /// <summary>The last packing plan (shown in the tabs).</summary>
        public static RhinoWood.Core.Logistics.PackingPlan LastPacking;

        /// <summary>Packs the active piece (or the whole room) into cartons and pallets, draws it in 3D and returns the report.</summary>
        public static string Palletize(bool wholeRoom = false)
        {
            if (P.Project == null) return "Nu există un proiect activ.";
            var doc = RhinoDoc.ActiveDoc;
            RhinoWood.Core.Logistics.PackingPlan plan; string name, id;
            System.Collections.Generic.List<RhinoWood.Core.Projects.WoodProject> projects;
            if (wholeRoom && P.Room != null)
            {
                projects = P.Room.Pieces.Select(x => x.Project).ToList(); name = P.Room.Name; id = "pack:" + P.Room.Id;
                plan = RhinoWood.Core.Logistics.PackingPlanner.Plan(P.Room.Pieces.Select(x => (x.Project, x.Project.Recalculate(), x.Name + ":")).ToList());
            }
            else
            {
                projects = new System.Collections.Generic.List<RhinoWood.Core.Projects.WoodProject> { P.Project }; name = P.Project.Name; id = "pack:" + P.Project.Id;
                plan = RhinoWood.Core.Logistics.PackingPlanner.Plan(RhinoWood.Core.Logistics.PackingPlanner.ItemsOf(P.Project, P.Recalculate()));
            }
            LastPacking = plan;
            if (doc == null) return plan.Report;
            var prims = projects.SelectMany(p => p.GenerateGeometry()).ToList();
            double minX = prims.Count == 0 ? 0 : prims.Min(p => p.Box.Min.X), minY = prims.Count == 0 ? 0 : prims.Min(p => p.Box.Min.Y);
            string msg = PalletExport.Run(doc, plan, name, id, minX, minY - 3500 - (plan.Pallets.Count == 0 ? 0 : plan.Pallets.Max(q => q.Spec.W)));
            return msg + "\n" + plan.Report;
        }

        /// <summary>Applies a suggested solution and refreshes the document / preview.</summary>
        public static void ApplyRemedy(RhinoWood.Core.Projects.Remedy remedy)
        {
            if (P.Project == null) return;
            RhinoWood.Core.Projects.RemedyEngine.Apply(P.Project, remedy);
            var doc = RhinoDoc.ActiveDoc;
            if (doc != null) Refresh(doc, false); else P.Recalculate();
        }

        /// <summary>Selects the Rhino objects of a part so a click on a cutting-list row highlights it in the viewport.</summary>
        public static void SelectPart(RhinoDoc doc, string partId)
        {
            if (doc == null || P.Project == null || !P.Generated) return;
            doc.Objects.UnselectAll();
            foreach (var o in doc.Objects.GetObjectList(new Rhino.DocObjects.ObjectEnumeratorSettings { NormalObjects = true, LockedObjects = false, HiddenObjects = false }))
                if (o.Attributes.GetUserString(RhinoSync.KProject) == P.Project.Id && o.Attributes.GetUserString(RhinoSync.KPart) == partId) o.Select(true);
            doc.Views.Redraw();
        }

        private static void ResolveManualEdits(RhinoDoc doc, RhinoSync.SyncReport rep)
        {
            double s = RhinoMath.UnitScale(UnitSystem.Millimeters, doc.ModelUnitSystem);
            foreach (var group in rep.ManualEdits.GroupBy(e => e.PartId))
            {
                var go = new GetOption();
                go.SetCommandPrompt("Part " + group.Key + " was modified by hand but its parametric definition changed. Choose");
                int rec = go.AddOption("Recalculate"), keep = go.AddOption("KeepManualOverride"), cust = go.AddOption("ConvertToCustomComponent");
                go.AcceptNothing(true);
                var res = go.Get();
                OverrideResolution how = OverrideResolution.KeepManualOverride;       // default (Enter/Esc): never destroy user work
                if (res == GetResult.Option)
                {
                    int idx = go.Option().Index;
                    how = idx == rec ? OverrideResolution.Recalculate : idx == cust ? OverrideResolution.ConvertToCustomComponent : OverrideResolution.KeepManualOverride;
                }
                foreach (var e in group) RhinoSync.ApplyResolution(doc, P.Project, e, how, s);
            }
            doc.Views.Redraw();
        }
    }

    [System.Runtime.InteropServices.Guid("2f0f8a3a-6c1e-4b53-b9a4-0a1c3d5e7f01")]
    public class WoodNewTableCommand : Command
    {
        public override string EnglishName => "WoodNewTable";
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Atelier: no modal dialogs. A draft is created in the panel and previewed in the viewport; "Generează" writes it into the document.
            var project = WoodProject.CreateTable("Masă sufragerie", "OAK", WoodActions.P.Library);
            WoodActions.P.PreviewOn = true;
            WoodActions.P.SetProject(project, generated: false);
            Rhino.UI.Panels.OpenPanel(typeof(RhinoWood.Plugin.UI.WoodPanel).GUID);
            RhinoApp.WriteLine("Rhino Wood: ciornă creată; ajustează în panou, apoi apasă Generează.");
            return Result.Success;
        }
    }
    [System.Runtime.InteropServices.Guid("2f0f8a3a-6c1e-4b53-b9a4-0a1c3d5e7f02")]
    public class WoodSetCommand : Command
    {
        public override string EnglishName => "WoodSet";
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (!WoodActions.RequireProject()) return Result.Failure;
            var p = WoodActions.P.Project;
            var go = new GetOption(); go.SetCommandPrompt("Parameter to change");
            var defs = p.Furniture.Parameters.ToList();
            var idx = defs.Select(d => go.AddOption(d.Key)).ToList();
            if (go.Get() != GetResult.Option) return Result.Cancel;
            var def = defs[idx.IndexOf(go.Option().Index)];
            double v = p.Parameters[def.Key];
            if (RhinoGet.GetNumber(def.Label + " (" + def.Min + ".." + def.Max + ")", false, ref v, def.Min, def.Max) != Result.Success) return Result.Cancel;
            p.SetParameter(def.Key, v);
            WoodActions.Refresh(doc);
            return Result.Success;
        }
    }

    [System.Runtime.InteropServices.Guid("2f0f8a3a-6c1e-4b53-b9a4-0a1c3d5e7f03")]
    public class WoodOverrideCommand : Command
    {
        public override string EnglishName => "WoodOverride";
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (!WoodActions.RequireProject()) return Result.Failure;
            var p = WoodActions.P.Project;
            var go = new GetOption(); go.SetCommandPrompt("Overridable value (rule stays active underneath)");
            var nodes = p.Furniture.OverridableNodes.ToList();
            var idx = nodes.Select(n => go.AddOption(n.Replace(".", "_"))).ToList();
            int clear = go.AddOption("ClearOverride");
            if (go.Get() != GetResult.Option) return Result.Cancel;
            if (go.Option().Index == clear)
            {
                foreach (var o in p.Overrides.ToList()) p.ResolveOverride(o.Key, OverrideResolution.Recalculate);
                WoodActions.Refresh(doc); return Result.Success;
            }
            var node = nodes[idx.IndexOf(go.Option().Index)];
            var info = p.GetOverride(node);
            RhinoApp.WriteLine("{0}: calculated {1:0.##}, current {2:0.##}", node, info.Calculated, info.Final);
            double v = 0;
            if (RhinoGet.GetNumber("Offset to ADD to the calculated value (mm)", true, ref v) != Result.Success) return Result.Cancel;
            p.SetOverride(node, OverrideMode.Add, v);
            WoodActions.Refresh(doc);
            return Result.Success;
        }
    }

    [System.Runtime.InteropServices.Guid("2f0f8a3a-6c1e-4b53-b9a4-0a1c3d5e7f04")]
    public class WoodDisplayCommand : Command
    {
        public override string EnglishName => "WoodDisplay";
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (!WoodActions.RequireProject()) return Result.Failure;
            var p = WoodActions.P.Project;
            var go = new GetOption(); go.SetCommandPrompt("Display mode (current: " + p.Settings.Display + ")");
            var modes = Enum.GetValues(typeof(DisplayMode)).Cast<DisplayMode>().ToList();
            var idx = modes.Select(m => go.AddOption(m.ToString())).ToList();
            int grain = go.AddOption("ToggleGrain");
            if (go.Get() != GetResult.Option) return Result.Cancel;
            if (go.Option().Index == grain) p.Settings.ShowGrain = !p.Settings.ShowGrain;
            else p.Settings.Display = modes[idx.IndexOf(go.Option().Index)];
            WoodActions.Refresh(doc, false);
            return Result.Success;
        }
    }

    [System.Runtime.InteropServices.Guid("2f0f8a3a-6c1e-4b53-b9a4-0a1c3d5e7f05")]
    public class WoodOptimizeCommand : Command
    {
        public override string EnglishName => "WoodOptimize";
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (!WoodActions.RequireProject()) return Result.Failure;
            var p = WoodActions.P.Project;
            var go = new GetOption(); go.SetCommandPrompt("Optimization strategy (Enter = keep " + p.Settings.Strategy + ")");
            var strategies = Enum.GetValues(typeof(OptimizationStrategy)).Cast<OptimizationStrategy>().ToList();
            var idx = strategies.Select(s => go.AddOption(s.ToString())).ToList();
            go.AcceptNothing(true);
            if (go.Get() == GetResult.Option) p.Settings.Strategy = strategies[idx.IndexOf(go.Option().Index)];
            var r = WoodActions.P.Recalculate();
            WoodActions.P.SaveToDocument(doc);
            RhinoApp.WriteLine(ReportBuilder.OptimizationText(r));
            Rhino.UI.Panels.OpenPanel(typeof(RhinoWood.Plugin.UI.WoodPanel).GUID);
            return Result.Success;
        }
    }

    [System.Runtime.InteropServices.Guid("2f0f8a3a-6c1e-4b53-b9a4-0a1c3d5e7f06")]
    public class WoodValidateCommand : Command
    {
        public override string EnglishName => "WoodValidate";
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (!WoodActions.RequireProject()) return Result.Failure;
            var r = WoodActions.P.Recalculate();
            RhinoApp.WriteLine("Validation: {0} error(s), {1} warning(s), {2} info", r.Issues.Count(i => i.Severity == Severity.Error), r.Issues.Count(i => i.Severity == Severity.Warning), r.Issues.Count(i => i.Severity == Severity.Info));
            foreach (var i in r.Issues) RhinoApp.WriteLine("  " + i);
            return Result.Success;
        }
    }

    [System.Runtime.InteropServices.Guid("2f0f8a3a-6c1e-4b53-b9a4-0a1c3d5e7f07")]
    public class WoodReportCommand : Command
    {
        public override string EnglishName => "WoodReport";
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (!WoodActions.RequireProject()) return Result.Failure;
            var dlg = new Eto.Forms.SelectFolderDialog { Title = "Export Rhino Wood documentation to..." };
            if (dlg.ShowDialog(Rhino.UI.RhinoEtoApp.MainWindow) != Eto.Forms.DialogResult.Ok) return Result.Cancel;
            var p = WoodActions.P.Project; var r = WoodActions.P.Recalculate();
            var files = ReportWriter.WriteAll(dlg.Directory, p, r);
            RhinoApp.WriteLine("Rhino Wood: wrote {0} documents to {1}", files.Count, dlg.Directory);
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Path.Combine(dlg.Directory, "project_summary.html")) { UseShellExecute = true }); } catch { }
            return Result.Success;
        }
    }

    [System.Runtime.InteropServices.Guid("2f0f8a3a-6c1e-4b53-b9a4-0a1c3d5e7f08")]
    public class WoodSaveCommand : Command
    {
        public override string EnglishName => "WoodSave";
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (!WoodActions.RequireProject()) return Result.Failure;
            WoodActions.P.SaveToDocument(doc);
            var dlg = new Eto.Forms.SaveFileDialog { Title = "Save Rhino Wood project file", FileName = WoodActions.P.Project.Name + ".rhinowood.json" };
            if (dlg.ShowDialog(Rhino.UI.RhinoEtoApp.MainWindow) == Eto.Forms.DialogResult.Ok)
            { File.WriteAllText(dlg.FileName, ProjectSerializer.Serialize(WoodActions.P.Project, WoodActions.P.LastResult)); RhinoApp.WriteLine("Saved " + dlg.FileName); }
            RhinoApp.WriteLine("Project data is stored inside the Rhino document; save the .3dm as usual.");
            return Result.Success;
        }
    }

    [System.Runtime.InteropServices.Guid("2f0f8a3a-6c1e-4b53-b9a4-0a1c3d5e7f09")]
    public class WoodOpenCommand : Command
    {
        public override string EnglishName => "WoodOpen";
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var dlg = new Eto.Forms.OpenFileDialog { Title = "Open Rhino Wood project file" };
            if (dlg.ShowDialog(Rhino.UI.RhinoEtoApp.MainWindow) != Eto.Forms.DialogResult.Ok) return Result.Cancel;
            var (p, ok) = ProjectSerializer.Open(File.ReadAllText(dlg.FileName), WoodActions.P.Library);
            WoodActions.P.SetProject(p);
            WoodActions.Refresh(doc);
            RhinoApp.WriteLine(ok ? "Project reopened; relationships and plan verified." : "Project reopened; plan differs from the saved snapshot (rules/library changed).");
            return Result.Success;
        }
    }

    [System.Runtime.InteropServices.Guid("2f0f8a3a-6c1e-4b53-b9a4-0a1c3d5e7f0a")]
    public class WoodSettingsCommand : Command
    {
        public override string EnglishName => "WoodSettings";
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (!WoodActions.RequireProject()) return Result.Failure;
            var s = WoodActions.P.Project.Settings;
            double reserve = s.GlobalReservePercent, kerf = s.Rules.SawKerf, lenAllow = s.Rules.LengthAllowance;
            if (RhinoGet.GetNumber("Global material reserve % (applied once, after optimization)", true, ref reserve, 0, 50) != Result.Success) return Result.Cancel;
            if (RhinoGet.GetNumber("Saw kerf (mm)", true, ref kerf, 0, 10) != Result.Success) return Result.Cancel;
            if (RhinoGet.GetNumber("Length allowance (mm)", true, ref lenAllow, 0, 200) != Result.Success) return Result.Cancel;
            s.GlobalReservePercent = reserve; s.Rules.SawKerf = kerf; s.Rules.LengthAllowance = lenAllow;
            WoodActions.P.Project.Rebuild();
            WoodActions.Refresh(doc);
            return Result.Success;
        }
    }

    [System.Runtime.InteropServices.Guid("2f0f8a3a-6c1e-4b53-b9a4-0a1c3d5e7f0b")]
    public class WoodPanelCommand : Command
    {
        public override string EnglishName => "WoodPanel";
        protected override Result RunCommand(RhinoDoc doc, RunMode mode) { Rhino.UI.Panels.OpenPanel(typeof(RhinoWood.Plugin.UI.WoodPanel).GUID); return Result.Success; }
    }

    [System.Runtime.InteropServices.Guid("2f0f8a3a-6c1e-4b53-b9a4-0a1c3d5e7f0d")]
    public class WoodSheetCommand : Command
    {
        public override string EnglishName => "WoodSheet";
        protected override Result RunCommand(RhinoDoc doc, RunMode mode) { Rhino.UI.Panels.OpenPanel(typeof(RhinoWood.Plugin.UI.SheetPanel).GUID); return Result.Success; }
    }

    [System.Runtime.InteropServices.Guid("2f0f8a3a-6c1e-4b53-b9a4-0a1c3d5e7f0e")]
    public class WoodPdfCommand : Command
    {
        public override string EnglishName => "WoodPdf";
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (!WoodActions.RequireProject()) return Result.Failure;
            var go = new GetOption(); go.SetCommandPrompt("Tip document PDF");
            go.AddOption("Design"); int sale = go.AddOption("Vanzare");
            go.AcceptNothing(true);
            var sm = RhinoWood.Core.Reports.SheetMode.Design;
            if (go.Get() == GetResult.Option && go.Option().Index == sale) sm = RhinoWood.Core.Reports.SheetMode.Sale;
            RhinoApp.WriteLine("Rhino Wood: " + WoodActions.ExportPdf(sm));
            return Result.Success;
        }
    }

    [System.Runtime.InteropServices.Guid("2f0f8a3a-6c1e-4b53-b9a4-0a1c3d5e7f0f")]
    public class WoodAboutCommand : Command
    {
        public override string EnglishName => "WoodAbout";
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            RhinoApp.WriteLine("Rhino Wood " + RhinoWood.Plugin.UI.BuildInfo.Text);
            RhinoApp.WriteLine("Fișier încărcat: " + RhinoWood.Plugin.UI.BuildInfo.Path);
            return Result.Success;
        }
    }

    [System.Runtime.InteropServices.Guid("2f0f8a3a-6c1e-4b53-b9a4-0a1c3d5e7f11")]
    public class WoodCutPartsCommand : Command
    {
        public override string EnglishName => "WoodUnfold";
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (!WoodActions.RequireProject()) return Result.Failure;
            RhinoApp.WriteLine("Rhino Wood: " + WoodActions.ExportCutParts());
            return Result.Success;
        }
    }

    [System.Runtime.InteropServices.Guid("2f0f8a3a-6c1e-4b53-b9a4-0a1c3d5e7f12")]
    public class WoodNewCommand : Command
    {
        public override string EnglishName => "WoodNew";
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var types = WoodActions.FurnitureTypes().ToList();
            var go = new GetOption(); go.SetCommandPrompt("Piesă nouă");
            var idx = types.Select(t => go.AddOption(t.id.Replace(".", "_").Replace("casework_", ""))).ToList();
            if (go.Get() != GetResult.Option) return Result.Cancel;
            WoodActions.NewProject(types[idx.IndexOf(go.Option().Index)].id);
            RhinoApp.WriteLine("Rhino Wood: ciornă creată; ajustează în fereastra WoodStart, apoi Generează.");
            return Result.Success;
        }
    }

    [System.Runtime.InteropServices.Guid("2f0f8a3a-6c1e-4b53-b9a4-0a1c3d5e7f13")]
    public class WoodBedroomCommand : Command
    {
        public override string EnglishName => "WoodBedroom";
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            WoodActions.NewBedroom("STANDARD");
            RhinoApp.WriteLine("Rhino Wood: dormitor creat (pat, 2 noptiere, comodă). Deschide WoodStart → tab Cameră.");
            RhinoWood.Plugin.UI.StartWindow.Open();
            return Result.Success;
        }
    }

    [System.Runtime.InteropServices.Guid("2f0f8a3a-6c1e-4b53-b9a4-0a1c3d5e7f14")]
    public class WoodCleanCommand : Command
    {
        public override string EnglishName => "WoodClean";
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var keep = WoodActions.P.Room != null ? WoodActions.P.Room.Pieces.Select(x => x.Project.Id).ToList() : new System.Collections.Generic.List<string>();
            if (WoodActions.P.Room == null && WoodActions.P.Project != null && WoodActions.P.Generated) keep.Add(WoodActions.P.Project.Id);
            int n = RhinoSync.RemoveOtherProjects(doc, keep);
            RhinoApp.WriteLine("Rhino Wood: " + n + " obiecte șterse (rămase de la proiecte care nu mai sunt active).");
            return Result.Success;
        }
    }

    [System.Runtime.InteropServices.Guid("2f0f8a3a-6c1e-4b53-b9a4-0a1c3d5e7f15")]
    public class WoodPalletCommand : Command
    {
        public override string EnglishName => "WoodPallet";
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (!WoodActions.RequireProject()) return Result.Failure;
            RhinoApp.WriteLine("Rhino Wood: " + WoodActions.Palletize(WoodActions.P.Room != null));
            return Result.Success;
        }
    }

    [System.Runtime.InteropServices.Guid("2f0f8a3a-6c1e-4b53-b9a4-0a1c3d5e7f10")]
    public class WoodStartCommand : Command
    {
        public override string EnglishName => "WoodStart";
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (WoodActions.P.Project == null)
            {
                WoodActions.P.PreviewOn = true;
                WoodActions.P.SetProject(RhinoWood.Core.Projects.WoodProject.CreateTable("Masă sufragerie", "OAK", WoodActions.P.Library), generated: false);
            }
            RhinoWood.Plugin.UI.StartWindow.Open();
            return Result.Success;
        }
    }

    [System.Runtime.InteropServices.Guid("2f0f8a3a-6c1e-4b53-b9a4-0a1c3d5e7f0c")]
    public class WoodAddSpeciesCommand : Command
    {
        public override string EnglishName => "WoodAddSpecies";
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var plugin = WoodActions.P;
            string name = "";
            if (RhinoGet.GetString("New species name", false, ref name) != Result.Success || string.IsNullOrWhiteSpace(name)) return Result.Cancel;
            double price = 1500, dens = 650, tang = 0.0040;
            if (RhinoGet.GetNumber("Price per m3", false, ref price, 1, 100000) != Result.Success) return Result.Cancel;
            if (RhinoGet.GetNumber("Density kg/m3", true, ref dens, 100, 1400) != Result.Success) return Result.Cancel;
            if (RhinoGet.GetNumber("Tangential movement (fraction per 1% MC, e.g. 0.004)", true, ref tang, 0.0005, 0.02) != Result.Success) return Result.Cancel;
            var id = name.ToUpperInvariant().Replace(" ", "");
            var user = File.Exists(plugin.UserLibraryPath) ? RhinoWood.Core.Projects.LibrarySerializer.DeserializeUser(File.ReadAllText(plugin.UserLibraryPath)) : new RhinoWood.Core.Libraries.WoodLibrary();
            user.Species[id] = new WoodSpecies { Id = id, Name = name, PricePerM3 = price, DensityKgM3 = dens, TangentialMovementPerPercent = tang, SupplierId = "SUP-TIMBER", IsUserDefined = true };
            foreach (var st in plugin.Library.StockFor("OAK")) user.Stock.Add(new StockItem { Id = id + "-" + st.Width + "x" + st.Thickness, SpeciesId = id, Width = st.Width, Thickness = st.Thickness, Lengths = st.Lengths.ToList(), SupplierId = "SUP-TIMBER" });
            plugin.SaveUserLibrary(user); plugin.LoadLibrary();
            RhinoApp.WriteLine("Species '" + name + "' added to the user library with standard commercial profiles (edit user-library.json to adjust).");
            return Result.Success;
        }
    }
}
