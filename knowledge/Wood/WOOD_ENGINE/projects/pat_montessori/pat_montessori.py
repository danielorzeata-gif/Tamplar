#! python 3
"""WOOD ENGINE — PILOT: Pat Montessori tip 'casuta', stejar, saltea 1400 x 2000, demontabil, fara feronerie vizibila.

Rulare:
  * Python normal  -> verificari numerice + out/pat_montessori.xlsx + out/RAPORT.md (fara Rhino)
  * Rhino 8 (ScriptEditor, Python 3) -> ANSAMBLU / PIESE / DESEURI::Imbinari (rosu) / DESEURI::Debitare (rosu deschis)
Toate cotele in mm. Fiecare piesa si fiecare taietura poarta regulile din data/rules.json ("De ce?").
"""
import os, sys, math

# ------------------------------------------------------------------ cale catre WOOD_ENGINE
try:
    HERE = os.path.dirname(os.path.abspath(__file__))
except NameError:
    HERE = r"C:\Users\Orzi\Desktop\General\Wood\WOOD_ENGINE\projects\pat_montessori"
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
if ROOT not in sys.path:
    sys.path.insert(0, ROOT)
for m in [k for k in list(sys.modules) if k == "engine" or k.startswith("engine.")]:
    del sys.modules[m]          # re-rulare curata in Rhino dupa modificari in engine/
from engine import geom as G
from engine.model import Project, Part
from engine import joints as JN
from engine import checks as CK
from engine import reports as RP
from engine import components as CP

# ================================================================== PARAMETRI (editabili)
MAT          = "stejar"
SALTEA_W     = 1400      # Y
SALTEA_L     = 2000      # X
JOC_SALTEA   = 10        # mm in plus pe fiecare dimensiune (cadru interior)
SALTEA_H     = 120       # grosime saltea (pentru verificarea inaltimii Montessori)
POST         = 70        # picior/stalp 70 x 70           (FURN-BED-SECT)
H_POST       = 1050      # inaltimea stalpilor (streasina)
H_TOTAL      = 1450      # varful coamei
RAIL_T, RAIL_H, RAIL_Z0 = 30, 150, 40      # lonjeroane si traverse 30 x 150, la 40 mm de podea (FURN-BED-SECT)
TENON_LEN    = 20        # cep scurt de aliniere              (FURN-BED-A)
FIT          = 0.25      # joc cep / parte                    (TOL-KD)
NUT_DIST     = 60        # piulita cilindrica de la capat     (FURN-BED-A)
Z_TENON_SIDE = (45, 80)  # banda cep lonjeron  (decalata)     (TAMPLAR-P3)
Z_BOLT_SIDE  = 165
Z_TENON_END  = (110, 145)  # banda cep traversa de capat
Z_BOLT_END   = 95
BEAM_W, BEAM_H = 45, 100   # grinda centrala                  (FURN-BED-SECT, FURN-BED-BEAM)
TENON_LEN_BEAM = 15
Z_TENON_BEAM = (60, 120)
FOOT_H       = 40
CLEAT_T, CLEAT_H, CLEAT_Z1 = 25, 40, 140    # sipca (FURN-BED-CLEAT); lamelele stau la Z = 140
CLEAT_X0     = 90        # sipca incepe dupa locasul piulitei (acces la montaj)
SLAT_T, SLAT_W, SLAT_GAP_MAX = 20, 80, 60  # lamele (FURN-BED-SLATS)
SLAT_X0      = 100
RAFTER       = 70        # sectiune caprior 70 x 70
RAFTER_EXT   = 130       # capriorii trec de incrucisare (capete in X deasupra coamei)
RIDGE        = 70
RIDGE_OVER   = 60        # coama iese peste fronton
PEG_D        = 12        # cui de lemn                        (JOINERY-MT-005)
E            = 2.5       # TOLERANTA_TAIERE: prelungirea sculelor dincolo de fete
ROUGH        = (4, 4, 30)  # adaos brut gros/lat/lung         (CON-DIM-004)

P = dict(E=E, tenon_len=TENON_LEN, fit=FIT, nut_dist=NUT_DIST, tenon_len_beam=TENON_LEN_BEAM, peg_d=PEG_D)

