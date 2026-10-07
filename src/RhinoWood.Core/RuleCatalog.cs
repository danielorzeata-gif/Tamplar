using System;
using System.Collections.Generic;
using System.Linq;

namespace RhinoWood.Core.Rules
{
    /// <summary>How far a rule may be trusted/coded (from knowledge/Wood/13_DESIGN_AND_ENGINEERING/RULES_EN_PEER_REVIEW_2026.md).</summary>
    public enum RuleConfidence
    {
        /// <summary>Safe to code within its stated domain.</summary>
        Safe,
        /// <summary>Safe but provisional: value read from a preview, confirm in the paid standard text.</summary>
        SafeProvisional,
        /// <summary>Usable only with the stated condition (units, species, domain).</summary>
        Conditional,
        /// <summary>Typology only - no numeric capacity may be shown (no peer-reviewed data).</summary>
        TypologyOnly,
        /// <summary>Must NOT be coded until verified.</summary>
        Forbidden
    }

    public sealed class RuleSpec
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Formula { get; set; }
        public string Basis { get; set; }
        public string Label { get; set; }      // [V-STD] / [V-TEXT] / [V-DATA] / [REF] / [ESTIMARE]
        public RuleConfidence Confidence { get; set; }
        public string Note { get; set; }
    }

    /// <summary>The 19 configurator rules R1..R19 with their evidence level. The UI shows this as the answer to "why?".</summary>
    public static class RuleCatalog
    {
        private static RuleSpec R(string id, string title, string formula, string basis, string label, RuleConfidence c, string note = null) =>
            new RuleSpec { Id = id, Title = title, Formula = formula, Basis = basis, Label = label, Confidence = c, Note = note };

        public static readonly IReadOnlyList<RuleSpec> All = new List<RuleSpec>
        {
            R("R1", "Chair design loads", "seat 1300 N, back 450 N, user 110 kg", "EN 12520:2024", "[V-STD]", RuleConfidence.SafeProvisional, "Recheck against EN 12520:2024/A1 (Jan 2026); back 410 vs 450 N unclear."),
            R("R2", "Static capacity of chair joints", ">= 2384 N (domestic) / 2780 N (medium) front-back", "Kilic 2018 + cyclic factor 0.56", "[V-TEXT]", RuleConfidence.Safe, "Proxy only: no published mapping from ALA levels to EN 12520 cycles."),
            R("R3", "Mortise & tenon moment", "E1 with factor 1.5-2 or 0.68 x mean; E2/E3a in beech", "Kasal 2015, Hu & Liu 2020, Hu & Chen 2021", "[V-TEXT]", RuleConfidence.Conditional, "Only inside the validated domain; outside = 'needs testing per EN 1728/1730'."),
            R("R4", "Cyclic/static factor", "0.56 rectangular, 0.67 round tenon", "Kilic 2018; Likos 2013", "[V-TEXT]/[REF]", RuleConfidence.Safe),
            R("R5", "Tenon geometry", "w <= l <= 2w; t = 1/3..1/2 of rail; full shoulder (+54 % moment)", "Hu & Chen 2021; Kasal 2015", "[V-TEXT]/[REF]", RuleConfidence.Safe, "t ratio is a default, not a threshold."),
            R("R6", "CNC fit (beech)", "thickness -0.2 mm; width +0.1..0.2 mm; warn > 0.3 mm", "Hu & Guan 2019", "[V-TEXT]/[ESTIMARE]", RuleConfidence.Conditional, "Beech only; other species default, marked untested."),
            R("R7", "Tool & corners", "r_int = D/2; D >= cut depth/7.5; 90 deg on 3 axes", "EPFL IBOIS", "[V-TEXT]", RuleConfidence.Safe),
            R("R8", "Dowel capacity", "E6/E7; b in INCHES; maximise embedment; several dowels", "Eckelman; Chen 2019", "[V-TEXT]", RuleConfidence.Conditional, "Unit of the clearance coefficient to be confirmed."),
            R("R9", "Panel movement gap", "gap = W * diff_T/100 * dMC, dMC = 5", "DIN 68100 + Wood Handbook formula", "[V-DATA]+[ESTIMARE]", RuleConfidence.Safe),
            R("R10", "Storage stability", "H > 900 & m >= 10 kg  or  H > 350 & m >= 35 kg => anchor + label + tip-over calc", "EN 14749:2016+A1:2022", "[V-STD]", RuleConfidence.Safe),
            R("R11", "Delicate table", "top <= 0.30 m2, H >= 600, m > 10 kg => stability check", "EN 12521:2023", "[V-STD]", RuleConfidence.Safe),
            R("R12", "Finger traps / shear gaps", "no hole dia 7-12 mm with depth >= 10 mm; no 8-25 mm gaps on moving parts (8-18 normal use)", "EN 12520 / EN 12521", "[V-STD]", RuleConfidence.Safe),
            R("R13", "Shelf and drawer load", "0.65 kg/dm3 shelf, 0.2 kg/dm3 drawer (storage volume)", "EN 14749", "[V-STD]", RuleConfidence.SafeProvisional, "Unit to be confirmed. Clothes rail 4.0 kg/dm3 is suspect: NOT coded."),
            R("R14", "Formaldehyde", "<= 0.062 mg/m3 for wood-based boards (from 6 Aug 2026)", "REACH Annex XVII entry 77 (Reg. 2023/1464)", "[V-TEXT]", RuleConfidence.Safe, "E1-only boards are no longer enough."),
            R("R15", "GPSR / EUDR dossier", "marking, lot, DDS numbers; keep 10 y (GPSR) / 5 y (EUDR DDS)", "Reg. 2023/988; EUDR 2025/2650", "[V-TEXT]", RuleConfidence.Safe),
            R("R16", "Assembly sequence", "directional blocking graph, one key part, 1DOF", "IBOIS; Song et al. 2017", "[V-TEXT]/[REF]", RuleConfidence.Safe),
            R("R17", "Hardness", "Brinell perpendicular (DIN 68364) ONLY", "DIN 68364 via LWF", "[V-DATA]", RuleConfidence.Safe, "Janka from the Wood Handbook extraction is FORBIDDEN (column misaligned)."),
            R("R18", "Bending equation Fb (Hu & Chen)", "-", "does not reproduce the reported optimum (+17 %)", "[V-TEXT]", RuleConfidence.Forbidden, "Not coded until checked in the PDF."),
            R("R19", "Glue-free joints (wedge, tusk, sliding dovetail with key)", "-", "no peer-reviewed data 2015-2026", "-", RuleConfidence.TypologyOnly, "No numeric capacity: mark 'test per EN 1728/1730'."),
        };

        public static RuleSpec Get(string id) => All.FirstOrDefault(r => r.Id == id) ?? throw new KeyNotFoundException(id);
    }

    /// <summary>Pure, unit-tested helpers for the safe rules. All lengths mm, masses kg, forces N.</summary>
    public static class SafetyRules
    {
        // ---- R10 / R11
        public static bool RequiresStabilityCheck(double heightMm, double massKg) =>
            (heightMm > 900 && massKg >= 10) || (heightMm > 350 && massKg >= 35);

        public static bool IsDelicateTable(double topAreaM2, double heightMm, double massKg) =>
            topAreaM2 <= 0.30 && heightMm >= 600 && massKg > 10;

        // ---- R12
        public static bool IsFingerTrap(double holeDiameterMm, double depthMm) => holeDiameterMm >= 7 && holeDiameterMm <= 12 && depthMm >= 10;
        public static bool IsShearGap(double gapMm, bool mechanismOrMovingPart) => gapMm >= 8 && gapMm <= (mechanismOrMovingPart ? 25 : 18);

        // ---- R13 (storage volume in dm3)
        public static double ShelfLoadKg(double storageVolumeDm3) => 0.65 * storageVolumeDm3;
        public static double DrawerLoadKg(double storageVolumeDm3) => 0.2 * storageVolumeDm3;

        // ---- R9
        public static double PanelMovementMm(double widthMm, double diffTangentialPct, double deltaMoisturePct = 5) =>
            widthMm * diffTangentialPct / 100.0 * deltaMoisturePct;

        // ---- R2 / R4
        public const double CyclicFactorRectangular = 0.56, CyclicFactorRound = 0.67;
        public static double RequiredStaticChairCapacityN(double alaLevelN, double cyclicFactor = CyclicFactorRectangular) => alaLevelN / cyclicFactor;

        // ---- R5
        public sealed class TenonCheck { public bool LengthOk, ThicknessOk; public string Message; }
        public static TenonCheck CheckTenonGeometry(double lengthMm, double widthMm, double thicknessMm, double railThicknessMm)
        {
            var c = new TenonCheck
            {
                LengthOk = lengthMm >= widthMm - 1e-9 && lengthMm <= 2 * widthMm + 1e-9,
                ThicknessOk = thicknessMm >= railThicknessMm / 3.0 - 0.75 && thicknessMm <= railThicknessMm / 2.0 + 0.75
            };
            c.Message = (c.LengthOk ? "" : "tenon length outside w..2w; ") + (c.ThicknessOk ? "" : "tenon thickness outside 1/3..1/2 of the rail; ");
            return c;
        }

        // ---- R3 / E1 (Kasal 2015): M in N*m
        public sealed class MomentResult { public double MeanNm; public double DesignNm; public bool InDomain; public string Note; }

        /// <summary>
        /// L-shaped mortise&amp;tenon bending moment. W, L = tenon width/length (30-50 mm), d = shoulder width, S = shear strength (6.2-10.3 N/mm2).
        /// DesignNm uses the 0.68 x mean lower-tolerance factor. InDomain=false means the result must NOT be shown as a capacity.
        /// </summary>
        public static MomentResult TenonMomentE1(double wMm, double lMm, double dMm, double shearMPa, bool tension = true, bool pva = true)
        {
            double k1 = tension ? 1.0 : 1.066, k2 = pva ? 0.85 : 1.0;
            double m = 0.00227 * (wMm * lMm) * (0.229 * wMm + dMm) * Math.Pow(shearMPa, 0.42) * k1 * k2;
            bool inDomain = wMm >= 30 && wMm <= 50 && lMm >= 30 && lMm <= 50 && shearMPa >= 6.2 && shearMPa <= 10.3;
            return new MomentResult
            {
                MeanNm = m, DesignNm = 0.68 * m, InDomain = inDomain,
                Note = inDomain ? "inside the validated domain (beech/pine, PVAc/PU)" : "outside the validated domain: needs testing per EN 1728/1730"
            };
        }
    }
}
