using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RhinoWood.Core.Domain;
using RhinoWood.Core.Geometry;
using RhinoWood.Core.HardwareSystem;
using RhinoWood.Core.Joinery;
using RhinoWood.Core.Parametric;

namespace RhinoWood.Core.Furniture
{
    /// <summary>
    /// Wardrobe (dulap): carcass of panels, overlay hinged doors (cup hinges 35 mm, count by door height), a clothes rail at 1520–1770 mm, fixed shelves, HDF back,
    /// feet. Doors and visible panels are class A; the shelves are class B. Tall and heavy: the anti-tip kit is added automatically (EN 14749).
    /// </summary>
    public sealed class WardrobeDefinition : IFurnitureDefinition
    {
        public string TypeId => "casework.wardrobe";
        public string Name => "Solid-wood wardrobe";
        public string Category => "Wardrobes";

        public IReadOnlyList<ParameterDef> Parameters { get; } = new[]
        {
            new ParameterDef { Key = "width", Label = "Width", Default = 1000, Min = 500, Max = 2400, Group = "Main" },
            new ParameterDef { Key = "depth", Label = "Depth", Default = 600, Min = 450, Max = 700, Group = "Main" },
            new ParameterDef { Key = "height", Label = "Height", Default = 2200, Min = 1200, Max = 2600, Group = "Main" },
            new ParameterDef { Key = "legHeight", Label = "Leg height", Default = 100, Min = 60, Max = 200, Group = "Legs" },
            new ParameterDef { Key = "doors", Label = "Doors", Unit = "", Default = 2, Min = 1, Max = 6, Group = "Doors" },
            new ParameterDef { Key = "shelves", Label = "Fixed shelves above the rail", Unit = "", Default = 1, Min = 0, Max = 4, Group = "Interior" },
            new ParameterDef { Key = "panelThickness", Label = "Panel thickness", Default = 19, Min = 16, Max = 28, Group = "Body", Advanced = true },
            new ParameterDef { Key = "biscuitPitch", Label = "Biscuit pitch (glued panels)", Default = 200, Min = 120, Max = 300, Group = "Body", Advanced = true },
        };

        public IReadOnlyList<TierDef> Tiers { get; } = new[]
        {
            new TierDef { Id = Furniture.Tiers.Economa, Meta = "interior din molid", Choices = { ["materialB"] = "SPRUCE", ["jointBody"] = "dowel" } },
            new TierDef { Id = Furniture.Tiers.Standard, Meta = "interior din frasin", Choices = { ["materialB"] = "ASH", ["jointBody"] = "dowel" } },
            new TierDef { Id = Furniture.Tiers.Premium, Meta = "stejar integral", Choices = { ["materialB"] = "OAK", ["jointBody"] = "dowel" } },
        };

        public IReadOnlyList<ChoiceDef> Choices { get; } = new[]
        {
            new ChoiceDef { StyleKind = StyleKind.Structure, StyleKey = "material.interior", Key = "materialB", Label = "Interior (class B) species", Group = "Materials", Kind = "species", Default = "ASH", Options = { "OAK", "ASH", "SPRUCE", "PINE" } },
            new ChoiceDef { StyleKind = StyleKind.Structure, StyleKey = "joint.body", Key = "jointBody", Label = "Body joints", Group = "Joinery", Kind = "joint", Default = "dowel", Options = { "dowel", "biscuit" } },
            new ChoiceDef { StyleKind = StyleKind.Aspect, StyleKey = "front.style", Key = "frontStyle", Label = "Door opening", Group = "Doors", Kind = "frontstyle", Default = "handle", Options = { "scoop", "handle", "push", "jrabbet" },
                Description = "Handle (vertical, 128 mm), push-to-open, or a routed finger groove on the free edge." },
        };

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
            string Ch(string k) => ch != null && ch.TryGetValue(k, out var s) ? s : Choices.First(c => c.Key == k).Default;
            g.AddInput("species", speciesId);
            foreach (var c in Choices) g.AddInput(c.Key, Ch(c.Key));
            foreach (var p in Parameters) g.AddInput(NodeFor(p.Key), Val(p.Key));
            g.AddComputed("layout", new[] { "species", "materialB", "jointBody", "frontStyle", "width", "depth", "height", "legHeight", "doors", "shelves", "panelThickness", "biscuit.pitch" }, r => Build(ctx, r));
            return g;
        }