# ================================================================== COTE DERIVATE
IN_W = SALTEA_W + JOC_SALTEA          # Y interior 0 .. IN_W
IN_L = SALTEA_L + JOC_SALTEA          # X interior 0 .. IN_L
RZ = (RAIL_Z0, RAIL_Z0 + RAIL_H)
GX = {"st": (-POST, 0.0), "dr": (IN_L, IN_L + POST)}       # frontoane (X)
GY = {"fata": (-POST, 0.0), "spate": (IN_W, IN_W + POST)}  # randuri de stalpi (Y)
YC = IN_W / 2.0
ZR = H_TOTAL - RIDGE                   # baza coamei
ZX = ZR - 12                           # incrucisarea axelor capriorilor (sub coama)
FOOT_X = [IN_L / 3.0, 2 * IN_L / 3.0]


def rafter_axis(side):
    """Axa capriorului in planul YZ: de la axa stalpului (sub streasina) la incrucisare."""
    if side == "fata":
        return (-POST / 2.0, H_POST - POST / 2.0), (YC, ZX)
    return (IN_W + POST / 2.0, H_POST - POST / 2.0), (YC, ZX)


def rafter_polys(side):
    p0, p1 = rafter_axis(side)
    full = G.band(p0, p1, RAFTER / 2.0, ext0=POST * 1.6, ext1=RAFTER_EXT)
    if side == "fata":
        clipped = G.clip_halfplane(full, 1, 0, POST)              # Y >= -POST (taietura verticala la fata stalpului)
    else:
        clipped = G.clip_halfplane(full, -1, 0, IN_W + POST)      # Y <= IN_W + POST
    rough = G.band(p0, p1, RAFTER / 2.0 + 2, ext0=POST * 1.6 + 15, ext1=RAFTER_EXT + 15)
    return full, clipped, rough


def best_pegs(region, n, r, extra=None, grid=5.0):
    """Alege n puncte in zona comuna cu marja >= r + 12, cat mai departate (si conditia extra)."""
    ys = [q[0] for q in region]; zs = [q[1] for q in region]
    cands = []
    y = min(ys)
    while y <= max(ys):
        z = min(zs)
        while z <= max(zs):
            m = JN.poly_margin((y, z), region)
            if m >= r + 12 and (extra is None or extra(y, z)):
                cands.append(((y, z), m))
            z += grid
        y += grid
    assert cands, "nu exista loc pentru cuie in zona comuna"
    if n == 1:
        return [max(cands, key=lambda c: c[1])[0]]
    best, bd = None, -1
    for i in range(len(cands)):
        for j in range(i + 1, len(cands)):
            d = math.dist(cands[i][0], cands[j][0])
            if d > bd:
                bd, best = d, [cands[i][0], cands[j][0]]
    return best


