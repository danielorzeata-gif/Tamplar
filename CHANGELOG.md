# Changelog

## 1.1.0 (unreleased)
- Selectable joints per connection (mortise-tenon, loose tenon, dowel, bridle, Japanese kusabi, biscuit, pocket screw) and tabletop
  fixing (Z-clip, figure-8, wooden button); rail lengths, tenons, holes and hardware follow the choice; choices persist.
- `JointInfo` (strength, difficulty, pros/cons, source) for every joint; THROUGH_CONFLICT validation.
- `PreviewEngine` (2D oblique preview from the Core model); 83 tests.
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
