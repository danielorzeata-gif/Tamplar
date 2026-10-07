using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using RhinoWood.Core.Domain;
using RhinoWood.Core.Geometry;
using RhinoWood.Core.Libraries;

namespace RhinoWood.Core.Display
{
    public enum PrimKind { Box, Cylinder, Line }

    /// <summary>A volume removed from a part (mortise, hole, tenon cheek...), in WORLD coordinates. Boxes use Box, cylinders use P0/P1/Radius.</summary>
    public sealed class CutVolume
    {
        public PrimKind Kind { get; set; }
        public Box3 Box { get; set; }
        public Vec3 P0 { get; set; }
        public Vec3 P1 { get; set; }
        public double Radius { get; set; }
        public string Label { get; set; }
    }
    public enum PrimCategory { Part, Feature, Hardware, Grain, Operation }

    /// <summary>Renderer-independent geometry description; the Rhino plugin converts these into Breps/curves.</summary>
    public sealed class GeometryPrimitive
    {
        public string Key { get; set; }          // stable object id within the project
        public string PartId { get; set; }
        public PrimKind Kind { get; set; }
        public PrimCategory Category { get; set; }
        public Box3 Box { get; set; }
        public Vec3 P0 { get; set; }
        public Vec3 P1 { get; set; }
        public double Radius { get; set; }
        public string Label { get; set; }
        public string Version { get; set; }
        /// <summary>For Part primitives (Normal mode and above): volumes to subtract so the part is a real cut solid, not a bounding box.</summary>
        public List<CutVolume> Cuts { get; set; }
    }

