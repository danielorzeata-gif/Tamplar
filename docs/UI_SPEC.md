# UI specification — "Rhino Wood Studio" (agreed decisions, 2026-10-07)

Status: **specification + Core model done (Workspace/Room/StyleSet, rule catalog, 100 tests)**; the Studio window itself is next.

## Decisions taken with the user
| # | Decision |
|---|---|
| 1 | **Two style sets per room**: *Aspect* (visible: species, front style, handle, edge profile, foot shape, visible gaps) and *Structure* (how it is built: joints, fixings, hidden hardware). |
| 2 | **Room level**: Workspace → Room → Piece → Part. Each room has its own two sets; rooms are independent. |
| 3 | **Editing a field of a piece** defaults to **"only here"**; the notification offers **"apply to the whole set"**. |
| 4 | **No visual finish/colour for now** (species + finish label/quantity only). |
| 5 | **Design system: "Atelier" (the user's PDF, v1, 2026-10-07)** — one docked panel of 320 px (control height 24, row 22, 4 px grid), modes **DESIGN / VÂNZARE**, tiers **ECONOMA → STANDARD → PREMIUM**, stock truth = **DEPOZIT_LEMN**, waste always red, Romanian with diacritics, no emoji. This **replaces** the earlier "Studio window" and 3-column ideas. |
| 6 | **Documents**: the **technical sheet** is the live preview inside the working interface; **joint dimensions**, **cutting plan** and **assembly notes** are separate sheets, exported together in one **PDF**. |

## How the 3 columns map onto Atelier
Atelier forbids windows ("everything happens in one 320 px panel, modal dialogs only for delete confirmations"). The three areas the user described therefore become:
```
┌ Rhino viewport (centre) ───────────────┐  ┌ Atelier panel 320 px (dock) ─┐  ┌ "Fișă tehnică" panel ─┐
│ live 3D preview (display conduit),     │  │ Atelier        [DESIGN|VÂNZARE]│  │ top / front / side     │
│ selected part highlighted, waste red   │  │ ▾ ELEMENT   Cameră › Masă › …  │  │ dimensions + materials │
│ ← "Previzualizare" shows the piece     │  │ ▾ CAMERĂ    set Aspect/Structură│  │ list, scale 1:20       │
│    BEFORE "Generează" creates it       │  │ ▾ DIMENSIUNI  ParamField −/+   │  │ refreshes on every     │
└────────────────────────────────────────┘  │ ▾ VARIANTĂ  ECONOMA STANDARD PREMIUM│  │ change (WebView)       │
                                            │ ▾ ÎMBINĂRI  per connection ▼  │  └────────────────────────┘
                                            │ ▾ DEBITARE  CutList (DESIGN)   │
                                            │ ▾ COST / OFERTĂ  PriceSummary  │
                                            │ [Previzualizare]  [Generează]  │
                                            └────────────────────────────────┘
```
* left column (structure) → section **CAMERĂ** (rooms, pieces, set summary) in the same panel;
* right column (properties) → sections **DIMENSIUNI / ÎMBINĂRI** of the selected piece;
* centre (preview) → Rhino viewport (3D) + the **Fișă tehnică** panel (2D sheet).

**Tiers = Structure-set presets.** ECONOMA / STANDARD / PREMIUM choose, per role, the joint family (see the `imbinari-colt` skill: ECONOMA fast & simple, STANDARD repeatable "product", PREMIUM visible craft joints). The user can still override individual joints (field provenance stays: set / piece override).

**DESIGN vs VÂNZARE.** DESIGN shows joints, cutting list, waste and production cost. VÂNZARE shows only dimensions, variant and the customer price; it never shows internal costs, stock codes or waste (enforced in `SheetBuilder` and tested).

## PDF export (implemented: `SheetBuilder`, `SheetDocument`, `PdfExporter`)
| Sheet | Content | Mode |
|---|---|---|
| 1 Fișă tehnică | 3 orthographic views with overall dimensions, numbered parts, materials list, oblique overview, joints & hardware, scale | DESIGN + VÂNZARE (live preview) |
| 2 Cote de îmbinare | per joint type: tenon end/side view, mortise face, parameters, operations + tools, checks R5 / R3 ("needs testing" when outside the validated domain) | DESIGN |
| 3 Plan de debitare | boards with cuts, reusable remnants, **waste in red**; cut list (piece, section, L, qty, stock/board, waste) | DESIGN |
| 4 Comandă și cost | purchase list incl. reserve, production cost, "why this order" | DESIGN |
| 5 Note de montaj | assembly order, hardware table + install notes, warnings | DESIGN |
| Ofertă | name, species, variant, dimensions, price (cost × (1 + margin)) | VÂNZARE |
PDF is rendered from HTML/SVG with an installed Chromium browser (Edge ships with Windows); if none is found the HTML is saved for manual printing.

## Field provenance, rules, room purchase (unchanged)
See previous sections: `FieldInfo.Source` (standard / set / piece override), three feedback levels (blocked / warning / info-needs testing) driven by `RuleCatalog` R1–R19, one global purchase per room (`RoomResult.Saving`).

## Next implementation steps
1. Replace the modal `NewProjectDialog` with the Atelier panel flow (Previzualizare → Generează) + Fișă panel (done) + tokens (colors/typography/spacing) in Eto.
2. **DEPOZIT_LEMN** import (`depozit_lemn.xlsx`: Cod, Grupa, Grosime, Latime, Diametru, Lungime, Pret, Lei/m, Specie, Observatii) → stock badges (● în stoc / ▲ stoc mic / ○ lipsă) and stock-first optimization.
3. Tier presets for the Structure set; detail library; casework pieces; door/window.
