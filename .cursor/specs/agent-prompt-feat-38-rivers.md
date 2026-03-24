# AI agent prompt — FEAT-38 rivers (analysis & planning only)

**Mode:** Read, analyze, synthesize. **Do not** write production code until the user asks for an implementation plan or implementation.

## Goal

Formal kickoff for **`BACKLOG.md` [FEAT-38](../../BACKLOG.md)** — **procedural rivers** on **New Game**, after terrain and the existing water pipeline. Produce a **short** prioritized problem list, **sequential** dependencies, and **explicit decisions/questions** before any concrete code changes.

## Must read

1. **[`.cursor/specs/rivers.md`](rivers.md)** — scope, vocabulary, checklist, out-of-scope (no fluid sim in this pass).
2. **`.cursor/specs/water-system-refactor.md`** — goals, Phase **D** (flow; data-driven), non-goals.
3. **`BACKLOG.md`** — FEAT-38 entry + **BUG-08** if generation overlap.
4. **`ARCHITECTURE.md`** — Geography / Terrain initialization flow.
5. **Skim:** `GeographyManager.cs` (init order), `WaterManager.cs` + `WaterMap.cs` + `WaterBody.cs`, `HeightMap` usage for lakes; `Cell` / `CellData` / save path for water.

## Concepts to define (then map to data structures)

- **Water source** — where generation starts (peak, lake outlet, seed, …).
- **Flow direction** — discrete downhill step, not a physics velocity.
- **Channel** — eligible elongated basin/path on the height field (constraints vs lakes).

## Hard constraints

- **Not** full fluid simulation: no Navier–Stokes, no per-tick volume, **no** caudal/spill/floods/drainage/tides **in this development** (may note future hooks only).
- Reuse **`WaterMap` / `WaterBody`** model; `WaterBodyType.River` exists — define behavior and persistence.
- Respect **Inspector + `FindObjectOfType`** patterns; **no new singletons**.

## Deliverables (your output)

1. **Synthesized risks** (save/load, merge with lakes/sea, sorting/shores, perf).
2. **Ordered work phases** (what must be decided first vs later).
3. **Decision list** with options (bullets, not essays).
4. **Open questions** for the user/design (mark blockers).
5. **Suggestion:** what to add or update in **`rivers.md`** §7 after this pass.

Do **not** produce large code diffs; linking to **file:symbol** references is enough.
