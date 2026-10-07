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
    /// Bed (pat): 4 legs (tall at the head), end rails, side rails knocked down with hidden M8 bed bolts (BED-R-030), a headboard (top rail + floating panel in grooves),
    /// ledgers, slats on edge sized to the mattress, a central beam and leg. Visible parts (class A) keep the project species; the hidden structure (slats, ledgers,
    /// beam, centre leg - class C) follows the variant: PREMIUM oak, STANDARD/ECONOMA pine.
    /// </summary>
    public sealed class BedDefinition : IFurnitureDefinition
    {
        public string TypeId => "casework.bed";
        public string Name => "Solid-wood bed";
        public string Category => "Beds";

        public IReadOnlyList<ParameterDef> Parameters { get; } = new[]
        {
            new ParameterDef { Key = "mattressW", Label = "Mattress width", Default = 1600, Min = 800, Max = 2000, Group = "Main" },
            new ParameterDef { Key = "mattressL", Label = "Mattress length", Default = 2000, Min = 1900, Max = 2200, Group = "Main" },
            new ParameterDef { Key = "clearance", Label = "Space under the bed", Default = 150, Min = 50, Max = 400, Group = "Main" },
            new ParameterDef { Key = "headHeight", Label = "Headboard height", Default = 1000, Min = 700, Max = 1400, Group = "Headboard" },
            new ParameterDef { Key = "footHeight", Label = "Foot leg height", Default = 430, Min = 300, Max = 700, Group = "Main" },
            new ParameterDef { Key = "slatPitch", Label = "Slat pitch", Default = 125, Min = 70, Max = 160, Group = "Slats", Advanced = true },
            new ParameterDef { Key = "panelThickness", Label = "Headboard panel thickness", Default = 12, Min = 8, Max = 20, Group = "Headboard", Advanced = true },
            new ParameterDef { Key = "biscuitPitch", Label = "Biscuit pitch (glued panel)", Default = 200, Min = 120, Max = 300, Group = "Headboard", Advanced = true },
        };

        public IReadOnlyList<TierDef> Tiers { get; } = new[]
        {
            new TierDef { Id = Furniture.Tiers.Economa, Meta = "structură ascunsă din pin", Choices = { ["materialC"] = "PINE", ["jointRail"] = "dowel" } },
            new TierDef { Id = Furniture.Tiers.Standard, Meta = "structură ascunsă din pin", Choices = { ["materialC"] = "PINE", ["jointRail"] = "dowel" } },
            new TierDef { Id = Furniture.Tiers.Premium, Meta = "stejar integral", Choices = { ["materialC"] = "OAK", ["jointRail"] = "loose-tenon" } },
        };

        public IReadOnlyList<ChoiceDef> Choices { get; } = new List<ChoiceDef>
        {
            new ChoiceDef { StyleKind = StyleKind.Structure, StyleKey = "material.hidden", Key = "materialC", Label = "Hidden structure (class C) species", Group = "Materials", Kind = "species", Default = "PINE", Options = { "OAK", "ASH", "PINE", "SPRUCE" },
                Description = "Slats, ledgers, beam and centre leg: never seen. Sections are the same (they pass the deflection check in pine)." },
            new ChoiceDef { StyleKind = StyleKind.Structure, StyleKey = "joint.bed-rail", Key = "jointRail", Label = "End rails to legs", Group = "Joinery", Kind = "joint", Default = "dowel", Options = { "dowel", "loose-tenon", "biscuit" },
                Description = "Glued joint of the end rails and the headboard top rail. The side rails are always knock-down (bed bolts)." },
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
            public List<(string part, Box3 box, double depth, string purpose)> Grooves = new List<(string, Box3, double, string)>();   // local boxes
        }

        public DependencyGraph CreateGraph(ProjectContext ctx, IDictionary<string, double> v, IDictionary<string, string> ch, string speciesId)
        {
            var g = new DependencyGraph();
            double Val(string k) => v.TryGetValue(k, out var d) ? d : Parameters.First(p => p.Key == k).Default;
            string Ch(string k) => ch != null && ch.TryGetValue(k, out var s) ? s : Choices.First(c => c.Key == k).Default;
            g.AddInput("species", speciesId);
            foreach (var c in Choices) g.AddInput(c.Key, Ch(c.Key));
            foreach (var p in Parameters) g.AddInput(NodeFor(p.Key), Val(p.Key));
            g.AddComputed("layout", new[] { "species", "materialC", "jointRail", "mattressW", "mattressL", "clearance", "headHeight", "footHeight", "slatPitch", "panelThickness", "biscuit.pitch" }, r => Build(ctx, r));
            return g;
        }

        private static object Build(ProjectContext ctx, IGraphReader r)
        {
            string spA = r.Get<string>("species"), spC = r.Get<string>("materialC"), joint = r.Get<string>("jointRail");
            double mW = r.Get<double>("mattressW"), mL = r.Get<double>("mattressL"), clr = r.Get<double>("clearance"), hH = r.Get<double>("headHeight"), fH = r.Get<double>("footHeight");
            double pitch = r.Get<double>("slatPitch"), pT = r.Get<double>("panelThickness");
            var L = new Layout();
            const double ls = 70, rt = 28, rw = 120, topW = 95;

            double innerW = mW + 4, W = innerW + 2 * rt, innerL = mL + 4, Ltot = innerL + 2 * 49;
            double railZ0 = clr, railZ1 = clr + rw;
            double slatTop = railZ1, slatH = 70, slatW = 28, ledgerTop = slatTop - slatH;

            // ---- class A: legs, rails, headboard
            var hleg = CaseKit.Family(ctx, "F-BED-HLEG", "Headboard leg", PartType.Leg, spA, new Dims(hH, ls, ls), Axis.Z, 'A', false, "Frame");
            CaseKit.Add(hleg, "HLEG-1", CaseKit.B(0, 0, 0, ls, ls, hH), Axis.Z, Axis.X, Axis.Y); CaseKit.Add(hleg, "HLEG-2", CaseKit.B(W - ls, 0, 0, W, ls, hH), Axis.Z, Axis.X, Axis.Y);
            var fleg = CaseKit.Family(ctx, "F-BED-FLEG", "Foot leg", PartType.Leg, spA, new Dims(fH, ls, ls), Axis.Z, 'A', false, "Frame");
            CaseKit.Add(fleg, "FLEG-1", CaseKit.B(0, Ltot - ls, 0, ls, Ltot, fH), Axis.Z, Axis.X, Axis.Y); CaseKit.Add(fleg, "FLEG-2", CaseKit.B(W - ls, Ltot - ls, 0, W, Ltot, fH), Axis.Z, Axis.X, Axis.Y);
            double erLen = W - 2 * ls;
            var hrail = CaseKit.Family(ctx, "F-BED-HRAIL", "Head end rail", PartType.Rail, spA, new Dims(erLen, rw, rt), Axis.X, 'A', false, "Frame");
            CaseKit.Add(hrail, "HRAIL-1", CaseKit.B(ls, 21, railZ0, W - ls, 21 + rt, railZ1), Axis.X, Axis.Z, Axis.Y);
            var frail = CaseKit.Family(ctx, "F-BED-FRAIL", "Foot end rail", PartType.Rail, spA, new Dims(erLen, rw, rt), Axis.X, 'A', false, "Frame");
            CaseKit.Add(frail, "FRAIL-1", CaseKit.B(ls, Ltot - 21 - rt, railZ0, W - ls, Ltot - 21, railZ1), Axis.X, Axis.Z, Axis.Y);
            double srLen = Ltot - 2 * ls;
            var srail = CaseKit.Family(ctx, "F-BED-SRAIL", "Side rail", PartType.Rail, spA, new Dims(srLen, rw, rt), Axis.Y, 'A', false, "Frame");
            CaseKit.Add(srail, "SRAIL-1", CaseKit.B(0, ls, railZ0, rt, Ltot - ls, railZ1), Axis.Y, Axis.Z, Axis.X); CaseKit.Add(srail, "SRAIL-2", CaseKit.B(W - rt, ls, railZ0, W, Ltot - ls, railZ1), Axis.Y, Axis.Z, Axis.X);
            var trail = CaseKit.Family(ctx, "F-BED-TRAIL", "Headboard top rail", PartType.Rail, spA, new Dims(erLen, topW, rt), Axis.X, 'A', false, "Headboard");
            CaseKit.Add(trail, "TRAIL-1", CaseKit.B(ls, 21, hH - topW, W - ls, 21 + rt, hH), Axis.X, Axis.Z, Axis.Y);
            // floating panel: tongue 10 mm into grooves (rails above/below, legs at the sides), 2 mm free play
            double gz0 = railZ1, gz1 = hH - topW, tongue = 10;
            double panelX0 = ls - tongue + 2, panelX1 = W - ls + tongue - 2, panelZ0 = gz0 - tongue + 2, panelZ1 = gz1 + tongue - 2, py0 = 35 - pT / 2;
            var panel = CaseKit.Family(ctx, "F-BED-PANEL", "Headboard panel", PartType.Panel, spA, new Dims(panelX1 - panelX0, panelZ1 - panelZ0, pT), Axis.X, 'A', true, "Headboard");
            panel.Floating = true;
            CaseKit.Add(panel, "PANEL-1", CaseKit.B(panelX0, py0, panelZ0, panelX1, py0 + pT, panelZ1), Axis.X, Axis.Z, Axis.Y);
            L.Families.AddRange(new[] { hleg, fleg, hrail, frail, srail, trail, panel });

            // ---- class C: ledgers, slats, beam, centre leg
            double yh = 49, yf = Ltot - 49;
            var ledger = CaseKit.Family(ctx, "F-BED-LEDGER", "Support ledger", PartType.Support, spC, new Dims(Ltot - 2 * ls, 42, 20), Axis.Y, 'C', false, "Slats");
            CaseKit.Add(ledger, "LEDGER-1", CaseKit.B(rt, ls, ledgerTop - 42, rt + 20, Ltot - ls, ledgerTop), Axis.Y, Axis.Z, Axis.X); CaseKit.Add(ledger, "LEDGER-2", CaseKit.B(W - rt - 20, ls, ledgerTop - 42, W - rt, Ltot - ls, ledgerTop), Axis.Y, Axis.Z, Axis.X);
            int ns = Math.Max(3, (int)Math.Floor(innerL / pitch)); double sp = innerL / ns;
            var slat = CaseKit.Family(ctx, "F-BED-SLAT", "Slat", PartType.Support, spC, new Dims(innerW, slatH, slatW), Axis.X, 'C', false, "Slats");
            for (int i = 0; i < ns; i++) { double yc = yh + sp * (i + 0.5); CaseKit.Add(slat, "SLAT-" + (i + 1), CaseKit.B(rt, yc - slatW / 2, ledgerTop, W - rt, yc + slatW / 2, slatTop), Axis.X, Axis.Z, Axis.Y); }
            double beamTop = ledgerTop, beamH = 70, beamW = 45;
            var beam = CaseKit.Family(ctx, "F-BED-BEAM", "Centre beam", PartType.Support, spC, new Dims(yf - yh - 6, beamH, beamW), Axis.Y, 'C', false, "Slats");
            CaseKit.Add(beam, "BEAM-1", CaseKit.B(W / 2 - beamW / 2, yh + 3, beamTop - beamH, W / 2 + beamW / 2, yf - 3, beamTop), Axis.Y, Axis.Z, Axis.X);
            var cleg = CaseKit.Family(ctx, "F-BED-CLEG", "Centre leg", PartType.Leg, spC, new Dims(beamTop - beamH, 45, 45), Axis.Z, 'C', false, "Slats");
            CaseKit.Add(cleg, "CLEG-1", CaseKit.B(W / 2 - 22.5, Ltot / 2 - 22.5, 0, W / 2 + 22.5, Ltot / 2 + 22.5, beamTop - beamH), Axis.Z, Axis.X, Axis.Y);
            L.Families.AddRange(new[] { ledger, slat, beam, cleg });

            // ---- glued joints (end rails, headboard top rail); side rails are knock-down
            void J(string type, string a, bool atStart, string b) => L.Requests.Add(new JointEngine.Request { JointTypeId = type, PartAId = a, PartBId = b, AAtStart = atStart });
            J(joint, "HRAIL-1", true, "HLEG-1"); J(joint, "HRAIL-1", false, "HLEG-2");
            J(joint, "FRAIL-1", true, "FLEG-1"); J(joint, "FRAIL-1", false, "FLEG-2");
            J(joint, "TRAIL-1", true, "HLEG-1"); J(joint, "TRAIL-1", false, "HLEG-2");

            // ---- panel grooves (Dado features) in the rails and legs
            double gc0 = 35 - pT / 2, gc1 = 35 + pT / 2, railC0 = gc0 - 21, railC1 = gc1 - 21;      // groove centred on the panel; rails start at y = 21
            L.Grooves.Add(("HRAIL-1", new Box3(new Vec3(0, rw - tongue, railC0), new Vec3(erLen, rw, railC1)), tongue, "Headboard panel groove"));
            L.Grooves.Add(("TRAIL-1", new Box3(new Vec3(0, 0, railC0), new Vec3(erLen, tongue, railC1)), tongue, "Headboard panel groove"));
            L.Grooves.Add(("HLEG-1", new Box3(new Vec3(gz0, ls - tongue, gc0), new Vec3(gz1, ls, gc1)), tongue, "Headboard panel groove"));
            L.Grooves.Add(("HLEG-2", new Box3(new Vec3(gz0, 0, gc0), new Vec3(gz1, tongue, gc1)), tongue, "Headboard panel groove"));

            // ---- hardware: 4 bed bolts (through the legs into the side rails), levelling feet
            void Bolt(string rail, double x, double yEnd, double yOuter, double dir, string leg) =>
                L.Installs.Add(new HardwareInstall { HardwareId = "BED-BOLT", HostPartId = rail, MatePartId = leg, Point = new Vec3(x, yEnd, railZ0 + rw / 2), Normal = new Vec3(0, dir, 0), AxisU = new Vec3(1, 0, 0), AxisV = new Vec3(0, 0, 1),
                    MatePoint = new Vec3(x, yOuter, railZ0 + rw / 2), MateNormal = new Vec3(0, dir, 0), MateAxisV = new Vec3(0, 0, 1) });
            Bolt("SRAIL-1", rt / 2, ls, 0, 1, "HLEG-1"); Bolt("SRAIL-2", W - rt / 2, ls, 0, 1, "HLEG-2");
            Bolt("SRAIL-1", rt / 2, Ltot - ls, Ltot, -1, "FLEG-1"); Bolt("SRAIL-2", W - rt / 2, Ltot - ls, Ltot, -1, "FLEG-2");
            foreach (var leg in new[] { "HLEG-1", "HLEG-2", "FLEG-1", "FLEG-2", "CLEG-1" })
                L.Installs.Add(new HardwareInstall { HardwareId = "FOOT-LEVEL", HostPartId = leg, MatePartId = "", Point = new Vec3(0, 0, 0), Normal = new Vec3(0, 0, 1), AxisU = new Vec3(1, 0, 0), AxisV = new Vec3(0, 1, 0), MatePoint = new Vec3(0, 0, 0), MateNormal = new Vec3(0, 0, 1), MateAxisV = new Vec3(0, 1, 0) });

            return new Boxed<Layout>(L, CaseKit.Fingerprint(L.Families, L.Requests, L.Sheets) + "|" + string.Join(";", L.Installs.Select(h => h.HardwareId + h.HostPartId + h.Point)) + "|" + string.Join(";", L.Grooves.Select(x => x.part + x.box)));
        }

        public FurnitureModel Assemble(DependencyGraph g, ProjectContext ctx)
        {
            var model = new FurnitureModel { TypeId = TypeId, Name = Name, Category = Category };
            var L = g.Get<Boxed<Layout>>("layout").Value;
            foreach (var fam in L.Families) model.Families.Add(TableDefinition.CloneFamily(fam));
            new JointEngine(ctx.Joints, ctx.Library).Apply(model, L.Requests);
            int gn = 0;
            foreach (var gr in L.Grooves)
            {
                var part = model.FindPart(gr.part);
                part.Features.Add(JointGeometry.Rect(part, gr.box, FeatureKind.Dado, gr.depth, gr.purpose, "ROUT-8", "GROOVE-PANEL-1", "GRV" + (++gn).ToString("000")));
            }
            var installs = L.Installs.Select(h => new HardwareInstall
            {
                HardwareId = h.HardwareId, HostPartId = h.HostPartId, MatePartId = h.MatePartId, Point = h.Point, Normal = h.Normal, AxisU = h.AxisU, AxisV = h.AxisV,
                MatePoint = h.MatePoint, MateNormal = h.MateNormal, MateAxisV = h.MateAxisV, Quantity = 1
            }).ToList();
            new HardwareInstaller(ctx.Library).Install(model, installs);
            BiscuitPlanner.Apply(model, g.Get<double>("biscuit.pitch"), g.Get<string>("edgeJoint"));
            return model;
        }
    }
}
