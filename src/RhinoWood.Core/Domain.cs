using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RhinoWood.Core.Geometry;

namespace RhinoWood.Core.Domain
{
    public enum PartType { Leg, Apron, Rail, Stretcher, Top, Panel, Frame, Door, Drawer, Shelf, Back, Foot, Support, Bracket, Custom }
    public enum Severity { Info, Warning, Error }
    public enum DisplayMode { Performance, Normal, Engineering, Manufacturing }
    public enum OptimizationStrategy { MinPurchase, MinWaste, MinCost, MinBoards, GrainFirst, Balanced }
    public enum RemnantClass { PurchasedStock, ProjectRemnant, ProcessWaste, Scrap }
    public enum OperationType { Stock, Cut, Joint, Plane, Rip, Crosscut, Glue, Rout, Drill, Mortise, Tenon, Slot, Sand, Assemble, Finish }

    public enum FeatureKind
    {
        ThroughHole, BlindHole, Counterbore, Countersink, Pocket, Slot, ElongatedHole, DowelHole, ScrewHole,
        HingeCup, ShelfPin, ConnectorHole, ThreadedHole, Mortise, Tenon, RoutPocket, Dado, Rabbet, Lap, BiscuitSlot,
        Finger, Dovetail, Scarf, WedgeSlot
    }

    public interface IFingerprint { string Fingerprint { get; } }

    public readonly record struct Dims(double Length, double Width, double Thickness)
    {
        public double VolumeM3 => Length * Width * Thickness / 1e9;
        public override string ToString() =>
            string.Format(CultureInfo.InvariantCulture, "{0:0.#} x {1:0.#} x {2:0.#}", Length, Width, Thickness);
    }

