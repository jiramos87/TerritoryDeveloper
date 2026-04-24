---
purpose: "Living design doc for the master-plan foldering + lifecycle-skill simplification refactor. Captures locked decisions, open questions, pending flags during the iterative polling phase. Seeds a future `/master-plan-new` once decisions stabilize."
audience: both
loaded_by: ondemand
slices_via: none
---

# Master-plan foldering + lifecycle simplification — design doc

> **Status:** In-flight polling. Decisions locked below are binding for the eventual master plan.
> **Created:** 2026-04-24
> **Scope:** Structural refactor of master-plan layout + lifecycle skill surface. Stage-file + stage-authoring + ship-stage + closeout chain simplification. New MCP tools for state sync + bundles + migration.
> **Companion doc:** [`docs/master-plan-execution-friction-log.md`](master-plan-execution-friction-log.md) — captures raw friction; this doc captures the refactor response.
> **Out of scope (by operator decision):** measurement harness, weekly triage cadence, `mechanicalization_score` concept.

---

## 1. Driving goals (operator, 2026-04-24)

1. **Simplification** — fewer skill seams, fewer pair-tail dispatches, fewer persisted planning artifacts.
2. **Token economy** — subagents read only the stage they need; MCP bundles replace N granular tool calls.
3. **Execution speed** — faster `/stage-file` + `/ship-stage` wall-clock; accept reduced mechanical audits for speed.
4. **Correctness via stronger MCP, less agent reliance** — atomic mutation tools, state-sync queries, migration tooling.
5. **Preserve authoring + testable implementation benefits** — consolidation, not elimination, of authoring work.

**Explicitly dismissed:**

- `mechanicalization_score` concept — drop from all skills, templates, validators, §Stage File Plan, §Plan Digest.
- Measurement harness + weekly friction triage — log exists as capture surface only.
- §Stage File Plan tuple list — persisted planning artifact retired.

---

## 2. Locked decisions (Round 1)

### 2.1 Foldering shape

| Id | Decision |
|---|---|
| A1 | Folder name: `ia/projects/{slug}/` (drop `master-plan` suffix) |
| A2 | Index filename: `index.md` (at folder root) |
| A3 | Stage filename: `stage-1.1-{short-name}.md` (human-readable suffix) |
| A4 | Per-task spec: colocated at `ia/projects/{slug}/tasks/TECH-XXX.md` |
| A5 | Stage file contents: Stage Objectives + Exit (terse, top of file) + task table. **Phase bullets move into per-task specs.** Real "meat" lives in task specs (upgraded by C6). |
| A6 | Closed-stage archive: `ia/projects/{slug}/_closed/stage-X.Y-{name}.md` (subfolder inside live plan) |

**Coupling flagged (pending C6):** A5 removes Stage Objectives/Exit from stage file. Where they live (index.md vs terser stage-file block vs first task) is Round 2 C6.

### 2.2 Skill merges + drops

| Id | Decision |
|---|---|
| B1 | `stage-file-plan` + `stage-file-apply` → merge into single `stage-file` skill. No pair-tail. Operator note: stage-file skill lays down stage objectives/exit + structure (from MASTER-PLAN-STRUCTURE.md / template / project-hierarchy.md) + per-task objectives/exit |
| B2 | `plan-author` + `plan-digest` → merge into single `stage-authoring` skill (one Opus bulk pass) |
| B3 | `plan-review` drift scan → **drop entirely** (trust authoring + validators) |
| B4 | `opus-auditor` §Audit per task → **drop** (code-review covers it) |
| B5 | `plan-reviewer-mechanical` + `plan-reviewer-semantic` → **drop both** |
| B6 | §Plan Author section → replaced by `stage-authoring` output (not a survivor) |
| B7 | §Audit section in specs → **drop** (tied to B4) |
| B8 | §Stage File Plan tuple list → **drop entirely**. Stage-file skill relies on a new MCP tool for status/ids/meta. **Flagged pain point:** stage↔task status sync problem — must be addressed by new MCP tool (Round 2 C1–C3) |

---

## 3. Locked decisions (Round 2)

### 3.1 State sync + stage status

| Id | Decision |
|---|---|
| C1 | Stage rollup source of truth: **both** (derived authoritative, index cached). **Additional dimension:** stage status is not only task completion — also **stage verification** (verify-loop result). Both dimensions persisted + queryable. |
| C2 | MCP tool `stage_state({slug}, stage_id)` shape accepted: `{tasks, progress, blocker, next_pending, objectives, exit, commit_hashes_seen}` + extended for verification dimension (see Round 3 D1). |
| C3 | Atomic mutation tool `task_status_flip(task_id, new_status)` — **one tool, one lock**. Flips yaml + index.md rollup in single `flock`-guarded op. |

### 3.2 MCP bundles

| Id | Decision |
|---|---|
| C4 | `stage_bundle({slug}, stage_id)` — **yes**. Consumers: stage-file, stage-authoring, ship-stage. One call replaces N granular fetches. |
| C5 | `task_bundle(task_id)` — **yes**. Consumers: spec-implementer, code-reviewer. |

