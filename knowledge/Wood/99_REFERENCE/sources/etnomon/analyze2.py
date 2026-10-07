# Analiza finala ETNOMON -> tabele markdown pentru baza de cunostinte.
import json, re, sys, collections, unicodedata
recs = [json.loads(l) for l in open(sys.argv[1], encoding="utf-8") if l.strip()]
OUTMD = sys.argv[2]

def norm(s):
    s = (s or "").lower()
    for a, b in [("ş","s"),("ș","s"),("ţ","t"),("ț","t"),("ă","a"),("â","a"),("î","i")]: s = s.replace(a, b)
    return unicodedata.normalize("NFKD", s).encode("ascii", "ignore").decode()

SPECIES = {"stejar": ["stejar"], "gorun": ["gorun"], "brad": ["brad"], "molid": ["molid"], "rasinoase (nespec.)": ["rasinoase"],
           "fag": ["fag "," fag", "fag;", "fag,"], "pin": [" pin ", " pin,", " pin;"], "larice": ["larice"], "ulm": ["ulm"],
           "salcam": ["salcam"], "carpen": ["carpen"], "tei": [" tei"], "plop": ["plop"], "frasin": ["frasin"], "salcie": ["salcie"],
           "mesteacan": ["mesteacan"], "anin/arin": ["anin", " arin"], "cer": [" cer ", " cer;", " cer,"]}
def species(t):
    t = " " + norm(t) + " "
    return sorted({k for k, v in SPECIES.items() if any(x in t for x in v)})

def joint(t):
    t = norm(t).replace("cheut", "cheot").replace("chiot", "cheot")
    out = []
    if "coada de randunic" in t or "nemt" in t or "trapez" in t: out.append("cheotori in coada de randunica / nemtesti")
    if re.search(r"cheot\w* rotund|rotunde", t) and "cheot" in t: out.append("cheotori rotunde")
    if re.search(r"cheot\w* drept|cheotoare dreapta", t) or re.fullmatch(r"\s*-?\s*(imbinare )?dreapta\s*", t): out.append("cheotori drepte")
    if "ciobanes" in t or "stanes" in t: out.append("cheotori ciobanesti / stâneste")
    if "batran" in t: out.append("cheia batraneasca")
    if "crestez" in t or "crestat" in t: out.append("in crestez / crestat")
    if re.search(r"\bsosi|\bsesi|sosii", t): out.append("soşi (stalpi intermediari santuiti)")
    if "amnar" in t: out.append("amnare (stalpi santuiti, Bucovina)")
    if "catei" in t: out.append("căţei")
    if "muc" in t: out.append("muc (cep in stalp)")
    if "limba si uluc" in t or "nut si feder" in t: out.append("limba si uluc / nut si feder")
    if "cleste" in t: out.append("in cleste")
    if "cep" in t and "muc" not in t: out.append("cep")
    if "cheot" in t and not any(x.startswith("cheotori") or x.startswith("cheia") for x in out): out.append("cheotori (tip nespecificat)")
    if not out and re.search(r"\bstalp|\bpari\b|schelet|contrafis|montant", t): out.append("stalpi / schelet")
    if not out and re.search(r"blockbau|cunun|barne\w* (asezat|dispus|orizont|suprapus)|grinzi orizont|suprapuner|asezare orizont|randuri suprapuse|imbinat\w* la colt|imbinare la colt", t):
        out.append("cununi, imbinare de colt nespecificata")
    if not out and re.search(r"zid|impletit|caramid|piatra|ceamur", t): out.append("(nu e imbinare de lemn)")
    if not out and re.search(r"cuie", t): out.append("cuie")
    return out or (["(gol / altele)"])

def beams(t):
    t = norm(t); out = []
    if re.search(r"barne\w* rotund|lemn rotund|grinzi rotund", t): out.append("rotunde")
    if re.search(r"(2|doua) (fete|muchii|laturi)", t): out.append("cioplite 2 fete")
    if re.search(r"(4|patru) (fete|muchii|laturi)", t): out.append("cioplite 4 fete")
    if not out and ("cioplit" in t or "fasonat" in t): out.append("cioplite (nespec.)")
    return out

def county(r):
    m = re.search(r"jud\.\s*([A-Za-zĂÂÎŞŢȘȚăâîşţșț\- ]+?)(?:\s+CC BY-SA|$|,|;)", r.get("Provenienţa", "").strip())
    return m.group(1).strip() if m else "?"

def roof(r):
    t = norm(r.get("Şarpanta", "") + " " + r.get("Descriere", ""))
    for k, v in [("4 ape", ["patru ape", "4 ape"]), ("2 ape", ["doua ape", "2 ape"]), ("3 ape", ["trei ape", "3 ape"]), ("1 apa", ["o apa", "1 apa", "intr-o apa"])]:
        if any(x in t for x in v): return k
    return "?"
