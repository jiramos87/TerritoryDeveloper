---
purpose: "C# coding standards for Territory Developer"
audience: agent
loaded_by: router
slices_via: none
description: C# coding standards for Territory Developer
globs: "**/*.cs"
alwaysApply: false
---

# Coding Conventions

Language: see `project-overview.md`.

## Documentation Requirements
- Every class MUST have a `/// <summary>` (2-4 lines): responsibility, key dependencies, how it fits in the system
- Public methods MUST have `/// <summary>`, with `<param>` and `<returns>` when applicable
- Files over 300 lines MUST use `#region` to group methods by category
- Inspector fields MUST use `[Header("Category Name")]` for visual grouping

## Naming
- PascalCase: classes, methods, public properties
- camelCase: private fields
- Enums: PascalCase for type and values
- Do NOT use C# reserved keywords as identifiers (e.g. `base`, `class`, `ref`, `out`, `in`)

## Prefabs and asset naming (new content)
- **Do not rename** existing prefab files or asset filenames — only apply conventions to **new** assets and variants.
- **Slope / zoning / building variants** (see geography spec §6.4): `{flatPrefabName}_{slopeCode}Slope` where `slopeCode` is `N`, `S`, `E`, `W`, `NE`, `NW`, `SE`, `SW`, `NEUp`, `NWUp`, `SEUp`, `SWUp`.
- **Roads, terrain, UI:** follow existing prefixes in each folder (e.g. `roadTilePrefab*`, `northSlopePrefab`).

## Dependency Pattern
```csharp
[Header("Manager References")]
[SerializeField] private GridManager gridManager;

void Awake() {
    if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
}
```

## Debug / temporary diagnostics
- For one-off cell inspection, prefer `if (x == debugX && y == debugY) Debug.Log(...)` with local constants — not a new logging framework or global toggles.
- If a temporary `Debug.Log` is needed, prefix it with `[DEBUG]` to make it easier to find.
- Remove or gate temporary `Debug.Log` before merging if it would spam the console.

Anti-patterns: see `AGENTS.md`.

## Reference Files
- Documentation model: `EconomyManager.cs` (XML docs + regions + headers)
- Region model: `Cell.cs` (4 well-named regions)
