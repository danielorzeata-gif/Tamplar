using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RhinoWood.Core.Domain;
using RhinoWood.Core.Geometry;
using RhinoWood.Core.HardwareSystem;
using RhinoWood.Core.Joinery;
using RhinoWood.Core.Libraries;
using RhinoWood.Core.Parametric;
using RhinoWood.Core.Rules;

namespace RhinoWood.Core.Furniture
{
    public sealed class ParameterDef
    {
        public string Key { get; set; }
        public string Label { get; set; }
        public string Unit { get; set; } = "mm";
        public double Default { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public string Group { get; set; }
        /// <summary>Progressive disclosure: advanced parameters are hidden until requested.</summary>
        public bool Advanced { get; set; }
        public string Description { get; set; }
        /// <summary>True when the graph node is derived by a rule (a value of 0 means "auto").</summary>
        public bool AutoWhenZero { get; set; }
    }

    public sealed class ProjectContext
    {
        public WoodLibrary Library { get; set; }
        public JointRegistry Joints { get; set; }
        public ManufacturingRules Rules { get; set; }
    }

    public interface IFurnitureDefinition
    {
        string TypeId { get; }
        string Name { get; }
        string Category { get; }
        IReadOnlyList<ParameterDef> Parameters { get; }
        /// <summary>Registers all inputs and computed nodes (dependency graph). No geometry is built here eagerly.</summary>
        DependencyGraph CreateGraph(ProjectContext ctx, IDictionary<string, double> values, string speciesId);
        /// <summary>Pulls the graph and produces the full furniture model (parts, joinery, hardware, validation).</summary>
        FurnitureModel Assemble(DependencyGraph graph, ProjectContext ctx);
        /// <summary>Node ids that correspond to numeric parameters that can be manually overridden.</summary>
        IReadOnlyList<string> OverridableNodes { get; }
        /// <summary>Maps a user parameter key to its graph input node id.</summary>
        string NodeFor(string parameterKey);
    }

    public sealed class FurnitureRegistry
    {
        private readonly Dictionary<string, IFurnitureDefinition> _defs = new Dictionary<string, IFurnitureDefinition>();
        public IEnumerable<IFurnitureDefinition> All => _defs.Values;
        public void Register(IFurnitureDefinition d) => _defs[d.TypeId] = d;
        public IFurnitureDefinition Get(string id) => _defs.TryGetValue(id, out var d) ? d : throw new KeyNotFoundException("Unknown furniture type " + id);
        public static FurnitureRegistry CreateDefault()
        {
            var r = new FurnitureRegistry();
            r.Register(new TableDefinition());
            return r;
        }
    }

    internal sealed class Boxed<T> : IFingerprint
    {
        public T Value { get; }
        public string Fingerprint { get; }
        public Boxed(T value, string fp) { Value = value; Fingerprint = fp; }
    }

    internal static class Sig
    {
        public static string Of(IEnumerable<PartFamily> fams) =>
            string.Join("|", fams.Select(f => f.Id + ":" + f.SpeciesId + ":" + f.Finished + ":" + string.Join(";", f.RoughPieces.Select(r => r.CountPerPart + "x" + r.Rough)) + ":" +
                string.Join(";", f.Instances.Select(i => i.Id + "@" + i.Bounds))));
    }

    /// <summary>Solid-wood dining table: top (edge-glued strips), 4 legs, 4 aprons, mortise &amp; tenon, tabletop fasteners.</summary>
    public sealed class TableDefinition : IFurnitureDefinition
    {
        public string TypeId => "table.dining";
        public string Name => "Solid-wood dining table";
        public string Category => "Tables";

