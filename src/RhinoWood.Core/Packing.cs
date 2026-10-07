using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using RhinoWood.Core.Domain;
using RhinoWood.Core.Geometry;
using RhinoWood.Core.Libraries;
using RhinoWood.Core.Projects;
using RhinoWood.Core.Reports;

namespace RhinoWood.Core.Logistics
{
    /// <summary>A shipping carton (inner dimensions, mm). Standard = ready-made retail size; custom = made to order (long boxes for rails, slats, doors).</summary>
    public sealed class CartonSpec
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double L { get; set; }
        public double W { get; set; }
        public double H { get; set; }
        public bool Standard { get; set; }
        public string Source { get; set; }
        public double Volume => L * W * H;
    }

    /// <summary>A pallet footprint. Deck height is the pallet itself (EPAL: 144 mm).</summary>
    public sealed class PalletSpec
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double L { get; set; }
        public double W { get; set; }
        public double Deck { get; set; } = 144;
        public double MaxKg { get; set; } = 1500;
        public string Source { get; set; }
        public double Area => L * W;
    }

    public static class PackingCatalog
    {
        // Retail sizes (inner mm) seen in carton shops: FEFCO 0201 regular slotted case / 0203 full overlap, double-wall BC [REF, vendor listings].
        public static readonly IReadOnlyList<CartonSpec> Cartons = new List<CartonSpec>
        {
            new CartonSpec { Id = "C-590x450x210", Name = "FEFCO 0201 590×450×210", L = 590, W = 450, H = 210, Standard = true, Source = "[REF] listă comercială" },
            new CartonSpec { Id = "C-500x400x300", Name = "FEFCO 0201 500×400×300", L = 500, W = 400, H = 300, Standard = true, Source = "[REF] listă comercială" },
            new CartonSpec { Id = "C-620x540x540", Name = "FEFCO 0201 620×540×540", L = 620, W = 540, H = 540, Standard = true, Source = "[REF] listă comercială" },
            new CartonSpec { Id = "C-990x590x437", Name = "FEFCO 0201 990×590×437", L = 990, W = 590, H = 437, Standard = true, Source = "[REF] listă comercială" },
            new CartonSpec { Id = "C-1280x390x180", Name = "FEFCO 0203 1280×390×180", L = 1280, W = 390, H = 180, Standard = true, Source = "[REF] listă comercială" },
            new CartonSpec { Id = "C-1286x286x279", Name = "FEFCO 0201 1286×286×279", L = 1286, W = 286, H = 279, Standard = true, Source = "[REF] listă comercială" },
            // Made to order (telescopic / full overlap): long parts. 1750 = DPD length limit, 2000 = GLS length limit [REF carrier sites].
            new CartonSpec { Id = "C-1750x300x200", Name = "FEFCO 0203 la comandă 1750×300×200", L = 1750, W = 300, H = 200, Source = "[ESTIMARE] limită DPD 1750" },
            new CartonSpec { Id = "C-1750x450x300", Name = "FEFCO 0203 la comandă 1750×450×300", L = 1750, W = 450, H = 300, Source = "[ESTIMARE] limită DPD 1750" },
            new CartonSpec { Id = "C-2000x400x250", Name = "FEFCO 0203 la comandă 2000×400×250", L = 2000, W = 400, H = 250, Source = "[ESTIMARE] limită GLS 2000" },
            new CartonSpec { Id = "C-2000x600x350", Name = "FEFCO 0203 la comandă 2000×600×350", L = 2000, W = 600, H = 350, Source = "[ESTIMARE] limită GLS 2000" },
            new CartonSpec { Id = "C-2250x500x350", Name = "FEFCO 0203 la comandă 2250×500×350", L = 2250, W = 500, H = 350, Source = "[ESTIMARE]" },
            new CartonSpec { Id = "C-2250x700x450", Name = "FEFCO 0203 la comandă 2250×700×450", L = 2250, W = 700, H = 450, Source = "[ESTIMARE]" },
        };

        public static readonly IReadOnlyList<PalletSpec> Pallets = new List<PalletSpec>
        {
            new PalletSpec { Id = "EPAL1", Name = "EUR/EPAL 1 · 1200×800", L = 1200, W = 800, Deck = 144, MaxKg = 1500, Source = "[V-DATA] EPAL: 1200×800×144, 1500 kg, stivuire max 5500 kg" },
            new PalletSpec { Id = "EPAL3", Name = "EPAL 3 · 1200×1000", L = 1200, W = 1000, Deck = 144, MaxKg = 1500, Source = "[V-DATA] EPAL 3: 1000×1200×144, 1500 kg" },
            new PalletSpec { Id = "LONG2000", Name = "Palet lung · 2000×1200 (la comandă)", L = 2000, W = 1200, Deck = 144, MaxKg = 1500, Source = "[ESTIMARE]" },
            new PalletSpec { Id = "LONG2400", Name = "Palet lung · 2400×1200 (la comandă)", L = 2400, W = 1200, Deck = 144, MaxKg = 1500, Source = "[ESTIMARE]" },
        };
    }

    public sealed class PackingOptions
    {
        /// <summary>Gap between parts inside a carton (foam / cardboard separators), mm.</summary>
        public double Padding { get; set; } = 10;
        /// <summary>Carton board thickness (double-wall BC ≈ 7-8 mm), added to each side for the outer size.</summary>
        public double CartonWall { get; set; } = 8;
        /// <summary>Handling limit per carton: ~30 kg for BC double-wall guideline [REF]; courier DPD 31.5 kg.</summary>
        public double MaxCartonKg { get; set; } = 30;
        /// <summary>Loaded pallet height limit including the pallet (road/LTL practice, to be confirmed with the carrier) [ESTIMARE].</summary>
        public double MaxPalletHeight { get; set; } = 1800;
        public double MaxPalletKg { get; set; } = 1000;
        public double MinSupport { get; set; } = 0.75;
    }

    public sealed class PackItem
    {
        public string Id { get; set; }
        public string Label { get; set; }
        public string Group { get; set; }          // family name, for the report
        public string Piece { get; set; }          // the furniture piece it belongs to: cartons never mix pieces
        public double A { get; set; }              // dimensions (any order)
        public double B { get; set; }
        public double C { get; set; }
        public double MassKg { get; set; }
        public double Volume => A * B * C;
    }

    public sealed class PlacedItem { public PackItem Item { get; set; } public Box3 Box { get; set; } }

    public sealed class PackedCarton
    {
        public string Id { get; set; }
        public CartonSpec Spec { get; set; }
        public bool Custom { get; set; }
        public List<PlacedItem> Items { get; set; } = new List<PlacedItem>();
        public double ContentKg { get; set; }
        public double CartonKg { get; set; }
        public double Mass => ContentKg + CartonKg;
        public double OuterL, OuterW, OuterH;
        public double InnerL, InnerW, InnerH;
        public string Courier { get; set; }
        public string Piece { get; set; }
    }

    public sealed class PlacedCarton { public PackedCarton Carton { get; set; } public Box3 Box { get; set; } }

    public sealed class PalletLoad
    {
        public string Id { get; set; }
        public PalletSpec Spec { get; set; }
        public List<PlacedCarton> Cartons { get; set; } = new List<PlacedCarton>();
        public double MassKg => Cartons.Sum(c => c.Carton.Mass) + 25;
        public double Height => Spec.Deck + (Cartons.Count == 0 ? 0 : Cartons.Max(c => c.Box.Max.Z));
    }

    public sealed class PackingPlan
    {
        public List<PackedCarton> Cartons { get; set; } = new List<PackedCarton>();
        public List<PalletLoad> Pallets { get; set; } = new List<PalletLoad>();
        public List<string> Warnings { get; set; } = new List<string>();
        public double TotalKg => Pallets.Sum(p => p.MassKg);
        public int ItemCount => Cartons.Sum(c => c.Items.Count);
        public string Report { get; set; }
    }

    /// <summary>
    /// Packs finished parts into standard cartons (smallest carton that takes everything left, otherwise the carton that takes the most), then the cartons onto the
    /// smallest pallets that hold them. Extreme-point 3D heuristic with support check; every part and carton is placed inside its container (verified by tests).
    /// </summary>
    public static class PackingPlanner
    {
        private const double Eps = 1e-6;

        // ------------------------------------------------------------------------------------------ items from projects
        public static List<PackItem> ItemsOf(WoodProject project, ProjectResult result, string prefix = "")
        {
            var items = new List<PackItem>();
            var lib = project.Library; var model = result.Model;
            foreach (var part in model.AllParts)
            {
                var fam = model.FamilyOf(part.Id); var f = part.Finished;
                double dens = lib.Species.TryGetValue(fam.SpeciesId, out var sp) && sp.DensityKgM3 > 0 ? sp.DensityKgM3 : 600;
                items.Add(new PackItem { Id = prefix + part.Id, Label = prefix + part.Id, Group = Ro.Family(fam), Piece = project.Name, A = f.Length, B = f.Width, C = f.Thickness, MassKg = f.VolumeM3 * dens });
            }
            foreach (var s in model.SheetParts)
            {
                var d = s.Bounds.Size; var dims = new[] { d.X, d.Y, d.Z }.OrderByDescending(x => x).ToArray();
                items.Add(new PackItem { Id = prefix + s.Id, Label = prefix + s.Id, Group = s.Name + " (" + s.Material + ")", Piece = project.Name, A = dims[0], B = dims[1], C = dims[2], MassKg = s.AreaM2 * s.Thickness / 1000.0 * 850 });
            }
            int hw = model.HardwareInstalls.Count;
            if (hw + model.Biscuits.Count > 0) items.Add(new PackItem { Id = prefix + "HW", Label = prefix + "Feronerie", Group = "Feronerie și lamele", Piece = project.Name, A = 300, B = 200, C = 100, MassKg = 0.15 * hw + 0.005 * model.Biscuits.Count });
            return items;
        }

        // ------------------------------------------------------------------------------------------ geometry helpers
        private sealed class Bin
        {
            public double L, W, H;
            public readonly List<Box3> Placed = new List<Box3>();
            public readonly List<Vec3> Points = new List<Vec3> { new Vec3(0, 0, 0) };
        }

        private static bool Overlap(Box3 a, Box3 b) =>
            a.Min.X < b.Max.X - Eps && a.Max.X > b.Min.X + Eps && a.Min.Y < b.Max.Y - Eps && a.Max.Y > b.Min.Y + Eps && a.Min.Z < b.Max.Z - Eps && a.Max.Z > b.Min.Z + Eps;

        private static double Support(Bin bin, Box3 box)
        {
            if (box.Min.Z < Eps) return 1;
            double area = (box.Max.X - box.Min.X) * (box.Max.Y - box.Min.Y), sup = 0;
            foreach (var p in bin.Placed)
            {
                if (Math.Abs(p.Max.Z - box.Min.Z) > 0.5) continue;
                double dx = Math.Min(p.Max.X, box.Max.X) - Math.Max(p.Min.X, box.Min.X), dy = Math.Min(p.Max.Y, box.Max.Y) - Math.Max(p.Min.Y, box.Min.Y);
                if (dx > 0 && dy > 0) sup += dx * dy;
            }
            return sup / area;
        }

        private static IEnumerable<(double x, double y, double z)> Orientations(double a, double b, double c, bool upright)
        {
            if (upright) { yield return (a, b, c); yield return (b, a, c); yield break; }
            var seen = new HashSet<string>();
            foreach (var o in new[] { (a, b, c), (a, c, b), (b, a, c), (b, c, a), (c, a, b), (c, b, a) })
                if (seen.Add(o.Item1.ToString("0.###", CultureInfo.InvariantCulture) + "," + o.Item2.ToString("0.###", CultureInfo.InvariantCulture) + "," + o.Item3.ToString("0.###", CultureInfo.InvariantCulture))) yield return o;
        }

        /// <summary>Best extreme point for an item of size (a,b,c) (already padded) or null.</summary>
        private static Box3? Place(Bin bin, double a, double b, double c, bool upright, double minSupport)
        {
            Box3? best = null; double bz = double.MaxValue, by = double.MaxValue, bx = double.MaxValue, bd = double.MaxValue;
            foreach (var p in bin.Points)
                foreach (var (dx, dy, dz) in Orientations(a, b, c, upright))
                {
                    if (p.X + dx > bin.L + Eps || p.Y + dy > bin.W + Eps || p.Z + dz > bin.H + Eps) continue;
                    var box = new Box3(p, new Vec3(p.X + dx, p.Y + dy, p.Z + dz));
                    bool ok = true;
                    foreach (var q in bin.Placed) if (Overlap(box, q)) { ok = false; break; }
                    if (!ok || Support(bin, box) < minSupport) continue;
                    // prefer low, then lying flat (small height), then back-left
                    if (p.Z < bz - Eps || (Math.Abs(p.Z - bz) < Eps && (dz < bd - Eps || (Math.Abs(dz - bd) < Eps && (p.Y < by - Eps || (Math.Abs(p.Y - by) < Eps && p.X < bx))))))
                    { best = box; bz = p.Z; bd = dz; by = p.Y; bx = p.X; }
                }
            return best;
        }

        private static void Commit(Bin bin, Box3 box)
        {
            bin.Placed.Add(box);
            bin.Points.RemoveAll(p => p.X > box.Min.X + Eps && p.X < box.Max.X - Eps && p.Y > box.Min.Y + Eps && p.Y < box.Max.Y - Eps && p.Z > box.Min.Z + Eps && p.Z < box.Max.Z - Eps);
            foreach (var np in new[] { new Vec3(box.Max.X, box.Min.Y, box.Min.Z), new Vec3(box.Min.X, box.Max.Y, box.Min.Z), new Vec3(box.Min.X, box.Min.Y, box.Max.Z) })
                if (np.X < bin.L - Eps && np.Y < bin.W - Eps && np.Z < bin.H - Eps && !bin.Points.Any(q => Math.Abs(q.X - np.X) < Eps && Math.Abs(q.Y - np.Y) < Eps && Math.Abs(q.Z - np.Z) < Eps)) bin.Points.Add(np);
        }

        // ------------------------------------------------------------------------------------------ cartons
        private static double CartonKgOf(double l, double w, double h, double wall) => 2 * (l * w + l * h + w * h) / 1e6 * 0.9;   // double-wall BC ≈ 0.9 kg/m²

        private sealed class CartonTry { public PackedCarton Carton; public List<PackItem> Packed = new List<PackItem>(); public double PackedVolume; }

        private static CartonTry Fill(CartonSpec spec, List<PackItem> items, PackingOptions o, bool all)
        {
            var bin = new Bin { L = spec.L, W = spec.W, H = spec.H };
            var res = new CartonTry { Carton = new PackedCarton { Spec = spec, InnerL = spec.L, InnerW = spec.W, InnerH = spec.H, OuterL = spec.L + 2 * o.CartonWall, OuterW = spec.W + 2 * o.CartonWall, OuterH = spec.H + 2 * o.CartonWall, CartonKg = CartonKgOf(spec.L, spec.W, spec.H, o.CartonWall) } };
            double kg = 0;
            foreach (var it in items)
            {
                if (kg + it.MassKg > o.MaxCartonKg + 1e-9) { if (all) return null; continue; }
                var box = Place(bin, it.A + o.Padding, it.B + o.Padding, it.C + o.Padding, false, 0.5);
                if (box == null) { if (all) return null; continue; }
                Commit(bin, box.Value);
                var b = box.Value; double h = o.Padding / 2;
                res.Carton.Items.Add(new PlacedItem { Item = it, Box = new Box3(new Vec3(b.Min.X + h, b.Min.Y + h, b.Min.Z + h), new Vec3(b.Max.X - h, b.Max.Y - h, b.Max.Z - h)) });
                res.Packed.Add(it); res.PackedVolume += it.Volume; kg += it.MassKg;
            }
            res.Carton.ContentKg = kg;
            return res;
        }

        /// <summary>Cartons never mix furniture pieces: each piece is packed on its own, numbering continues across pieces.</summary>
        public static List<PackedCarton> PackCartons(List<PackItem> items, PackingOptions o, List<string> warnings)
        {
            var all = new List<PackedCarton>();
            foreach (var g in items.GroupBy(i => i.Piece ?? "").Select(g => g.ToList()))
                all.AddRange(PackGroup(g, o, warnings, all.Count));
            return all;
        }

        private static List<PackedCarton> PackGroup(List<PackItem> items, PackingOptions o, List<string> warnings, int offset)
        {
            var result = new List<PackedCarton>();
            var remaining = items.OrderByDescending(i => Math.Max(i.A, Math.Max(i.B, i.C))).ThenByDescending(i => i.Volume).ToList();
            var catalog = PackingCatalog.Cartons.OrderBy(c => c.Volume).ToList();
            int n = offset;
            while (remaining.Count > 0)
            {
                CartonTry chosen = null;
                if (remaining.Sum(i => i.MassKg) <= o.MaxCartonKg)
                    foreach (var c in catalog) { var t = Fill(c, remaining, o, true); if (t != null) { chosen = t; break; } }
                if (chosen == null)
                {
                    foreach (var c in catalog)
                    {
                        var t = Fill(c, remaining, o, false);
                        if (t.Packed.Count == 0) continue;
                        if (chosen == null || t.PackedVolume > chosen.PackedVolume * 1.001 || (Math.Abs(t.PackedVolume - chosen.PackedVolume) <= chosen.PackedVolume * 0.001 && c.Volume < chosen.Carton.Spec.Volume)) chosen = t;
                    }
                }
                if (chosen == null)
                {
                    // nothing fits any catalogue carton (too long / too heavy): individual wrap sized to the part
                    var it = remaining[0];
                    var dims = new[] { it.A, it.B, it.C }.OrderByDescending(x => x).ToArray();
                    var spec = new CartonSpec { Id = "C-ind-" + (n + 1), Name = "Ambalaj individual la comandă " + dims[0].ToString("0") + "×" + dims[1].ToString("0") + "×" + dims[2].ToString("0"), L = dims[0] + 2 * o.Padding, W = dims[1] + 2 * o.Padding, H = dims[2] + 2 * o.Padding, Source = "[ESTIMARE]" };
                    chosen = Fill(spec, new List<PackItem> { it }, o, false);
                    if (chosen.Packed.Count == 0) { chosen.Carton.Items.Add(new PlacedItem { Item = it, Box = new Box3(new Vec3(o.Padding, o.Padding, o.Padding), new Vec3(o.Padding + dims[0], o.Padding + dims[1], o.Padding + dims[2])) }); chosen.Carton.ContentKg = it.MassKg; chosen.Packed.Add(it); }
                    chosen.Carton.Custom = true;
                    if (it.MassKg > o.MaxCartonKg) warnings.Add(it.Label + ": " + it.MassKg.ToString("0.#", CultureInfo.InvariantCulture) + " kg — manipulare cu 2 persoane / stivuitor.");
                }
                chosen.Carton.Id = "Cutia " + (++n); chosen.Carton.Piece = remaining.FirstOrDefault()?.Piece;
                chosen.Carton.Courier = CourierOf(chosen.Carton);
                result.Add(chosen.Carton);
                foreach (var p in chosen.Packed) remaining.Remove(p);
            }
            return result;
        }

        /// <summary>DPD home delivery: 31.5 kg, length ≤ 1750 mm, girth ≤ 3000 mm; GLS parcel: 40 kg, length ≤ 2000 mm, girth ≤ 3000 mm [REF carrier sites]. Heavier / longer goes on a pallet.</summary>
        private static string CourierOf(PackedCarton c)
        {
            var d = new[] { c.OuterL, c.OuterW, c.OuterH }.OrderByDescending(x => x).ToArray();
            double girth = d[0] + 2 * d[1] + 2 * d[2];
            if (girth > 3000) return "doar paletat";
            if (d[0] <= 1750 && c.Mass <= 31.5) return "DPD / GLS";
            if (d[0] <= 2000 && c.Mass <= 40) return "GLS";
            return "doar paletat";
        }

        // ------------------------------------------------------------------------------------------ pallets
        private static bool PackOnPallet(PalletSpec spec, List<PackedCarton> cartons, PackingOptions o, out List<PlacedCarton> placed)
        {
            placed = new List<PlacedCarton>();
            var bin = new Bin { L = spec.L, W = spec.W, H = o.MaxPalletHeight - spec.Deck };
            double kg = 25;
            foreach (var c in cartons.OrderByDescending(c => c.OuterL * c.OuterW).ThenByDescending(c => c.Mass))
            {
                if (kg + c.Mass > Math.Min(o.MaxPalletKg, spec.MaxKg) + 1e-9) return false;
                var box = Place(bin, c.OuterL, c.OuterW, c.OuterH, true, o.MinSupport);
                if (box == null) return false;
                Commit(bin, box.Value); kg += c.Mass;
                placed.Add(new PlacedCarton { Carton = c, Box = box.Value });
            }
            return true;
        }

        public static List<PalletLoad> Palletize(List<PackedCarton> cartons, PackingOptions o, List<string> warnings)
        {
            var specs = PackingCatalog.Pallets.OrderBy(p => p.Area).ToList();
            var groups = new List<(PalletSpec spec, List<PackedCarton> cartons)>();
            foreach (var c in cartons.OrderByDescending(c => Math.Max(c.OuterL, c.OuterW)).ThenByDescending(c => c.OuterL * c.OuterW))
            {
                bool done = false;
                foreach (var g in groups)
                {
                    var trial = g.cartons.Concat(new[] { c }).ToList();
                    if (PackOnPallet(g.spec, trial, o, out _)) { g.cartons.Add(c); done = true; break; }
                }
                if (done) continue;
                var spec = specs.FirstOrDefault(s => PackOnPallet(s, new List<PackedCarton> { c }, o, out _));
                if (spec == null) { warnings.Add(c.Id + ": nu încape pe niciun palet din catalog (" + c.OuterL.ToString("0") + "×" + c.OuterW.ToString("0") + "×" + c.OuterH.ToString("0") + " mm) — transport special."); spec = specs.Last(); }
                groups.Add((spec, new List<PackedCarton> { c }));
            }
            var loads = new List<PalletLoad>(); int n = 0;
            foreach (var g in groups)
            {
                // downsize: the smallest pallet that takes the whole group
                var spec = specs.FirstOrDefault(s => s.Area < g.spec.Area && PackOnPallet(s, g.cartons, o, out _)) ?? g.spec;
                if (!PackOnPallet(spec, g.cartons, o, out var placed)) { spec = g.spec; placed = g.cartons.Select(c => new PlacedCarton { Carton = c, Box = new Box3(new Vec3(0, 0, 0), new Vec3(c.OuterL, c.OuterW, c.OuterH)) }).ToList(); }
                loads.Add(new PalletLoad { Id = "Palet " + (++n), Spec = spec, Cartons = placed });
            }
            return loads;
        }

        public static PackingPlan Plan(List<PackItem> items, PackingOptions o = null)
        {
            o = o ?? new PackingOptions();
            var plan = new PackingPlan();
            plan.Cartons = PackCartons(items, o, plan.Warnings);
            plan.Pallets = Palletize(plan.Cartons, o, plan.Warnings);
            plan.Report = Report(plan, o);
            return plan;
        }

        public static PackingPlan Plan(IEnumerable<(WoodProject project, ProjectResult result, string prefix)> pieces, PackingOptions o = null) =>
            Plan(pieces.SelectMany(p => ItemsOf(p.project, p.result, p.prefix)).ToList(), o);

        // ------------------------------------------------------------------------------------------ report
        public static string Report(PackingPlan plan, PackingOptions o)
        {
            var ci = CultureInfo.InvariantCulture; var sb = new StringBuilder();
            sb.AppendLine(string.Format(ci, "PLAN DE PALETARE: {0} palet(i), {1} cutii, {2} piese, {3:0} kg în total", plan.Pallets.Count, plan.Cartons.Count, plan.ItemCount, plan.TotalKg));
            sb.AppendLine(string.Format(ci, "Reguli: cutie max {0:0} kg, spațiu între piese {1:0} mm, palet max {2:0} mm înălțime / {3:0} kg, fără depășire a paletului.", o.MaxCartonKg, o.Padding, o.MaxPalletHeight, o.MaxPalletKg));
            foreach (var p in plan.Pallets)
            {
                sb.AppendLine();
                sb.AppendLine(string.Format(ci, "{0} · {1} · înălțime {2:0} mm · {3:0} kg · {4} cutii", p.Id, p.Spec.Name, p.Height, p.MassKg, p.Cartons.Count));
                foreach (var pc in p.Cartons.OrderBy(x => x.Box.Min.Z).ThenBy(x => x.Box.Min.Y).ThenBy(x => x.Box.Min.X))
                {
                    var c = pc.Carton;
                    sb.AppendLine(string.Format(ci, "  {0} [{7}] · {1} · exterior {2:0}×{3:0}×{4:0} · {5:0.0} kg · curier: {6}", c.Id, c.Spec.Name, c.OuterL, c.OuterW, c.OuterH, c.Mass, c.Courier, c.Piece));
                    foreach (var g in c.Items.GroupBy(i => i.Item.Group))
                        sb.AppendLine(string.Format(ci, "      {0} × {1}  ({2})", g.Count(), g.Key, string.Join(", ", g.Select(i => i.Item.Label).Take(6)) + (g.Count() > 6 ? "…" : "")));
                }
            }
            foreach (var w in plan.Warnings) sb.AppendLine("ATENȚIE: " + w);
            sb.AppendLine();
            sb.AppendLine("Surse: EPAL 1 1200×800×144 mm, 1500 kg [V-DATA]; EPAL 3 1000×1200 [V-DATA]; curier DPD 31,5 kg / 1,75 m, GLS 40 kg / 2 m, circumferință max 3 m [REF]; cutii FEFCO 0201/0203 dublu strat BC, ~30 kg [REF]; paleți lungi, cutii lungi la comandă și înălțimea de 1800 mm sunt [ESTIMARE] — confirmă cu transportatorul.");
            return sb.ToString();
        }
    }
}
