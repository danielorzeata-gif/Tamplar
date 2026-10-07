using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RhinoWood.Core.Domain;
using RhinoWood.Core.Geometry;
using RhinoWood.Core.Joinery;

namespace RhinoWood.Core.Furniture
{
    /// <summary>
    /// Biscuits between the strips of every edge-glued panel (physical rules): #20 (56x23x4) from 20 mm thickness, #10 (53x19x4) below; centred in the
    /// thickness (two rows from 45 mm); first/last biscuit centre ~60 mm from the ends; evenly spread at no more than the pitch, so the count follows the
    /// panel length. Slots are cut in both neighbouring strips (the slot box straddles the glue line); the biscuit solids go to model.Biscuits.
    /// </summary>
    public static class BiscuitPlanner
    {
        public static void Apply(FurnitureModel model, double pitch)
        {
            int k = 0, fn = 0;
            foreach (var fam in model.Families.Where(f => f.EdgeGlued))
            {
                int strips = Math.Max(1, fam.RoughPieces.Sum(r => r.CountPerPart));
                if (strips < 2) continue;
                foreach (var part in fam.Instances)
                {
                    var fin = part.Finished; double L = fin.Length, W = fin.Width, T = fin.Thickness;
                    bool big = T >= 20; double bl = big ? 56 : 53, bw = big ? 23 : 19; string size = big ? "#20" : "#10";
                    if (L < bl + 40) continue;
                    double m = Math.Min(60, Math.Max(bl / 2 + 15, L / 4)), span = Math.Max(0, L - 2 * m);
                    int nb = Math.Max(2, (int)Math.Floor(span / pitch + 1e-9) + 1);
                    int rows = T >= 45 ? 2 : 1;
                    double sw = W / strips;
                    for (int e = 1; e < strips; e++)
                        for (int i = 0; i < nb; i++)
                            for (int rw = 0; rw < rows; rw++)
                            {
                                double x = m + span * i / (nb - 1), y = e * sw, z = rows == 1 ? T / 2 : T * (rw + 1) / 3.0;
                                var body = part.LocalBoxToWorld(new Box3(new Vec3(x - bl / 2, y - bw / 2, z - 2), new Vec3(x + bl / 2, y + bw / 2, z + 2)));
                                var slotLocal = new Box3(new Vec3(x - bl / 2 - 1, y - bw / 2 - 0.5, z - 2.05), new Vec3(x + bl / 2 + 1, y + bw / 2 + 0.5, z + 2.05));
                                string id = "BSC-" + (++k).ToString("000", CultureInfo.InvariantCulture);
                                part.Features.Add(JointGeometry.Rect(part, slotLocal, FeatureKind.BiscuitSlot, slotLocal.Size.Y / 2, "Biscuit slot " + size + " (strip edge " + e + ")", "ROUT-SLOT-3", id, "BSL" + (++fn).ToString("000", CultureInfo.InvariantCulture)));
                                model.Biscuits.Add(new BiscuitInstance { Id = id, PartId = part.Id, Edge = e, Size = size, Box = body });
                            }
                }
            }
        }
    }
}
