# Changelog

## Unreleased
- `WoodStart`: fereastră cu taburi independente. Primul tab **Configurare** = interfața Atelier; celelalte (Proiect, Afișare, Optimizare, Suprascrieri, Fișă tehnică, Documente, Verificări, Setări, Materiale, Despre) înlocuiesc comenzile din linia de comandă (care rămân disponibile).

## 1.1.0 (unreleased)
- Selectable joints per connection (mortise-tenon, loose tenon, dowel, bridle, Japanese kusabi, biscuit, pocket screw) and tabletop
  fixing (Z-clip, figure-8, wooden button); rail lengths, tenons, holes and hardware follow the choice; choices persist.
- `JointInfo` (strength, difficulty, pros/cons, source) for every joint; THROUGH_CONFLICT validation.
- `PreviewEngine` (2D oblique preview from the Core model); 83 tests.
- Room workspace: `Workspace/Room/PieceEntry/StyleSet` (Aspect + Structure sets per room), field provenance (standard / set / piece override), "only here" vs "whole set" edits, re-link, conflicts reported instead of silently applied; one global optimization per room (never worse than separate plans) and workspace persistence.
- Optimizer: reserve is now part of candidate comparison and is covered by the cheapest commercial option (fixes tighter plans being penalised).
- `RuleCatalog` R1-R19 with confidence levels (safe / provisional / conditional / typology-only / forbidden) + tested helpers (EN 14749/12521 stability, finger traps, panel movement, chair capacities, tenon equation E1 with domain check); stability validation rule.
- Species data replaced by verified DIN 68364/68100 values (`DataLabel` [V-DATA]/[UNVERIFIED]); Janka removed (R17); default moisture swing 5 points.
- `knowledge/Wood/13_DESIGN_AND_ENGINEERING/RULES_EN_PEER_REVIEW_2026.md` added; `docs/UI_SPEC.md`.
- PDF sheets in the Atelier style (Romanian, diacritics): technical sheet (live preview), joint dimensions, cutting plan + cut list, order & cost, assembly notes; SALE mode (offer only, no internal costs/waste/stock codes); `WoodPdf`, `WoodSheet` commands and a "Fișă tehnică" panel.
- Atelier panel (Eto): tokens, controls and the full panel with ECONOMA/STANDARD/PREMIUM variants, draft + viewport preview before generation; modal new-project dialog removed; DEPOZIT_LEMN replaced by "Necesar lucrare".
- `knowledge/Wood`: imported knowledge base + iteration 9 research on doors, windows, furniture matrix, hardware; `docs/ROADMAP_V2.md`.

## 1.0.0
First complete release (roadmap V1: core + parametric table + material optimization).

- Project model, dependency graph (pull-based, early cut-off, per-node evaluation counters), numeric manual overrides
  (Add/Replace, Recalculate / Keep / Convert-to-custom resolutions).
- Materials: 10 species + custom species, commercial stock catalogue, rule-driven manufacturing allowances.
- Part families (identical parts are one procurement problem), rough pieces, glued panels (top strips).
- Joinery: mortise & tenon (mitred when mortises collide), loose tenon, dowel, biscuit, dovetail, finger, box, dado, rabbet,
  half-lap, bridle, scarf, Japanese wedged through tenon (kusabi), custom template joints.
- Holes/features as first-class data; hardware installers that generate holes/slots/pockets (Z-clips, slotted-hole screws).
- Global stock optimizer (multi-policy packing + merge/empty/retype improvement, 6 strategies), remnant reuse, kerf, reserve
  applied once after optimization, purchase lines, explanation text.
- Manufacturing operations, full traceability (part → purchase → board → cut → operations → joinery/hardware → assembly).
- BOM, cut list, procurement list, cost engine, validation (INFO/WARNING/ERROR), documentation export (CSV, SVG, HTML, MD).
- Persistence (JSON project file + single string in the .3dm), project reopening with snapshot verification.
- Rhino 8 plugin: commands, lightweight geometry sync with LOD display modes, manual-edit protection, dockable panel.