        public IReadOnlyList<ParameterDef> Parameters { get; } = new[]
        {
            new ParameterDef { Key = "length", Label = "Length", Default = 1800, Min = 600, Max = 3600, Group = "Main" },
            new ParameterDef { Key = "width", Label = "Width", Default = 900, Min = 400, Max = 1400, Group = "Main" },
            new ParameterDef { Key = "height", Label = "Height", Default = 760, Min = 400, Max = 1100, Group = "Main" },
            new ParameterDef { Key = "topThickness", Label = "Top thickness", Default = 35, Min = 18, Max = 80, Group = "Main" },
            new ParameterDef { Key = "legSectionUser", Label = "Leg section (0 = rule)", Default = 0, Min = 0, Max = 160, Group = "Legs", AutoWhenZero = true, Description = "0 lets the rule engine choose the section from the table length." },
            new ParameterDef { Key = "apronHeight", Label = "Apron height", Default = 80, Min = 40, Max = 160, Group = "Aprons", Advanced = true },
            new ParameterDef { Key = "apronThickness", Label = "Apron thickness", Default = 25, Min = 18, Max = 50, Group = "Aprons", Advanced = true },
            new ParameterDef { Key = "overhang", Label = "Top overhang", Default = 40, Min = 0, Max = 200, Group = "Top", Advanced = true },
            new ParameterDef { Key = "reveal", Label = "Apron setback from leg face", Default = 5, Min = 0, Max = 20, Group = "Aprons", Advanced = true },
            new ParameterDef { Key = "clipSpacing", Label = "Top fastener spacing", Default = 350, Min = 150, Max = 600, Group = "Hardware", Advanced = true },
        };

        public string NodeFor(string key) => key == "apronHeight" ? "apron.height" : key == "apronThickness" ? "apron.thickness" : key;

        public IReadOnlyList<string> OverridableNodes { get; } = new[] { "leg.section", "apron.height", "overhang", "leg.height" };

