# Architecture

## Technical stack
- **C# / .NET**: `RhinoWood.Core` targets `netstandard2.1` (and its sources are compiled into the plugin so `RhinoWood.rhp` is a single self-contained file) (no Rhino dependency → fully testable headless); the plugin targets
  `net7.0-windows` (Rhino 8 default runtime) with **RhinoCommon 8** and **Eto** (panel).
- **Persistence**: System.Text.Json files (embedded, no server) behind `ProjectSerializer` / `LibrarySerializer`.
- **UI**: Eto dockable panel + command-line commands; Grasshopper and Excel are optional and not required.
- **Tests**: xUnit (net8.0). **CI**: `.github/workflows/ci.yml`.

## Layering

```
PROJECT MODEL  (WoodProject: furniture type, species, parameters, overrides, settings)   ← source of truth
      │
PARAMETRIC ENGINE   DependencyGraph (inputs + computed nodes, early cut-off, evaluation log)
      │
RULE ENGINE         ManufacturingRules (allowances, kerf, trims), joint/hardware rules, ValidationRules
      │
FURNITURE MODEL     IFurnitureDefinition.Assemble → PartFamily/PartInstance, JointInstance, HardwareInstall, Features
      │
OPTIMIZATION        CutDemand → StockOptimizer → OptimizationResult (boards, cuts, remnants, purchase lines)
      │
MANUFACTURING/COST  ManufacturingPlanner (operations + traceability), CostEngine (BOM + cost)
      │
GEOMETRY ENGINE     GeometryEngine + GeometryCache → GeometryPrimitive (LOD: Performance/Normal/Engineering/Manufacturing)
      │
RHINO MODEL         RhinoSync (plugin): primitives → Breps/curves tagged with 5 lightweight user strings
```

Business logic never lives in the UI or in Rhino objects. The Rhino document stores one project string plus tagged geometry.

## Key design decisions

1. **Dependency graph** (`Parametric/DependencyGraph.cs`). Nodes declare their dependencies; reading an undeclared node throws.
   `Set()` marks dependents dirty; `Get()` pulls. A computed node re-runs only if a dependency's *fingerprint* changed
   (early cut-off), so e.g. changing the table width does not recompute length-only nodes, and clamped/ruled values stop
   propagation. `EvaluationLog`/`EvaluationCount` expose exactly what recomputed (used by tests and the Rhino command line).
2. **Overrides**: a `NumericOverride` (Add/Replace) is applied *on top of* the node's computed value, which keeps being
   recalculated; user work is never lost. `ResolveOverride` offers Recalculate / Keep / Convert-to-custom (freezes the final value).
   In Rhino, `RhinoSync` never overwrites an object whose geometry was moved by hand; it asks for the same three resolutions.
3. **Part families**: a `PartFamily` has `Quantity` instances and `RoughPieces` (e.g. top = 7 strips). Demands are generated per
   instance/piece, but optimized **all together**.
4. **Global optimizer** (`Optimization/Optimizer.cs`):
   1. rough dimensions = finished + rule-driven allowances (before optimization – RULE 6);
   2. pieces are packed on commercial boards (species, section fit incl. rotation, kerf, end trim) by several policies
      (longest-first / most-constrained-first × utilization-lookahead / smallest-fit / largest-stock / each fixed commercial
      length), each followed by local improvement (re-type board to the cheapest profile/length that still fits, merge board
      pairs, empty boards by moving pieces into other boards' slack);
   3. the plan best for the chosen strategy is selected (MinPurchase, MinWaste, MinCost, MinBoards, GrainFirst, Balanced);
   4. leftover tails become **project remnants** (≥ `MinReusableRemnant`) or scrap; tails are available to *every* later piece
      because all pieces of a species compete for the same boards (cross-use of remnants);
   5. **reserve** (global / stock / species / project override) is applied **once, after** optimization: target = reserve% × rough
      volume of that species; it is first covered by the plan's reusable remnants and only extra boards are added if remnants
      do not suffice (never multiplied);
   6. purchase lines (commercial quantities) and an explanation (`theoretical → rough → optimized → commercial`) are produced.
5. **Traceability**: every `CutPlacement` references its `CutDemand` (part id); `ManufacturingPlanner` builds `PartTrace`
   (purchase line, board, cut, operations, joints, hardware, assembly).
6. **Geometry LOD + cache**: `GeometryEngine` builds per-part primitive lists keyed by part fingerprint (bounds + features) and
   mode; unchanged parts hit the cache. Detail (joinery, hardware, grain, operation markers) exists only in Engineering /
   Manufacturing modes and is never Boolean-subtracted.

## Extension points (no core rewrite)
| Add | How |
|---|---|
| Furniture | implement `IFurnitureDefinition`, `FurnitureRegistry.Register` (test `NewFurnitureType_WorksWithoutChangingTheCore`) |
| Component | build `PartFamily` objects in a graph node (see `TableDefinition`) |
| Joinery | derive from `JointBase`/implement `IJointDefinition`, `JointRegistry.Register`; or `CustomJoint` templates |
| Hardware | add `HardwareItem` with `HolePatternEntry` list to the library (installer generates features) |
| Material / tool / supplier | add to `WoodLibrary` (user library JSON) |
| Rule | `ManufacturingRules` values or `IValidationRule` via `Validator.Register` |
| Operation | extend `ManufacturingPlanner.Classify` / `OperationType` |
| Strategy | extend `OptimizationStrategy` + `SelectBest` comparer |
| Export | add a function to `ReportBuilder` and a line in `ReportWriter` |
| AI assistant (future) | operate on `WoodProject` (`SetParameter`, `SetOverride`, `Settings`, `Recalculate`) – never on Rhino geometry |