        /// <summary>Cup hinges per door: 2 up to 900 mm, 3 up to 1600 mm, 4 above (FUR-DRW-003, to be checked against the maker's table).</summary>
        public static int HingesFor(double doorHeight) => doorHeight <= 900 ? 2 : doorHeight <= 1600 ? 3 : 4;

        private static object Build(ProjectContext ctx, IGraphReader r)
        {
            string spA = r.Get<string>("species"), spB = r.Get<string>("materialB"), joint = r.Get<string>("jointBody"), fstyle = r.Get<string>("frontStyle");
            double W = r.Get<double>("width"), D = r.Get<double>("depth"), H = r.Get<double>("height"), legH = r.Get<double>("legHeight"), t = r.Get<double>("panelThickness");
            int nd = Math.Max(1, (int)Math.Round(r.Get<double>("doors"))), nsh = (int)Math.Round(r.Get<double>("shelves"));
            var L = new Layout();
            double bx0 = 0, bx1 = W, yb0 = t, yb1 = D - 5, zTop = H - t, iw = W - 2 * t, latD = yb1 - yb0;

            var cap = CaseKit.Family(ctx, "F-WR-CAP", "Cap", PartType.Panel, spA, new Dims(W, D, t), Axis.X, 'A', true); CaseKit.Add(cap, "CAP-1", CaseKit.B(0, 0, zTop, W, D, H), Axis.X, Axis.Y, Axis.Z);
            var side = CaseKit.Family(ctx, "F-WR-SIDE", "Side", PartType.Panel, spA, new Dims(zTop - legH, latD, t), Axis.Z, 'A', true);
            CaseKit.Add(side, "SIDE-1", CaseKit.B(0, yb0, legH, t, yb1, zTop), Axis.Z, Axis.Y, Axis.X); CaseKit.Add(side, "SIDE-2", CaseKit.B(W - t, yb0, legH, W, yb1, zTop), Axis.Z, Axis.Y, Axis.X);
            var bot = CaseKit.Family(ctx, "F-WR-BOT", "Bottom", PartType.Panel, spB, new Dims(iw, latD, t), Axis.X, 'B', true); CaseKit.Add(bot, "BOT-1", CaseKit.B(t, yb0, legH, W - t, yb1, legH + t), Axis.X, Axis.Y, Axis.Z);
            L.Families.AddRange(new[] { cap, side, bot });
            void J(string type, string a, bool atStart, string b, Dictionary<string, double> p = null) => L.Requests.Add(new JointEngine.Request { JointTypeId = type, PartAId = a, PartBId = b, AAtStart = atStart, Params = p ?? new Dictionary<string, double>() });
            J(joint, "BOT-1", true, "SIDE-1"); J(joint, "BOT-1", false, "SIDE-2"); J(joint, "SIDE-1", false, "CAP-1"); J(joint, "SIDE-2", false, "CAP-1");