        public DependencyGraph CreateGraph(ProjectContext ctx, IDictionary<string, double> v, string speciesId)
        {
            var g = new DependencyGraph();
            double Val(string k) => v.TryGetValue(k, out var d) ? d : Parameters.First(p => p.Key == k).Default;

            g.AddInput("species", speciesId);
            g.AddInput("length", Val("length")); g.AddInput("width", Val("width")); g.AddInput("height", Val("height"));
            g.AddInput("topThickness", Val("topThickness")); g.AddInput("legSectionUser", Val("legSectionUser"));
            g.AddInput("apron.height", Val("apronHeight")); g.AddInput("apron.thickness", Val("apronThickness"));
            g.AddInput("overhang", Val("overhang")); g.AddInput("reveal", Val("reveal")); g.AddInput("clipSpacing", Val("clipSpacing"));

            var stock = ctx.Library.PanelStripStock(speciesId, ctx.Rules.Rough(new Dims(1, 1, Val("topThickness"))).Thickness);
            g.AddInput("stock.top.width", stock == null ? 0.0 : Math.Max(stock.Width, stock.Thickness));
            var rules = ctx.Rules;

            g.AddComputed("leg.section", new[] { "legSectionUser", "length" }, r =>
            {
                double user = r.Get<double>("legSectionUser");
                if (user > 0) return user;
                return Math.Min(120.0, Math.Max(60.0, Math.Round(r.Get<double>("length") / 22.5 / 5) * 5));
            });
            g.AddComputed("leg.height", new[] { "height", "topThickness" }, r => r.Get<double>("height") - r.Get<double>("topThickness"));
            g.AddComputed("span.x", new[] { "length", "overhang" }, r => r.Get<double>("length") - 2 * r.Get<double>("overhang"));
            g.AddComputed("span.y", new[] { "width", "overhang" }, r => r.Get<double>("width") - 2 * r.Get<double>("overhang"));
            g.AddComputed("mt", new[] { "apron.thickness", "apron.height", "leg.section" }, r =>
                new Boxed<MortiseTenonParams>(MortiseTenonJoint.Compute(r.Get<double>("apron.thickness"), r.Get<double>("apron.height"), r.Get<double>("leg.section")),
                    r.Get<double>("apron.thickness") + "/" + r.Get<double>("apron.height") + "/" + r.Get<double>("leg.section")));

            g.AddComputed("comp.legs", new[] { "species", "leg.section", "leg.height", "overhang", "span.x", "span.y" }, r =>
            {
                double s = r.Get<double>("leg.section"), h = r.Get<double>("leg.height"), o = r.Get<double>("overhang");
                double sx = r.Get<double>("span.x"), sy = r.Get<double>("span.y");
                var fin = new Dims(h, s, s);
                var fam = new PartFamily
                {
                    Id = "F-LEG", Name = "Table leg", Type = PartType.Leg, Assembly = "Frame", SpeciesId = r.Get<string>("species"),
                    Finished = fin, GrainAxis = Axis.Z, GrainAlongLength = true, RequiresGrainContinuity = true,
                    RoughPieces = { new RoughPieceSpec { CountPerPart = 1, Rough = rules.Rough(fin), Role = "Leg blank" } }
                };
                var pos = new[] { (o, o), (o + sx - s, o), (o, o + sy - s), (o + sx - s, o + sy - s) };
                for (int i = 0; i < 4; i++)
                    fam.Instances.Add(new PartInstance
                    {
                        Id = "LEG-" + (i + 1), FamilyId = fam.Id, Index = i,
                        Bounds = Box3.FromMinSize(new Vec3(pos[i].Item1, pos[i].Item2, 0), new Vec3(s, s, h)),
                        LengthAxis = Axis.Z, WidthAxis = Axis.X, ThicknessAxis = Axis.Y
                    });
                return new Boxed<List<PartFamily>>(new List<PartFamily> { fam }, Sig.Of(new[] { fam }));
            });

            g.AddComputed("comp.aprons", new[] { "species", "leg.section", "leg.height", "overhang", "span.x", "span.y", "mt", "apron.height", "apron.thickness", "reveal" }, r =>
            {
                double s = r.Get<double>("leg.section"), h = r.Get<double>("leg.height"), o = r.Get<double>("overhang");
                double sx = r.Get<double>("span.x"), sy = r.Get<double>("span.y");
                double ah = r.Get<double>("apron.height"), at = r.Get<double>("apron.thickness"), rev = r.Get<double>("reveal");
                var mt = r.Get<Boxed<MortiseTenonParams>>("mt").Value;
                var sp = r.Get<string>("species");
                double lenL = sx - 2 * s + 2 * mt.TenonLength, lenS = sy - 2 * s + 2 * mt.TenonLength;
                var finL = new Dims(lenL, ah, at); var finS = new Dims(lenS, ah, at);
                var famL = new PartFamily { Id = "F-APRON-L", Name = "Long apron", Type = PartType.Apron, Assembly = "Frame", SpeciesId = sp, Finished = finL, GrainAxis = Axis.X, RequiresGrainContinuity = true, RoughPieces = { new RoughPieceSpec { Rough = rules.Rough(finL), Role = "Apron blank" } } };
                var famS = new PartFamily { Id = "F-APRON-S", Name = "Short apron", Type = PartType.Apron, Assembly = "Frame", SpeciesId = sp, Finished = finS, GrainAxis = Axis.Y, RequiresGrainContinuity = true, RoughPieces = { new RoughPieceSpec { Rough = rules.Rough(finS), Role = "Apron blank" } } };
                double top = h, zMin = h - ah;
                double xs = o + s - mt.TenonLength, ys = o + s - mt.TenonLength;
                famL.Instances.Add(new PartInstance { Id = "APR-L-1", FamilyId = famL.Id, Index = 0, Bounds = new Box3(new Vec3(xs, o + rev, zMin), new Vec3(xs + lenL, o + rev + at, top)), LengthAxis = Axis.X, WidthAxis = Axis.Z, ThicknessAxis = Axis.Y });
                famL.Instances.Add(new PartInstance { Id = "APR-L-2", FamilyId = famL.Id, Index = 1, Bounds = new Box3(new Vec3(xs, o + sy - rev - at, zMin), new Vec3(xs + lenL, o + sy - rev, top)), LengthAxis = Axis.X, WidthAxis = Axis.Z, ThicknessAxis = Axis.Y });
                famS.Instances.Add(new PartInstance { Id = "APR-S-1", FamilyId = famS.Id, Index = 0, Bounds = new Box3(new Vec3(o + rev, ys, zMin), new Vec3(o + rev + at, ys + lenS, top)), LengthAxis = Axis.Y, WidthAxis = Axis.Z, ThicknessAxis = Axis.X });
                famS.Instances.Add(new PartInstance { Id = "APR-S-2", FamilyId = famS.Id, Index = 1, Bounds = new Box3(new Vec3(o + sx - rev - at, ys, zMin), new Vec3(o + sx - rev, ys + lenS, top)), LengthAxis = Axis.Y, WidthAxis = Axis.Z, ThicknessAxis = Axis.X });
                var list = new List<PartFamily> { famL, famS };
                return new Boxed<List<PartFamily>>(list, Sig.Of(list));
            });

            g.AddComputed("comp.top", new[] { "species", "length", "width", "height", "topThickness", "stock.top.width" }, r =>
            {
                double L = r.Get<double>("length"), W = r.Get<double>("width"), H = r.Get<double>("height"), T = r.Get<double>("topThickness");
                double stockW = r.Get<double>("stock.top.width");
                var fin = new Dims(L, W, T);
                var roughFull = rules.Rough(fin);
                double maxFinishedStrip = (stockW > 0 ? stockW : 140) - rules.PanelStripWidthAllowance;
                int n = Math.Max(1, (int)Math.Ceiling((W + rules.GluedPanelTrim) / maxFinishedStrip));
                double stripFinished = (W + rules.GluedPanelTrim) / n;
                var strip = new Dims(roughFull.Length, stripFinished + rules.PanelStripWidthAllowance, roughFull.Thickness);
                var fam = new PartFamily
                {
                    Id = "F-TOP", Name = "Table top (edge-glued)", Type = PartType.Top, Assembly = "Top", SpeciesId = r.Get<string>("species"),
                    Finished = fin, GrainAxis = Axis.X, RequiresGrainContinuity = false, VisualGrainRequired = true, GrainGroup = "TOP",
                    Notes = "Edge-glued from " + n + " strips; alternate growth-ring orientation to limit cupping.",
                    RoughPieces = { new RoughPieceSpec { CountPerPart = n, Rough = strip, Role = "Top strip" } }
                };
                fam.Instances.Add(new PartInstance { Id = "TOP-1", FamilyId = fam.Id, Index = 0, Bounds = new Box3(new Vec3(0, 0, H - T), new Vec3(L, W, H)), LengthAxis = Axis.X, WidthAxis = Axis.Y, ThicknessAxis = Axis.Z });
                return new Boxed<List<PartFamily>>(new List<PartFamily> { fam }, Sig.Of(new[] { fam }));
            });

            g.AddComputed("joints.requests", new[] { "comp.legs", "comp.aprons" }, r =>
            {
                var reqs = new List<JointEngine.Request>();
                void M(string a, bool start, string b) => reqs.Add(new JointEngine.Request { JointTypeId = "mortise-tenon", PartAId = a, PartBId = b, AAtStart = start });
                M("APR-L-1", true, "LEG-1"); M("APR-L-1", false, "LEG-2");
                M("APR-L-2", true, "LEG-3"); M("APR-L-2", false, "LEG-4");
                M("APR-S-1", true, "LEG-1"); M("APR-S-1", false, "LEG-3");
                M("APR-S-2", true, "LEG-2"); M("APR-S-2", false, "LEG-4");
                return new Boxed<List<JointEngine.Request>>(reqs, r.Get<Boxed<List<PartFamily>>>("comp.legs").Fingerprint + r.Get<Boxed<List<PartFamily>>>("comp.aprons").Fingerprint);
            });

            g.AddComputed("hardware.installs", new[] { "comp.aprons", "comp.top", "clipSpacing", "leg.section", "span.x", "mt" }, r =>
            {
                var aprons = r.Get<Boxed<List<PartFamily>>>("comp.aprons").Value;
                var list = new List<HardwareInstall>();
                double spacing = r.Get<double>("clipSpacing");
                var topPart = r.Get<Boxed<List<PartFamily>>>("comp.top").Value[0].Instances[0];
                double zTop = topPart.Bounds.Min.Z;
                foreach (var ap in aprons[0].Instances)   // long aprons: Z-clips (allow top to move across grain)
                {
                    double x0 = ap.Bounds.Min.X + r.Get<Boxed<MortiseTenonParams>>("mt").Value.TenonLength + 80;
                    double x1 = ap.Bounds.Max.X - r.Get<Boxed<MortiseTenonParams>>("mt").Value.TenonLength - 80;
                    int n = Math.Max(2, (int)Math.Ceiling((x1 - x0) / spacing) + 1);
                    bool front = ap.Id.EndsWith("-1");
                    double faceY = front ? ap.Bounds.Max.Y : ap.Bounds.Min.Y;
                    for (int i = 0; i < n; i++)
                    {
                        double x = x0 + (x1 - x0) * i / (n - 1);
                        list.Add(new HardwareInstall
                        {
                            HardwareId = "TOP-ZCLIP", HostPartId = ap.Id, MatePartId = "TOP-1",
                            Point = new Vec3(x, faceY, zTop), Normal = new Vec3(0, front ? -1 : 1, 0), AxisU = new Vec3(1, 0, 0), AxisV = new Vec3(0, 0, -1),
                            MatePoint = new Vec3(x, faceY, zTop), MateNormal = new Vec3(0, 0, 1), MateAxisV = new Vec3(0, front ? 1 : -1, 0)
                        });
                    }
                }
                foreach (var ap in aprons[1].Instances)   // short aprons: one fixed-centre slotted screw
                {
                    double xc = (ap.Bounds.Min.X + ap.Bounds.Max.X) / 2, yc = (ap.Bounds.Min.Y + ap.Bounds.Max.Y) / 2;
                    list.Add(new HardwareInstall
                    {
                        HardwareId = "TOP-SLOTSCREW", HostPartId = ap.Id, MatePartId = "TOP-1",
                        Point = new Vec3(xc, yc, zTop), Normal = new Vec3(0, 0, -1), AxisU = new Vec3(0, 1, 0), AxisV = new Vec3(1, 0, 0),
                        MatePoint = new Vec3(xc, yc, zTop), MateNormal = new Vec3(0, 0, 1), MateAxisV = new Vec3(1, 0, 0)
                    });
                }
                return new Boxed<List<HardwareInstall>>(list, string.Join("|", list.Select(h => h.HardwareId + h.Point)));
            });
            return g;
        }

