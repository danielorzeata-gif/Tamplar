using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using RhinoWood.Core.Domain;
using RhinoWood.Core.Furniture;
using RhinoWood.Core.Libraries;
using RhinoWood.Core.Optimization;
using RhinoWood.Core.Projects;
using RhinoWood.Core.Rules;

namespace RhinoWood.Core.Workspaces
{
    /// <summary>
    /// A set of decisions shared by every piece of a room. Two kinds exist per room:
    /// ASPECT (visible: species, front style, handle, edge profile...) and STRUCTURE (how it is built: joints, fixings...).
    /// </summary>
    public sealed class StyleSet
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public StyleKind Kind { get; set; }
        public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>();
    }

    public enum ValueSource { Standard, Set, PieceOverride }
    public enum ChangeScope { OnlyHere, WholeSet }

    public sealed class FieldInfo
    {
        public string Key { get; set; }
        public string Label { get; set; }
        public StyleKind Kind { get; set; }
        public string StyleKey { get; set; }
        public string Value { get; set; }
        public string SetValue { get; set; }
        public ValueSource Source { get; set; }
    }

    public sealed class StyleImpact
    {
        public List<string> PieceIds { get; } = new List<string>();
        public List<string> Conflicts { get; } = new List<string>();
        public int PiecesAffected => PieceIds.Count;
        public override string ToString() => PiecesAffected + " piece(s) updated" + (Conflicts.Count > 0 ? ", " + Conflicts.Count + " conflict(s): " + string.Join("; ", Conflicts) : "");
    }

    public sealed class PieceEntry
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public WoodProject Project { get; set; }
        /// <summary>Field keys where this piece deliberately differs from its room set (provenance: PieceOverride).</summary>
        public HashSet<string> Unlinked { get; set; } = new HashSet<string>();
    }

    public sealed class Room
    {
        private int _next = 1;
        public string Id { get; set; }
        public string Name { get; set; }
        public StyleSet Aspect { get; set; }
        public StyleSet Structure { get; set; }
        public List<PieceEntry> Pieces { get; set; } = new List<PieceEntry>();
        /// <summary>Execution variant of the room's Structure set (ECONOMA / STANDARD / PREMIUM) or null.</summary>
        public string Tier { get; set; }

        public Room(string name)
        {
            Name = name; Id = "R-" + name.ToUpperInvariant().Replace(' ', '_');
            Aspect = new StyleSet { Id = Id + ".ASPECT", Name = name + " - aspect", Kind = StyleKind.Aspect };
            Structure = new StyleSet { Id = Id + ".STRUCT", Name = name + " - structure", Kind = StyleKind.Structure };
        }

        public StyleSet SetFor(StyleKind k) => k == StyleKind.Aspect ? Aspect : k == StyleKind.Structure ? Structure : null;

        private const string SpeciesField = "species";

        // ------------------------------------------------------------------ field model
        public IEnumerable<FieldInfo> Fields(PieceEntry piece)
        {
            var p = piece.Project;
            yield return Describe(piece, SpeciesField, "Species", StyleKind.Aspect, "species", p.SpeciesId);
            foreach (var c in p.Furniture.Choices.Where(c => c.StyleKind != StyleKind.None))
                yield return Describe(piece, c.Key, c.Label, c.StyleKind, c.StyleKey, p.Choices[c.Key]);
            foreach (var d in p.Furniture.Parameters.Where(d => d.StyleKind != StyleKind.None))
                yield return Describe(piece, d.Key, d.Label, d.StyleKind, d.StyleKey, p.Parameters[d.Key].ToString("0.###", CultureInfo.InvariantCulture));
        }

        private FieldInfo Describe(PieceEntry piece, string key, string label, StyleKind kind, string styleKey, string value)
        {
            var set = SetFor(kind);
            set.Values.TryGetValue(styleKey, out var sv);
            var src = sv == null ? ValueSource.Standard : piece.Unlinked.Contains(key) ? ValueSource.PieceOverride : ValueSource.Set;
            return new FieldInfo { Key = key, Label = label, Kind = kind, StyleKey = styleKey, Value = value, SetValue = sv, Source = src };
        }

        // ------------------------------------------------------------------ adding pieces
        /// <summary>Adds a piece. Set values the piece does not have yet are APPLIED to it; set keys that are still empty are INITIALISED from the piece.</summary>
        public PieceEntry AddPiece(string name, WoodProject project)
        {
            var e = new PieceEntry { Id = "P" + (_next++), Name = name, Project = project };
            Pieces.Add(e);
            foreach (var f in Fields(e))
            {
                var set = SetFor(f.Kind);
                if (f.SetValue == null) set.Values[f.StyleKey] = f.Value;
                else if (f.SetValue != f.Value) TryApply(e, f.Key, f.SetValue, new StyleImpact());
            }
            return e;
        }

        /// <summary>
        /// Sets the room's variant: writes the variant's joint/fixing choices into the Structure set and pushes them to every piece still linked.
        /// Pieces whose furniture type does not define the variant, or that reject a value, are reported as conflicts.
        /// </summary>
        public StyleImpact ApplyTier(string tierId)
        {
            var total = new StyleImpact();
            foreach (var piece in Pieces)
            {
                var def = piece.Project.Furniture;
                var tier = def.Tiers.FirstOrDefault(t => t.Id == tierId);
                if (tier == null) { total.Conflicts.Add(piece.Name + ": nu are varianta " + tierId); continue; }
                foreach (var kv in tier.Choices)
                {
                    var cd = def.Choices.First(c => c.Key == kv.Key);
                    if (cd.StyleKind != StyleKind.Structure) continue;
                    var imp = SetStyle(StyleKind.Structure, cd.StyleKey, kv.Value);
                    foreach (var id in imp.PieceIds) if (!total.PieceIds.Contains(id)) total.PieceIds.Add(id);
                    total.Conflicts.AddRange(imp.Conflicts.Where(c => !total.Conflicts.Contains(c)));
                }
            }
            Tier = tierId;
            foreach (var piece in Pieces) if (piece.Project.Furniture.Tiers.Any(t => t.Id == tierId)) piece.Project.MarkTier(tierId);
            return total;
        }

        public void RemovePiece(PieceEntry e) => Pieces.Remove(e);

        /// <summary>Makes new piece ids unique after loading pieces from a file.</summary>
        public void SyncCounter() => _next = Pieces.Select(p => int.TryParse(p.Id.TrimStart('P'), out var n) ? n : 0).DefaultIfEmpty(0).Max() + 1;

        // ------------------------------------------------------------------ changing values
        /// <summary>Changes a value in the set and pushes it to every piece still linked for fields bound to that key.</summary>
        public StyleImpact SetStyle(StyleKind kind, string styleKey, string value)
        {
            var impact = new StyleImpact();
            SetFor(kind).Values[styleKey] = value;
            foreach (var piece in Pieces)
                foreach (var f in Fields(piece).Where(f => f.Kind == kind && f.StyleKey == styleKey && !piece.Unlinked.Contains(f.Key)))
                    if (f.Value != value && TryApply(piece, f.Key, value, impact)) { if (!impact.PieceIds.Contains(piece.Id)) impact.PieceIds.Add(piece.Id); }
            return impact;
        }

        /// <summary>
        /// Edits one field of one piece. OnlyHere (default) detaches just that field from the set; WholeSet updates the set and every linked piece
        /// (and re-links this field).
        /// </summary>
        public StyleImpact ChangeField(PieceEntry piece, string fieldKey, string value, ChangeScope scope = ChangeScope.OnlyHere)
        {
            var f = Fields(piece).FirstOrDefault(x => x.Key == fieldKey) ?? throw new ArgumentException("Field " + fieldKey + " is not style-controlled for this piece");
            var impact = new StyleImpact();
            if (scope == ChangeScope.OnlyHere)
            {
                if (TryApply(piece, fieldKey, value, impact)) { piece.Unlinked.Add(fieldKey); impact.PieceIds.Add(piece.Id); }
                return impact;
            }
            if (!TryApply(piece, fieldKey, value, impact)) return impact;       // the piece itself must accept it, otherwise the set is untouched
            piece.Unlinked.Remove(fieldKey);
            var all = SetStyle(f.Kind, f.StyleKey, value);
            impact.PieceIds.AddRange(new[] { piece.Id }.Concat(all.PieceIds).Distinct());
            impact.Conflicts.AddRange(all.Conflicts);
            impact.PieceIds.Sort(StringComparer.Ordinal);
            return impact;
        }

        public bool Relink(PieceEntry piece, string fieldKey)
        {
            var f = Fields(piece).FirstOrDefault(x => x.Key == fieldKey);
            if (f == null) return false;
            piece.Unlinked.Remove(fieldKey);
            return f.SetValue == null || f.SetValue == f.Value || TryApply(piece, fieldKey, f.SetValue, new StyleImpact());
        }

        private bool TryApply(PieceEntry piece, string fieldKey, string value, StyleImpact impact)
        {
            var p = piece.Project;
            try
            {
                if (fieldKey == SpeciesField) p.SetSpecies(value);
                else if (p.Furniture.Choices.Any(c => c.Key == fieldKey)) p.SetChoice(fieldKey, value);
                else p.SetParameter(fieldKey, double.Parse(value, CultureInfo.InvariantCulture));
                return true;
            }
            catch (Exception ex) when (ex is ArgumentException || ex is KeyNotFoundException || ex is FormatException)
            {
                impact.Conflicts.Add(piece.Name + "." + fieldKey + ": " + value + " not allowed (" + ex.Message + ")");
                return false;
            }
        }
    }

    public sealed class PieceResultSummary
    {
        public string PieceId { get; set; }
        public string Name { get; set; }
        public double SeparateCost { get; set; }
        public int SeparateBoards { get; set; }
        public int Errors { get; set; }
        public int Warnings { get; set; }
    }

    public sealed class RoomResult
    {
        public Room Room { get; set; }
        public List<PieceResultSummary> Pieces { get; set; } = new List<PieceResultSummary>();
        /// <summary>ONE optimization for every piece of the room (all pieces compete for the same boards).</summary>
        public OptimizationResult Combined { get; set; }
        public List<ProjectResult> Results { get; set; } = new List<ProjectResult>();
        /// <summary>Species declaration by visibility class, ready for the offer (MIX-011).</summary>
        public string Declaration { get; set; }
        public double SeparateCostTotal => Pieces.Sum(p => p.SeparateCost);
        public double CombinedCost => Combined?.TotalCost ?? 0;
        public double Saving => Math.Max(0, SeparateCostTotal - CombinedCost);
        public int SeparateBoardsTotal => Pieces.Sum(p => p.SeparateBoards);
        public int CombinedBoards => Combined?.BoardCount ?? 0;
        public List<Issue> Issues { get; set; } = new List<Issue>();
    }

    public sealed class Workspace
    {
        public string Name { get; set; }
        public WoodLibrary Library { get; }
        public ProjectSettings Settings { get; set; } = new ProjectSettings();
        public List<Room> Rooms { get; } = new List<Room>();

        public Workspace(string name, WoodLibrary library = null) { Name = name; Library = library ?? WoodLibrary.CreateDefault(); }

        public Room AddRoom(string name) { var r = new Room(name); Rooms.Add(r); return r; }

        public RoomResult Recalculate(Room room)
        {
            var res = new RoomResult { Room = room };
            var demands = new List<CutDemand>();
            var perPiece = new List<IList<CutDemand>>();
            foreach (var piece in room.Pieces)
            {
                var own = piece.Project.Recalculate();          // separate plan (for comparison + per-piece validation)
                res.Results.Add(own);
                res.Pieces.Add(new PieceResultSummary
                {
                    PieceId = piece.Id, Name = piece.Name, SeparateCost = own.Optimization.TotalCost, SeparateBoards = own.Optimization.BoardCount,
                    Errors = own.Issues.Count(i => i.Severity == Severity.Error), Warnings = own.Issues.Count(i => i.Severity == Severity.Warning)
                });
                foreach (var i in own.Issues.Where(i => i.Severity != Severity.Info))
                    res.Issues.Add(new Issue { Severity = i.Severity, Code = i.Code, SubjectId = piece.Id + "/" + i.SubjectId, Message = piece.Name + ": " + i.Message });
                var mine = new List<CutDemand>();
                foreach (var d in WoodProject.BuildDemands(own.Model))
                {
                    d.Id = piece.Id + "." + d.Id;
                    d.PartId = piece.Id + "/" + d.PartId;
                    demands.Add(d); mine.Add(d);
                }
                perPiece.Add(mine);
            }
            res.Combined = new StockOptimizer(Library, Settings).Optimize(demands, perPiece);
            res.Declaration = SpeciesDeclaration.Text(res.Results, room.Tier);
            return res;
        }
    }

    // ===================================================================== persistence
    public sealed class WorkspaceFile
    {
        public int SchemaVersion { get; set; } = 1;
        public string Name { get; set; }
        public ProjectSettings Settings { get; set; }
        public List<RoomDto> Rooms { get; set; } = new List<RoomDto>();
    }
    public sealed class RoomDto
    {
        public string Name { get; set; }
        public StyleSet Aspect { get; set; }
        public StyleSet Structure { get; set; }
        public string Tier { get; set; }
        public List<PieceDto> Pieces { get; set; } = new List<PieceDto>();
    }
    public sealed class PieceDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<string> Unlinked { get; set; } = new List<string>();
        /// <summary>The piece is stored as its own project file (source of truth: parameters, choices, overrides).</summary>
        public string ProjectJson { get; set; }
    }

    public static class WorkspaceSerializer
    {
        public static string Serialize(Workspace w)
        {
            var f = new WorkspaceFile { Name = w.Name, Settings = w.Settings };
            foreach (var r in w.Rooms)
            {
                var rd = new RoomDto { Name = r.Name, Aspect = r.Aspect, Structure = r.Structure, Tier = r.Tier };
                foreach (var p in r.Pieces)
                    rd.Pieces.Add(new PieceDto { Id = p.Id, Name = p.Name, Unlinked = p.Unlinked.OrderBy(x => x, StringComparer.Ordinal).ToList(), ProjectJson = ProjectSerializer.Serialize(p.Project) });
                f.Rooms.Add(rd);
            }
            return JsonSerializer.Serialize(f, ProjectSerializer.Options);
        }

        public static Workspace Deserialize(string json, WoodLibrary lib = null)
        {
            var f = JsonSerializer.Deserialize<WorkspaceFile>(json, ProjectSerializer.Options);
            if (f.SchemaVersion > 1) throw new NotSupportedException("Workspace saved by a newer plugin version.");
            var w = new Workspace(f.Name, lib) { Settings = f.Settings ?? new ProjectSettings() };
            foreach (var rd in f.Rooms)
            {
                var room = w.AddRoom(rd.Name);
                room.Aspect = rd.Aspect; room.Structure = rd.Structure; room.Tier = rd.Tier;
                foreach (var pd in rd.Pieces)
                {
                    var proj = ProjectSerializer.Deserialize(pd.ProjectJson, w.Library);
                    room.Pieces.Add(new PieceEntry { Id = pd.Id, Name = pd.Name, Project = proj, Unlinked = new HashSet<string>(pd.Unlinked) });
                }
                room.SyncCounter();
            }
            return w;
        }
    }
}
