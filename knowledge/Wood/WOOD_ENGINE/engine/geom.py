"""WOOD ENGINE — nucleu geometric ca DATE PURE (fara Rhino).

Primitive (tupluri), toate in mm:
    ('box',   x0, x1, y0, y1, z0, z1)
    ('cyl',   axis, c1, c2, a0, a1, r)        axis 'x'|'y'|'z'; (c1, c2) = celelalte doua coordonate in ordinea xyz
    ('prism', axis, pts2d, a0, a1)            poligon in planul perpendicular pe axis, extrudat intre a0 si a1
                                              axis 'x' -> pts (y, z); 'y' -> (x, z); 'z' -> (x, y)

Principiul (skill ansamblu-piese-deseuri):
    piesa finita     = brut - uniune(scule)
    deseu (rosu)     = brut ∩ uniune(scule)
Aceleasi date se folosesc pentru verificare numerica (aici) si pentru geometria Rhino (rhino_out.py).
"""

AX = {"x": 0, "y": 1, "z": 2}
OTHER = {"x": (1, 2), "y": (0, 2), "z": (0, 1)}


def box(x0, x1, y0, y1, z0, z1):
    return ("box", min(x0, x1), max(x0, x1), min(y0, y1), max(y0, y1), min(z0, z1), max(z0, z1))


def cyl(axis, c1, c2, a0, a1, r):
    return ("cyl", axis, c1, c2, min(a0, a1), max(a0, a1), r)


def prism(axis, pts2d, a0, a1):
    return ("prism", axis, [tuple(p) for p in pts2d], min(a0, a1), max(a0, a1))


# ---------------------------------------------------------------- bbox
def bbox(p):
    k = p[0]
    if k == "box":
        return p[1:7]
    if k == "cyl":
        _, axis, c1, c2, a0, a1, r = p
        lo, hi = [0, 0, 0], [0, 0, 0]
        i, j = OTHER[axis]
        a = AX[axis]
        lo[a], hi[a] = a0, a1
        lo[i], hi[i] = c1 - r, c1 + r
        lo[j], hi[j] = c2 - r, c2 + r
        return (lo[0], hi[0], lo[1], hi[1], lo[2], hi[2])
    if k == "prism":
        _, axis, pts, a0, a1 = p
        i, j = OTHER[axis]
        a = AX[axis]
        lo, hi = [0, 0, 0], [0, 0, 0]
        lo[a], hi[a] = a0, a1
        lo[i], hi[i] = min(q[0] for q in pts), max(q[0] for q in pts)
        lo[j], hi[j] = min(q[1] for q in pts), max(q[1] for q in pts)
        return (lo[0], hi[0], lo[1], hi[1], lo[2], hi[2])
    raise ValueError(k)


def bb_union(bbs):
    bbs = list(bbs)
    return (min(b[0] for b in bbs), max(b[1] for b in bbs), min(b[2] for b in bbs),
            max(b[3] for b in bbs), min(b[4] for b in bbs), max(b[5] for b in bbs))


def bb_inter(a, b, eps=1e-6):
    r = (max(a[0], b[0]), min(a[1], b[1]), max(a[2], b[2]), min(a[3], b[3]), max(a[4], b[4]), min(a[5], b[5]))
    if r[1] - r[0] <= eps or r[3] - r[2] <= eps or r[5] - r[4] <= eps:
        return None
    return r


# ---------------------------------------------------------------- punct in primitiva
def point_in_poly(x, y, pts):
    inside = False
    n = len(pts)
    for k in range(n):
        x1, y1 = pts[k]
        x2, y2 = pts[(k + 1) % n]
        if (y1 > y) != (y2 > y):
            xc = x1 + (y - y1) * (x2 - x1) / (y2 - y1)
            if x < xc:
                inside = not inside
    return inside


