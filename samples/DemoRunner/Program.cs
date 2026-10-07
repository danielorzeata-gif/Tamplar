using System.Linq;
using System;
using System.IO;
using RhinoWood.Core.Projects;
using RhinoWood.Core.Reports;

// End-to-end demonstration of the 19-step workflow from the specification (headless, no Rhino required).
var outDir = args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, "demo-output");

Console.WriteLine("1-3. Create project, select table, select material (Oak)");
var project = WoodProject.CreateTable("Oak dining table", "OAK");
Console.WriteLine("4. Configure dimensions 1800 x 900 x 760, top 35");
project.SetParameter("length", 1800); project.SetParameter("width", 900); project.SetParameter("height", 760); project.SetParameter("topThickness", 35);

Console.WriteLine("5-14. Geometry, joinery, hardware, validation, rough dims, global optimization, procurement, cutting plan, BOM, operations");
var r = project.Recalculate();
Console.WriteLine($"   parts: {r.Model.AllParts.Count()}  families: {r.Model.Families.Count}  joints: {r.Model.Joints.Count}  hardware: {r.Model.HardwareInstalls.Count}");
foreach (var i in r.Issues) Console.WriteLine("   " + i);
Console.WriteLine();
Console.WriteLine(ReportBuilder.OptimizationText(r));

Console.WriteLine("15. Documentation -> " + outDir);
var files = ReportWriter.WriteAll(outDir, project, r);
foreach (var f in files) Console.WriteLine("   " + Path.GetFileName(f));

foreach (var (mode, name) in new[] { (SheetMode.Design, "planse_design.pdf"), (SheetMode.Sale, "oferta_vanzare.pdf") })
{
    var (pdfOk, msg) = PdfExporter.ExportSheets(project, r, Path.Combine(outDir, name), mode, "STANDARD");
    Console.WriteLine("   " + name + ": " + (pdfOk ? "OK" : "NU - " + msg));
}
Console.WriteLine("16-19. Save, 'close', reopen, verify");
var json = File.ReadAllText(Path.Combine(outDir, "project.rhinowood.json"));
var (reopened, ok) = ProjectSerializer.Open(json);
Console.WriteLine("   snapshot matches after reopening: " + ok + "  parameters: length=" + reopened.Parameters["length"]);
Console.WriteLine($"TOTAL COST {r.Cost.Total:0.00} {r.Cost.Currency}");
