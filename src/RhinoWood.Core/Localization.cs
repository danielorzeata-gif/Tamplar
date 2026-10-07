using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using RhinoWood.Core.Domain;
using RhinoWood.Core.Joinery;
using RhinoWood.Core.Libraries;

namespace RhinoWood.Core.Reports
{
    /// <summary>Romanian terminology (with diacritics) for documents and UI, per the Atelier content rules. Identifiers in code stay English.</summary>
    public static class Ro
    {
        private static readonly Dictionary<string, string> Species = new Dictionary<string, string>
        {
            ["OAK"] = "Stejar", ["BEECH"] = "Fag", ["ASH"] = "Frasin", ["MAPLE"] = "Paltin", ["PINE"] = "Pin", ["SPRUCE"] = "Molid", ["WALNUT"] = "Nuc",
            ["CHERRY"] = "Cireș", ["ROBINIA"] = "Salcâm", ["BIRCH"] = "Mesteacăn", ["LINDEN"] = "Tei", ["ELM"] = "Ulm", ["ALDER"] = "Anin",
            ["POPLAR"] = "Plop", ["DOUGLAS"] = "Duglas", ["MAHOGANY"] = "Mahon"
        };
        private static readonly Dictionary<string, string> Families = new Dictionary<string, string>
        {
            ["F-TOP"] = "Blat (încleiat din lamele)", ["F-LEG"] = "Picior", ["F-APRON-L"] = "Zargă lungă", ["F-APRON-S"] = "Zargă scurtă"
        };
        private static readonly Dictionary<string, string> Joints = new Dictionary<string, string>
        {
            ["mortise-tenon"] = "Cep-mortază", ["loose-tenon"] = "Cep liber", ["dowel"] = "Dibluri", ["biscuit"] = "Lamele (biscuit)",
            ["pocket-screw"] = "Șuruburi în buzunar", ["bridle"] = "Cep deschis (bridle)", ["japanese-kusabi"] = "Cep pătruns cu pene (kusabi)",
            ["dovetail"] = "Coadă de rândunică", ["finger"] = "Îmbinare cu dinți", ["box-joint"] = "Îmbinare cu dinți (box)",
            ["dado"] = "Canal (housing)", ["rabbet"] = "Falț", ["half-lap"] = "Înjumătățire", ["scarf"] = "Prelungire în pană"
        };
        private static readonly Dictionary<string, string> Hardware = new Dictionary<string, string>
        {
            ["TOP-ZCLIP"] = "Clips Z 30×20", ["TOP-FIGURE8"] = "Fixare figure-8", ["TOP-BUTTON"] = "Buton de lemn", ["TOP-SLOTSCREW"] = "Șurub în gaură alungită 4×35 + șaibă",
            ["SCR-4x16"] = "Șurub 4×16", ["SCR-4x35"] = "Șurub 4×35", ["WSH-4"] = "Șaibă 4"
        };
        private static readonly Dictionary<FeatureKind, string> Features = new Dictionary<FeatureKind, string>
        {
            [FeatureKind.Mortise] = "Mortază", [FeatureKind.Tenon] = "Cep", [FeatureKind.Slot] = "Canal", [FeatureKind.ElongatedHole] = "Gaură alungită",
            [FeatureKind.ScrewHole] = "Gaură de șurub", [FeatureKind.DowelHole] = "Gaură de diblu", [FeatureKind.BiscuitSlot] = "Locaș lamelă",
            [FeatureKind.Dado] = "Canal", [FeatureKind.Rabbet] = "Falț", [FeatureKind.Lap] = "Înjumătățire", [FeatureKind.Pocket] = "Buzunar",
            [FeatureKind.WedgeSlot] = "Tăietură pentru pană", [FeatureKind.Counterbore] = "Gaură în buzunar", [FeatureKind.Finger] = "Dinte", [FeatureKind.Dovetail] = "Coadă de rândunică"
        };

