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

Language: `project-overview.md`.

## Documentation

- Every class MUST have `/// <summary>` (2–4 lines): responsibility, key deps, system fit.
- Public methods MUST have `/// <summary>` + `<param>` / `<returns>` when applicable.
- XML doc prose authored caveman **Full** style — see [`xml-doc-caveman.md`](xml-doc-caveman.md). Tags (`<see cref>`, `<paramref>`), identifiers, numeric literals stay verbatim.
- Files >300 lines MUST use `#region` grouped by category.
- Inspector fields MUST use `[Header("Category Name")]`.

## Naming

- PascalCase: classes, methods, public properties.
- camelCase: private fields.
- Enums: PascalCase type + values.
- No C# reserved keywords as identifiers (`base`, `class`, `ref`, `out`, `in`).

## Prefabs / asset naming (new content only)

- Do NOT rename existing prefab/asset filenames — convention applies to NEW assets + variants.
- Slope/zoning/building variants (geography §6.4): `{flatPrefabName}_{slopeCode}Slope`, `slopeCode` ∈ `N`, `S`, `E`, `W`, `NE`, `NW`, `SE`, `SW`, `NEUp`, `NWUp`, `SEUp`, `SWUp`.
- Roads, terrain, UI: existing folder prefixes (`roadTilePrefab*`, `northSlopePrefab`).

## Dependency pattern

```csharp
[Header("Manager References")]
[SerializeField] private GridManager gridManager;

void Awake() {
    if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
}
```

## Debug / temporary diagnostics

- One-off cell inspection: `if (x == debugX && y == debugY) Debug.Log(...)` with local constants — no new framework / global toggle.
- Temporary `Debug.Log` → prefix `[DEBUG]`.
- Remove or gate before merging if it would spam console.

Anti-patterns: `AGENTS.md`.

## Static helpers / patch-data types (new types)

- Before adding a new static helper class or patch-data struct: grep the proposed type name in its namespace — duplicate names fail compile (CS0101).
- If a bare noun collides, suffix `-Stepper`, `-Builder`, or `-Service`.

## Reference files

- Docs model: `EconomyManager.cs` (XML + regions + headers).
- Region model: `Cell.cs` (4 named regions).
