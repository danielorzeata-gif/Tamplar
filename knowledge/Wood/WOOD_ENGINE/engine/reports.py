"""WOOD ENGINE — din model: debitare, feronerie, operatii, scule (disponibil / lipsa), 'De ce?' (provenienta)."""
import json, os, collections

PREP_OPS = [
    ("debitare grosiera din scandura (lungime +30, latime +4)", "circular_masa|fierastrau_panglica", ("CON-DIM-004",)),
    ("fata + cant de referinta", "abricht", ("CON-DIM-004",)),
    ("grosime la cota", "masina_grosime", ()),
    ("latime la cota", "circular_masa", ()),
    ("lungime la cota (capete in echer)", "circular_masa|fierastrau_retezat", ()),
]
END_OPS = [
    ("rotunjire muchii R5 (Montessori)", "freza_mana+freza_rotunjire_r5", ("MONT-001",)),
    ("slefuire 120-180", "slefuitor_orbital", ()),
]


def load_json(path):
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def tool_status(tool_expr, inv):
    """'a+b|c' -> (status, detaliu). '+' = toate necesare, '|' = alternative."""
    best = None
    for alt in tool_expr.split("|"):
        sts = [inv.get(t, {}).get("status", "?") for t in alt.split("+")]
        if all(s == "da" for s in sts):
            st = "DISPONIBIL"
        elif any(s == "nu" for s in sts):
            st = "LIPSA"
        else:
            st = "NECUNOSCUT"
        rank = {"DISPONIBIL": 0, "NECUNOSCUT": 1, "LIPSA": 2}[st]
        if best is None or rank < best[0]:
            best = (rank, st, alt)
    return best[1], best[2]


def part_operations(p, special_prep=None):
    ops = []
    for op, tool, rules in (special_prep or PREP_OPS):
        ops.append(dict(op=op, tool=tool, rules=list(rules), n=1, joint=""))
    agg = collections.OrderedDict()
    for c in p.cuts:
        key = (c.op, c.tool, c.joint)
        if key not in agg:
            agg[key] = dict(op=c.op, tool=c.tool, rules=list(c.rules), n=0, joint=c.joint, nodes=[])
        agg[key]["n"] += 1
        if c.note and c.note not in agg[key]["nodes"]:
            agg[key]["nodes"].append(c.note)
    ops += list(agg.values())
    for op, tool, rules in END_OPS:
        ops.append(dict(op=op, tool=tool, rules=list(rules), n=1, joint=""))
    return ops


def build_tables(prj, rules_db, inv, cutlist_extra=None):
    groups = collections.OrderedDict()
    for p in prj.parts:
        key = (p.code, p.name)
        groups.setdefault(key, []).append(p)
    cut = []
    for (code, name), ps in groups.items():
        p = ps[0]
        g, l, L = p.finished_dims
        ra = p.rough_allow
        waste = sorted(set(c.op for c in p.cuts))
        cut.append(dict(cod=code, piesa=name, buc=getattr(p, "qty", None) or len(ps), material=p.mat, gros=round(g, 1), lat=round(l, 1), lung=round(L, 1),
                        brut="%.0f x %.0f x %.0f" % (g + ra[0], l + ra[1], L + ra[2]),
                        rosu="; ".join(waste) if waste else "-", reguli=", ".join(p.rules), note=p.notes))
    # feronerie agregata
    hw = collections.OrderedDict()
    for h in prj.hardware:
        k = (h["name"], h["spec"])
        if k not in hw:
            hw[k] = dict(name=h["name"], spec=h["spec"], qty=0, where=[], rules=h["rules"])
        hw[k]["qty"] += h["qty"]
        hw[k]["where"].append(h["where"])
    # operatii + scule
    ops_rows, tools_need = [], collections.OrderedDict()
    for (code, name), ps in groups.items():
        for i, o in enumerate(part_operations(ps[0]), 1):
            st, alt = tool_status(o["tool"], inv)
            ops_rows.append(dict(cod=code, piesa=name, buc=len(ps), nr=i, operatie=o["op"], repetari=o["n"],
                                 imbinare=o["joint"], noduri=", ".join(o.get("nodes", [])), scula=o["tool"], status=st,
                                 reguli=", ".join(o["rules"])))
            for alt_t in o["tool"].split("|"):
                for t in alt_t.split("+"):
                    tools_need.setdefault(t, set()).add("%s %s" % (code, name))
    tools_rows = []
    for t, used in tools_need.items():
        e = inv.get(t, {})
        tools_rows.append(dict(scula=t, status=e.get("status", "?"), model=e.get("model", ""), folosita_la=", ".join(sorted(used)),
                               nota=e.get("note", "")))
    # De ce? — toate regulile folosite (piese + scule + feronerie)
    used = collections.OrderedDict()
    def mark(rid, where):
        used.setdefault(rid, set()).add(where)
    for p in prj.parts:
        for r in p.rules:
            mark(r, "%s %s (sectiune)" % (p.code, p.name))
        for c in p.cuts:
            for r in c.rules:
                mark(r, "%s %s: %s" % (p.code, p.name, c.op))
    for h in prj.hardware:
        for r in h["rules"]:
            mark(r, "feronerie: " + h["name"])
    rdb = {r["id"]: r for r in rules_db["rules"]}
    why_rows = []
    for rid, ws in used.items():
        r = rdb.get(rid, {})
        why_rows.append(dict(id=rid, regula=r.get("rule", "(lipseste din rules.json!)"), valoare=r.get("value", ""),
                             sursa=r.get("source", ""), locatie=r.get("location", ""), nivel=r.get("level", "?"),
                             kb=r.get("kb", ""), folosita_la="; ".join(sorted(ws))[:900]))
    return dict(cut=cut, hw=list(hw.values()), ops=ops_rows, tools=tools_rows, why=why_rows,
                missing_rules=[r["id"] for r in why_rows if r["regula"].startswith("(lipseste")])


