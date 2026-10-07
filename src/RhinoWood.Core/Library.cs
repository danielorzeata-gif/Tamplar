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

            void Sp(string id, string name, double dens, double janka, double tang, double rad, string stab, double price, double? reserve, params string[] joinery)
                => lib.Species[id] = new WoodSpecies
                {
                    Id = id, Name = name, DensityKgM3 = dens, JankaHardnessN = janka, TangentialMovementPerPercent = tang,
                    RadialMovementPerPercent = rad, DimensionalStability = stab, PricePerM3 = price, SupplierId = "SUP-TIMBER",
                    ReservePercent = reserve, RecommendedJoinery = joinery.ToList(),
                    MachiningNotes = "Sharp tooling; climb-cut end grain carefully.", FinishingNotes = "Sand to P180 before oil/lacquer."
                };
            Sp("OAK", "Oak", 720, 5340, 0.0040, 0.0020, "Good", 1400, null, "mortise-tenon", "dowel", "dovetail");
            Sp("BEECH", "Beech", 710, 5800, 0.0046, 0.0020, "Fair", 900, null, "mortise-tenon", "dowel");
            Sp("ASH", "Ash", 690, 5870, 0.0041, 0.0020, "Fair", 1100, null, "mortise-tenon", "bridle");
            Sp("WALNUT", "Walnut", 650, 4490, 0.0050, 0.0026, "Very good", 2600, 15, "mortise-tenon", "dovetail");
            Sp("MAPLE", "Maple", 700, 6450, 0.0050, 0.0030, "Fair", 1300, null, "dowel", "mortise-tenon");
            Sp("PINE", "Pine", 520, 2250, 0.0036, 0.0019, "Fair", 450, null, "dowel", "half-lap");
            Sp("SPRUCE", "Spruce", 450, 1700, 0.0036, 0.0019, "Fair", 380, null, "half-lap", "dowel");
            Sp("DOUGLAS", "Douglas Fir", 530, 2700, 0.0034, 0.0018, "Good", 520, null, "mortise-tenon", "bridle");
            Sp("CHERRY", "Cherry", 580, 4220, 0.0042, 0.0021, "Good", 2200, null, "mortise-tenon", "dovetail");
            Sp("MAHOGANY", "Mahogany", 550, 3600, 0.0030, 0.0020, "Very good", 3000, 15, "mortise-tenon", "dovetail");

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
                InstallationNotes = "Cut a 3 mm slot in the inner face of the apron, 12 mm below the apron top; clip hooks into the slot and is screwed to the underside of the top.",
                Pattern =
                {
                    new HolePatternEntry { Target = PatternTarget.Host, Kind = FeatureKind.Slot, U = 0, V = 12, SizeU = 34, SizeV = 3, Depth = 10, Purpose = "Z-clip slot" },
                    new HolePatternEntry { Target = PatternTarget.Mate, Kind = FeatureKind.ScrewHole, U = 0, V = 20, Diameter = 3, Depth = 12, Purpose = "Z-clip pilot hole" },
                }
            };
            lib.Hardware["SCR-4x16"] = new HardwareItem { Id = "SCR-4x16", Category = "Screws", Manufacturer = "Generic", Model = "Wood screw 4x16", Dimensions = new Vec3(16, 4, 4), UnitPrice = 0.03 };
            lib.Hardware["SCR-4x35"] = new HardwareItem { Id = "SCR-4x35", Category = "Screws", Manufacturer = "Generic", Model = "Wood screw 4x35", Dimensions = new Vec3(35, 4, 4), UnitPrice = 0.05 };
            lib.Hardware["WSH-4"] = new HardwareItem { Id = "WSH-4", Category = "Washers", Manufacturer = "Generic", Model = "Washer 4", Dimensions = new Vec3(1, 12, 12), UnitPrice = 0.02 };
            lib.Hardware["TOP-SLOTSCREW"] = new HardwareItem
            {
                Id = "TOP-SLOTSCREW", Category = "Tabletop fastener", Manufacturer = "Generic", Model = "Slotted-hole screw 4x35 + washer",
                Dimensions = new Vec3(35, 4, 4), UnitPrice = 0.07, TravelAllowance = 10,
                FastenerIds = { "SCR-4x35", "WSH-4" }, FastenersPerUnit = { { "SCR-4x35", 1 }, { "WSH-4", 1 } },
                RequiredToolIds = { "DRILL-5", "ROUT-8" },
                InstallationNotes = "Elongated hole in the apron (long axis along the movement direction) lets the top move; screw into the top underside.",
                Pattern =
                {
                    new HolePatternEntry { Target = PatternTarget.Host, Kind = FeatureKind.ElongatedHole, U = 0, V = 0, Diameter = 5, SizeU = 18, SizeV = 5, Depth = 25, Purpose = "Elongated screw slot (allows top movement)" },
                    new HolePatternEntry { Target = PatternTarget.Mate, Kind = FeatureKind.ScrewHole, U = 0, V = 0, Diameter = 3, Depth = 20, Purpose = "Pilot hole" },
                }
            };

            lib.Consumables["GLUE-PVAC"] = new ConsumableItem { Id = "GLUE-PVAC", Name = "PVA D3 wood glue", Unit = "m2 glue line", UnitPrice = 1.2 };
            lib.Consumables["FINISH-OIL"] = new ConsumableItem { Id = "FINISH-OIL", Name = "Hardwax oil", Unit = "m2 surface", UnitPrice = 4.5 };
            return lib;
        }
    }
}
