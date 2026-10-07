"""WOOD ENGINE — imbinarile ca functii: (piese, parametri) -> scule de taiere + feronerie + verificari.

Fiecare functie corespunde unui fisier data/joints/<ID>.json (parametri, reguli, operatii, scule).
Conventie: sculele care ies printr-o fata se prelungesc cu E dincolo de ea (fara fete coplanare).
"""
import math
from . import geom as G

BOLT_STD = [100, 120, 140, 150, 160, 180, 200]


def _axis_box(axis, arange, cross_axis, crange, zrange):
    """Cutie data prin intervale pe axa lunga a traversei ('x'/'y'), axa transversala si Z."""
    lo = {"x": None, "y": None, "z": zrange}
    lo[axis] = arange
    lo[cross_axis] = crange
    return G.box(lo["x"][0], lo["x"][1], lo["y"][0], lo["y"][1], lo["z"][0], lo["z"][1])


def _rng(a, b):
    return (min(a, b), max(a, b))


class _NodeTag:
    """Marcheaza cu numele nodului ('where') toate sculele adaugate pe piese in timpul unei imbinari."""
    def __init__(self, parts, where):
        self.parts, self.where = list(parts), where
    def __enter__(self):
        self.start = [len(p.cuts) for p in self.parts]
        return self
    def __exit__(self, *a):
        for p, s0 in zip(self.parts, self.start):
            for c in p.cuts[s0:]:
                c.note = self.where