# ================================================================== CONSTRUCTIE
def build():
    prj = Project("Pat Montessori casuta %dx%d %s" % (SALTEA_W, SALTEA_L, MAT))
    log = prj.checks_log
    R_SECT = ["FURN-BED-SECT"]

    # --- stalpi (A)
    posts = {}
    for gx in ("st", "dr"):
        for gy in ("fata", "spate"):
            x0, x1 = GX[gx]; y0, y1 = GY[gy]
            posts[(gx, gy)] = CP.post(prj, "A", "Stalp", MAT, x0, y0, POST, POST, H_POST, rules=R_SECT, notes="picior + stalp casuta dintr-o bucata")

    # --- lonjeroane (B) de-a lungul X, cu cepuri incluse in brut
    side = {}
    for gy, (c0, c1, inner) in {"fata": (-RAIL_T, 0.0, +1), "spate": (IN_W, IN_W + RAIL_T, -1)}.items():
        p = CP.rail(prj, "B", "Lonjeron", MAT, "x", 0.0, IN_L, c0, c1, RZ[0], RZ[1], tenons=(TENON_LEN, TENON_LEN), rules=R_SECT + ["FURN-BED-A"])
        side[gy] = (p, (c0, c1), inner)
    # --- traverse de capat (C) de-a lungul Y
    endr = {}
    for gx, (c0, c1, inner) in {"st": (-RAIL_T, 0.0, +1), "dr": (IN_L, IN_L + RAIL_T, -1)}.items():
        p = CP.rail(prj, "C", "Traversa capat", MAT, "y", 0.0, IN_W, c0, c1, RZ[0], RZ[1], tenons=(TENON_LEN, TENON_LEN), rules=R_SECT + ["FURN-BED-A"])
        endr[gx] = (p, (c0, c1), inner)

    res = []
    # --- KD-BB-01: lonjeroane -> stalpi (axa x)
    for gy, (rail, rc, inner) in side.items():
        for gx, face, d, outer in (("st", 0.0, -1, -POST), ("dr", IN_L, +1, IN_L + POST)):
            res.append(JN.kd_bedbolt_stub(prj, rail, posts[(gx, gy)], axis="x", face=face, d=d, rail_c=rc, rail_z=RZ,
                                          post_c=GY[gy], post_outer=outer, inner_sign=inner, z_tenon=Z_TENON_SIDE,
                                          z_bolt=Z_BOLT_SIDE, P=P, where="lonjeron %s -> stalp %s-%s" % (gy, gx, gy)))
    # --- KD-BB-01: traverse -> stalpi (axa y)
    for gx, (rail, rc, inner) in endr.items():
        for gy, face, d, outer in (("fata", 0.0, -1, -POST), ("spate", IN_W, +1, IN_W + POST)):
            res.append(JN.kd_bedbolt_stub(prj, rail, posts[(gx, gy)], axis="y", face=face, d=d, rail_c=rc, rail_z=RZ,
                                          post_c=GX[gx], post_outer=outer, inner_sign=inner, z_tenon=Z_TENON_END,
                                          z_bolt=Z_BOLT_END, P=P, where="traversa %s -> stalp %s-%s" % (gx, gx, gy)))

    # --- grinda centrala (D) + picioare centrale (E)
    bz = (FOOT_H, FOOT_H + BEAM_H)
    beam = CP.rail(prj, "D", "Grinda centrala", MAT, "x", 0.0, IN_L, YC - BEAM_W / 2, YC + BEAM_W / 2, bz[0], bz[1],
                   tenons=(TENON_LEN_BEAM, TENON_LEN_BEAM), rules=["FURN-BED-SECT", "FURN-BED-BEAM"])
    for gx, face, d in (("st", 0.0, -1), ("dr", IN_L, +1)):
        res.append(JN.stub_tenon_loose(prj, beam, endr[gx][0], axis="x", face=face, d=d, beam_c=(YC - BEAM_W / 2, YC + BEAM_W / 2),
                                       beam_z=bz, z_tenon=Z_TENON_BEAM, P=P, where="grinda -> traversa %s" % gx))
    feet = []
    for fx in FOOT_X:
        f = CP.post(prj, "E", "Picior central", MAT, fx - POST / 2, YC - POST / 2, POST, POST, FOOT_H, rules=["FURN-BED-BEAM"], rough=(4, 4, 10))
        feet.append(f)
        peg = G.cyl("z", fx, YC, FOOT_H - 25, bz[1] + E, PEG_D / 2)
        for part in (f, beam):
            part.add_cut(peg, "gaura cui de lemn D12 vertical (grinda + picior)", "bormasina+burghiu_12", ["JOINERY-MT-005"], "PEG", "picior central x=%.0f" % fx)
        prj.add_hw("Cui de lemn stejar D12", 1, "D12 x %d" % (BEAM_H + 25), "grinda -> picior central", ["JOINERY-MT-005"])

    # --- sipci (F) pe fata interioara a lonjeroanelor
    for gy, (y0, y1) in {"fata": (0.0, CLEAT_T), "spate": (IN_W - CLEAT_T, IN_W)}.items():
        CP.board(prj, "F", "Sipca lamele", MAT, CLEAT_X0, IN_L - CLEAT_X0, y0, y1, CLEAT_Z1 - CLEAT_H, CLEAT_Z1, rules=["FURN-BED-CLEAT"],
                 notes="lipita (PVAc D3) + suruburi inox 4x50 la 180 mm, din interior")
    n_scr = 2 * (int((IN_L - 2 * CLEAT_X0) / 180) + 1)
    prj.add_hw("Surub inox A2 4 x 50 (sipci)", n_scr, "cap ingropat, gaura de ghidare 90 % (FAST-001)", "sipci -> lonjeroane", ["FAST-001", "WOOD-SP-001"])

    # --- lamele (G)
    span = (IN_L - SLAT_X0) - SLAT_X0
    n = math.ceil((span + SLAT_GAP_MAX) / (SLAT_W + SLAT_GAP_MAX))
    gap = (span - n * SLAT_W) / (n - 1)
    for k in range(n):
        x = SLAT_X0 + k * (SLAT_W + gap)
        CP.board(prj, "G", "Lamela", MAT, x, x + SLAT_W, 2.0, IN_W - 2.0, CLEAT_Z1, CLEAT_Z1 + SLAT_T,
                 layout=(k == 0), qty_label="x%d" % n, rules=["FURN-BED-SLATS"])
    prj.add_hw("Surub inox A2 4 x 40 (lamele, cate 1 la fiecare capat)", 2 * n, "gaura ovala in lamela (miscare)", "lamele -> sipci", ["FURN-BED-SLATS", "WOOD-MOIST-005"])
    log.append(("interspatiu lamele <= %d mm (FURN-BED-SLATS)" % SLAT_GAP_MAX, gap <= SLAT_GAP_MAX, "%d lamele, gol %.1f mm" % (n, gap)))

    # --- capriori (H) — prisme in planul YZ, grosime pe X
    rafters = {}
    for gx in ("st", "dr"):
        x0, x1 = GX[gx]
        for sd in ("fata", "spate"):
            p0, p1 = rafter_axis(sd)
            clip = (1, 0, POST) if sd == "fata" else (-1, 0, IN_W + POST)   # taietura verticala la fata stalpului
            part, full, clipped = CP.inclined_member(prj, "H", "Caprior", MAT, "x", x0, x1, p0, p1, RAFTER / 2.0,
                                                     ext0=POST * 1.6, ext1=RAFTER_EXT, clips=[clip], rules=["FURN-BED-SECT"],
                                                     notes="capete: taietura verticala la fata stalpului; capatul de sus trece de coama")
            rafters[(gx, sd)] = (part, full, clipped)
        ang = math.degrees(math.atan2(ZX - (H_POST - POST / 2.0), YC + POST / 2.0))
        log.append(("panta capriorilor 15-45 grade", 15 <= ang <= 45, "%.1f grade" % ang))

    # --- HL-PEG-01: stalp <-> caprior  si  caprior <-> caprior (coama)
    keep_outer = {"st": "lo", "dr": "hi"}
    inv = {"lo": "hi", "hi": "lo"}
    for gx in ("st", "dr"):
        x0, x1 = GX[gx]
        for sd in ("fata", "spate"):
            rf, full, clipped = rafters[(gx, sd)]
            post = posts[(gx, sd)]
            py0, py1 = GY[sd]
            post_rect = [(py0, 0.0), (py1, 0.0), (py1, H_POST), (py0, H_POST)]
            ext = -E if sd == "fata" else E
            post_rect_cut = [(py0 + (ext if sd == "fata" else 0), -E), (py1 + (0 if sd == "fata" else ext), -E),
                             (py1 + (0 if sd == "fata" else ext), H_POST), (py0 + (ext if sd == "fata" else 0), H_POST)]
            region = G.clip_poly(clipped, post_rect)
            pegs = best_pegs(region, 2, PEG_D / 2.0)
            res.append(JN.half_lap_pegged(prj, post, rf, axis="x", thick=(x0, x1), keepA=keep_outer[gx], keepB=inv[keep_outer[gx]],
                                          cutA_poly=full, cutB_poly=post_rect_cut, region=region, pegs=pegs, P=P,
                                          where="stalp %s-%s <-> caprior" % (gx, sd)))
        (rA, fA, cA), (rB, fB, cB) = rafters[(gx, "fata")], rafters[(gx, "spate")]
        region = G.clip_poly(cA, cB)
        region = G.clip_halfplane(region, 0, -1, ZR)          # deasupra ZR lemnul e scos de locasul coamei
        # cuiul orizontal nu are voie sa intalneasca cuiul vertical al coamei (axa y = YC, coboara pana la ZR - 20)
        peg = best_pegs(region, 1, PEG_D / 2.0,
                        extra=lambda y, z: abs(y - YC) >= PEG_D + 4 or z <= ZR - 20 - PEG_D / 2.0 - 4)
        res.append(JN.half_lap_pegged(prj, rA, rB, axis="x", thick=(x0, x1), keepA=keep_outer[gx], keepB=inv[keep_outer[gx]],
                                      cutA_poly=fB, cutB_poly=fA, region=region, pegs=peg, P=P, where="coama %s: caprior <-> caprior" % gx))

    # --- coama (I) + RS-PEG-01
    rb = G.box(-POST - RIDGE_OVER, IN_L + POST + RIDGE_OVER, YC - RIDGE / 2, YC + RIDGE / 2, ZR, ZR + RIDGE)
    ridge = CP.board(prj, "I", "Coama", MAT, rb[1], rb[2], rb[3], rb[4], rb[5], rb[6], rules=["FURN-BED-SECT", "DES-STR-001"])
    res.append(JN.ridge_seat_pegged(prj, ridge, [v[0] for v in rafters.values()], ridge_box=rb,
                                    peg_xy=[((GX[g][0] + GX[g][1]) / 2.0, YC) for g in ("st", "dr")], peg_z=ZR - 20, P=P,
                                    where="coama -> capriori"))

    # --- dopuri (J) — piese mici, fara gauri globale
    n_bolts = sum(1 for r in res if r["joint"] == "KD-BB-01")
    prj.add(Part("J", "Dop cap bulon", "lemn_dop", G.box(-POST - 40, -POST - 30, -40, -20, 0, 10), (10, 20, 20), (0, 0, 0),
                 drill=False, layout=True, qty_label="x%d" % n_bolts, rules=["BED-WH-001"], notes="D20 x 10, fibra in sensul piciorului; NU se lipeste (demontare)"))
    prj.parts[-1].blank = G.cyl("x", -30, 5, -POST - 40, -POST - 30, 10)
    prj.parts[-1].layout_only = True
    prj.parts[-1].qty = n_bolts

    # --- verificari locale ale imbinarilor
    for r in res:
        for (t, ok, dtl) in r["msgs"]:
            log.append((t, ok, dtl))

    # --- cote globale
    log.append(("spatiu interior = saltea + joc (Y)", abs((IN_W - 0) - (SALTEA_W + JOC_SALTEA)) < 0.01, "%.0f mm" % IN_W))
    log.append(("spatiu interior = saltea + joc (X)", abs(IN_L - (SALTEA_L + JOC_SALTEA)) < 0.01, "%.0f mm" % IN_L))
    log.append(("inaltime totala = varful coamei", abs(ridge.bbox()[5] - H_TOTAL) < 0.01, "%.0f mm" % ridge.bbox()[5]))
    top = CLEAT_Z1 + SLAT_T + SALTEA_H
    log.append(("pat de podea Montessori: saltea sus la 200-320 mm (MONT-001)", 200 <= top <= 320, "%d mm" % top))
    log.append(("marginea lonjeronului peste lamele >= 20 mm (FURN-BED-CLEAT)", RZ[1] - (CLEAT_Z1 + SLAT_T) >= 20, "%d mm" % (RZ[1] - CLEAT_Z1 - SLAT_T)))
    mv = RAIL_H * 0.00376 * 4
    log.append(("miscare lonjeron pe inaltime (dUM 4 %%, WOOD-MOIST-005) < 3 mm", mv < 3, "%.1f mm" % mv))

    prj.assembly = [
        ("Montaj uscat complet la atelier, fara adeziv; numeroteaza fiecare nod (ex. 'st-fata').", "-"),
        ("Pune picioarele centrale (E) si grinda (D) pe pozitie, cu cuiele de lemn D12.", "ciocan de lemn"),
        ("Traversa de capat stanga (C) intre stalpii st-fata si st-spate: cep in scobitura, bulon M8 prin stalp in piulita cilindrica; strange pana umerii stau plan.", "cheie imbus / surubelnita lunga"),
        ("Idem traversa dreapta; grinda (D) intra cu cepurile in traverse.", "cheie imbus"),
        ("Lonjeroanele (B) intre stalpii de pe aceeasi parte; buloane M8; verifica diagonalele cadrului de jos (egale).", "ruleta, cheie imbus"),
        ("Dopurile D20 in lamaje (fara adeziv, la presiune) — feroneria nu se vede.", "ciocan de lemn"),
        ("Sipcile (F) sunt deja lipite si insurubate pe lonjeroane (la atelier). Asaza lamelele (G), cate 1 surub la fiecare capat, in gaura ovala.", "surubelnita"),
        ("Capriorii (H) pe stalpi: injumatatire peste injumatatire, cuie de lemn D12 batute din exterior.", "ciocan de lemn"),
        ("La coama: capriorii se incruciseaza injumatatit; cui D12 orizontal.", "ciocan de lemn"),
        ("Coama (I) se lasa in furca capriorilor; cui D12 vertical la fiecare fronton.", "ciocan de lemn"),
        ("Verificare finala: cadru in echer, nicio piesa nu se misca; re-strange buloanele dupa 1-2 luni (lemnul se aseaza).", "cheie imbus"),
    ]
    return prj


