using System;
using System.Collections.Generic;
using System.Linq;
using RhinoWood.Core.Domain;
using RhinoWood.Core.Geometry;
using RhinoWood.Core.HardwareSystem;
using RhinoWood.Core.Joinery;
using RhinoWood.Core.Parametric;

namespace RhinoWood.Core.Furniture
{
    /// <summary>
    /// Shelving unit (etajeră): two uprights and N shelves housed in dados (depth 1/3 of the upright, max 12 mm), HDF back. Everything is visible (class A).
    /// The shelf span is checked for deflection (L/300 under 0.65 kg/dm³): the validation tells when to shorten the span or thicken the shelf.
    /// </summary>
    public sealed class ShelvingDefinition : IFurnitureDefinition
    {
        public string TypeId => "casework.shelving";
        public string Name => "Solid-wood shelving unit";
        public string Category => "Shelving";

        public IReadOnlyList<ParameterDef> Parameters { get; } = new[]
        {
            new ParameterDef { Key = "width", Label = "Width", Default = 800, Min = 300, Max = 1400, Group = "Main" },
            new ParameterDef { Key = "depth", Label = "Depth", Default = 300, Min = 180, Max = 500, Group = "Main" },
            new ParameterDef { Key = "height", Label = "Height", Default = 1800, Min = 500, Max = 2400, Group = "Main" },
            new ParameterDef { Key = "shelves", Label = "Shelves", Unit = "", Default = 5, Min = 1, Max = 10, Group = "Shelves" },
            new ParameterDef { Key = "shelfThickness", Label = "Shelf thickness", Default = 22, Min = 16, Max = 40, Group = "Shelves" },
            new ParameterDef { Key = "biscuitPitch", Label = "Biscuit pitch (glued panels)", Default = 200, Min = 120, Max = 300, Group = "Shelves", Advanced = true },
        };

        public IReadOnlyList<TierDef> Tiers { get; } = new TierDef[0];
        public IReadOnlyList<ChoiceDef> Choices { get; } = new ChoiceDef[0];
        public IReadOnlyList<string> OverridableNodes { get; } = new string[0];
        public string NodeFor(string key) => key == "biscuitPitch" ? "biscuit.pitch" : key;

        private sealed class Layout
        {
            public List<PartFamily> Families = new List<PartFamily>();
            public List<JointEngine.Request> Requests = new List<JointEngine.Request>();
            public List<HardwareInstall> Installs = new List<HardwareInstall>();
            public List<SheetPart> Sheets = new List<SheetPart>();
            public string AntiTipHost; public Vec3 AntiTipPoint;
        }

        public DependencyGraph CreateGraph(ProjectContext ctx, IDictionary<string, double> v, IDictionary<string, string> ch, string speciesId)
        {
            var g = new DependencyGraph();
            double Val(string k) => v.TryGetValue(k, out var d) ? d : Parameters.First(p => p.Key == k).Default;
            g.AddInput("species", speciesId);
            foreach (var p in Parameters) g.AddInput(NodeFor(p.Key), Val(p.Key));
            g.AddComputed("layout", new[] { "species", "width", "depth", "height", "shelves", "shelfThickness", "biscuit.pitch" }, r => Build(ctx, r));
            return g;
        }

        private static object Build(ProjectContext ctx, IGraphReader r)
        {
            string sp = r.Get<string>("species");
            double W = r.Get<double>("width"), D = r.Get<double>("depth"), H = r.Get<double>("height"), st = r.Get<double>("shelfThickness");
            int n = Math.Max(1, (int)Math.Round(r.Get<double>("shelves")));
            var L = new Layout();
            double ut = 28;                                   // upright thickness
            double dado = Math.Min(12, ut / 3.0);             // housing depth (same rule as the dado joint)
            double iw = W - 2 * ut, yb1 = D - 5;
            var up = CaseKit.Family(ctx, "F-SH-UP", "Upright", PartType.Panel, sp, new Dims(H, yb1, ut), Axis.Z, 'A', true);
            CaseKit.Add(up, "UP-1", CaseKit.B(0, 0, 0, ut, yb1, H), Axis.Z, Axis.Y, Axis.X); CaseKit.Add(up, "UP-2", CaseKit.B(W - ut, 0, 0, W, yb1, H), Axis.Z, Axis.Y, Axis.X);
            // shelves: bottom shelf at 80 mm (kick), top shelf flush with the top; the rest evenly spread. Bounds reach into the housings.
            var sh = CaseKit.Family(ctx, "F-SH-SHELF", "Shelf", PartType.Shelf, sp, new Dims(iw + 2 * dado, yb1, st), Axis.X, 'A', true);
            double z0 = 80, z1 = H - st;
            for (int i = 0; i < n; i++)
            {
                double z = n == 1 ? z1 : z0 + (z1 - z0) * i / (n - 1);
                string id = "SHELF-" + (i + 1);
                CaseKit.Add(sh, id, CaseKit.B(ut - dado, 0, z, W - ut + dado, yb1, z + st), Axis.X, Axis.Y, Axis.Z);
                L.Requests.Add(new JointEngine.Request { JointTypeId = "dado", PartAId = id, PartBId = "UP-1", AAtStart = true });
                L.Requests.Add(new JointEngine.Request { JointTypeId = "dado", PartAId = id, PartBId = "UP-2", AAtStart = false });
            }
            L.Families.AddRange(new[] { up, sh });
            L.AntiTipHost = "UP-1"; L.AntiTipPoint = new Vec3(ut, yb1 - 20, H - 40);
            L.Sheets.Add(new SheetPart { Id = "BACK-1", Name = "Back", Material = "HDF 3", Thickness = 3, Bounds = CaseKit.B(ut, yb1 - 3, z0, W - ut, yb1, H - 5), AreaM2 = iw * (H - 5 - z0) / 1e6 });
            return new Boxed<Layout>(L, CaseKit.Fingerprint(L.Families, L.Requests, L.Sheets));
        }

        public FurnitureModel Assemble(DependencyGraph g, ProjectContext ctx)
        {
            var model = new FurnitureModel { TypeId = TypeId, Name = Name, Category = Category };
            var L = g.Get<Boxed<Layout>>("layout").Value;
            foreach (var fam in L.Families) model.Families.Add(TableDefinition.CloneFamily(fam));
            model.SheetParts.AddRange(L.Sheets.Select(s => new SheetPart { Id = s.Id, Name = s.Name, Material = s.Material, Bounds = s.Bounds, Thickness = s.Thickness, AreaM2 = s.AreaM2 }));
            new JointEngine(ctx.Joints, ctx.Library).Apply(model, L.Requests);
            var installs = new List<HardwareInstall>();
            CaseKit.AntiTip(installs, model, ctx.Library, L.AntiTipHost, L.AntiTipPoint);
            new HardwareInstaller(ctx.Library).Install(model, installs);
            BiscuitPlanner.Apply(model, g.Get<double>("biscuit.pitch"));
            return model;
        }
    }
}
