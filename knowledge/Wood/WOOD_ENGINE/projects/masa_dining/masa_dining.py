#! python 3
"""WOOD ENGINE — PROIECT 2: Masa de dining din stejar, 1800 x 900 x 750, blat masiv prins cu butoni.

Al doilea proiect, construit din aceleasi componente (engine/components.py) ca patul Montessori,
plus doua imbinari noi: MT-GL-01 (cep lipit cu haunch si capete la 45 grade) si BTN-01 (butoni in nut).
Rulare: Python normal -> verificari + out/masa_dining.xlsx;  Rhino 8 -> model cu deseu rosu.
"""
import os, sys, math

try:
    HERE = os.path.dirname(os.path.abspath(__file__))
except NameError:
    HERE = r"C:\Users\Orzi\Desktop\General\Wood\WOOD_ENGINE\projects\masa_dining"
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
if ROOT not in sys.path:
    sys.path.insert(0, ROOT)
for m in [k for k in list(sys.modules) if k == "engine" or k.startswith("engine.")]:
    del sys.modules[m]
from engine import geom as G
from engine.model import Project
from engine import joints as JN
from engine import checks as CK
from engine import reports as RP
from engine import components as CP

# ================================================================== PARAMETRI
MAT        = "stejar"
L, W, H    = 1800, 900, 750      # blat X, Y; inaltime totala          (FURN-TBL-H)
TOP_T      = 30                  # blat masiv                          (FURN-TBL-SECT)
TOP_BOARDS = 6                   # scanduri ~150 mm                     (ADH-004b)
LEG        = 70                  # picior 70 x 70                       (FURN-TBL-SECT)
OVER_X, OVER_Y = 100, 60         # consola blatului peste picioare
APRON_T, APRON_H = 30, 100       # zarga 30 x 100: T/3 = 10 = freza/dalta din atelier (vezi nota)
SETBACK    = 5                   # zarga retrasa fata de fata piciorului
TENON_LEN  = 50                  # cep lung; cepurile se intalnesc in picior -> capete 45 grade (JOINERY-MT-167)
HAUNCH     = 10                  # (JOINERY-MT-004)
SHOULDER_BOT = 12
FIT_GLUE   = 0.1                 # (TOL-GLUE)
MITRE_GAP  = 1.0
DUM        = 4.0                 # variatia sezoniera de umiditate in casa (puncte procentuale) — WOOD-MOIST-003/004
E          = 2.5
P = dict(E=E, fit_glue=FIT_GLUE, tenon_len_table=TENON_LEN, mitre_gap=MITRE_GAP)

# ================================================================== COTE DERIVATE
TOP_Z0 = H - TOP_T                                   # dedesubtul blatului = capatul picioarelor
LX = {"st": (OVER_X, OVER_X + LEG), "dr": (L - OVER_X - LEG, L - OVER_X)}
LY = {"fata": (OVER_Y, OVER_Y + LEG), "spate": (W - OVER_Y - LEG, W - OVER_Y)}
AZ = (TOP_Z0 - APRON_H, TOP_Z0)
ZT = (AZ[0] + SHOULDER_BOT, AZ[1] - 28)              # cep; deasupra lui haunch-ul (28 mm inaltime, 10 adancime)


