using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RhinoWood.Core.Costing;
using RhinoWood.Core.Display;
using RhinoWood.Core.Domain;
using RhinoWood.Core.Furniture;
using RhinoWood.Core.Joinery;
using RhinoWood.Core.Libraries;
using RhinoWood.Core.Manufacturing;
using RhinoWood.Core.Optimization;
using RhinoWood.Core.Parametric;
using RhinoWood.Core.Rules;
using RhinoWood.Core.Validation;

namespace RhinoWood.Core.Projects
{
    public sealed class ProjectResult
    {
        public FurnitureModel Model { get; set; }
        public List<CutDemand> Demands { get; set; }
        public OptimizationResult Optimization { get; set; }
        public ManufacturingPlan Manufacturing { get; set; }
        public Bom Bom { get; set; }
        public CostSummary Cost { get; set; }
        public List<Issue> Issues { get; set; }
        public List<string> RecomputedNodes { get; set; } = new List<string>();
        public bool OptimizationReused { get; set; }
        public string Fingerprint { get; set; }
        public bool HasErrors => Issues.Any(i => i.Severity == Severity.Error);
    }

    public enum OverrideResolution { Recalculate, KeepManualOverride, ConvertToCustomComponent }

    /// <summary>
    /// Source of truth: parameters + overrides + selections + settings. Everything else (parts, joinery, plan) is derived through the
    /// dependency graph and can be rebuilt at any time (e.g. after reopening the project).
    /// </summary>
    public sealed class WoodProject
    {
        public string Id { get; private set; }
        public string Name { get; set; }
        public string SpeciesId { get; private set; }
        public IFurnitureDefinition Furniture { get; private set; }
        public Dictionary<string, double> Parameters { get; private set; }
        public ProjectSettings Settings { get; set; } = new ProjectSettings();
        public WoodLibrary Library { get; }
        public JointRegistry Joints { get; }
        public FurnitureRegistry FurnitureTypes { get; }
        public Validator Validator { get; set; } = Validator.CreateDefault();
        public GeometryCache GeometryCache { get; } = new GeometryCache();
        public DependencyGraph Graph { get; private set; }
        /// <summary>Components the user converted to custom (frozen) components: node id list.</summary>
        public List<string> CustomComponents { get; } = new List<string>();

        private FurnitureModel _model; private string _graphKey;
        private string _optKey; private ProjectResult _last;

        public WoodProject(string name, IFurnitureDefinition furniture, string speciesId, WoodLibrary lib, JointRegistry joints, FurnitureRegistry types)
        {
            Id = "PRJ-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            Name = name; Furniture = furniture; SpeciesId = speciesId; Library = lib; Joints = joints; FurnitureTypes = types;
            Parameters = furniture.Parameters.ToDictionary(p => p.Key, p => p.Default);
            Rebuild();
        }

        public static WoodProject CreateTable(string name, string speciesId = "OAK", WoodLibrary lib = null)
        {
            lib = lib ?? WoodLibrary.CreateDefault();
            var types = FurnitureRegistry.CreateDefault();
            return new WoodProject(name, types.Get("table.dining"), speciesId, lib, JointRegistry.CreateDefault(), types);
        }

        internal void ForceId(string id) { Id = id; }

        private ProjectContext Context => new ProjectContext { Library = Library, Joints = Joints, Rules = Settings.Rules };

        /// <summary>Re-creates the dependency graph (after species / rules / library change) preserving overrides.</summary>
        public void Rebuild()
        {
            var saved = Graph?.Overrides.ToList() ?? new List<KeyValuePair<string, NumericOverride>>();
            Graph = Furniture.CreateGraph(Context, Parameters, SpeciesId);
            foreach (var kv in saved) if (Graph.Contains(kv.Key)) Graph.SetOverride(kv.Key, kv.Value);
            _model = null; _graphKey = null; _optKey = null; GeometryCache.Clear();
        }

        public void SetSpecies(string speciesId) { Library.GetSpecies(speciesId); SpeciesId = speciesId; Rebuild(); }
        public void SetFurniture(IFurnitureDefinition def) { Furniture = def; Parameters = def.Parameters.ToDictionary(p => p.Key, p => p.Default); Graph = null; Rebuild(); }

