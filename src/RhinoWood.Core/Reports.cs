using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using RhinoWood.Core.Domain;
using RhinoWood.Core.Geometry;
using RhinoWood.Core.Optimization;
using RhinoWood.Core.Projects;

namespace RhinoWood.Core.Reports
{
    public static class Fmt
    {
        public static readonly CultureInfo C = CultureInfo.InvariantCulture;
        public static string N(double v, string f = "0.##") => v.ToString(f, C);
        public static string Csv(params string[] cells) => string.Join(",", cells.Select(c => c == null ? "" : (c.IndexOfAny(new[] { ',', '"', '\n' }) >= 0 ? "\"" + c.Replace("\"", "\"\"") + "\"" : c)));
        public static string Esc(string s) => WebUtility.HtmlEncode(s ?? "");
    }

    public static class ReportBuilder
    {
        private static string Dim(Dims d) => Fmt.N(d.Length, "0.#") + " x " + Fmt.N(d.Width, "0.#") + " x " + Fmt.N(d.Thickness, "0.#");

        public static string BomCsv(ProjectResult r)
        {
            var sb = new StringBuilder();
            sb.AppendLine(Fmt.Csv("Category", "Id", "Description", "Quantity", "Unit", "Unit cost", "Total", "Notes"));
            foreach (var l in r.Bom.Lines)
                sb.AppendLine(Fmt.Csv(l.Category, l.Id, l.Description, Fmt.N(l.Quantity, "0.###"), l.Unit, Fmt.N(l.UnitCost, "0.00"), Fmt.N(l.Total, "0.00"), l.Notes));
            return sb.ToString();
        }

        public static string CutListCsv(ProjectResult r)
        {
            var sb = new StringBuilder();
            sb.AppendLine(Fmt.Csv("Part ID", "Part family", "Quantity", "Rough L x W x T", "Finished L x W x T", "Material", "Grain direction", "Stock source (board)", "Purchase line", "Cut ID", "Cut start (mm)", "Cutting operation"));
            foreach (var b in r.Optimization.Boards.Where(x => !x.IsReserve))
                foreach (var c in b.Cuts)
                {
                    var fam = r.Model.Families.First(f => f.Id == c.Demand.FamilyId);
                    var pl = r.Optimization.Purchase.First(p => p.BoardIds.Contains(b.Id));
                    sb.AppendLine(Fmt.Csv(c.Demand.PartId, fam.Name, "1", Fmt.N(c.Length, "0.#") + " x " + Fmt.N(c.Demand.SecA, "0.#") + " x " + Fmt.N(c.Demand.SecB, "0.#"),
                        Dim(fam.Finished), fam.SpeciesId, fam.GrainAxis + (fam.GrainAlongLength ? " (along length)" : ""), b.Id + " " + b.Item.Label + " x " + Fmt.N(b.Length, "0"), pl.Id, c.CutId, Fmt.N(c.Start, "0.#"),
                        "Crosscut from board " + b.Id));
                }
            return sb.ToString();
        }

        public static string FamilyListCsv(ProjectResult r)
        {
            var sb = new StringBuilder();
            sb.AppendLine(Fmt.Csv("Family", "Name", "Quantity", "Finished", "Rough piece(s)", "Species", "Assembly", "Grain", "Unit cost"));
            foreach (var f in r.Model.Families)
                sb.AppendLine(Fmt.Csv(f.Id, f.Name, f.Quantity.ToString(Fmt.C), Dim(f.Finished), string.Join(" + ", f.RoughPieces.Select(p => p.CountPerPart + " x " + Dim(p.Rough))), f.SpeciesId, f.Assembly, f.GrainAxis.ToString(), Fmt.N(f.UnitCost, "0.00")));
            return sb.ToString();
        }

