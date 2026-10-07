"""WOOD ENGINE — constructia in Rhino 8 (Python 3). Se importa doar din Rhino.

Layere:  WOOD::<proiect>::ANSAMBLU::<material>
         WOOD::<proiect>::PIESE
         WOOD::<proiect>::DESEURI::Imbinari   (rosu 255,0,0)      = brut ∩ uniune(scule)
         WOOD::<proiect>::DESEURI::Debitare   (rosu deschis)      = brut de debitare - piesa bruta (adaos)
         WOOD::<proiect>::ETICHETE
"De ce?": fiecare piesa / deseu are User Text (Properties > Attribute User Text):
         WOOD_piesa, WOOD_operatii, WOOD_reguli  — ID-urile se cauta in data/rules.json sau in foaia 'De ce (surse)'.
"""
import json
import Rhino
import scriptcontext as sc
import System
from Rhino.Geometry import (Point3d, Vector3d, Plane, Circle, Cylinder, Brep, Polyline, PolylineCurve, Surface,
                            Transform, BrepSolidOrientation)

DOC = Rhino.RhinoDoc.ActiveDoc
TOL = DOC.ModelAbsoluteTolerance
RED = System.Drawing.Color.FromArgb(255, 0, 0)
LIGHT_RED = System.Drawing.Color.FromArgb(255, 170, 170)


def _scale():
    u = DOC.ModelUnitSystem
    if u == Rhino.UnitSystem.Millimeters:
        return 1.0
    if u == Rhino.UnitSystem.Centimeters:
        return 0.1
    if u == Rhino.UnitSystem.Meters:
        return 0.001
    if u == Rhino.UnitSystem.Inches:
        return 1 / 25.4
    return 1.0


S = 1.0


def P3(x, y, z):
    return Point3d(x * S, y * S, z * S)


def _layer(path, color=None):
    idx = DOC.Layers.FindByFullPath(path, -1)
    if idx >= 0:
        return idx
    parent_id = System.Guid.Empty
    parts = path.split("::")
    cur = ""
    for i, name in enumerate(parts):
        cur = name if i == 0 else cur + "::" + name
        j = DOC.Layers.FindByFullPath(cur, -1)
        if j < 0:
            L = Rhino.DocObjects.Layer()
            L.Name = name
            if parent_id != System.Guid.Empty:
                L.ParentLayerId = parent_id
            if color is not None and i == len(parts) - 1:
                L.Color = color
            j = DOC.Layers.Add(L)
        parent_id = DOC.Layers[j].Id
    return DOC.Layers.FindByFullPath(path, -1)


def _clear(root):
    for L in list(DOC.Layers):
        if L.FullPath == root or L.FullPath.startswith(root + "::"):
            for o in DOC.Objects.FindByLayer(L) or []:
                DOC.Objects.Delete(o, True)


def brep_of(p):
    k = p[0]
    if k == "box":
        _, x0, x1, y0, y1, z0, z1 = p
        bb = Rhino.Geometry.BoundingBox(P3(x0, y0, z0), P3(x1, y1, z1))
        return Brep.CreateFromBox(bb)
    if k == "cyl":
        _, axis, c1, c2, a0, a1, r = p
        if axis == "x":
            o, v = P3(a0, c1, c2), Vector3d(1, 0, 0)
        elif axis == "y":
            o, v = P3(c1, a0, c2), Vector3d(0, 1, 0)
        else:
            o, v = P3(c1, c2, a0), Vector3d(0, 0, 1)
        c = Cylinder(Circle(Plane(o, v), r * S), (a1 - a0) * S)
        return c.ToBrep(True, True)
    if k == "prism":
        _, axis, pts, a0, a1 = p
        if axis == "x":
            P = [P3(a0, u, w) for u, w in pts]; v = Vector3d((a1 - a0) * S, 0, 0)
        elif axis == "y":
            P = [P3(u, a0, w) for u, w in pts]; v = Vector3d(0, (a1 - a0) * S, 0)
        else:
            P = [P3(u, w, a0) for u, w in pts]; v = Vector3d(0, 0, (a1 - a0) * S)
        P.append(P[0])
        crv = PolylineCurve(P)
        b = Surface.CreateExtrusion(crv, v).ToBrep().CapPlanarHoles(TOL)
        if b is not None and b.SolidOrientation == BrepSolidOrientation.Inward:
            b.Flip()
        return b
    raise ValueError(k)


def _union(breps):
    breps = [b for b in breps if b is not None]
    if not breps:
        return []
    u = Brep.CreateBooleanUnion(breps, TOL)
    return list(u) if u else breps


def _difference(blank, cutters):
    if not cutters:
        return [blank]
    r = Brep.CreateBooleanDifference([blank], cutters, TOL)
    if r:
        return list(r)
    cur = [blank]                                   # fallback: o scula pe rand
    for c in cutters:
        nxt = []
        for b in cur:
            rr = Brep.CreateBooleanDifference(b, c, TOL)
            nxt += list(rr) if rr else [b]
        cur = nxt
    return cur


def _intersection(blank, cutters):
    if not cutters:
        return []
    r = Brep.CreateBooleanIntersection([blank], cutters, TOL)
    if r:
        return list(r)
    out = []
    for c in cutters:
        rr = Brep.CreateBooleanIntersection(blank, c, TOL)
        if rr:
            out += list(rr)
    return out


def _add(geo, layer_idx, name, user=None):
    a = Rhino.DocObjects.ObjectAttributes()
    a.LayerIndex = layer_idx
    a.Name = name
    for k, v in (user or {}).items():
        a.SetUserString(k, v)
    return DOC.Objects.AddBrep(geo, a)