def run_checks(prj):
    prj.apply_globals()
    col = CK.collisions([p for p in prj.parts if not getattr(p, "layout_only", False)])
    prj.checks_log.append(("fara suprapuneri intre piese finite", not col, "; ".join("%s %s x %s %s (%d puncte, ex. %s)" % (a, an, b, bn, n, tuple(round(v, 1) for v in h)) for a, an, b, bn, n, h in col) or "OK: nicio suprapunere"))
    allow = {frozenset(("HL-PEG-01", "RS-PEG-01")): "coama (RS-PEG-01) e proiectata sa stea pe incrucisarea injumatatita a capriorilor; cuiul ei vertical prinde intentionat ambele jumatati"}
    for k, why in allow.items():
        prj.checks_log.append(("suprapunere permisa: " + " + ".join(sorted(k)), True, why))
    cc = CK.cut_conflicts(prj.parts, allow=set(allow))
    prj.checks_log.append(("imbinari diferite nu se intersecteaza in aceeasi piesa", not cc, "; ".join("%s %s: [%s | %s] x [%s | %s]" % c for c in cc) or "OK"))
    lonely = CK.supported([p for p in prj.parts if not getattr(p, "layout_only", False)])
    prj.checks_log.append(("fiecare piesa atinge cel putin o alta", not lonely, ", ".join(lonely) or "OK"))
    return prj


