using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RhinoWood.Core.Domain;
using RhinoWood.Core.Libraries;
using RhinoWood.Core.Optimization;
using RhinoWood.Core.Rules;

namespace RhinoWood.Core.Manufacturing
{
    public sealed class Operation
    {
        public string Id { get; set; }
        public int Sequence { get; set; }
        public string PartId { get; set; }
        public OperationType Type { get; set; }
        public string Description { get; set; }
        public string ToolId { get; set; }
        public string Machine { get; set; }
        public double Minutes { get; set; }
        public string FeatureId { get; set; }
        public string SourceId { get; set; }   // joint or hardware id
        public string BoardId { get; set; }
        public string CutId { get; set; }
    }

    /// <summary>Full back-trace: finished part -> purchase -> board -> cut -> operations -> joinery/hardware -> assembly.</summary>
    public sealed class PartTrace
    {
        public string PartId { get; set; }
        public string FamilyId { get; set; }
        public string Assembly { get; set; }
        public List<string> PurchaseLineIds { get; set; } = new List<string>();
        public List<string> BoardIds { get; set; } = new List<string>();
        public List<string> CutIds { get; set; } = new List<string>();
        public List<string> OperationIds { get; set; } = new List<string>();
        public List<string> JointIds { get; set; } = new List<string>();
        public List<string> HardwareIds { get; set; } = new List<string>();
    }

    public sealed class ManufacturingPlan
    {
        public List<Operation> Operations { get; set; } = new List<Operation>();
        public Dictionary<string, PartTrace> Traces { get; set; } = new Dictionary<string, PartTrace>();
        public List<string> AssemblySequence { get; set; } = new List<string>();
        public double TotalMinutes => Operations.Sum(o => o.Minutes) + AssemblyMinutes;
        public double AssemblyMinutes { get; set; }
    }

    public sealed class ManufacturingPlanner
    {
        private readonly WoodLibrary _lib; private readonly ManufacturingRules _rules;
        public ManufacturingPlanner(WoodLibrary lib, ManufacturingRules rules) { _lib = lib; _rules = rules; }

