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
    /// Nightstand (noptieră): 4 legs, two side panels, bottom, fixed shelf, cap and one drawer (box + overlay front) with a niche below.
    /// Parts are classified by visibility (A visible closed / B visible when open); the species of class B follows the variant
    /// (PREMIUM oak, STANDARD ash, ECONOMA spruce - the sheet "Fișa dormitor"). Panels wider than one board are edge-glued from strips joined with biscuits.
    /// Backs and drawer bottoms are HDF sheet parts (priced by area). Body joints are dowels (MIX-013: no visible screws).
    /// </summary>
    public sealed class NightstandDefinition : IFurnitureDefinition
    {
        public string TypeId => "casework.nightstand";
        public string Name => "Solid-wood nightstand";
        public string Category => "Cabinets";

        public IReadOnlyList<ParameterDef> Parameters { get; } = new[]
        {
            new ParameterDef { Key = "width", Label = "Width", Default = 520, Min = 350, Max = 800, Group = "Main" },
            new ParameterDef { Key = "depth", Label = "Depth", Default = 410, Min = 300, Max = 550, Group = "Main" },
            new ParameterDef { Key = "height", Label = "Height", Default = 550, Min = 400, Max = 750, Group = "Main" },
            new ParameterDef { Key = "legHeight", Label = "Leg height", Default = 150, Min = 60, Max = 300, Group = "Legs" },
            new ParameterDef { Key = "panelThickness", Label = "Panel thickness", Default = 19, Min = 16, Max = 28, Group = "Body", Advanced = true },
            new ParameterDef { Key = "drawerHeight", Label = "Drawer front height", Default = 200, Min = 120, Max = 300, Group = "Drawer", Advanced = true },
            new ParameterDef { Key = "biscuitPitch", Label = "Biscuit pitch (glued panels)", Default = 200, Min = 120, Max = 300, Group = "Body", Advanced = true },
        };

        public IReadOnlyList<TierDef> Tiers { get; } = new[]
        {
            new TierDef { Id = Furniture.Tiers.Economa, Meta = "interior din molid", Choices = { ["materialB"] = "SPRUCE", ["jointBody"] = "dowel" } },
            new TierDef { Id = Furniture.Tiers.Standard, Meta = "interior din frasin", Choices = { ["materialB"] = "ASH", ["jointBody"] = "dowel" } },
            new TierDef { Id = Furniture.Tiers.Premium, Meta = "stejar integral", Choices = { ["materialB"] = "OAK", ["jointBody"] = "dowel" } },
        };

        public IReadOnlyList<ChoiceDef> Choices { get; } = new List<ChoiceDef>
        {
            new ChoiceDef { StyleKind = StyleKind.Structure, StyleKey = "material.interior", Key = "materialB", Label = "Interior (class B) species", Group = "Materials", Kind = "species", Default = "ASH", Options = { "OAK", "ASH", "SPRUCE", "PINE" },
                Description = "Species of the parts seen only when the drawer is open (drawer box, bottom, shelf). Class A (fronts, sides, cap, legs) uses the project species." },
            new ChoiceDef { StyleKind = StyleKind.Structure, StyleKey = "joint.body", Key = "jointBody", Label = "Body joints", Group = "Joinery", Kind = "joint", Default = "dowel", Options = { "dowel", "biscuit", "loose-tenon", "dado", "pocket-screw" },
                Description = "Joint between the body panels (no visible screws)." },
            new ChoiceDef { StyleKind = StyleKind.Aspect, StyleKey = "front.style", Key = "frontStyle", Label = "Front opening", Group = "Drawer", Kind = "frontstyle", Default = "scoop", Options = { "scoop", "handle", "push", "jrabbet" },
                Description = "How the drawer opens: finger scoop, handle (128 mm), push-to-open, or J finger rabbet. The same in the whole room." },
            CaseKit.EdgeJointChoice(),
        };

        public IReadOnlyList<string> OverridableNodes { get; } = new string[0];
        public string NodeFor(string key) => key == "biscuitPitch" ? "biscuit.pitch" : key;

        private sealed class Layout
        {
            public List<PartFamily> Families = new List<PartFamily>();
            public List<JointEngine.Request> Requests = new List<JointEngine.Request>();
            public List<HardwareInstall> Installs = new List<HardwareInstall>();
            public List<SheetPart> Sheets = new List<SheetPart>();
        }

        public DependencyGraph CreateGraph(ProjectContext ctx, IDictionary<string, double> v, IDictionary<string, string> ch, string speciesId)
        {
            var g = new DependencyGraph();
            double Val(string k) => v.TryGetValue(k, out var d) ? d : Parameters.First(p => p.Key == k).Default;
            string Ch(string k) => ch != null && ch.TryGetValue(k, out var s) ? s : Choices.First(c => c.Key == k).Default;
            g.AddInput("species", speciesId);
            g.AddInput("materialB", Ch("materialB")); g.AddInput("jointBody", Ch("jointBody")); g.AddInput("frontStyle", Ch("frontStyle")); g.AddInput("edgeJoint", Ch("edgeJoint"));
            foreach (var p in Parameters) g.AddInput(NodeFor(p.Key), Val(p.Key));
            var deps = new[] { "species", "materialB", "jointBody", "frontStyle", "width", "depth", "height", "legHeight", "panelThickness", "drawerHeight", "biscuit.pitch" };
            g.AddComputed("layout", deps, r => Build(ctx, r));
            return g;
        }

        private static PartFamily Family(ProjectContext ctx, string id, string name, PartType type, string species, Dims fin, Axis grain, char cls, bool glued) => CaseKit.Family(ctx, id, name, type, species, fin, grain, cls, glued);
        private static void Add(PartFamily fam, string id, Box3 b, Axis len, Axis wid, Axis thk) => CaseKit.Add(fam, id, b, len, wid, thk);
        private static Box3 B(double x0, double y0, double z0, double x1, double y1, double z1) => CaseKit.B(x0, y0, z0, x1, y1, z1);

        private static object Build(ProjectContext ctx, IGraphReader r)
        {
            string spA = r.Get<string>("species"), spB = r.Get<string>("materialB"), joint = r.Get<string>("jointBody"), fstyle = r.Get<string>("frontStyle");
            double W = r.Get<double>("width"), D = r.Get<double>("depth"), H = r.Get<double>("height"), legH = r.Get<double>("legHeight");
            double t = r.Get<double>("panelThickness"), dH = r.Get<double>("drawerHeight");
            var L = new Layout();
            double dd = CaseKit.DadoDepth(joint, t);   // dado housing: panels reach into the grooves

            double x0 = 10, xe = W - 10, back = 5;
            double yb0 = t, yb1 = D - back;                  // body depth range (the drawer front occupies 0..t)
            double iw = xe - x0 - 2 * t, latD = yb1 - yb0, zTop = H - t;
            double zShelf1 = zTop - (dH + 3), zShelf0 = zShelf1 - t;
            double slide = 12.7;

            // ---- class A
            var cap = Family(ctx, "F-NS-CAP", "Cap", PartType.Panel, spA, new Dims(W, D, t), Axis.X, 'A', true); Add(cap, "CAP-1", B(0, 0, zTop, W, D, H), Axis.X, Axis.Y, Axis.Z);
            var side = Family(ctx, "F-NS-SIDE", "Side", PartType.Panel, spA, new Dims(zTop - legH + dd, latD, t), Axis.Z, 'A', true);
            Add(side, "SIDE-1", B(x0, yb0, legH, x0 + t, yb1, zTop + dd), Axis.Z, Axis.Y, Axis.X); Add(side, "SIDE-2", B(xe - t, yb0, legH, xe, yb1, zTop + dd), Axis.Z, Axis.Y, Axis.X);
            double frontW = xe - x0 - 3, frontH = dH;
            var front = Family(ctx, "F-NS-FRONT", "Drawer front", PartType.Drawer, spA, new Dims(frontW, frontH, t), Axis.X, 'A', true);
            Add(front, "FRONT-1", B(x0 + 1.5, 0, zShelf1 + 1.5, xe - 1.5, t, zShelf1 + 1.5 + frontH), Axis.X, Axis.Z, Axis.Y);
            var leg = Family(ctx, "F-NS-LEG", "Leg", PartType.Leg, spA, new Dims(legH, 45, 45), Axis.Z, 'A', false);
            double[] lx = { x0, xe - 45 }, ly = { yb0, yb1 - 45 }; int li = 0;
            foreach (var y in ly) foreach (var x in lx) Add(leg, "LEG-" + (++li), B(x, y, 0, x + 45, y + 45, legH), Axis.Z, Axis.X, Axis.Y);

            // ---- class B
            var bot = Family(ctx, "F-NS-BOT", "Bottom", PartType.Panel, spB, new Dims(iw + 2 * dd, latD, t), Axis.X, 'B', true); Add(bot, "BOT-1", B(x0 + t - dd, yb0, legH, xe - t + dd, yb1, legH + t), Axis.X, Axis.Y, Axis.Z);
            var shelf = Family(ctx, "F-NS-SHELF", "Niche shelf", PartType.Shelf, spB, new Dims(iw + 2 * dd, latD - 14, t), Axis.X, 'B', true); Add(shelf, "SHELF-1", B(x0 + t - dd, yb0, zShelf0, xe - t + dd, yb1 - 14, zShelf1), Axis.X, Axis.Y, Axis.Z);

            // drawer box
            double bw = iw - 2 * slide, sl = Math.Max(150, Math.Floor((latD - t - 15) / 50) * 50), bh = dH - 46.5, zb = zShelf1 + 15;
            double xs0 = x0 + t + slide, xs1 = xe - t - slide;
            var dside = Family(ctx, "F-NS-DSIDE", "Drawer side", PartType.Drawer, spB, new Dims(sl, bh, t), Axis.Y, 'B', true);
            Add(dside, "DSIDE-1", B(xs0, yb0, zb, xs0 + t, yb0 + sl, zb + bh), Axis.Y, Axis.Z, Axis.X); Add(dside, "DSIDE-2", B(xs1 - t, yb0, zb, xs1, yb0 + sl, zb + bh), Axis.Y, Axis.Z, Axis.X);
            double fbw = bw - 2 * t;
            var dfront = Family(ctx, "F-NS-DFRONT", "Drawer inner front", PartType.Drawer, spB, new Dims(fbw, bh, t), Axis.X, 'B', true); Add(dfront, "DFRONT-1", B(xs0 + t, yb0, zb, xs1 - t, yb0 + t, zb + bh), Axis.X, Axis.Z, Axis.Y);
            var dback = Family(ctx, "F-NS-DBACK", "Drawer back", PartType.Drawer, spB, new Dims(fbw, bh - 12, t), Axis.X, 'B', true); Add(dback, "DBACK-1", B(xs0 + t, yb0 + sl - t, zb, xs1 - t, yb0 + sl, zb + bh - 12), Axis.X, Axis.Z, Axis.Y);

            L.Families.AddRange(new[] { cap, side, front, leg, bot, shelf, dside, dfront, dback });

            // ---- joints (dowels or biscuits; legs always dowelled, 10 mm)
            void J(string type, string a, bool atStart, string b, Dictionary<string, double> p = null) => L.Requests.Add(new JointEngine.Request { JointTypeId = type, PartAId = a, PartBId = b, AAtStart = atStart, Params = p ?? new Dictionary<string, double>() });
            J(joint, "BOT-1", true, "SIDE-1"); J(joint, "BOT-1", false, "SIDE-2");
            J(joint, "SHELF-1", true, "SIDE-1"); J(joint, "SHELF-1", false, "SIDE-2");
            J(joint, "SIDE-1", false, "CAP-1"); J(joint, "SIDE-2", false, "CAP-1");
            for (int i = 1; i <= 4; i++) J("dowel", "LEG-" + i, false, "BOT-1", new Dictionary<string, double> { ["diameter"] = 10, ["depth"] = 20 });
            J("dowel", "DFRONT-1", true, "DSIDE-1"); J("dowel", "DFRONT-1", false, "DSIDE-2");
            J("dowel", "DBACK-1", true, "DSIDE-1"); J("dowel", "DBACK-1", false, "DSIDE-2");

            CaseKit.FrontHardware(L.Installs, fstyle, "FRONT-1", B(x0 + 1.5, 0, zShelf1 + 1.5, xe - 1.5, t, zShelf1 + 1.5 + frontH));
            // ---- hardware: one soft-close slide pair, levelling feet
            L.Installs.Add(new HardwareInstall { HardwareId = "SLIDE-SC", HostPartId = "DSIDE-1", MatePartId = "", Point = new Vec3(xs0, yb0 + sl / 2, zb + 20), Normal = new Vec3(-1, 0, 0), AxisU = new Vec3(0, 1, 0), AxisV = new Vec3(0, 0, 1), MatePoint = new Vec3(x0 + t, yb0 + sl / 2, zb + 20), MateNormal = new Vec3(1, 0, 0), MateAxisV = new Vec3(0, 0, 1) });
            for (int i = 1; i <= 4; i++)
                L.Installs.Add(new HardwareInstall { HardwareId = "FOOT-LEVEL", HostPartId = "LEG-" + i, MatePartId = "", Point = new Vec3(0, 0, 0), Normal = new Vec3(0, 0, -1), AxisU = new Vec3(1, 0, 0), AxisV = new Vec3(0, 1, 0), MatePoint = new Vec3(0, 0, 0), MateNormal = new Vec3(0, 0, 1), MateAxisV = new Vec3(0, 1, 0) });

            // ---- HDF sheet parts: back in a groove, drawer bottom
            L.Sheets.Add(new SheetPart { Id = "BACK-1", Name = "Back", Material = "HDF 3", Thickness = 3, Bounds = B(x0 + t - 7.5, yb1 - 3, legH + t - 7.5, xe - t + 7.5, yb1, zTop + 7.5), AreaM2 = (iw + 15) * (zTop - legH - t + 15) / 1e6 });
            L.Sheets.Add(new SheetPart { Id = "DBOT-1", Name = "Drawer bottom", Material = "HDF 3", Thickness = 3, Bounds = B(xs0 + t - 6, yb0 + 5, zb + 8, xs1 - t + 6, yb0 + 5 + sl - 15, zb + 11), AreaM2 = (fbw + 12) * (sl - 15) / 1e6 });

            return new Boxed<Layout>(L, CaseKit.Fingerprint(L.Families, L.Requests, L.Sheets) + "|" + fstyle + string.Join(";", L.Installs.Select(h => h.HardwareId)));
        }

        public FurnitureModel Assemble(DependencyGraph g, ProjectContext ctx)
        {
            var model = new FurnitureModel { TypeId = TypeId, Name = Name, Category = Category };
            var L = g.Get<Boxed<Layout>>("layout").Value;
            foreach (var fam in L.Families) model.Families.Add(TableDefinition.CloneFamily(fam));
            new JointEngine(ctx.Joints, ctx.Library).Apply(model, L.Requests);
            var installs = L.Installs.Select(h => new HardwareInstall
            {
                HardwareId = h.HardwareId, HostPartId = h.HostPartId, MatePartId = h.MatePartId, Point = h.Point, Normal = h.Normal, AxisU = h.AxisU, AxisV = h.AxisV,
                MatePoint = h.MatePoint, MateNormal = h.MateNormal, MateAxisV = h.MateAxisV, Quantity = 1
            }).ToList();
            new HardwareInstaller(ctx.Library).Install(model, installs);
            BiscuitPlanner.Apply(model, g.Get<double>("biscuit.pitch"), g.Get<string>("edgeJoint"));
            CaseKit.AddFrontFeatures(model, g.Get<string>("frontStyle"));
            model.SheetParts.AddRange(L.Sheets.Select(s => new SheetPart { Id = s.Id, Name = s.Name, Material = s.Material, Bounds = s.Bounds, Thickness = s.Thickness, AreaM2 = s.AreaM2 }));
            return model;
        }
    }
}