def outputs(prj):
    rules_db = RP.load_json(os.path.join(ROOT, "data", "rules.json"))
    inv = RP.load_json(os.path.join(ROOT, "data", "tools_inventory.json"))["tools"]
    T = RP.build_tables(prj, rules_db, inv)
    out = os.path.join(HERE, "out")
    os.makedirs(out, exist_ok=True)
    RP.write_xlsx(os.path.join(out, "pat_montessori.xlsx"), T, prj)
    RP.write_csv(os.path.join(out, "pat_montessori_debitare.csv"), T)
    return T


if __name__ == "__main__" or "Rhino" in sys.modules:
    PRJ = run_checks(build())
    try:
        import Rhino  # noqa
        IN_RHINO = True
    except ImportError:
        IN_RHINO = False
    if IN_RHINO:
        from engine import rhino_out
        rhino_out.build_in_rhino(PRJ, os.path.join(ROOT, "data", "materials.json"))
        try:
            T = outputs(PRJ)          # xlsx necesita openpyxl in Python-ul din Rhino; altfel ramane CSV-ul
            print("Lista de debitare: " + os.path.join(HERE, "out"))
        except Exception as ex:
            rules_db = RP.load_json(os.path.join(ROOT, "data", "rules.json"))
            inv = RP.load_json(os.path.join(ROOT, "data", "tools_inventory.json"))["tools"]
            T = RP.build_tables(PRJ, rules_db, inv)
            os.makedirs(os.path.join(HERE, "out"), exist_ok=True)
            RP.write_csv(os.path.join(HERE, "out", "pat_montessori_debitare.csv"), T)
            print("xlsx nescris in Rhino (%s); CSV scris in out/" % ex)
        bad = [c for c in PRJ.checks_log if not c[1]]
        print("Verificari:", "TOATE TREC" if not bad else "%d ESUATE: %s" % (len(bad), bad))
    else:
        T = outputs(PRJ)
        bad = [c for c in PRJ.checks_log if not c[1]]
        for t, ok, d in PRJ.checks_log:
            print(("OK   " if ok else "FAIL ") + t + " — " + d)
        print("\nPiese: %d | Feronerie: %d pozitii | Operatii: %d | Reguli folosite: %d | Reguli lipsa: %s"
              % (len(PRJ.parts), len(T["hw"]), len(T["ops"]), len(T["why"]), T["missing_rules"] or "-"))
        print("REZULTAT:", "TOATE VERIFICARILE TREC" if not bad else "%d VERIFICARI ESUATE" % len(bad))