def build():
    prj = Project("Masa dining %dx%dx%d %s" % (L, W, H, MAT))
    log = prj.checks_log
    legs = {(gx, gy): CP.post(prj, "A", "Picior", MAT, LX[gx][0], LY[gy][0], LEG, LEG, TOP_Z0, rules=["FURN-TBL-SECT"])
            for gx in ("st", "dr") for gy in ("fata", "spate")}
    # zargi lungi (B) de-a lungul X — la SETBACK de fata exterioara a piciorului
    ylong = {"fata": (LY["fata"][0] + SETBACK, LY["fata"][0] + SETBACK + APRON_T, +1),
             "spate": (LY["spate"][1] - SETBACK - APRON_T, LY["spate"][1] - SETBACK, -1)}
    xshort = {"st": (LX["st"][0] + SETBACK, LX["st"][0] + SETBACK + APRON_T, +1),
              "dr": (LX["dr"][1] - SETBACK - APRON_T, LX["dr"][1] - SETBACK, -1)}
    longs = {gy: CP.rail(prj, "B", "Zarga lunga", MAT, "x", LX["st"][1], LX["dr"][0], c0, c1, AZ[0], AZ[1],
                         tenons=(TENON_LEN, TENON_LEN), rules=["FURN-TBL-SECT"],
                         notes="zarga 30 (nu 22-25): cepul T/3 = 10 mm se face cu freza spirala D10 / dalta 10 din atelier")
             for gy, (c0, c1, s) in ylong.items()}
    shorts = {gx: CP.rail(prj, "C", "Zarga scurta", MAT, "y", LY["fata"][1], LY["spate"][0], c0, c1, AZ[0], AZ[1],
                          tenons=(TENON_LEN, TENON_LEN), rules=["FURN-TBL-SECT"])
              for gx, (c0, c1, s) in xshort.items()}
    res = []
    for gx in ("st", "dr"):
        for gy in ("fata", "spate"):
            c0L, c1L, _ = ylong[gy]; c0S, c1S, _ = xshort[gx]
            cy_long, cx_short = (c0L + c1L) / 2.0, (c0S + c1S) / 2.0
            uL = (1.0, 0.0) if gx == "st" else (-1.0, 0.0)          # spre corpul zargii lungi
            uS = (0.0, 1.0) if gy == "fata" else (0.0, -1.0)        # spre corpul zargii scurte
            nx, ny = uL[0] - uS[0], uL[1] - uS[1]
            k = math.hypot(nx, ny); nx, ny = nx / k, ny / k
            mL = (nx, ny, -(nx * cx_short + ny * cy_long))
            mS = (-mL[0], -mL[1], -mL[2])
            faceX, dX = (LX["st"][1], -1) if gx == "st" else (LX["dr"][0], +1)
            faceY, dY = (LY["fata"][1], -1) if gy == "fata" else (LY["spate"][0], +1)
            res.append(JN.mt_glued(prj, longs[gy], legs[(gx, gy)], axis="x", face=faceX, d=dX, rail_c=(c0L, c1L), rail_z=AZ,
                                   leg_c=LY[gy], z_tenon=ZT, haunch_depth=HAUNCH, leg_top=TOP_Z0, mitre=mL, P=P,
                                   where="zarga lunga %s -> picior %s-%s" % (gy, gx, gy)))
            res.append(JN.mt_glued(prj, shorts[gx], legs[(gx, gy)], axis="y", face=faceY, d=dY, rail_c=(c0S, c1S), rail_z=AZ,
                                   leg_c=LX[gx], z_tenon=ZT, haunch_depth=HAUNCH, leg_top=TOP_Z0, mitre=mS, P=P,
                                   where="zarga scurta %s -> picior %s-%s" % (gx, gx, gy)))
    # blat (D) — scanduri incleiate; miscarea pe latime (Y) din Wood Handbook
    top = CP.panel(prj, "D", "Blat", MAT, 0, L, 0, W, TOP_Z0, H, TOP_BOARDS, rules=["FURN-TBL-SECT", "TBL-WH-005"])
    dW = W * 0.00376 * DUM                                    # WOOD-MOIST-005 (stejar, flatsawn)
    log.append(("miscarea blatului pe latime (dUM %.0f %%, WOOD-MOIST-005)" % DUM, True, "%.1f mm total — blatul NU se fixeaza rigid" % dW))
    # butoni (K) — zargi lungi: miscare perpendiculara; zargi scurte: miscare in lungul nutului
    for gy, (c0, c1, s) in ylong.items():
        inner = c1 if s > 0 else c0
        a = (LX["st"][1] + 20, LX["dr"][0] - 20)
        n = max(2, math.ceil((a[1] - a[0]) / 400.0) + 1)
        res.append(JN.buttons_in_groove(prj, longs[gy], axis="x", a_range=a, inner_face=inner, s=s, top_z=TOP_Z0, n=n,
                                        move_across=dW, P=P, where="butoni zarga lunga %s" % gy))
    for gx, (c0, c1, s) in xshort.items():
        inner = c1 if s > 0 else c0
        a = (LY["fata"][1] + 20, LY["spate"][0] - 20)
        n = max(2, math.ceil((a[1] - a[0]) / 400.0) + 1)
        res.append(JN.buttons_in_groove(prj, shorts[gx], axis="y", a_range=a, inner_face=inner, s=s, top_z=TOP_Z0, n=n,
                                        move_across=0.0, P=P, where="butoni zarga scurta %s" % gx))
    for r in res:
        for t in r["msgs"]:
            log.append(t)
    nb = sum(1 for p in prj.parts if p.code == "K")
    for p in prj.parts:
        if p.code == "K":
            p.qty_label = "x%d" % nb
    # cote globale si ergonomie
    log.append(("inaltime masa 730-760 mm (FURN-TBL-H)", 730 <= H <= 760, "%d mm" % H))
    log.append(("spatiu genunchi sub zarga >= 600 mm (FURN-TBL-H)", AZ[0] >= 600, "%d mm" % AZ[0]))
    log.append(("locuri: >= 600 mm / persoana pe latura lunga", (L - 2 * OVER_X) / 600 >= 2, "%.1f persoane / latura" % (L / 600)))
    log.append(("consola blat <= 1/3 din distanta intre picioare", OVER_X <= (L - 2 * OVER_X) / 3, "%d mm" % OVER_X))
    prj.assembly = [
        ("Montaj uscat complet; numeroteaza fiecare cep cu scobitura lui; verifica capetele la 45 grade (joc ~1 mm) in fiecare picior.", "-"),
        ("Sub-ansamblu 1: picioarele stanga + zarga scurta stanga — adeziv PVAc D3 pe obraji si in scobituri, strange (0,7-1,7 MPa), diagonale egale.", "cleme, ruleta"),
        ("Sub-ansamblu 2: idem dreapta. Lasa la clema >= 1 ora; nu solicita 24 h.", "cleme"),
        ("Leaga cele doua capete cu zargile lungi (adeziv); masa pe o suprafata plana — fara 'scaun'; diagonale egale in plan.", "cleme lungi (min. 1800 mm), ruleta"),
        ("Dupa ~7 zile (rosturile de adeziv s-au stabilizat — ADH-005c), niveleaza si slefuieste blatul incleiat.", "rindea / slefuitor"),
        ("Blatul cu fata in jos pe paturi; cadrul pe el, centrat; butonii in nut, cu jocul fata de zarga indicat (lungi: spre interior).", "-"),
        ("Insurubeaza butonii (gaura de ghidare D3 in blat, adancime max. 20 mm — blat 30).", "bormasina, surubelnita"),
        ("Finisaj: ulei-ceara dur pe TOATE fetele blatului (si dedesubt) — FIN-003.", "laveta / pensula"),
    ]
    return prj