        public static string ProcurementCsv(ProjectResult r)
        {
            var sb = new StringBuilder();
            sb.AppendLine(Fmt.Csv("Purchase line", "Material", "Species", "Profile (W x T)", "Commercial length (mm)", "Quantity", "of which reserve", "Supplier", "Unit price", "Total", "Expected usable remnant (m3)", "Expected waste (m3)", "Boards"));
            foreach (var p in r.Optimization.Purchase)
                sb.AppendLine(Fmt.Csv(p.Id, p.Item.Label, p.Item.SpeciesId, Fmt.N(p.Item.Width, "0.#") + " x " + Fmt.N(p.Item.Thickness, "0.#"), Fmt.N(p.Length, "0"), p.Quantity.ToString(Fmt.C), p.ReserveQuantity.ToString(Fmt.C), p.SupplierId, Fmt.N(p.UnitPrice, "0.00"), Fmt.N(p.Total, "0.00"), Fmt.N(p.ExpectedUsableRemnantM3, "0.0000"), Fmt.N(p.ExpectedWasteM3, "0.0000"), string.Join(" ", p.BoardIds)));
            sb.AppendLine(Fmt.Csv("TOTAL", "", "", "", "", r.Optimization.Purchase.Sum(p => p.Quantity).ToString(Fmt.C), "", "", "", Fmt.N(r.Optimization.TotalCost, "0.00")));
            return sb.ToString();
        }

        public static string ManufacturingCsv(ProjectResult r)
        {
            var sb = new StringBuilder();
            sb.AppendLine(Fmt.Csv("Operation", "Part", "Seq", "Type", "Description", "Machine", "Tool", "Minutes", "Source (joint/hardware)", "Board", "Cut"));
            foreach (var o in r.Manufacturing.Operations)
                sb.AppendLine(Fmt.Csv(o.Id, o.PartId, o.Sequence.ToString(Fmt.C), o.Type.ToString(), o.Description, o.Machine, o.ToolId, Fmt.N(o.Minutes, "0.#"), o.SourceId, o.BoardId, o.CutId));
            return sb.ToString();
        }

        public static string TraceabilityText(ProjectResult r)
        {
            var sb = new StringBuilder();
            foreach (var t in r.Manufacturing.Traces.Values.OrderBy(x => x.PartId, StringComparer.Ordinal))
                sb.AppendLine(string.Format("PART {0}  <- PURCHASE {1} / BOARD {2} / CUT {3}  -> OPS {4}  | JOINTS {5} | HARDWARE {6} | ASSEMBLY {7}",
                    t.PartId, string.Join(",", t.PurchaseLineIds), string.Join(",", t.BoardIds), string.Join(",", t.CutIds), string.Join(" ", t.OperationIds), string.Join(",", t.JointIds), string.Join(",", t.HardwareIds), t.Assembly));
            return sb.ToString();
        }

