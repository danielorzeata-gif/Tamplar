using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using RhinoWood.Core.Display;
using RhinoWood.Core.Domain;
using RhinoWood.Core.Geometry;
using RhinoWood.Core.Optimization;
using RhinoWood.Core.Projects;
using RhinoWood.Core.Rules;

namespace RhinoWood.Core.Reports
{
    /// <summary>DESIGN = workshop documents (cut plan, costs, waste). SALE = customer documents: never internal costs, stock codes or waste.</summary>
    public enum SheetMode { Design, Sale }

    public sealed class Sheet
    {
        public string Id { get; set; }
        public string Title { get; set; }
        /// <summary>Inner HTML of the sheet body (may embed SVG). The page frame (title, footer, page number) is added by <see cref="SheetDocument"/>.</summary>
        public string Body { get; set; }
        /// <summary>True for the sheet shown live in the working interface (technical sheet).</summary>
        public bool IsLivePreview { get; set; }
    }

    /// <summary>Atelier design tokens (light theme) used by all sheets.</summary>
    internal static class Atelier
    {
        public const string Surface = "#f3f1ed", Raised = "#ffffff", Sunken = "#e7e3dc", Line = "#d3cdc3", LineStrong = "#8c8376",
            Ink = "#1f1b16", Muted = "#5f574c", Accent = "#9c5410", AccentSoft = "#f2e3d1", Waste = "#c23a22", Ok = "#1f5f8f", Warn = "#8a5d00", Danger = "#b3261e",
            Economa = "#5f6b72", Standard = "#2e6e62", Premium = "#5a3a22";
        public const string Sans = "'Segoe UI', system-ui, -apple-system, Arial, sans-serif";
        public const string Mono = "'Cascadia Mono', Consolas, 'SF Mono', 'DejaVu Sans Mono', monospace";
    }

    public static class Money
    {
        /// <summary>"1 840 lei": thin-space thousands, whole numbers (Atelier content rules).</summary>
        public static string Format(double v, string currency)
        {
            var s = Math.Round(v).ToString("#,##0", CultureInfo.InvariantCulture).Replace(',', ' ');
            return s + " " + currency;
        }
    }

    public static class SheetBuilder
    {
        private static string N(double v, string f = "0") => v.ToString(f, CultureInfo.InvariantCulture);
        private static string E(string s) => WebUtility.HtmlEncode(s ?? "");

        public static List<Sheet> Build(WoodProject p, ProjectResult r, SheetMode mode = SheetMode.Design, string variantName = null)
        {
            variantName = variantName ?? TierName(p);
            var list = new List<Sheet> { TechnicalSheet(p, r, mode, variantName) };
            if (mode == SheetMode.Sale) { list.Add(OfferSheet(p, r, variantName)); return list; }
            list.AddRange(JointSheets(p, r));
            list.AddRange(CuttingSheets(p, r));
            list.Add(AssemblySheet(p, r));
            return list;
        }

        public static string TierName(WoodProject p) => p.Tier == null ? null : p.Tier + (p.TierMatches ? "" : " (modificat)");

        // ================================================================= 1. technical sheet (also the live preview in the interface)
        public static Sheet TechnicalSheet(WoodProject p, ProjectResult r, SheetMode mode, string variant)
        {
            var m = r.Model; var bb = m.Bounds;
            var famNo = new Dictionary<string, int>(); int k = 0;
            foreach (var f in m.Families) famNo[f.Id] = ++k;

            // common scale for the three orthographic views, rounded to a standard ratio
            var regions = new[]
            {
                (h: Axis.X, v: Axis.Y, x: 0.0, y: 0.0, w: 185.0, hh: 62.0, t: "VEDERE DE SUS"),
                (h: Axis.X, v: Axis.Z, x: 0.0, y: 68.0, w: 185.0, hh: 78.0, t: "VEDERE DIN FAȚĂ"),
                (h: Axis.Y, v: Axis.Z, x: 195.0, y: 0.0, w: 82.0, hh: 70.0, t: "VEDERE LATERALĂ"),
            };
            double fit = regions.Min(g => Math.Min((g.w - 12) / bb.Size.Get(g.h), (g.hh - 14) / bb.Size.Get(g.v)));
            double[] denoms = { 1, 2, 2.5, 5, 10, 15, 20, 25, 30, 40, 50, 75, 100, 150, 200 };
            double denom = denoms.FirstOrDefault(d => 1.0 / d <= fit); if (denom == 0) denom = 200;
            double s = 1.0 / denom;

            var sb = new StringBuilder();
            sb.Append("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 277 178' width='277mm' height='178mm' font-family=\"" + Atelier.Sans + "\">");
            foreach (var g in regions) DrawView(sb, m, bb, g.h, g.v, g.x, g.y, g.w, g.hh, s, g.t, famNo);

            // small oblique overview
            var polys = PreviewEngine.Build(m, DisplayMode.Normal, false);
            var (minX, minY, maxX, maxY) = PreviewEngine.Extent(polys);
            double ow = 95, oh = 28, oy0 = 150;
            double os = Math.Min(ow / (maxX - minX), oh / (maxY - minY));
            sb.Append($"<text x='0' y='{N(oy0 - 1.5, "0.0")}' font-size='2.6' font-weight='600' fill='{Atelier.Muted}'>VEDERE ORIENTATIVĂ</text>");
            foreach (var pl in polys)
            {
                var pts = string.Join(" ", pl.X.Select((x, i) => N((x - minX) * os, "0.00") + "," + N(oy0 + (pl.Y[i] - minY) * os, "0.00")));
                sb.Append($"<polygon points='{pts}' fill='{pl.Fill ?? "none"}' stroke='{Atelier.Ink}' stroke-width='0.12'/>");
            }

            // materials list
            double mx = 195, my = 78;
            sb.Append($"<text x='{mx}' y='{my}' font-size='2.6' font-weight='600' fill='{Atelier.Muted}'>MATERIALE</text>");
            sb.Append($"<line x1='{mx}' y1='{my + 1.5}' x2='277' y2='{my + 1.5}' stroke='{Atelier.LineStrong}' stroke-width='0.2'/>");
            double ry = my + 6;
            foreach (var f in m.Families)
            {
                sb.Append($"<circle cx='{mx + 1.8}' cy='{N(ry - 1, "0.0")}' r='1.7' fill='none' stroke='{Atelier.Accent}' stroke-width='0.25'/><text x='{mx + 1.8}' y='{N(ry - 0.1, "0.0")}' font-size='2.1' text-anchor='middle' fill='{Atelier.Accent}' font-family=\"{Atelier.Mono}\">{famNo[f.Id]}</text>");
                sb.Append($"<text x='{mx + 5}' y='{N(ry, "0.0")}' font-size='2.8' fill='{Atelier.Ink}'>{E(Ro.Family(f))}</text>");
                sb.Append($"<text x='277' y='{N(ry, "0.0")}' font-size='2.8' text-anchor='end' fill='{Atelier.Ink}' font-family=\"{Atelier.Mono}\">{f.Quantity} buc · {N(f.Finished.Length)}×{N(f.Finished.Width)}×{N(f.Finished.Thickness)}</text>");
                sb.Append($"<text x='{mx + 5}' y='{N(ry + 3.2, "0.0")}' font-size='2.3' fill='{Atelier.Muted}'>{E(SpeciesName(p, f.SpeciesId))}{(f.RoughPieces.Sum(q => q.CountPerPart) > 1 ? " · " + f.RoughPieces.Sum(q => q.CountPerPart) + " lamele încleiate" : "")}</text>");
                ry += 8.5;
            }

            // construction notes
            double nx = 105, ny = 151;
            sb.Append($"<text x='{nx}' y='{ny - 1.5}' font-size='2.6' font-weight='600' fill='{Atelier.Muted}'>ÎMBINĂRI ȘI FERONERIE</text>");
            var lines = new List<string>();
            foreach (var g in m.Joints.GroupBy(j => j.JointTypeId))
            {
                var def = p.Joints.Get(g.Key);
                lines.Add(g.Count() + " × " + Ro.Joint(def));
            }
            foreach (var g in m.HardwareInstalls.GroupBy(h => h.HardwareId)) lines.Add(g.Count() + " × " + Ro.HardwareName(g.Key, p.Library.Hardware.TryGetValue(g.Key, out var hw) ? hw.Model : g.Key));
            double yy = ny + 2.5;
            foreach (var l in lines.Take(8)) { sb.Append($"<text x='{nx}' y='{N(yy, "0.0")}' font-size='2.7' fill='{Atelier.Ink}'>{E(l)}</text>"); yy += 3.6; }
            sb.Append($"<text x='277' y='176' font-size='2.4' text-anchor='end' fill='{Atelier.Muted}' font-family=\"{Atelier.Mono}\">Scara 1:{N(denom, denom % 1 == 0 ? "0" : "0.0")} · cote în mm</text>");
            sb.Append("</svg>");

            string title = p.Name + " · fișă tehnică";
            return new Sheet { Id = "tech", Title = title, Body = sb.ToString(), IsLivePreview = true };
        }

