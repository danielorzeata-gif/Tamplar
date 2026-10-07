"""WOOD ENGINE — previzualizare fara Rhino (PIL): esantioneaza volumul pieselor si deseneaza izometric.
Lemn = piesa finita; ROSU = deseu imbinari (brut ∩ scule); ROZ = adaos de debitare (brut de debitare - brut).
Scop: verificare vizuala independenta a datelor care merg in Rhino. NU e randare de calitate."""
import math
from PIL import Image, ImageDraw
from . import geom as G

WOOD = (196, 160, 110)
RED = (220, 20, 20)
PINK = (255, 175, 175)


def _iso(x, y, z, ang=math.radians(30)):
    u = (x - y) * math.cos(ang)
    v = (x + y) * math.sin(ang) - z
    depth = x + y + z
    return u, v, depth


def sample_part(p, step, with_rough=False, only_surface=True, finished_only=False):
    """Puncte (x,y,z,culoare). only_surface: pastreaza doar punctele cu cel putin un vecin 'gol' (mai rapid de desenat)."""
    rb = G.bbox(p.rough()) if with_rough else p.bbox()
    nx = max(2, int((rb[1] - rb[0]) / step)); ny = max(2, int((rb[3] - rb[2]) / step)); nz = max(2, int((rb[5] - rb[4]) / step))
    dx, dy, dz = (rb[1] - rb[0]) / nx, (rb[3] - rb[2]) / ny, (rb[5] - rb[4]) / nz
    cls = {}
    for i in range(nx):
        x = rb[0] + (i + .5) * dx
        for j in range(ny):
            y = rb[2] + (j + .5) * dy
            for k in range(nz):
                z = rb[4] + (k + .5) * dz
                pt = (x, y, z)
                if G.inside(p.blank, pt):
                    c = 1 if p.contains(pt) else 2
                    if c == 2 and finished_only:
                        continue
                    cls[(i, j, k)] = c
                elif with_rough and G.inside(p.rough(), pt):
                    cls[(i, j, k)] = 3
    out = []
    for (i, j, k), c in cls.items():
        if only_surface:
            nb = [(i + 1, j, k), (i - 1, j, k), (i, j + 1, k), (i, j - 1, k), (i, j, k + 1), (i, j, k - 1)]
            if all(n in cls and (cls[n] == c or (c == 1 and cls[n] == 1)) for n in nb):
                continue
        x = rb[0] + (i + .5) * dx; y = rb[2] + (j + .5) * dy; z = rb[4] + (k + .5) * dz
        out.append((x, y, z, {1: WOOD, 2: RED, 3: PINK}[c]))
    return out


def render(points, path, size=(1600, 1100), dot=3, title=None, shade=True):
    proj = [(_iso(x, y, z) + (col, z)) for (x, y, z, col) in points]
    us = [p[0] for p in proj]; vs = [p[1] for p in proj]
    u0, u1, v0, v1 = min(us), max(us), min(vs), max(vs)
    sc = min((size[0] - 80) / (u1 - u0 + 1e-9), (size[1] - 120) / (v1 - v0 + 1e-9))
    img = Image.new("RGB", size, "white")
    dr = ImageDraw.Draw(img)
    proj.sort(key=lambda p: p[2])
    zmin = min(p[4] for p in proj); zmax = max(p[4] for p in proj)
    for u, v, dep, col, z in proj:
        X = 40 + (u - u0) * sc; Y = 80 + (v - v0) * sc
        if shade:
            f = 0.75 + 0.25 * ((z - zmin) / (zmax - zmin + 1e-9))
            col = tuple(min(255, int(c * f)) for c in col)
        dr.rectangle([X - dot, Y - dot, X + dot, Y + dot], fill=col)
    if title:
        dr.text((40, 20), title, fill="black")
        dr.rectangle([40, 45, 55, 58], fill=WOOD); dr.text((60, 45), "lemn (piesa finita)", fill="black")
        dr.rectangle([220, 45, 235, 58], fill=RED); dr.text((240, 45), "DESEURI::Imbinari", fill="black")
        dr.rectangle([400, 45, 415, 58], fill=PINK); dr.text((420, 45), "DESEURI::Debitare (adaos)", fill="black")
    img.save(path)
    return path


def exploded(parts, step, gap=150.0):
    """Asaza piesele una langa alta (de-a lungul X), pentru vedere de detaliu cu deseurile."""
    pts = []
    xoff = 0.0
    for p in parts:
        s = sample_part(p, step, with_rough=True)
        bb = G.bbox(p.rough())
        for (x, y, z, c) in s:
            pts.append((x - bb[0] + xoff, y - bb[2], z - bb[4], c))
        xoff += (bb[1] - bb[0]) + gap
    return pts


