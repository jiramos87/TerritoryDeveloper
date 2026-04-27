# Territory Developer — Architecture

2D isometric city-builder. Unity + C#. Index pointing to sub-specs under `ia/specs/architecture/` (per DEC-A11 doc-home-architecture-subdir).

## Sub-specs

- [`ia/specs/architecture/layers.md`](ia/specs/architecture/layers.md) — System Layers + Helper Services + Full Dependency Map.
- [`ia/specs/architecture/data-flows.md`](ia/specs/architecture/data-flows.md) — Initialization, Simulation, Player Input, Persistence, Interchange JSON intro, UI/UX, Water, Isometric geography.
- [`ia/specs/architecture/interchange.md`](ia/specs/architecture/interchange.md) — Agent IA + MCP, JSON interchange, Postgres bridge contracts (B1/B3/P5), Local verification, MCP tool catalog stub.
- [`ia/specs/architecture/decisions.md`](ia/specs/architecture/decisions.md) — DEC-A1..N table-driven architectural decisions + trade-offs.

## Source of truth

Decisions: [`ia/specs/architecture/decisions.md`](ia/specs/architecture/decisions.md). DB-indexed via `arch_decisions` + `arch_surfaces` + `arch_changelog` + `stage_arch_surfaces` (migration `db/migrations/0034_architecture_index.sql`). Per DEC-A10 source-of-truth-split: humans edit markdown, DB indexes relations.
