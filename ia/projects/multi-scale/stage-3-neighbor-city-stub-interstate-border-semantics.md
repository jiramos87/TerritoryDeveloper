### Stage 3 — Parent-scale conceptual stubs / Neighbor-city stub + interstate-border semantics

**Status:** Final (2026-04-14 — all tasks archived TECH-102→TECH-109)

**Objectives:** ≥1 neighbor stub per city at interstate border. Inert read contract for future cross-scale flow.

**Exit:**

- `NeighborCityStub` struct: `id` (GUID), display name, border side enum.
- New-game init places ≥1 stub at random interstate border (seed-deterministic).
- Interstate road exit binds to stub ref (lookup by border side).
- Flow consumer reads stub via inert API (returns 0 / empty; no behavior).
- Save/load preserves stubs + bindings round-trip.
- Glossary rows land for **neighbor-city stub** + **interstate border**.
- Phase 1 — Stub schema + save wiring.
- Phase 2 — Interstate-border binding (new-game init + on-road-build at border).
- Phase 3 — City-sim inert read surface + glossary rows.
- Phase 4 — Round-trip + testmode smoke.

**Tasks:**


| Task | Name | Issue | Status | Intent |
| ------ | ------------------------- | ------------ | --------------- | -------------------------------------------------------------------------------------------- |
| T3.1 | NeighborCityStub struct | **TECH-102** | Done | `NeighborCityStub` struct (id GUID, display name, border side enum) + serialize schema. |
| T3.2 | neighborStubs save field | **TECH-103** | Done | `GameSaveData.neighborStubs` list + save version bump. |
| T3.3 | New-game stub placement | **TECH-104** | Done | New-game init: place ≥1 stub at random interstate border (seed-deterministic). |
| T3.4 | Road exit border bind | **TECH-105** | Done | On-road-build: road exit at border binds to stub ref by border side. |
| T3.5 | GetNeighborStub API | **TECH-106** | Done | `GridManager.GetNeighborStub(side)` inert read contract (returns stub or null; no behavior). |
| T3.6 | Stub + border glossary | **TECH-107** | Done | Glossary rows for `neighbor-city stub` + `interstate border`. |
| T3.7 | Save/load round-trip | **TECH-108** | Done | Save/load round-trip test (stubs + bindings preserved). |
| T3.8 | Border smoke test | **TECH-109** | Done (archived) | Testmode smoke — stub at border after new-game; binding intact after road build at border. |


**Backlog state (Step 1):** Stage 1.1 filed + archived (TECH-87 / TECH-88 / TECH-89). Stage 1.2 filed + archived (TECH-90 → TECH-97). Stage 1.3 filed + archived (TECH-102 → TECH-109) under `§ Multi-scale simulation lane`.
