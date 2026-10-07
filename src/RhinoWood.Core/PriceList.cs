using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using RhinoWood.Core.Libraries;
using RhinoWood.Core.Reports;
using RhinoWood.Core.Rules;

namespace RhinoWood.Core.Projects
{
    /// <summary>
    /// The price list as a CSV the user can edit in Excel: species (lei/m³), hardware and fasteners (lei/buc), and the pricing parameters (hourly rate, margin, VAT, HDF per m²…).
    /// Columns: tip;id;nume;pret;unitate;sursa. Numbers accept "2.800" (thousands) or "2,5" (decimal comma).
    /// </summary>
    public static class PriceList
    {
        public const string Header = "tip;id;nume;pret;unitate;sursa";

        private static readonly (string name, string label, string unit)[] RuleFields =
        {
            ("LaborRatePerHour", "Tarif manoperă", "lei/oră"), ("HdfPricePerM2", "Placă HDF 3 mm", "lei/m²"), ("GluePricePerM2", "Adeziv (pe suprafață de lipire)", "lei/m²"),
            ("FinishPricePerM2", "Finisaj (pe suprafață)", "lei/m²"), ("BiscuitPrice", "Lamelă (biscuit)", "lei/buc"), ("SplinePricePerM", "Cep liber (pană) 6×19", "lei/m"),
        };
        private static readonly (string name, string label, string unit)[] SettingFields =
        {
            ("SalesMarginPercent", "Marjă de bază pe preț", "%"), ("ComplexityMarginPercent", "Adaos maxim de complexitate", "puncte"), ("OverheadPercent", "Regie atelier", "%"),
            ("VatPercent", "TVA", "%"), ("PriceRounding", "Rotunjire preț (în sus)", "lei"), ("YieldBlanks", "Factor randament semifabricate (1,15–1,25)", "×"), ("YieldEdgedA", "Factor randament tivit clasa A (1,4–1,5)", "×"), ("YieldEdged", "Factor randament tivit clasa B/AB (1,6–1,8)", "×"), ("YieldUnedged", "Factor randament netivit (1,8–2,2)", "×"), ("YieldRustic", "Factor randament clasa C/rustic (2,0–2,5)", "×"), ("DeliveryKm", "Distanță transport material", "km"), ("TransportLeiPerKm", "Transport dubă", "lei/km"),
        };

        public static string Export(WoodLibrary lib, ProjectSettings s)
        {
            var ci = CultureInfo.InvariantCulture; var sb = new StringBuilder();
            sb.AppendLine(Header);
            foreach (var sp in lib.Species.Values.OrderByDescending(x => x.PricePerM3))
                sb.AppendLine(string.Join(";", "specie", sp.Id, Ro.SpeciesName(sp.Id, sp.Name), sp.PricePerM3.ToString("0.##", ci), "lei/m3", sp.PriceLabel));
            foreach (var h in lib.Hardware.Values.OrderBy(x => x.Id))
                sb.AppendLine(string.Join(";", "feronerie", h.Id, Ro.HardwareName(h.Id, h.Model), h.UnitPrice.ToString("0.##", ci), "lei/buc", ""));
            foreach (var (n, l, u) in RuleFields) sb.AppendLine(string.Join(";", "regula", n, l, ((double)typeof(ManufacturingRules).GetProperty(n).GetValue(s.Rules)).ToString("0.###", ci), u, ""));
            foreach (var (n, l, u) in SettingFields) sb.AppendLine(string.Join(";", "setare", n, l, ((double)typeof(ProjectSettings).GetProperty(n).GetValue(s)).ToString("0.###", ci), u, ""));
            return sb.ToString();
        }

        public sealed class ImportResult
        {
            public int Species, Hardware, Rules, Settings;
            public List<string> Messages = new List<string>();
            /// <summary>Rule / setting values that were set (so they can be remembered as defaults for new projects).</summary>
            public Dictionary<string, double> Parameters = new Dictionary<string, double>();
            public override string ToString() => Species + " specii, " + Hardware + " articole de feronerie, " + (Rules + Settings) + " parametri de preț" + (Messages.Count > 0 ? "; " + Messages.Count + " rânduri ignorate (" + Messages[0] + ")" : "");
        }