            // clothes rail (1520-1770 mm from the floor) and fixed shelves above it
            double railZ = Math.Min(1700, zTop - 130);
            var shelf = CaseKit.Family(ctx, "F-WR-SHELF", "Shelf", PartType.Shelf, spB, new Dims(iw, latD - 14, t), Axis.X, 'B', true);
            for (int i = 0; i < nsh; i++)
            {
                double zs = railZ + 110 + (zTop - (railZ + 110) - t) * (i + 1) / (nsh + 1) - (nsh == 0 ? 0 : 0);
                zs = Math.Min(zs, zTop - 120);
                string id = "SHELF-" + (i + 1);
                CaseKit.Add(shelf, id, CaseKit.B(t, yb0, zs, W - t, yb1 - 14, zs + t), Axis.X, Axis.Y, Axis.Z);
                J(joint, id, true, "SIDE-1"); J(joint, id, false, "SIDE-2");
            }
            if (nsh > 0) L.Families.Add(shelf);
            L.Installs.Add(new HardwareInstall { HardwareId = "ROD-25", HostPartId = "SIDE-1", MatePartId = "", Point = new Vec3(t, yb0 + latD / 2, railZ), Normal = new Vec3(1, 0, 0), AxisU = new Vec3(0, 1, 0), AxisV = new Vec3(0, 0, 1), MatePoint = new Vec3(W - t, yb0 + latD / 2, railZ), MateNormal = new Vec3(-1, 0, 0), MateAxisV = new Vec3(0, 0, 1) });

            // feet
            var leg = CaseKit.Family(ctx, "F-WR-LEG", "Leg", PartType.Leg, spA, new Dims(legH, 45, 45), Axis.Z, 'A', false);
            int li = 0;
            foreach (var (x, y) in new[] { (0.0, yb0), (W - 45, yb0), (0.0, yb1 - 45), (W - 45, yb1 - 45) })
            {
                string id = "LEG-" + (++li);
                CaseKit.Add(leg, id, CaseKit.B(x, y, 0, x + 45, y + 45, legH), Axis.Z, Axis.X, Axis.Y);
                J("dowel", id, false, "BOT-1", new Dictionary<string, double> { ["diameter"] = 10, ["depth"] = 20 });
                L.Installs.Add(new HardwareInstall { HardwareId = "FOOT-LEVEL", HostPartId = id, MatePartId = "", Point = new Vec3(x + 22.5, y + 22.5, 0), Normal = new Vec3(0, 0, 1), AxisU = new Vec3(1, 0, 0), AxisV = new Vec3(0, 1, 0), MatePoint = new Vec3(0, 0, 0), MateNormal = new Vec3(0, 0, 1), MateAxisV = new Vec3(0, 1, 0) });
            }
            L.Families.Add(leg);

            // doors: overlay on the front, vertical grain, hinged on the outer edge of each pair
            double dw = W / nd - 3, dz0 = legH + 1.5, dh = zTop + t - 1.5 - dz0 - 3;
            var door = CaseKit.Family(ctx, "F-WR-DOOR", "Door", PartType.Door, spA, new Dims(dh, dw, t), Axis.Z, 'A', true);
            int hinges = HingesFor(dh);
            for (int d = 0; d < nd; d++)
            {
                double x0 = bx0 + d * (W / nd) + 1.5; string id = "DOOR-" + (d + 1);
                CaseKit.Add(door, id, CaseKit.B(x0, 0, dz0, x0 + dw, t, dz0 + dh), Axis.Z, Axis.X, Axis.Y);
                bool hingeLeft = d % 2 == 0; double hx = hingeLeft ? x0 + 22.5 : x0 + dw - 22.5;
                for (int k = 0; k < hinges; k++)
                {
                    double hz = hinges == 2 ? (k == 0 ? dz0 + 100 : dz0 + dh - 100) : dz0 + 100 + (dh - 200) * k / (hinges - 1);
                    L.Installs.Add(new HardwareInstall { HardwareId = "HINGE-CUP35", HostPartId = id, MatePartId = "", Point = new Vec3(hx, t, hz), Normal = new Vec3(0, -1, 0), AxisU = new Vec3(1, 0, 0), AxisV = new Vec3(0, 0, 1), MatePoint = new Vec3(hx, t, hz), MateNormal = new Vec3(0, 1, 0), MateAxisV = new Vec3(0, 0, 1) });
                }
                if (fstyle == "handle" || fstyle == "push")
                {
                    double ex = hingeLeft ? x0 + dw - 40 : x0 + 40, ez = dz0 + dh / 2;
                    L.Installs.Add(new HardwareInstall { HardwareId = fstyle == "handle" ? "HANDLE-128" : "PUSH-OPEN", HostPartId = id, MatePartId = "", Point = new Vec3(ex, 0, ez), Normal = new Vec3(0, 1, 0), AxisU = new Vec3(0, 0, 1), AxisV = new Vec3(1, 0, 0), MatePoint = new Vec3(ex, 0, ez), MateNormal = new Vec3(0, -1, 0), MateAxisV = new Vec3(1, 0, 0) });
                }
            }
            L.Families.Add(door);
            L.Sheets.Add(new SheetPart { Id = "BACK-1", Name = "Back", Material = "HDF 3", Thickness = 3, Bounds = CaseKit.B(t - 7.5, yb1 - 3, legH + t - 7.5, W - t + 7.5, yb1, zTop + 7.5), AreaM2 = (iw + 15) * (zTop - legH - t + 15) / 1e6 });
            L.AntiTipHost = "SIDE-1"; L.AntiTipPoint = new Vec3(t, yb1 - 20, zTop - 30);
            return new Boxed<Layout>(L, CaseKit.Fingerprint(L.Families, L.Requests, L.Sheets) + "|" + fstyle + string.Join(";", L.Installs.Select(h => h.HardwareId + h.HostPartId + h.Point)));
        }