def inside(p, pt, tol=0.0):
    """Punct in primitiva (inchisa; tol > 0 o micsoreaza — util pentru teste stricte)."""
    k = p[0]
    if k == "box":
        _, x0, x1, y0, y1, z0, z1 = p
        return (x0 + tol <= pt[0] <= x1 - tol and y0 + tol <= pt[1] <= y1 - tol and z0 + tol <= pt[2] <= z1 - tol)
    if k == "cyl":
        _, axis, c1, c2, a0, a1, r = p
        a = AX[axis]
        i, j = OTHER[axis]
        if not (a0 + tol <= pt[a] <= a1 - tol):
            return False
        return (pt[i] - c1) ** 2 + (pt[j] - c2) ** 2 <= (r - tol) ** 2
    if k == "prism":
        _, axis, pts, a0, a1 = p
        a = AX[axis]
        i, j = OTHER[axis]
        if not (a0 + tol <= pt[a] <= a1 - tol):
            return False
        return point_in_poly(pt[i], pt[j], pts)
    raise ValueError(k)


# ---------------------------------------------------------------- poligoane 2D
def clip_halfplane(pts, a, b, c):
    """Pastreaza partea poligonului cu a*u + b*v + c >= 0 (Sutherland–Hodgman)."""
    out = []
    n = len(pts)
    for k in range(n):
        P, Q = pts[k], pts[(k + 1) % n]
        fp, fq = a * P[0] + b * P[1] + c, a * Q[0] + b * Q[1] + c
        if fp >= 0:
            out.append(P)
        if (fp >= 0) != (fq >= 0):
            t = fp / (fp - fq)
            out.append((P[0] + t * (Q[0] - P[0]), P[1] + t * (Q[1] - P[1])))
    return out


def clip_poly(subject, clipper):
    """Intersectia a doua poligoane (clipper convex, orientare oarecare)."""
    out = list(subject)
    n = len(clipper)
    # orientare clipper
    area = sum(clipper[k][0] * clipper[(k + 1) % n][1] - clipper[(k + 1) % n][0] * clipper[k][1] for k in range(n))
    s = 1 if area > 0 else -1
    for k in range(n):
        if not out:
            break
        (x1, y1), (x2, y2) = clipper[k], clipper[(k + 1) % n]
        a, b = -(y2 - y1) * s, (x2 - x1) * s
        c = -(a * x1 + b * y1)
        out = clip_halfplane(out, a, b, c)
    return out


def poly_area(pts):
    n = len(pts)
    return abs(sum(pts[k][0] * pts[(k + 1) % n][1] - pts[(k + 1) % n][0] * pts[k][1] for k in range(n))) / 2


def poly_centroid(pts):
    n = len(pts)
    a = cx = cy = 0.0
    for k in range(n):
        x1, y1 = pts[k]
        x2, y2 = pts[(k + 1) % n]
        cr = x1 * y2 - x2 * y1
        a += cr
        cx += (x1 + x2) * cr
        cy += (y1 + y2) * cr
    a *= 0.5
    return (cx / (6 * a), cy / (6 * a))


def band(p0, p1, half_w, ext0=0.0, ext1=0.0):
    """Dreptunghi (banda) de latime 2*half_w in jurul segmentului p0->p1, prelungit cu ext0/ext1 la capete."""
    import math
    dx, dy = p1[0] - p0[0], p1[1] - p0[1]
    L = math.hypot(dx, dy)
    ux, uy = dx / L, dy / L
    nx, ny = -uy, ux
    a = (p0[0] - ux * ext0, p0[1] - uy * ext0)
    b = (p1[0] + ux * ext1, p1[1] + uy * ext1)
    return [(a[0] + nx * half_w, a[1] + ny * half_w), (b[0] + nx * half_w, b[1] + ny * half_w),
            (b[0] - nx * half_w, b[1] - ny * half_w), (a[0] - nx * half_w, a[1] - ny * half_w)]


def volume_box(b):
    return (b[1] - b[0]) * (b[3] - b[2]) * (b[5] - b[4])
