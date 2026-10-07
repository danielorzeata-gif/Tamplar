# Rhino Wood

Woodworking CAD + engineering + manufacturing + procurement system for **solid-wood furniture** inside **Rhino 8**.

```
DESIGN → PARAMETRIC MODEL → ENGINEERING → JOINERY → HARDWARE → MATERIAL REQUIREMENT
→ GLOBAL MATERIAL OPTIMIZATION → PROCUREMENT → MANUFACTURING → ASSEMBLY → DOCUMENTATION
```

The **project model is the source of truth**; Rhino geometry is a lightweight, regenerated representation
(each Rhino object only carries project id, object key, part id, version and type).

## Status (v1.0.0)

| Area | State |
|---|---|
| Core engine (`RhinoWood.Core`, no Rhino dependency) | implemented, **66 automated tests pass** |
| Global material optimizer, reserve, procurement, cutting plan, remnants | implemented + tested (mandatory 4-legs test included) |
| Dependency graph, overrides, geometry cache, display modes (LOD) | implemented + tested |
| Joinery library (13 joint types + custom), holes, hardware, tools | implemented + tested |
| Solid-wood dining table (rules-driven) | implemented + tested end to end |
| Persistence (save / close / reopen / verify) | implemented + tested |
| Documentation exports (BOM, cut list, procurement, SVG drawings, HTML summary…) | implemented + tested |
| Rhino 8 plugin (`RhinoWood.Plugin`: commands, sync, dockable panel) | **compiles against RhinoCommon 8; not yet run inside Rhino** (see `docs/KNOWN_LIMITATIONS.md`) |

> The build environment used to create this repository was a Linux container without Rhino 8, so acceptance criteria
> 1–4 (install/load/UI/new project **inside Rhino**) still need a first run on your Windows machine. Everything else is
> exercised headlessly by the test-suite and by `samples/DemoRunner`.

## Quick start (Windows + Rhino 8)

```powershell
# 1. (optional) copy the project into your working folder
.\scripts\Sync-To-RhinoWoodFolder.ps1          # -> Desktop\Rhino Wood
# 2. build + test (needs the .NET 8 SDK)
.\scripts\build.ps1
# 3. install for the current user and restart Rhino 8
.\scripts\install.ps1
```

In Rhino: `WoodNewTable` → choose species and dimensions → geometry, joinery, hardware and the optimized purchase plan are
generated. `WoodPanel` opens the dockable panel with the 12 sections (PROJECT … SETTINGS).

Commands: `WoodNewTable`, `WoodSet`, `WoodOverride`, `WoodDisplay`, `WoodOptimize`, `WoodValidate`, `WoodReport`,
`WoodSave`, `WoodOpen`, `WoodSettings`, `WoodAddSpecies`, `WoodPanel`.

## Try the engine without Rhino

```bash
dotnet run --project samples/DemoRunner -- ./demo-output     # full 19-step workflow, writes all documents
dotnet test tests/RhinoWood.Tests                             # 66 tests
```

`samples/sample-oak-dining-table/` contains the generated output for the reference table (open `project_summary.html`).

## Repository layout

```
src/RhinoWood.Core      domain, parametric engine, rules, joinery, hardware, optimizer, procurement, manufacturing, reports
src/RhinoWood.Plugin    Rhino 8 plugin (.rhp): commands, Rhino geometry sync, Eto panel
tests/RhinoWood.Tests   xUnit tests (unit + integration)
samples/                DemoRunner + generated sample project
docs/                   ARCHITECTURE, USER_GUIDE, DEVELOPER_GUIDE, DATA_MODEL, KNOWN_LIMITATIONS
scripts/                build / install / sync / test
```

## Knowledge base

`knowledge/Wood/` holds the woodworking research the plugin is built on (joinery, species and wood movement, furniture,
doors, windows, hardware, finishing, ~165 sourced rules with verification labels) plus the original Python engine. Start with
`knowledge/Wood/00_INDEX/00_README_START_HERE.md`; the latest additions are in `00_INDEX/ITERATIA_9_USI_FERESTRE_MOBILIER.md`.
Next steps: `docs/ROADMAP_V2.md`.

See `docs/` for details and `CHANGELOG.md` for the release notes.


## Pornire rapidă
După instalare, în Rhino scrie `WoodStart`. Se deschide fereastra Atelier: primul tab **Configurare**, apoi taburi independente pentru restul funcțiilor (Proiect, Afișare, Optimizare, Suprascrieri, Fișă tehnică, Documente, Verificări, Setări, Materiale, Despre). Verifică versiunea în tabul **Despre** sau cu `WoodAbout`.