# ------------------------------------------------------------------ KD-BB-01
def kd_bedbolt_stub(prj, rail, post, *, axis, face, d, rail_c, rail_z, post_c, post_outer, inner_sign,
                    z_tenon, z_bolt, P, where):
    """Cep scurt de aliniere + bulon de pat ascuns (traversa -> picior).

    axis       axa lunga a traversei ('x' sau 'y'); cross = cealalta axa orizontala
    face       coordonata fetei piciorului unde intra traversa
    d          +1/-1: directia dinspre fata spre interiorul piciorului
    rail_c     (c0, c1) banda traversei pe axa transversala;  rail_z = (z0, z1)
    post_c     (c0, c1) banda piciorului pe axa transversala (pentru peretii scobiturii)
    post_outer coordonata fetei exterioare a piciorului pe axa traversei (unde intra bulonul)
    inner_sign +1 daca fata interioara a traversei e rail_c[1], -1 daca e rail_c[0]
    z_tenon    (z0, z1) banda pe inaltime a cepului (decalata fata de traversa perpendiculara — TAMPLAR-P3)
    """
    with _NodeTag((rail, post), where):
        E = P["E"]
        cross = "y" if axis == "x" else "x"
        c0, c1 = rail_c
        T = c1 - c0
        t = round(T / 3.0)                       # JOINERY-MT-001
        cc = (c0 + c1) / 2.0
        tc0, tc1 = cc - t / 2.0, cc + t / 2.0
        L, fit = P["tenon_len"], P["fit"]
        zt0, zt1 = z_tenon
        rz0, rz1 = rail_z
        J, R = "KD-BB-01", ("JOINERY-MT-001", "FURN-BED-A", "BED-WH-001", "TAMPLAR-P3")

        # --- traversa: umerii cepului (brutul traversei include deja cepul pana la face + d*L)
        ar = _rng(face, face + d * (L + E))
        rail.add_cut(_axis_box(axis, ar, cross, (c0 - E, tc0), (rz0 - E, rz1 + E)), "umeri cep (taiere)", "circular_masa|fierastrau_cepuri", R, J)
        rail.add_cut(_axis_box(axis, ar, cross, (tc1, c1 + E), (rz0 - E, rz1 + E)), "umeri cep (taiere)", "circular_masa|fierastrau_cepuri", R, J)
        if zt0 > rz0:
            rail.add_cut(_axis_box(axis, ar, cross, (tc0 - E, tc1 + E), (rz0 - E, zt0)), "umeri cep (taiere)", "circular_masa|fierastrau_cepuri", R, J)
        if zt1 < rz1:
            rail.add_cut(_axis_box(axis, ar, cross, (tc0 - E, tc1 + E), (zt1, rz1 + E)), "umeri cep (taiere)", "circular_masa|fierastrau_cepuri", R, J)

        # --- picior: scobitura (cep + joc TOL-KD; +2 mm pe adancime)
        am = _rng(face - d * E, face + d * (L + 2))
        post.add_cut(_axis_box(axis, am, cross, (tc0 - fit, tc1 + fit), (zt0 - 0.5, zt1 + 0.5)),
                     "scobitura cep %dx%d adanc %d" % (t, zt1 - zt0, L + 2), "freza_mana+freza_spirala_10|dalta_10",
                     R + ("TOL-KD",), J)

        # --- bulon M8: gaura D9 prin picior + in capatul traversei; lamaj D20x12 pe fata exterioara; piulita D12
        rb, rn, rcb, cbd, nd = 4.5, 6.0, 10.0, 12.0, P["nut_dist"]
        a_post = _rng(post_outer + d * E, face - d * E)
        post.add_cut(G.cyl(axis, cc, z_bolt, a_post[0], a_post[1], rb), "gaura bulon D9 (strapunge piciorul)",
                     "masina_gaurit_coloana+burghiu_9_lung", ("FURN-BED-A",), J)
        a_cb = _rng(post_outer + d * E, post_outer - d * cbd)
        post.add_cut(G.cyl(axis, cc, z_bolt, a_cb[0], a_cb[1], rcb), "lamaj cap bulon D20x12 (pentru dop)",
                     "burghiu_forstner_20", ("BED-WH-001",), J)
        a_rail = _rng(face + d * (L + E), face - d * (nd + 15))
        rail.add_cut(G.cyl(axis, cc, z_bolt, a_rail[0], a_rail[1], rb), "gaura bulon D9 in capat (axial, %d mm)" % (nd + 15 + L),
                     "ghidaj_gaurire_lunga+burghiu_9_lung", ("FURN-BED-A",), J)
        c_in = c1 if inner_sign > 0 else c0
        cn = _rng(c_in + inner_sign * E, c_in - inner_sign * (T - 4))
        a_nut = face - d * nd
        rail.add_cut(G.cyl(cross, a_nut, z_bolt, cn[0], cn[1], rn), "locas piulita cilindrica D12 (orb, din fata interioara)",
                     "burghiu_12", ("FURN-BED-A",), J)

        # --- feronerie: lungime bulon standard
        need = abs(post_outer - face) - cbd + nd + rn + 4
        blen = next(b for b in BOLT_STD if b >= need)
        prj.add_hw("Bulon de pat M8 inox A2, cap cilindric", 1, "M8 x %d (necesar >= %.0f)" % (blen, need), where, ["FURN-BED-A", "WOOD-SP-001"])
        prj.add_hw("Piulita cilindrica (barrel nut) M8 inox", 1, "D12 x 20", where, ["FURN-BED-A"])
        prj.add_hw("Dop stejar D20 (ascunde capul bulonului)", 1, "D20 x 10, fibra in acelasi sens", where, ["BED-WH-001"])
        prj.use_joint(J, where)

        # --- verificari locale
        msgs = []
        wall = min(tc0 - fit - post_c[0], post_c[1] - (tc1 + fit))
        msgs.append(("perete scobitura >= 8 mm", wall >= 8, "%s: %.1f mm" % (where, wall)))
        msgs.append(("cep = T/3 (JOINERY-MT-001)", abs(t - T / 3.0) <= 0.5, "%s: cep %d / traversa %.0f" % (where, t, T)))
        msgs.append(("bulon in grosimea traversei (marja >= 8)", min(cc - rb - c0, c1 - cc - rb) >= 8, "%s: marja %.1f" % (where, min(cc - rb - c0, c1 - cc - rb))))
        msgs.append(("bulon in inaltimea traversei", rz0 + rn + 8 <= z_bolt <= rz1 - rn - 8, "%s: z=%.1f in [%.0f,%.0f]" % (where, z_bolt, rz0, rz1)))
        msgs.append(("cep in inaltimea traversei", rz0 <= zt0 < zt1 <= rz1, "%s: cep z %.0f-%.0f" % (where, zt0, zt1)))
        msgs.append(("piulita la >= 60 mm de capat (FURN-BED-A)", nd >= 60, "%s: %d mm" % (where, nd)))
        return dict(joint=J, bolt_len=blen, msgs=msgs, tenon=(t, zt1 - zt0, L))