    public static class Hashing
    {
        public static string Short(string s)
        {
            using (var sha = SHA256.Create())
            {
                var h = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
                return string.Concat(h.Take(8).Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }
    }

    public sealed class GeometryCache
    {
        private readonly Dictionary<string, List<GeometryPrimitive>> _items = new Dictionary<string, List<GeometryPrimitive>>();
        public int Hits { get; private set; }
        public int Misses { get; private set; }
        public int Count => _items.Count;
        public List<GeometryPrimitive> GetOrAdd(string key, Func<List<GeometryPrimitive>> factory)
        {
            if (_items.TryGetValue(key, out var v)) { Hits++; return v; }
            Misses++;
            return _items[key] = factory();
        }
        public void Clear() { _items.Clear(); Hits = Misses = 0; }
        public void ResetCounters() { Hits = Misses = 0; }
    }

    /// <summary>Level-of-detail geometry. Detail (joinery, holes, hardware, operations) is generated on demand and cached by hash.</summary>
    public sealed class GeometryEngine
    {
        private readonly GeometryCache _cache;
        public GeometryEngine(GeometryCache cache) { _cache = cache; }

        public static string PartFingerprint(PartInstance p)
        {
            var sb = new StringBuilder();
            sb.Append(p.Id).Append(p.Bounds.ToString());
            foreach (var f in p.Features) sb.Append(f.Kind).Append(f.HasBox ? f.Box.ToString() : f.Position + "/" + f.Direction + "/" + f.Diameter + "/" + f.Depth).Append(f.Angle);
            return Hashing.Short(sb.ToString());
        }

        public List<GeometryPrimitive> Generate(FurnitureModel model, DisplayMode mode, bool showGrain = false)
        {
            var all = new List<GeometryPrimitive>();
            foreach (var part in model.AllParts)
            {
                string fp = PartFingerprint(part);
                var fam = model.FamilyOf(part.Id);
                bool grain = showGrain || mode >= DisplayMode.Engineering;
                var key = part.Id + "|" + fp + "|" + mode + "|" + (grain ? "g" : "-") + "|s" + (mode >= DisplayMode.Normal ? PartSolids.StripCount(fam) : 1);
                all.AddRange(_cache.GetOrAdd(key, () => Build(part, fam, mode, grain, fp)));
            }
            foreach (var sp in model.SheetParts)
                all.Add(new GeometryPrimitive { Key = sp.Id, PartId = sp.Id, Kind = PrimKind.Box, Category = PrimCategory.Part, Box = sp.Bounds, Label = sp.Name + " (" + sp.Material + ")", Version = Hashing.Short(sp.Bounds.ToString()) });
            if (mode >= DisplayMode.Engineering)
                foreach (var b in model.Biscuits)
                {
                    var key = "BSC|" + b.Id + "|" + b.Box;
                    all.AddRange(_cache.GetOrAdd(key, () => new List<GeometryPrimitive>
                    {
                        new GeometryPrimitive { Key = b.Id, PartId = b.PartId, Kind = PrimKind.Box, Category = PrimCategory.Hardware, Box = b.Box, Label = "Lamelă " + b.Size + " (biscuit)", Version = Hashing.Short(b.Box.ToString()) }
                    }));
                }
            if (mode >= DisplayMode.Engineering)
                foreach (var h in model.HardwareInstalls)
                {
                    var key = "HW|" + h.Id + "|" + h.Point + h.HardwareId + "|" + mode;
                    all.AddRange(_cache.GetOrAdd(key, () => new List<GeometryPrimitive>
                    {
                        new GeometryPrimitive { Key = h.Id, PartId = h.HostPartId, Kind = PrimKind.Box, Category = PrimCategory.Hardware, Box = Box3.FromMinSize(h.Point - new Vec3(15, 15, 4), new Vec3(30, 30, 8)), Label = h.HardwareId, Version = Hashing.Short(h.Point.ToString()) }
                    }));
                }
            return all;
        }

        private static List<GeometryPrimitive> Build(PartInstance part, PartFamily fam, DisplayMode mode, bool grain, string version)
        {
            var list = new List<GeometryPrimitive>();
            var cuts = mode >= DisplayMode.Normal ? PartSolids.CutsFor(part) : null;
            var strips = mode >= DisplayMode.Normal ? PartSolids.StripBoxes(part, fam) : new List<Box3> { part.Bounds };
            if (strips.Count == 1)
                list.Add(new GeometryPrimitive { Key = part.Id, PartId = part.Id, Kind = PrimKind.Box, Category = PrimCategory.Part, Box = part.Bounds, Label = fam?.Name ?? part.Id, Version = version, Cuts = cuts });
            else
                for (int s = 0; s < strips.Count; s++)       // edge-glued panel: every strip is its own solid so the boards are visible
                    list.Add(new GeometryPrimitive { Key = part.Id + "#S" + (s + 1), PartId = part.Id, Kind = PrimKind.Box, Category = PrimCategory.Part, Box = strips[s], Label = (fam?.Name ?? part.Id) + " · lamela " + (s + 1) + "/" + strips.Count, Version = version + "s" + s, Cuts = cuts == null ? null : PartSolids.CutsIn(cuts, strips[s]) });
            if (mode >= DisplayMode.Engineering)
            {
                int i = 0;
                foreach (var f in part.Features)
                {
                    string key = part.Id + "#" + f.Id + "#" + (++i);
                    if (f.HasBox)
                        list.Add(new GeometryPrimitive { Key = key, PartId = part.Id, Kind = PrimKind.Box, Category = PrimCategory.Feature, Box = part.FeatureWorldBox(f), Label = f.Describe(), Version = version });
                    else
                    {
                        var a = part.LocalToWorld(f.Position); var b = part.LocalToWorld(f.Position + f.Direction * f.Depth);
                        list.Add(new GeometryPrimitive { Key = key, PartId = part.Id, Kind = PrimKind.Cylinder, Category = PrimCategory.Feature, P0 = a, P1 = b, Radius = f.Diameter / 2, Label = f.Describe(), Version = version });
                    }
                    if (mode == DisplayMode.Manufacturing)
                        list.Add(new GeometryPrimitive { Key = key + "~op", PartId = part.Id, Kind = PrimKind.Line, Category = PrimCategory.Operation, P0 = part.LocalToWorld(f.HasBox ? f.Box.Center : f.Position), P1 = part.LocalToWorld(f.HasBox ? f.Box.Center : f.Position) + new Vec3(0, 0, 25), Label = (f.ToolId ?? "") + " " + f.Purpose, Version = version });
                }
            }
            if (grain)
            {
                var ax = fam?.GrainAxis ?? part.LengthAxis;
                for (int s = 0; s < strips.Count; s++)
                {
                    var c = strips[s].Center; double half = strips[s].Size.Get(ax) / 2;
                    list.Add(new GeometryPrimitive { Key = part.Id + "#grain" + (strips.Count > 1 ? "" + (s + 1) : ""), PartId = part.Id, Kind = PrimKind.Line, Category = PrimCategory.Grain, P0 = c - Vec3.Unit(ax, half * 0.8), P1 = c + Vec3.Unit(ax, half * 0.8), Label = "grain " + ax, Version = version + "s" + s });
                }
            }
            return list;
        }
    }
}