### 3.3 Stage objectives/exit location (A5 upgrade)

| Id | Decision |
|---|---|
| C6 | **Three-tier content split:** `index.md` carries only global master-plan objectives + stage index + global change log. `stage-1.1-{name}.md` carries stage objectives + exit at top, task table below. Each task spec carries its own objectives + exit. All small. Real "meat" lives in **task specs**. |

### 3.4 Stage-authoring output

| Id | Decision |
|---|---|
| C7 | One Opus pass writes **§Plan Digest only** per task. No §Plan Author phase — stub → digest direct. No aggregate `docs/implementation/{slug}-stage-X.Y.md` (aggregate doc retires — implied drop of that doc pattern). |
| C8 | `stage-file` tail **calls `stage-authoring` inline** — one command = filed + authored. |

### 3.5 Ship-stage shape post-drops

| Id | Decision |
|---|---|
| C9 | **Pass A (per-task):** implement + compile. **NO per-task commit.** Review modified files without committing. **Pass B (per-stage):** verify-loop + code-review + closeout + stage-end commit(s). |
| C10 | Stage closeout **folded into ship-stage Pass B tail** — no separate `stage-closeout` skill. `stage-closeout-planner` + `plan-applier` stage-closeout mode both retire. |

### 3.6 Migration tooling

| Id | Decision |
|---|---|
| C11 | **Node script** `tools/scripts/fold-master-plan.ts` — one-shot CLI per plan. |
| C12 | **No back-compat window.** Operator holds off on other plans during refactor (not strict freeze). |
| C13 | **One bulk commit** — all plans migrated in a single commit. Design doc tracks migration progress to survive context compaction. |

### 3.7 Validator surface (reduced-audit regime)

| Id | Decision |
|---|---|
| C14 | **Hard gates kept:** `validate:all`, `validate:backlog-yaml`, `validate:master-plan-status`, `invariant_preflight`, `unity:compile-check`. **Dropped:** `validate:dead-project-specs`, `validate:frontmatter`, `validate:claude-imports`. |
| C15 | **Warnings-only list: none.** Everything either hard gate or dropped. |

---

## 4. PIVOT — database-backed state (2026-04-24)

> **Operator directive (verbatim):** "instinct is telling me that we are ready to move the state of the implementation progress into the database for quick reads and writes. Git is not enough and database is faster, and we are approaching 1000 issues worked on. Use the crystallized decision and logic and find design for database aided project planning and execution via mcp tools and connections with skills. Web dashboard should read database via CRUD controller in backend."

### 4.1 What this changes

- **DB is primary state surface** for task/stage/verification/commit/journal metadata. Filesystem keeps narrative content only.
- **Round 3 D1–D13 superseded** — all state-persistence questions re-open under DB model (§4.4 Round 4 below).
- **Round 1 + Round 2 narrative/skill/foldering decisions stand** — foldering of `ia/projects/{slug}/` still happens; scope narrows to narrative content only. Task table + rollup status become DB queries rendered at read time (not hand-edited cells).

### 4.2 Provisional split — DB vs filesystem

**DB (source of truth — metadata + spec bodies + journal):**

- `ia_master_plans` — slug, title, global goal, created_at, closed_at.
- `ia_stages` — (slug, stage_id) composite PK, name, objectives, exit, closed_at.
- `ia_tasks` — task_id (TECH-XXX) PK, slug, stage_id, title, type, priority, status enum, depends_on[], related[], created_at, updated_at, **`body` text (full spec markdown)**, **`body_tsv` tsvector (full-text index)**. No `spec_path` — DB is sole body store.
- `ia_task_spec_history` (optional audit) — task_id, body_before, body_after, section, written_at, actor.
- `ia_task_commits` — task_id, commit_sha, created_at.
- `ia_stage_verifications` — (slug, stage_id), verdict enum, commit_sha, ran_at, notes. Single latest row (E11).
- `ia_ship_stage_journal` — session_id, task_id, phase, payload jsonb, created_at. Replaces `runtime-state.json` + in-process `{CHAIN_JOURNAL}` + TECH-493 disk journal (E5=a).
- `ia_fix_plan_tuples` — task_id, round, tuples jsonb, applied_at. No markdown sync (E12).

**Filesystem (narrative nav only, version-controlled, human-readable):**

- `ia/projects/{slug}/index.md` — global master-plan objectives + global change log. No task table (DB query renders on demand).
- `ia/projects/{slug}/stage-1.1-{name}.md` — Stage Objectives + Exit + narrative. No task table (DB query).
- `ia/projects/{slug}/_closed/stage-*.md` — archived stage files post-closeout.
- **`tasks/` subfolder does NOT exist** — task spec bodies live in DB only (E4=b). A4's "colocated task" intent preserved via `index.md` + `stage-*.md` grouping, not filesystem tree.
- `ia/rules/*.md`, `ia/skills/*/SKILL.md`, `ia/templates/*.md`, `ia/specs/glossary.md` — authoring surface, unchanged.
- `docs/` — exploration + mechanical plan + design docs, stays flat (E18). Foldering of `docs/` deferred as follow-up.