        public ManufacturingPlan Plan(FurnitureModel model, OptimizationResult opt)
        {
            var plan = new ManufacturingPlan();
            int opN = 0;
            var cutsByPart = opt.Boards.SelectMany(b => b.Cuts.Select(c => (b, c))).GroupBy(x => x.c.Demand.PartId).ToDictionary(g => g.Key, g => g.ToList());

            Operation Add(PartInstance p, OperationType t, string desc, double min, string machine, string tool = null, Feature f = null, string board = null, string cut = null, string src = null)
            {
                var o = new Operation { Id = "OP" + (++opN).ToString("000", CultureInfo.InvariantCulture), Sequence = plan.Operations.Count(x => x.PartId == p.Id) + 1, PartId = p.Id, Type = t, Description = desc, Minutes = min, Machine = machine, ToolId = tool, FeatureId = f?.Id, SourceId = src ?? f?.SourceId, BoardId = board, CutId = cut };
                plan.Operations.Add(o);
                plan.Traces[p.Id].OperationIds.Add(o.Id);
                return o;
            }

            foreach (var fam in model.Families)
                foreach (var part in fam.Instances)
                {
                    var tr = new PartTrace { PartId = part.Id, FamilyId = fam.Id, Assembly = fam.Assembly };
                    plan.Traces[part.Id] = tr;
                    tr.JointIds.AddRange(model.Joints.Where(j => j.PartAId == part.Id || j.PartBId == part.Id).Select(j => j.Id));
                    tr.HardwareIds.AddRange(model.HardwareInstalls.Where(h => h.HostPartId == part.Id || h.MatePartId == part.Id).Select(h => h.Id));

                    if (cutsByPart.TryGetValue(part.Id, out var cuts))
                        foreach (var (b, c) in cuts)
                        {
                            tr.BoardIds.Add(b.Id); tr.CutIds.Add(c.CutId);
                            var pl = opt.Purchase.FirstOrDefault(x => x.BoardIds.Contains(b.Id));
                            if (pl != null && !tr.PurchaseLineIds.Contains(pl.Id)) tr.PurchaseLineIds.Add(pl.Id);
                            Add(part, OperationType.Cut, string.Format(CultureInfo.InvariantCulture, "Cut rough piece {0:0.#} mm from board {1} ({2}) at {3:0.#} mm [{4}]", c.Length, b.Id, b.Item.Label + " x " + b.Length, c.Start, c.CutId), 4, "Crosscut saw", "SAW-TS-300", board: b.Id, cut: c.CutId);
                        }

                    bool multi = fam.RoughPieces.Sum(r => r.CountPerPart) > 1;
                    var rough = fam.RoughPieces.First().Rough;
                    if (multi)
                    {
                        Add(part, OperationType.Plane, "Joint one face and one edge of every strip", 6 * fam.RoughPieces.Sum(r => r.CountPerPart), "Jointer", "PLANER-HSS");
                        Add(part, OperationType.Rip, "Rip strips to equal width and joint the glue edges", 3 * fam.RoughPieces.Sum(r => r.CountPerPart), "Table saw", "SAW-TS-RIP");
                        Add(part, OperationType.Glue, "Edge-glue " + fam.RoughPieces.Sum(r => r.CountPerPart) + " strips into a panel (alternate ring orientation)", 20 + 4 * fam.RoughPieces.Sum(r => r.CountPerPart), "Clamps");
                        Add(part, OperationType.Plane, "Flatten and plane panel to " + fam.Finished.Thickness.ToString("0.#", CultureInfo.InvariantCulture) + " mm", 25, "Planer / sander", "PLANER-HSS");
                        Add(part, OperationType.Crosscut, "Trim panel to " + fam.Finished.Length.ToString("0.#", CultureInfo.InvariantCulture) + " x " + fam.Finished.Width.ToString("0.#", CultureInfo.InvariantCulture), 10, "Crosscut saw", "SAW-TS-300");
                    }
                    else
                    {
                        Add(part, OperationType.Plane, "Joint face and edge (reference surfaces)", 6, "Jointer", "PLANER-HSS");
                        Add(part, OperationType.Plane, "Plane to thickness " + fam.Finished.Thickness.ToString("0.#", CultureInfo.InvariantCulture) + " mm", 4, "Thicknesser", "PLANER-HSS");
                        if (rough.Width - fam.Finished.Width > 0.5) Add(part, OperationType.Rip, "Rip to width " + fam.Finished.Width.ToString("0.#", CultureInfo.InvariantCulture) + " mm", 3, "Table saw", "SAW-TS-RIP");
                        Add(part, OperationType.Crosscut, "Crosscut to finished length " + fam.Finished.Length.ToString("0.#", CultureInfo.InvariantCulture) + " mm", 3, "Crosscut saw", "SAW-TS-300");
                    }

                    foreach (var f in part.Features.OrderBy(x => x.Kind))
                    {
                        var (t, machine, minutes) = Classify(f);
                        Add(part, t, f.Purpose + " - " + f.Describe(), minutes, machine, f.ToolId, f);
                    }
                    Add(part, OperationType.Sand, "Sand to P180", 8, "Random orbital sander");
                }

            // assembly sequence (generic: joints in order, then hardware, then finish)
            int step = 0; double asmMin = 0;
            plan.AssemblySequence.Add(string.Format("{0}. Dry-fit all parts without glue and check squareness.", ++step)); asmMin += 15;
            foreach (var fam in model.Families.Where(f => f.RoughPieces.Sum(r => r.CountPerPart) > 1))
            { plan.AssemblySequence.Add(string.Format("{0}. Glue up {1} (see part operations).", ++step, fam.Name)); }
            foreach (var j in model.Joints)
            {
                plan.AssemblySequence.Add(string.Format("{0}. Glue and clamp joint {1}: {2} -> {3} ({4}){5}", ++step, j.Id, j.PartAId, j.PartBId, j.Description, j.Mitred ? " [mitred tenon ends]" : ""));
                asmMin += 6;
            }
            foreach (var h in model.HardwareInstalls.GroupBy(x => x.HardwareId))
            { plan.AssemblySequence.Add(string.Format("{0}. Install {1} x {2} (see hardware installation details).", ++step, h.Count(), h.Key)); asmMin += 2 * h.Count(); }
            plan.AssemblySequence.Add(string.Format("{0}. Finish (oil/lacquer) and cure; fit top with fasteners last.", ++step)); asmMin += 30;
            plan.AssemblyMinutes = asmMin;
            return plan;
        }

        private static (OperationType, string, double) Classify(Feature f)
        {
            switch (f.Kind)
            {
                case FeatureKind.Mortise: return (OperationType.Mortise, "Hollow-chisel mortiser", 5);
                case FeatureKind.Tenon: return (OperationType.Tenon, "Table saw / tenoning jig", 6);
                case FeatureKind.BiscuitSlot: return (OperationType.Slot, "Biscuit joiner", 0.6);
                case FeatureKind.Slot: case FeatureKind.WedgeSlot: return (OperationType.Slot, "Router / slot cutter", f.HasBox && f.Box.Size.X > 300 ? 4 : 3);
                case FeatureKind.DowelHole: case FeatureKind.ScrewHole: case FeatureKind.ShelfPin: case FeatureKind.ConnectorHole: return (OperationType.Drill, "Doweling jig / drill press", 0.5);
                case FeatureKind.HingeCup: case FeatureKind.Counterbore: case FeatureKind.Countersink: return (OperationType.Drill, "Drill press (Forstner)", 1.5);
                case FeatureKind.ElongatedHole: return (OperationType.Rout, "Router", 3);
                case FeatureKind.Dado: case FeatureKind.Rabbet: case FeatureKind.Lap: case FeatureKind.Pocket: case FeatureKind.RoutPocket: case FeatureKind.Finger: case FeatureKind.Dovetail: case FeatureKind.Scarf: return (OperationType.Rout, "Router table", 5);
                default: return (OperationType.Drill, "Drill press", 0.8);
            }
        }
    }
}