    public sealed class WoodSpecies
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double DensityKgM3 { get; set; }
        /// <summary>DEPRECATED: never load Janka from memory/unverified extractions (RULES_EN_PEER_REVIEW_2026 R17). Use Brinell.</summary>
        public double JankaHardnessN { get; set; }
        public double YoungModulusMPa { get; set; }
        public double ModulusOfRuptureMPa { get; set; }
        public double CompressionParallelMPa { get; set; }
        /// <summary>Brinell hardness perpendicular to grain, N/mm2 (DIN 68364), min..max.</summary>
        public double BrinellPerpMin { get; set; }
        public double BrinellPerpMax { get; set; }
        /// <summary>Radial differential shrinkage, %/% moisture (DIN 68100).</summary>
        public double DiffShrinkRadialPct { get; set; }
        /// <summary>Tangential differential shrinkage, %/% moisture (DIN 68100); TangentialMovementPerPercent = this / 100.</summary>
        public double DiffShrinkTangentialPct { get; set; }
        /// <summary>Shear strength along grain, MPa (needed by the tenon capacity equation E1; null = unknown).</summary>
        public double? ShearStrengthMPa { get; set; }
        /// <summary>Provenance: [V-DATA] source data, [PROXY] borrowed from a related species, [UNVERIFIED] from memory.</summary>
        public string DataLabel { get; set; } = "[UNVERIFIED]";
        public string DataSource { get; set; }
        /// <summary>Tangential movement, fraction of dimension per 1% moisture-content change.</summary>
        public double TangentialMovementPerPercent { get; set; }
        public double RadialMovementPerPercent { get; set; }
        public string DimensionalStability { get; set; }
        public List<string> RecommendedJoinery { get; set; } = new List<string>();
        public string MachiningNotes { get; set; }
        public string FinishingNotes { get; set; }
        public double PricePerM3 { get; set; }
        public string SupplierId { get; set; }
        /// <summary>Material specific reserve (null = use global).</summary>
        public double? ReservePercent { get; set; }
        public bool IsUserDefined { get; set; }
    }

    public sealed class Supplier
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Contact { get; set; }
    }

    /// <summary>A commercially purchasable profile: species x section x set of available lengths.</summary>
    public sealed class StockItem
    {
        public string Id { get; set; }
        public string SpeciesId { get; set; }
        public double Width { get; set; }
        public double Thickness { get; set; }
        public List<double> Lengths { get; set; } = new List<double>();
        /// <summary>Price per m3 override; 0 = use species price.</summary>
        public double PricePerM3 { get; set; }
        public string SupplierId { get; set; }
        public double? ReservePercent { get; set; }
        public string Label => string.Format(CultureInfo.InvariantCulture, "{0} {1:0.#}x{2:0.#}", SpeciesId, Width, Thickness);
        public double SectionArea => Width * Thickness;
        public double Volume(double length) => Width * Thickness * length / 1e9;

        public bool SectionFits(double a, double b)
        {
            double sMax = Math.Max(a, b), sMin = Math.Min(a, b);
            double wMax = Math.Max(Width, Thickness), wMin = Math.Min(Width, Thickness);
            return sMax <= wMax + 1e-6 && sMin <= wMin + 1e-6;
        }
    }

    public enum LocalFace { XMin, XMax, YMin, YMax, ZMin, ZMax }

    /// <summary>A manufacturing feature in the PART-LOCAL frame (x = length, y = width, z = thickness, origin at min corner).</summary>
    public sealed class Feature
    {
        public string Id { get; set; }
        public string PartId { get; set; }
        public FeatureKind Kind { get; set; }
        public Vec3 Position { get; set; }
        public Vec3 Direction { get; set; }
        public double Diameter { get; set; }
        public bool HasBox { get; set; }
        public Box3 Box { get; set; }
        public double Depth { get; set; }
        public double Tolerance { get; set; }
        public bool IsThrough { get; set; }
        public string Purpose { get; set; }
        public string ToolId { get; set; }
        public string SourceId { get; set; }
        public double Angle { get; set; }

        public bool IsHole => !HasBox;
        public string Describe() => HasBox
            ? string.Format(CultureInfo.InvariantCulture, "{0} {1:0.#}x{2:0.#}x{3:0.#}", Kind, Box.Size.X, Box.Size.Y, Box.Size.Z)
            : string.Format(CultureInfo.InvariantCulture, "{0} D{1:0.#} depth {2:0.#}", Kind, Diameter, Depth);
    }

    public sealed class PartInstance
    {
        public string Id { get; set; }
        public string FamilyId { get; set; }
        public int Index { get; set; }
        public Box3 Bounds { get; set; }
        public Axis LengthAxis { get; set; }
        public Axis WidthAxis { get; set; }
        public Axis ThicknessAxis { get; set; }
        public List<Feature> Features { get; } = new List<Feature>();

        public Dims Finished => new Dims(Bounds.Size.Get(LengthAxis), Bounds.Size.Get(WidthAxis), Bounds.Size.Get(ThicknessAxis));

        public Vec3 LocalToWorld(Vec3 l)
        {
            var w = Bounds.Min;
            w = w.With(LengthAxis, w.Get(LengthAxis) + l.X);
            w = w.With(WidthAxis, w.Get(WidthAxis) + l.Y);
            w = w.With(ThicknessAxis, w.Get(ThicknessAxis) + l.Z);
            return w;
        }
        public Vec3 WorldToLocal(Vec3 w) => new Vec3(
            w.Get(LengthAxis) - Bounds.Min.Get(LengthAxis),
            w.Get(WidthAxis) - Bounds.Min.Get(WidthAxis),
            w.Get(ThicknessAxis) - Bounds.Min.Get(ThicknessAxis));
        public Box3 LocalBoxToWorld(Box3 lb) => new Box3(LocalToWorld(lb.Min), LocalToWorld(lb.Max));
        public Box3 WorldBoxToLocal(Box3 wb) => new Box3(WorldToLocal(wb.Min), WorldToLocal(wb.Max));
        /// <summary>Maps a world direction to the local frame.</summary>
        public Vec3 WorldDirToLocal(Vec3 d) => new Vec3(d.Get(LengthAxis), d.Get(WidthAxis), d.Get(ThicknessAxis));

        public Box3 FeatureWorldBox(Feature f)
        {
            if (f.HasBox) return LocalBoxToWorld(f.Box);
            var a = f.Position;
            var b = f.Position + f.Direction * f.Depth;
            double r = f.Diameter / 2;
            Vec3 lo = new Vec3(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Min(a.Z, b.Z));
            Vec3 hi = new Vec3(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y), Math.Max(a.Z, b.Z));
            if (Math.Abs(f.Direction.X) < 0.5) { lo = lo.With(Axis.X, lo.X - r); hi = hi.With(Axis.X, hi.X + r); }
            if (Math.Abs(f.Direction.Y) < 0.5) { lo = lo.With(Axis.Y, lo.Y - r); hi = hi.With(Axis.Y, hi.Y + r); }
            if (Math.Abs(f.Direction.Z) < 0.5) { lo = lo.With(Axis.Z, lo.Z - r); hi = hi.With(Axis.Z, hi.Z + r); }
            return LocalBoxToWorld(new Box3(lo, hi));
        }
    }

    /// <summary>One rough (pre-machining) piece that must be cut from commercial stock.</summary>
    public sealed class RoughPieceSpec
    {
        public int CountPerPart { get; set; } = 1;
        public Dims Rough { get; set; }
        public string Role { get; set; }
    }

    /// <summary>Identical/equivalent parts grouped (e.g. TABLE LEG x4).</summary>
    public sealed class PartFamily
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public PartType Type { get; set; }
        public string Assembly { get; set; }
        public string SpeciesId { get; set; }
        public Dims Finished { get; set; }
        public List<RoughPieceSpec> RoughPieces { get; set; } = new List<RoughPieceSpec>();
        public Axis GrainAxis { get; set; }
        public bool GrainAlongLength { get; set; } = true;
        public bool RequiresGrainContinuity { get; set; }
        public bool VisualGrainRequired { get; set; }
        /// <summary>True for panels made of edge-glued strips (RoughPieces count = strips per part); strips are drawn separately and joined with biscuits.</summary>
        public bool EdgeGlued { get; set; }
        /// <summary>A panel floating in grooves (headboard, door, back): free to move, so it needs no fasteners.</summary>
        public bool Floating { get; set; }
        /// <summary>Visibility class (A = visible closed, B = visible when open, C = hidden); drives the species per variant.</summary>
        public char VisClass { get; set; } = 'A';
        public string GrainGroup { get; set; }
        public List<PartInstance> Instances { get; set; } = new List<PartInstance>();
        public string Notes { get; set; }
        public int Quantity => Instances.Count;
        public double UnitCost { get; set; }
    }

    public sealed class FurnitureModel
    {
        public string TypeId { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public List<PartFamily> Families { get; set; } = new List<PartFamily>();
        public List<JointInstance> Joints { get; set; } = new List<JointInstance>();
        public List<HardwareInstall> HardwareInstalls { get; set; } = new List<HardwareInstall>();
        /// <summary>Biscuits (lamelle) that join the edge-glued strips of panels; count and position follow the panel size and the biscuit pitch.</summary>
        public List<BiscuitInstance> Biscuits { get; set; } = new List<BiscuitInstance>();
        /// <summary>Non-wood sheet parts (HDF back, drawer bottoms): shown and priced by area, not cut from boards.</summary>
        public List<SheetPart> SheetParts { get; set; } = new List<SheetPart>();
        public List<Issue> Issues { get; set; } = new List<Issue>();
        public IEnumerable<PartInstance> AllParts => Families.SelectMany(f => f.Instances);
        public PartInstance FindPart(string id) => AllParts.FirstOrDefault(p => p.Id == id);
        public PartFamily FamilyOf(string partId) => Families.FirstOrDefault(f => f.Instances.Any(i => i.Id == partId));
        public Box3 Bounds => AllParts.Select(p => p.Bounds).Aggregate((a, b) => a.Union(b));
    }

    /// <summary>A flat biscuit (#10/#20) in an edge joint between two strips of a glued panel. Box = the biscuit body in world coordinates.</summary>
    public sealed class SheetPart
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Material { get; set; }
        public Box3 Bounds { get; set; }
        public double Thickness { get; set; }
        public double AreaM2 { get; set; }
    }

    public sealed class BiscuitInstance
    {
        public string Id { get; set; }
        public string PartId { get; set; }
        public int Edge { get; set; }
        public string Size { get; set; }
        public Box3 Box { get; set; }
    }

    public sealed class JointInstance
    {
        public string Id { get; set; }
        public string JointTypeId { get; set; }
        public string PartAId { get; set; }
        public string PartBId { get; set; }
        public Dictionary<string, double> Parameters { get; set; } = new Dictionary<string, double>();
        public List<string> FeatureIds { get; set; } = new List<string>();
        public bool Mitred { get; set; }
        public string Description { get; set; }
    }

    public sealed class HardwareInstall
    {
        public string Id { get; set; }
        public string HardwareId { get; set; }
        public string HostPartId { get; set; }
        public string MatePartId { get; set; }
        public Vec3 Point { get; set; }
        public Vec3 Normal { get; set; }
        public Vec3 AxisU { get; set; }
        public Vec3 AxisV { get; set; }
        public Vec3 MatePoint { get; set; }
        public Vec3 MateNormal { get; set; }
        public Vec3 MateAxisV { get; set; }
        public int Quantity { get; set; } = 1;
        public List<string> FeatureIds { get; set; } = new List<string>();
    }

    public sealed class Issue
    {
        public Severity Severity { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
        public string SubjectId { get; set; }
        public override string ToString() => Severity.ToString().ToUpperInvariant() + " [" + Code + "] " + Message + (SubjectId != null ? " (" + SubjectId + ")" : "");
    }
}
