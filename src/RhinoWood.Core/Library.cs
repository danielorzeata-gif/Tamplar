using System;
using System.Collections.Generic;
using System.Linq;
using RhinoWood.Core.Domain;
using RhinoWood.Core.Geometry;

namespace RhinoWood.Core.Libraries
{
    public sealed class ToolItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }          // Drill, Forstner, Router, Dovetail, Mortiser, Saw, CNC
        public double Diameter { get; set; }
        public double Shank { get; set; }
        public double CuttingLength { get; set; }
        public double MaxDepth { get; set; }
        public OperationType Operation { get; set; }
        public List<string> CompatibleSpecies { get; set; } = new List<string>(); // empty = all
        public double RecommendedRpm { get; set; }
        public double FeedMmPerMin { get; set; }
    }

    public enum PatternTarget { Host, Mate }

    public sealed class HolePatternEntry
    {
        public PatternTarget Target { get; set; }
        public FeatureKind Kind { get; set; }
        public double U { get; set; }
        public double V { get; set; }
        public double Diameter { get; set; }
        public double SizeU { get; set; }
        public double SizeV { get; set; }
        public double Depth { get; set; }
        public string Purpose { get; set; }
    }

    public sealed class HardwareItem
    {
        public string Id { get; set; }
        public string Category { get; set; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public Vec3 Dimensions { get; set; }
        public double UnitPrice { get; set; }
        public double TravelAllowance { get; set; }   // free sliding travel (mm) for movement checks
        public List<HolePatternEntry> Pattern { get; set; } = new List<HolePatternEntry>();
        public List<string> RequiredToolIds { get; set; } = new List<string>();
        public List<string> FastenerIds { get; set; } = new List<string>();
        public Dictionary<string, int> FastenersPerUnit { get; set; } = new Dictionary<string, int>();
        public string InstallationNotes { get; set; }
        public string InstallationNotesRo { get; set; }
    }

    public sealed class ConsumableItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public double UnitPrice { get; set; }
    }

    /// <summary>System + user data. System data ships with the plugin; user data is persisted separately.</summary>
    public sealed class WoodLibrary
    {
        public Dictionary<string, WoodSpecies> Species { get; } = new Dictionary<string, WoodSpecies>();
        public Dictionary<string, Supplier> Suppliers { get; } = new Dictionary<string, Supplier>();
        public List<StockItem> Stock { get; } = new List<StockItem>();
        public Dictionary<string, ToolItem> Tools { get; } = new Dictionary<string, ToolItem>();
        public Dictionary<string, HardwareItem> Hardware { get; } = new Dictionary<string, HardwareItem>();
        public Dictionary<string, ConsumableItem> Consumables { get; } = new Dictionary<string, ConsumableItem>();

        public WoodSpecies GetSpecies(string id) => Species.TryGetValue(id, out var s) ? s : throw new KeyNotFoundException("Unknown species " + id);
        public IEnumerable<StockItem> StockFor(string speciesId) => Stock.Where(s => s.SpeciesId == speciesId);

        public double PricePerM3(StockItem s) => s.PricePerM3 > 0 ? s.PricePerM3 : GetSpecies(s.SpeciesId).PricePerM3;
        public double PiecePrice(StockItem s, double length) => Math.Round(PricePerM3(s) * s.Volume(length), 2);

        /// <summary>Widest commercial profile with thickness &gt;= roughThickness (used for glued panel strips).</summary>
        public StockItem PanelStripStock(string speciesId, double roughThickness)
        {
            return StockFor(speciesId)
                .Where(s => Math.Min(s.Width, s.Thickness) >= roughThickness - 1e-6 && Math.Max(s.Width, s.Thickness) > Math.Min(s.Width, s.Thickness) * 1.5)
                .OrderBy(s => Math.Min(s.Width, s.Thickness))     // thinnest sufficient thickness
                .ThenByDescending(s => Math.Max(s.Width, s.Thickness))
                .FirstOrDefault();
        }

        public ToolItem SelectTool(OperationType op, double size)
        {
            return Tools.Values.Where(t => t.Operation == op && t.Diameter <= size + 1e-6)
                .OrderByDescending(t => t.Diameter).FirstOrDefault()
                ?? Tools.Values.Where(t => t.Operation == op).OrderBy(t => t.Diameter).FirstOrDefault();
        }

        public void Merge(WoodLibrary user)
        {
            foreach (var kv in user.Species) { kv.Value.IsUserDefined = true; Species[kv.Key] = kv.Value; }
            foreach (var kv in user.Suppliers) Suppliers[kv.Key] = kv.Value;
            foreach (var kv in user.Tools) Tools[kv.Key] = kv.Value;
            foreach (var kv in user.Hardware) Hardware[kv.Key] = kv.Value;
            foreach (var kv in user.Consumables) Consumables[kv.Key] = kv.Value;
            foreach (var s in user.Stock) { Stock.RemoveAll(x => x.Id == s.Id); Stock.Add(s); }
        }

        /// <summary>Default system data.</summary>
        public static WoodLibrary CreateDefault()
        {
            var lib = new WoodLibrary();
            lib.Suppliers["SUP-TIMBER"] = new Supplier { Id = "SUP-TIMBER", Name = "Default Timber Supplier" };
            lib.Suppliers["SUP-HW"] = new Supplier { Id = "SUP-HW", Name = "Default Hardware Supplier" };

            const string LWF = "DIN 68364 / DIN 68100 via LWF Bayern (RULES_EN_PEER_REVIEW_2026, Tab. 4-5)";
            void Sp(string id, string name, double dens, double e, double mor, double sc, double bMin, double bMax, double dR, double dT, double? shear,
                string stab, double price, double? reserve, string label, string src, params string[] joinery)
                => lib.Species[id] = new WoodSpecies
                {
                    Id = id, Name = name, DensityKgM3 = dens, YoungModulusMPa = e, ModulusOfRuptureMPa = mor, CompressionParallelMPa = sc,
                    BrinellPerpMin = bMin, BrinellPerpMax = bMax, DiffShrinkRadialPct = dR, DiffShrinkTangentialPct = dT,
                    TangentialMovementPerPercent = dT / 100.0, RadialMovementPerPercent = dR / 100.0, ShearStrengthMPa = shear,
                    DimensionalStability = stab, PricePerM3 = price, SupplierId = "SUP-TIMBER", ReservePercent = reserve,
                    DataLabel = label, DataSource = src, RecommendedJoinery = joinery.ToList(),
                    MachiningNotes = "Sharp tooling; climb-cut end grain carefully.", FinishingNotes = "Sand to P180 before oil/lacquer."
                };
            // European species - [V-DATA] (prices are placeholders: edit in the user library)
            Sp("OAK", "Oak", 710, 13000, 95, 52, 23, 42, 0.16, 0.36, null, "Good", 1400, null, "[V-DATA]", LWF, "mortise-tenon", "dowel", "dovetail");
            Sp("BEECH", "Beech", 715, 14000, 120, 60, 28, 40, 0.20, 0.41, 10.3, "Fair", 900, null, "[V-DATA]", LWF, "mortise-tenon", "dowel");
            Sp("ASH", "Ash", 700, 13000, 105, 50, 28, 40, 0.21, 0.38, null, "Fair", 1100, null, "[V-DATA]", LWF, "mortise-tenon", "bridle");
            Sp("MAPLE", "Maple (sycamore)", 630, 10500, 95, 50, 26, 34, 0.15, 0.26, null, "Fair", 1300, null, "[V-DATA]", LWF + "; R/T = midpoints of ranges", "dowel", "mortise-tenon");
            Sp("PINE", "Scots pine", 520, 11000, 85, 47, 19, 19, 0.19, 0.36, 6.2, "Fair", 450, null, "[V-DATA]", LWF, "dowel", "half-lap");
            Sp("SPRUCE", "Spruce", 460, 11000, 80, 45, 12, 12, 0.19, 0.39, null, "Fair", 380, null, "[V-DATA]", LWF, "half-lap", "dowel");
            Sp("WALNUT", "Walnut (European)", 680, 12500, 0, 0, 0, 0, 0.18, 0.29, null, "Very good", 2600, 15, "[V-DATA]", LWF + "; MOR/hardness extraction corrupt - not loaded", "mortise-tenon", "dovetail");
            Sp("CHERRY", "Cherry", 615, 10000, 0, 0, 0, 0, 0.17, 0.28, null, "Good", 2200, null, "[V-DATA]", LWF + "; MOR/hardness extraction corrupt - not loaded; R/T midpoints", "mortise-tenon", "dovetail");
            Sp("ROBINIA", "Black locust (robinia)", 740, 13600, 150, 73, 40, 57, 0.23, 0.35, null, "Good", 1000, null, "[V-DATA]", LWF, "mortise-tenon", "dowel");
            Sp("BIRCH", "Birch", 650, 14000, 120, 50, 23, 23, 0.29, 0.41, null, "Fair", 650, null, "[V-DATA]", LWF + "; ranges: mid values", "dowel", "mortise-tenon");
            Sp("LINDEN", "Linden", 530, 7400, 90, 44, 13, 20, 0.0, 0.0, null, "Good", 600, null, "[V-DATA]", LWF + "; shrinkage not listed - 0 means unknown", "dowel");
            Sp("ELM", "Elm", 650, 11000, 81, 51, 27, 37, 0.0, 0.0, null, "Fair", 1200, null, "[V-DATA]", LWF + "; shrinkage not listed - 0 means unknown", "mortise-tenon");
            Sp("ALDER", "Black alder", 550, 7700, 85, 47, 16, 17, 0.0, 0.0, null, "Good", 600, null, "[V-DATA]", LWF + "; shrinkage not listed - 0 means unknown", "dowel");
            Sp("POPLAR", "Black poplar", 450, 8800, 55, 30, 10, 10, 0.13, 0.31, null, "Fair", 450, null, "[V-DATA]", LWF, "dowel");
            // not covered by the verified report: kept as placeholders
            Sp("DOUGLAS", "Douglas fir", 530, 0, 0, 0, 0, 0, 0.18, 0.34, null, "Good", 520, null, "[UNVERIFIED]", "from memory - verify (Wood Handbook has US data)", "mortise-tenon", "bridle");
            Sp("MAHOGANY", "Mahogany", 550, 0, 0, 0, 0, 0, 0.20, 0.30, null, "Very good", 3000, 15, "[UNVERIFIED]", "from memory - verify", "mortise-tenon", "dovetail");

            // commercial profiles: generated for every species so any species is purchasable
            var sections = new (double w, double t, double[] lens)[]
            {
                (90, 90, new[] { 3000.0, 4000, 5000 }),
                (60, 60, new[] { 3000.0, 4000 }),
                (40, 140, new[] { 3000.0, 4000 }),
                (25, 140, new[] { 3000.0, 4000 }),
                (50, 200, new[] { 3000.0, 4000 }),
            };
            foreach (var sp in lib.Species.Values)
                foreach (var s in sections)
                    lib.Stock.Add(new StockItem
                    {
                        Id = sp.Id + "-" + s.w + "x" + s.t, SpeciesId = sp.Id, Width = s.w, Thickness = s.t,
                        Lengths = s.lens.ToList(), SupplierId = sp.SupplierId
                    });

            void Tool(string id, string name, string type, double d, double shank, double cl, double maxd, OperationType op, double rpm, double feed)
                => lib.Tools[id] = new ToolItem { Id = id, Name = name, Type = type, Diameter = d, Shank = shank, CuttingLength = cl, MaxDepth = maxd, Operation = op, RecommendedRpm = rpm, FeedMmPerMin = feed };
            foreach (var d in new[] { 3.0, 4, 5, 6, 8, 10 })
                Tool("DRILL-" + d, "Brad point drill " + d, "Drill", d, d, 60, 60, OperationType.Drill, 2500, 600);
            Tool("FORSTNER-35", "Forstner 35", "Forstner", 35, 10, 90, 40, OperationType.Drill, 600, 200);
            foreach (var d in new[] { 8.0, 10, 12, 16 })
                Tool("MORT-" + d, "Hollow chisel " + d, "Mortiser", d, d, 80, 75, OperationType.Mortise, 1800, 100);
            Tool("ROUT-8", "Straight router bit 8", "Router", 8, 8, 25, 25, OperationType.Rout, 18000, 3000);
            Tool("ROUT-SLOT-3", "Slot cutter 3mm", "Router", 3, 8, 12, 12, OperationType.Slot, 18000, 2500);
            Tool("DOVETAIL-14", "Dovetail bit 14 deg", "Dovetail", 12.7, 8, 12, 12, OperationType.Rout, 18000, 2000);
            Tool("SAW-TS-300", "Table saw blade 300 (kerf 3)", "Saw", 300, 30, 100, 100, OperationType.Crosscut, 4000, 0);
            Tool("SAW-TS-RIP", "Rip blade 300 (kerf 3)", "Saw", 300, 30, 100, 100, OperationType.Rip, 4000, 0);
            Tool("PLANER-HSS", "Planer/jointer knives", "Planer", 0, 0, 0, 0, OperationType.Plane, 0, 0);

            // hardware
            lib.Hardware["TOP-ZCLIP"] = new HardwareItem
            {
                Id = "TOP-ZCLIP", Category = "Tabletop fastener", Manufacturer = "Generic", Model = "Z-clip 30x20",
                Dimensions = new Vec3(30, 20, 8), UnitPrice = 0.45, TravelAllowance = 8,
                FastenerIds = { "SCR-4x16" }, FastenersPerUnit = { { "SCR-4x16", 1 } },
                RequiredToolIds = { "ROUT-SLOT-3", "DRILL-3" },
                InstallationNotesRo = "Frezează un canal de 3 mm pe fața interioară a zargii, la 12 mm sub marginea de sus; clipsul se agață în canal și se înșurubează pe fața inferioară a blatului.",
                InstallationNotes = "Cut a 3 mm slot in the inner face of the apron, 12 mm below the apron top; clip hooks into the slot and is screwed to the underside of the top.",
                Pattern =
                {
                    new HolePatternEntry { Target = PatternTarget.Host, Kind = FeatureKind.Slot, U = 0, V = 12, SizeU = 34, SizeV = 3, Depth = 10, Purpose = "Z-clip slot" },
                    new HolePatternEntry { Target = PatternTarget.Mate, Kind = FeatureKind.ScrewHole, U = 0, V = 20, Diameter = 3, Depth = 12, Purpose = "Z-clip pilot hole" },
                }
            };
            lib.Hardware["TOP-FIGURE8"] = new HardwareItem
            {
                Id = "TOP-FIGURE8", Category = "Tabletop fastener", Manufacturer = "Generic", Model = "Figure-8 fastener",
                Dimensions = new Vec3(30, 16, 5), UnitPrice = 0.6, TravelAllowance = 3,
                FastenerIds = { "SCR-4x16" }, FastenersPerUnit = { { "SCR-4x16", 2 } },
                RequiredToolIds = { "FORSTNER-35", "DRILL-3" },
                InstallationNotesRo = "Adâncitură în muchia de sus a zargii (la ~6 mm de fața interioară); un șurub în zargă și unul în fața inferioară a blatului. Balans mic: nu pentru blaturi mai late de ~450 mm.",
                InstallationNotes = "Recess in the top edge of the apron (about 6 mm from the inner face); one screw into the apron, one into the underside of the top. Limited swing: not recommended for tops wider than ~450 mm.",
                Pattern =
                {
                    new HolePatternEntry { Target = PatternTarget.Host, Kind = FeatureKind.Pocket, U = 0, V = 0, SizeU = 32, SizeV = 14, Depth = 8, Purpose = "Figure-8 recess" },
                    new HolePatternEntry { Target = PatternTarget.Mate, Kind = FeatureKind.ScrewHole, U = 0, V = 22, Diameter = 3, Depth = 12, Purpose = "Figure-8 pilot hole" },
                }
            };
            lib.Hardware["TOP-BUTTON"] = new HardwareItem
            {
                Id = "TOP-BUTTON", Category = "Tabletop fastener", Manufacturer = "Shop-made", Model = "Wooden button",
                Dimensions = new Vec3(40, 25, 10), UnitPrice = 0.3, TravelAllowance = 10,
                FastenerIds = { "SCR-4x35" }, FastenersPerUnit = { { "SCR-4x35", 1 }, { "WSH-4", 1 } },
                RequiredToolIds = { "ROUT-SLOT-3", "DRILL-5" },
                InstallationNotesRo = "Canal de 6 mm pe fața interioară a zargii; butonul din lemn tare (fibra pe lungime) alunecă în canal și se înșurubează pe blat printr-o gaură mai mare.",
                InstallationNotes = "Groove 6 mm wide in the inner face of the apron; a shop-made hardwood button (grain along its length) slides in the groove and is screwed to the underside of the top through an oversized hole.",
                Pattern =
                {
                    new HolePatternEntry { Target = PatternTarget.Host, Kind = FeatureKind.Slot, U = 0, V = 12, SizeU = 44, SizeV = 6, Depth = 10, Purpose = "Button groove" },
                    new HolePatternEntry { Target = PatternTarget.Mate, Kind = FeatureKind.ScrewHole, U = 0, V = 20, Diameter = 4, Depth = 15, Purpose = "Button screw pilot" },
                }
            };
            lib.Hardware["SCR-4x16"] = new HardwareItem { Id = "SCR-4x16", Category = "Screws", Manufacturer = "Generic", Model = "Wood screw 4x16", Dimensions = new Vec3(16, 4, 4), UnitPrice = 0.03 };
            lib.Hardware["SCR-4x35"] = new HardwareItem { Id = "SCR-4x35", Category = "Screws", Manufacturer = "Generic", Model = "Wood screw 4x35", Dimensions = new Vec3(35, 4, 4), UnitPrice = 0.05 };
            lib.Hardware["WSH-4"] = new HardwareItem { Id = "WSH-4", Category = "Washers", Manufacturer = "Generic", Model = "Washer 4", Dimensions = new Vec3(1, 12, 12), UnitPrice = 0.02 };
            lib.Hardware["BED-BOLT"] = new HardwareItem
            {
                Id = "BED-BOLT", Category = "Bed bolt", Manufacturer = "Generic", Model = "Hidden bed bolt M8 (stainless, with barrel nut)", Dimensions = new Vec3(90, 8, 8), UnitPrice = 12,
                InstallationNotesRo = "Bulon M8 prin picior în capătul lonjeronului; piuliță cilindrică în lonjeron; capul acoperit cu dop Ø20 (demontabil pentru transport).",
                InstallationNotes = "M8 bolt through the leg into the rail end with a barrel nut in the rail; head hidden by a Ø20 plug (knock-down for transport).",
                FastenersPerUnit = { { "BARREL-NUT-M8", 1 }, { "PLUG-20", 1 } },
                Pattern =
                {
                    new HolePatternEntry { Target = PatternTarget.Host, Kind = FeatureKind.BlindHole, U = 0, V = 0, Diameter = 8, Depth = 60, Purpose = "Bolt bore in the rail end" },
                    new HolePatternEntry { Target = PatternTarget.Mate, Kind = FeatureKind.ThroughHole, U = 0, V = 0, Diameter = 8, Depth = 70, Purpose = "Bolt hole through the leg" },
                    new HolePatternEntry { Target = PatternTarget.Mate, Kind = FeatureKind.Counterbore, U = 0, V = 0, Diameter = 20, Depth = 15, Purpose = "Plug recess" },
                }
            };
            lib.Hardware["BARREL-NUT-M8"] = new HardwareItem { Id = "BARREL-NUT-M8", Category = "Fasteners", Manufacturer = "Generic", Model = "Barrel nut M8 (12x20)", Dimensions = new Vec3(20, 12, 12), UnitPrice = 2 };
            lib.Hardware["PLUG-20"] = new HardwareItem { Id = "PLUG-20", Category = "Fasteners", Manufacturer = "Generic", Model = "Wood plug 20 mm", Dimensions = new Vec3(10, 20, 20), UnitPrice = 0.5 };
            lib.Hardware["HINGE-CUP35"] = new HardwareItem
            {
                Id = "HINGE-CUP35", Category = "Hinge", Manufacturer = "Generic", Model = "Soft-close cup hinge 35 mm (overlay)", Dimensions = new Vec3(48, 40, 12), UnitPrice = 12,
                InstallationNotesRo = "Cupă Ø35 adâncime 12 mm, la 22,5 mm de muchia de balamă și 100 mm de capete; placă de montaj pe lateral.",
                InstallationNotes = "Ø35 cup 12 mm deep, 22.5 mm from the hinge edge and 100 mm from the ends; mounting plate on the side panel.",
                FastenersPerUnit = { { "HINGE-PLATE", 1 } },
                Pattern = { new HolePatternEntry { Target = PatternTarget.Host, Kind = FeatureKind.HingeCup, U = 0, V = 0, Diameter = 35, Depth = 12, Purpose = "Hinge cup" } }
            };
            lib.Hardware["HINGE-PLATE"] = new HardwareItem { Id = "HINGE-PLATE", Category = "Fasteners", Manufacturer = "Generic", Model = "Hinge mounting plate", Dimensions = new Vec3(45, 40, 5), UnitPrice = 3 };
            lib.Hardware["ROD-25"] = new HardwareItem { Id = "ROD-25", Category = "Clothes rail", Manufacturer = "Generic", Model = "Clothes rail 25 mm with flanges", Dimensions = new Vec3(1000, 25, 25), UnitPrice = 28 };
            lib.Hardware["HANDLE-128"] = new HardwareItem
            {
                Id = "HANDLE-128", Category = "Handle", Manufacturer = "Generic", Model = "Drawer handle 128 mm", Dimensions = new Vec3(128, 12, 30), UnitPrice = 22,
                InstallationNotesRo = "Două găuri Ø5 la 128 mm între axe, centrate pe față.", InstallationNotes = "Two Ø5 holes at 128 mm centres, centred on the front.",
                Pattern =
                {
                    new HolePatternEntry { Target = PatternTarget.Host, Kind = FeatureKind.ThroughHole, U = -64, V = 0, Diameter = 5, Depth = 19, Purpose = "Handle screw hole" },
                    new HolePatternEntry { Target = PatternTarget.Host, Kind = FeatureKind.ThroughHole, U = 64, V = 0, Diameter = 5, Depth = 19, Purpose = "Handle screw hole" },
                }
            };
            lib.Hardware["PUSH-OPEN"] = new HardwareItem { Id = "PUSH-OPEN", Category = "Push latch", Manufacturer = "Generic", Model = "Push-to-open latch", Dimensions = new Vec3(40, 20, 15), UnitPrice = 35 };
            lib.Hardware["ANTITIP-KIT"] = new HardwareItem { Id = "ANTITIP-KIT", Category = "Safety", Manufacturer = "Generic", Model = "Anti-tip wall anchor kit (EN 14749 / EN 15939)", Dimensions = new Vec3(60, 20, 20), UnitPrice = 18 };
            lib.Hardware["SLIDE-SC"] = new HardwareItem { Id = "SLIDE-SC", Category = "Drawer slide", Manufacturer = "Generic", Model = "Hidden soft-close drawer slide 350 (pair)", Dimensions = new Vec3(350, 13, 45), UnitPrice = 95 };
            lib.Hardware["FOOT-LEVEL"] = new HardwareItem { Id = "FOOT-LEVEL", Category = "Feet", Manufacturer = "Generic", Model = "Adjustable levelling foot", Dimensions = new Vec3(30, 30, 20), UnitPrice = 6 };
            lib.Hardware["TOP-SLOTSCREW"] = new HardwareItem
            {
                Id = "TOP-SLOTSCREW", Category = "Tabletop fastener", Manufacturer = "Generic", Model = "Slotted-hole screw 4x35 + washer",
                Dimensions = new Vec3(35, 4, 4), UnitPrice = 0.07, TravelAllowance = 10,
                FastenerIds = { "SCR-4x35", "WSH-4" }, FastenersPerUnit = { { "SCR-4x35", 1 }, { "WSH-4", 1 } },
                RequiredToolIds = { "DRILL-5", "ROUT-8" },
                InstallationNotesRo = "Gaură alungită în zargă (axa lungă pe direcția de mișcare) lasă blatul să se miște; șurubul intră în fața inferioară a blatului.",
                InstallationNotes = "Elongated hole in the apron (long axis along the movement direction) lets the top move; screw into the top underside.",
                Pattern =
                {
                    new HolePatternEntry { Target = PatternTarget.Host, Kind = FeatureKind.ElongatedHole, U = 0, V = 0, Diameter = 5, SizeU = 18, SizeV = 5, Depth = 25, Purpose = "Elongated screw slot (allows top movement)" },
                    new HolePatternEntry { Target = PatternTarget.Mate, Kind = FeatureKind.ScrewHole, U = 0, V = 0, Diameter = 3, Depth = 20, Purpose = "Pilot hole" },
                }
            };
            lib.Hardware["TOP-SLOTSCREW-L"] = new HardwareItem
            {
                Id = "TOP-SLOTSCREW-L", Category = "Tabletop fastener", Manufacturer = "Generic", Model = "Slotted-hole screw 4x35 + washer (long slot)",
                Dimensions = new Vec3(35, 4, 4), UnitPrice = 0.07, TravelAllowance = 26,
                FastenerIds = { "SCR-4x35", "WSH-4" }, FastenersPerUnit = { { "SCR-4x35", 1 }, { "WSH-4", 1 } },
                RequiredToolIds = { "DRILL-5", "ROUT-8" },
                InstallationNotesRo = "Gaură alungită în zargă (axa lungă pe direcția de mișcare) lasă blatul să se miște; șurubul intră în fața inferioară a blatului.",
                InstallationNotes = "Elongated hole in the apron (long axis along the movement direction) lets the top move; screw into the top underside.",
                Pattern =
                {
                    new HolePatternEntry { Target = PatternTarget.Host, Kind = FeatureKind.ElongatedHole, U = 0, V = 0, Diameter = 5, SizeU = 34, SizeV = 5, Depth = 25, Purpose = "Elongated screw slot (allows top movement)" },
                    new HolePatternEntry { Target = PatternTarget.Mate, Kind = FeatureKind.ScrewHole, U = 0, V = 0, Diameter = 3, Depth = 20, Purpose = "Pilot hole" },
                }
            };

            lib.Consumables["GLUE-PVAC"] = new ConsumableItem { Id = "GLUE-PVAC", Name = "PVA D3 wood glue", Unit = "m2 glue line", UnitPrice = 1.2 };
            lib.Consumables["FINISH-OIL"] = new ConsumableItem { Id = "FINISH-OIL", Name = "Hardwax oil", Unit = "m2 surface", UnitPrice = 4.5 };
            return lib;
        }
    }
}