        public static string OptimizationText(ProjectResult r)
        {
            var o = r.Optimization; var sb = new StringBuilder();
            sb.AppendLine("PROJECT MATERIAL OPTIMIZATION (strategy: " + o.Strategy + ")");
            sb.AppendLine();
            sb.AppendLine("Required (theoretical finished):  " + Fmt.N(o.TheoreticalFinishedM3, "0.0000") + " m3");
            sb.AppendLine("Rough with allowances:            " + Fmt.N(o.RoughRequiredM3, "0.0000") + " m3");
            sb.AppendLine("Optimized manufacturing:          " + Fmt.N(o.OptimizedManufacturingM3, "0.0000") + " m3");
            sb.AppendLine("Purchase (commercial, incl. reserve): " + Fmt.N(o.PurchasedM3, "0.0000") + " m3");
            foreach (var p in o.Purchase) sb.AppendLine("   " + p.Quantity + " x " + p.Item.Label + " x " + Fmt.N(p.Length, "0") + (p.ReserveQuantity > 0 ? "  (" + p.ReserveQuantity + " reserve)" : "") + "   = " + Fmt.N(p.Total, "0.00") + " " + r.Cost.Currency);
            sb.AppendLine("Reserve: " + string.Join(", ", o.ReservePercentBySpecies.Select(kv => kv.Key + " " + Fmt.N(kv.Value) + "%")));
            sb.AppendLine("Expected reusable remnants:       " + Fmt.N(o.ReusableRemnantM3, "0.0000") + " m3");
            sb.AppendLine("Expected process waste:           " + Fmt.N(o.ProcessWasteM3, "0.0000") + " m3");
            sb.AppendLine("Scrap:                            " + Fmt.N(o.ScrapM3, "0.0000") + " m3");
            sb.AppendLine("Estimated material cost:          " + Fmt.N(o.TotalCost, "0.00") + " " + r.Cost.Currency);
            sb.AppendLine();
            sb.AppendLine("WHY THIS PURCHASE?");
            foreach (var l in o.Explanation) sb.AppendLine(l);
            sb.AppendLine();
            sb.AppendLine("CUTTING PLAN");
            foreach (var b in o.Boards)
            {
                sb.AppendLine(b.Id + (b.IsReserve ? " (RESERVE)" : "") + "  " + b.Item.Label + " x " + Fmt.N(b.Length, "0") + " mm");
                foreach (var c in b.Cuts) sb.AppendLine("   " + Fmt.N(c.Length, "0.#").PadLeft(8) + " -> " + c.Demand.PartId + "  [" + c.CutId + ", " + c.Demand.Role + ", start " + Fmt.N(c.Start, "0.#") + "]");
                if (b.TailLength > 0) sb.AppendLine("   " + Fmt.N(b.TailLength, "0.#").PadLeft(8) + " -> " + (b.TailClass == RemnantClass.ProjectRemnant ? "REMNANT (reusable)" : "SCRAP"));
            }
            sb.AppendLine();
            sb.AppendLine("OPTIMIZER CANDIDATES (all evaluated; best by strategy is selected)");
            foreach (var c in o.Candidates) sb.AppendLine((c.Selected ? " * " : "   ") + c.Policy.PadRight(34) + c.Boards + " boards, " + Fmt.N(c.PurchasedM3, "0.0000") + " m3, cost " + Fmt.N(c.Cost, "0.00"));
            return sb.ToString();
        }

        public static string ValidationText(ProjectResult r) => string.Join(Environment.NewLine, r.Issues.Select(i => i.ToString())) + Environment.NewLine;