**Retires entirely (replaced by DB):**

- `ia/backlog/{id}.yaml` + `ia/backlog-archive/{id}.yaml` — metadata moves to `ia_tasks` (E2).
- `BACKLOG.md` + `BACKLOG-ARCHIVE.md` generated views — dashboard + MCP query replace grep-over-markdown (E2).
- `ia/state/id-counter.json` + `reserve-id.sh` — replaced by DB sequence per prefix (E3).
- `ia/state/runtime-state.json` — replaced by `ia_ship_stage_journal` (E5=a).
- `ia/projects/{slug}/tasks/TECH-XXX.md` — entire filesystem task-spec surface retires (E4=b).
- `materialize-backlog.sh` + `validate:backlog-yaml` + `validate:dead-project-specs` — redundant under DB (E2 cascade).

### 4.3 MCP tool surface under DB regime

**Read:**

- `stage_state(slug, stage_id)` — DB query; no file read.
- `task_state(task_id)` — DB query; returns metadata + status + commits + deps.
- `stage_bundle(slug, stage_id)` — DB state + filesystem stage narrative + task body slices in one payload.
- `task_bundle(task_id)` — DB state + task spec body slices.
- `master_plan_state(slug)` — DB rollup: stages + progress + blockers.
- `task_spec_body(task_id)` — full body markdown from DB.
- `task_spec_section(task_id, section)` — single section slice (§Goal, §Intent, §Plan Digest, etc.).
- `task_spec_search(query, filters)` — full-text + trigram search across all task bodies (replaces grep-over-markdown).
- `backlog_list` / `backlog_search` / `backlog_issue` — re-implement as DB queries (schemas survive, storage flips).

**Mutate (atomic, transactional):**

- `task_insert({slug, stage_id, title, body, ...})` → returns reserved task_id via DB sequence + writes initial body.
- `task_status_flip(task_id, new_status)` — single transaction.
- `task_spec_section_write(task_id, section, content)` — sectioned atomic body update; optional audit row in `ia_task_spec_history`.
- `stage_verification_flip(slug, stage_id, verdict, commit_sha)`.
- `stage_closeout_apply(slug, stage_id)` — DB flip + filesystem `mv` stage file to `_closed/` in one op.
- `task_commit_record(task_id, commit_sha)`.
- `journal_append(session_id, task_id, phase, payload)` / `journal_get` / `journal_search`.
- `fix_plan_write(task_id, tuples)` / `fix_plan_consume(task_id, round)`.

### 4.4 Round 4 — locked decisions

| Id | Decision |
|---|---|
| E1 | **Postgres** — reuse existing Unity-bridge instance (also used for web asset manager / dev dashboards) |
| E1b | **Share `db:migrate`** — one migration tree, `ia_*` prefix on tables |
| E2 | `ia/backlog/*.yaml` **retires entirely**. `BACKLOG.md` + `materialize-backlog.sh` + `validate:backlog-yaml` + `validate:dead-project-specs` drop. DB is sole source |
| E3 | **DB sequence** per prefix (`tech_id_seq`, ...). `reserve-id.sh` + `id-counter.json` retire. Skills call `task_insert` MCP |
| E4 | **(b)** DB carries task spec bodies — `ia_tasks.body` text column + tsvector full-text index. Filesystem keeps only `index.md` + `stage-*.md` + `_closed/`. `tasks/` subfolder drops. Git history via E10 daily snapshot + optional `ia_task_spec_history` audit table. MCP tools: `task_spec_body`, `task_spec_section`, `task_spec_section_write`, `task_spec_search` |
| E5 | **(a)** `ia_ship_stage_journal` fully replaces `runtime-state.json` + in-process `{CHAIN_JOURNAL}` + TECH-493 disk journal proposal |
| E6 | **Read-only** dashboard for now. Future actionable-dashboard scope persisted as separate exploration doc (see §4.4.2) |
| E7 | **Next.js API routes** in `web/app/api/...` — colocated |
| E8 | **DB transactions replace all flock** — `.id-counter.lock`, `.closeout.lock`, `.materialize-backlog.lock`, `.runtime-state.lock` retire |
| E9 | **DB + foldering in one migration** — single commit (per C13) |
| E10 | **Committed DB snapshot, daily refresh** — `ia/state/db-snapshot.sql` (or compressed) committed by cron/CI; protects against data loss |
| E11 | `ia_stage_verifications` — **single latest row** per stage (overwrite on re-run) |
| E12 | `ia_fix_plan_tuples` — **DB-only**; no markdown sync, no ephemeral file |
| E13 | Stage-end commit granularity: **single commit for whole stage** (`feat({slug}-stage-X.Y): ...`); task traceability via DB + spec §Code Review notes |
| E14 | `plan-applier` code-fix mode **dropped** — code-reviewer applies fix inline; TECH-506 pair retires fully |
| E15 | Per-task spec surviving sections (unclear ones): **keep** §Implementation Plan, §Acceptance Criteria, §Verification |
| E16 | `stage-decompose` skill — **drop** confirmed. Flow preserved via `design-explore` → `master-plan-new` (authors stages inline) → `master-plan-extend` |
| E17 | `release-rollout` — **stays**, out of scope for this refactor |
| E18 | `docs/` directory — **stays flat for now**. Foldering of `docs/` deferred as explicit follow-up; do NOT draft under this refactor |

