# Agent tooling and verification — integrated priority task list

**Purpose:** Single **sequential** backlog of work merged from three retired exploration notes: Unity↔IDE feedback, spec-driven simulation scripts, and MCP-vs-script proposals. Use this file for **implementation order**, impact rationale, and traceability to [`BACKLOG.md`](../BACKLOG.md).

**Supersedes (removed):** `docs/unity-to-ide-agent-feedback-loop.md`, `docs/spec-driven-scripts-auto-performance-exploration.md`, `docs/backlog-driven-agent-tooling-ideas.md`.

**Audience:** Developers and Cursor agents planning tooling, CI, MCP, and Unity Editor utilities.

**Related:** [`docs/mcp-ia-server.md`](mcp-ia-server.md), [`AGENTS.md`](../AGENTS.md), [`projects/agent-friendly-tasks-with-territory-ia-context.md`](../projects/agent-friendly-tasks-with-territory-ia-context.md), [`.cursor/rules/agent-router.mdc`](../.cursor/rules/agent-router.mdc).

**territory-ia:** Use **`router_for_task`**, **`spec_outline`**, **`spec_section`**, **`glossary_discover`**, **`invariants_summary`**, and **`backlog_issue`** when scoping tasks that touch simulation, roads, water, sorting, or invariants — keep script/MCP **outputs** aligned with the same vocabulary.

---

## 1. How the former documents related

| Source (retired) | Main thrust | Overlap with others |
|------------------|-------------|---------------------|
| **Unity ↔ IDE feedback** | Structured artifacts (`tools/reports/`), Editor export, glossary-aligned bug reports | Feeds **measurement** and **context JSON**; pairs with simulation harness output shape |
| **Spec-driven AUTO / performance** | Spec-labeled profiler phases, tick drift, scenario generators, ring what-if | Same `tools/reports/` contract; **depends** on simulation spec vocabulary (`simulation-system.md`) |
| **Backlog-driven MCP vs scripts** | TECH-19/18 foundation, `search_specs`, domain bundles, Node/Unity scripts | **Splits delivery:** MCP for low-token retrieval; scripts for volume/CI; master “impact list” informed ordering below |

**Design rule (carried forward):** Keep **heavy or noisy** output in **scripts** and **artifacts**; keep **MCP** responses **small and structured**. Do not duplicate the same check as both a mandatory MCP tool and a CI script unless one is a thin wrapper.

---

## 2. Ordering principles

1. **Agent safety and feedback first** — Cheap mechanical checks and Unity→workspace exports reduce bad diffs and improve prompts **before** large IA refactors.
2. **Measure, then optimize** — Profiling harnesses (**TECH-15**, **TECH-16**) precede aggressive optimization work.
3. **Correctness gates for ordered systems** — Tick-order drift detection before deep AUTO/scenario investment.
4. **Human/Unity literacy in-repo** — **`unity-development-context.md`** (shipped **TECH-20** completed) before MCP slices that assume that body exists (**`unity_context_section`**).
5. **IA platform** — **TECH-19** → **TECH-18** unlocks search, relationships, and advanced MCP tools at scale.
6. **Domain bundles last among high-impact MCP** — They multiply value **after** cross-spec search / kickoff tools exist.

---

## 3. Sequential tasks (implement in order)

Each row is a **single deliverable** or a **tightly coupled bundle**. Skip numbers only if a **Depends on** block is unfinished.

