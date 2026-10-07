# Data model and persistence

Three separated data domains:

| Domain | Contents | Storage |
|---|---|---|
| **System** | default species, stock catalogue, tools, hardware, joint library, rules, validation rules | compiled into `RhinoWood.Core` (`WoodLibrary.CreateDefault`, `JointRegistry.CreateDefault`) |
| **User** | custom species, stock profiles, suppliers, tools, hardware, custom joints | `user-library.json` in the plugin settings directory (`PlugIn.SettingsDirectory`, via `LibrarySerializer`) |
| **Project** | parameters, overrides, selections, settings (+ snapshot of the resulting plan) | one string in the `.3dm` (`RhinoWood/project`) and/or `*.rhinowood.json` |

The project file stores **only the source of truth** (furniture type, species, parameters, overrides, settings). Parts,
joinery, hardware, optimization, BOM and operations are *derived* deterministically through the dependency graph when the
project is opened; a stored snapshot (fingerprint, board count, purchase list, cost) verifies that the rebuilt plan is
identical (`ProjectSerializer.Open` → `snapshotMatches`).

## `*.rhinowood.json` schema (schema version 1)

```jsonc
{
  "SchemaVersion": 1, "PluginVersion": "1.0.0",
  "ProjectId": "PRJ-1a2b3c4d", "Name": "Oak dining table",
  "FurnitureTypeId": "table.dining", "SpeciesId": "OAK",
  "Parameters": { "length": 1800, "width": 900, "height": 760, "topThickness": 35, "legSectionUser": 0, "apronHeight": 80, ... },
  "Overrides": [ { "NodeId": "overhang", "Mode": "Add", "Value": 10 } ],
  "CustomComponents": [ "overhang" ],
  "Settings": { "Rules": { "SectionBands": [...], "LengthAllowance": 30, "SawKerf": 3, "EndTrim": 0, "MinReusableRemnant": 300, ... },
                "Strategy": "MinPurchase", "GlobalReservePercent": 10, "ReserveOverrides": { "WALNUT": 15 }, "Currency": "EUR", "Display": "Normal" },
  "Snapshot": { "Fingerprint": "…", "BoardCount": 6, "PurchasedM3": 0.1388, "TotalCost": 525.8, "Purchase": ["4 x OAK 40x140 x 4000", ...] },
  "SavedUtc": "…"
}
```

Schemas newer than the running plugin are rejected with a clear message (`NotSupportedException`) instead of being
silently mis-read. Rhino objects carry only: `rw.project`, `rw.key`, `rw.part`, `rw.version`, `rw.type`, `rw.origin`.

## Domain classes (RhinoWood.Core.Domain)

`WoodSpecies`, `Supplier`, `StockItem` (species × section × available lengths), `PartFamily` (quantity, finished dims, rough
pieces, grain), `PartInstance` (id, bounds, local axes, features), `Feature` (kind, position/direction/diameter/depth or local
box, tool, source joint/hardware), `JointInstance`, `HardwareInstall`, `Issue`.
Optimization: `CutDemand → Board → CutPlacement`, `PurchaseLine`, `RemnantInfo` (PurchasedStock / ProjectRemnant /
ProcessWaste / Scrap), `OptimizationResult` (theoretical / optimized / commercial volumes + explanation lines).
