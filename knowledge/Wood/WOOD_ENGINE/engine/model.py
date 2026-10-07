"""WOOD ENGINE — modelul de date al unui proiect.

Fiecare SCULA de taiere (cut) poarta metadate: ce operatie e, cu ce unealta, din ce regula vine si
din ce imbinare face parte. Asa, acelasi volum rosu devine si OPERATIE, si cerinta de UNEALTA, si raspuns
la intrebarea "De ce?" (provenance).
"""
from . import geom as G


class Cut:
    __slots__ = ("prim", "op", "tool", "rules", "joint", "note")

    def __init__(self, prim, op, tool, rules=(), joint="", note=""):
        self.prim, self.op, self.tool = prim, op, tool
        self.rules, self.joint, self.note = tuple(rules), joint, note


class Part:
    def __init__(self, code, name, mat, blank, finished_dims, rough_allow=(4, 4, 30), drill=True, layout=True,
                 qty_label="", rules=(), notes=""):
        self.code, self.name, self.mat, self.blank = code, name, mat, blank
        self.finished_dims = tuple(sorted(finished_dims))      # (gros, lat, lung) finite, mm
        self.rough_allow = rough_allow                          # adaos (gros, lat, lung) pentru brut de debitare
        self.drill, self.layout, self.qty_label = drill, layout, qty_label
        self.cuts = []
        self.rules = list(rules)          # reguli care au decis sectiunea/lungimea piesei
        self.notes = notes
        self.idx = 0

    @property
    def label(self):
        return "%s%d" % (self.code, self.idx)

    def add_cut(self, prim, op, tool, rules=(), joint="", note=""):
        self.cuts.append(Cut(prim, op, tool, rules, joint, note))

    def bbox(self):
        return G.bbox(self.blank)

    def contains(self, pt, tol=0.0):
        """Punct in piesa FINITA (brut minus scule)."""
        if not G.inside(self.blank, pt, tol):
            return False
        for c in self.cuts:
            if G.inside(c.prim, pt, -1e-9):
                return False
        return True

    def rough(self):
        """Brutul de debitare: primitiva orientata (ex. caprior inclinat) daca exista, altfel cutia aliniata."""
        return getattr(self, "rough_prim", None) or self.rough_box()

    def rough_box(self):
        """Brutul de debitare (DESEURI::Debitare = rough - blank), aliniat la bbox-ul brutului."""
        b = self.bbox()
        dims = [(b[1] - b[0], 0), (b[3] - b[2], 1), (b[5] - b[4], 2)]
        order = sorted(dims)                    # gros, lat, lung
        add = [0, 0, 0]
        for (d, ax), a in zip(order, self.rough_allow):
            add[ax] = a / 2.0
        return G.box(b[0] - add[0], b[1] + add[0], b[2] - add[1], b[3] + add[1], b[4] - add[2], b[5] + add[2])


class GlobalDrill:
    """Gaura data la montaj prin mai multe piese (cui de lemn, bulon)."""
    def __init__(self, prim, op, tool, rules=(), joint="", note="", only=None):
        self.prim, self.op, self.tool, self.rules, self.joint, self.note = prim, op, tool, tuple(rules), joint, note
        self.only = only     # None = toate piesele drill=True intersectate; altfel lista de coduri de piese


class Project:
    def __init__(self, name, units="mm"):
        self.name, self.units = name, units
        self.parts, self.globals, self.hardware, self.assembly, self.checks_log = [], [], [], [], []
        self.joints_used = {}        # joint_id -> lista de noduri (text)
        self._count = {}

    def add(self, part):
        self._count[part.code] = self._count.get(part.code, 0) + 1
        part.idx = self._count[part.code]
        self.parts.append(part)
        return part

    def add_global(self, gd):
        self.globals.append(gd)

    def apply_globals(self):
        for gd in self.globals:
            gb = G.bbox(gd.prim)
            for p in self.parts:
                if not p.drill:
                    continue
                if gd.only is not None and p.code not in gd.only:
                    continue
                if G.bb_inter(p.bbox(), gb):
                    p.add_cut(gd.prim, gd.op, gd.tool, gd.rules, gd.joint, gd.note)

    def add_hw(self, name, qty, spec, where, rules=()):
        self.hardware.append(dict(name=name, qty=qty, spec=spec, where=where, rules=list(rules)))

    def use_joint(self, joint_id, where):
        self.joints_used.setdefault(joint_id, []).append(where)
