# User guide

## 1. Create a project
`WoodNewTable` → pick the species (Enter = Oak) → enter length, width, height, top thickness. Rhino Wood generates:
top (edge-glued strips), 4 legs, 4 aprons, 8 mortise-and-tenon joints, 12 tabletop fasteners, the optimized purchase plan,
BOM, cost and manufacturing operations. The dockable panel (`WoodPanel`) shows everything in 12 sections.

## 2. Change the design
- `WoodSet` (or the FURNITURE tab) changes a parameter; only affected nodes recalculate. Advanced parameters (apron size,
  overhang, set-back, fastener spacing) are hidden in a collapsed section.
- Leg section is **rule driven** (≈ length / 22.5, rounded to 5 mm, 60–120 mm) unless you enter a value.
- `WoodOverride`: add an offset to a calculated value (e.g. overhang +10). The rule keeps working underneath; the panel
  shows `rule → override → final`. Moving a generated object by hand is never silently reverted: after the next change you are
  asked **Recalculate / KeepManualOverride / ConvertToCustomComponent**.

## 3. Display modes (`WoodDisplay`)
Performance (simple parts) · Normal · Engineering (joinery, holes, hardware, grain arrows) · Manufacturing (+ operation
markers). Detail is generated on demand and cached; grain display can be toggled separately.

## 4. Material optimization (OPTIMIZATION / PROCUREMENT tabs, `WoodOptimize`)
All parts of the project are optimized together. The report shows *why*: theoretical → rough (allowances) → optimized → commercial
purchase, kerf, remnants reused by other parts, reserve (applied once, after optimization) and a graphical cut plan.
Strategies: MinPurchase (default), MinWaste, MinCost, MinBoards, GrainFirst, Balanced. Reserve: global 10 %, species-specific
(e.g. walnut 15 %) or per project (`WoodSettings`).

## 5. Validation (ENGINEERING tab, `WoodValidate`)
INFO / WARNING / ERROR, e.g. mortise deeper than the leg, cross-grain glued joints, insufficient allowance, stock cannot
produce a part, tabletop fastener travel smaller than the expected seasonal movement, excessive waste, slender legs.

## 6. Documentation (`WoodReport`)
Exports `bom.csv`, `cutlist.csv`, `part_families.csv`, `procurement.csv`, `manufacturing_operations.csv`, `traceability.txt`,
`optimization.txt`, `validation.txt`, `joinery.md`, `hardware.md`, `assembly.md`, SVG drawings (top/front/side/assembled/exploded/cut plan),
`project_summary.html`, `project.rhinowood.json`.

## 7. Save / reopen
Project data is stored inside the `.3dm` automatically on save and restored when the file is opened; `WoodSave` / `WoodOpen`
also read/write the standalone `.rhinowood.json`. After reopening, the plan is rebuilt and compared with the stored snapshot.

## 8. Custom species
`WoodAddSpecies` adds a species (price, density, movement) with the standard commercial profiles to the user library
(`user-library.json`, editable for exact supplier sizes/prices).
