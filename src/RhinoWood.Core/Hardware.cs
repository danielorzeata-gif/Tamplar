using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RhinoWood.Core.Domain;
using RhinoWood.Core.Geometry;
using RhinoWood.Core.Joinery;
using RhinoWood.Core.Libraries;

namespace RhinoWood.Core.HardwareSystem
{
    /// <summary>Turns a hardware placement into machining features on host and mate parts (holes, slots, pockets).</summary>
    public sealed class HardwareInstaller
    {
        private readonly WoodLibrary _lib;
        public HardwareInstaller(WoodLibrary lib) { _lib = lib; }

        public void Install(FurnitureModel model, IEnumerable<HardwareInstall> installs)
        {
            int n = 0;
            foreach (var hi in installs)
            {
                var hw = _lib.Hardware[hi.HardwareId];
                hi.Id = "HW" + (++n).ToString("000", CultureInfo.InvariantCulture);
                int k = 0;
                foreach (var e in hw.Pattern)
                {
                    bool host = e.Target == PatternTarget.Host;
                    var part = model.FindPart(host ? hi.HostPartId : hi.MatePartId);
                    if (part == null) continue;
                    var origin = host ? hi.Point : hi.MatePoint;
                    var normal = host ? hi.Normal : hi.MateNormal;
                    var u = hi.AxisU; var v = host ? hi.AxisV : hi.MateAxisV;
                    var pos = origin + u * e.U + v * e.V;
                    string fid = hi.Id + "." + (++k).ToString(CultureInfo.InvariantCulture);
                    Feature f;
                    string tool = ToolFor(hw, e);
                    if (e.Kind == FeatureKind.Slot || e.Kind == FeatureKind.Pocket || e.Kind == FeatureKind.RoutPocket || e.Kind == FeatureKind.ElongatedHole)
                    {
                        // ElongatedHole: SizeU along u, SizeV along v (both include the hole diameter)
                        double su = Math.Max(e.SizeU, e.Diameter), sv = Math.Max(e.SizeV, e.Diameter);
                        var p0 = pos - u * (su / 2) - v * (sv / 2);
                        var p1 = pos + u * (su / 2) + v * (sv / 2) + normal * e.Depth;
                        f = JointGeometry.Rect(part, part.WorldBoxToLocal(new Box3(p0, p1)), e.Kind, e.Depth, e.Purpose, tool, hi.Id, fid);
                        if (e.Kind == FeatureKind.ElongatedHole) { f.Diameter = e.Diameter; }
                    }
                    else
                        f = JointGeometry.Hole(part, pos, normal, e.Kind, e.Diameter, e.Depth, e.Purpose, tool, hi.Id, fid);
                    part.Features.Add(f);
                    hi.FeatureIds.Add(fid);
                }
                model.HardwareInstalls.Add(hi);
            }
        }

        private string ToolFor(HardwareItem hw, HolePatternEntry e)
        {
            if (e.Kind == FeatureKind.Slot) return "ROUT-SLOT-3";
            if (e.Kind == FeatureKind.ElongatedHole) return "ROUT-8";
            var t = _lib.SelectTool(OperationType.Drill, e.Diameter);
            return t?.Id;
        }
    }
}