### 4.4.1 Round 4 — all items locked

All four open items resolved:

- **E4=(b)** — DB body + full-text index. Tasks/ subfolder drops. MCP tools for body slice + search (see §4.4 table).
- **E5=(a)** — `ia_ship_stage_journal` fully replaces `runtime-state.json` + `{CHAIN_JOURNAL}` + TECH-493 disk journal.
- **E16** — `stage-decompose` skill drop confirmed. Full flow preserved via `design-explore` → `master-plan-new` → `master-plan-extend`.
- **E18** — `docs/` stays flat for now. Foldering of `docs/` deferred as explicit follow-up; NOT drafted under this refactor.

### 4.4.2 E6 addendum — actionable dashboard exploration doc stub

Per your ask, a separate exploration doc seeds the core idea for future scope. Created at [`docs/actionable-agent-dashboard-exploration.md`](actionable-agent-dashboard-exploration.md).

### 4.5 Round 5 — DB schema specifics (locked)

| Id | Decision |
|---|---|
| F1 | **(a)** Postgres ENUM types for `task_status` + `stage_verdict` — type-safe, index-efficient; accept ALTER TYPE friction on new values |
| F2 | **(a)** `ia_tasks.task_id text PRIMARY KEY` — direct `TECH-123` string; no surrogate int |
| F3 | **(b)** Separate join table `ia_task_deps(task_id, depends_on_id, kind)` — reverse lookups ("what blocks X?") trivial at scale |
| F4 | **(c)** Both indexes on `ia_tasks.body`: `GIN(body_tsv)` for word match (`tsvector GENERATED ALWAYS AS STORED`) + `GIN(body gin_trgm_ops)` for substring/identifier fuzzy search |
| F5 | **(a)** `ia_task_spec_history` full snapshots — every `task_spec_section_write` writes full body + section + actor + ts; reconstruction trivial |
| F6 | **(d)** Split daily snapshot — metadata tables plain SQL (diffable), body + journal dump binary (`-Fc`) |
| F7 | **(c)** `ia_ship_stage_journal.payload_kind text` + `payload jsonb` discriminated union; per-kind schemas documented in `ia/rules/ship-stage-journal-schema.md` |
| F8 | **(c)** `ia_fix_plan_tuples` soft-deleted via `applied_at` timestamp + TTL cleanup (30 days) |
| F9 | **(a)** Stage closeout flips `ia_tasks.status → archived`; rows stay in same table |
| F10 | **(c)** Mixed concurrency — advisory locks (`pg_advisory_lock`) for id sequence + stage closeout; row-level `SELECT ... FOR UPDATE` for task status |
| F11 | **(a)** Import order: schema → master_plans → stages → tasks metadata → bodies → tsvector generate → GIN indexes last (bulk-load pattern) |
| F12 | **(c)** Idempotent re-run — import script detects existing rows, skips; wrap each run in transaction |
| F13 | **(a)** No partitioning — 1000 rows is trivial at Postgres scale |
| F14 | **(b)** Next.js API routes call MCP tools (shared mutation logic) — not direct Postgres client |
| F15 | **(a)** Singleton `pg.Pool` at `territory-ia` MCP server boot; all tools share |

**Derived artifacts (pending Stage 0 master plan):**

- `ia/migrations/{timestamp}-ia-schema.sql` — CREATE TYPE + CREATE TABLE + CREATE INDEX statements.
- `tools/scripts/ia-db-migrate.ts` — one-shot import script (idempotent, transactional per run, bulk-load order per F11).
- `ia/rules/ship-stage-journal-schema.md` — per-phase payload schemas (F7).

### 4.4.3 Round 4 — deferred sub-polls (pre-pivot Round 3 content, now locked)

#### 4.4.1 Platform + reuse

- **E1.** DB engine: (a) Postgres — reuse existing Unity-bridge Postgres instance (already running per `db:bridge-preflight` + `db:migrate`); (b) SQLite (simpler, embedded, git-trackable schema); (c) other.
- **E1b.** Schema migration tooling: (a) same `db:migrate` chain used for Unity bridge; (b) separate migration tree (`ia-migrate`) isolated from game-state schema; (c) shared schema, namespaced tables (`ia_*` prefix).

#### 4.4.2 Yaml retirement

- **E2.** `ia/backlog/{id}.yaml` fate under DB model:
  - (a) **Retire entirely** — DB is sole source; `BACKLOG.md` view generated from DB; `materialize-backlog.sh` + `validate:backlog-yaml` + `validate:dead-project-specs` drop.
  - (b) **Yaml as mirror** — DB primary; yaml regenerated on demand for git history/portability.
  - (c) **Yaml canonical, DB mirror** — yaml stays authoritative (current), DB is fast-read cache rebuilt from yaml.