def _rot(pt, rot):
    x, y, z = pt
    for _ in range((rot // 90) % 4):
        x, y = -y, x
    return x, y, z


def tile(part, clip, rot, step, size=(520, 420), title="", show_rough=False):
    """O imagine de detaliu: zona `clip` (bbox) dintr-o piesa, vazuta izometric dupa rotirea cu `rot` grade in jurul Z."""
    import copy
    q = copy.copy(part)
    rb = G.bbox(part.rough())
    lim = [max(rb[0], clip[0]), min(rb[1], clip[1]), max(rb[2], clip[2]), min(rb[3], clip[3]), max(rb[4], clip[4]), min(rb[5], clip[5])]
    rough = part.rough()
    q.rough_prim = ("__clip__",)
    pts = []
    nx = max(2, int((lim[1] - lim[0]) / step)); ny = max(2, int((lim[3] - lim[2]) / step)); nz = max(2, int((lim[5] - lim[4]) / step))
    dx, dy, dz = (lim[1] - lim[0]) / nx, (lim[3] - lim[2]) / ny, (lim[5] - lim[4]) / nz
    cls = {}
    for i in range(nx):
        x = lim[0] + (i + .5) * dx
        for j in range(ny):
            y = lim[2] + (j + .5) * dy
            for k in range(nz):
                z = lim[4] + (k + .5) * dz
                pt = (x, y, z)
                if G.inside(part.blank, pt):
                    cls[(i, j, k)] = 1 if part.contains(pt) else 2
                elif show_rough and G.inside(rough, pt):
                    cls[(i, j, k)] = 3
    for (i, j, k), c in cls.items():
        nb = [(i + 1, j, k), (i - 1, j, k), (i, j + 1, k), (i, j - 1, k), (i, j, k + 1), (i, j, k - 1)]
        if all(n in cls and cls[n] == c for n in nb):
            continue
        x = lim[0] + (i + .5) * dx; y = lim[2] + (j + .5) * dy; z = lim[4] + (k + .5) * dz
        pts.append(_rot((x, y, z), rot) + ({1: WOOD, 2: RED, 3: PINK}[c],))
    img = Image.new("RGB", size, "white")
    if not pts:
        return img
    proj = [(_iso(x, y, z) + (col, z)) for (x, y, z, col) in pts]
    us = [p[0] for p in proj]; vs = [p[1] for p in proj]
    u0, u1, v0, v1 = min(us), max(us), min(vs), max(vs)
    sc = min((size[0] - 30) / (u1 - u0 + 1e-9), (size[1] - 50) / (v1 - v0 + 1e-9))
    dot = max(1, int(step * sc / 2) + 1)
    dr = ImageDraw.Draw(img)
    proj.sort(key=lambda p: p[2])
    zmin = min(p[4] for p in proj); zmax = max(p[4] for p in proj)
    for u, v, dep, col, z in proj:
        X = 15 + (u - u0) * sc; Y = 40 + (v - v0) * sc
        f = 0.72 + 0.28 * ((z - zmin) / (zmax - zmin + 1e-9))
        dr.rectangle([X - dot, Y - dot, X + dot, Y + dot], fill=tuple(min(255, int(c * f)) for c in col))
    dr.rectangle([0, 0, size[0] - 1, size[1] - 1], outline=(180, 180, 180))
    dr.text((10, 10), title, fill="black")
    return img


def sheet(tiles, path, cols=3, header=""):
    w, h = tiles[0].size
    rows = (len(tiles) + cols - 1) // cols
    S = Image.new("RGB", (w * cols, h * rows + 70), "white")
    dr = ImageDraw.Draw(S)
    dr.text((15, 10), header, fill="black")
    dr.rectangle([15, 35, 30, 48], fill=WOOD); dr.text((35, 35), "lemn (piesa finita)", fill="black")
    dr.rectangle([190, 35, 205, 48], fill=RED); dr.text((210, 35), "DESEURI::Imbinari - ce scoate imbinarea", fill="black")
    dr.rectangle([470, 35, 485, 48], fill=PINK); dr.text((490, 35), "DESEURI::Debitare - adaos de la scandura bruta", fill="black")
    for i, t in enumerate(tiles):
        S.paste(t, ((i % cols) * w, 70 + (i // cols) * h))
    S.save(path)
    return path
