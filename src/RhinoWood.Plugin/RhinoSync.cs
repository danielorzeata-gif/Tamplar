using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using RhinoWood.Core.Display;
using RhinoWood.Core.Domain;
using RhinoWood.Core.Projects;

namespace RhinoWood.Plugin
{
    /// <summary>
    /// Synchronises generated primitives with the Rhino document. Objects carry ONLY: project id, object key, part id, version, type.
    /// Unchanged objects (same key + version) are never touched; manually moved objects are never silently overwritten.
    /// </summary>
    public static class RhinoSync
    {
        public const string KProject = "rw.project", KKey = "rw.key", KPart = "rw.part", KVersion = "rw.version", KType = "rw.type", KOrigin = "rw.origin";
        private static readonly Dictionary<string, GeometryBase> Cache = new Dictionary<string, GeometryBase>();

        public sealed class SyncReport
        {
            public int Added, Replaced, Deleted, Unchanged, CacheHits;
            public List<ManualEdit> ManualEdits = new List<ManualEdit>();
        }
        public sealed class ManualEdit { public Guid ObjectId; public string Key; public string PartId; public GeometryPrimitive Primitive; }

        private static readonly Dictionary<PrimCategory, (string name, System.Drawing.Color color)> CategoryLayers = new Dictionary<PrimCategory, (string, System.Drawing.Color)>
        {
            { PrimCategory.Feature, ("Joinery and holes", System.Drawing.Color.FromArgb(200, 60, 60)) },
            { PrimCategory.Hardware, ("Hardware", System.Drawing.Color.FromArgb(80, 120, 190)) },
            { PrimCategory.Grain, ("Grain", System.Drawing.Color.FromArgb(120, 90, 50)) },
            { PrimCategory.Operation, ("Operations", System.Drawing.Color.FromArgb(30, 150, 80)) },
        };
        private static readonly System.Drawing.Color[] WoodTones =
        {
            System.Drawing.Color.FromArgb(192, 132, 90), System.Drawing.Color.FromArgb(176, 120, 78), System.Drawing.Color.FromArgb(205, 150, 100), System.Drawing.Color.FromArgb(160, 105, 70),
            System.Drawing.Color.FromArgb(214, 168, 120), System.Drawing.Color.FromArgb(150, 100, 64)
        };

        /// <summary>Finds or creates a layer by its full path (parents are created on the way).</summary>
        public static int EnsureLayer(RhinoDoc doc, string fullPath, System.Drawing.Color color)
        {
            int idx = doc.Layers.FindByFullPath(fullPath, -1);
            if (idx >= 0) return idx;
            var parts = fullPath.Split(new[] { "::" }, StringSplitOptions.None);
            Guid parent = Guid.Empty; string path = "";
            for (int i = 0; i < parts.Length; i++)
            {
                path = i == 0 ? parts[0] : path + "::" + parts[i];
                int found = doc.Layers.FindByFullPath(path, -1);
                if (found < 0)
                {
                    var layer = new Layer { Name = parts[i], Color = i == parts.Length - 1 ? color : System.Drawing.Color.Sienna };
                    if (parent != Guid.Empty) layer.ParentLayerId = parent;
                    found = doc.Layers.Add(layer);
                }
                parent = doc.Layers[found].Id; idx = found;
            }
            return idx;
        }

        /// <summary>Layer of a primitive: one top layer per piece (its name), a sub-layer per part family (identical parts share one), and the detail layers
        /// Joinery and holes / Operations / Grain / Hardware as sub-layers of the piece.</summary>
        private static int TargetLayer(RhinoDoc doc, WoodProject project, FurnitureModel model, GeometryPrimitive p)
        {
            string root = SafeName(project.Name);
            if (CategoryLayers.TryGetValue(p.Category, out var c)) return EnsureLayer(doc, root + "::" + c.name, c.color);
            var fam = model?.FamilyOf(p.PartId);
            string name = fam != null ? SafeName(RhinoWood.Core.Reports.Ro.Family(fam)) : "Panouri HDF";
            return EnsureLayer(doc, root + "::" + name, WoodTones[Math.Abs(name.GetHashCode()) % WoodTones.Length]);
        }

        private static string SafeName(string n) => string.IsNullOrWhiteSpace(n) ? "Piesa" : n.Replace("::", " ").Trim();