#### 4.4.3 Id reservation

- **E3.** Monotonic id source under DB:
  - (a) DB sequence per prefix (`tech_id_seq`, `feat_id_seq`, ...); `reserve-id.sh` retires; skills call `task_insert` MCP tool.
  - (b) Keep `id-counter.json` + `reserve-id.sh` (filesystem flock); DB takes id passed in.
  - (c) DB advisory lock replaces filesystem flock; monotonic counter still in DB.

#### 4.4.4 Sync contract

- **E4.** Relationship between DB task row + filesystem spec at `ia/projects/{slug}/tasks/TECH-XXX.md`:
  - (a) DB carries `spec_path`; file exists ↔ task row has status ≠ archived; validator enforces bidirectional link
  - (b) DB carries spec body too (jsonb or text column); filesystem file generated on demand (no file writes by skills)
  - (c) DB carries metadata + narrative structural fields; filesystem holds raw markdown; spec_path derived not stored

#### 4.4.5 Journal / runtime state

- **E5.** `ship_stage_journal` scope:
  - (a) Full replacement of `runtime-state.json` + in-process `{CHAIN_JOURNAL}` + disk journal TECH-493 proposal
  - (b) Journal DB-only; runtime-state stays filesystem (different concerns)
  - (c) Both DB; runtime-state deprecated separately after measurement

#### 4.4.6 Web dashboard surface

- **E6.** Mutation policy for web dashboard (`web/`):
  - (a) Read-only — dashboard shows state; all mutations go through skills/MCP
  - (b) Partial mutate — dashboard can flip task status, record notes, trigger `/ship` via API; full lifecycle ops (stage-file, ship-stage) still via CLI
  - (c) Full CRUD — dashboard owns all mutations; CLI is one of several clients

#### 4.4.7 Backend controller shape

- **E7.** CRUD controller:
  - (a) Next.js App Router API routes (`web/app/api/projects/[slug]/route.ts` etc.) — colocated with web workspace
  - (b) Separate Node service `tools/ia-backend/` serving REST; web consumes
  - (c) MCP tools proxied through web API (thin wrapper — single source of logic)

#### 4.4.8 Concurrency

- **E8.** Replace filesystem `flock` (`.id-counter.lock`, `.closeout.lock`, `.materialize-backlog.lock`, `.runtime-state.lock`):
  - (a) DB transactions + row-level locks — all `flock` retires
  - (b) DB advisory locks for cross-tool serialization
  - (c) Hybrid — DB mutations transactional; filesystem `mv` ops (e.g. stage archive) keep `flock` for disk atomicity

#### 4.4.9 Migration sequencing (DB + foldering together)

- **E9.** Order:
  - (a) **DB schema + migration first** — import all existing yamls + master plans into DB in one migration; folder structure lays down in same migration script; skills flip in next step
  - (b) **Foldering first, yaml retained** — run `fold-master-plan.ts` to rearrange filesystem; then second migration moves state to DB
  - (c) **Interleaved** — DB schema first, skills dual-read during window, filesystem foldering last

#### 4.4.10 Offline / portability

- **E10.** Git-cloned repo without DB running:
  - (a) Read-only degraded mode — narrative specs readable, no state queries (acceptable — this is a dev-tool repo, not a product)
  - (b) Periodic DB dump committed to `ia/state/db-snapshot.sql` for offline inspection
  - (c) DB dump committed + skills fall back to snapshot read when DB unreachable

#### 4.4.11 Stage verification row

- **E11.** `stage_verifications` shape:
  - (a) Single row per stage — latest verdict only (overwrite on re-run)
  - (b) Append-only history — every verify-loop run recorded; `latest()` view picks most recent
  - (c) Latest row + separate `stage_verification_history` audit table

#### 4.4.12 §Code Fix Plan tuples

- **E12.** `fix_plan_tuples` table:
  - (a) DB-only during window; cleared after plan-applier green (no markdown)
  - (b) DB + synced §Code Fix Plan markdown section in task spec for visibility
  - (c) DB audit log + ephemeral `ia/plans/{task_id}-fix-{ts}.md` for human review

#### 4.4.13 Deferred (superseded) — Round 3 questions

Round 3 D1–D13 folded into Round 4:

- D1 / D2 / D11 → superseded by DB model + E11.
- D3 → superseded by §4.3 mutation tool list.
- D4 → still open: stage-end commit granularity (one commit per task vs one per stage vs meta commit). **Carry forward as E13.**
- D5 → superseded by E3.
- D6 → still open: `plan-applier` code-fix mode survival under DB fix_plan_tuples table. **Carry forward as E14.**
- D7 → still open: surviving per-task spec sections. **Carry forward as E15.**
- D8 → confirmed drop (aggregate `docs/implementation/` pattern retires) — lock.
- D9 → `stage-decompose` drop — pending confirm as **E16**.
- D10 → superseded by E12.
- D12 → `release-rollout` stays — pending confirm as **E17**.
- D13 → exploration + mechanical plan docs stay flat — pending confirm as **E18**.