def run_checks(prj):
    col = CK.collisions(prj.parts)
    prj.checks_log.append(("fara suprapuneri intre piese finite", not col,
                           "; ".join("%s %s x %s %s (%d puncte, ex. %s)" % (a, an, b, bn, n, tuple(round(v, 1) for v in h)) for a, an, b, bn, n, h in col) or "OK: nicio suprapunere"))
    cc = CK.cut_conflicts(prj.parts)
    prj.checks_log.append(("imbinari diferite nu se intersecteaza in aceeasi piesa", not cc, "; ".join("%s %s: [%s | %s] x [%s | %s]" % c for c in cc) or "OK"))
    lonely = CK.supported(prj.parts)
    prj.checks_log.append(("fiecare piesa atinge cel putin o alta", not lonely, ", ".join(lonely) or "OK"))
    return prj


def outputs(prj):
    rules_db = RP.load_json(os.path.join(ROOT, "data", "rules.json"))
    inv = RP.load_json(os.path.join(ROOT, "data", "tools_inventory.json"))["tools"]
    T = RP.build_tables(prj, rules_db, inv)
    out = os.path.join(HERE, "out"); os.makedirs(out, exist_ok=True)
    RP.write_xlsx(os.path.join(out, "masa_dining.xlsx"), T, prj)
    RP.write_csv(os.path.join(out, "masa_dining_debitare.csv"), T)
    return T


if __name__ == "__main__":
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
            outputs(PRJ)
        except Exception as ex:
            print("xlsx nescris in Rhino (%s)" % ex)
    else:
        T = outputs(PRJ)
        for t, ok, d in PRJ.checks_log:
            print(("OK   " if ok else "FAIL ") + t + " — " + d)
        bad = [c for c in PRJ.checks_log if not c[1]]
        print("\nPiese: %d | Feronerie: %d pozitii | Operatii: %d | Reguli folosite: %d | Reguli lipsa: %s"
              % (len(PRJ.parts), len(T["hw"]), len(T["ops"]), len(T["why"]), T["missing_rules"] or "-"))
        print("REZULTAT:", "TOATE VERIFICARILE TREC" if not bad else "%d VERIFICARI ESUATE" % len(bad))
