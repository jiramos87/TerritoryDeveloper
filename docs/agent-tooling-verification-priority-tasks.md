# Agent tooling and verification — priority themes

**Purpose:** Capture **design rules** and **ordering principles** for Unity↔IDE tooling, simulation harnesses, **CI** checks, and **MCP** evolution. **Executable issue ids and row-level sequencing** live in [`BACKLOG.md`](../BACKLOG.md) and [`BACKLOG-ARCHIVE.md`](../BACKLOG-ARCHIVE.md) — this file does **not** duplicate backlog siglas (see [`.cursor/rules/terminology-consistency.mdc`](../.cursor/rules/terminology-consistency.mdc)).

**Supersedes (removed):** `docs/unity-to-ide-agent-feedback-loop.md`, `docs/spec-driven-scripts-auto-performance-exploration.md`, `docs/backlog-driven-agent-tooling-ideas.md`.

**Audience:** Developers and Cursor agents planning tooling, **CI**, **MCP**, and **Unity Editor** utilities.

**Related:** [`docs/mcp-ia-server.md`](mcp-ia-server.md), [`AGENTS.md`](../AGENTS.md), [`projects/agent-friendly-tasks-with-territory-ia-context.md`](../projects/agent-friendly-tasks-with-territory-ia-context.md), [`.cursor/rules/agent-router.mdc`](../.cursor/rules/agent-router.mdc).

**territory-ia:** Use **`router_for_task`**, **`spec_outline`**, **`spec_section`**, **`glossary_discover`**, **`invariants_summary`**, and **`backlog_issue`** when scoping tasks that touch simulation, roads, water, sorting, or invariants — keep script/MCP **outputs** aligned with the same vocabulary.

---

## 1. How the former documents related

| Source (retired) | Main thrust | Overlap with others |
|------------------|-------------|---------------------|
| **Unity ↔ IDE feedback** | Structured artifacts (`tools/reports/`), Editor export, glossary-aligned bug reports | Feeds **measurement** and **context JSON**; pairs with simulation harness output shape |
| **Spec-driven AUTO / performance** | Spec-labeled profiler phases, tick drift, scenario generators, ring what-if | Same `tools/reports/` contract; **depends** on simulation spec vocabulary (`simulation-system.md`) |
| **Backlog-driven MCP vs scripts** | **Postgres IA** foundation, `search_specs`, domain bundles, Node/Unity scripts | **Splits delivery:** MCP for low-token retrieval; scripts for volume/CI |

**Design rule (carried forward):** Keep **heavy or noisy** output in **scripts** and **artifacts**; keep **MCP** responses **small and structured**. Do not duplicate the same check as both a mandatory MCP tool and a **CI** script unless one is a thin wrapper.

---

## 2. Ordering principles

1. **Agent safety and feedback first** — Cheap mechanical checks and Unity→workspace exports reduce bad diffs and improve prompts **before** large IA refactors.
2. **Measure, then optimize** — Profiling harnesses for **geography initialization** and the **simulation tick** precede aggressive optimization work.
3. **Correctness gates for ordered systems** — Tick-order drift detection before deep AUTO/scenario investment.
4. **Human/Unity literacy in-repo** — **`unity-development-context.md`** before MCP slices that assume that body exists.
5. **IA platform** — **Postgres** dev schema and future **DB-backed** retrieval unlock search, relationships, and advanced MCP tools at scale.
6. **Domain bundles last among high-impact MCP** — They multiply value **after** cross-spec search / kickoff tools exist.

---

## 3. Task themes (track on BACKLOG)

Use [`BACKLOG.md`](../BACKLOG.md) (**Code Health**, **IA tools**, simulation, minimap, etc.) for the **numbered** deliverables, **Depends on**, and **Files** lists. Themes this doc used to enumerate in a single table include:

- **Mechanical repo checks** — `FindObjectOfType` in per-frame loops; direct **`gridArray`** / **`cellArray`** access outside **`GridManager`**.
- **Editor Reports** — **Agent context** JSON, **Sorting debug** Markdown, interchange exports — see **unity-development-context** §10.
- **Profilers and harnesses** — **Geography initialization** timing, **simulation tick** phases with spec-aligned labels, drift detection vs **`simulation-system.md`**.
- **IA hygiene** — validate **project spec** paths (`npm run validate:dead-project-specs`), optional glossary↔spec link checks, **BACKLOG** id references inside `.cursor/projects/*.md`.
- **Postgres dev** — migrations under **`db/migrations/`**, [`docs/postgres-ia-dev-setup.md`](postgres-ia-dev-setup.md), [`docs/postgres-interchange-patterns.md`](postgres-interchange-patterns.md).
- **Future MCP** — search across specs, kickoff checklists, topic bundles (geo/roads/simulation/unity), extended tools once DB-backed retrieval ships.
- **JSON interchange** — schemas under `docs/schemas/`, **`validate:fixtures`**, **Zod** mirrors in **`tools/mcp-ia-server`**.

**Parallelization:** Early mechanical checks and Editor exports can proceed in parallel with harness design; tick drift detection should assume stable phase **ids** from the simulation harness work. **Postgres IA** and extended MCP rows depend on the sequencing spelled out in **BACKLOG**.

---

## 4. Maintenance

- When a theme ships, update [`BACKLOG.md`](../BACKLOG.md) / [`BACKLOG-ARCHIVE.md`](../BACKLOG-ARCHIVE.md) and trim redundant prose here if desired.
- New **MCP** tools: register in `tools/mcp-ia-server/`, update [`docs/mcp-ia-server.md`](mcp-ia-server.md) and package README per project policy.
- After large edits to `.cursor/specs/`, regenerate any manifest consumed by drift detectors or refresh MCP parse cache per [`AGENTS.md`](../AGENTS.md).

---

*Integrated 2026-04-02; de-duplicated from backlog siglas 2026-04-04. territory-ia tools support scoping and vocabulary alignment.*