        /// <summary>Removes the old flat layers (RhinoWood::Parts ...) once nothing is on them.</summary>
        private static void RemoveLegacyLayers(RhinoDoc doc)
        {
            foreach (var name in new[] { "RhinoWood::Parts", "RhinoWood::Joinery and holes", "RhinoWood::Hardware", "RhinoWood::Grain", "RhinoWood::Operations", "RhinoWood" })
            {
                int i = doc.Layers.FindByFullPath(name, -1); if (i < 0) continue;
                var layer = doc.Layers[i];
                if (doc.Objects.FindByLayer(layer)?.Length > 0) continue;
                if (layer.GetChildren()?.Length > 0) continue;
                doc.Layers.Delete(i, true);
            }
        }

        private static GeometryBase Build(GeometryPrimitive p, double s)
        {
            string ck = p.Key + "|" + p.Version + "|" + s.ToString("R", CultureInfo.InvariantCulture);
            if (Cache.TryGetValue(ck, out var g)) return g;
            switch (p.Kind)
            {
                case PrimKind.Box when p.Category == PrimCategory.Hardware && p.Label != null && p.Label.StartsWith("Lamel"):
                    g = CutSolids.BiscuitBrep(p.Box, s);
                    break;
                case PrimKind.Box when p.Category == PrimCategory.Part && p.Cuts != null && p.Cuts.Count > 0:
                    g = CutSolids.CutPart(p, s);
                    break;
                case PrimKind.Box:
                    g = new Box(Plane.WorldXY, new Interval(p.Box.Min.X * s, p.Box.Max.X * s), new Interval(p.Box.Min.Y * s, p.Box.Max.Y * s), new Interval(p.Box.Min.Z * s, p.Box.Max.Z * s)).ToBrep();
                    break;
                case PrimKind.Cylinder:
                {
                    var a = new Point3d(p.P0.X * s, p.P0.Y * s, p.P0.Z * s); var b = new Point3d(p.P1.X * s, p.P1.Y * s, p.P1.Z * s);
                    var dir = b - a; double h = dir.Length;
                    g = h < 1e-9 ? null : new Cylinder(new Circle(new Plane(a, dir), Math.Max(p.Radius * s, 1e-6)), h).ToBrep(true, true);
                    break;
                }
                default:
                    g = new LineCurve(new Point3d(p.P0.X * s, p.P0.Y * s, p.P0.Z * s), new Point3d(p.P1.X * s, p.P1.Y * s, p.P1.Z * s));
                    break;
            }
            if (Cache.Count > 5000) Cache.Clear();
            if (g != null) Cache[ck] = g;
            return g;
        }

        private static string BoxTag(BoundingBox bb) => string.Format(CultureInfo.InvariantCulture, "{0:0.###},{1:0.###},{2:0.###}", bb.Min.X, bb.Min.Y, bb.Min.Z);

        public static SyncReport Sync(RhinoDoc doc, WoodProject project, IList<GeometryPrimitive> prims, ICollection<string> customKeys = null)
        {
            var rep = new SyncReport();
            double s = RhinoMath.UnitScale(UnitSystem.Millimeters, doc.ModelUnitSystem);
            var existing = new Dictionary<string, RhinoObject>();
            foreach (var o in doc.Objects.GetObjectList(new ObjectEnumeratorSettings { NormalObjects = true, LockedObjects = true, HiddenObjects = true, ObjectTypeFilter = ObjectType.AnyObject }))
                if (o.Attributes.GetUserString(KProject) == project.Id) existing[o.Attributes.GetUserString(KKey)] = o;

            var model = project.Recalculate().Model;
            var wanted = new HashSet<string>();
            foreach (var p in prims)
            {
                wanted.Add(p.Key);
                if (customKeys != null && customKeys.Contains(p.PartId)) continue;   // converted to custom component: no longer regenerated
                if (existing.TryGetValue(p.Key, out var obj))
                {
                    int wantLayer = TargetLayer(doc, project, model, p);
                    if (obj.Attributes.GetUserString(KVersion) == p.Version)
                    {
                        if (obj.Attributes.LayerIndex != wantLayer) { var la = obj.Attributes.Duplicate(); la.LayerIndex = wantLayer; doc.Objects.ModifyAttributes(obj, la, true); }
                        rep.Unchanged++; continue;
                    }
                    // geometry differs: was it edited by hand since we created it? then NEVER overwrite silently
                    var origin = obj.Attributes.GetUserString(KOrigin);
                    var bb = obj.Geometry.GetBoundingBox(true);
                    if (origin != null && origin != BoxTag(bb) && p.Kind != PrimKind.Line)
                    {
                        rep.ManualEdits.Add(new ManualEdit { ObjectId = obj.Id, Key = p.Key, PartId = p.PartId, Primitive = p });
                        continue;
                    }
                    var attrs = obj.Attributes.Duplicate();
                    attrs.LayerIndex = wantLayer;
                    attrs.SetUserString(KVersion, p.Version);
                    var g = Build(p, s); rep.CacheHits += 0;
                    if (g == null) continue;
                    ReplaceGeometry(doc, obj, g, attrs);
                    rep.Replaced++;
                }
                else
                {
                    var g = Build(p, s); if (g == null) continue;
                    var attrs = new ObjectAttributes { LayerIndex = TargetLayer(doc, project, model, p), Name = p.Key };
                    attrs.SetUserString(KProject, project.Id); attrs.SetUserString(KKey, p.Key); attrs.SetUserString(KPart, p.PartId ?? "");
                    attrs.SetUserString(KVersion, p.Version); attrs.SetUserString(KType, p.Category.ToString());
                    attrs.SetUserString(KOrigin, BoxTag(g.GetBoundingBox(true)));
                    AddGeometry(doc, g, attrs);
                    rep.Added++;
                }
            }
            foreach (var kv in existing.Where(k => !wanted.Contains(k.Key)))
            {
                if (kv.Value.Attributes.GetUserString(KType) == "Custom") continue;
                doc.Objects.Delete(kv.Value, true); rep.Deleted++;
            }
            RemoveLegacyLayers(doc);
            doc.Views.Redraw();
            return rep;
        }