        public FurnitureModel Assemble(DependencyGraph g, ProjectContext ctx)
        {
            var model = new FurnitureModel { TypeId = TypeId, Name = Name };
            foreach (var key in new[] { "comp.top", "comp.legs", "comp.aprons" })
                foreach (var fam in g.Get<Boxed<List<PartFamily>>>(key).Value)
                    model.Families.Add(CloneFamily(fam));

            var engine = new JointEngine(ctx.Joints, ctx.Library);
            engine.Apply(model, g.Get<Boxed<List<JointEngine.Request>>>("joints.requests").Value);

            var installs = g.Get<Boxed<List<HardwareInstall>>>("hardware.installs").Value
                .Select(h => new HardwareInstall
                {
                    HardwareId = h.HardwareId, HostPartId = h.HostPartId, MatePartId = h.MatePartId, Point = h.Point, Normal = h.Normal,
                    AxisU = h.AxisU, AxisV = h.AxisV, MatePoint = h.MatePoint, MateNormal = h.MateNormal, MateAxisV = h.MateAxisV, Quantity = 1
                }).ToList();
            new HardwareInstaller(ctx.Library).Install(model, installs);
            return model;
        }

        internal static PartFamily CloneFamily(PartFamily f)
        {
            var c = new PartFamily
            {
                Id = f.Id, Name = f.Name, Type = f.Type, Assembly = f.Assembly, SpeciesId = f.SpeciesId, Finished = f.Finished,
                RoughPieces = f.RoughPieces.Select(r => new RoughPieceSpec { CountPerPart = r.CountPerPart, Rough = r.Rough, Role = r.Role }).ToList(),
                GrainAxis = f.GrainAxis, GrainAlongLength = f.GrainAlongLength, RequiresGrainContinuity = f.RequiresGrainContinuity,
                VisualGrainRequired = f.VisualGrainRequired, GrainGroup = f.GrainGroup, Notes = f.Notes
            };
            foreach (var i in f.Instances)
                c.Instances.Add(new PartInstance { Id = i.Id, FamilyId = i.FamilyId, Index = i.Index, Bounds = i.Bounds, LengthAxis = i.LengthAxis, WidthAxis = i.WidthAxis, ThicknessAxis = i.ThicknessAxis });
            return c;
        }
    }
}