        private static string SpeciesName(WoodProject p, string id) => Ro.SpeciesName(id, p.Library.Species.TryGetValue(id, out var s) ? s.Name : id);

        private static void DrawView(StringBuilder sb, FurnitureModel m, Box3 bb, Axis h, Axis v, double rx, double ry, double rw, double rh, double s, string title, Dictionary<string, int> famNo)
        {
            double ext_h = bb.Size.Get(h) * s, ext_v = bb.Size.Get(v) * s;
            double ox = rx + 2, oy = ry + 7;
            sb.Append($"<text x='{rx}' y='{ry + 3}' font-size='2.6' font-weight='600' fill='{Atelier.Muted}'>{title}</text>");
            sb.Append($"<line x1='{rx}' y1='{ry + 4.2}' x2='{rx + rw}' y2='{ry + 4.2}' stroke='{Atelier.Line}' stroke-width='0.2'/>");
            var seen = new HashSet<string>();
            foreach (var part in m.AllParts.OrderBy(q => q.Bounds.Center.Get(h == Axis.X && v == Axis.Y ? Axis.Z : h == Axis.Y ? Axis.X : Axis.Y)))
            {
                var b = part.Bounds;
                double x = ox + (b.Min.Get(h) - bb.Min.Get(h)) * s, y = oy + (bb.Max.Get(v) - b.Max.Get(v)) * s;
                double w = b.Size.Get(h) * s, hgt = b.Size.Get(v) * s;
                var fam = m.FamilyOf(part.Id);
                bool top = fam.Type == PartType.Top;
                sb.Append($"<rect x='{N(x, "0.00")}' y='{N(y, "0.00")}' width='{N(w, "0.00")}' height='{N(hgt, "0.00")}' fill='{(top ? "#e4cfae" : "#c0845a")}' fill-opacity='{(top && v == Axis.Y ? "0.35" : "0.85")}' stroke='{Atelier.Ink}' stroke-width='0.2'/>");
                if (seen.Add(fam.Id))
                {
                    double cx = x + w / 2, cy = y + hgt / 2;
                    sb.Append($"<circle cx='{N(cx, "0.0")}' cy='{N(cy, "0.0")}' r='1.7' fill='{Atelier.Raised}' stroke='{Atelier.Accent}' stroke-width='0.25'/><text x='{N(cx, "0.0")}' y='{N(cy + 0.8, "0.0")}' font-size='2.1' text-anchor='middle' fill='{Atelier.Accent}' font-family=\"{Atelier.Mono}\">{famNo[fam.Id]}</text>");
                }
            }
            // overall dimensions
            Dim(sb, ox, oy + ext_v + 5, ox + ext_h, oy + ext_v + 5, N(bb.Size.Get(h)), false);
            Dim(sb, ox + ext_h + 5, oy, ox + ext_h + 5, oy + ext_v, N(bb.Size.Get(v)), true);
        }

        private static void Dim(StringBuilder sb, double x1, double y1, double x2, double y2, string text, bool vertical)
        {
            sb.Append($"<line x1='{N(x1, "0.00")}' y1='{N(y1, "0.00")}' x2='{N(x2, "0.00")}' y2='{N(y2, "0.00")}' stroke='{Atelier.Ok}' stroke-width='0.2'/>");
            double t = 1.2;
            if (vertical) { sb.Append($"<line x1='{N(x1 - t, "0.00")}' y1='{N(y1, "0.00")}' x2='{N(x1 + t, "0.00")}' y2='{N(y1, "0.00")}' stroke='{Atelier.Ok}' stroke-width='0.2'/><line x1='{N(x2 - t, "0.00")}' y1='{N(y2, "0.00")}' x2='{N(x2 + t, "0.00")}' y2='{N(y2, "0.00")}' stroke='{Atelier.Ok}' stroke-width='0.2'/>"); }
            else { sb.Append($"<line x1='{N(x1, "0.00")}' y1='{N(y1 - t, "0.00")}' x2='{N(x1, "0.00")}' y2='{N(y1 + t, "0.00")}' stroke='{Atelier.Ok}' stroke-width='0.2'/><line x1='{N(x2, "0.00")}' y1='{N(y2 - t, "0.00")}' x2='{N(x2, "0.00")}' y2='{N(y2 + t, "0.00")}' stroke='{Atelier.Ok}' stroke-width='0.2'/>"); }
            double cx = (x1 + x2) / 2, cy = (y1 + y2) / 2;
            if (vertical) sb.Append($"<text x='{N(cx + 2.4, "0.00")}' y='{N(cy, "0.00")}' font-size='2.7' fill='{Atelier.Ok}' font-family=\"{Atelier.Mono}\" transform='rotate(-90 {N(cx + 2.4, "0.00")} {N(cy, "0.00")})' text-anchor='middle'>{text}</text>");
            else sb.Append($"<text x='{N(cx, "0.00")}' y='{N(cy + 3.4, "0.00")}' font-size='2.7' fill='{Atelier.Ok}' font-family=\"{Atelier.Mono}\" text-anchor='middle'>{text}</text>");
        }

        // ================================================================= 2. joint dimensions
        private static IEnumerable<Sheet> JointSheets(WoodProject p, ProjectResult r)
        {
            var m = r.Model;
            var groups = m.Joints.GroupBy(j => j.JointTypeId + "|" + string.Join(";", j.Parameters.OrderBy(x => x.Key).Select(x => x.Key + "=" + N(x.Value, "0.##")))).ToList();
            if (groups.Count == 0) yield break;
            int page = 0;
            foreach (var chunk in groups.Select((g, i) => (g, i)).GroupBy(x => x.i / 2))
            {
                var sb = new StringBuilder();
                foreach (var (g, _) in chunk)
                {
                    var first = g.First(); var def = p.Joints.Get(first.JointTypeId);
                    sb.Append("<div class='block'>");
                    sb.Append($"<h2>{E(Ro.Joint(def))} <span class='muted'>· {g.Count()} îmbinări: {E(string.Join(", ", g.Select(j => j.Id + " (" + j.PartAId + "→" + j.PartBId + ")")))}</span></h2>");
                    sb.Append("<div class='cols'>");
                    sb.Append("<div class='drw'>" + JointDrawing(m, first) + "</div>");
                    sb.Append("<div class='tbl'>");
                    sb.Append("<table><tr><th>Parametru</th><th class='num'>Valoare</th></tr>");
                    foreach (var kv in first.Parameters.OrderBy(x => x.Key)) sb.Append($"<tr><td>{E(Label(kv.Key))}</td><td class='num'>{N(kv.Value, "0.##")} mm</td></tr>");
                    sb.Append("</table>");
                    sb.Append("<table><tr><th>Operație</th><th>Piesa</th><th class='num'>Dimensiuni</th><th>Sculă</th></tr>");
                    foreach (var f in m.AllParts.SelectMany(q => q.Features).Where(f => f.SourceId == first.Id))
                        sb.Append($"<tr><td>{E(Ro.Feature(f.Kind))}</td><td>{E(f.PartId)}</td><td class='num'>{E(FeatureSize(f))}</td><td>{E(f.ToolId ?? "")}</td></tr>");
                    sb.Append("</table>");
                    sb.Append(JointChecks(p, m, first, def));
                    sb.Append("</div></div></div>");
                }
                page++;
                yield return new Sheet { Id = "joints" + page, Title = p.Name + " · cote de îmbinare", Body = sb.ToString() };
            }
        }

        private static string Label(string key)
        {
            switch (key)
            {
                case "tenonThickness": return "Grosime cep";
                case "tenonWidth": return "Lățime cep";
                case "tenonLength": return "Lungime cep";
                case "mortiseDepth": return "Adâncime mortază";
                case "dowelCount": return "Număr dibluri";
                case "fingerCount": return "Număr dinți";
                case "fingerWidth": return "Lățime dinte";
                case "looseTenonLength": return "Lungime cep liber";
                case "scarfLength": return "Lungime înclinare";
                case "depth": return "Adâncime";
                default: return key;
            }
        }

        private static string FeatureSize(Feature f) => f.HasBox
            ? N(f.Box.Size.X, "0.#") + "×" + N(f.Box.Size.Y, "0.#") + "×" + N(f.Box.Size.Z, "0.#")
            : "Ø" + N(f.Diameter, "0.#") + " · adânc. " + N(f.Depth, "0.#");

        private static string JointChecks(WoodProject p, FurnitureModel m, JointInstance j, Joinery.IJointDefinition def)
        {
            var sb = new StringBuilder("<div class='checks'>");
            sb.Append($"<div><b>Rezistență</b> {new string('●', def.Info.Strength)}{new string('○', 5 - def.Info.Strength)} · dificultate {new string('●', def.Info.Difficulty)}{new string('○', 5 - def.Info.Difficulty)} · {(def.Info.VisibleFromOutside ? "aparentă" : "ascunsă")}</div>");
            var tenon = m.FindPart(j.PartAId)?.Features.FirstOrDefault(f => f.SourceId == j.Id && f.Kind == FeatureKind.Tenon);
            if (tenon != null)
            {
                var a = m.FindPart(j.PartAId);
                double t = tenon.Box.Size.Z, w = tenon.Box.Size.Y, l = tenon.Box.Size.X;
                var chk = SafetyRules.CheckTenonGeometry(l, w, t, a.Finished.Thickness);
                string r5 = !chk.InDomain
                    ? "<span class='warn'>▲ lățimea cepului (" + N(w, "0.#") + " mm) e în afara domeniului validat (15–25 mm): raportul lungime/lățime nu se evaluează; necesită încercare</span>"
                    : chk.LengthOk && chk.ThicknessOk ? "<span class='ok'>● conform</span>" : "<span class='warn'>▲ " + E(chk.Message) + "</span>";
                sb.Append($"<div><b>R5 geometrie cep</b> grosime {N(t, "0.#")} mm = {N(t / a.Finished.Thickness, "0.00")} din grosimea traversei (1/3–1/2) · {r5}</div>");
                var fam = m.FamilyOf(j.PartAId);
                var sp = p.Library.Species.TryGetValue(fam.SpeciesId, out var spp) ? spp : null;
                if (sp?.ShearStrengthMPa != null)
                {
                    var mo = SafetyRules.TenonMomentE1(w, l, 10, sp.ShearStrengthMPa.Value);
                    sb.Append($"<div><b>R3 capacitate</b> {(mo.InDomain ? "moment mediu ≈ " + N(mo.MeanNm, "0") + " N·m, de calcul ≈ " + N(mo.DesignNm, "0") + " N·m" : "<span class='warn'>▲ în afara domeniului validat — necesită încercare EN 1728/1730; nu se afișează valoare</span>")}</div>");
                }
                else sb.Append("<div><b>R3 capacitate</b> <span class='warn'>▲ fără date de forfecare pentru această specie — necesită încercare EN 1728/1730</span></div>");
                if (j.Mitred) sb.Append("<div><b>Capete cep</b> teșite la 45° (mortazele se intersectează în picior)</div>");
            }
            if (def.Id == "japanese-kusabi" || def.Id == "bridle") sb.Append("<div class='muted'>R19: îmbinare fără date de rezistență publicate — doar tipologic, fără capacitate numerică.</div>");
            sb.Append("</div>");
            return sb.ToString();
        }

        /// <summary>Schematic drawings of the tenon (end + side view) and the mortise (face view) of a joint.</summary>
        private static string JointDrawing(FurnitureModel m, JointInstance j)
        {
            var a = m.FindPart(j.PartAId); var b = m.FindPart(j.PartBId);
            var tenon = a.Features.FirstOrDefault(f => f.SourceId == j.Id && f.Kind == FeatureKind.Tenon);
            var mort = b.Features.FirstOrDefault(f => f.SourceId == j.Id && (f.Kind == FeatureKind.Mortise || f.Kind == FeatureKind.Slot));
            if (tenon == null || mort == null) return "<div class='muted'>Detaliu grafic indisponibil pentru acest tip (vezi tabelul).</div>";
            double W = a.Finished.Width, T = a.Finished.Thickness;
            double sc = 38.0 / Math.Max(W, 2 * tenon.Box.Size.X);
            var sb = new StringBuilder($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 150 62' width='150mm' height='62mm' font-family=\"{Atelier.Sans}\">");
            // end view
            double x0 = 6, y0 = 10;
            sb.Append($"<text x='{x0}' y='6' font-size='2.5' font-weight='600' fill='{Atelier.Muted}'>CEP — CAPĂT</text>");
            sb.Append($"<rect x='{x0}' y='{y0}' width='{N(T * sc, "0.0")}' height='{N(W * sc, "0.0")}' fill='#e4cfae' stroke='{Atelier.Ink}' stroke-width='0.25'/>");
            double tx = x0 + tenon.Box.Min.Z * sc, ty = y0 + tenon.Box.Min.Y * sc;
            sb.Append($"<rect x='{N(tx, "0.0")}' y='{N(ty, "0.0")}' width='{N(tenon.Box.Size.Z * sc, "0.0")}' height='{N(tenon.Box.Size.Y * sc, "0.0")}' fill='#c0845a' stroke='{Atelier.Accent}' stroke-width='0.3'/>");
            Dim(sb, tx, y0 + W * sc + 3.5, tx + tenon.Box.Size.Z * sc, y0 + W * sc + 3.5, N(tenon.Box.Size.Z, "0.#"), false);
            Dim(sb, x0 - 3.5, ty, x0 - 3.5, ty + tenon.Box.Size.Y * sc, N(tenon.Box.Size.Y, "0.#"), true);
            Dim(sb, x0 + T * sc + 3.5, y0, x0 + T * sc + 3.5, y0 + W * sc, N(W, "0.#"), true);
            // side view
            double sx = 60;
            double len = 2 * tenon.Box.Size.X;
            sb.Append($"<text x='{sx}' y='6' font-size='2.5' font-weight='600' fill='{Atelier.Muted}'>CEP — LATERAL</text>");
            sb.Append($"<rect x='{sx}' y='{y0}' width='{N(len * sc, "0.0")}' height='{N(W * sc, "0.0")}' fill='#e4cfae' stroke='{Atelier.Ink}' stroke-width='0.25'/>");
            sb.Append($"<rect x='{N(sx + tenon.Box.Size.X * sc, "0.0")}' y='{N(y0 + tenon.Box.Min.Y * sc, "0.0")}' width='{N(tenon.Box.Size.X * sc, "0.0")}' height='{N(tenon.Box.Size.Y * sc, "0.0")}' fill='#c0845a' stroke='{Atelier.Accent}' stroke-width='0.3'/>");
            Dim(sb, sx + tenon.Box.Size.X * sc, y0 + W * sc + 3.5, sx + len * sc, y0 + W * sc + 3.5, N(tenon.Box.Size.X, "0.#"), false);
            Dim(sb, sx + len * sc + 3.5, y0 + tenon.Box.Min.Y * sc, sx + len * sc + 3.5, y0 + (tenon.Box.Min.Y + tenon.Box.Size.Y) * sc, N(tenon.Box.Size.Y, "0.#"), true);
            // mortise face view: vertical = mortise local X, horizontal = the thin cross axis
            double mx = 108;
            double thin = Math.Min(mort.Box.Size.Y, mort.Box.Size.Z), deep = Math.Max(mort.Box.Size.Y, mort.Box.Size.Z);
            double mlen = mort.Box.Size.X;
            double s2 = 38.0 / Math.Max(mlen * 1.6, 1);
            sb.Append($"<text x='{mx}' y='6' font-size='2.5' font-weight='600' fill='{Atelier.Muted}'>MORTAZĂ — FAȚĂ</text>");
            double pw = Math.Max(thin * s2 * 5, 14), ph = mlen * 1.6 * s2;
            sb.Append($"<rect x='{mx}' y='{y0}' width='{N(pw, "0.0")}' height='{N(ph, "0.0")}' fill='#e4cfae' stroke='{Atelier.Ink}' stroke-width='0.25'/>");
            double mw = thin * s2, mh = mlen * s2;
            sb.Append($"<rect x='{N(mx + (pw - mw) / 2, "0.0")}' y='{N(y0 + (ph - mh) / 2, "0.0")}' width='{N(mw, "0.0")}' height='{N(mh, "0.0")}' fill='{Atelier.Waste}' fill-opacity='0.55' stroke='{Atelier.Waste}' stroke-width='0.3'/>");
            Dim(sb, mx + pw + 3.5, y0 + (ph - mh) / 2, mx + pw + 3.5, y0 + (ph + mh) / 2, N(mlen, "0.#"), true);
            Dim(sb, mx + (pw - mw) / 2, y0 + ph + 3.5, mx + (pw + mw) / 2, y0 + ph + 3.5, N(thin, "0.#"), false);
            sb.Append($"<text x='{mx}' y='{N(y0 + ph + 12, "0.0")}' font-size='2.6' fill='{Atelier.Ink}' font-family=\"{Atelier.Mono}\">adâncime {N(mort.Depth, "0.#")}</text>");
            sb.Append("</svg>");
            return sb.ToString();
        }

        // ================================================================= 3. cutting plan (workshop only)
        private static IEnumerable<Sheet> CuttingSheets(WoodProject p, ProjectResult r)
        {
            var o = r.Optimization;
            var boards = o.Boards.ToList();
            double maxLen = boards.Count == 0 ? 3000 : boards.Max(b => b.Length);
            int per = 11, page = 0;
            var chunks = boards.Select((b, i) => (b, i)).GroupBy(x => x.i / per).ToList();
            foreach (var ch in chunks)
            {
                var sb = new StringBuilder();
                double w = 205, sc = w / maxLen, rowH = 13;
                sb.Append($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 277 {N(ch.Count() * rowH + 4)}' width='277mm' font-family=\"{Atelier.Sans}\">");
                int row = 0;
                var fams = r.Model.Families.Select(f => f.Id).ToList();
                string[] pal = { "#c0845a", "#7a9e7e", "#6f8fb5", "#b58fb0", "#d1b45c", "#8d8d8d" };
                foreach (var (b, _) in ch)
                {
                    double y = row * rowH + 2;
                    sb.Append($"<text x='0' y='{N(y + 4)}' font-size='3' font-weight='600' fill='{Atelier.Ink}'>{E(b.Id)}{(b.IsReserve ? " · rezervă" : "")}</text>");
                    sb.Append($"<text x='0' y='{N(y + 8)}' font-size='2.4' fill='{Atelier.Muted}' font-family=\"{Atelier.Mono}\">{E(b.Item.Label)} × {N(b.Length)}</text>");
                    sb.Append($"<rect x='45' y='{N(y)}' width='{N(b.Length * sc, "0.0")}' height='9' fill='{Atelier.Raised}' stroke='{Atelier.LineStrong}' stroke-width='0.25'/>");
                    foreach (var c in b.Cuts)
                    {
                        var col = pal[Math.Max(0, fams.IndexOf(c.Demand.FamilyId)) % pal.Length];
                        sb.Append($"<rect x='{N(45 + c.Start * sc, "0.0")}' y='{N(y)}' width='{N(c.Length * sc, "0.0")}' height='9' fill='{col}' stroke='{Atelier.Ink}' stroke-width='0.2'/>");
                        sb.Append($"<text x='{N(45 + c.Start * sc + 0.8, "0.0")}' y='{N(y + 5.6, "0.0")}' font-size='2.3' fill='#fff' font-family=\"{Atelier.Mono}\">{E(c.Demand.PartId)} {N(c.Length)}</text>");
                    }
                    if (b.TailLength > 0)
                    {
                        double tx = 45 + (b.Length - b.TailLength) * sc;
                        bool reuse = b.TailClass == RemnantClass.ProjectRemnant;
                        sb.Append($"<rect x='{N(tx, "0.0")}' y='{N(y)}' width='{N(b.TailLength * sc, "0.0")}' height='9' fill='{(reuse ? "#e8f3df" : Atelier.Waste)}' fill-opacity='{(reuse ? "1" : "0.35")}' stroke='{(reuse ? Atelier.LineStrong : Atelier.Waste)}' stroke-width='0.25' stroke-dasharray='1.2 0.8'/>");
                        sb.Append($"<text x='{N(tx + 0.8, "0.0")}' y='{N(y + 5.6, "0.0")}' font-size='2.3' fill='{(reuse ? Atelier.Muted : Atelier.Waste)}' font-family=\"{Atelier.Mono}\">{(reuse ? "rest" : "deșeu")} {N(b.TailLength)}</text>");
                    }
                    row++;
                }
                sb.Append("</svg>");
                page++;
                var body = new StringBuilder("<p class='muted'>Ordinea de tăiere: de la stânga la dreapta; kerf " + N(p.Settings.Rules.SawKerf, "0.#") + " mm între piese. Deșeul este colorat roșu; restul reutilizabil (≥ " + N(p.Settings.Rules.MinReusableRemnant) + " mm) rămâne disponibil pentru alte piese.</p>");
                body.Append(sb);
                yield return new Sheet { Id = "cut" + page, Title = p.Name + " · plan de debitare", Body = body.ToString() };
            }

            // table of cuts + purchase summary
            var rows = new List<string>();
            double totalWaste = 0; int totalQty = 0;
            foreach (var b in boards.Where(x => !x.IsReserve))
                for (int i = 0; i < b.Cuts.Count; i++)
                {
                    var c = b.Cuts[i];
                    var fam = r.Model.Families.First(f => f.Id == c.Demand.FamilyId);
                    bool last = i == b.Cuts.Count - 1;
                    double waste = last && b.TailClass == RemnantClass.Scrap ? b.TailLength : 0;
                    totalWaste += waste; totalQty++;
                    rows.Add($"<tr><td>{E(c.Demand.PartId)}</td><td>{E(Ro.Family(fam))}</td><td class='num'>{N(c.Demand.SecB)}×{N(c.Demand.SecA)}</td><td class='num'>{N(c.Length)}</td><td class='num'>1</td><td class='num'>{E(b.Id)} · {E(b.Item.Id)}</td><td class='num waste'>{(waste > 0 ? N(waste) : "–")}</td></tr>");
                }
            int rowsPer = 24, tp = 0;
            var pages = rows.Select((x, i) => (x, i)).GroupBy(x => x.i / rowsPer).ToList();
            foreach (var pg in pages)
            {
                var sb = new StringBuilder("<table class='wide'><tr><th>Piesă</th><th>Denumire</th><th class='num'>Secț. brută (gros×lăț)</th><th class='num'>L (mm)</th><th class='num'>Buc</th><th>Bară</th><th class='num'>Deșeu (mm)</th></tr>");
                foreach (var x in pg) sb.Append(x.x);
                bool lastPage = pg.Key == pages.Count - 1;
                if (lastPage)
                {
                    sb.Append($"<tr class='tot'><td colspan='3'>{totalQty} repere</td><td class='num'></td><td class='num'>{totalQty}</td><td></td><td class='num waste'>{N(totalWaste)}</td></tr></table>");
                }
                else sb.Append("</table>");
                tp++;
                yield return new Sheet { Id = "cutlist" + tp, Title = p.Name + " · listă de debitare", Body = sb.ToString() };
            }
            yield return OrderSheet(p, r);
        }

        private static Sheet OrderSheet(WoodProject p, ProjectResult r)
        {
            var o = r.Optimization; var cur = r.Cost.Currency;
            var sb = new StringBuilder("<div class='cols2'><div>");
            sb.Append("<h2>Necesar lucrare <span class='muted'>· material de comandat, optimizat global, cu rezerva</span></h2><table><tr><th>Cod</th><th>Material</th><th class='num'>Lungime</th><th class='num'>Buc</th><th class='num'>din care rezervă</th><th class='num'>Preț unitar</th><th class='num'>Total</th></tr>");
            foreach (var l in o.Purchase) sb.Append($"<tr><td class='num'>{E(l.Id)}</td><td>{E(Ro.SpeciesName(l.Item.SpeciesId))} {N(Math.Min(l.Item.Width, l.Item.Thickness))}×{N(Math.Max(l.Item.Width, l.Item.Thickness))}</td><td class='num'>{N(l.Length)}</td><td class='num'>{l.Quantity}</td><td class='num'>{l.ReserveQuantity}</td><td class='num'>{E(Money.Format(l.UnitPrice, cur))}</td><td class='num'>{E(Money.Format(l.Total, cur))}</td></tr>");
            sb.Append($"<tr class='tot'><td colspan='6'>Material</td><td class='num'>{E(Money.Format(o.TotalCost, cur))}</td></tr></table>");
            sb.Append($"<h2>Cost de producție</h2><table class='narrow'><tr><td>Material</td><td class='num'>{E(Money.Format(r.Cost.RawMaterial, cur))}</td></tr><tr><td>Feronerie și mărunțișuri</td><td class='num'>{E(Money.Format(r.Cost.Hardware + r.Cost.Consumables, cur))}</td></tr><tr><td>Manoperă</td><td class='num'>{E(Money.Format(r.Cost.Labor, cur))}</td></tr><tr class='tot'><td>Cost producție</td><td class='num big'>{E(Money.Format(r.Cost.Total, cur))}</td></tr></table>");
            sb.Append("</div><div><h2>De ce acest necesar</h2><ol class='steps'>");
            int pieces = o.Boards.Where(b => !b.IsReserve).Sum(b => b.Pieces.Count);
            int reuse = o.Boards.Where(b => !b.IsReserve).Sum(b => Math.Max(0, b.Pieces.Count - 1));
            var buy = string.Join(", ", o.Purchase.Select(l => l.Quantity + " × " + Ro.SpeciesName(l.Item.SpeciesId) + " " + N(Math.Min(l.Item.Width, l.Item.Thickness)) + "×" + N(Math.Max(l.Item.Width, l.Item.Thickness)) + "×" + N(l.Length)));
            sb.Append("<li>Necesar teoretic: " + N(o.TheoreticalFinishedM3, "0.0000") + " m³ de piese finite (" + N(o.TheoreticalLinearM, "0.0") + " m liniari).</li>");
            sb.Append("<li>Cu adaosurile de prelucrare (rindeluire, tăiere la lungime): " + N(o.RoughRequiredM3, "0.0000") + " m³ în " + pieces + " bucăți brute.</li>");
            sb.Append("<li>După optimizarea globală a tuturor pieselor (kerf " + N(p.Settings.Rules.SawKerf, "0.#") + " mm): " + N(o.OptimizedManufacturingM3, "0.0000") + " m³ pe " + o.Boards.Count(b => !b.IsReserve) + " bare; " + reuse + " bucăți se taie din restul unei bare deja începute.</li>");
            sb.Append("<li>Necesar comercial (lungimi standard, cu rezerva): " + E(buy) + " = " + N(o.PurchasedM3, "0.0000") + " m³; piesele brute ocupă " + N(100 * o.RoughRequiredM3 / Math.Max(1e-9, o.PurchasedM3), "0") + "% din material.</li>");
            sb.Append("<li>Resturi reutilizabile " + N(o.ReusableRemnantM3, "0.0000") + " m³, deșeu " + N(o.ScrapM3, "0.0000") + " m³, pierderi de prelucrare (kerf, capete, surplus de secțiune, rindeluire) " + N(o.ProcessWasteM3, "0.0000") + " m³.</li>");
            int extra = o.Boards.Count(b => b.IsReserve);
            sb.Append("<li>Rezerva de " + string.Join(", ", o.ReservePercentBySpecies.Select(kv => N(kv.Value, "0.#") + "% " + Ro.SpeciesName(kv.Key))) + " se aplică o singură dată, după optimizare: " + (extra == 0 ? "este acoperită de resturile reutilizabile, fără bare în plus." : "s-au adăugat " + extra + " bare de rezervă.") + "</li>");
            sb.Append("</ol></div></div>");
            return new Sheet { Id = "order", Title = p.Name + " · necesar lucrare și cost", Body = sb.ToString() };
        }

        // ================================================================= 4. assembly notes
        private static Sheet AssemblySheet(WoodProject p, ProjectResult r)
        {
            var steps = new List<string> { "Probă uscată a tuturor pieselor, fără adeziv; verifică echerul și jocurile." };
            foreach (var fam in r.Model.Families.Where(f => f.RoughPieces.Sum(q => q.CountPerPart) > 1))
                steps.Add("Încleiază " + Ro.Family(fam).ToLowerInvariant() + " din " + fam.RoughPieces.Sum(q => q.CountPerPart) + " lamele (rost plan, fibra în aceeași direcție); lasă să se întărească înainte de rindeluire.");
            foreach (var j in r.Model.Joints)
                steps.Add("Încleiază și strânge îmbinarea " + j.Id + ": " + j.PartAId + " → " + j.PartBId + " (" + Ro.Joint(p.Joints.Get(j.JointTypeId)) + ")" + (j.Mitred ? " — capete de cep teșite la 45°" : "") + ".");
            foreach (var h in r.Model.HardwareInstalls.GroupBy(x => x.HardwareId))
                steps.Add("Montează " + h.Count() + " × " + Ro.HardwareName(h.Key, p.Library.Hardware[h.Key].Model) + " (vezi detaliile de feronerie).");
            steps.Add("Finisează (ulei/lac) și lasă să se usuce; fixează blatul la urmă, cu elementele care permit mișcarea lui.");
            var sb = new StringBuilder("<div class='cols2'><div><h2>Ordinea de montaj</h2><ol class='steps'>");
            foreach (var st in steps) sb.Append("<li>" + E(st) + "</li>");
            sb.Append("</ol></div><div>");
            sb.Append("<h2>Feronerie și montaj</h2><table><tr><th>Element</th><th>Pe piesa</th><th class='num'>Poziție</th></tr>");
            foreach (var h in r.Model.HardwareInstalls)
            {
                var hw = p.Library.Hardware[h.HardwareId];
                sb.Append($"<tr><td>{E(Ro.HardwareName(h.HardwareId, hw.Model))}</td><td>{E(h.HostPartId)}</td><td class='num'>{E(h.Point.ToString())}</td></tr>");
            }
            sb.Append("</table>");
            foreach (var hw in r.Model.HardwareInstalls.Select(h => h.HardwareId).Distinct().Select(id => p.Library.Hardware[id]).Where(x => !string.IsNullOrEmpty(x.InstallationNotes) || !string.IsNullOrEmpty(x.InstallationNotesRo)))
                sb.Append($"<p class='note'><b>{E(Ro.HardwareName(hw.Id, hw.Model))}.</b> {E(string.IsNullOrEmpty(hw.InstallationNotesRo) ? hw.InstallationNotes : hw.InstallationNotesRo)}</p>");
            sb.Append("<h2>Atenție</h2><ul class='why'>");
            foreach (var i in r.Issues.Where(i => i.Severity != Severity.Info)) sb.Append($"<li class='{(i.Severity == Severity.Error ? "bad" : "warnc")}'>{(i.Severity == Severity.Error ? "○" : "▲")} {E(Ro.Issue(i))}</li>");
            foreach (var i in r.Issues.Where(i => i.Code == "MOVEMENT_INFO")) sb.Append($"<li>● {E(Ro.Issue(i))}</li>");
            sb.Append("<li>Adezivul în exces se îndepărtează înainte să se întărească; după strângere, verifică diagonalele.</li>");
            sb.Append("</ul></div></div>");
            return new Sheet { Id = "assembly", Title = p.Name + " · note de montaj", Body = sb.ToString() };
        }

        // ================================================================= offer (SALE mode) - no internal costs, stock codes or waste
        private static Sheet OfferSheet(WoodProject p, ProjectResult r, string variant)
        {
            double price = Math.Ceiling(r.Cost.Total * (1 + p.Settings.SalesMarginPercent / 100.0) / 10.0) * 10.0;
            var bb = r.Model.Bounds.Size;
            var sb = new StringBuilder();
            sb.Append($"<div class='offer'><div class='display'>{E(p.Name)}</div><div class='muted'>{E(SpeciesName(p, p.SpeciesId))}{(string.IsNullOrEmpty(variant) ? "" : " · variantă " + E(variant))}</div>");
            sb.Append($"<table class='narrow'><tr><td>Lungime</td><td class='num'>{N(bb.X)} mm</td></tr><tr><td>Lățime</td><td class='num'>{N(bb.Y)} mm</td></tr><tr><td>Înălțime</td><td class='num'>{N(bb.Z)} mm</td></tr>");
            sb.Append($"<tr class='tot'><td>Preț</td><td class='num big'>{E(Money.Format(price, r.Cost.Currency))}</td></tr></table>");
            sb.Append("<p class='muted'>Prețul include materialul, execuția și montajul. Lemn masiv: mici variații de culoare și desen sunt naturale.</p></div>");
            return new Sheet { Id = "offer", Title = p.Name + " · ofertă", Body = sb.ToString() };
        }
    }

    public static class SheetDocument
    {
        public const string Css = @"
@page { size: 297mm 210mm; margin: 0 }
* { box-sizing: border-box }
body { margin: 0; background: #d9d4ca; font-family: 'Segoe UI', system-ui, -apple-system, Arial, sans-serif; color: #1f1b16; -webkit-print-color-adjust: exact; print-color-adjust: exact }
.sheet { width: 297mm; height: 210mm; padding: 9mm 10mm 12mm; position: relative; background: #f3f1ed; overflow: hidden; page-break-after: always; margin: 0 auto 6mm }
@media print { body { background: #f3f1ed } .sheet { margin: 0 } }
.head { display: flex; justify-content: space-between; align-items: baseline; border-bottom: .3mm solid #8c8376; padding-bottom: 1.6mm; margin-bottom: 3mm }
h1 { font-size: 4.6mm; margin: 0; font-weight: 600 }
h2 { font-size: 3.4mm; margin: 3mm 0 1.4mm; font-weight: 600; letter-spacing: .02em }
.meta, .muted { color: #5f574c } .meta { font-size: 2.8mm }
.foot { position: absolute; left: 10mm; right: 10mm; bottom: 4.5mm; display: flex; justify-content: space-between; font-size: 2.5mm; color: #5f574c; border-top: .2mm solid #d3cdc3; padding-top: 1mm }
table { border-collapse: collapse; font-size: 2.9mm; background: #fff; margin: 1mm 0 2mm }
th { background: #e7e3dc; text-align: left; font-weight: 600; font-size: 2.6mm; padding: .8mm 2mm; border: .2mm solid #d3cdc3 }
td { padding: .7mm 2mm; border: .2mm solid #d3cdc3 }
.num { font-family: 'Cascadia Mono', Consolas, 'DejaVu Sans Mono', monospace; text-align: right }
th.num { text-align: right }
.waste { color: #c23a22 } .ok { color: #1f5f8f } .warn, .warnc { color: #8a5d00 } .bad { color: #b3261e }
table.wide { width: 100% } table.narrow { width: 90mm }
tr.tot td { background: #e7e3dc; font-weight: 600 } .big { font-size: 4mm }
.block { margin-bottom: 4mm } .cols { display: flex; gap: 6mm } .drw { flex: 0 0 150mm } .tbl { flex: 1 } .cols2 { display: flex; gap: 8mm } .cols2 > div { flex: 1 }
.checks { font-size: 2.8mm; line-height: 1.5; margin-top: 1mm } .checks div { margin-bottom: .6mm }
ol.steps, ul.why { font-size: 3mm; line-height: 1.5; padding-left: 5mm; margin: 0 } ul.why { list-style: none; padding-left: 0 }
p.note { font-size: 2.8mm; margin: 1mm 0 }
.offer { padding-top: 24mm } .display { font-size: 22px; line-height: 28px; font-weight: 600 }
";

        public static string Html(IList<Sheet> sheets, string title, string subtitle = null)
        {
            var sb = new StringBuilder("<!doctype html><html lang='ro'><head><meta charset='utf-8'><title>" + WebUtility.HtmlEncode(title) + "</title><style>" + Css + "</style></head><body>");
            for (int i = 0; i < sheets.Count; i++)
            {
                var s = sheets[i];
                sb.Append("<section class='sheet' id='" + s.Id + "'><div class='head'><h1>" + WebUtility.HtmlEncode(s.Title) + "</h1><div class='meta'>" + WebUtility.HtmlEncode(subtitle ?? "") + "</div></div>");
                sb.Append(s.Body);
                sb.Append("<div class='foot'><span>Atelier · Rhino Wood</span><span>" + DateTime.Now.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + "</span><span>Planșa " + (i + 1) + " / " + sheets.Count + "</span></div></section>");
            }
            return sb.Append("</body></html>").ToString();
        }

        /// <summary>Single-sheet HTML for the live preview in the working interface.</summary>
        public static string PreviewHtml(Sheet sheet, string subtitle = null) => Html(new List<Sheet> { sheet }, sheet.Title, subtitle);
    }

    /// <summary>PDF export through any installed Chromium-based browser (Edge ships with Windows 10/11); falls back to saving the HTML.</summary>
    public static class PdfExporter
    {
        public static string FindBrowser()
        {
            var candidates = new List<string>();
            foreach (var env in new[] { "ProgramFiles(x86)", "ProgramFiles", "LocalAppData" })
            {
                var root = Environment.GetEnvironmentVariable(env);
                if (string.IsNullOrEmpty(root)) continue;
                candidates.Add(Path.Combine(root, "Microsoft", "Edge", "Application", "msedge.exe"));
                candidates.Add(Path.Combine(root, "Google", "Chrome", "Application", "chrome.exe"));
            }
            candidates.AddRange(new[] { "/usr/bin/chromium", "/usr/bin/chromium-browser", "/usr/bin/google-chrome", "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome", "/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge" });
            var pw = Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH");
            if (!string.IsNullOrEmpty(pw) && Directory.Exists(pw))
                foreach (var d in Directory.GetDirectories(pw, "chromium-*")) candidates.Add(Path.Combine(d, "chrome-linux", "chrome"));
            return candidates.FirstOrDefault(File.Exists);
        }

        public static (bool ok, string message) Export(string html, string pdfPath, int timeoutMs = 90000)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(pdfPath)));
            var htmlPath = Path.ChangeExtension(pdfPath, ".html");
            File.WriteAllText(htmlPath, html, new UTF8Encoding(false));
            var browser = FindBrowser();
            if (browser == null) return (false, "Nu am găsit Edge/Chrome pentru PDF; am salvat HTML-ul (deschide-l și tipărește în PDF): " + htmlPath);
            var profile = Path.Combine(Path.GetTempPath(), "rhinowood-pdf-" + Guid.NewGuid().ToString("N"));
            var psi = new ProcessStartInfo(browser)
            {
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true,
                Arguments = $"--headless --no-sandbox --disable-gpu --no-pdf-header-footer --user-data-dir=\"{profile}\" --print-to-pdf=\"{Path.GetFullPath(pdfPath)}\" \"{new Uri(Path.GetFullPath(htmlPath)).AbsoluteUri}\""
            };
            try
            {
                if (File.Exists(pdfPath)) File.Delete(pdfPath);
                using (var proc = Process.Start(psi))
                {
                    if (!proc.WaitForExit(timeoutMs)) { try { proc.Kill(); } catch { } return (false, "Exportul PDF a depășit timpul; HTML salvat: " + htmlPath); }
                }
                try { Directory.Delete(profile, true); } catch { }
                return File.Exists(pdfPath) && new FileInfo(pdfPath).Length > 1000 ? (true, pdfPath) : (false, "PDF-ul nu a fost generat; HTML salvat: " + htmlPath);
            }
            catch (Exception ex) { return (false, "Eroare la export PDF (" + ex.Message + "); HTML salvat: " + htmlPath); }
        }

        public static (bool ok, string message) ExportSheets(WoodProject p, ProjectResult r, string pdfPath, SheetMode mode = SheetMode.Design, string variant = null)
        {
            variant = variant ?? SheetBuilder.TierName(p);
            var sheets = SheetBuilder.Build(p, r, mode, variant);
            var sub = SheetDocumentSubtitle(p, mode, variant);
            return Export(SheetDocument.Html(sheets, p.Name, sub), pdfPath);
        }

        public static string SheetDocumentSubtitle(WoodProject p, SheetMode mode, string variant) =>
            (mode == SheetMode.Sale ? "VÂNZARE" : "DESIGN") + " · " + Ro.FurnitureName(p.Furniture.TypeId, p.Furniture.Name) + (string.IsNullOrEmpty(variant) ? "" : " · " + variant);
    }
}