        public void SetParameter(string key, double value)
        {
            var def = Furniture.Parameters.FirstOrDefault(p => p.Key == key) ?? throw new ArgumentException("Unknown parameter " + key);
            if (value < def.Min || value > def.Max) throw new ArgumentOutOfRangeException(key, "Value " + value + " outside " + def.Min + ".." + def.Max);
            Parameters[key] = value;
            Graph.Set(Furniture.NodeFor(key), value);
        }

        // ---------------------------------------------------------------- manual overrides (never destroy the parametric rule)
        public void SetOverride(string nodeId, OverrideMode mode, double value)
        {
            if (!Furniture.OverridableNodes.Contains(nodeId)) throw new ArgumentException(nodeId + " cannot be overridden");
            Graph.SetOverride(nodeId, new NumericOverride { Mode = mode, Value = value });
        }
        public OverrideInfo GetOverride(string nodeId) => Graph.GetOverrideInfo(nodeId);
        public IEnumerable<KeyValuePair<string, NumericOverride>> Overrides => Graph.Overrides;

        public void ResolveOverride(string nodeId, OverrideResolution how)
        {
            switch (how)
            {
                case OverrideResolution.Recalculate: Graph.ClearOverride(nodeId); break;
                case OverrideResolution.KeepManualOverride: break;
                case OverrideResolution.ConvertToCustomComponent:
                {
                    var info = Graph.GetOverrideInfo(nodeId);
                    Graph.SetOverride(nodeId, new NumericOverride { Mode = OverrideMode.Replace, Value = info.Final });
                    if (!CustomComponents.Contains(nodeId)) CustomComponents.Add(nodeId);
                    break;
                }
            }
        }

        // ---------------------------------------------------------------- the pipeline
        public FurnitureModel BuildModel()
        {
            string key = string.Join(",", Graph.NodeIds.OrderBy(x => x, StringComparer.Ordinal).Select(id => id + "=" + Graph.VersionOf(id)));
            if (_model == null || key != _graphKey)
            {
                _model = Furniture.Assemble(Graph, Context);
                _graphKey = key;
            }
            return _model;
        }

        public static List<CutDemand> BuildDemands(FurnitureModel model)
        {
            var list = new List<CutDemand>(); int n = 0;
            foreach (var fam in model.Families)
                foreach (var part in fam.Instances)
                {
                    int pieces = fam.RoughPieces.Sum(r => r.CountPerPart);
                    foreach (var rp in fam.RoughPieces)
                        for (int i = 0; i < rp.CountPerPart; i++)
                        {
                            double a = Math.Max(rp.Rough.Width, rp.Rough.Thickness), b = Math.Min(rp.Rough.Width, rp.Rough.Thickness);
                            list.Add(new CutDemand
                            {
                                Id = "D" + (++n).ToString("000", CultureInfo.InvariantCulture), PartId = part.Id, FamilyId = fam.Id, FamilyName = fam.Name,
                                SpeciesId = fam.SpeciesId, Length = rp.Rough.Length, SecA = a, SecB = b, GrainGroup = fam.GrainGroup, Role = rp.Role,
                                FinishedLength = pieces > 1 ? fam.Finished.Length : fam.Finished.Length,
                                FinishedVolumeM3 = fam.Finished.VolumeM3 / pieces
                            });
                        }
                }
            return list;
        }

