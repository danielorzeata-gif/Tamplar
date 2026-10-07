# Known limitations (v1.0.0)

**Verification gaps**
- The Rhino plugin (`RhinoWood.Plugin`) has been compiled against RhinoCommon 8 but **not executed inside Rhino** (the
  authoring environment had no Rhino). Expect small first-run fixes in Eto layout / panel registration. Acceptance criteria
  1–4 must be confirmed on a Windows machine with Rhino 8.
- No .yak package is built (a `manifest.yml` is provided; `scripts/build.ps1` creates a zip, `scripts/install.ps1` installs
  into the user's Rhino package folder).

**Engineering / modelling**
- All parts are axis-aligned boxes; features (mortises, holes…) are shown as separate cut-volume primitives in Engineering /
  Manufacturing mode and are **not** Boolean-subtracted (by design, for performance). Mitred tenon ends and sloped features
  (dovetail, scarf, wedge) are stored as angle metadata, not as true sloped geometry.
- Only the dining table is implemented; the framework (`IFurnitureDefinition`) is ready for other categories.
- Overrides are numeric (Add/Replace) on selected graph nodes (`leg.section`, `apron.height`, `overhang`, `leg.height`).
- Seasonal movement uses a single tangential coefficient per species and a configurable moisture swing.

**Optimization**
- One-dimensional cutting stock: each board yields pieces along its length. Boards are not ripped into several lanes, so a
  90×30 apron cut from a 40×140 board counts the unused cross-section as "section excess" (shown as process waste).
- Heuristic (multi-policy packing + local improvement), not a proven optimum; deterministic and verified to always honour
  kerf/end trim and commercial lengths.
- Defects (knots, checks) are only covered through the reserve; there is no per-board defect map.
- Prices are per m³ (stock or species) – per-piece supplier price lists can be imported by merging a user library.

**Persistence / data**
- System and user data are stored as JSON files (embedded, no server) instead of SQLite; schema in `docs/DATA_MODEL.md`.
  A SQLite store can be added behind the same `LibrarySerializer` boundary.
- Excel import/export and Grasshopper components are not included (optional modules); CSV export covers BOM, cut list,
  procurement and operations.
- Documentation drawings are SVG (top/front/side, assembled and exploded oblique) rather than Rhino layouts / TechDraw.

**UI**
- The panel presents all 12 sections; editing is limited to parameters, species, strategy, display mode and settings
  (detail editors for custom joints/components are command-line / JSON based in V1).