        public FurnitureModel Assemble(DependencyGraph g, ProjectContext ctx)
        {
            var model = new FurnitureModel { TypeId = TypeId, Name = Name, Category = Category };
            var L = g.Get<Boxed<Layout>>("layout").Value;
            foreach (var fam in L.Families) model.Families.Add(TableDefinition.CloneFamily(fam));
            model.SheetParts.AddRange(L.Sheets.Select(s => new SheetPart { Id = s.Id, Name = s.Name, Material = s.Material, Bounds = s.Bounds, Thickness = s.Thickness, AreaM2 = s.AreaM2 }));
            new JointEngine(ctx.Joints, ctx.Library).Apply(model, L.Requests);
            var installs = L.Installs.Select(h => new HardwareInstall
            {
                HardwareId = h.HardwareId, HostPartId = h.HostPartId, MatePartId = h.MatePartId, Point = h.Point, Normal = h.Normal, AxisU = h.AxisU, AxisV = h.AxisV,
                MatePoint = h.MatePoint, MateNormal = h.MateNormal, MateAxisV = h.MateAxisV, Quantity = 1
            }).ToList();
            CaseKit.AntiTip(installs, model, ctx.Library, L.AntiTipHost, L.AntiTipPoint);
            new HardwareInstaller(ctx.Library).Install(model, installs);
            BiscuitPlanner.Apply(model, g.Get<double>("biscuit.pitch"));
            // finger groove on the free edge of every door (scoop 120 mm / full-length J rabbet)
            string st = g.Get<string>("frontStyle");
            if (st == "scoop" || st == "jrabbet")
            {
                int n = 0;
                foreach (var d in model.AllParts.Where(p => p.Id.StartsWith("DOOR", StringComparison.Ordinal)))
                {
                    var f = d.Finished; double t = f.Thickness; bool hingeLeft = d.Index % 2 == 0;
                    double y0 = hingeLeft ? f.Width - 30 : 0, y1 = hingeLeft ? f.Width : 30;      // local y = across the door, free edge opposite the hinge
                    var box = st == "scoop" ? new Box3(new Vec3(f.Length / 2 - 60, y0, t - 10), new Vec3(f.Length / 2 + 60, y1, t)) : new Box3(new Vec3(0, hingeLeft ? f.Width - 22 : 0, t - 10), new Vec3(f.Length, hingeLeft ? f.Width : 22, t));
                    d.Features.Add(JointGeometry.Rect(d, box, st == "scoop" ? FeatureKind.RoutPocket : FeatureKind.Rabbet, 10, st == "scoop" ? "Finger scoop" : "J finger rabbet", "ROUT-8", "STY", "STY" + (++n).ToString("000", CultureInfo.InvariantCulture)));
                }
            }
            return model;
        }
    }
}