| # | Task | Type | Impact / need | Primary backlog / notes |
|---|------|------|---------------|-------------------------|
| 1 | **Mechanical repo checks:** scanner for `FindObjectOfType` in `Update` / `LateUpdate` / `FixedUpdate`; optional **`rg`** CI gate blocking new `gridArray` / `cellArray` outside `GridManager` | Script / CI | Very high — enforces [`invariants.mdc`](../.cursor/rules/invariants.mdc); stops recurring **BUG-14**-class and **TECH-04** violations during agent edits | **TECH-26** (`.cursor/projects/TECH-26.md`) |
| 2 | **Editor menu: “Export agent context”** — write `tools/reports/agent-context-{timestamp}.json` (scene, seed if available, selection, sample cell / grid facts, `schema_version`) | Unity Editor | High — closes Unity↔IDE gap; makes prompts reproducible | **TECH-28** completed — **Territory Developer → Reports → Export Agent Context** |
| 3 | **New Game / geography initialization profiler** — timed phases → JSON under `tools/reports/` (wall time, optional GC) | Unity Editor / batch | High — evidence base for **TECH-15**; avoids blind micro-optimization | **TECH-15** (`.cursor/projects/TECH-15.md`) |
| 4 | **Simulation tick harness** — N ticks on fixed seed/scene; JSON with **spec-labeled** phases (GrowthBudget, centroid/rings, AutoRoadBuilder, AutoZoningManager, AutoResourcePlanner, …) | Unity Editor / batch | Very high — grounds **TECH-16**, **BUG-52**, **FEAT-43** / **FEAT-36** work in numbers | **TECH-16** (`.cursor/projects/TECH-16.md`) |
| 5 | **Simulation tick order drift detector** — compare `SimulationManager` call order to manifest derived from `simulation-system.md` / `spec_section` (or checked-in ordered list) | Script / CI | High — cheap insurance on strict tick order | **TECH-29** (`.cursor/projects/TECH-29.md`) |
| 6 | **Hot-path static scan manifest** — generate or maintain list from `ARCHITECTURE.md` / managers-reference; extend scanner to **prioritize** AUTO / per-frame participants | Script | Medium–high — focuses **BUG-14** / performance reviews | **TECH-26** Phase 2 |
| 7 | **`unity-development-context.md`** — Unity lifecycle, Inspector / `SerializeField`, `FindObjectOfType` policy, execution order, 2D sorting vs script-driven **Sorting order** | Doc | Very high — prerequisite for IDE-native Unity literacy; helps **BUG-16**, **BUG-17**, **FEAT-19** | **TECH-20** + **TECH-25** completed |
| 8 | **Minimap `RebuildTexture` cost metric** — one-shot or throttled; JSON/ms/size | Unity Editor / script | Medium — informs **BUG-48** throttle vs rebuild | **BUG-48** (`.cursor/projects/BUG-48.md`) |
| 9 | **Validate BACKLOG issue IDs** referenced in `.cursor/projects/*.md` | Node / npm script | Medium — doc hygiene; fewer broken agent references | **TECH-30** (`.cursor/projects/TECH-30.md`) |
| 10 | **Glossary ↔ spec link checker** — paths in glossary “Spec” column exist; optional anchor check | Script | Medium — IA drift control | **TECH-27** (`.cursor/projects/TECH-27.md`) |
| 11 | **TECH-19 — PostgreSQL IA schema** — migrations, seed, minimal read surface for glossary/spec/relationships | Infra | Very high — foundation for durable search and MCP evolution | **TECH-19** (`.cursor/projects/TECH-19.md`) |
| 12 | **TECH-18 — IA migration + extended MCP** — primary retrieval path; regen markdown as needed | Infra / MCP | Very high — unlocks tools below at scale | **TECH-18** (`.cursor/projects/TECH-18.md`) |
| 13 | **MCP: `search_specs` / `ia_search`** — ranked snippets across registered specs + rules | MCP | Very high — replaces manual `spec_section` chains for multi-domain bugs | **TECH-18** Phase |
| 14 | **MCP: `what_do_i_need_to_know(task_description)`** — checklist: specs, glossary, invariants, typical files | MCP | High — structured kickoff | **TECH-18** Phase |
| 15 | **MCP: `dependency_chain(term)`** — e.g. HeightMap ↔ `Cell.height`, road preparation family | MCP | High — fewer invariant violations | **TECH-18** Phase |
| 16 | **MCP quick wins (markdown-backed):** `backlog_search`, `backlog_by_file`, `architecture_slice` | MCP | High — discovery without id; wiring discipline | **TECH-18** Phase |
| 17 | **MCP: `geo_topic_bundle`** — merged slice: shore, cliff, **Sorting order**, map border, `H_bed`, … | MCP | High — **BUG-44**, **BUG-28**, **FEAT-39**, **FEAT-41** | **TECH-18** Phase |
| 18 | **MCP: `roads_topic_bundle`** — validation, resolver, bridge / wet run | MCP | High — **BUG-31**, **BUG-49** | **TECH-18** Phase |
| 19 | **MCP: `simulation_tick_outline`** — ordered tick + AUTO / rings cross-links | MCP | High — **BUG-52**, **FEAT-43**, **FEAT-36** | **TECH-18** Phase |
| 20 | **MCP: `unity_context_section`** — slices of **unity-development-context.md** | MCP | Medium–high — **BUG-16**, **BUG-17**, **FEAT-19** | **TECH-18** Phase |
| 21 | **Scenario / fixture generator** — from project templates or YAML → Play Mode tests or serialized fixtures | Script / Unity | Medium — reproducible **BUG-52**-class regressions | **TECH-31** (`.cursor/projects/TECH-31.md`) |
| 22 | **Invariant validation report in harness** — sample checks (HeightMap vs `Cell.height`, road cache assumptions, shore samples) emitted as JSON | Unity / test | Medium — teaches agents via failures | **TECH-15** / **TECH-16** harness phases |
| 23 | **Sorting / region debug export** — optional `sorting-debug.md` for selected cells (formula inputs, order) | Unity Editor | Medium — **Sorting order** investigations (geo §7) | **TECH-28** completed — **Export Sorting Debug (Markdown)** |
| 24 | **Ring / centroid recompute what-if** — compare full recompute vs throttled; report desync risk | Script / Unity | Lower — research; schedule after row 4 | **TECH-32** (`.cursor/projects/TECH-32.md`); **FEAT-43** |
| 25 | **ProfilerMarker / allocation grouping** — names mirror spec steps; post-run parser groups AUTO vs manual | Code + script | Medium — attribution for memory spikes | **TECH-16** |
| 26 | **Prefab manifest** — missing script references under `Assets/Prefabs` | Unity batch / Editor | Medium — **ART-01**–**ART-04** | **TECH-33** |
| 27 | **Scene helper** — MonoBehaviour types/paths in agreed scene (e.g. `MainScene.unity`) | Script | Medium — **BUG-19**, **TECH-07** | **TECH-33** |
| 28 | **Generate `gridmanager-regions.json`** from `#region` + optional MCP `gridmanager_region_map` | Script + MCP | Medium — **TECH-01** extraction | **TECH-34** (+ **TECH-18** for MCP) |
| 29 | **MCP: `persistence_restore_checklist`** | MCP | Medium — **BUG-20** | **TECH-18** Phase |
| 30 | **MCP: `economy_concepts`**, **`ui_input_patterns`** | MCP | Medium — economy/UI issue clusters | **TECH-18** Phase |
| 31 | **JSON schema validation** for save DTOs or exported fixtures | Script | Medium — **TECH-21** pilot | **TECH-21** (`.cursor/projects/TECH-21.md`) |
| 32 | **MCP: `coding_conventions_slice`** | MCP | Lower–medium — prefab naming, SerializeField policy | **TECH-18** Phase |
| 33 | **Public API list per manager** + XML doc presence report | Script | Lower–medium — **TECH-02** | **TECH-02** |
| 34 | **Magic numbers extraction report** (thresholded literals) | Script | Lower — **TECH-03** planning | **TECH-03** |
| 35 | **MCP: `violations_direct_grid_access`** (optional if CI gate is sufficient) | MCP | Lower — exploratory duplicate of row 1 gate | **TECH-18** Phase (optional) |
| 36 | **MCP: `findobjectoftype_scan`** | MCP | Low — prefer row 1 script as source of truth | Defer / skip (documented in **TECH-18** spec) |
| 37 | **MCP: `find_symbol`** | MCP | Low — IDE overlap; only if repo-specific index justified | Defer / skip (documented in **TECH-18** spec) |
| 38 | **Property-based / random mutation invariant fuzzing** | Test harness | High setup cost — schedule only if geometric bugs dominate | **TECH-35** (`.cursor/projects/TECH-35.md`) |

---

## 4. Parallelization note

Rows **1–6** and **3–4** can **partially overlap** across people (e.g. one owner on **TECH-26**, another on **TECH-15**), but **row 5** should assume **row 4**’s phase names are stable. **Rows 11–20** require **TECH-19/18** sequencing as in **BACKLOG.md**.

---

## 5. Maintenance

- When a row ships, update [`BACKLOG.md`](../BACKLOG.md) (check off or add Files/Notes) and trim this table if the task is fully redundant.
- New MCP tools: register in `tools/mcp-ia-server/`, update [`docs/mcp-ia-server.md`](mcp-ia-server.md) and package README per project policy.
- After large edits to `.cursor/specs/`, regenerate any manifest consumed by row **5** or refresh MCP parse cache per [`AGENTS.md`](../AGENTS.md).

---

*Integrated 2026-04-02. Replaces three exploration documents; territory-ia tools above support scoping and vocabulary alignment.*
