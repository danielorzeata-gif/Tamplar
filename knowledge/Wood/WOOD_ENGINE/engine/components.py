"""WOOD ENGINE — biblioteca de componente (V3).

O componenta = o functie care creeaza o piesa (Part) cu brutul corect, dimensiunile finite pentru debitare,
brutul de debitare si regulile care i-au decis sectiunea. Imbinarile (joints.py) taie apoi in ea.
Toate cotele in mm; axele: X lungime, Y latime, Z sus.
"""
import math
from . import geom as G
from .model import Part

DEFAULT_ROUGH = (4, 4, 30)       # CON-DIM-004


def post(prj, code, name, mat, x0, y0, w, d, h, z0=0.0, rules=(), notes="", rough=DEFAULT_ROUGH):
    """Picior / stalp vertical: sectiune w (X) x d (Y), inaltime h."""
    p = Part(code, name, mat, G.box(x0, x0 + w, y0, y0 + d, z0, z0 + h), (w, d, h), rough, rules=list(rules), notes=notes)
    return prj.add(p)


def rail(prj, code, name, mat, axis, a0, a1, c0, c1, z0, z1, tenons=(0.0, 0.0), rules=(), notes="", rough=DEFAULT_ROUGH, **kw):
    """Traversa / lonjeron / zarga / grinda orizontala de-a lungul `axis` ('x'|'y').
    a0..a1 = intre fetele elementelor in care intra; c0..c1 = grosimea pe cealalta axa orizontala;
    tenons = (lungimea cepului la capatul a0, la capatul a1) — incluse in brut (umerii se taie de imbinare)."""
    b0, b1 = a0 - tenons[0], a1 + tenons[1]
    if axis == "x":
        blank = G.box(b0, b1, c0, c1, z0, z1)
    else:
        blank = G.box(c0, c1, b0, b1, z0, z1)
    p = Part(code, name, mat, blank, (c1 - c0, z1 - z0, b1 - b0), rough, rules=list(rules), notes=notes, **kw)
    return prj.add(p)


def board(prj, code, name, mat, x0, x1, y0, y1, z0, z1, rules=(), notes="", rough=DEFAULT_ROUGH, **kw):
    """Piesa paralelipipedica fara cepuri (sipca, lamela, bloc, blat dintr-o bucata)."""
    p = Part(code, name, mat, G.box(x0, x1, y0, y1, z0, z1), (x1 - x0, y1 - y0, z1 - z0), rough, rules=list(rules), notes=notes, **kw)
    return prj.add(p)


def panel(prj, code, name, mat, x0, x1, y0, y1, z0, z1, n_boards, rules=(), notes="", rough=(5, 10, 40), **kw):
    """Blat / panou incleiat din n scanduri (modelat ca o piesa; latimea scandurilor in observatii)."""
    w = (y1 - y0) / n_boards
    note = ("din %d scanduri ~%.0f mm incleiate pe cant (rost plan, PVAc D3); alterneaza/aleg dupa aspect — vezi C-05. " % (n_boards, w)) + notes
    return board(prj, code, name, mat, x0, x1, y0, y1, z0, z1, rules=list(rules) + ["ADH-004b"], notes=note, rough=rough, **kw)


def inclined_member(prj, code, name, mat, plane_axis, t0, t1, p0, p1, half_w, ext0=0.0, ext1=0.0, clips=(),
                    rules=(), notes="", rough=DEFAULT_ROUGH):
    """Element inclinat (caprior, contrafisa) in planul perpendicular pe `plane_axis`, grosime t0..t1 pe acea axa.
    p0 -> p1 = axa in plan; clips = semiplane (a, b, c) pastrate (a*u + b*v + c >= 0). Returneaza (piesa, banda completa)."""
    full = G.band(p0, p1, half_w, ext0=ext0, ext1=ext1)
    poly = list(full)
    for (a, b, c) in clips:
        poly = G.clip_halfplane(poly, a, b, c)
    ux, uv = p1[0] - p0[0], p1[1] - p0[1]
    L = math.hypot(ux, uv); ux, uv = ux / L, uv / L
    proj = [q[0] * ux + q[1] * uv for q in poly]
    length = max(proj) - min(proj)
    part = Part(code, name, mat, G.prism(plane_axis, poly, t0, t1), (t1 - t0, 2 * half_w, length), rough, rules=list(rules), notes=notes)
    ra = rough[1] / 2.0
    rough_band = G.band(p0, p1, half_w + ra, ext0=ext0 + rough[2] / 2.0, ext1=ext1 + rough[2] / 2.0)
    part.rough_prim = G.prism(plane_axis, rough_band, t0 - rough[0] / 2.0, t1 + rough[0] / 2.0)
    av = [0.0, 0.0, 0.0]
    i, j = G.OTHER[plane_axis]
    av[i], av[j] = ux, uv
    fv = [0.0, 0.0, 0.0]; fv[G.AX[plane_axis]] = 1.0
    part.axis_vec, part.flat_vec = tuple(av), tuple(fv)
    return prj.add(part), full, poly
