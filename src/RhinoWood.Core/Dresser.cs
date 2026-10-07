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
    /// Dresser (comodă): carcass of panels (2 sides, bottom, cap, separators), legs (corners + under every separator), a grid of columns x drawers with graded fronts
    /// (the lower ones taller), hidden soft-close slides, HDF back and drawer bottoms. Visibility classes A/B as in the nightstand; the anti-tip kit is added when
    /// EN 14749 thresholds are exceeded (R10).
    /// </summary>
    public sealed class DresserDefinition : IFurnitureDefinition
    {
        public string TypeId => "casework.dresser";
        public string Name => "Solid-wood dresser";
        public string Category => "Cabinets";

        public IReadOnlyList<ParameterDef> Parameters { get; } = new[]
        {
            new ParameterDef { Key = "width", Label = "Width", Default = 1800, Min = 700, Max = 2400, Group = "Main" },
            new ParameterDef { Key = "depth", Label = "Depth", Default = 460, Min = 350, Max = 650, Group = "Main" },
            new ParameterDef { Key = "height", Label = "Height", Default = 800, Min = 500, Max = 1100, Group = "Main" },
            new ParameterDef { Key = "legHeight", Label = "Leg height", Default = 120, Min = 60, Max = 250, Group = "Legs" },
            new ParameterDef { Key = "columns", Label = "Columns", Unit = "", Default = 2, Min = 1, Max = 4, Group = "Drawer" },
            new ParameterDef { Key = "drawers", Label = "Drawers per column", Unit = "", Default = 4, Min = 1, Max = 6, Group = "Drawer" },
            new ParameterDef { Key = "gradation", Label = "Front gradation (taller at the bottom)", Default = 23, Min = 0, Max = 40, Group = "Drawer", Advanced = true },
            new ParameterDef { Key = "panelThickness", Label = "Panel thickness", Default = 19, Min = 16, Max = 28, Group = "Body", Advanced = true },
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
                Description = "Species of the parts seen only when a drawer is open (drawer boxes, bottom, separators)." },
            new ChoiceDef { StyleKind = StyleKind.Structure, StyleKey = "joint.body", Key = "jointBody", Label = "Body joints", Group = "Joinery", Kind = "joint", Default = "dowel", Options = { "dowel", "biscuit", "loose-tenon", "dado", "pocket-screw" },
                Description = "Joint between the body panels (no visible screws)." },
            new ChoiceDef { StyleKind = StyleKind.Aspect, StyleKey = "front.style", Key = "frontStyle", Label = "Front opening", Group = "Drawer", Kind = "frontstyle", Default = "scoop", Options = { "scoop", "handle", "push", "jrabbet" },
                Description = "How the drawers open. The same in the whole room." },
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
            g.AddComputed("layout", new[] { "species", "materialB", "jointBody", "frontStyle", "width", "depth", "height", "legHeight", "columns", "drawers", "gradation", "panelThickness", "biscuit.pitch" }, r => Build(ctx, r));
            return g;
        }

        private static object Build(ProjectContext ctx, IGraphReader r)
        {
            string spA = r.Get<string>("species"), spB = r.Get<string>("materialB"), joint = r.Get<string>("jointBody"), fstyle = r.Get<string>("frontStyle");
            double W = r.Get<double>("width"), D = r.Get<double>("depth"), H = r.Get<double>("height"), legH = r.Get<double>("legHeight"), t = r.Get<double>("panelThickness"), grad = r.Get<double>("gradation");
            int cols = Math.Max(1, (int)Math.Round(r.Get<double>("columns"))), n = Math.Max(1, (int)Math.Round(r.Get<double>("drawers")));
            var L = new Layout();
            double dd = CaseKit.DadoDepth(joint, t);

            double bx0 = 10, bx1 = 10 + W, yb0 = t, yb1 = D - 5, zTop = H - t, slide = 12.7;
            double iw = W - 2 * t, cw = (iw - (cols - 1) * t) / cols, latD = yb1 - yb0;

            var cap = CaseKit.Family(ctx, "F-DR-CAP", "Cap", PartType.Panel, spA, new Dims(W + 20, D, t), Axis.X, 'A', true); CaseKit.Add(cap, "CAP-1", CaseKit.B(0, 0, zTop, W + 20, D, H), Axis.X, Axis.Y, Axis.Z);
            var side = CaseKit.Family(ctx, "F-DR-SIDE", "Side", PartType.Panel, spA, new Dims(zTop - legH + dd, latD, t), Axis.Z, 'A', true);
            CaseKit.Add(side, "SIDE-1", CaseKit.B(bx0, yb0, legH, bx0 + t, yb1, zTop + dd), Axis.Z, Axis.Y, Axis.X); CaseKit.Add(side, "SIDE-2", CaseKit.B(bx1 - t, yb0, legH, bx1, yb1, zTop + dd), Axis.Z, Axis.Y, Axis.X);
            var bot = CaseKit.Family(ctx, "F-DR-BOT", "Bottom", PartType.Panel, spB, new Dims(iw + 2 * dd, latD, t), Axis.X, 'B', true); CaseKit.Add(bot, "BOT-1", CaseKit.B(bx0 + t - dd, yb0, legH, bx1 - t + dd, yb1, legH + t), Axis.X, Axis.Y, Axis.Z);
            L.Families.AddRange(new[] { cap, side, bot });

            void J(string type, string a, bool atStart, string b, Dictionary<string, double> p = null) => L.Requests.Add(new JointEngine.Request { JointTypeId = type, PartAId = a, PartBId = b, AAtStart = atStart, Params = p ?? new Dictionary<string, double>() });
            J(joint, "BOT-1", true, "SIDE-1"); J(joint, "BOT-1", false, "SIDE-2");
            J(joint, "SIDE-1", false, "CAP-1"); J(joint, "SIDE-2", false, "CAP-1");

            // separators
            var sepX = new List<double>();
            if (cols > 1)
            {
                var sep = CaseKit.Family(ctx, "F-DR-SEP", "Separator", PartType.Panel, spB, new Dims(zTop - legH - t, latD - 14, t), Axis.Z, 'B', true);
                for (int k = 1; k < cols; k++)
                {
                    double sx = bx0 + t + k * cw + (k - 1) * t; sepX.Add(sx);
                    CaseKit.Add(sep, "SEP-" + k, CaseKit.B(sx, yb0, legH + t, sx + t, yb1 - 14, zTop), Axis.Z, Axis.Y, Axis.X);
                    J("dowel", "SEP-" + k, false, "CAP-1"); J("dowel", "SEP-" + k, true, "BOT-1");
                }
                L.Families.Add(sep);
            }

            // legs: corners + under every separator (front and back)
            var leg = CaseKit.Family(ctx, "F-DR-LEG", "Leg", PartType.Leg, spA, new Dims(legH, 45, 45), Axis.Z, 'A', false);
            var legPos = new List<(double x, double y)> { (bx0, yb0), (bx1 - 45, yb0), (bx0, yb1 - 45), (bx1 - 45, yb1 - 45) };
            foreach (var sx in sepX) { legPos.Add((sx + t / 2 - 22.5, yb0)); legPos.Add((sx + t / 2 - 22.5, yb1 - 45)); }
            int li = 0;
            foreach (var (x, y) in legPos)
            {
                string id = "LEG-" + (++li);
                CaseKit.Add(leg, id, CaseKit.B(x, y, 0, x + 45, y + 45, legH), Axis.Z, Axis.X, Axis.Y);
                J("dowel", id, false, "BOT-1", new Dictionary<string, double> { ["diameter"] = 10, ["depth"] = 20 });
                L.Installs.Add(new HardwareInstall { HardwareId = "FOOT-LEVEL", HostPartId = id, MatePartId = "", Point = new Vec3(x + 22.5, y + 22.5, 0), Normal = new Vec3(0, 0, 1), AxisU = new Vec3(1, 0, 0), AxisV = new Vec3(0, 1, 0), MatePoint = new Vec3(0, 0, 0), MateNormal = new Vec3(0, 0, 1), MateAxisV = new Vec3(0, 1, 0) });
            }
            L.Families.Add(leg);

            // fronts graded: row 0 = bottom (tallest)
            double zLow = legH + 1.5, zHigh = zTop - 1.5, F = zHigh - zLow - (n - 1) * 3;
            double baseH = (F - grad * n * (n - 1) / 2.0) / n;
            double fw = W / cols - 3;
            double zRow = zLow;
            double sl = Math.Max(150, Math.Floor((latD - t - 15) / 50) * 50);
            double bw = cw - 2 * slide, fbw = bw - 2 * t;
            for (int row = 0; row < n; row++)
            {
                double fh = baseH + grad * (n - 1 - row);
                double bh = Math.Max(60, fh - 40);
                double zb = Math.Max(zRow + 15, legH + t + 3);
                var ffam = CaseKit.Family(ctx, "F-DR-FRONT-" + (row + 1), "Drawer front", PartType.Drawer, spA, new Dims(fw, fh, t), Axis.X, 'A', true);
                var dside = CaseKit.Family(ctx, "F-DR-DSIDE-" + (row + 1), "Drawer side", PartType.Drawer, spB, new Dims(sl, bh, t), Axis.Y, 'B', true);
                var dfront = CaseKit.Family(ctx, "F-DR-DFRONT-" + (row + 1), "Drawer inner front", PartType.Drawer, spB, new Dims(fbw, bh, t), Axis.X, 'B', true);
                var dback = CaseKit.Family(ctx, "F-DR-DBACK-" + (row + 1), "Drawer back", PartType.Drawer, spB, new Dims(fbw, bh - 12, t), Axis.X, 'B', true);
                for (int c = 0; c < cols; c++)
                {
                    string cell = "R" + (row + 1) + "C" + (c + 1);
                    double fx0 = bx0 + c * (W / cols) + 1.5;
                    CaseKit.Add(ffam, "FRONT-" + cell, CaseKit.B(fx0, 0, zRow, fx0 + fw, t, zRow + fh), Axis.X, Axis.Z, Axis.Y);
                    CaseKit.FrontHardware(L.Installs, fstyle, "FRONT-" + cell, CaseKit.B(fx0, 0, zRow, fx0 + fw, t, zRow + fh));
                    double xL = bx0 + t + c * (cw + t), xs0 = xL + slide, xs1 = xL + cw - slide;
                    CaseKit.Add(dside, "DSIDE-" + cell + "-1", CaseKit.B(xs0, yb0, zb, xs0 + t, yb0 + sl, zb + bh), Axis.Y, Axis.Z, Axis.X);
                    CaseKit.Add(dside, "DSIDE-" + cell + "-2", CaseKit.B(xs1 - t, yb0, zb, xs1, yb0 + sl, zb + bh), Axis.Y, Axis.Z, Axis.X);
                    CaseKit.Add(dfront, "DFRONT-" + cell, CaseKit.B(xs0 + t, yb0, zb, xs1 - t, yb0 + t, zb + bh), Axis.X, Axis.Z, Axis.Y);
                    CaseKit.Add(dback, "DBACK-" + cell, CaseKit.B(xs0 + t, yb0 + sl - t, zb, xs1 - t, yb0 + sl, zb + bh - 12), Axis.X, Axis.Z, Axis.Y);
                    J("dowel", "DFRONT-" + cell, true, "DSIDE-" + cell + "-1"); J("dowel", "DFRONT-" + cell, false, "DSIDE-" + cell + "-2");
                    J("dowel", "DBACK-" + cell, true, "DSIDE-" + cell + "-1"); J("dowel", "DBACK-" + cell, false, "DSIDE-" + cell + "-2");
                    L.Installs.Add(new HardwareInstall { HardwareId = "SLIDE-SC", HostPartId = "DSIDE-" + cell + "-1", MatePartId = "", Point = new Vec3(xs0, yb0 + sl / 2, zb + 20), Normal = new Vec3(-1, 0, 0), AxisU = new Vec3(0, 1, 0), AxisV = new Vec3(0, 0, 1), MatePoint = new Vec3(xs0, yb0 + sl / 2, zb + 20), MateNormal = new Vec3(1, 0, 0), MateAxisV = new Vec3(0, 0, 1) });
                    L.Sheets.Add(new SheetPart { Id = "DBOT-" + cell, Name = "Drawer bottom", Material = "HDF 3", Thickness = 3, Bounds = CaseKit.B(xs0 + t - 6, yb0 + 5, zb + 8, xs1 - t + 6, yb0 + 5 + sl - 15, zb + 11), AreaM2 = (fbw + 12) * (sl - 15) / 1e6 });
                }
                L.Families.AddRange(new[] { ffam, dside, dfront, dback });
                zRow += fh + 3;
            }
            L.Sheets.Add(new SheetPart { Id = "BACK-1", Name = "Back", Material = "HDF 3", Thickness = 3, Bounds = CaseKit.B(bx0 + t - 7.5, yb1 - 3, legH + t - 7.5, bx1 - t + 7.5, yb1, zTop + 7.5), AreaM2 = (iw + 15) * (zTop - legH - t + 15) / 1e6 });
            L.AntiTipHost = "SIDE-1"; L.AntiTipPoint = new Vec3(bx0 + t, yb1 - 20, zTop - 30);
            return new Boxed<Layout>(L, CaseKit.Fingerprint(L.Families, L.Requests, L.Sheets) + "|" + fstyle + string.Join(";", L.Installs.Select(h => h.HardwareId + h.HostPartId)));
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
            BiscuitPlanner.Apply(model, g.Get<double>("biscuit.pitch"), g.Get<string>("edgeJoint"));
            CaseKit.AddFrontFeatures(model, g.Get<string>("frontStyle"));
            return model;
        }
    }
}
