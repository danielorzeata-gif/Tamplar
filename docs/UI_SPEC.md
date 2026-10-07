# UI specification — "Rhino Wood Studio" (agreed decisions, 2026-10-07)

Status: **specification + Core model done (Workspace/Room/StyleSet, rule catalog, 100 tests)**; the Studio window itself is next.

## Decisions taken with the user
| # | Decision |
|---|---|
| 1 | **Two style sets per room**: *Aspect* (what is visible: species, front style, handle, edge profile, foot shape, visible gaps) and *Structure* (how it is built: joints, fixings, hidden hardware). Structure varies more between pieces than aspect. |
| 2 | **Room level**: Workspace → Room → Piece → Part. Each room has its own two sets; rooms are independent (a house does not force one set everywhere). |
| 3 | **Editing a field of a piece** defaults to **"only here"** (detaches that field from the set). The notification offers **"apply to the whole set"**. |
| 4 | **No visual finish/colour for now.** The set only carries the species ("oak furniture") and a finish *label/quantity*. |
| 5 | Window type **B – Studio window** (3 columns); dockable panels (A) remain as fallback. |

## Layout (3 columns + status bar)
```
┌─ Structure ───────────────┬─ Preview ──────────────────────────┬─ Properties (selected item) ───────────┐
│ Workspace "Casa"          │ [3D] [Technical sheet] [Cut] [Docs]│ Piece: Masa · standard 1800×900×760    │
│  ▾ Room Dormitor          │ 3D: selected part highlighted,     │ ┌Dimensions│Joints│Details│Material│HW┐ │
│    Set Aspect  (oak, …)   │     callouts on details            │ │ Length  [1800] 🔒 standard   ↺      │ │
│    Set Structure (M&T…)   │ Sheet: top / front / side with     │ │ Width   [ 900] ✎ piece       ↺      │ │
│    ▸ Bed ▸ Nightstand ×2  │        dimensions + materials list │ │ Species [Oak]  🔗 set Aspect  🔓     │ │
│    ▸ Wardrobe ▸ Dresser   │        (like the user's sheets)    │ │ Apron joint [Mortise-tenon ▼] 🔗    │ │
│  ▸ Room Kitchen           │                                    │ │   ★★★★★ …  [compare]               │ │
│ [+ Piece] presets [4][6][8]│                                    │ └───────────────────────────────────────┘ │
├───────────────────────────┴────────────────────────────────────┴─────────────────────────────────────────┤
│ Rules: ✔ OK · ⚠ warnings · ✖ blocked · "needs testing" flags   Purchase (room, global): 10 boards · 341 € │
└──────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

### Field provenance (every field shows where its value comes from)
`🔒 standard` (rule/preset) · `🔗 set` (follows the room set) · `✎ piece override` (detached, ↺ re-link) · changing a set shows the **impact** ("9 pieces affected, +0.02 m³").
Core API: `Room.Fields(piece)` → `FieldInfo{Value, SetValue, Source}`, `Room.SetStyle(kind,key,value)`, `Room.ChangeField(piece,key,value,OnlyHere|WholeSet)`, `Room.Relink`, `StyleImpact{PieceIds, Conflicts}`.

### What a set contains / does not contain
Contains (visible, must match): species, front style, stile width and panel groove, handle model + placement rule, edge profile, foot shape, visible gaps.
Does **not** contain: overall dimensions, hidden joint types per piece (bed bolts vs. dovetailed drawers), number of drawers, interior hardware.
Sets keep **proportions and rules**, not fixed sizes: a front below the minimum panel size becomes flat; handle model is shared, its position is computed from the front height; adjacent drawer fronts are cut in sequence from one board (grain continuity).

### Rule feedback (three levels, always visible)
| Level | Behaviour | Source |
|---|---|---|
| **Blocked** | physically impossible value is not accepted, with the reason | geometry |
| **Warning** | allowed, needs confirmation, suggests a fix ("use Z-clip") | rules R1–R19 with `Confidence` |
| **Info / Needs testing** | outside the validated domain of a published equation → *"test per EN 1728/1730"*, never an extrapolated capacity | `RuleCatalog`, R3/R19 |
Unsuitable options stay in the list, greyed, with the reason (e.g. "dowels: rail too thin for 2 dowels").
**Never coded** (shown as unavailable): Janka hardness (R17 → Brinell), Hu & Chen bending Fb (R18), numeric capacity of glue-free joints (R19).
Compliance flags (R14/R15): formaldehyde ≤ 0.062 mg/m³ for boards, GPSR dossier (10 y), EUDR DDS numbers (5 y) as checklist items in the Docs tab.

### Room-level purchase
All pieces of a room are optimized **together** (one purchase list, remnants shared). The status bar shows the saving versus buying each piece separately (`RoomResult.Saving`).

## Next implementation steps
1. Studio window (3 columns) on top of `Workspace`/`Room` + technical sheet renderer (top/front/side with dimensions + materials list).
2. Detail library (edge profiles, foot shapes, front styles, handles) as data with rules and manufacturing operations.
3. Casework pieces (nightstand, dresser, wardrobe, bed) so a bedroom can actually be built; DoorDefinition/WindowDefinition.
4. Data-driven roles/rules from `knowledge/Wood/PLUGIN_DATA/joint_roles_and_rules.json`.