        public ProjectResult Recalculate()
        {
            var model = BuildModel();
            var demands = BuildDemands(model);
            string optKey = _graphKey + "|" + JsonSerializer.Serialize(Settings, ProjectSerializer.Options);
            bool reused = _last != null && optKey == _optKey;
            if (reused)
            {
                var r = _last;
                r.OptimizationReused = true; r.RecomputedNodes = Graph.EvaluationLog.ToList(); Graph.ClearLog();
                return r;
            }
            var optimizer = new StockOptimizer(Library, Settings);
            var opt = optimizer.Optimize(demands);
            var planner = new ManufacturingPlanner(Library, Settings.Rules);
            var mfg = planner.Plan(model, opt);
            var (bom, cost) = new CostEngine(Library, Settings).Calculate(model, opt, mfg);
            var issues = Validator.Run(new ValidationContext { Model = model, Optimization = opt, Library = Library, Settings = Settings });
            var result = new ProjectResult
            {
                Model = model, Demands = demands, Optimization = opt, Manufacturing = mfg, Bom = bom, Cost = cost, Issues = issues,
                RecomputedNodes = Graph.EvaluationLog.ToList(), Fingerprint = ComputeFingerprint(model, opt)
            };
            Graph.ClearLog();
            _last = result; _optKey = optKey;
            return result;
        }

        public static string ComputeFingerprint(FurnitureModel model, OptimizationResult opt)
        {
            var sb = new StringBuilder();
            foreach (var p in model.AllParts.OrderBy(x => x.Id, StringComparer.Ordinal)) sb.Append(GeometryEngine.PartFingerprint(p));
            foreach (var l in opt.Purchase) sb.Append(l.Item.Id).Append(l.Length).Append(l.Quantity);
            foreach (var b in opt.Boards) foreach (var c in b.Cuts) sb.Append(b.Id).Append(c.Demand.PartId).Append(c.Start.ToString("0.###", CultureInfo.InvariantCulture));
            return Hashing.Short(sb.ToString());
        }

        public List<GeometryPrimitive> GenerateGeometry(DisplayMode? mode = null)
        {
            var model = BuildModel();
            return new GeometryEngine(GeometryCache).Generate(model, mode ?? Settings.Display, Settings.ShowGrain);
        }
    }

    // ================================================================= persistence
    public sealed class OverrideDto { public string NodeId { get; set; } public OverrideMode Mode { get; set; } public double Value { get; set; } }

    public sealed class PlanSnapshot
    {
        public string Fingerprint { get; set; }
        public int BoardCount { get; set; }
        public double PurchasedM3 { get; set; }
        public double TotalCost { get; set; }
        public List<string> Purchase { get; set; } = new List<string>();
    }

    public sealed class ProjectFile
    {
        public int SchemaVersion { get; set; } = 1;
        public string PluginVersion { get; set; } = PluginInfo.Version;
        public string ProjectId { get; set; }
        public string Name { get; set; }
        public string FurnitureTypeId { get; set; }
        public string SpeciesId { get; set; }
        public Dictionary<string, double> Parameters { get; set; } = new Dictionary<string, double>();
        public List<OverrideDto> Overrides { get; set; } = new List<OverrideDto>();
        public List<string> CustomComponents { get; set; } = new List<string>();
        public ProjectSettings Settings { get; set; }
        public PlanSnapshot Snapshot { get; set; }
        public DateTime SavedUtc { get; set; }
    }

    public static class PluginInfo { public const string Version = "1.0.0"; }

    /// <summary>Vec3 is a readonly struct; serialize as [x,y,z] so library data (e.g. hardware dimensions) round-trips.</summary>
    public sealed class Vec3JsonConverter : JsonConverter<RhinoWood.Core.Geometry.Vec3>
    {
        public override RhinoWood.Core.Geometry.Vec3 Read(ref Utf8JsonReader reader, Type t, JsonSerializerOptions o)
        {
            var a = JsonSerializer.Deserialize<double[]>(ref reader, o);
            return new RhinoWood.Core.Geometry.Vec3(a[0], a[1], a[2]);
        }
        public override void Write(Utf8JsonWriter w, RhinoWood.Core.Geometry.Vec3 v, JsonSerializerOptions o)
        {
            w.WriteStartArray(); w.WriteNumberValue(v.X); w.WriteNumberValue(v.Y); w.WriteNumberValue(v.Z); w.WriteEndArray();
        }
    }