        public static string SpeciesName(string id, string fallback = null) => Species.TryGetValue(id ?? "", out var v) ? v : fallback ?? id;
        public static string Family(PartFamily f) => Families.TryGetValue(f.Id, out var v) ? v : f.Name;
        public static string Joint(IJointDefinition d) => Joints.TryGetValue(d.Id, out var v) ? v : d.Name;
        public static string Joint(string id, string fallback) => Joints.TryGetValue(id, out var v) ? v : fallback;
        public static string HardwareName(string id, string fallback) => Hardware.TryGetValue(id, out var v) ? v : fallback ?? id;
        private static readonly Dictionary<string, string> Params = new Dictionary<string, string>
        {
            ["length"] = "Lungime", ["width"] = "Lățime", ["height"] = "Înălțime", ["topThickness"] = "Grosime blat", ["legSectionUser"] = "Secțiune picior (0 = regulă)",
            ["apronHeight"] = "Înălțime zargă", ["apronThickness"] = "Grosime zargă", ["overhang"] = "Prelungire blat", ["reveal"] = "Retragere zargă", ["clipSpacing"] = "Pas fixare blat"
        };
        private static readonly Dictionary<string, string> Choices = new Dictionary<string, string>
        {
            ["jointApronLong"] = "Zargă lungă – picior", ["jointApronShort"] = "Zargă scurtă – picior", ["topFixing"] = "Fixare blat"
        };
        public static string Param(string key, string fallback) => Params.TryGetValue(key, out var v) ? v : fallback;
        public static string Choice(string key, string fallback) => Choices.TryGetValue(key, out var v) ? v : fallback;
        public static string Group(string g) => g == "Joinery" ? "Îmbinări" : g == "Hardware" ? "Feronerie" : g == "Main" ? "Principale" : g;
        public static string FurnitureName(string typeId, string fallback) => typeId == "table.dining" ? "Masă de sufragerie din lemn masiv" : fallback;
        public static string Feature(FeatureKind k) => Features.TryGetValue(k, out var v) ? v : k.ToString();

        private static string Num(Match m, int g) => m.Groups[g].Value;

        /// <summary>Romanian text of a validation issue; falls back to the original message for codes without a translation.</summary>
        public static string Issue(Issue i)
        {
            Match m;
            switch (i.Code)
            {
                case "MOVEMENT_INFO":
                    m = Regex.Match(i.Message, @"movement across grain ([\d.]+) mm total \(±([\d.]+) mm\) for a ([\d.]+)%");
                    if (m.Success) return i.SubjectId + ": mișcarea sezonieră estimată pe lățime (contra fibrei) este " + Num(m, 1) + " mm în total (±" + Num(m, 2) + " mm) la o variație de umiditate de " + Num(m, 3) + " puncte — lasă jocuri sau fixează cu elemente care permit mișcarea.";
                    break;
                case "MOVEMENT_FASTENER":
                    m = Regex.Match(i.Message, @"^(.*) on (\S+): expected movement ([\d.]+) mm exceeds fastener travel ([\d.]+) mm");
                    if (m.Success) return Ro.HardwareName("", Num(m, 1)) + " pe " + Num(m, 2) + ": mișcarea estimată de " + Num(m, 3) + " mm depășește cursa fixării (" + Num(m, 4) + " mm); folosește găuri alungite sau fixări mai flexibile.";
                    break;
                case "TENON_MITRED":
                    return "Mortazele se întâlnesc în " + i.SubjectId + "; capetele cepurilor se taie la 45°.";
                case "THROUGH_CONFLICT":
                    return "Două mortaze se intersectează în " + i.SubjectId + " și cel puțin una e pătrunsă: eșalonează înălțimile traverselor sau folosește îmbinare oarbă la una dintre ele.";
                case "STABILITY_TABLE":
                    return "Masă mică în intervalul „masă delicată” (EN 12521): verifică stabilitatea.";
                case "STABILITY_STORAGE":
                    return "Corp înalt/greu peste pragurile EN 14749: kit de ancorare în perete, etichetă de avertizare și verificarea la răsturnare cu sertarele deschise și încărcate.";
                case "SLENDER":
                    return "Piciorul este zvelt; mărește secțiunea sau adaugă traverse.";
                case "WASTE":
                    return "Planul de debitare are deșeu mare; încearcă alte profile din depozit sau altă strategie.";
            }
            return i.Message;
        }
    }
}