# ------------------------------------------------------------------ ST-LOOSE-01
def stub_tenon_loose(prj, beam, rail, *, axis, face, d, beam_c, beam_z, z_tenon, P, where):
    with _NodeTag((beam, rail), where):
        E, L, fit = P["E"], P["tenon_len_beam"], P["fit"]
        cross = "y" if axis == "x" else "x"
        c0, c1 = beam_c
        T = c1 - c0
        t = round(T / 3.0)
        cc = (c0 + c1) / 2.0
        tc0, tc1 = cc - t / 2.0, cc + t / 2.0
        zt0, zt1 = z_tenon
        bz0, bz1 = beam_z
        J, R = "ST-LOOSE-01", ("JOINERY-MT-001", "FURN-BED-A")
        ar = _rng(face, face + d * (L + E))
        beam.add_cut(_axis_box(axis, ar, cross, (c0 - E, tc0), (bz0 - E, bz1 + E)), "umeri cep", "circular_masa|fierastrau_cepuri", R, J)
        beam.add_cut(_axis_box(axis, ar, cross, (tc1, c1 + E), (bz0 - E, bz1 + E)), "umeri cep", "circular_masa|fierastrau_cepuri", R, J)
        beam.add_cut(_axis_box(axis, ar, cross, (tc0 - E, tc1 + E), (bz0 - E, zt0)), "umeri cep", "circular_masa|fierastrau_cepuri", R, J)
        beam.add_cut(_axis_box(axis, ar, cross, (tc0 - E, tc1 + E), (zt1, bz1 + E)), "umeri cep", "circular_masa|fierastrau_cepuri", R, J)
        am = _rng(face - d * E, face + d * (L + 2))
        rail.add_cut(_axis_box(axis, am, cross, (tc0 - fit, tc1 + fit), (zt0 - 0.5, zt1 + 0.5)),
                     "scobitura oarba cep %dx%d adanc %d" % (t, zt1 - zt0, L + 2), "freza_mana+freza_spirala_10|dalta_10", R + ("TOL-KD",), J)
        prj.use_joint(J, where)
        return dict(joint=J, msgs=[("cep grinda = T/3", abs(t - T / 3.0) <= 0.5, "%s: %d/%.0f" % (where, t, T))])


# ------------------------------------------------------------------ HL-PEG-01
def poly_margin(pt, poly):
    """Distanta minima de la punct la marginile poligonului (pozitiva daca e in interior)."""
    best = 1e18
    n = len(poly)
    for k in range(n):
        (x1, y1), (x2, y2) = poly[k], poly[(k + 1) % n]
        dx, dy = x2 - x1, y2 - y1
        L2 = dx * dx + dy * dy
        u = max(0.0, min(1.0, ((pt[0] - x1) * dx + (pt[1] - y1) * dy) / L2)) if L2 else 0.0
        best = min(best, math.hypot(pt[0] - (x1 + u * dx), pt[1] - (y1 + u * dy)))
    return best if G.point_in_poly(pt[0], pt[1], poly) else -best


