---
purpose: "Project spec for TECH-15 — New Game / geography initialization profiler and harness outputs."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-15 — New Game / geography initialization profiler and harness outputs

> **Issue:** [TECH-15](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-02
> **Last updated:** 2026-04-04 (**TECH-39** **carryover:** **top**-**method** **ranking** **/** **optional** **`ProfilerMarker`** **on** **geography**-**hot** **paths**)

**Related tooling:** [docs/agent-tooling-verification-priority-tasks.md](../../docs/agent-tooling-verification-priority-tasks.md) — tasks **3**, **22**.

**Spec pipeline program:** **TECH-60** **§ Completed** — **glossary** **territory-ia spec-pipeline program (TECH-60)** lists this issue as a **prerequisite** for **geography initialization** **JSON** harnesses and long-term verification — [`projects/spec-pipeline-exploration.md`](../../projects/spec-pipeline-exploration.md).

## 1. Summary

Deliver a **repeatable profiler** for **New Game** / **geography initialization** that writes **JSON** under `tools/reports/` (or agreed path) with **wall-clock** per major phase and optional **GC / allocation** summaries. Optionally extend the same harness to emit **sample invariant checks** (e.g. **HeightMap** vs **cell** height on a sparse sample) so agents get machine-readable evidence before **TECH-15** optimization work.

## 2. Goals and Non-Goals

### 2.1 Goals

1. One-command or Editor-menu run produces `geography-init-profile-{timestamp}.json` with `schema_version`, commit hash (if available), scene/seed metadata, and **named phases** aligned to real code regions (e.g. terrain, **water map**, **rivers**, **forests**, **interstate**, **sorting order** passes — exact names agent-owned but glossary-aligned in output keys).
2. Document output location and **gitignore** policy (committed golden vs local-only).
3. Optional: append **validation_samples** block with read-only checks that do **not** mutate the grid (or run on a throwaway test scene).

### 2.2 Non-Goals (Out of Scope)

1. Shipping profiler code in **player** builds (Editor / development only unless explicitly gated).
2. Replacing full Unity Profiler deep captures — this is a **thin summary** for CI and agents.
3. Fixing **TECH-15** performance in this issue — this spec is **measurement** first.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | I want numbers before optimizing **geography initialization**. | JSON lists phases with ms (or ticks) and total. |
| 2 | AI agent | I want to attach a file to a prompt instead of pasting Console. | Report path under `tools/reports/` documented in **Notes**. |

## 4. Current State

### 4.1 Domain behavior

**Product:** **New Game** must remain correct; profiling must not change **HeightMap**, **water map**, or **sorting order** semantics.

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Backlog | `BACKLOG.md` — **TECH-15** |
| Code | `GeographyManager.cs`, `TerrainManager.cs`, `WaterManager.cs`, `GridManager.cs`, … |
| Spec | `ia/specs/isometric-geography-system.md` — **geography initialization** vocabulary |

### 4.3 Implementation investigation notes (optional)

- Use `UnityEngine.Profiling.Profiler` markers or `Stopwatch` per phase; keep overhead low.
- Batch mode (`-batchmode`) optional stretch goal; Editor menu is minimum.

## 5. Proposed Design

### 5.1 Target behavior (product)

No player-visible behavior change when profiler is **off**.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

- Editor window or menu: **Territory Developer → Reports → Profile geography initialization**.
- JSON schema version `1` with fields: `schema_version`, `generated_at_utc`, `phases[]: { id, ms, optional_alloc_bytes }`, `optional.validation_samples[]`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-02 | Spec created from agent-tooling roadmap | Tracks task 3 + 22 | — |
| 2026-04-04 | **Former** **TECH-39** **§7.11.1** **geography** **slice** **merged** **here** | **Profiler** **JSON** **is** **the** **right** **home** **for** **“top** **methods**” **under** **GeographyManager** **/** **TerrainManager** **/** **water** **/** **rivers** | **Avoid** **duplicating** **perf** **checklists** **in** **TECH-39** |

## 7. Implementation Plan

### Phase 1 — Minimal JSON profiler

- [ ] Add Editor entry point and write JSON to `tools/reports/`.
- [ ] Cover at least 3 coarse phases (e.g. pre-terrain, terrain+water, post-terrain visuals).
- [ ] Document in **TECH-15** backlog **Notes** or `docs/` pointer.

### Phase 2 — Finer phases + optional invariant samples

- [ ] Align phase names with hotspots named in **TECH-15** backlog.
- [ ] Optional read-only samples for **HeightMap**/**cell** height agreement on N random cells.
- [ ] (**TECH-39** **§ Completed** **relocation**) From Deep Profile / profiler JSON exports: list top C# methods attributed to geography init (`GeographyManager`, `TerrainManager`, `WaterManager`, `ProceduralRiverGenerator`, `ForestManager`, `InterstateManager` as applicable).
- [ ] (Optional) `ProfilerMarker` (or scoped `BeginSample`) on geography-hot paths only when a baseline shows regression (do not add markers without numbers).

## 8. Acceptance Criteria

- [ ] Running the tool produces valid JSON and does not corrupt the active scene.
- [ ] **Unity:** New Game still completes; manual smoke test.
- [ ] Output keys use glossary-aligned terms where they name domain concepts (**water map**, **HeightMap**, **sorting order**, etc.).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- 

## Open Questions (resolve before / during implementation)

None — tooling only; acceptance criteria in §8. Policy choices (blocking CI vs local-only) belong in **Decision Log**.
