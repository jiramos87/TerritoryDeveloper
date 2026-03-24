# Rivers — definitions, metaknowledge, and progress (FEAT-38)

> **Backlog:** [FEAT-38](../../BACKLOG.md) · **Parent context:** [`water-system-refactor.md`](water-system-refactor.md) (Phase D — flow & coast; data-driven first)  
> **Language:** All additions to this file and to code must be **English**.

## 1. Purpose

Track **vocabulary**, **design decisions**, **open questions**, and **implementation progress** for **procedural rivers** during geography generation: water bodies that follow **downhill** structure on the `HeightMap`, integrated with the existing **`WaterMap` / `WaterBody`** model and **FEAT-37c** save/load.

This file is the **living spec** for the rivers workstream. Update it when decisions are locked or scope changes.

## 2. Scope (this development)

- **New Game / geography init only** (hook after terrain + existing lake/sea water pipeline unless design orders otherwise).
- **Data-driven flow:** gradients, paths, and body registration — **not** Navier–Stokes, pressure fields, or per-tick fluid volume.
- Rivers use the **same abstraction as lakes** where possible: per-cell body ids, `WaterBody` with `SurfaceHeight`, `WaterBodyType.River` on `Cell` / `CellData` (enum already includes `River`).
- Visuals and sorting may **reuse** lake shore paths initially; river-bank polish can be follow-up (**BUG-33** and related specs).

## 3. Out of scope (explicit — not now)

Do **not** implement in this FEAT-38 pass unless explicitly moved into scope later:

- **Discharge / caudal**, **spill**, **flooding**, **drainage networks**, **tides**, **waves**, or other **time-stepped** or **physics-heavy** fluid behavior.
- Full **hydrological** simulation (infiltration, evaporation, seasonal flow).
- Any requirement that **gameplay** or **economy** consume river flow rates (can be a future layer on top of static/graph data).

Document **future hooks** (e.g. “edge could carry nominal width”) in §7 when useful, without implementing them.

## 4. Core concepts (to refine into code)

Definitions are **working** until marked **Locked** in §7.

| Concept | Working definition |
|--------|---------------------|
| **Water source** | A grid location (or small region) where river **generation** starts: e.g. local maximum, spring tile, lake outlet, or procedural seed. Must be distinguishable in data from “any wet cell.” |
| **Flow direction** | For each step along a river, the **downhill** direction on the height field (cardinal/diagonal rules TBD). Not a velocity vector; a **discrete direction** or **next-cell** link. |
| **Channel** | The **terrain-backed** path a river occupies: cells that are part of the river body and satisfy **basin / connectivity** constraints (e.g. monotonic descent, no illegal climbs). May overlap **depression-fill basins** conceptually but is **not** the same as “lake bowl” — channels are **elongated** flow paths. |

## 5. Code and doc touchpoints (read before designing)

| Area | Role |
|------|------|
| `GeographyManager.InitializeGeography()` | Order: terrain → water → … — rivers must fit **without** breaking dependents (forests, sorting, borders). |
| `WaterManager` / `WaterMap` / `WaterBody` | Body ids, surface height, merge rules, `InitializeLakesFromDepressionFill` — rivers likely **add** a pass or helper that registers bodies/cells. |
| `HeightMap` | Sole authority for **logical** elevation for flow. |
| `TerrainManager` | Carving, shores, cliffs — river placement must stay consistent with height and existing water visuals. |
| `Cell` / `CellData` / `GameSaveManager` | **FEAT-37c** snapshot path — new river data must **round-trip** or be **regenerable** per project rules. |
| `WaterBodyType` | Already includes **`River`** — semantics must be defined (see §7). |

Related specs: [`isometric-geography-system.md`](isometric-geography-system.md), [`bugs/cliff-water-shore-sorting.md`](bugs/cliff-water-shore-sorting.md) (banks), `ARCHITECTURE.md` (Terrain / Geography).

## 6. Progress checklist

Use this section as a simple **kanban**; adjust rows as sub-tasks appear.

- [ ] Vocabulary locked (§4) + **Locked** rows in §7
- [ ] Initialization order and idempotency agreed (GeographyManager)
- [ ] River body registration + merge behavior with lakes/sea specified
- [ ] Save/load strategy chosen (extend `WaterMapData` vs derive-on-load — decision recorded)
- [ ] Minimal visuals / sorting path chosen (reuse lake tiles vs river-specific prefabs)
- [ ] Implementation complete + `BACKLOG.md` FEAT-38 updated per project workflow

## 7. Decisions and open questions

_Add rows as the team resolves them. Mark **Status:** Draft | Locked | Superseded._

| Topic | Decision / question | Status |
|-------|---------------------|--------|
| Example: outlet vs spring | … | Draft |

## 8. Future (post–FEAT-38)

Possible later phases ( **do not** implement as part of FEAT-38 unless promoted): variable width, animated flow direction, **flow rate** as a scalar, spill/flood into neighboring cells, tide coupling at sea, drainage.

---

**Last updated:** 2026-03-24 — file created for FEAT-38 kickoff; moved to `.cursor/specs/rivers.md`.
