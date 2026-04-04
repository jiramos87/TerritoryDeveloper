# Backend database value for AI tooling and game-adjacent data

**Purpose:** Map ideas and examples from [`ia-driven-dev-territory-prompt.md`](ia-driven-dev-territory-prompt.md) to cases that **gain materially** from a **PostgreSQL**-style backend, using [`docs/postgres-interchange-patterns.md`](../docs/postgres-interchange-patterns.md) patterns (**B1** row + **JSONB**, **B3** idempotent patch **envelope**, **P5** large-document / streaming guidance). This file is **exploratory narrative**; **normative pattern text** lives in that doc (shipped **TECH-44a** — [`BACKLOG.md`](../BACKLOG.md) **§ Completed**); the **TECH-44** extension mapping (**E1**–**E3**) lives in [`docs/postgres-interchange-patterns.md`](../docs/postgres-interchange-patterns.md) **Program extension mapping (E1–E3)** (umbrella **TECH-44** completed — [`BACKLOG.md`](../BACKLOG.md) **§ Completed** **TECH-44**).

**Relationship to TECH-44 program (exact vs mapped):**

- **TECH-44a** (completed) deliverables are **documentation only**: **B1**, **B3**, **P5**, and the SQL vs **Interchange JSON** naming table — see [`docs/postgres-interchange-patterns.md`](../docs/postgres-interchange-patterns.md); **TECH-44b** ships the **IA** DB.
- This document **does not** restate those as “implementations to ship”; it **maps** workflows from [`ia-driven-dev-territory-prompt.md`](ia-driven-dev-territory-prompt.md) to **which pattern** fits (**B1** / **B3** / **P5** / **TECH-44b**).
- **New capabilities** that need **extra normative rules** (lifecycle beyond **B3**, staging tables) are tracked as **E1**–**E3** in [`docs/postgres-interchange-patterns.md`](../docs/postgres-interchange-patterns.md) **Program extension mapping (E1–E3)**: **E1** → **TECH-44c** **§ Completed**; **E2** → **[TECH-53](../BACKLOG.md)**; **E3** → **[TECH-54](../BACKLOG.md)** (**TECH-53**/**TECH-54** are backlog-only, no project specs). Promoting scope means updating [`docs/postgres-interchange-patterns.md`](../docs/postgres-interchange-patterns.md) and/or refining those backlog rows.

**Boundaries (from [`docs/postgres-interchange-patterns.md`](../docs/postgres-interchange-patterns.md) / glossary):** Player **Save data** (`GameSaveData` / **CellData** / **WaterMapData**, **Load pipeline**) stays on the Unity **persistence-system** path until a **dedicated migration issue**. Backend rows hold **interchange**-shaped or **sync** payloads (**Interchange JSON (artifact)**), analytics, IA caches, or dev telemetry — not a silent replacement for **Save data**.

**MCP context used:** `backlog_issue` (**TECH-44b**), `glossary_lookup` (**Save data**, **Postgres interchange patterns (B1, B3, P5)**), `glossary_discover` (**Interchange JSON (artifact)**).

---

## 1. What the Postgres interchange patterns doc optimizes for (short recap)

| Pattern | Role |
|--------|------|
| **B1** | Queryable scalars + **JSONB** `payload` for evolving documents (e.g. **`world_snapshot_dev`**, **FEAT-47** / **FEAT-48**-shaped experimental fields). |
| **B3** | Standard **idempotent** patch **envelope** over HTTP / queues / workers (`natural_key`, **`artifact`**, **`schema_version`**, `patch`). |
| **P5** | When exports grow huge: **NDJSON**, chunk manifests, streaming parse, paginated APIs — before OOM / GC pain. |

**Related backlog:** **TECH-44b** (first milestone: **IA** tables for MCP-adjacent reads — glossary/spec sections/invariants in **Postgres**, Markdown remains authoritative). **TECH-44c** = **E1** repro registry; **TECH-53**/**TECH-54** = **E2**/**E3** (see **TECH-44** §3). **TECH-43** owns **B2** append-only JSON lines.

---

## 2. Mapping: `ia-driven-dev-territory-prompt.md` → backend benefit

Legend: **High** = clear win with **B1**/**B3**/**P5** or **TECH-44b** IA queries; **Medium** = useful for teams at scale; **Low** = files + CI + Editor exports stay sufficient.

| Source in prompt doc | Idea / example | Backend benefit | Primary pattern hook |
|----------------------|----------------|-----------------|------------------------|
| Section 1 table — **Persistence / DB** | **JSON** program **TECH-21** → **TECH-40** / **41** / **44a** + **TECH-44** | **High** for **stored** validated payloads, cross-run queries, and API **envelopes**; schemas stay in repo (**JSON Schema** / **Zod**). | **B1** (store interchange docs), **B3** (apply patches) |
| Section 3.3 — **HTTP / WebSockets** | Candidates for **TECH-44** program | **High** — real-time or sync services assume durable identity, idempotency, and optional pagination. | **B3**, **P5** |
| Section 3.4 — *Game state as a service* | Server or headless batchmode | **High** for **command logs**, **snapshots** metadata, tenant/slot keys; gameplay still authoritative in Unity. | **B1**, **B3** |
| Section 3.6 — Remote **control loop** | Dev service, idempotent commands | **High** — audit trail, deduplication by **`natural_key`**, rate limits, “who applied what patch”. | **B3** |
| Section 5.7 — **G1** **world_snapshot** | Large or frequent snapshots | **Medium** → **High** as size/frequency grows; small exports stay `tools/reports/` + gitignore. | **B1** (metadata + **JSONB** or object storage pointer), **P5** |
| Section 5.8 — **G2** **cell_chunk** | CLI validation across many chunks | **Medium** — index chunks by map id, bounds, schema hash for regression dashboards. | **B1** |
| Section 5.12 — Remote control loop (future) | Same as section 3.6 | **High** | **B3** |
| Sections 2 / 3.1 — **JSON Schema** CI (**TECH-40**) | Validation in GitHub Actions | **Low** for DB *per se* — **High** if you **also** store validation results, timings, or fixture outcomes per commit. | **B1** (build/artifact metadata rows) |
| Section 5.11 — Cron / CI | `npm test`, schema validation | **Low**–**Medium**; DB helps **flake tracking**, historical pass rates, bisect-friendly test run ids. | **B1** |
| Section 5.10 — **Save data** regression | Hash **CellData** / **zones** after **Load pipeline** | **Low** for replacing **Save** — **Medium** for a **sidecar**: store fingerprints, scenario id, Unity version (**B1** scalars + optional **JSONB** summary). | **B1** (not player blob replacement) |
| Sections 5.4–5.5 — **Agent context** / **Sorting debug** | Editor exports | **Low** locally; **Medium** if teams **upload** exports to a shared **bug repro** store keyed by issue id. | **B1** (**TECH-44c** / **E1**) |
| Section 5.9 — **territory-ia** as read API | MCP today is file-backed | **High** overlaps **TECH-44b**: relational queries over glossary/spec edges; optional **materialized** slices for heavy dashboards. | **TECH-44b** (milestone 1), **B1** later for non-IA payloads |
| Sections 5.1, 5.2, 5.3 — Tests in Unity | Edit/Play Mode tests | **Low** for execution; **Medium** for **test result warehouse** and linking runs to **commit** + **world_snapshot_dev** id. | **B1** |
| Section 5.13 — Derived issues | **BACKLOG.md** remains source of truth | **Low** unless you add a **proposal queue** table for agent-suggested rows before human merge. | **B3** (submit patch proposal), **B1** |

---

## 3. Prompt items that should stay mostly file- or Unity-first

- **Invariants** and **guardrails** — canonical in `.cursor/rules/invariants.mdc`; **TECH-44b** may **mirror** for MCP queries, not replace.
- **Reference specs** — Markdown authoritative (**TECH-18** policy); DB is a **derived** index when **TECH-44b** ships.
- **Minimal per-change tests** — no database required to get value.
- **Export Agent Context** / **Sorting debug** as the **default** dev loop — still the lowest-friction path; DB is optional aggregation (**TECH-44c** makes **E1** explicit).

---

## 4. Extensions (**E1**–**E3** — canonical in TECH-44 charter)

The table below mirrors [`docs/postgres-interchange-patterns.md`](../docs/postgres-interchange-patterns.md) **Program extension mapping (E1–E3)**. When prioritizing backend work, update that section (and [`BACKLOG.md`](../BACKLOG.md)) first; keep this narrative aligned when possible.

| Capability | ID | Backlog / spec |
|------------|-----|----------------|
| Repro bundle registry | **E1** | **TECH-44c** |
| Schema validation history | **E2** | **TECH-53** (no project spec) |
| Agent patch proposal staging | **E3** | **TECH-54** (no project spec); **B3** + approval lifecycle |
| Balancing, **FEAT-47** / **FEAT-48** studies | *(charter narrative)* | **B1** today; promote field conventions via **TECH-44b** **Decision Log** / [`docs/postgres-interchange-patterns.md`](../docs/postgres-interchange-patterns.md) |

---

## 5. New ideas (brainstorm detail) — **IA dev** and **game-adjacent** value

Each row is a **candidate** for a future **TECH-** / **FEAT-** issue; none are commitments. Cross-reference **E1**–**E3** when they apply; other ideas need new backlog rows if scheduled.

### 5.1 IA development / agents

| Idea | Value | Suggested shape |
|------|--------|-----------------|
| **Repro bundle registry** (**E1**) | Link `agent-context-*.json`, **Sorting debug** MD, commit SHA, and **BACKLOG** issue id in one queryable row (**B1** scalars: `issue_id`, `git_sha`, `exported_at`; **JSONB**: pointers or embedded bounded snapshot). | **TECH-44c** |
| **Schema validation history** (**E2**) | Store per-CI-run outcomes for `docs/schemas/*.json` vs fixtures; trend **regressions** after **Interchange JSON** changes. | **TECH-53**; **B1** + small **JSONB** for failure details. |
| **Glossary/spec query API** | After **TECH-44b**, MCP tools (or a thin HTTP layer) run SQL for “terms related to **Water map**” instead of scanning JSON indexes only. | **TECH-44b** first; **P5** if responses grow large. |
| **Idempotent “agent patch proposal”** (**E3**) | Agents submit **B3**-shaped **`city_patch`**-style envelopes to a **staging** table; humans promote to git. | **TECH-54**; **B3** + approval lifecycle. |
| **Simulation / snapshot / CI warehouse / push** (unscheduled) | **AUTO** tick ledgers, **`world_snapshot_dev`** catalogs, Unity/CI run warehouses, WebSocket delivery — useful at scale; no **TECH-44** extension id. | New **TECH-**/**FEAT-** issue + updates to [`docs/postgres-interchange-patterns.md`](../docs/postgres-interchange-patterns.md) if normative rules are needed. |

### 5.2 Game logic / product (game-adjacent only — no new gameplay spec here)

| Idea | Value | Suggested shape |
|------|--------|-----------------|
| **Balancing / telemetry** (unscheduled) | Aggregate anonymous **economy** or **demand** samples (if product later allows) with strict privacy review. | **B1** with tenant/anonymization rules; not **Save data**; needs its own issue if pursued. |
| **Multipolar growth experiments** | **FEAT-47**-shaped parameters and measured outcomes in **JSONB** for offline analysis; aligns with [`docs/postgres-interchange-patterns.md`](../docs/postgres-interchange-patterns.md) **FEAT-47** / **FEAT-48** field names. | **B1**; promote conventions via **TECH-44b** **Decision Log** |
| **Water / surface-height studies** | **FEAT-48**-related measurement rows keyed by scenario id, not live **Water map** mutation. | **B1** |
| **A/B map templates** | **`Geography initialization`** variants (seeds, weights) and outcome metrics for **New Game** tuning. | **B1** + **B3** to assign cohorts idempotently |

---

## 6. Anti-patterns (keep out of v1 backend scope)

- Treating **Postgres** as the live **HeightMap** / **Cell** authority during normal play — violates current **GridManager** / **persistence-system** split.
- Storing full player **Save data** in **JSONB** “because TECH-44 exists” — needs an explicit migration issue and product decision.
- Duplicating **Sorting order** math in SQL — keep **isometric-geography-system** authoritative; store **outputs** or **TerrainManager**-derived samples only for debugging.

---

## 7. Suggested reading order

1. [`docs/postgres-interchange-patterns.md`](../docs/postgres-interchange-patterns.md) **Program extension mapping (E1–E3)** + [`BACKLOG.md`](../BACKLOG.md) **§ Completed** **TECH-44** — **44a**/**b**/**c** order; open **TECH-53**/**TECH-54**/**TECH-55** on [`BACKLOG.md`](../BACKLOG.md).  
2. [`docs/postgres-interchange-patterns.md`](../docs/postgres-interchange-patterns.md) — **B1** / **B3** / **P5**, naming.  
3. **TECH-44b** (completed — [`BACKLOG.md`](../BACKLOG.md) **§ Completed**) — **IA** Postgres milestone: [`docs/postgres-ia-dev-setup.md`](../docs/postgres-ia-dev-setup.md), **`db/migrations/`**.  
4. **TECH-44c** (completed — [`BACKLOG.md`](../BACKLOG.md) **§ Completed**) — **E1** **`dev_repro_bundle`** repro registry: [`docs/postgres-ia-dev-setup.md`](../docs/postgres-ia-dev-setup.md), glossary **Dev repro bundle**.  
5. [`TECH-21-json-use-cases-brainstorm.md`](TECH-21-json-use-cases-brainstorm.md) — **G1** / **G2** / **Geography initialization**.  
6. [`ia-driven-dev-territory-prompt.md`](ia-driven-dev-territory-prompt.md) — agent workflow and Editor exports.

---

*Exploration and mapping; [`docs/postgres-interchange-patterns.md`](../docs/postgres-interchange-patterns.md) holds normative patterns (**TECH-44a** completed); **TECH-44** owns the **E1**–**E3** mapping. When implementing, record contracts in **Decision Log** / **TECH-44b** scope and keep **Save data** / **Load pipeline** boundaries explicit.*