    public static class ProjectSerializer
    {
        public static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter(), new Vec3JsonConverter() },
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
        };

        public static string Serialize(WoodProject p, ProjectResult result = null)
        {
            var f = new ProjectFile
            {
                ProjectId = p.Id, Name = p.Name, FurnitureTypeId = p.Furniture.TypeId, SpeciesId = p.SpeciesId,
                Parameters = new Dictionary<string, double>(p.Parameters), Settings = p.Settings, SavedUtc = DateTime.UtcNow,
                CustomComponents = p.CustomComponents.ToList(),
                Overrides = p.Overrides.Select(kv => new OverrideDto { NodeId = kv.Key, Mode = kv.Value.Mode, Value = kv.Value.Value }).ToList()
            };
            result = result ?? p.Recalculate();
            f.Snapshot = new PlanSnapshot
            {
                Fingerprint = result.Fingerprint, BoardCount = result.Optimization.BoardCount, PurchasedM3 = result.Optimization.PurchasedM3, TotalCost = result.Cost.Total,
                Purchase = result.Optimization.Purchase.Select(x => x.Quantity + " x " + x.Item.Label + " x " + x.Length.ToString("0", CultureInfo.InvariantCulture)).ToList()
            };
            return JsonSerializer.Serialize(f, Options);
        }

        public static ProjectFile ReadFile(string json) => JsonSerializer.Deserialize<ProjectFile>(json, Options);

        public static WoodProject Deserialize(string json, WoodLibrary lib = null)
        {
            var f = ReadFile(json);
            if (f.SchemaVersion > 1) throw new NotSupportedException("Project was saved by a newer plugin version (schema " + f.SchemaVersion + ").");
            lib = lib ?? WoodLibrary.CreateDefault();
            var types = FurnitureRegistry.CreateDefault();
            var p = new WoodProject(f.Name, types.Get(f.FurnitureTypeId), f.SpeciesId, lib, JointRegistry.CreateDefault(), types) { Settings = f.Settings ?? new ProjectSettings() };
            p.ForceId(f.ProjectId);
            p.Rebuild();
            foreach (var kv in f.Parameters) p.SetParameter(kv.Key, kv.Value);
            foreach (var o in f.Overrides) p.Graph.SetOverride(o.NodeId, new NumericOverride { Mode = o.Mode, Value = o.Value });
            p.CustomComponents.AddRange(f.CustomComponents);
            return p;
        }

        /// <summary>Re-opens and verifies that the rebuilt plan matches the stored snapshot (relationships survived).</summary>
        public static (WoodProject project, bool snapshotMatches) Open(string json, WoodLibrary lib = null)
        {
            var f = ReadFile(json);
            var p = Deserialize(json, lib);
            var r = p.Recalculate();
            return (p, f.Snapshot == null || f.Snapshot.Fingerprint == r.Fingerprint);
        }
    }

    /// <summary>User library persistence (custom species, stock, tools, hardware).</summary>
    public static class LibrarySerializer
    {
        public sealed class UserLibraryDto
        {
            public List<WoodSpecies> Species { get; set; } = new List<WoodSpecies>();
            public List<StockItem> Stock { get; set; } = new List<StockItem>();
            public List<Supplier> Suppliers { get; set; } = new List<Supplier>();
            public List<ToolItem> Tools { get; set; } = new List<ToolItem>();
            public List<HardwareItem> Hardware { get; set; } = new List<HardwareItem>();
        }

        public static string SerializeUser(WoodLibrary user) => JsonSerializer.Serialize(new UserLibraryDto
        {
            Species = user.Species.Values.ToList(), Stock = user.Stock.ToList(), Suppliers = user.Suppliers.Values.ToList(),
            Tools = user.Tools.Values.ToList(), Hardware = user.Hardware.Values.ToList()
        }, ProjectSerializer.Options);

        public static WoodLibrary DeserializeUser(string json)
        {
            var dto = JsonSerializer.Deserialize<UserLibraryDto>(json, ProjectSerializer.Options);
            var l = new WoodLibrary();
            foreach (var s in dto.Species) l.Species[s.Id] = s;
            foreach (var s in dto.Suppliers) l.Suppliers[s.Id] = s;
            foreach (var t in dto.Tools) l.Tools[t.Id] = t;
            foreach (var h in dto.Hardware) l.Hardware[h.Id] = h;
            l.Stock.AddRange(dto.Stock);
            return l;
        }
    }
}
