# TECH-37 — Computational infrastructure + pilot MCP tool

> **Issue:** [TECH-37](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-03
> **Last updated:** 2026-04-03

**Parent program:** [TECH-36](TECH-36.md) · **Next phase:** [TECH-38](TECH-38.md)

## 1. Summary

Establish **`tools/compute-lib/`** as the shared **TypeScript** package for **JSON** schemas, **pure** math that is safe to duplicate under **golden** tests, and imports consumed by **`tools/mcp-ia-server/`**. Define **C#** conventions for **pure** **computational** helpers under **`Assets/Scripts/`**. Ship **one** new **territory-ia** **`registerTool`** pilot that proves end-to-end wiring, **`npm run verify`**, and **`docs/mcp-ia-server.md`** updates.

## 2. Goals and Non-Goals

### 2.1 Goals

1. **`tools/compute-lib/`** with `package.json`, `tsconfig.json`, **`node:test`** (or project-standard test runner), and at least one passing unit test.
2. **Dependency edge:** `tools/mcp-ia-server` declares **`file:../compute-lib`** (or **npm workspaces** at repo root if already introduced — prefer minimal churn: **`file:`** first).
3. **Zod** (or existing MCP validation stack) schemas for **computational** DTOs shared between MCP handlers and tests.
4. **C#** namespace + folder convention documented (e.g. `Territory.Utilities.Compute` under `Assets/Scripts/Utilities/Compute/`) for **stateless** / **static** types — **no** new **singletons**; **no** **`gridArray`** access outside **`GridManager`**.
5. **One** pilot MCP tool (recommended: **`isometric_world_to_grid`** or **`grid_world_to_cell`**) using **compute-lib** for the numeric core, with **English** description aligned to **glossary** **World ↔ Grid conversion** (**geo** §1.1, §1.3).

### 2.2 Non-Goals

1. Extracting large **Unity** **managers** (**TECH-38**).
2. Full suite of **computational** MCP tools (**TECH-39**).
3. Changing **player-visible** **game** rules.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | MCP maintainer | I want shared types in **compute-lib** so tools do not duplicate Zod. | MCP imports from **compute-lib**; tests run in CI/local for both packages. |
| 2 | Unity developer | I know where **pure** **grid** math lives before extractions land. | **TECH-37** **Decision Log** names folder + namespace; empty `README.md` or `.gitkeep` in folder optional. |
| 3 | IA agent | I can call one **new** tool and get structured JSON. | **`npm run verify`** includes tool smoke; **README** table lists the tool. |

## 4. Current State

### 4.1 Domain behavior

**World ↔ Grid conversion** is defined in **isometric-geography-system** §1.1 / §1.3. The pilot tool must **not** contradict those formulas; document any parameter names (tile size, origin offset) explicitly in the tool schema.

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| MCP registration | `tools/mcp-ia-server/src/index.ts` |
| Verify | `tools/mcp-ia-server/scripts/verify-mcp.ts` (or equivalent) |
| Docs | `docs/mcp-ia-server.md`, `tools/mcp-ia-server/README.md` |
| Program charter | [TECH-36](TECH-36.md) |
| JSON program | **TECH-21** program — **TECH-40** (**artifact** / schema policy); **TECH-41** (**GeographyInitParams** and interchange DTOs). Align **Zod** field names when overlapping. |

## 5. Proposed Design

### 5.1 Target behavior (product)

N/A — tooling only.

### 5.2 Architecture

**Package layout (`tools/compute-lib/`):**

```
tools/compute-lib/
  package.json          # name: "@territory/compute-lib" or territory-compute-lib
  tsconfig.json
  src/
    index.ts            # re-exports
    schemas/            # Zod: world_point, grid_point, conversion_params
    isometric/          # pilot: worldToGrid / gridToWorld (pure functions)
  test/
    *.test.ts
```

**MCP server:** Add dependency, import `worldToGrid` (or equivalent) inside the pilot tool handler; keep **handler thin** — validation + call + JSON response.

**C# mirror (documentation + optional stub):** Add **`Assets/Scripts/Utilities/Compute/README.md`** (English) describing that **Unity** remains **authoritative**; **Node** duplicates **only** formulas backed by **golden** tests. Optionally add **`IsometricGridConvert.cs`** with **static** methods **matching** **CoordinateConversionService** once **TECH-38** aligns constants — **TECH-37** may leave C# as **doc-only** if timeboxed.

**Golden test flow (pilot):**

1. Add **Editor** or **batchmode** one-shot that prints **one** **world** point and expected **grid** **cell** (or use existing **TECH-28** export if compatible).
2. Commit **JSON** golden under `tools/compute-lib/test/fixtures/world-to-grid.json`.
3. **Node** test loads fixture and asserts **compute-lib** output.

### 5.3 Pilot tool contract (illustrative)

- **Name:** `isometric_world_to_grid` (**`snake_case`**).
- **Input:** `world_x`, `world_y`, `tile_width`, `tile_height`, optional `origin_x`, `origin_y` (names final per Zod schema).
- **Output:** `cell_x`, `cell_y` integers; `ok: true` or structured `error` with **English** message.
- **Limits:** Reject absurd inputs (NaN, zero tile size) without throwing uncaught.

## 6. Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-04-03 | **`tools/compute-lib/`** root | User direction; isolation from MCP glue |
| 2026-04-03 | Pilot = **World ↔ Grid** | Small surface; high agent value; matches **glossary** |
| 2026-04-03 | **`file:`** dependency MCP → compute-lib | Minimal repo churn until workspaces needed |

## 7. Implementation Plan

### 7.1 Scaffold `tools/compute-lib`

- [ ] Create **`tools/compute-lib/package.json`** with `name`, `version`, `type: module`, scripts: `test`, `build` (if tsc emit needed — else **tsx** direct like MCP).
- [ ] Add **`tsconfig.json`** compatible with **Node** LTS used by **mcp-ia-server** (match **`target`/`module`** to avoid dual resolution pain).
- [ ] Add **`src/index.ts`** exporting **schemas** and **isometric** functions.
- [ ] Add **`test/`** with **`node:test`** (or project convention); **`npm test`** passes with **zero** tests initially, then **one** test for **worldToGrid**.

### 7.2 Implement pure isometric conversion in TypeScript

- [ ] Transcribe **geo** §1.1 / §1.3 formulas into **`src/isometric/worldToGrid.ts`** (and inverse if useful for symmetry).
- [ ] Add **Zod** schemas in **`src/schemas/isometric.ts`** for inputs/outputs.
- [ ] Document **parameter** mapping to **Unity** `CoordinateConversionService` / **GridManager** constants in **`compute-lib/README.md`** (English).
- [ ] Export **golden** vectors: either manual **JSON** from **Unity** or spreadsheet → **`test/fixtures/`**; assert **≤ 1e-5** float tolerance if floats involved.

### 7.3 Wire `tools/mcp-ia-server` to `compute-lib`

- [ ] Add **`"territory-compute-lib": "file:../compute-lib"`** (or chosen name) to **mcp-ia-server** `package.json`.
- [ ] Run **`npm install`** from **mcp-ia-server**; fix **TypeScript** path if needed.
- [ ] Create **`registerTool`** handler file (e.g. **`src/tools/isometricWorldToGrid.ts`**) importing from **compute-lib**; keep **< ~80 lines** per handler pattern used elsewhere in server.

### 7.4 Register pilot tool and verify

- [ ] **`registerTool('isometric_world_to_grid', …)`** in **`index.ts`** with **English** description citing **World ↔ Grid conversion** and **geo** §1.
- [ ] Extend **`verify-mcp.ts`** (or equivalent) to invoke the new tool with a **fixture** input and assert shape of output.
- [ ] Run **`npm run verify`** from **mcp-ia-server** until green.

### 7.5 Documentation

- [ ] Update **`docs/mcp-ia-server.md`**: increment tool count; add row to tools table; **terminology** note (glossary **English**).
- [ ] Update **`tools/mcp-ia-server/README.md`** mirror table.
- [ ] Add **`tools/compute-lib/README.md`**: purpose, authority (**C#** wins on conflict), how to add **golden** tests.

### 7.6 C# convention (minimal)

- [ ] Create **`Assets/Scripts/Utilities/Compute/`** (or chosen path) with **README.md** stating namespace **`Territory.Utilities.Compute`**, **no** **MonoBehaviour** in this folder, **GridManager** **GetCell** for **cell** reads.
- [ ] Optional: stub **static** class **`IsometricGridMath`** with **XML** **`<summary>`** pointing to **geo** §1 — implementation can be **TECH-38** if **CoordinateConversionService** move is risky now.

### 7.7 Handoff to TECH-38

- [ ] Open **TECH-38** checklist item: “align **C#** **IsometricGridMath** with **compute-lib** **golden** set”.
- [ ] List **JSON** DTO names that **TECH-41** (and **TECH-40** schema **`$id`s**) should reuse or alias.

## 8. Acceptance Criteria

- [ ] **`tools/compute-lib`** installs and **`npm test`** passes.
- [ ] **MCP** **`npm run verify`** passes with new tool exercised.
- [ ] **Docs** updated (**mcp-ia-server.md** + README + **compute-lib** README).
- [ ] **Decision Log** + **Implementation Plan** checked items for **7.1**–**7.5** complete or explicitly deferred with date in **Issues Found** table.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- 

## Open Questions

None — charter decisions in [TECH-36](TECH-36.md).