- **E13.** Stage-end commit granularity (Pass B): (a) one commit per task + one meta closeout commit; (b) single commit for whole stage; (c) per-task + closeout squashed into last.
- **E14.** `plan-applier` code-fix mode survival: (a) keep (reads fix_plan_tuples DB row); (b) drop — inline fix in code-reviewer; (c) keep but rename.
- **E15.** Surviving per-task spec sections — confirm keep/drop for each: §Implementation Plan, §Acceptance Criteria, §Verification (other sections already decided in D7).
- **E16.** `stage-decompose` skill drop — confirm y/n.
- **E17.** `release-rollout` stays as-is — confirm y/n.
- **E18.** `docs/` exploration + mechanical plans stay flat (not foldered) — confirm y/n.

---

## 5. Deferred (pre-pivot Round 3 polling) — archived reference

*These questions were open when pivot landed. D1–D3, D5, D10, D11 superseded by DB model. D4, D6, D7, D8, D9, D12, D13 carried into Round 4 as E13–E18 (or locked).*

<details>
<summary>Pre-pivot Round 3 question text (preserved for audit)</summary>

### D1 Stage verification persistence
(a) index.md Stage row column; (b) stage file frontmatter; (c) both; (d) derived from git + journal.

### D2 Stage status enum
(a) monotonic `pending|implemented|verified|closed`; (b) multi-field; (c) other.

### D3 Mutation tool surface
(a) three atomic tools; (b) composite `stage_mutate`; (c) other.

### D4 Stage-end commit granularity
(a) per-task + meta; (b) single stage commit; (c) per-task no meta.

### D5 reserve-id
(a) shell; (b) MCP primary; (c) parallel.

### D6 plan-applier code-fix mode
(a) keep; (b) drop; (c) rename.

### D7 Surviving per-task spec sections
Unclear: §Implementation Plan, §Acceptance Criteria, §Verification.

### D8 docs/implementation/ aggregate drop
Confirm y/n.

### D9 stage-decompose skill drop
Confirm y/n.

### D10 §Code Fix Plan tuple location
(a) task spec; (b) ephemeral file; (c) never persisted.

### D11 validate:master-plan-status reimpl
(a) filesystem walk; (b) MCP delegate; (c) both.

### D12 release-rollout stays
Confirm y/n.

### D13 docs/ stays flat
Confirm y/n.

</details>

### 4.1 Stage verification dimension (C1 extension)

- **D1.** Where is stage verification result persisted?
  - (a) `index.md` Stage row — new column `Verify` with enum `pending | green | red`
  - (b) Stage file frontmatter — `verify: pending|green|red` + `verify_commit: {sha}`
  - (c) Both (index caches, stage file authoritative)
  - (d) Derived from git + `{CHAIN_JOURNAL}` (no persisted field)
- **D2.** Stage status enum (composite of task completion × verification × closeout):
  - (a) `pending | implemented | verified | closed` (4 states, monotonic)
  - (b) Multi-field `{tasks: "N/M", verify: pending|green|red, closed: bool}` (no composite enum)
  - (c) Other

### 4.2 Mutation tool surface

- **D3.** Separate atomic tools or one composite?
  - (a) Three tools: `task_status_flip`, `stage_verification_flip(slug, stage_id, verdict, commit_sha)`, `stage_closeout_apply(slug, stage_id)` (last one moves stage file to `_closed/`, updates index)
  - (b) One composite `stage_mutate(op, ...)` with op enum
  - (c) Other

### 4.3 Stage-end commit granularity (C9 follow-through)

- **D4.** When Pass B closes out, commit granularity:
  - (a) One commit per task (`feat(TECH-XXX): ...`) replayed at stage end from agent-tracked per-task file set; plus one meta commit for stage closeout (`chore({slug}-stage-X.Y): close`)
  - (b) One single commit for the whole stage (`feat({slug}-stage-X.Y): ...`) — task-level traceability via spec + code-review notes only
  - (c) One commit per task + zero meta commit (closeout ops squashed into last task commit)

### 4.4 Reserve-id workflow

- **D5.** `reserve-id.sh` vs MCP `reserve_backlog_ids` wrapper:
  - (a) Keep shell script, skills shell out
  - (b) MCP tool wraps script + exposes `reserve_backlog_ids(count, prefix)` to agents (already exists per schema — confirm use as primary surface)
  - (c) Shell for CI, MCP for agents (parallel paths)

### 4.5 `plan-applier` wrapper survival

- **D6.** Code-fix mode kept? (Plan-fix + stage-closeout modes retire with B3/C10 drops.)
  - (a) Yes — opus-code-reviewer critical branch still emits `§Code Fix Plan` tuples → plan-applier Mode code-fix
  - (b) No — inline fix application in code-reviewer itself (removes TECH-506 pair entirely)
  - (c) Yes but rename (mode enum shrinks to single mode)

### 4.6 Surviving per-task spec sections