        /// <summary>Applies the CSV: species / hardware go into <paramref name="lib"/> (and are copied to <paramref name="user"/> so they persist), parameters into <paramref name="s"/>.</summary>
        public static ImportResult Import(string csv, WoodLibrary lib, ProjectSettings s, WoodLibrary user = null)
        {
            var res = new ImportResult(); int line = 0;
            foreach (var raw in csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                line++;
                var cells = raw.Split(';'); if (cells.Length < 4) cells = raw.Split(',');
                if (cells.Length < 4 || cells[0].Trim().ToLowerInvariant() == "tip" || cells[0].TrimStart().StartsWith("#")) continue;
                string type = cells[0].Trim().ToLowerInvariant(), id = cells[1].Trim();
                if (!TryNumber(cells[3], out var v) || v < 0) { res.Messages.Add("rândul " + line + ": preț invalid"); continue; }
                switch (type)
                {
                    case "specie":
                        if (!lib.Species.TryGetValue(id, out var sp)) { res.Messages.Add("rândul " + line + ": specia " + id + " nu există"); break; }
                        sp.PricePerM3 = v; if (sp.PriceLabel != "[REF]") sp.PriceLabel = "[LISTĂ]";
                        if (user != null) user.Species[id] = sp;
                        res.Species++; break;
                    case "feronerie":
                        if (!lib.Hardware.TryGetValue(id, out var hw)) { res.Messages.Add("rândul " + line + ": articolul " + id + " nu există"); break; }
                        hw.UnitPrice = v; if (user != null) user.Hardware[id] = hw;
                        res.Hardware++; break;
                    case "regula":
                        { var pi = typeof(ManufacturingRules).GetProperty(id); if (pi == null || pi.PropertyType != typeof(double)) { res.Messages.Add("rândul " + line + ": parametrul " + id + " nu există"); break; } pi.SetValue(s.Rules, v); res.Rules++; res.Parameters["R." + id] = v; break; }
                    case "setare":
                        { var pi = typeof(ProjectSettings).GetProperty(id); if (pi == null || pi.PropertyType != typeof(double)) { res.Messages.Add("rândul " + line + ": parametrul " + id + " nu există"); break; } pi.SetValue(s, v); res.Settings++; res.Parameters["S." + id] = v; break; }
                    default: res.Messages.Add("rândul " + line + ": tip necunoscut „" + cells[0] + "”"); break;
                }
            }
            return res;
        }

        /// <summary>Applies remembered parameters (from earlier imports) to the settings of a new project.</summary>
        public static void ApplyParameters(IDictionary<string, double> parameters, ProjectSettings s)
        {
            foreach (var kv in parameters)
            {
                if (kv.Key.StartsWith("R.")) typeof(ManufacturingRules).GetProperty(kv.Key.Substring(2))?.SetValue(s.Rules, kv.Value);
                else if (kv.Key.StartsWith("S.")) typeof(ProjectSettings).GetProperty(kv.Key.Substring(2))?.SetValue(s, kv.Value);
            }
        }

        /// <summary>"2.800" → 2800 (thousands dot), "2,5" → 2.5, "1 950" → 1950.</summary>
        public static bool TryNumber(string text, out double v)
        {
            var t = (text ?? "").Trim().Replace(" ", "").Replace(" ", "");
            if (System.Text.RegularExpressions.Regex.IsMatch(t, @"^\d{1,3}(\.\d{3})+$")) t = t.Replace(".", "");
            else if (System.Text.RegularExpressions.Regex.IsMatch(t, @"^\d{1,3}(,\d{3})+$")) t = t.Replace(",", "");
            else t = t.Replace(',', '.');
            return double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out v);
        }
    }
}
