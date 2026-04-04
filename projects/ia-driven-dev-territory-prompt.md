# AI-assisted development for Territory Developer (spec-driven + agents + territory-ia)

**Purpose:** English adaptation of the prompt in [`ia-driven-dev.md`](ia-driven-dev.md) for **this repository**: canonical vocabulary (glossary + specs), **territory-ia** MCP, `AGENTS.md`, **reference specs** vs **project specs**, and the game’s technical reality (Unity 2D isometric, **GridManager**, **Save data**, no PostgreSQL in the product core until **TECH-44b**/**c** land).

**Sources consulted (territory-ia):** `invariants_summary`, `router_for_task` (save/load, simulation, unity), `glossary_discover` / `glossary_lookup`, `spec_section` (**unity-development-context** section 10), `backlog_issue` (**TECH-21**).

---

## 1. Contrast: generic prompt vs this project

| Aspect | Original prompt (generic) | Territory Developer (this repo) |
|--------|---------------------------|-----------------------------------|
| **Behavior source of truth** | Abstract specs | [`.cursor/specs/`](.cursor/specs/glossary.md) (canonical geo: `isometric-geography-system.md`) + [`glossary.md`](.cursor/specs/glossary.md) |
| **Issues and human pipeline** | Issues + generic cron | [`BACKLOG.md`](BACKLOG.md) (`BUG-` / `FEAT-` / `TECH-` / …); skills [`.cursor/skills/project-spec-kickoff`](.cursor/skills/project-spec-kickoff/SKILL.md) and [project-spec-implement](.cursor/skills/project-spec-implement/SKILL.md) |
| **MCP** | Generic tools + skills | **territory-ia** (`backlog_issue`, `spec_section`, `glossary_*`, `router_for_task`, `invariants_summary`, …) — see [`docs/mcp-ia-server.md`](../docs/mcp-ia-server.md) |
| **Persistence / DB** | PostgreSQL in the example | **Save data** and **Load pipeline** in Unity runtime ([`persistence-system.md`](../.cursor/specs/persistence-system.md)); **JSON** program **TECH-21** → **TECH-40** / **41** / **TECH-44a** **§ Completed** ([`docs/postgres-interchange-patterns.md`](../docs/postgres-interchange-patterns.md)); **TECH-44** (**TECH-44b**/**c**) for Postgres + first dev rows |
| **Runtime → agent exposure** | HTTP API, WebSockets, etc. (idea) | **Already:** Editor menus **Territory Developer → Reports** → JSON/Markdown export under `tools/reports/` (**Agent context**, **Sorting debug**) — [`unity-development-context.md`](../.cursor/specs/unity-development-context.md) section 10 |
| **Architecture risks** | ECS, generic control loop | Strict **invariants**: `HeightMap[x,y]` == `Cell.height`; **roads** via preparation family → `PathTerraformPlan` + Phase-1 + `Apply`; no new singletons; no `gridArray` / `cellArray` outside **GridManager** — use **`GetCell(x, y)`** |

---

## 2. Adjusted prompt (copy for another agent or chat)

Use this block as a **system / user prompt** when you want an agent to work **inside** Territory Developer:

```markdown
You are assisting on **Territory Developer**: Unity 2D isometric city-builder (C#, MonoBehaviour managers).

**Authoritative context (use in this order):**
1. **territory-ia MCP** when available: `backlog_issue` for BUG-/FEAT-/TECH- ids → `invariants_summary` → `router_for_task` for the task domain → `glossary_discover` / `glossary_lookup` (queries in **English**) → `spec_section` / `spec_outline`. Do not read entire `.cursor/specs/*.md` files when a slice suffices.
2. **AGENTS.md** workflow; **.cursor/rules/invariants.mdc** — never violate invariants or guardrails.
3. **Canonical geography**: `isometric-geography-system.md` wins over other docs for grid math, **HeightMap**, **Water map**, roads, rivers, **Sorting order**.

**Vocabulary:** Use glossary-linked terms: **Cell**, **HeightMap**, **Water map**, **Save data**, **Load pipeline**, **Road validation pipeline**, **Terraform plan**, **Shore band**, **River** / **River bed (H_bed)**, **Geography initialization**, **AUTO** simulation pipeline, **Sorting order**, etc. Do not invent synonyms for documented concepts.

**Code constraints:**
- Access cells only via **GridManager.GetCell(x, y)** — no direct **gridArray** / **cellArray**.
- On **HeightMap** or **Cell.height** writes, keep both in sync.
- After road graph changes, **InvalidateRoadCache()**.
- Road placement: **road preparation family** ending in **PathTerraformPlan** + Phase-1 + **Apply** — never **ComputePathPlan** alone.
- New managers: MonoBehaviour in scene, **SerializeField** + **FindObjectOfType** fallback in Awake — no new singletons (except documented **GameNotificationManager**).

**Specs:**
- Permanent behavior: `.cursor/specs/` only.
- Active feature/bug specs: `.cursor/projects/{ISSUE_ID}.md` from template; close by migrating lessons to canonical docs.

**Runtime → agent friction reduction:**
- Prefer **Editor** exports: **Territory Developer → Reports → Export Agent Context** → `tools/reports/agent-context-*.json` (bounded **grid** sample: **Cell**, **HeightMap**, **WaterMap** fields per spec).
- For **Sorting order** issues, **Export Sorting Debug (Markdown)** in **Play Mode** with initialized **GridManager**.

**Testing:** Prefer **Unity Test Framework** where added; align tests with spec acceptance and invariants. No broad test suite is assumed today — propose minimal tests per change.

**JSON / interchange program:** Respect **TECH-21** charter and children **TECH-40**, **TECH-41**, **TECH-44a** (completed — [`docs/postgres-interchange-patterns.md`](../docs/postgres-interchange-patterns.md)); **Postgres** program **TECH-44** (**TECH-44b**, **TECH-44c**); see `projects/TECH-21-json-use-cases-brainstorm.md` for snapshot / **cell** chunk / **Geography initialization** ideas.

Deliver: concrete file paths, spec citations, and changes that respect the above.
```

---

## 3. Answers to the original document (questions 1–6), tied to this project

### 3.1 Automated testing and validation

- **At which pipeline stage should tests integrate?**  
  After **enriching** the **project spec** and **before** treating implementation as done: the issue **Acceptance** (and spec sections) should map to executable checks. In repo CI, **JSON Schema** checks (**TECH-40**) are already a validation layer *outside* Unity; inside Unity, the natural point is **post-implementation** and **regression** before moving an issue to **Completed** (human confirmation only, per `AGENTS.md`).

- **Which test types fit best?**  
  - **Unit tests (Edit Mode):** pure logic or extracted helpers (aligned with *not* bloating **GridManager** — extract testable helper classes).  
  - **Play Mode tests:** scenes with initialized **GridManager**, **Road validation pipeline**, **Water map**, **Load pipeline** (higher cost).  
  - **“Simulation testing”:** pin **simulation tick** / **AUTO** to reproducible scenarios (**TECH-16** / harness JSON in related backlog items) — fits the **JSON** program and fixtures; it does not replace specs.

- **TDD + spec-driven + agents:**  
  Write **Acceptance** in **BACKLOG.md** and criteria in `.cursor/projects/{ISSUE_ID}.md` in canonical vocabulary first; then failing tests; then implementation. Agents use **territory-ia** so they do not “invent” rules already in **geo** section 13 (roads) or **simulation-system**.

### 3.2 Unity testing with AI support

- **Practices:** Unity Test Framework, `*.Tests` assemblies, avoid **FindObjectOfType** in per-frame test loops (same invariant as in-game).  
- **Generate / run / evaluate by agents:** the agent reads `UnityTest` results from CI or local output; for **world state**, attach `tools/reports/agent-context-*.json` to the prompt after exporting from the Editor.

### 3.3 Exposing runtime state

- **Techniques already specified:** machine-readable export in **unity-development-context** section 10 (`agent-context` JSON, `sorting-debug` Markdown).  
- **Backlog-aligned extension:** **G1** / **G2** in [`TECH-21-json-use-cases-brainstorm.md`](TECH-21-json-use-cases-brainstorm.md) (**world_snapshot**, **cell_chunk**) — always **read-only** and respecting **HeightMap** / **Cell.height** consistency.  
- **HTTP / WebSockets:** candidates for **TECH-44** program (**B3**/**P5**), not an immediate requirement if Editor + files + MCP cover IDE-side AI.

### 3.4 Unity ↔ AI integration (IDE / agents)

- **Reduce friction:** **Reports** menus + `@tools/reports/...` in chat; **territory-ia** to avoid pasting whole specs.  
- **Patterns:**  
  - *Observability-first:* bounded, glossary-aligned exports (section 10).  
  - *Debugging interfaces for agents:* **Sorting debug** Markdown + **Agent context** JSON.  
  - *Game state as a service:* only if you formalize a server or headless batchmode; the current product is **Editor / Play Mode** focused.

### 3.5 Tools and ecosystem

- **Unity Test Framework** — appropriate when adding tests.  
- **territory-ia** + **TECH-40** indexes (spec/glossary) — already in the repo.  
- **Zod / JSON Schema** — CI validation for payloads (**Geography initialization**, etc.).  
- **Instrumentation:** do not duplicate the **Sorting order** formula outside **isometric-geography-system** section 7; use public **TerrainManager** APIs where the spec says so.

### 3.6 Custom tooling design (complexity and architecture)