def half_lap_pegged(prj, A, B, *, axis, thick, keepA, keepB, cutA_poly, cutB_poly, region, pegs, P, where):
    """Injumatatire la orice unghi, in planul perpendicular pe `axis` (grosimea pieselor pe `axis`).

    keepA/keepB  'lo' sau 'hi' — jumatatea de grosime pe care o PASTREAZA fiecare piesa in zona comuna
    cutA_poly    conturul piesei B (prelungit in aer acolo unde ar coincide cu fete ale lui A) — taie A
    cutB_poly    conturul piesei A (idem) — taie B
    region       zona comuna A∩B (pentru verificarea cuielor)
    pegs         puncte (u, v) in plan pentru cuiele de lemn D12, axa = `axis`
    """
    with _NodeTag((A, B), where):
        E, r = P["E"], P["peg_d"] / 2.0
        t0, t1 = thick
        mid = (t0 + t1) / 2.0
        half = {"lo": (t0 - E, mid), "hi": (mid, t1 + E)}
        other = {"lo": "hi", "hi": "lo"}
        J, R = "HL-PEG-01", ("JOINERY-LAP-001",)
        a = half[other[keepA]]
        A.add_cut(G.prism(axis, cutA_poly, a[0], a[1]), "injumatatire (1/2 grosime pe zona comuna)",
                  "freza_mana+freza_dreapta_18|fierastrau_cepuri+dalta_25", R, J)
        b = half[other[keepB]]
        B.add_cut(G.prism(axis, cutB_poly, b[0], b[1]), "injumatatire (1/2 grosime pe zona comuna)",
                  "freza_mana+freza_dreapta_18|fierastrau_cepuri+dalta_25", R, J)
        msgs = [("jumatati complementare", keepA != keepB, where)]
        for (u, v) in pegs:
            for part in (A, B):
                part.add_cut(G.cyl(axis, u, v, t0 - E, t1 + E, r), "gaura cui de lemn D%d (impreuna, la montaj)" % P["peg_d"],
                             "bormasina+burghiu_12", ("JOINERY-MT-005", "JP-09"), J)
            m = poly_margin((u, v), region)
            msgs.append(("cui in zona injumatatirii (marja >= r+12)", m >= r + 12, "%s: cui (%.0f,%.0f) marja %.1f" % (where, u, v, m)))
            prj.add_hw("Cui de lemn stejar D%d" % P["peg_d"], 1, "D%d x %.0f, usor tesit" % (P["peg_d"], t1 - t0 + 6), where, ["JOINERY-MT-005"])
        prj.use_joint(J, where)
        return dict(joint=J, msgs=msgs)


# ------------------------------------------------------------------ RS-PEG-01
def ridge_seat_pegged(prj, ridge, rafters, *, ridge_box, peg_xy, peg_z, P, where):
    """Coama in furca capriorilor: locasul in capriori = volumul coamei; cui de lemn vertical."""
    with _NodeTag([ridge] + list(rafters), where):
        E, r = P["E"], P["peg_d"] / 2.0
        x0, x1, y0, y1, z0, z1 = ridge_box[1:7]
        J = "RS-PEG-01"
        seat = G.box(x0, x1, y0, y1, z0, z1 + E)
        for rf in rafters:
            rf.add_cut(seat, "locas coama (taiere)", "fierastrau_panglica|fierastrau_cepuri+dalta_25", ("DES-STR-001",), J)
        for (px, py) in peg_xy:
            c = G.cyl("z", px, py, peg_z, z1 + E, r)
            ridge.add_cut(c, "gaura cui de lemn D%d vertical" % P["peg_d"], "bormasina+burghiu_12", ("JOINERY-MT-005",), J)
            for rf in rafters:
                if G.bb_inter(rf.bbox(), G.bbox(c)):
                    rf.add_cut(c, "gaura cui de lemn D%d vertical" % P["peg_d"], "bormasina+burghiu_12", ("JOINERY-MT-005",), J)
            prj.add_hw("Cui de lemn stejar D%d" % P["peg_d"], 1, "D%d x %.0f" % (P["peg_d"], z1 - peg_z + 5), where, ["JOINERY-MT-005"])
        prj.use_joint(J, where)
        msgs = [("cui in sectiunea coamei (marja >= 15)", all(min(px - x0, x1 - px, py - y0, y1 - py) - r >= 15 for px, py in peg_xy), where)]
        return dict(joint=J, msgs=msgs)


# ------------------------------------------------------------------ MT-GL-01
def _xy_rect(axis, arange, crange):
    """Dreptunghi in planul XY (pentru prism 'z') dat prin intervale pe axa lunga si pe cea transversala."""
    if axis == "x":
        (x0, x1), (y0, y1) = arange, crange
    else:
        (y0, y1), (x0, x1) = arange, crange
    return [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]