        public static string JoineryMarkdown(WoodProject p, ProjectResult r)
        {
            var sb = new StringBuilder("# Joinery details\n\n");
            foreach (var j in r.Model.Joints)
            {
                var def = p.Joints.Get(j.JointTypeId);
                sb.AppendLine("## " + j.Id + " - " + def.Name + " (" + j.PartAId + " -> " + j.PartBId + ")");
                sb.AppendLine(j.Description + (j.Mitred ? " - tenon ends mitred" : ""));
                sb.AppendLine();
                sb.AppendLine("_" + def.Documentation + "_");
                sb.AppendLine();
                foreach (var kv in j.Parameters) sb.AppendLine("- " + kv.Key + ": " + Fmt.N(kv.Value));
                foreach (var f in r.Model.AllParts.SelectMany(x => x.Features).Where(f => f.SourceId == j.Id)) sb.AppendLine("- feature on " + f.PartId + ": " + f.Describe() + (f.ToolId != null ? " (tool " + f.ToolId + ")" : ""));
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public static string HardwareMarkdown(WoodProject p, ProjectResult r)
        {
            var sb = new StringBuilder("# Hardware installation details\n\n");
            foreach (var g in r.Model.HardwareInstalls.GroupBy(h => h.HardwareId))
            {
                var hw = p.Library.Hardware[g.Key];
                sb.AppendLine("## " + hw.Model + " x " + g.Count());
                sb.AppendLine(hw.InstallationNotes);
                sb.AppendLine();
                foreach (var h in g)
                {
                    sb.AppendLine("- " + h.Id + " on " + h.HostPartId + " at " + h.Point + ":");
                    foreach (var f in r.Model.AllParts.SelectMany(x => x.Features).Where(f => f.SourceId == h.Id)) sb.AppendLine("    - " + f.PartId + ": " + f.Describe() + " - " + f.Purpose + (f.ToolId != null ? " [" + f.ToolId + "]" : ""));
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public static string AssemblyMarkdown(ProjectResult r) =>
            "# Assembly sequence\n\n" + string.Join("\n", r.Manufacturing.AssemblySequence) + "\n";

        // ---------------------------------------------------------------- SVG
        private static readonly string[] Palette = { "#c0845a", "#7a9e7e", "#6f8fb5", "#b58fb0", "#d1b45c", "#8d8d8d", "#c46b6b", "#5fa8a8" };

        public static string CutPlanSvg(ProjectResult r)
        {
            var o = r.Optimization;
            double maxLen = o.Boards.Count == 0 ? 3000 : o.Boards.Max(b => b.Length);
            double scale = 900.0 / maxLen; int rowH = 44, left = 230;
            var fams = r.Model.Families.Select(f => f.Id).ToList();
            var sb = new StringBuilder();
            sb.AppendLine($"<svg xmlns='http://www.w3.org/2000/svg' width='{left + 920}' height='{o.Boards.Count * rowH + 40}' font-family='Segoe UI,Arial' font-size='11'>");
            int y = 10;
            foreach (var b in o.Boards)
            {
                sb.AppendLine($"<text x='4' y='{y + 16}' font-weight='bold'>{Fmt.Esc(b.Id)}{(b.IsReserve ? " (reserve)" : "")}</text><text x='4' y='{y + 30}' fill='#555'>{Fmt.Esc(b.Item.Label)} x {Fmt.N(b.Length, "0")}</text>");
                sb.AppendLine($"<rect x='{left}' y='{y}' width='{Fmt.N(b.Length * scale)}' height='30' fill='#f3ece2' stroke='#333'/>");
                foreach (var c in b.Cuts)
                {
                    var color = Palette[fams.IndexOf(c.Demand.FamilyId) % Palette.Length];
                    sb.AppendLine($"<rect x='{Fmt.N(left + c.Start * scale)}' y='{y}' width='{Fmt.N(c.Length * scale)}' height='30' fill='{color}' stroke='#333'><title>{Fmt.Esc(c.Demand.PartId)} {Fmt.N(c.Length)} mm</title></rect>");
                    sb.AppendLine($"<text x='{Fmt.N(left + c.Start * scale + 3)}' y='{y + 19}' fill='#fff' font-size='10'>{Fmt.Esc(c.Demand.PartId)} {Fmt.N(c.Length, "0")}</text>");
                }
                if (b.TailLength > 0)
                {
                    double tx = left + (b.Length - b.TailLength) * scale;
                    sb.AppendLine($"<rect x='{Fmt.N(tx)}' y='{y}' width='{Fmt.N(b.TailLength * scale)}' height='30' fill='{(b.TailClass == RemnantClass.ProjectRemnant ? "#e8f3df" : "#ddd")}' stroke='#333' stroke-dasharray='4 2'/>");
                    sb.AppendLine($"<text x='{Fmt.N(tx + 3)}' y='{y + 19}' fill='#333' font-size='10'>{(b.TailClass == RemnantClass.ProjectRemnant ? "REMNANT" : "SCRAP")} {Fmt.N(b.TailLength, "0")}</text>");
                }
                y += rowH;
            }
            sb.AppendLine("</svg>");
            return sb.ToString();
        }

        /// <summary>Orthographic view with overall dimensions. view: "top" (X,Y), "front" (X,Z), "side" (Y,Z).</summary>
        public static string DrawingSvg(ProjectResult r, string view)
        {
            Axis ah = view == "side" ? Axis.Y : Axis.X, av = view == "top" ? Axis.Y : Axis.Z;
            var bb = r.Model.Bounds; double w = bb.Size.Get(ah), h = bb.Size.Get(av);
            double scale = Math.Min(700 / w, 380 / h); int ox = 60, oy = 40;
            var sb = new StringBuilder();
            sb.AppendLine($"<svg xmlns='http://www.w3.org/2000/svg' width='{Fmt.N(w * scale + 140)}' height='{Fmt.N(h * scale + 110)}' font-family='Segoe UI,Arial' font-size='11'>");
            sb.AppendLine($"<text x='{ox}' y='20' font-weight='bold'>{view.ToUpperInvariant()} VIEW - {Fmt.Esc(r.Model.Name)}</text>");
            foreach (var p in r.Model.AllParts.OrderBy(p => view == "top" ? p.Bounds.Min.Z : view == "front" ? -p.Bounds.Min.Y : -p.Bounds.Min.X))
            {
                double x = ox + (p.Bounds.Min.Get(ah) - bb.Min.Get(ah)) * scale, wd = p.Bounds.Size.Get(ah) * scale;
                double ytop = oy + (bb.Max.Get(av) - p.Bounds.Max.Get(av)) * scale, ht = p.Bounds.Size.Get(av) * scale;
                if (view == "top") { ytop = oy + (p.Bounds.Min.Get(av) - bb.Min.Get(av)) * scale; }
                sb.AppendLine($"<rect x='{Fmt.N(x)}' y='{Fmt.N(ytop)}' width='{Fmt.N(wd)}' height='{Fmt.N(ht)}' fill='{(p.Id.StartsWith("TOP") ? "#e4cfae" : "#c0845a")}' fill-opacity='0.75' stroke='#222'><title>{Fmt.Esc(p.Id)}</title></rect>");
            }
            double yb = oy + h * scale + 25;
            sb.AppendLine($"<line x1='{ox}' y1='{Fmt.N(yb)}' x2='{Fmt.N(ox + w * scale)}' y2='{Fmt.N(yb)}' stroke='#036'/><text x='{Fmt.N(ox + w * scale / 2 - 20)}' y='{Fmt.N(yb + 14)}' fill='#036'>{Fmt.N(w, "0")} mm</text>");
            sb.AppendLine($"<line x1='{Fmt.N(ox + w * scale + 20)}' y1='{oy}' x2='{Fmt.N(ox + w * scale + 20)}' y2='{Fmt.N(oy + h * scale)}' stroke='#036'/><text x='{Fmt.N(ox + w * scale + 26)}' y='{Fmt.N(oy + h * scale / 2)}' fill='#036'>{Fmt.N(h, "0")}</text>");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }

        /// <summary>Oblique projection of assembled or exploded furniture (painter's algorithm on box faces).</summary>
        public static string ObliqueSvg(ProjectResult r, bool exploded)
        {
            Func<Vec3, (double, double)> proj = v => (v.X + 0.5 * v.Y, -(v.Z + 0.35 * v.Y));
            var items = new List<(PartInstance p, Vec3 off)>();
            foreach (var p in r.Model.AllParts)
            {
                Vec3 off = Vec3.Zero;
                if (exploded)
                {
                    var t = r.Model.FamilyOf(p.Id).Type;
                    off = t == PartType.Top ? new Vec3(0, 0, 220) : t == PartType.Leg ? new Vec3((p.Bounds.Center.X < r.Model.Bounds.Center.X ? -1 : 1) * 120, (p.Bounds.Center.Y < r.Model.Bounds.Center.Y ? -1 : 1) * 120, -120) : new Vec3(0, 0, 40);
                }
                items.Add((p, off));
            }
            var polys = new List<(double depth, string pts, string fill)>();
            foreach (var (p, off) in items)
            {
                var b = p.Bounds.Offset(off);
                Vec3 a = b.Min, c = b.Max;
                var corners = new Func<double, double, double, Vec3>((x, y, z) => new Vec3(x, y, z));
                Func<Vec3[], string, double, (double, string, string)> face = (vs, fill, depth) => (depth, string.Join(" ", vs.Select(v => { var q = proj(v); return Fmt.N(q.Item1 * 0.18 + 40) + "," + Fmt.N(q.Item2 * 0.18 + 300); })), fill);
                double d = (a.Y + c.Y) / 2 - 0.1 * (a.Z + c.Z) / 2 - 0.0005 * (a.X + c.X);
                string baseC = p.Id.StartsWith("TOP") ? "#e4cfae" : "#c0845a";
                polys.Add(face(new[] { corners(a.X, a.Y, a.Z), corners(c.X, a.Y, a.Z), corners(c.X, a.Y, c.Z), corners(a.X, a.Y, c.Z) }, baseC, d));              // front
                polys.Add(face(new[] { corners(a.X, a.Y, c.Z), corners(c.X, a.Y, c.Z), corners(c.X, c.Y, c.Z), corners(a.X, c.Y, c.Z) }, "#f0dcc0", d + 0.01));  // top
                polys.Add(face(new[] { corners(c.X, a.Y, a.Z), corners(c.X, c.Y, a.Z), corners(c.X, c.Y, c.Z), corners(c.X, a.Y, c.Z) }, "#a46f48", d + 0.02));  // right
            }
            var sb = new StringBuilder();
            sb.AppendLine("<svg xmlns='http://www.w3.org/2000/svg' width='760' height='460' font-family='Segoe UI,Arial' font-size='11'>");
            sb.AppendLine($"<text x='10' y='18' font-weight='bold'>{(exploded ? "EXPLODED" : "ASSEMBLED")} VIEW</text>");
            foreach (var pl in polys.OrderByDescending(x => x.depth)) sb.AppendLine($"<polygon points='{pl.pts}' fill='{pl.fill}' stroke='#222' stroke-width='0.6'/>");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }

        public static string SummaryHtml(WoodProject p, ProjectResult r)
        {
            var o = r.Optimization; var sb = new StringBuilder();
            sb.AppendLine("<!doctype html><html><head><meta charset='utf-8'><title>" + Fmt.Esc(p.Name) + " - project summary</title><style>body{font-family:Segoe UI,Arial;margin:24px;max-width:1100px}table{border-collapse:collapse;margin:8px 0}td,th{border:1px solid #bbb;padding:3px 8px;text-align:left}th{background:#eee}.err{color:#b00}.warn{color:#a60}.info{color:#555}pre{background:#f6f6f6;padding:10px;overflow:auto}</style></head><body>");
            sb.AppendLine("<h1>" + Fmt.Esc(p.Name) + "</h1><p>" + Fmt.Esc(p.Furniture.Name) + " - " + Fmt.Esc(p.Library.GetSpecies(p.SpeciesId).Name) + " - project " + Fmt.Esc(p.Id) + " - fingerprint " + r.Fingerprint + "</p>");
            sb.AppendLine("<h2>Parameters</h2><table>" + string.Join("", p.Parameters.Select(kv => "<tr><td>" + Fmt.Esc(kv.Key) + "</td><td>" + Fmt.N(kv.Value) + "</td></tr>")) + "</table>");
            foreach (var ov in p.Overrides) sb.AppendLine("<p>Override on <b>" + Fmt.Esc(ov.Key) + "</b>: " + ov.Value.Mode + " " + Fmt.N(ov.Value.Value) + "</p>");
            sb.AppendLine("<h2>Validation</h2><ul>" + string.Join("", r.Issues.Select(i => "<li class='" + i.Severity.ToString().ToLowerInvariant().Replace("warning", "warn") + "'>" + Fmt.Esc(i.ToString()) + "</li>")) + "</ul>");
            sb.AppendLine("<h2>Part families</h2><table><tr><th>Id</th><th>Name</th><th>Qty</th><th>Finished</th><th>Rough</th><th>Species</th></tr>" + string.Join("", r.Model.Families.Select(f => "<tr><td>" + f.Id + "</td><td>" + Fmt.Esc(f.Name) + "</td><td>" + f.Quantity + "</td><td>" + Dim(f.Finished) + "</td><td>" + string.Join(" + ", f.RoughPieces.Select(x => x.CountPerPart + " x " + Dim(x.Rough))) + "</td><td>" + f.SpeciesId + "</td></tr>")) + "</table>");
            sb.AppendLine("<h2>Material optimization</h2><pre>" + Fmt.Esc(OptimizationText(r)) + "</pre>");
            sb.AppendLine("<h2>Visual cut plan</h2>" + CutPlanSvg(r));
            sb.AppendLine("<h2>Drawings</h2>" + DrawingSvg(r, "top") + DrawingSvg(r, "front") + DrawingSvg(r, "side") + ObliqueSvg(r, false) + ObliqueSvg(r, true));
            sb.AppendLine("<h2>Bill of materials</h2><table><tr><th>Category</th><th>Id</th><th>Description</th><th>Qty</th><th>Unit</th><th>Unit cost</th><th>Total</th></tr>" + string.Join("", r.Bom.Lines.Select(l => "<tr><td>" + l.Category + "</td><td>" + Fmt.Esc(l.Id) + "</td><td>" + Fmt.Esc(l.Description) + "</td><td>" + Fmt.N(l.Quantity, "0.###") + "</td><td>" + l.Unit + "</td><td>" + Fmt.N(l.UnitCost, "0.00") + "</td><td>" + Fmt.N(l.Total, "0.00") + "</td></tr>")) + "</table>");
            sb.AppendLine("<h2>Cost</h2><table><tr><td>Raw material (optimized purchase)</td><td>" + Fmt.N(r.Cost.RawMaterial, "0.00") + "</td></tr><tr><td>Hardware + fasteners</td><td>" + Fmt.N(r.Cost.Hardware, "0.00") + "</td></tr><tr><td>Adhesive + finish</td><td>" + Fmt.N(r.Cost.Consumables, "0.00") + "</td></tr><tr><td>Estimated labor (" + Fmt.N(r.Manufacturing.TotalMinutes / 60, "0.0") + " h)</td><td>" + Fmt.N(r.Cost.Labor, "0.00") + "</td></tr><tr><th>Total (" + r.Cost.Currency + ")</th><th>" + Fmt.N(r.Cost.Total, "0.00") + "</th></tr><tr><td class='info'>of which estimated waste</td><td class='info'>" + Fmt.N(r.Cost.EstimatedWasteCost, "0.00") + "</td></tr></table>");
            sb.AppendLine("<h2>Assembly sequence</h2><pre>" + Fmt.Esc(string.Join("\n", r.Manufacturing.AssemblySequence)) + "</pre>");
            sb.AppendLine("<h2>Traceability</h2><pre>" + Fmt.Esc(TraceabilityText(r)) + "</pre></body></html>");
            return sb.ToString();
        }
    }

    public static class ReportWriter
    {
        public static List<string> WriteAll(string dir, WoodProject p, ProjectResult r)
        {
            Directory.CreateDirectory(dir);
            var written = new List<string>();
            void W(string name, string content) { var path = Path.Combine(dir, name); File.WriteAllText(path, content, new UTF8Encoding(false)); written.Add(path); }
            W("bom.csv", ReportBuilder.BomCsv(r));
            W("part_families.csv", ReportBuilder.FamilyListCsv(r));
            W("cutlist.csv", ReportBuilder.CutListCsv(r));
            W("procurement.csv", ReportBuilder.ProcurementCsv(r));
            W("manufacturing_operations.csv", ReportBuilder.ManufacturingCsv(r));
            W("traceability.txt", ReportBuilder.TraceabilityText(r));
            W("optimization.txt", ReportBuilder.OptimizationText(r));
            W("validation.txt", ReportBuilder.ValidationText(r));
            W("joinery.md", ReportBuilder.JoineryMarkdown(p, r));
            W("hardware.md", ReportBuilder.HardwareMarkdown(p, r));
            W("assembly.md", ReportBuilder.AssemblyMarkdown(r));
            W("cutplan.svg", ReportBuilder.CutPlanSvg(r));
            W("drawing_top.svg", ReportBuilder.DrawingSvg(r, "top"));
            W("drawing_front.svg", ReportBuilder.DrawingSvg(r, "front"));
            W("drawing_side.svg", ReportBuilder.DrawingSvg(r, "side"));
            W("view_assembled.svg", ReportBuilder.ObliqueSvg(r, false));
            W("view_exploded.svg", ReportBuilder.ObliqueSvg(r, true));
            W("project_summary.html", ReportBuilder.SummaryHtml(p, r));
            W("project.rhinowood.json", ProjectSerializer.Serialize(p, r));
            var pdf = PdfExporter.ExportSheets(p, r, Path.Combine(dir, "planse.pdf"), SheetMode.Design);
            written.Add(pdf.ok ? Path.Combine(dir, "planse.pdf") : Path.Combine(dir, "planse.html"));
            var sale = PdfExporter.ExportSheets(p, r, Path.Combine(dir, "oferta.pdf"), SheetMode.Sale);
            written.Add(sale.ok ? Path.Combine(dir, "oferta.pdf") : Path.Combine(dir, "oferta.html"));
            return written;
        }
    }
}
