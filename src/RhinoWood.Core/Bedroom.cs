using System;
using System.Collections.Generic;
using System.Linq;
using RhinoWood.Core.Display;
using RhinoWood.Core.Domain;
using RhinoWood.Core.Furniture;
using RhinoWood.Core.Geometry;
using RhinoWood.Core.Reports;
using RhinoWood.Core.Workspaces;

namespace RhinoWood.Core.Projects
{
    /// <summary>MIX-011: the offer states the species of every visibility class (A visible closed, B visible when open, C hidden) - never a bare "solid oak".</summary>
    public static class SpeciesDeclaration
    {
        public static string Text(IEnumerable<ProjectResult> results, string tier = null)
        {
            var fams = results.SelectMany(r => r.Model.Families).ToList();
            string Sp(char c) => string.Join(" / ", fams.Where(f => f.VisClass == c).Select(f => f.SpeciesId).Distinct().OrderBy(x => x, StringComparer.Ordinal).Select(id => Ro.SpeciesName(id).ToLowerInvariant() + " masiv"));
            string a = Sp('A'), b = Sp('B'), c = Sp('C');
            bool hasSheet = results.Any(r => r.Model.SheetParts.Count > 0);
            var all = fams.Select(f => f.SpeciesId).Distinct().ToList();
            string text;
            if (all.Count <= 1 && all.Count == 1)
                text = Cap(Ro.SpeciesName(all[0]).ToLowerInvariant()) + " masiv în toate componentele din lemn: structură, fronturi, interior și sertare.";
            else
            {
                var parts = new List<string>();
                if (a.Length > 0) parts.Add("Exterior din " + a + " (fronturi, blat/laterale, picioare, tăblie).");
                if (b.Length > 0) parts.Add("Interior și cutiile sertarelor din " + b + ".");
                if (c.Length > 0) parts.Add("Elemente structurale ascunse (somieră, grinzi) din " + c + ".");
                text = string.Join(" ", parts);
            }
            if (hasSheet) text += " Spate și funduri de sertar: HDF 3 mm.";
            return text;
        }
        private static string Cap(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
    }

    /// <summary>The first bedroom collection (Fișa dormitor): bed + 2 nightstands + dresser with one set of choices, one purchase plan and a tidy layout.</summary>
    public static class BedroomSet
    {
        public static Room Create(Workspace ws, string tier = "STANDARD", string speciesId = "OAK", string name = "Dormitor")
        {
            var room = ws.AddRoom(name);
            WoodProject Mk(string type, string nm) { var p = WoodProject.Create(type, nm, speciesId, ws.Library); return p; }
            room.AddPiece("Pat", Mk("casework.bed", "Pat"));
            room.AddPiece("Noptieră stânga", Mk("casework.nightstand", "Noptieră stânga"));
            room.AddPiece("Noptieră dreapta", Mk("casework.nightstand", "Noptieră dreapta"));
            room.AddPiece("Comodă", Mk("casework.dresser", "Comodă"));
            room.ApplyTier(tier);
            Layout(room);
            return room;
        }

        /// <summary>Bed against the head wall (y = 0), a nightstand on each side facing the foot of the bed, the dresser on the opposite wall facing the bed.</summary>
        public static void Layout(Room room)
        {
            var bed = room.Pieces.FirstOrDefault(p => p.Project.Furniture.TypeId == "casework.bed");
            var ns = room.Pieces.Where(p => p.Project.Furniture.TypeId == "casework.nightstand").ToList();
            var dr = room.Pieces.FirstOrDefault(p => p.Project.Furniture.TypeId == "casework.dresser");
            if (bed == null) return;
            var bb = bed.Project.Recalculate().Model.Bounds.Size;
            double bw = bb.X, bl = bb.Y; bed.Project.Placement = new Placement();
            for (int i = 0; i < ns.Count; i++)
            {
                var s = ns[i].Project.Recalculate().Model.Bounds.Size;
                bool left = i % 2 == 0;
                ns[i].Project.Placement = new Placement { RotZ = 180, X = left ? -(s.X + 60) : bw + 60, Y = 0 };
            }
            if (dr != null)
            {
                var s = dr.Project.Recalculate().Model.Bounds.Size;
                dr.Project.Placement = new Placement { RotZ = 0, X = bw / 2 - s.X / 2, Y = bl + 1000 };
            }
        }
    }
}