def write_xlsx(path, T, prj):
    from openpyxl import Workbook
    from openpyxl.styles import Font, PatternFill, Alignment
    wb = Workbook()
    def sheet(title, cols, rows, first=False):
        ws = wb.active if first else wb.create_sheet()
        ws.title = title
        ws.append([c[1] for c in cols])
        for c in ws[1]:
            c.font = Font(bold=True, color="FFFFFF")
            c.fill = PatternFill("solid", fgColor="7A4A1E")
        for r in rows:
            ws.append([r.get(c[0], "") if isinstance(r.get(c[0], ""), (int, float, str)) else str(r.get(c[0])) for c in cols])
        for i, c in enumerate(cols, 1):
            ws.column_dimensions[ws.cell(1, i).column_letter].width = c[2]
        for row in ws.iter_rows(min_row=2):
            for c in row:
                c.alignment = Alignment(wrap_text=True, vertical="top")
        return ws
    sheet("Debitare", [("cod", "Cod", 6), ("piesa", "Piesa", 22), ("buc", "Buc", 5), ("material", "Material", 10),
                       ("gros", "Gros finit mm", 10), ("lat", "Lat finit mm", 10), ("lung", "Lung finit mm", 11),
                       ("brut", "Brut debitare mm (DESEURI::Debitare)", 22), ("rosu", "Rosu in model = operatii", 60),
                       ("reguli", "Reguli (De ce?)", 28), ("note", "Observatii", 40)], T["cut"], first=True)
    sheet("Feronerie", [("name", "Element", 40), ("spec", "Specificatie", 26), ("qty", "Buc", 6),
                        ("where", "Unde", 60), ("rules", "Reguli", 24)],
          [dict(h, where=", ".join(h["where"]), rules=", ".join(h["rules"])) for h in T["hw"]])
    ws = sheet("Operatii", [("cod", "Cod", 6), ("piesa", "Piesa", 20), ("buc", "Buc", 5), ("nr", "Nr", 4), ("operatie", "Operatie", 52),
                            ("repetari", "x/piesa", 7), ("imbinare", "Imbinare", 12), ("noduri", "Noduri", 34), ("scula", "Scula (| = alternative)", 40),
                            ("status", "Status scula", 13), ("reguli", "Reguli", 30)], T["ops"])
    fills = {"DISPONIBIL": "C6EFCE", "LIPSA": "FFC7CE", "NECUNOSCUT": "E7E6E6"}
    for row in ws.iter_rows(min_row=2):
        st = row[9].value
        if st in fills:
            row[9].fill = PatternFill("solid", fgColor=fills[st])
    ws = sheet("Scule", [("scula", "Scula", 26), ("status", "Status (da/nu/?)", 14), ("model", "Model", 20),
                         ("folosita_la", "Folosita la", 70), ("nota", "Nota", 40)], T["tools"])
    sheet("Ordine asamblare", [("nr", "Nr", 4), ("pas", "Pas", 90), ("scule", "Scule", 30)],
          [dict(nr=i, pas=a[0], scule=a[1]) for i, a in enumerate(prj.assembly, 1)])
    sheet("De ce (surse)", [("id", "ID regula", 16), ("regula", "Regula", 60), ("valoare", "Valoare", 18), ("nivel", "Nivel", 10),
                            ("sursa", "Sursa", 40), ("locatie", "Locatie", 26), ("kb", "Fisier in baza", 40),
                            ("folosita_la", "Folosita la", 60)], T["why"])
    sheet("Verificari", [("test", "Test", 50), ("ok", "OK", 6), ("detaliu", "Detaliu", 70)],
          [dict(test=t, ok="DA" if ok else "NU", detaliu=d) for (t, ok, d) in prj.checks_log])
    wb.save(path)


def write_csv(path, T):
    import csv
    with open(path, "w", newline="", encoding="utf-8-sig") as f:
        w = csv.writer(f, delimiter=";")
        w.writerow(["Cod", "Piesa", "Buc", "Material", "Gros", "Lat", "Lung", "Brut", "Rosu in model"])
        for r in T["cut"]:
            w.writerow([r["cod"], r["piesa"], r["buc"], r["material"], r["gros"], r["lat"], r["lung"], r["brut"], r["rosu"]])