        /// <summary>Deletes every Rhino Wood object whose project is not in <paramref name="keep"/> (leftovers of earlier projects: the "ghosts"). Returns how many were removed.</summary>
        public static int RemoveOtherProjects(RhinoDoc doc, ICollection<string> keep)
        {
            var stale = doc.Objects.GetObjectList(new ObjectEnumeratorSettings { NormalObjects = true, LockedObjects = true, HiddenObjects = true, ObjectTypeFilter = ObjectType.AnyObject })
                .Where(o => { var id = o.Attributes.GetUserString(KProject); return id != null && !keep.Contains(id); }).ToList();
            if (stale.Count == 0) return 0;
            uint undo = doc.BeginUndoRecord("Rhino Wood: șterge obiectele proiectelor vechi");     // Ctrl+Z restores them
            foreach (var o in stale) doc.Objects.Delete(o, true);
            doc.EndUndoRecord(undo);
            doc.Views.Redraw();
            return stale.Count;
        }

        private static Guid AddGeometry(RhinoDoc doc, GeometryBase g, ObjectAttributes a)
        {
            if (g is Brep b) return doc.Objects.AddBrep(b, a);
            if (g is Curve c) return doc.Objects.AddCurve(c, a);
            return Guid.Empty;
        }

        private static void ReplaceGeometry(RhinoDoc doc, RhinoObject old, GeometryBase g, ObjectAttributes attrs)
        {
            attrs.SetUserString(KOrigin, BoxTag(g.GetBoundingBox(true)));
            var id = AddGeometry(doc, g, attrs);
            doc.Objects.Delete(old, true);
        }

        public static void ApplyResolution(RhinoDoc doc, WoodProject project, ManualEdit edit, OverrideResolution how, double scale)
        {
            var obj = doc.Objects.FindId(edit.ObjectId);
            if (obj == null) return;
            switch (how)
            {
                case OverrideResolution.Recalculate:
                    var attrs = obj.Attributes.Duplicate(); attrs.SetUserString(KVersion, edit.Primitive.Version);
                    ReplaceGeometry(doc, obj, Build(edit.Primitive, scale), attrs);
                    break;
                case OverrideResolution.KeepManualOverride:
                    var keep = obj.Attributes.Duplicate(); keep.SetUserString(KOrigin, BoxTag(obj.Geometry.GetBoundingBox(true))); keep.SetUserString(KVersion, edit.Primitive.Version);
                    doc.Objects.ModifyAttributes(obj, keep, true);
                    break;
                case OverrideResolution.ConvertToCustomComponent:
                    var cust = obj.Attributes.Duplicate(); cust.SetUserString(KType, "Custom");
                    doc.Objects.ModifyAttributes(obj, cust, true);
                    if (!project.CustomComponents.Contains(edit.PartId)) project.CustomComponents.Add(edit.PartId);
                    break;
            }
        }
    }
}