- **D7.** Final section list per task spec. Mark keep / drop:
  - §Goal + §Intent (keep)
  - §Objectives + §Exit (keep — per C6)
  - §Phase bullets (keep — moved from stage file per A5)
  - §Plan Digest (keep — per C7)
  - §Implementation Plan (keep / drop?)
  - §Acceptance Criteria (keep / drop?)
  - §Verification (keep — per-task notes or move to stage-level?)
  - §Code Review (keep — post-review mini-report lands here)
  - §Code Fix Plan (keep conditionally — only during fix window, retires on commit?)
  - §Closeout Plan (drop — C10 folded closeout into ship-stage Pass B tail)
  - §Audit (dropped — B7)
  - §Plan Author (dropped — B6)

### 4.7 Pattern drops — confirm

- **D8.** `docs/implementation/{slug}-stage-X.Y-plan.md` aggregate doc pattern — **drops entirely** (per C7=a). All stage-level info lives in stage file + task specs. Confirm.
- **D9.** `stage-decompose` skill — Step layer already retired; all stages decomposed at `master-plan-new` time. Skill **dead**. Confirm drop.
- **D10.** §Code Fix Plan — during the critical code-review → fix window, where does the tuple list live?
  - (a) Appended to task spec; removed after plan-applier green
  - (b) Ephemeral file under `ia/plans/{task_id}-fix-{timestamp}.md`; deleted after apply
  - (c) Never persisted — plan-applier invoked in-context with tuples in prompt

### 4.8 `validate:master-plan-status` under foldered shape

- **D11.** This validator is kept (C14) but must re-implement for folder traversal. Scope:
  - (a) Walks all `ia/projects/*/index.md` + `stage-*.md` + `tasks/*.yaml`; checks rollup consistency; checks task yaml status vs index Stage row; checks verify field consistency
  - (b) Delegates to `stage_state` MCP tool per stage (single source of truth for rollup math)
  - (c) Both (a) for CI no-MCP env, (b) for local agent path

### 4.9 Operator-confirmed but unlisted until now

- **D12.** `release-rollout` tracker + skill-bug-log pattern — stays as-is, orthogonal to refactor. Confirm.
- **D13.** Exploration docs + mechanical plan docs under `docs/` — stay flat, not foldered. Confirm (operator note: "explorations and mechanical plans will keep being large").

---

## 4. Design invariants (from locked decisions)

These follow directly from §2 and are not up for re-poll.

