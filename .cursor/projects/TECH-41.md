# TECH-41 — JSON payloads for current systems: geography, cell/chunk, snapshots, DTO layers

> **Issue:** [TECH-41](../../BACKLOG.md)  
> **Status:** Draft  
> **Created:** 2026-04-03  
> **Last updated:** 2026-04-03

**Parent program:** [TECH-21](TECH-21.md) · **Depends on:** **TECH-40** · **Feeds:** [TECH-42](TECH-42.md)

## 1. Summary

Implement **glossary-aligned** **JSON** for **today’s** game and tooling: **G4** declarative **Geography initialization** parameters (aligned with **TECH-15**, **TECH-38** Wave D, and planned **FEAT-46** knobs in [`docs/planned-domain-ideas.md`](../../docs/planned-domain-ideas.md)); **G2** single-**cell** or **chunk** interchange for harnesses and external validators; **G1** read-only world **snapshots** (Editor/dev); **E3** explicit **DTO** layering in code and docs; **P1** parse-once loading; **P2** static **catalog** tables (arrays) loaded at init — **not** per **simulation tick**; **P4** optional `by_id` maps for **hot** static lookups.

## 2. Goals and Non-Goals

### 2.1 Goals

1. At least **one** **runtime or Editor** path that loads **JSON** once per **New Game** or session boundary (**P1**) for **G4**.
2. At least **one** **export** path for **G2** (cell or chunk) and/or **G1** (subset snapshot) for debugging and **TECH-31**/**TECH-38** alignment.
3. Document **E3**: **`MonoBehaviour`** / managers → **interchange DTO** (JSON-shaped) → persistence **CellData** / **Save data** — schemas attach to **DTO** layer first.
4. Keep **HeightMap** / **`Cell.height`** invariant in any exported height fields.
5. Align field names with **TECH-39** `geography_init_params_validate` and **TECH-37** **`compute-lib`** where shared.

### 2.2 Non-Goals

1. Rewriting full **Save data** serialization to JSON.
2. Per-tick JSON read/write of large files (**P2**/**P4** are for **static** or **init** data unless profiling proves otherwise).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Performance engineer | I want **New Game** to read geography params from data once. | **Profiler** or log shows single load; params documented. |
| 2 | Tooling author | I want a **chunk** JSON to validate **Cell** fields vs **HeightMap**. | Sample export + schema reference (**TECH-40**). |

## 4. Current State

- **TECH-38** §7.5 defines **GeographyInitParams** DTO and harness direction.
- **TECH-16** tick harness JSON may adopt schemas owned here or in **TECH-16** spec — avoid duplicate definitions (**Decision Log** picks source of truth).
- **TECH-40** (shipped in repo): interchange JSON uses string **`artifact`** and optional **`schema_version`** per [`.cursor/projects/TECH-40.md`](TECH-40.md). Pilot **JSON Schema** for **`geography_init_params`**: `docs/schemas/geography-init-params.v1.schema.json` — align **G4** / **TECH-39** **Zod** field names with that shape when you implement loaders.

## 5. Proposed Design

### 5.1 **G4** — Geography init params

- File location: e.g. `StreamingAssets/Config/geography-default.json` (exact path in PR).
- Content: **seed**, map dimensions, flags for **rivers** / **lakes** / **forest** coverage, references to **template** ids — extend toward **FEAT-46** list without implementing the UI.

### 5.2 **G2** — Cell / chunk JSON

- Scope: **one cell** or **axis-aligned chunk** `{ x0, y0, w, h }` with array of **CellData**-compatible or **subset** fields for tooling.
- **No** requirement to mirror full **Save data** envelope.

### 5.3 **G1** — Snapshot

- Read-only; optional **Editor** menu; gitignored default output path under `tools/reports/`.

### 5.4 **P2** vs tick vs **Load pipeline**

- **P2** catalogs (buildings defs, flora): load **once** at boot or after **Load pipeline** completes a phase — **not** each tick.
- **Save**/**Load** may **serialize** **Cell** grids via existing pipeline; this issue does not imply JSON **per-frame**.

### 5.5 **P4** — `by_id`

- Apply to **static** catalogs and interchange DTOs where **O(1)** lookup matters; **not** mandatory for every **entity** type.

## 6. Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-04-03 | **Phase B** of **TECH-21** | Split implementation track |

## 7. Implementation Plan

- [ ] Define **GeographyInitParams** JSON shape + C# loader; wire **New Game** or **GeographyManager** entry (minimal).
- [ ] Add **G2** export API (Editor or batch) + document **CellData** field subset.
- [ ] Add **G1** optional snapshot (subset: **Water map** summary + **HeightMap** slice optional).
- [ ] Document **E3** layering in `ARCHITECTURE.md` or **TECH-41** §5 (link from **managers-reference** if needed).
- [ ] Update **TECH-39** **Zod** schema to match shipped **G4** fields.

## 8. Acceptance Criteria

- [ ] One **init** JSON load path merged + documented.
- [ ] One **export** path (**G1** or **G2**) merged + documented.
- [ ] **E3** paragraph merged (code comment + spec).
- [ ] **Save data** byte/layout unchanged or **Decision Log** + separate migration issue.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- 

## Open Questions

- Whether **G4** defaults ship **committed** or **gitignored** per-developer overrides.