def cover(r):
    t = norm(r.get("Învelitoare", "") + " " + r.get("Descriere", ""))
    for k in ["sindrila", "sita", "dranit", "paie", "stuf", "trestie", "tigla", "olane", "tabla", "scandur"]:
        if k in t: return k
    return "?"

rows = []
for r in recs:
    if norm(r.get("Categoria", "")) != "constructii": continue
    w = r.get("Pereţii construcţiei", ""); d = r.get("Descriere", "")
    g = lambda pat: (re.search(pat, w) or [None, ""])[1] if re.search(pat, w) else ""
    mat = g(r"MATERIAL:\s*-?(.*?)(?:;\s*TEHNICA|$)"); teh = g(r"TEHNICA:\s*-?(.*?)(?:;\s*[IÎ]MBINARE|$)"); imb = g(r"[IÎ]MBINARE:\s*-?(.*)$")
    rows.append(dict(county=county(r), zone=r.get("Zona etnografică", "").replace("CC BY-SA 4.0", "").strip(), museum=r.get("Muzeu", ""),
                     name=r.get("Denumirea în muzeu", ""), ID=r["ID"],
                     species=species(mat + " " + d), joints=joint(imb + " " + teh), beams=beams(teh + " " + d + " " + mat),
                     talpi=species(" ".join(re.findall(r"t[aă]lpi[^;.]*", d + " " + r.get("Temelia", "") + " " + w, flags=re.I))),
                     roof=roof(r), cover=cover(r), walls=w))
json.dump(rows, open(OUTMD.replace(".md", "_rows.json"), "w", encoding="utf-8"), ensure_ascii=False)
C = collections.Counter
WOODJ = lambda r: [j for j in r["joints"] if not j.startswith("(")]

def table(title, keyf, cols, rowsel=None, top=None, minn=3):
    lines = [f"### {title}", ""]
    grp = collections.defaultdict(list)
    for r in rows:
        if rowsel and not rowsel(r): continue
        grp[keyf(r)].append(r)
    keys = sorted(grp, key=lambda k: -len(grp[k]))
    keys = [k for k in keys if len(grp[k]) >= minn and k not in ("?", "")][:top]
    lines.append("| " + " | ".join(["Grup", "N"] + [c[0] for c in cols]) + " |")
    lines.append("|" + "---|" * (len(cols) + 2))
    for k in keys:
        rs = grp[k]
        cells = [k, str(len(rs))]
        for name, f in cols:
            cc = C(x for r in rs for x in f(r))
            cells.append(", ".join(f"{a} {b}" for a, b in cc.most_common(4)) or "—")
        lines.append("| " + " | ".join(cells) + " |")
    return "\n".join(lines) + "\n"

cols = [("Specii pereti (nr. fise)", lambda r: r["species"]), ("Imbinari pereti", WOODJ),
        ("Barne", lambda r: r["beams"]), ("Acoperis", lambda r: [r["roof"]] if r["roof"] != "?" else []),
        ("Invelitoare", lambda r: [r["cover"]] if r["cover"] != "?" else [])]

md = []
md.append(f"Fise ETNOMON descarcate: **{len(recs)}**; constructii analizate: **{len(rows)}**.\n")
md.append("### Totaluri\n")
for name, f in [("Imbinari pereti (lemn)", WOODJ), ("Specii pereti", lambda r: r["species"]), ("Barne", lambda r: r["beams"]),
                ("Specii talpi", lambda r: r["talpi"]), ("Acoperis", lambda r: [r["roof"]]), ("Invelitoare", lambda r: [r["cover"]])]:
    md.append(f"- **{name}**: " + ", ".join(f"{a} {b}" for a, b in C(x for r in rows for x in f(r)).most_common(14)))
md.append("")
md.append(table("Pe judet de provenienta (judete cu ≥ 5 constructii)", lambda r: r["county"], cols, minn=5))
md.append(table("Pe zona etnografica (zone cu ≥ 8 constructii)", lambda r: r["zone"], cols, minn=8, top=30))
md.append(table("Pe specie dominanta a peretilor → tip de imbinare si barne", lambda r: (r["species"] or ["?"])[0] if len(r["species"]) == 1 else ("mixt" if r["species"] else "?"),
                [("Imbinari", WOODJ), ("Barne", lambda r: r["beams"]), ("Judete", lambda r: [r["county"]])], minn=3))
def jkey(r):
    j = [x for x in WOODJ(r) if not x.startswith("cununi") and x not in ("cuie", "cep")]
    return j[0] if j else "?"
md.append(table("Pe tip de imbinare → specii, barne, judete", jkey,
                [("Specii", lambda r: r["species"]), ("Barne", lambda r: r["beams"]), ("Judete", lambda r: [r["county"]]),
                 ("Zone", lambda r: [r["zone"]] if r["zone"] else [])], minn=3))
open(OUTMD, "w", encoding="utf-8").write("\n".join(md))
print("\n".join(md)[:9000])
