---
purpose: C# XML doc comments authored in caveman Full style across codebase
audience: agent
loaded_by: router
slices_via: none
description: Applies caveman-full compression to /// XML doc bodies (summary, param, returns, exception, remarks). Keeps structured tags + identifiers verbatim.
globs: "**/*.cs"
alwaysApply: false
---

# XML doc comments — caveman Full default

All C# XML doc prose written in caveman **Full** style. Reduces tooltip + transcript token cost ~50%. Technical substance preserved.

## Rules

- Drop articles (a/an/the), filler (just/really/basically/simply/actually), pleasantries, hedging.
- Fragments OK. Pattern: `[thing] [action] [reason]. [next step].`
- Short synonyms: `fix` not `implement a solution for`, `return` not `returns and yields`.
- Arrows `→` allowed for causality.
- Keep verbatim: identifiers, flag values, enum names, numeric literals, `<see cref="..."/>`, `<paramref name="..."/>`, `<typeparamref/>`, `<c>` code fragments, `<code>` blocks.
- Keep normal English: security/auth rationale, license headers, `[Obsolete]` migration notes, public SDK surface consumed externally (none in this repo).

## Applies to

- `<summary>` — class, struct, enum, method, property, field, event, delegate.
- `<param name="...">` — parameter description body.
- `<returns>` — return description body.
- `<exception cref="...">` — exception description body.
- `<remarks>` — prose body.
- `<value>` — property value description body.

## Skip

- Single short sentence already ≤ ~8 words and no filler → leave alone. Example: `/// <summary>Gets the pivot cell.</summary>` stays.
- `<inheritdoc/>` — no prose.
- Auto-generated files (e.g., `*.Designer.cs`, protobuf/codegen output).

## Examples

### summary — full method doc

Before:
```csharp
/// <summary>
/// Returns the pivot cell for a multi-cell building. If the given cell is part of the building footprint, finds and returns the pivot cell (isPivot=true).
/// </summary>
```

After:
```csharp
/// <summary>
/// Return pivot cell of multi-cell building. If given cell inside footprint, find + return pivot (isPivot=true).
/// </summary>
```

### class summary with deps

Before:
```csharp
/// <summary>
/// Manages all road-related operations including placement, validation, and cache invalidation.
/// Depends on GridManager for cell access and PathfindingService for route computation.
/// </summary>
```

After:
```csharp
/// <summary>
/// Manage road ops: placement, validation, cache invalidation.
/// Deps: <see cref="GridManager"/> (cells), <see cref="PathfindingService"/> (routes).
/// </summary>
```

### param + returns

Before:
```csharp
/// <param name="cell">The cell to check for water adjacency.</param>
/// <returns>True if the cell has at least one water neighbor in its Moore neighborhood.</returns>
```

After:
```csharp
/// <param name="cell">Cell to check water adjacency.</param>
/// <returns>True if cell has ≥1 water neighbor in Moore neighborhood.</returns>
```

### exception

Before:
```csharp
/// <exception cref="ArgumentNullException">Thrown when the grid parameter is null.</exception>
```

After:
```csharp
/// <exception cref="ArgumentNullException">grid null.</exception>
```

### remarks

Before:
```csharp
/// <remarks>
/// This method is called during the simulation tick and must be thread-safe because it may run on a worker thread.
/// Callers must ensure the cell cache is invalidated before invoking.
/// </remarks>
```

After:
```csharp
/// <remarks>
/// Called during sim tick. Must be thread-safe (may run on worker). Caller must invalidate cell cache first.
/// </remarks>
```

## Authoring

- New code → caveman Full by default.
- Modified method → compress its XML doc same commit.
- Mass rewrite landed via dedicated sweep commit; don't re-expand.

## Do not

- Compress `[DllImport]` / `[StructLayout]` attribute-adjacent docs that describe P/Invoke contract nuance (none in repo today — flag if added).
- Compress XML docs inside auto-generated files.
- Remove `<see cref>` / `<paramref>` refs to save tokens — they drive IDE IntelliSense.
- Translate to another language. English only.