def mt_glued(prj, rail, leg, *, axis, face, d, rail_c, rail_z, leg_c, z_tenon, haunch_depth, leg_top, mitre, P, where):
    """Cep-scobitura lipit cu haunch (deschis la capatul de sus al piciorului) si, optional, capatul cepului la 45 grade.

    mitre  None sau (a, b, c) normalizat: s = a*x + b*y + c; cepul acestei zargi PASTREAZA s >= gap,
           scobitura ei pastreaza s >= 0 (cealalta zarga foloseste semiplanul opus) — JOINERY-MT-167.
    """
    with _NodeTag((rail, leg), where):
        E, fit, L, gap = P["E"], P["fit_glue"], P["tenon_len_table"], P["mitre_gap"]
        cross = "y" if axis == "x" else "x"
        c0, c1 = rail_c
        T = c1 - c0
        t = round(T / 3.0)
        cc = (c0 + c1) / 2.0
        tc0, tc1 = cc - t / 2.0, cc + t / 2.0
        zt0, zt1 = z_tenon
        rz0, rz1 = rail_z
        J = "MT-GL-01"
        R = ("JOINERY-MT-001", "JOINERY-MT-004")
        ar = _rng(face, face + d * (L + E))
        tool_sh = "circular_masa|fierastrau_cepuri"
        rail.add_cut(_axis_box(axis, ar, cross, (c0 - E, tc0), (rz0 - E, rz1 + E)), "umeri cep (obraji)", tool_sh, R, J)
        rail.add_cut(_axis_box(axis, ar, cross, (tc1, c1 + E), (rz0 - E, rz1 + E)), "umeri cep (obraji)", tool_sh, R, J)
        rail.add_cut(_axis_box(axis, ar, cross, (tc0 - E, tc1 + E), (rz0 - E, zt0)), "umar cep jos", tool_sh, R, J)
        ah = _rng(face + d * haunch_depth, face + d * (L + E))
        rail.add_cut(_axis_box(axis, ah, cross, (tc0 - E, tc1 + E), (zt1, rz1 + E)), "umar sus + haunch %d mm" % haunch_depth, tool_sh, R, J)
        if mitre is not None:
            a, b, c = mitre
            zone = _xy_rect(axis, _rng(face - d * E, face + d * (L + E)), (tc0 - E, tc1 + E))
            off = G.clip_halfplane(zone, -a, -b, -c + gap)
            if len(off) >= 3:
                rail.add_cut(G.prism("z", off, zt0 - E, zt1 + E), "capat cep la 45 grade (joc %.0f mm)" % gap,
                             "fierastrau_cepuri|fierastrau_panglica", ("JOINERY-MT-167",), J)
        am = _rng(face - d * E, face + d * (L + 2))
        mrect = _xy_rect(axis, am, (tc0 - fit, tc1 + fit))
        if mitre is not None:
            a, b, c = mitre
            mrect = G.clip_halfplane(mrect, a, b, c)
        leg.add_cut(G.prism("z", mrect, zt0 - 0.5, zt1 + 0.5),
                    "scobitura cep %dx%d adanc %d%s" % (t, zt1 - zt0, L + 2, " (capat 45 grade)" if mitre else ""),
                    "freza_mana+freza_spirala_10|dalta_10", R + ("TOL-GLUE",) + (("JOINERY-MT-167",) if mitre else ()), J)
        ahh = _rng(face - d * E, face + d * (haunch_depth + 1))
        leg.add_cut(_axis_box(axis, ahh, cross, (tc0 - fit, tc1 + fit), (zt1 - 0.5, leg_top + E)), "locas haunch (deschis sus)",
                    "fierastrau_cepuri+dalta_10", ("JOINERY-MT-004",), J)
        prj.add_hw("Adeziv PVAc D3 (EN 204)", 1, "pe obrajii cepului si peretii scobiturii; strangere 0,7-1,7 MPa", where, ["ADH-D3"])
        prj.use_joint(J, where)
        wall = min(tc0 - fit - leg_c[0], leg_c[1] - (tc1 + fit))
        msgs = [("cep = T/3 (JOINERY-MT-001)", abs(t - T / 3.0) <= 0.5, "%s: cep %d / zarga %.0f" % (where, t, T)),
                ("perete scobitura >= 8 mm", wall >= 8, "%s: %.1f mm" % (where, wall)),
                ("lemn deasupra cepului (haunch) >= 10 mm", leg_top - zt1 >= 10, "%s: %.0f mm" % (where, leg_top - zt1)),
                ("cep in inaltimea zargii", rz0 < zt0 < zt1 < rz1, "%s: %.0f-%.0f" % (where, zt0, zt1))]
        return dict(joint=J, msgs=msgs, tenon=(t, zt1 - zt0, L))