- **Complexity:** exporting bounded reads (JSON/Markdown) is **low**; a remote **control loop** (actions on the runtime) is **high** (security, determinism, **invariants**).  
- **Architecture recommended here:** **event-driven** in the sense that “player action / simulation produces bounded effects”; **client-server** only with an explicit backend (**TECH-44b**/**c**). **ECS** is not the dominant documented model; prefer **managers** + testable helpers.

---

## 4. End goal (rephrased for Territory Developer)

- Agents **understand** state via **territory-ia**, partial specs, and **Agent context** / **Sorting debug** exports.  
- They **validate** implementations against **invariants**, **Road validation pipeline**, **Save data** / **Load pipeline**, and **JSON** schemas from the **TECH-21** program.  
- They **propose changes** to code and temporary specs (`.cursor/projects/`) without violating guardrails (**GridManager**, **roads**, **water** / **shore**).

---

## 5. Short brainstorming examples per proposal

> Each example is **illustrative** (not necessarily implemented). Wording aligns with glossary/specs.

### 5.1 Issue → spec → tests → code pipeline

- **Example:** Issue **BUG-XX** — **wet run** misclassified at a **Road validation pipeline** corner. Agent: `backlog_issue` → `spec_section` **geo** section 13 → Play Mode test that places a stroke and asserts expected prefab → fix in road helper → `InvalidateRoadCache()` on the apply path.

### 5.2 Unit test (helper extracted from **GridManager**)

- **Example:** Extract **Chebyshev distance** or **Pathfinding cost model** math to a static class; Edit Mode test with a case table from **geo** section 10 spec commentary.

### 5.3 Play Mode: **HeightMap** / **Cell.height** invariant

- **Example:** After a test terraform operation, iterate a sample of cells with **GetCell** and assert `height == HeightMap[x,y]`.

### 5.4 Using **Export Agent Context**

- **Example:** After reproducing a **Water map** / **Shore band** bug, the developer exports JSON, references it in Cursor as `@tools/reports/agent-context-….json`, and asks the agent to compare with **geo** section 11 / **water-terrain-system**.

### 5.5 Using **Export Sorting Debug**

- **Example:** Visual bug on **Cliff** / 2D layers: export Markdown in Play Mode; the agent cross-checks **Sorting order** in **isometric-geography-system** section 7 without hand-deriving the formula.

### 5.6 **TDD + spec** for **Geography initialization**

- **Example:** New fixture `geography-init-params.good.json` under `docs/schemas/fixtures/` breaks CI if the schema changes; then **parse-once** code (**TECH-41**) consuming the DTO.

### 5.7 Snapshot **G1** (**TECH-21** brainstorm)

- **Example:** Dev menu “Export **world_snapshot** (32×32 bounds)” writes JSON with partial **cells** and water-body summary; external agent validates with **Zod** and flags inconsistent **Junction** data.

### 5.8 **cell_chunk** **G2**

- **Example:** Review script requests a chunk centered at (x, y) via CLI and validates **waterBodyId** against **Open water** / **Rim** rules from spec.

### 5.9 **territory-ia** as a read API for the agent

- **Example:** Before touching **rivers**, `router_for_task` “water” → `spec_section` **water** or **geo** section 12 → `glossary_lookup` “River bed (H_bed)” to avoid violating **H_bed** monotonicity toward the exit.

### 5.10 **Save data** regression (no PostgreSQL)

- **Example:** Test or manual checklist: save game, **Load pipeline**, compare hash of a **CellData** subset or **zones** count (per **persistence-system**).

### 5.11 Cron / CI (stand-in for the original prompt’s “cron”)

- **Example:** GitHub Actions workflow running `npm test` in `tools/mcp-ia-server` + **JSON Schema** validation for `docs/schemas/`; optional Unity tests when the project adds them.

### 5.12 Remote “control loop” (future, **TECH-44** program)

- **Example:** Dev service accepting only idempotent commands (“simulate **simulation tick** N”, “export snapshot”) — no direct **HeightMap** writes without going through **Terraform plan** / managers.

### 5.13 Deriving new issues (step 7 of the original pipeline)

- **Example:** When closing a spec, the agent proposes child **TECH-** or **BUG-** rows in **BACKLOG.md** when the **Decision Log** leaves debt (e.g. missing **InvalidateRoadCache** on a path).

### 5.14 Skill **project-spec-kickoff** + MCP

- **Example:** For **FEAT-YY**, the human runs kickoff; the agent runs `glossary_discover` with English keywords (“growth ring”, “AUTO”) and rewrites **Open Questions** using only canonical terms.

### 5.15 Skill **project-spec-implement**

- **Example:** Phase checklist: after each phase, export **Agent context** and add one minimal test covering the new **Acceptance** line.

---

## 6. Quick checklist for prompt authors

- [ ] Does the issue id exist in **BACKLOG.md** and did you call `backlog_issue`?  
- [ ] Did you read **invariants** before changing **roads** / **water** / **HeightMap**?  
- [ ] Are product terms in canonical English in **Open Questions** / **Acceptance**?  
- [ ] Is there `@tools/reports/agent-context-….json` or **Sorting debug** for world or layering bugs?  
- [ ] Are **JSON** changes aligned with **TECH-21** and schemas under `docs/schemas/`?

---

*This document aligns the generic `ia-driven-dev.md` prompt with Territory Developer. For JSON payload detail, follow [`TECH-21-json-use-cases-brainstorm.md`](TECH-21-json-use-cases-brainstorm.md) and the **TECH-21** / **TECH-44** charters.*