def _lay_flat(part):
    """Transformare care culca piesa: axa lunga -> X, apoi grosimea pe Z."""
    ax = getattr(part, "axis_vec", None)
    bb = part.bbox()
    c = P3((bb[0] + bb[1]) / 2, (bb[2] + bb[3]) / 2, (bb[4] + bb[5]) / 2)
    if ax is not None:
        xv = Vector3d(*ax); xv.Unitize()
        yv = Vector3d(*getattr(part, "flat_vec", (1, 0, 0)))
        pl = Plane(c, xv, yv)
    else:
        dims = sorted([(bb[1] - bb[0], 0), (bb[3] - bb[2], 1), (bb[5] - bb[4], 2)], reverse=True)
        e = [Vector3d(1, 0, 0), Vector3d(0, 1, 0), Vector3d(0, 0, 1)]
        pl = Plane(c, e[dims[0][1]], e[dims[1][1]])
    return Transform.PlaneToPlane(pl, Plane.WorldXY)


def build_in_rhino(prj, materials_json):
    global S
    S = _scale()
    mats = json.load(open(materials_json, encoding="utf-8"))["materials"]
    root = "WOOD::" + prj.name.replace("::", "-")
    _clear(root)
    L_pie = _layer(root + "::PIESE")
    L_wj = _layer(root + "::DESEURI::Imbinari", RED)
    L_wd = _layer(root + "::DESEURI::Debitare", LIGHT_RED)
    L_lab = _layer(root + "::ETICHETE")
    bb_all = None
    row_x, row_y, row_h = 0.0, None, 0.0
    max_w = 4200.0 * S
    gap = 120.0 * S
    asm_bb = [p.bbox() for p in prj.parts if not getattr(p, "layout_only", False)]
    y_start = (min(b[2] for b in asm_bb) - 700) * S
    x_start = min(b[0] for b in asm_bb) * S
    row_y = y_start
    row_x = x_start
    report = []
    for p in prj.parts:
        col = mats.get(p.mat, {}).get("color", [200, 170, 120])
        L_asm = _layer(root + "::ANSAMBLU::" + p.mat, System.Drawing.Color.FromArgb(*col))
        blank = brep_of(p.blank)
        cutters = _union([brep_of(c.prim) for c in p.cuts])
        fin = _difference(blank, cutters)
        wst = _intersection(blank, cutters)
        ops = sorted(set("%s [%s]" % (c.op, c.tool) for c in p.cuts))
        rules = sorted(set(r for c in p.cuts for r in c.rules) | set(p.rules))
        user = {"WOOD_piesa": "%s %s" % (p.label, p.name), "WOOD_material": p.mat,
                "WOOD_finit_mm": "%.0f x %.0f x %.0f" % p.finished_dims,
                "WOOD_operatii": " | ".join(ops), "WOOD_reguli": ", ".join(rules)}
        if not getattr(p, "layout_only", False):
            for b in fin:
                _add(b, L_asm, p.label + " " + p.name, user)
        report.append("%s: finit %d corp(uri), deseu %d corp(uri)" % (p.label, len(fin), len(wst)))
        if not p.layout:
            continue
        # --- planul de piese: piesa + deseu rosu + adaos de debitare, culcate si asezate in grila
        T = _lay_flat(p)
        geos = []
        for b in fin:
            g = b.DuplicateBrep(); g.Transform(T); geos.append(("fin", g))
        for b in wst:
            g = b.DuplicateBrep(); g.Transform(T); geos.append(("wj", g))
        rough = brep_of(p.rough())
        for b in _difference(rough, [brep_of(p.blank)]):
            g = b.DuplicateBrep(); g.Transform(T); geos.append(("wd", g))
        bbx = Rhino.Geometry.BoundingBox.Empty
        for _, g in geos:
            bbx.Union(g.GetBoundingBox(True))
        w, h = bbx.Max.X - bbx.Min.X, bbx.Max.Y - bbx.Min.Y
        if row_x + w > x_start + max_w:
            row_x = x_start
            row_y -= row_h + gap
            row_h = 0.0
        mv = Transform.Translation(row_x - bbx.Min.X, row_y - bbx.Max.Y, -bbx.Min.Z)
        ids = []
        for kind, g in geos:
            g.Transform(mv)
            lay = {"fin": L_pie, "wj": L_wj, "wd": L_wd}[kind]
            nm = {"fin": p.label + " " + p.name, "wj": p.label + " deseu imbinari", "wd": p.label + " adaos debitare"}[kind]
            u = dict(user)
            if kind == "wd":
                u = {"WOOD_piesa": user["WOOD_piesa"], "WOOD_operatii": "debitare + rindeluire la cota", "WOOD_reguli": "CON-DIM-004"}
            ids.append(_add(g, lay, nm, u))
        txt = "%s %s %s" % (p.label, p.name, p.qty_label)
        dot = Rhino.Geometry.TextDot(txt, Point3d(row_x + w / 2, row_y + 40 * S, (bbx.Max.Z - bbx.Min.Z) + 20 * S))
        a = Rhino.DocObjects.ObjectAttributes(); a.LayerIndex = L_lab
        ids.append(DOC.Objects.AddTextDot(dot, a))
        gi = DOC.Groups.Add()
        DOC.Groups.AddToGroup(gi, [i for i in ids if i != System.Guid.Empty])
        row_x += w + gap
        row_h = max(row_h, h)
    DOC.Views.Redraw()
    print("WOOD ENGINE: %d piese construite pe '%s'" % (len(prj.parts), root))
    for r in report:
        print("  " + r)