# ------------------------------------------------------------------ BTN-01
def buttons_in_groove(prj, apron, *, axis, a_range, inner_face, s, top_z, n, move_across, P, where, code="K"):
    """Nut oprit pe fata interioara a zargii + n butoni de lemn (piese noi) care prind blatul.

    axis        axa zargii; inner_face = coordonata fetei interioare pe axa transversala; s = +1/-1 spre interiorul mesei
    move_across miscarea totala sezoniera a blatului perpendicular pe zarga (zargile lungi); 0 la zargile scurte
    """
    from .model import Part
    E = P["E"]
    gw, gd, gtop = 10.0, 14.0, 12.0
    tg, eng, bw, bl, bh = 8.0, 10.0, 30.0, 40.0, 24.0
    J = "BTN-01"
    cross = "y" if axis == "x" else "x"
    gz1 = top_z - gtop
    gz0 = gz1 - gw
    half = move_across / 4.0      # o margine se misca dW/2 in tot sezonul; montat la umiditate medie -> +/- dW/4
    gap_body = max(2.0, float(math.ceil(half + 1.0)))
    a0, a1 = a_range
    btns = []
    with _NodeTag((apron,), where):
        apron.add_cut(_axis_box(axis, (a0, a1), cross, _rng(inner_face - s * gd, inner_face + s * E), (gz0, gz1)),
                      "nut oprit %dx%d pentru butoni" % (gw, gd), "freza_mana+freza_spirala_10", ("TBL-BTN",), J)
    first = not any(p.code == code for p in prj.parts)
    pitch = (a1 - a0 - bw) / (n - 1) if n > 1 else 0.0
    for k in range(n):
        ac = a0 + bw / 2.0 + k * pitch if n > 1 else (a0 + a1) / 2.0
        cr = _rng(inner_face - s * eng, inner_face + s * (gap_body + bl))
        blank = _axis_box(axis, (ac - bw / 2, ac + bw / 2), cross, cr, (top_z - bh, top_z))
        b = Part(code, "Buton fixare blat", "stejar", blank, (bw, bl + gap_body + eng, bh), (2, 2, 10),
                 rules=["TBL-BTN"], layout=(first and k == 0),
                 notes="lamba 8 mm intra %d mm in nut; joc %d mm fata de zarga (miscarea blatului)" % (eng, gap_body))
        prj.add(b)
        tz = (gz0 + (gw - tg) / 2.0, gz1 - (gw - tg) / 2.0)
        cz = _rng(inner_face - s * (eng + E), inner_face + s * gap_body)
        with _NodeTag((b,), where):
            b.add_cut(_axis_box(axis, (ac - bw / 2 - E, ac + bw / 2 + E), cross, cz, (top_z - bh - E, tz[0])),
                      "falt sub lamba", "circular_masa|fierastrau_cepuri", ("TBL-BTN",), J)
            b.add_cut(_axis_box(axis, (ac - bw / 2 - E, ac + bw / 2 + E), cross, cz, (tz[1], top_z + E)),
                      "falt peste lamba", "circular_masa|fierastrau_cepuri", ("TBL-BTN",), J)
            sc = inner_face + s * (gap_body + bl / 2.0)
            hole = G.cyl("z", ac, sc, top_z - bh - E, top_z + E, 2.5) if axis == "x" else G.cyl("z", sc, ac, top_z - bh - E, top_z + E, 2.5)
            b.add_cut(hole, "gaura surub D5 (trecere)", "bormasina+burghiu_4", ("FAST-001",), J)
        btns.append(b)
        prj.add_hw("Surub inox A2 4 x 35 (buton -> blat)", 1, "gaura de ghidare D3 in blat (90 % — FAST-001)", where, ["FAST-001", "WOOD-SP-001"])
    prj.use_joint(J, where)
    msgs = [("buton: angajare in nut dupa contractie >= 5 mm", eng - half >= 5, "%s: %.1f mm (margine +/-%.1f)" % (where, eng - half, half)),
            ("buton: joc fata de zarga >= umflare", gap_body >= half, "%s: joc %.0f >= %.1f" % (where, gap_body, half)),
            ("buton: lamba nu atinge fundul nutului la umflare", (gd - eng) >= half, "%s: %.0f mm liber" % (where, gd - eng)),
            ("butoni: distanta <= 400 mm", pitch <= 400 or n == 1, "%s: %.0f mm" % (where, pitch))]
    return dict(joint=J, msgs=msgs, buttons=btns)
