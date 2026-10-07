# Developer guide

## Build & test
```bash
dotnet test tests/RhinoWood.Tests                  # headless, any OS with .NET 8 SDK
dotnet run --project samples/DemoRunner -- out/    # full workflow + all documents
dotnet build src/RhinoWood.Plugin -c Release       # produces RhinoWood.rhp (RhinoCommon from NuGet)
```
Windows packaging: `scripts/build.ps1`, install for the current user: `scripts/install.ps1`.

## Code map (`src/RhinoWood.Core`)
`Geometry.cs` Vec3/Box3 · `Domain.cs` entities · `DependencyGraph.cs` · `Rules.cs` · `Library.cs` (species, stock, tools, hardware)
· `Joinery.cs` · `Hardware.cs` · `Furniture.cs` (`IFurnitureDefinition`, `TableDefinition`) · `Optimizer.cs` · `Manufacturing.cs`
· `Costing.cs` · `Validation.cs` · `GeometryEngine.cs` · `WoodProject.cs` (pipeline, persistence) · `Reports.cs`.

## Adding a furniture type (outline)
1. `class Bench : IFurnitureDefinition` – declare `Parameters`.
2. `CreateGraph`: `AddInput` for parameters, `AddComputed` for rule-driven values and one node per component
   (return `Boxed<List<PartFamily>>`-like objects implementing `IFingerprint` so unchanged components are not re-run).
3. `Assemble`: clone component families, run `JointEngine.Apply(model, requests)` and `HardwareInstaller.Install`.
4. Register it in `FurnitureRegistry`. Optimization, BOM, cost, operations, validation, geometry and reports work automatically.

## Conventions
- Units: millimetres everywhere in Core; the plugin scales to the document units.
- Part-local frame: x = length, y = width, z = thickness, origin at the min corner (`PartInstance.LocalToWorld`).
- IDs are deterministic (`B001`, `C001`, `PUR001`, `J001`, `HW001`, `OP001`) so plans are reproducible and diffable.
- No business logic in UI/commands; commands call `WoodProject` and `RhinoSync` only.