- **One folder per master plan.** No nested master plans. `ia/projects/{slug}/` is the unit.
- **Flat task namespace under `tasks/`.** Task ids (`TECH-XXX`) remain globally unique across the repo — not scoped per folder.
- **Stage file is a surface, not a container.** Contents minimal (A5); most stage detail lives in index.md + per-task specs.
- **No persisted planning tuples.** §Stage File Plan + §Plan Fix + §Code Fix Plan + §Stage Closeout Plan are either retired (drift scan / audit drops) or regenerated fresh at apply time without survive-in-spec persistence. Exception: §Code Fix Plan survives only across the single Opus-code-review → plan-applier pair window (to be re-examined in Round 2 preempt #2).
- **MCP tools own state.** Agents read via `stage_bundle` / `task_bundle`, mutate via `task_status_flip` (+ Round 3 D3). No hand-edit of index.md Stage rollup rows.
- **No per-task commits during Pass A.** Ship-stage keeps modifications uncommitted across Pass A (per-task implement + compile). All commits land at Pass B stage-end (verify → code-review → closeout → commit). Commit granularity pending D4.
- **Stage status is two-dimensional.** Task completion **and** stage verification. Both queryable via `stage_state`, both mutable via atomic MCP tools.
- **Aggregate `docs/implementation/{slug}-stage-X.Y-plan.md` retires.** All stage-scoped info lives in `ia/projects/{slug}/stage-*.md` + `tasks/*.md` (per C7 = a).
- **Dropped validators stay dropped.** `validate:dead-project-specs`, `validate:frontmatter`, `validate:claude-imports` do not return in warnings-only form.

---

## 6. Handoff plan (updated for DB pivot)

1. Consolidate §2 + §3 (locked) + Round 4 (§4.4) answers when locked.
2. Run `/design-explore --against` for gap analysis (persisted Design Expansion block).
3. Run `/master-plan-new docs/master-plan-foldering-refactor-design.md` once all rounds locked. The new plan itself uses foldered + DB-backed shape as its first self-hosting act (bootstrap ordering below).

**Draft stage sequencing (DB-first, revised):**

- **Stage 0 — DB schema + migration infra.** `ia_master_plans`, `ia_stages`, `ia_tasks` (with `body` + `body_tsv`), `ia_task_spec_history` (optional), `ia_task_commits`, `ia_stage_verifications`, `ia_ship_stage_journal`, `ia_fix_plan_tuples`. `db:migrate` extension. **One-shot import script** converts existing yamls + master plans + `ia/projects/{id}.md` bodies into DB rows (metadata + body text + tsvector regen). Folder layout laid down mechanically (`index.md` + `stage-*.md` only — no `tasks/`). Bulk commit per C13.
- **Stage 1 — MCP tool set.** `stage_state`, `task_state`, `stage_bundle`, `task_bundle`, `master_plan_state`, `task_spec_body`, `task_spec_section`, `task_spec_search`, `task_insert`, `task_status_flip`, `task_spec_section_write`, `stage_verification_flip`, `stage_closeout_apply`, `task_commit_record`, `journal_*`, `fix_plan_*`. Existing `backlog_issue` / `backlog_list` / `backlog_search` re-implement as DB queries (schemas stable).
- **Stage 2 — skill flips.** `stage-file` (merged, DB-aware — writes `ia_tasks` rows + initial body from template), `stage-authoring` (merged, DB-aware — writes §Plan Digest via `task_spec_section_write`), `ship-stage` (Pass A no-commit, Pass B stage-end commits + closeout inline). Drops: `plan-review`, `opus-auditor`, `plan-reviewer-*`, `stage-closeout-*` pair, `stage-decompose`, `plan-applier` code-fix mode, `mechanicalization_score` concept.
- **Stage 3 — web dashboard integration.** Backend CRUD controller (Next.js API routes per E7). Read surface (E6=a): render task bodies from DB, stage rollups, commit history, verification verdicts. Actionable dashboard deferred — see [`docs/actionable-agent-dashboard-exploration.md`](actionable-agent-dashboard-exploration.md).
- **Stage 4 — cleanup.** Drop `materialize-backlog.sh`, `validate:backlog-yaml`, `validate:dead-project-specs`, `validate:frontmatter`, `validate:claude-imports`. Retire `ia/backlog/` + `ia/backlog-archive/` + `BACKLOG.md` generated view (E2). Remove `reserve-id.sh` + `id-counter.json` + filesystem `flock` lockfiles (E3 / E8). Retire `runtime-state.json` (E5). Retire filesystem task-spec bodies (E4=b).

**Ordering rationale:** DB schema + MCP tools before skill flips (skills depend on tools); skill flips before web dashboard (dashboard reads same DB); cleanup last once nothing reads retired surfaces.

**Bootstrap caveat:** the refactor master plan itself lives in `ia/projects/{slug}/` under the new shape — first migration must include this plan. Filesystem layout decision: the plan folder is authored by hand before DB rows exist; Stage 0 migration inserts the DB rows afterward.

---

## 7. Change log

- **2026-04-24** — Doc seeded. Round 1 decisions locked (§2). Round 2 polling open (§3).
- **2026-04-24** — Round 2 answered + locked (§3). A5 upgraded via C6. §4 invariants grew (no per-task commits, 2-D stage status, aggregate doc retires). Round 3 polling open (§4).
- **2026-04-24 (pivot)** — Operator pivoted to **database-backed state model**. Round 3 superseded. §4 rewritten around DB as state surface; filesystem keeps narrative only. §5 archives pre-pivot Round 3 questions for audit. Round 4 polling open (§4.4) — DB platform, yaml retirement, id reservation, sync contract, journal, web dashboard, controller, concurrency, migration sequencing, offline mode, stage verification shape, fix plan tuples, + D4/D6–D9/D12–D13 carried as E13–E18.
- **2026-04-24 (Round 4 lock)** — E1, E1b, E2, E3, E6, E7–E15, E17 locked. E16 drop pending clarification re: exploration→design→master-plan flow. E4 pending operator confirm on recommendation (b) DB body + full-text index. E5 re-asked (skipped in response). E18 clarified (docs/ flat = no folder-per-doc). E6 addendum: [`docs/actionable-agent-dashboard-exploration.md`](actionable-agent-dashboard-exploration.md) stub created.
- **2026-04-24 (Round 4 final)** — Last four items locked: E4=(b) DB body + tsvector index (`tasks/` subfolder retires entirely); E5=(a) journal full replacement of runtime-state + chain-journal + TECH-493; E16 drop confirmed; E18 docs/ flat for now (foldering deferred). §4.2 DB/filesystem split rewritten. §4.3 MCP tool surface extended with `task_spec_body` / `task_spec_section` / `task_spec_search` / `task_spec_section_write`. §6 handoff updated across all 5 stages. **All Round 4 questions resolved.**
- **2026-04-24 (Round 5 lock)** — DB schema specifics locked (§4.5). F1–F15 all on recommendations: Postgres ENUM status (F1); `task_id text PK` (F2); join table for deps (F3); dual tsvector + trigram indexes on body (F4); full-snapshot history table (F5); split plain/binary snapshot (F6); discriminated-union journal payloads (F7); soft-delete + TTL for fix plans (F8); archived-status-flip for closeout (F9); mixed advisory + row locks (F10); bulk-load import order (F11); idempotent import script (F12); no partitioning (F13); API routes call MCP (F14); singleton pg.Pool (F15). Derived artifacts flagged: `ia/migrations/*.sql`, `tools/scripts/ia-db-migrate.ts`, `ia/rules/ship-stage-journal-schema.md`.
