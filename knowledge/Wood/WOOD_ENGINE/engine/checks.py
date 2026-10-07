"""WOOD ENGINE — verificari numerice (fara Rhino). Nimic nu se livreaza pana nu trec."""
from . import geom as G


def collisions(parts, step=6.0, maxn=60, tol=0.05):
    """Pentru fiecare pereche cu bbox-uri care se intersecteaza in volum, esantioneaza intersectia
    (puncte la mijloc de celula); un punct comun in ambele piese FINITE = coliziune."""
    found = []
    n = len(parts)
    for i in range(n):
        for j in range(i + 1, n):
            a, b = parts[i], parts[j]
            ib = G.bb_inter(a.bbox(), b.bbox())
            if not ib:
                continue
            nx = max(1, min(maxn, int((ib[1] - ib[0]) / step)))
            ny = max(1, min(maxn, int((ib[3] - ib[2]) / step)))
            nz = max(1, min(maxn, int((ib[5] - ib[4]) / step)))
            hit = None
            cnt = 0
            for ix in range(nx):
                x = ib[0] + (ix + 0.5) * (ib[1] - ib[0]) / nx
                for iy in range(ny):
                    y = ib[2] + (iy + 0.5) * (ib[3] - ib[2]) / ny
                    for iz in range(nz):
                        z = ib[4] + (iz + 0.5) * (ib[5] - ib[4]) / nz
                        p = (x, y, z)
                        if a.contains(p, tol) and b.contains(p, tol):
                            cnt += 1
                            if hit is None:
                                hit = p
            if cnt:
                found.append((a.label, a.name, b.label, b.name, cnt, hit))
    return found


def cut_conflicts(parts, min_gap=3.0, allow=None):
    """In aceeasi piesa, scule ale unor imbinari DIFERITE nu au voie sa se intersecteze in interiorul piesei
    (ex. gaura de bulon a unei traverse prin scobitura celeilalte). Test pe bbox + esantionare."""
    out = []
    for p in parts:
        pb = p.bbox()
        cs = [c for c in p.cuts]
        for i in range(len(cs)):
            for j in range(i + 1, len(cs)):
                ci, cj = cs[i], cs[j]
                if ci.joint == cj.joint and ci.note == cj.note:
                    # aceeasi imbinare: verificam doar perechi din noduri diferite (marcate prin 'note' = nod)
                    continue
                if allow and frozenset((ci.joint, cj.joint)) in allow:
                    continue      # suprapunere declarata ca intentionata (justificata in proiect)
                ib = G.bb_inter(G.bbox(ci.prim), G.bbox(cj.prim))
                if not ib:
                    continue
                ib = G.bb_inter(ib, pb)
                if not ib:
                    continue
                hit = False
                n = 8
                for ix in range(n):
                    if hit:
                        break
                    x = ib[0] + (ix + 0.5) * (ib[1] - ib[0]) / n
                    for iy in range(n):
                        y = ib[2] + (iy + 0.5) * (ib[3] - ib[2]) / n
                        for iz in range(n):
                            z = ib[4] + (iz + 0.5) * (ib[5] - ib[4]) / n
                            pt = (x, y, z)
                            if G.inside(ci.prim, pt) and G.inside(cj.prim, pt) and G.inside(p.blank, pt, 0.5):
                                hit = True
                                break
                if hit:
                    out.append((p.label, p.name, ci.op, ci.note, cj.op, cj.note))
    return out


def supported(parts, tol=1.0):
    """Fiecare piesa trebuie sa atinga (bbox la <= tol) cel putin o alta piesa — altfel 'pluteste'."""
    lonely = []
    for a in parts:
        ab = a.bbox()
        touch = False
        for b in parts:
            if b is a:
                continue
            bb = b.bbox()
            if (ab[0] <= bb[1] + tol and bb[0] <= ab[1] + tol and ab[2] <= bb[3] + tol and bb[2] <= ab[3] + tol
                    and ab[4] <= bb[5] + tol and bb[4] <= ab[5] + tol):
                touch = True
                break
        if not touch:
            lonely.append(a.label + " " + a.name)
    return lonely
