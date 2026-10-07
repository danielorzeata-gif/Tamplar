using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RhinoWood.Core.Domain;
using RhinoWood.Core.Geometry;
using RhinoWood.Core.Libraries;

namespace RhinoWood.Core.Joinery
{
    public sealed class JointParam
    {
        public string Name { get; set; }
        public double Default { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public string Unit { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// End-to-face joint context: part A's end (at local x=0 when <see cref="AAtStart"/>, else at x=Length)
    /// meets part B. A's bounds may overlap B by the tenon length (parts already include tenons).
    /// </summary>
    public sealed class JointContext
    {
        public string JointId { get; set; }
        public PartInstance A { get; set; }
        public PartInstance B { get; set; }
        public bool AAtStart { get; set; }
        public Dictionary<string, double> P { get; set; } = new Dictionary<string, double>();
        public WoodLibrary Library { get; set; }
        public double Get(string k, double def) => P.TryGetValue(k, out var v) ? v : def;
    }

    public sealed class JointResult
    {
        public List<Feature> Features { get; } = new List<Feature>();
        public List<Issue> Issues { get; } = new List<Issue>();
        public Dictionary<string, double> Resolved { get; } = new Dictionary<string, double>();
        public string Description { get; set; }
    }

    public interface IJointDefinition
    {
        string Id { get; }
        string Name { get; }
        string Documentation { get; }
        IReadOnlyList<JointParam> Parameters { get; }
        /// <summary>Species/material constraints, e.g. minimum thickness of the thinner part.</summary>
        double MinPartThickness { get; }
        JointResult Generate(JointContext ctx);
    }

    public sealed class MortiseTenonParams
    {
        public double TenonThickness, TenonWidth, TenonLength, Shoulder, MortiseDepth, Clearance;
    }

    internal sealed class FeatureIds
    {
        private int _n;
        private readonly string _prefix;
        public FeatureIds(string prefix) { _prefix = prefix; }
        public string Next(string partId) => _prefix + "." + partId + "." + (++_n).ToString(CultureInfo.InvariantCulture);
    }

    public static class JointGeometry
    {
        public static Vec3 IntoDirection(JointContext c) => Vec3.Unit(c.A.LengthAxis, c.AAtStart ? -1 : 1);

        /// <summary>World coordinate of A's end plane along its length axis.</summary>
        public static double EndPlane(JointContext c) =>
            c.AAtStart ? c.A.Bounds.Min.Get(c.A.LengthAxis) : c.A.Bounds.Max.Get(c.A.LengthAxis);

        /// <summary>Local box in A covering the end zone [0..depth] (or [len-depth..len]) with cross-section inset.</summary>
        public static Box3 AEndBox(PartInstance a, bool atStart, double depth, double insetW, double thicknessSize)
        {
            double len = a.Finished.Length, w = a.Finished.Width, t = a.Finished.Thickness;
            double x0 = atStart ? 0 : len - depth, x1 = atStart ? depth : len;
            double z0 = (t - thicknessSize) / 2;
            return new Box3(new Vec3(x0, insetW, z0), new Vec3(x1, w - insetW, z0 + thicknessSize));
        }

        public static Feature Hole(PartInstance p, Vec3 worldPos, Vec3 worldDir, FeatureKind kind, double dia, double depth,
            string purpose, string tool, string src, string id)
        {
            return new Feature
            {
                Id = id, PartId = p.Id, Kind = kind, Position = p.WorldToLocal(worldPos),
                Direction = p.WorldDirToLocal(worldDir).Normalized(), Diameter = dia, Depth = depth, Purpose = purpose, ToolId = tool, SourceId = src, Tolerance = 0.2
            };
        }

        public static Feature Rect(PartInstance p, Box3 localBox, FeatureKind kind, double depth, string purpose, string tool, string src, string id, double angle = 0)
        {
            return new Feature { Id = id, PartId = p.Id, Kind = kind, HasBox = true, Box = localBox, Depth = depth, Purpose = purpose, ToolId = tool, SourceId = src, Tolerance = 0.2, Angle = angle };
        }
    }

    public abstract class JointBase : IJointDefinition
    {
        public abstract string Id { get; }
        public abstract string Name { get; }
        public abstract string Documentation { get; }
        public abstract IReadOnlyList<JointParam> Parameters { get; }
        public virtual double MinPartThickness => 12;
        public abstract JointResult Generate(JointContext ctx);

        protected static JointParam P(string n, double def, double min, double max, string unit, string desc) =>
            new JointParam { Name = n, Default = def, Min = min, Max = max, Unit = unit, Description = desc };
    }

    // ---------------------------------------------------------------- Mortise & tenon family
    public class MortiseTenonJoint : JointBase
    {
        public override string Id => "mortise-tenon";
        public override string Name => "Mortise & Tenon";
        public override string Documentation =>
            "Tenon thickness = 1/3 of rail thickness, tenon length = 1/2 of the receiving member (capped), shoulders on the rail edges. " +
            "Mortise is cut slightly deeper than the tenon to leave a glue pocket. Where two mortises meet in a corner leg the tenon ends are mitred.";
        public override IReadOnlyList<JointParam> Parameters { get; } = new[]
        {
            P("tenonThicknessRatio", 1.0 / 3, 0.25, 0.5, "ratio", "Tenon thickness / rail thickness"),
            P("tenonLengthRatio", 0.5, 0.3, 0.8, "ratio", "Tenon length / receiving member size"),
            P("tenonLengthMax", 50, 20, 120, "mm", "Upper bound for the tenon length"),
            P("shoulder", 10, 0, 40, "mm", "Shoulder on the rail edges"),
            P("depthClearance", 3, 0, 10, "mm", "Extra mortise depth (glue pocket)"),
            P("fitClearance", 0.2, 0, 1, "mm", "Per-side fit tolerance"),
        };

        /// <summary>Pure rule: tenon geometry from rail section and receiving member size (used before parts exist).</summary>
        public static MortiseTenonParams Compute(double railThickness, double railWidth, double receivingSize, IDictionary<string, double> p = null)
        {
            double Get(string k, double d) => p != null && p.TryGetValue(k, out var v) ? v : d;
            double tt = Math.Round(railThickness * Get("tenonThicknessRatio", 1.0 / 3));
            double tl = Math.Min(Math.Round(receivingSize * Get("tenonLengthRatio", 0.5)), Get("tenonLengthMax", 50));
            double sh = Get("shoulder", 10);
            return new MortiseTenonParams
            {
                TenonThickness = Math.Max(4, tt), TenonLength = tl, Shoulder = sh,
                TenonWidth = Math.Max(10, railWidth - 2 * sh), Clearance = Get("fitClearance", 0.2),
                MortiseDepth = tl + Get("depthClearance", 3)
            };
        }

        protected virtual bool Through => false;
        protected virtual bool OpenSlot => false;

        public override JointResult Generate(JointContext c)
        {
            var r = new JointResult();
            var ids = new FeatureIds(c.JointId);
            var a = c.A; var b = c.B;
            double railT = a.Finished.Thickness, railW = a.Finished.Width;
            var m = Compute(railT, railW, b.Finished.Thickness, c.P);
            if (OpenSlot) m.Shoulder = 0;
            if (OpenSlot) m.TenonWidth = railW;
            if (Through) m.TenonLength = Math.Max(m.TenonLength, c.Get("tenonLengthOverride", 0));
            r.Resolved["tenonThickness"] = m.TenonThickness; r.Resolved["tenonLength"] = m.TenonLength;
            r.Resolved["tenonWidth"] = m.TenonWidth; r.Resolved["mortiseDepth"] = m.MortiseDepth;

            var tenonLocal = JointGeometry.AEndBox(a, c.AAtStart, m.TenonLength, OpenSlot ? 0 : m.Shoulder, m.TenonThickness);
            r.Features.Add(JointGeometry.Rect(a, tenonLocal, FeatureKind.Tenon, m.TenonLength, "Tenon", "SAW-TS-300", c.JointId, ids.Next(a.Id)));

            // mortise = tenon footprint (+fit clearance) projected into B, starting at A's end plane
            var tw = a.LocalBoxToWorld(tenonLocal);
            var into = JointGeometry.IntoDirection(c);
            var ax = a.LengthAxis;
            double plane = JointGeometry.EndPlane(c);
            double deep = Through ? Math.Abs(b.Bounds.Size.Get(ax)) + 0 : m.MortiseDepth;
            // B face is TenonLength away from A's end plane towards A body (A bounds overlap B)
            double faceCoord = plane - into.Get(ax) * m.TenonLength;
            Vec3 lo = tw.Min, hi = tw.Max;
            lo = lo.With(ax, Math.Min(faceCoord, faceCoord + into.Get(ax) * deep));
            hi = hi.With(ax, Math.Max(faceCoord, faceCoord + into.Get(ax) * deep));
            var mw = new Box3(lo, hi);
            foreach (Axis x in new[] { Axis.X, Axis.Y, Axis.Z })
            {
                if (x == ax) continue;
                bool alongRailWidth = x == a.WidthAxis;
                if (OpenSlot && alongRailWidth)
                {
                    mw = new Box3(mw.Min.With(x, b.Bounds.Min.Get(x) - 1), mw.Max.With(x, b.Bounds.Max.Get(x) + 1));
                    continue;
                }
                mw = new Box3(mw.Min.With(x, mw.Min.Get(x) - m.Clearance), mw.Max.With(x, mw.Max.Get(x) + m.Clearance));
            }
            var tool = c.Library?.SelectTool(OperationType.Mortise, m.TenonThickness + 2 * m.Clearance);
            var mort = JointGeometry.Rect(b, b.WorldBoxToLocal(mw), OpenSlot ? FeatureKind.Slot : FeatureKind.Mortise, deep,
                Through ? "Through mortise" : "Mortise", tool?.Id ?? "MORT-8", c.JointId, ids.Next(b.Id));
            mort.IsThrough = Through;
            r.Features.Add(mort);

            double bAlong = b.Bounds.Size.Get(ax);
            if (!Through && m.MortiseDepth > bAlong - 10)
                r.Issues.Add(new Issue { Severity = Severity.Error, Code = "MORTISE_DEPTH", SubjectId = b.Id, Message = "Mortise depth " + m.MortiseDepth.ToString("0.#", CultureInfo.InvariantCulture) + " mm exceeds available material in " + b.Id + " (" + bAlong.ToString("0.#", CultureInfo.InvariantCulture) + " mm)." });
            r.Description = Name + ": tenon " + m.TenonThickness + "x" + m.TenonWidth + "x" + m.TenonLength + " mm";
            return r;
        }
    }

    public sealed class BridleJoint : MortiseTenonJoint
    {
        public override string Id => "bridle";
        public override string Name => "Bridle Joint";
        public override string Documentation => "Open mortise & tenon: slot cut through the end of the receiving member, full-width tongue on the rail.";
        protected override bool OpenSlot => true;
    }

    /// <summary>Japanese wedged through tenon (kusabi / tusk-style): parametric ratios, wedge slots cut into the tenon.</summary>
    public sealed class WedgedThroughTenonJoint : MortiseTenonJoint
    {
        public override string Id => "japanese-kusabi";
        public override string Name => "Japanese Wedged Through Tenon (Kusabi)";
        public override string Documentation =>
            "Through tenon protruding 1/4 of the receiving member; two wedge slots at 1/4 and 3/4 of the tenon width, slot depth 2/3 of the protrusion, " +
            "wedge slope 1:8. Grain of tenon and wedge must run along the rail; wedges are driven perpendicular to the mortise's long grain.";
        public override IReadOnlyList<JointParam> Parameters { get; } = new[]
        {
            P("wedgeSlope", 8, 4, 16, "1:n", "Wedge slope 1:n"),
            P("protrusionRatio", 0.25, 0.1, 0.5, "ratio", "Protrusion / receiving member size"),
            P("tenonThicknessRatio", 1.0 / 3, 0.25, 0.5, "ratio", "Tenon thickness ratio"),
            P("shoulder", 10, 0, 40, "mm", "Shoulder"),
        };
        protected override bool Through => true;

        public override JointResult Generate(JointContext c)
        {
            double bSize = c.B.Bounds.Size.Get(c.A.LengthAxis);
            c.P["tenonLengthOverride"] = bSize * (1 + c.Get("protrusionRatio", 0.25));
            var r = base.Generate(c);
            var ids = new FeatureIds(c.JointId + ".w");
            var tenon = r.Features.First(f => f.Kind == FeatureKind.Tenon);
            double prot = bSize * c.Get("protrusionRatio", 0.25);
            double slotDepth = prot * 2.0 / 3;
            double slope = c.Get("wedgeSlope", 8);
            double w = tenon.Box.Size.Y;
            foreach (double frac in new[] { 0.25, 0.75 })
            {
                double yc = tenon.Box.Min.Y + w * frac;
                double x0 = c.AAtStart ? tenon.Box.Min.X : tenon.Box.Max.X - slotDepth;
                double x1 = c.AAtStart ? tenon.Box.Min.X + slotDepth : tenon.Box.Max.X;
                var box = new Box3(new Vec3(x0, yc - 1.5, tenon.Box.Min.Z), new Vec3(x1, yc + 1.5, tenon.Box.Max.Z));
                r.Features.Add(JointGeometry.Rect(c.A, box, FeatureKind.WedgeSlot, slotDepth, "Wedge slot (slope 1:" + slope + ")", "SAW-TS-300", c.JointId, ids.Next(c.A.Id), Math.Atan(1.0 / slope) * 180 / Math.PI));
            }
            r.Description = Name + ": through tenon, protrusion " + prot.ToString("0.#", CultureInfo.InvariantCulture) + " mm, 2 wedges 1:" + slope;
            return r;
        }
    }

    public sealed class LooseTenonJoint : JointBase
    {
        public override string Id => "loose-tenon";
        public override string Name => "Loose Tenon (Domino-style)";
        public override string Documentation => "Matching mortises in both parts; a separate hardwood tenon (length = 2 x depth - 2 mm) is glued in.";
        public override IReadOnlyList<JointParam> Parameters { get; } = new[]
        {
            P("tenonThicknessRatio", 1.0 / 3, 0.25, 0.5, "ratio", "Thickness / rail thickness"),
            P("depth", 25, 10, 60, "mm", "Mortise depth per part"),
            P("shoulder", 10, 0, 40, "mm", "Edge shoulder"),
        };
        public override JointResult Generate(JointContext c)
        {
            var r = new JointResult(); var ids = new FeatureIds(c.JointId);
            double tt = Math.Max(4, Math.Round(c.A.Finished.Thickness * c.Get("tenonThicknessRatio", 1.0 / 3)));
            double depth = c.Get("depth", 25), sh = c.Get("shoulder", 10);
            var local = JointGeometry.AEndBox(c.A, c.AAtStart, depth, sh, tt);
            r.Features.Add(JointGeometry.Rect(c.A, local, FeatureKind.Mortise, depth, "Loose tenon mortise", "MORT-8", c.JointId, ids.Next(c.A.Id)));
            var w = c.A.LocalBoxToWorld(local);
            var into = JointGeometry.IntoDirection(c); var ax = c.A.LengthAxis;
            double plane = JointGeometry.EndPlane(c);
            var lo = w.Min.With(ax, Math.Min(plane, plane + into.Get(ax) * depth));
            var hi = w.Max.With(ax, Math.Max(plane, plane + into.Get(ax) * depth));
            r.Features.Add(JointGeometry.Rect(c.B, c.B.WorldBoxToLocal(new Box3(lo, hi)), FeatureKind.Mortise, depth, "Loose tenon mortise", "MORT-8", c.JointId, ids.Next(c.B.Id)));
            r.Resolved["looseTenonLength"] = 2 * depth - 2; r.Resolved["tenonThickness"] = tt;
            r.Description = Name + ": loose tenon " + tt + " x " + (w.Size.Get(c.A.WidthAxis)) + " x " + (2 * depth - 2);
            return r;
        }
    }

    // ---------------------------------------------------------------- Dowel / biscuit
    public sealed class DowelJoint : JointBase
    {
        public override string Id => "dowel";
        public override string Name => "Dowel";
        public override string Documentation => "Dowels along the end face: diameter = 1/3 rail thickness (rounded to 6/8/10), depth 25 mm per part, spacing 80 mm.";
        public override IReadOnlyList<JointParam> Parameters { get; } = new[]
        {
            P("diameter", 8, 6, 12, "mm", "Dowel diameter"), P("depth", 25, 15, 40, "mm", "Hole depth per part"), P("spacing", 80, 40, 200, "mm", "Dowel spacing"),
        };
        public override JointResult Generate(JointContext c)
        {
            var r = new JointResult(); var ids = new FeatureIds(c.JointId);
            double dia = c.Get("diameter", 8), depth = c.Get("depth", 25), sp = c.Get("spacing", 80);
            double w = c.A.Finished.Width;
            int n = Math.Max(1, (int)Math.Floor((w - 20) / sp) + 1);
            var into = JointGeometry.IntoDirection(c);
            double plane = JointGeometry.EndPlane(c);
            var tool = "DRILL-" + dia.ToString("0.#", CultureInfo.InvariantCulture);
            for (int i = 0; i < n; i++)
            {
                double y = n == 1 ? w / 2 : 10 + (w - 20) * i / (n - 1);
                var pa = c.A.LocalToWorld(new Vec3(c.AAtStart ? 0 : c.A.Finished.Length, y, c.A.Finished.Thickness / 2));
                r.Features.Add(JointGeometry.Hole(c.A, pa, into * -1, FeatureKind.DowelHole, dia, depth, "Dowel", tool, c.JointId, ids.Next(c.A.Id)));
                r.Features.Add(JointGeometry.Hole(c.B, pa, into, FeatureKind.DowelHole, dia, depth, "Dowel", tool, c.JointId, ids.Next(c.B.Id)));
            }
            r.Resolved["dowelCount"] = n;
            r.Description = Name + ": " + n + " x dia " + dia + " x " + (2 * depth - 4) + " mm";
            return r;
        }
    }

    public sealed class BiscuitJoint : JointBase
    {
        public override string Id => "biscuit";
        public override string Name => "Biscuit";
        public override string Documentation => "#20 biscuits (56 x 23 x 4 mm), slot depth 10 mm, one per 150 mm of width.";
        public override IReadOnlyList<JointParam> Parameters { get; } = new[] { P("pitch", 150, 80, 300, "mm", "Spacing between biscuits") };
        public override JointResult Generate(JointContext c)
        {
            var r = new JointResult(); var ids = new FeatureIds(c.JointId);
            double w = c.A.Finished.Width, t = c.A.Finished.Thickness;
            int n = Math.Max(1, (int)Math.Round(w / c.Get("pitch", 150)));
            var into = JointGeometry.IntoDirection(c); var ax = c.A.LengthAxis; double plane = JointGeometry.EndPlane(c);
            for (int i = 0; i < n; i++)
            {
                double yc = w * (i + 0.5) / n;
                double x0 = c.AAtStart ? 0 : c.A.Finished.Length - 10, x1 = c.AAtStart ? 10 : c.A.Finished.Length;
                var la = new Box3(new Vec3(x0, yc - 28, t / 2 - 2), new Vec3(x1, yc + 28, t / 2 + 2));
                r.Features.Add(JointGeometry.Rect(c.A, la, FeatureKind.BiscuitSlot, 10, "Biscuit slot", "ROUT-SLOT-3", c.JointId, ids.Next(c.A.Id)));
                var w0 = c.A.LocalBoxToWorld(la);
                var lo = w0.Min.With(ax, Math.Min(plane, plane + into.Get(ax) * 10)); var hi = w0.Max.With(ax, Math.Max(plane, plane + into.Get(ax) * 10));
                r.Features.Add(JointGeometry.Rect(c.B, c.B.WorldBoxToLocal(new Box3(lo, hi)), FeatureKind.BiscuitSlot, 10, "Biscuit slot", "ROUT-SLOT-3", c.JointId, ids.Next(c.B.Id)));
            }
            r.Description = Name + ": " + n + " x #20";
            return r;
        }
    }

    // ---------------------------------------------------------------- Dado / rabbet
    public class DadoJoint : JointBase
    {
        public override string Id => "dado";
        public override string Name => "Dado (Housing)";
        public override string Documentation => "Groove in the receiving member sized to the shelf thickness; depth = 1/3 of receiving thickness (max 12 mm).";
        public override IReadOnlyList<JointParam> Parameters { get; } = new[] { P("depthRatio", 1.0 / 3, 0.2, 0.5, "ratio", "Depth / receiving thickness"), P("fit", 0.2, 0, 1, "mm", "Clearance per side") };
        protected virtual FeatureKind Kind => FeatureKind.Dado;
        public override JointResult Generate(JointContext c)
        {
            var r = new JointResult(); var ids = new FeatureIds(c.JointId);
            var ax = c.A.LengthAxis; var into = JointGeometry.IntoDirection(c);
            double depth = Math.Min(12, c.B.Bounds.Size.Get(ax) * c.Get("depthRatio", 1.0 / 3));
            double plane = JointGeometry.EndPlane(c);
            var w = c.A.LocalBoxToWorld(JointGeometry.AEndBox(c.A, c.AAtStart, 1, 0, c.A.Finished.Thickness));
            double fit = c.Get("fit", 0.2);
            var lo = w.Min.With(ax, Math.Min(plane, plane + into.Get(ax) * depth)); var hi = w.Max.With(ax, Math.Max(plane, plane + into.Get(ax) * depth));
            var box = new Box3(lo, hi).Inflate(0);
            foreach (Axis x in new[] { Axis.X, Axis.Y, Axis.Z }) if (x != ax) box = new Box3(box.Min.With(x, box.Min.Get(x) - fit), box.Max.With(x, box.Max.Get(x) + fit));
            r.Features.Add(JointGeometry.Rect(c.B, c.B.WorldBoxToLocal(box), Kind, depth, Name, "ROUT-8", c.JointId, ids.Next(c.B.Id)));
            r.Resolved["depth"] = depth;
            r.Description = Name + ": depth " + depth.ToString("0.#", CultureInfo.InvariantCulture) + " mm";
            return r;
        }
    }
    public sealed class RabbetJoint : DadoJoint
    {
        public override string Id => "rabbet";
        public override string Name => "Rabbet";
        public override string Documentation => "Open step along the edge of the receiving member; depth = 1/2 thickness of the inserted part (max 12 mm).";
        protected override FeatureKind Kind => FeatureKind.Rabbet;
    }

    public sealed class HalfLapJoint : JointBase
    {
        public override string Id => "half-lap";
        public override string Name => "Half Lap";
        public override string Documentation => "Overlap of the two members is split: each loses half of the overlap thickness. Stack axis = axis with smallest overlap.";
        public override IReadOnlyList<JointParam> Parameters { get; } = new JointParam[0];
        public override JointResult Generate(JointContext c)
        {
            var r = new JointResult(); var ids = new FeatureIds(c.JointId);
            var o = new Box3(
                new Vec3(Math.Max(c.A.Bounds.Min.X, c.B.Bounds.Min.X), Math.Max(c.A.Bounds.Min.Y, c.B.Bounds.Min.Y), Math.Max(c.A.Bounds.Min.Z, c.B.Bounds.Min.Z)),
                new Vec3(Math.Min(c.A.Bounds.Max.X, c.B.Bounds.Max.X), Math.Min(c.A.Bounds.Max.Y, c.B.Bounds.Max.Y), Math.Min(c.A.Bounds.Max.Z, c.B.Bounds.Max.Z)));
            var sz = o.Size;
            if (sz.X <= 0 || sz.Y <= 0 || sz.Z <= 0) { r.Issues.Add(new Issue { Severity = Severity.Error, Code = "LAP_NO_OVERLAP", Message = "Half-lap parts do not overlap.", SubjectId = c.JointId }); return r; }
            Axis s = sz.X <= sz.Y && sz.X <= sz.Z ? Axis.X : sz.Y <= sz.Z ? Axis.Y : Axis.Z;
            double mid = (o.Min.Get(s) + o.Max.Get(s)) / 2;
            bool aBelow = c.A.Bounds.Center.Get(s) < c.B.Bounds.Center.Get(s);
            Box3 aBox = aBelow ? new Box3(o.Min.With(s, mid), o.Max) : new Box3(o.Min, o.Max.With(s, mid));
            Box3 bBox = aBelow ? new Box3(o.Min, o.Max.With(s, mid)) : new Box3(o.Min.With(s, mid), o.Max);
            r.Features.Add(JointGeometry.Rect(c.A, c.A.WorldBoxToLocal(aBox), FeatureKind.Lap, sz.Get(s) / 2, "Half lap", "ROUT-8", c.JointId, ids.Next(c.A.Id)));
            r.Features.Add(JointGeometry.Rect(c.B, c.B.WorldBoxToLocal(bBox), FeatureKind.Lap, sz.Get(s) / 2, "Half lap", "ROUT-8", c.JointId, ids.Next(c.B.Id)));
            r.Description = Name + ": lap depth " + (sz.Get(s) / 2).ToString("0.#", CultureInfo.InvariantCulture) + " mm";
            return r;
        }
    }

    // ---------------------------------------------------------------- Finger / box / dovetail / scarf
    public class FingerJoint : JointBase
    {
        public override string Id => "finger";
        public override string Name => "Finger Joint";
        public override string Documentation => "Interlocking rectangular fingers along the end of part A; pitch = param (default 12 mm); finger length = thickness of part B.";
        public override IReadOnlyList<JointParam> Parameters { get; } = new[] { P("pitch", 12, 6, 40, "mm", "Finger width") };
        protected virtual FeatureKind Kind => FeatureKind.Finger;
        protected virtual double PitchFor(JointContext c) => c.Get("pitch", 12);
        protected virtual double AngleDeg(JointContext c) => 0;
        public override JointResult Generate(JointContext c)
        {
            var r = new JointResult(); var ids = new FeatureIds(c.JointId);
            double w = c.A.Finished.Width, t = c.A.Finished.Thickness;
            double depth = c.B.Bounds.Size.Get(c.A.LengthAxis);
            double pitch = PitchFor(c);
            int n = Math.Max(3, (int)Math.Round(w / pitch)); if (n % 2 == 0) n++;
            double fw = w / n;
            var into = JointGeometry.IntoDirection(c); var ax = c.A.LengthAxis; double plane = JointGeometry.EndPlane(c);
            for (int i = 0; i < n; i++)
            {
                bool aRemoves = i % 2 == 1; // A has fingers at even indices; removes material at odd
                var part = aRemoves ? c.A : c.B;
                double y0 = i * fw, y1 = (i + 1) * fw;
                double x0 = c.AAtStart ? 0 : c.A.Finished.Length - depth, x1 = c.AAtStart ? depth : c.A.Finished.Length;
                var la = new Box3(new Vec3(x0, y0, 0), new Vec3(x1, y1, t));
                if (aRemoves) r.Features.Add(JointGeometry.Rect(c.A, la, Kind, depth, Name + " socket", "SAW-TS-300", c.JointId, ids.Next(c.A.Id), AngleDeg(c)));
                else
                {
                    var w0 = c.A.LocalBoxToWorld(la);
                    var lo = w0.Min.With(ax, Math.Min(plane, plane + into.Get(ax) * t)); var hi = w0.Max.With(ax, Math.Max(plane, plane + into.Get(ax) * t));
                    r.Features.Add(JointGeometry.Rect(c.B, c.B.WorldBoxToLocal(new Box3(lo, hi)), Kind, t, Name + " socket", "SAW-TS-300", c.JointId, ids.Next(c.B.Id), AngleDeg(c)));
                }
            }
            r.Resolved["fingerCount"] = n; r.Resolved["fingerWidth"] = fw;
            r.Description = Name + ": " + n + " fingers x " + fw.ToString("0.##", CultureInfo.InvariantCulture) + " mm";
            return r;
        }
    }
    public sealed class BoxJoint : FingerJoint
    {
        public override string Id => "box-joint";
        public override string Name => "Box Joint";
        public override string Documentation => "Corner joint with finger width = part thickness (classic box joint).";
        protected override double PitchFor(JointContext c) => c.A.Finished.Thickness;
    }
    public sealed class DovetailJoint : FingerJoint
    {
        public override string Id => "dovetail";
        public override string Name => "Through Dovetail";
        public override string Documentation => "Tail pitch = 2 x thickness; slope 1:6 (softwood) or 1:8 (hardwood) stored as Angle on the features.";
        public override IReadOnlyList<JointParam> Parameters { get; } = new[] { P("slope", 8, 5, 10, "1:n", "Dovetail slope 1:n") };
        protected override FeatureKind Kind => FeatureKind.Dovetail;
        protected override double PitchFor(JointContext c) => 2 * c.A.Finished.Thickness;
        protected override double AngleDeg(JointContext c) => Math.Atan(1.0 / c.Get("slope", 8)) * 180 / Math.PI;
    }

    public sealed class ScarfJoint : JointBase
    {
        public override string Id => "scarf";
        public override string Name => "Scarf Joint";
        public override string Documentation => "End-to-end splice; bevel length = slope ratio x thickness (default 1:8).";
        public override IReadOnlyList<JointParam> Parameters { get; } = new[] { P("slope", 8, 4, 16, "1:n", "Scarf slope 1:n") };
        public override JointResult Generate(JointContext c)
        {
            var r = new JointResult(); var ids = new FeatureIds(c.JointId);
            double t = c.A.Finished.Thickness, w = c.A.Finished.Width, slope = c.Get("slope", 8);
            double len = t * slope;
            double x0 = c.AAtStart ? 0 : c.A.Finished.Length - len, x1 = c.AAtStart ? len : c.A.Finished.Length;
            double ang = Math.Atan(1.0 / slope) * 180 / Math.PI;
            r.Features.Add(JointGeometry.Rect(c.A, new Box3(new Vec3(x0, 0, 0), new Vec3(x1, w, t)), FeatureKind.Scarf, len, "Scarf bevel", "SAW-TS-300", c.JointId, ids.Next(c.A.Id), ang));
            double bx0 = c.B.Finished.Length - len, bx1 = c.B.Finished.Length;
            r.Features.Add(JointGeometry.Rect(c.B, new Box3(new Vec3(c.AAtStart ? bx0 : 0, 0, 0), new Vec3(c.AAtStart ? bx1 : len, c.B.Finished.Width, c.B.Finished.Thickness)), FeatureKind.Scarf, len, "Scarf bevel", "SAW-TS-300", c.JointId, ids.Next(c.B.Id), ang));
            r.Resolved["scarfLength"] = len;
            r.Description = Name + ": 1:" + slope + ", length " + len.ToString("0.#", CultureInfo.InvariantCulture) + " mm";
            return r;
        }
    }

    /// <summary>User defined joint: templates of end-zone features expressed as fractions, no code required.</summary>
    public sealed class CustomJointTemplate
    {
        public string Target { get; set; } = "A";          // A or B
        public FeatureKind Kind { get; set; } = FeatureKind.Pocket;
        public double WidthFraction { get; set; } = 1;
        public double ThicknessFraction { get; set; } = 0.5;
        public double Depth { get; set; } = 20;
        public string Purpose { get; set; } = "Custom feature";
        public string ToolId { get; set; } = "ROUT-8";
    }

    public sealed class CustomJoint : JointBase
    {
        private readonly string _id, _name; private readonly List<CustomJointTemplate> _tpl;
        public CustomJoint(string id, string name, IEnumerable<CustomJointTemplate> templates) { _id = id; _name = name; _tpl = templates.ToList(); }
        public override string Id => _id;
        public override string Name => _name;
        public override string Documentation => "User-defined joint built from " + _tpl.Count + " feature template(s).";
        public override IReadOnlyList<JointParam> Parameters { get; } = new JointParam[0];
        public override JointResult Generate(JointContext c)
        {
            var r = new JointResult(); var ids = new FeatureIds(c.JointId);
            foreach (var t in _tpl)
            {
                var p = t.Target == "B" ? c.B : c.A;
                double w = p.Finished.Width * t.WidthFraction, th = p.Finished.Thickness * t.ThicknessFraction;
                bool atStart = t.Target == "B" ? !c.AAtStart : c.AAtStart;
                var lb = JointGeometry.AEndBox(p, atStart, t.Depth, (p.Finished.Width - w) / 2, th);
                r.Features.Add(JointGeometry.Rect(p, lb, t.Kind, t.Depth, t.Purpose, t.ToolId, c.JointId, ids.Next(p.Id)));
            }
            r.Description = Name;
            return r;
        }
    }

    public sealed class JointRegistry
    {
        private readonly Dictionary<string, IJointDefinition> _defs = new Dictionary<string, IJointDefinition>();
        public IEnumerable<IJointDefinition> All => _defs.Values;
        public void Register(IJointDefinition d) => _defs[d.Id] = d;
        public IJointDefinition Get(string id) => _defs.TryGetValue(id, out var d) ? d : throw new KeyNotFoundException("Unknown joint type " + id);
        public bool Has(string id) => _defs.ContainsKey(id);

        public static JointRegistry CreateDefault()
        {
            var r = new JointRegistry();
            foreach (var d in new IJointDefinition[]
            {
                new MortiseTenonJoint(), new LooseTenonJoint(), new DowelJoint(), new BiscuitJoint(), new DovetailJoint(),
                new FingerJoint(), new BoxJoint(), new DadoJoint(), new RabbetJoint(), new HalfLapJoint(), new BridleJoint(),
                new ScarfJoint(), new WedgedThroughTenonJoint()
            }) r.Register(d);
            return r;
        }
    }

    /// <summary>Applies joint instances to a furniture model: generates features, validates, mitres colliding tenons.</summary>
    public sealed class JointEngine
    {
        private readonly JointRegistry _reg; private readonly WoodLibrary _lib;
        public JointEngine(JointRegistry reg, WoodLibrary lib) { _reg = reg; _lib = lib; }

        public sealed class Request
        {
            public string JointTypeId; public string PartAId; public string PartBId; public bool AAtStart;
            public Dictionary<string, double> Params = new Dictionary<string, double>();
        }

        public void Apply(FurnitureModel model, IEnumerable<Request> requests)
        {
            int n = 0;
            var tenonOf = new Dictionary<string, Feature>();
            foreach (var rq in requests)
            {
                var def = _reg.Get(rq.JointTypeId);
                var a = model.FindPart(rq.PartAId); var b = model.FindPart(rq.PartBId);
                var id = "J" + (++n).ToString("000", CultureInfo.InvariantCulture);
                var ctx = new JointContext { JointId = id, A = a, B = b, AAtStart = rq.AAtStart, P = new Dictionary<string, double>(rq.Params), Library = _lib };
                var res = def.Generate(ctx);
                var ji = new JointInstance { Id = id, JointTypeId = def.Id, PartAId = a.Id, PartBId = b.Id, Description = res.Description, Parameters = new Dictionary<string, double>(res.Resolved) };
                foreach (var f in res.Features)
                {
                    model.FindPart(f.PartId).Features.Add(f);
                    ji.FeatureIds.Add(f.Id);
                }
                if (Math.Min(a.Finished.Thickness, b.Finished.Thickness) < def.MinPartThickness)
                    model.Issues.Add(new Issue { Severity = Severity.Warning, Code = "JOINT_THIN", SubjectId = id, Message = def.Name + " between very thin parts (" + a.Id + "/" + b.Id + ")." });
                model.Issues.AddRange(res.Issues);
                model.Joints.Add(ji);
            }
            CheckMortiseCollisions(model);
        }

        private static void CheckMortiseCollisions(FurnitureModel model)
        {
            foreach (var part in model.AllParts)
            {
                var morts = part.Features.Where(f => f.HasBox && (f.Kind == FeatureKind.Mortise || f.Kind == FeatureKind.Slot)).ToList();
                for (int i = 0; i < morts.Count; i++)
                    for (int j = i + 1; j < morts.Count; j++)
                    {
                        if (morts[i].SourceId == morts[j].SourceId) continue;
                        if (!morts[i].Box.Intersects(morts[j].Box)) continue;
                        foreach (var src in new[] { morts[i].SourceId, morts[j].SourceId })
                        {
                            var ji = model.Joints.First(x => x.Id == src);
                            if (ji.Mitred) continue;
                            ji.Mitred = true;
                            foreach (var t in model.FindPart(ji.PartAId).Features.Where(f => f.SourceId == src && f.Kind == FeatureKind.Tenon)) t.Angle = 45;
                        }
                        model.Issues.Add(new Issue { Severity = Severity.Info, Code = "TENON_MITRED", SubjectId = part.Id, Message = "Mortises meet inside " + part.Id + "; tenon ends are mitred at 45 degrees." });
                    }
            }
        }
    }
}
