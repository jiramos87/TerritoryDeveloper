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

> **v2 remap refresh (2026-05-10):** Table below maps planned tool names (v1 design) to live registered tool names. Planned tools not yet shipped or superseded noted inline.

| Planned (v1 design) | Live registered name | Status |
|---|---|---|
| `stage_state(slug, stage_id)` | `stage_state` | live |
| `task_state(task_id)` | `task_state` | live |
| `stage_bundle(slug, stage_id)` | `stage_bundle` | live |
| `task_bundle(task_id)` | `task_bundle` | live |
| `master_plan_state(slug)` | `master_plan_state` | live |
| `task_spec_body(task_id)` | `task_spec_body` | live |
| `task_spec_section(task_id, section)` | `task_spec_section` | live |
| `task_spec_search(query, filters)` | `task_spec_search` | live |
| `backlog_issue` / `backlog_list` / `backlog_search` | `backlog_issue` / `backlog_list` / `backlog_search` | live (re-implemented as DB queries) |
| `master_plan_health(slug)` | `master_plan_health` | live |
| `master_plan_next_pending` | `master_plan_next_pending` | live |
| `master_plan_locate` | `master_plan_locate` | live |
| `task_insert(...)` | `task_insert` | live |
| `task_status_flip(task_id, new_status)` | `task_status_flip` | live |
| `task_status_flip` (batch) | `task_status_flip_batch` | live (additional batch variant) |
| `task_spec_section_write(task_id, section, content)` | `task_spec_section_write` | live |
| `stage_verification_flip(slug, stage_id, verdict, commit_sha)` | `cron_stage_verification_flip_enqueue` | live (fire-and-forget cron enqueue, not direct flip) |
| `stage_closeout_apply(slug, stage_id)` | `stage_closeout_apply` | live |
| `task_commit_record(task_id, commit_sha)` | `cron_task_commit_record_enqueue` | live (fire-and-forget cron enqueue) |
| `journal_append(session_id, task_id, phase, payload)` | `cron_journal_append_enqueue` | live (fire-and-forget cron enqueue) |
| `journal_get` / `journal_search` | (superseded) | not shipped; `stage_bundle` covers read path |
| `fix_plan_write(task_id, tuples)` | `fix_plan_write` | live |
| `fix_plan_consume(task_id, round)` | `fix_plan_consume` | live |
| `cron_audit_log_enqueue` | `cron_audit_log_enqueue` | live (additional — not in v1 design) |

**Read (current live):**

- `stage_state(slug, stage_id)` — DB query; no file read.
- `task_state(task_id)` — DB query; returns metadata + status + commits + deps.
- `stage_bundle(slug, stage_id)` — DB state + filesystem stage narrative + task body slices in one payload.
- `task_bundle(task_id)` — DB state + task spec body slices.
- `master_plan_state(slug)` — DB rollup: stages + progress + blockers.
- `task_spec_body(task_id)` — full body markdown from DB.
- `task_spec_section(task_id, section)` — single section slice (§Goal, §Intent, §Plan Digest, etc.).
- `task_spec_search(query, filters)` — full-text + trigram search across all task bodies (replaces grep-over-markdown).
- `backlog_list` / `backlog_search` / `backlog_issue` — live as DB queries.

**Mutate (current live — atomic, transactional or fire-and-forget cron enqueue):**

- `task_insert({slug, stage_id, title, body, ...})` → returns reserved task_id via DB sequence + writes initial body.
- `task_status_flip(task_id, new_status)` — single transaction; `task_status_flip_batch` for stage-level batch.
- `task_spec_section_write(task_id, section, content)` — sectioned atomic body update; optional audit row in `ia_task_spec_history`.
- `cron_stage_verification_flip_enqueue(slug, stage_id, verdict, commit_sha)` — fire-and-forget; cron drains to `ia_stage_verifications`.
- `stage_closeout_apply(slug, stage_id)` — DB flip + filesystem `mv` stage file to `_closed/` in one op.
- `cron_task_commit_record_enqueue(task_id, commit_sha)` — fire-and-forget; cron drains to `ia_task_commits`.
- `cron_journal_append_enqueue(session_id, phase, payload_kind, payload)` — fire-and-forget; cron drains to `ia_ship_stage_journal`.
- `cron_audit_log_enqueue(slug, audit_kind, body)` — fire-and-forget; cron drains to `ia_master_plan_change_log`.
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
- **2026-05-04** — `/design-explore` Phase 0–9 run. Locked decisions across Rounds 1–5 treated as the chosen approach (no Approaches list to compare; doc is post-poll). Plan-shape=flat (sequential Stage 0→4 ladder per §6). Phase 2.5 architecture-decision MCP writes deferred to `/master-plan-new` (DB rows premature pre-master-plan; surfaces hit will produce DEC-A{N} at plan-author time). Persist contract v2 §Core Prototype + §Iteration Roadmap authored. Self-review surfaced 2 BLOCKING items resolved inline (import scope clarification + bootstrap ordering). Single Design Expansion block appended.

---

## Design Expansion

### Plan Shape
- Shape: flat
- Rationale: §6 sequences Stage 0 (DB schema + import) → 1 (MCP tools) → 2 (skill flips) → 3 (web dashboard) → 4 (cleanup). Strict dependency chain: skills depend on tools; tools depend on schema; cleanup depends on nothing reading retired surfaces. Zero parallel work streams identified.

### Core Prototype
- `verb:` Agent reads master-plan rollup state (stage progress + task statuses) for the foldering refactor plan itself by calling `stage_state` MCP tool against the Postgres `ia_*` tables, instead of walking `ia/projects/*.md` markdown.
- `hardcoded_scope:`
  - Single master plan imported at Stage 1.0 boundary: `master-plan-foldering-refactor` (this very plan, hand-authored as carcass `index.md` + `stage-0.1-*.md` files BEFORE schema lands).
  - Single Postgres instance: existing Unity-bridge instance at `localhost:5434/territory_ia_dev` (E1).
  - Single migration file: `ia/migrations/{ts}-ia-schema.sql` covering ALL F1–F15 schema (no incremental migrations within Stage 1.0).
  - One-shot import script targets backlog snapshot taken at Stage 1.0 start (no re-import loop yet).
- `stubbed_systems:`
  - `task_status_flip` — writes only; no listeners / no cascade to UI / no journal append yet.
  - `stage_closeout_apply` — DB flip only; filesystem `mv` to `_closed/` deferred to Stage 2.x (skill flips).
  - `journal_append` — accepts payload but no `journal_search` / no consumer yet (validates write path only).
  - `task_spec_search` — index built but no caller wired.
  - Web dashboard route stubs return `{ok:true, payload:{}}` — render layer deferred to Stage 3.x.
- `throwaway:`
  - Hand-authored `ia/projects/master-plan-foldering-refactor/index.md` carcass (gets re-rendered from DB once read tools wire up).
  - Stage 1.0 import-script verbose stdout output (manual eyeball of row counts).
  - Sample `stage_state` CLI invocation used to verify schema (replaced by skill chain consumers in Stage 2.x).
- `forward_living:`
  - `ia/migrations/{ts}-ia-schema.sql` — full schema per F1–F15 (Postgres ENUMs, `task_id text PK`, deps join table, dual GIN indexes, history table, etc.).
  - Singleton `pg.Pool` at MCP server boot (F15) — survives all stages.
  - `ia_*` table set + invariants (F1–F15) — locked structural layer.
  - Daily snapshot job per F6 (split plain SQL + binary `-Fc`).
  - Bulk-load import order per F11 (GIN indexes last) — one-time but contract for any future re-import.

### Iteration Roadmap

| Stage | Scope | Visibility delta |
|---|---|---|
| 1.x | Tracer slice — schema + import + `stage_state` read tool only. Hand-authored refactor-plan carcass. | Agent queries refactor-plan rollup from DB; output identical to old yaml walk but ~50ms vs ~800ms. |
| 2.x | MCP tool surface — read suite (`task_state`, `stage_bundle`, `task_bundle`, `master_plan_state`, `task_spec_body`, `task_spec_section`, `task_spec_search`) + mutate suite (`task_insert`, `task_status_flip`, `task_spec_section_write`, `stage_verification_flip`, `stage_closeout_apply`, `task_commit_record`) + journal suite + fix-plan suite. Re-implement `backlog_*` tools as DB queries. | Agent fetches a single task spec body slice in one MCP call instead of glob+grep over `ia/projects/*.md`; full-text search across all task bodies in <100ms. |
| 3.x | Skill flips — merge `stage-file-plan + stage-file-apply` → `stage-file`; merge `plan-author + plan-digest` → `stage-authoring`; drop `plan-review`, `opus-auditor`, `plan-reviewer-*`, `stage-decompose`, `plan-applier` code-fix mode, `mechanicalization_score`. Fold closeout into `ship-stage` Pass B. Pass A no-commit; Pass B single stage commit (E13). `stage-file` calls `stage-authoring` inline (C8). | Operator runs `/stage-file {slug} {stage}` once and gets filed + authored stage. `/ship-stage` no longer per-task commits — single stage commit at end. ~5 fewer skill seams in the chain. |
| 4.x | Web dashboard read surface — Next.js API routes per E7 calling MCP tools per F14. Pages render master-plan rollup + stage detail + task body markdown from DB. Read-only (E6=a). | Operator opens `web/projects/{slug}` in browser and sees live rollup + task statuses + bodies; no CLI needed for visibility. |
| 5.x | Cleanup — drop `materialize-backlog.sh`, `validate:backlog-yaml`, `validate:dead-project-specs`, `validate:frontmatter`, `validate:claude-imports`. Retire `ia/backlog/`, `ia/backlog-archive/`, `BACKLOG.md`, `BACKLOG-ARCHIVE.md`, `reserve-id.sh`, `id-counter.json`, `runtime-state.json`, all `.lock` files. Update `interchange.md`, `orchestrator-vs-spec.md`, `project-hierarchy.md`, `glossary.md`, `invariants.md`. | Repo loses ~15 retired surfaces; `validate:all` chain shorter; new contributor onboarding sees one DB + one folder shape, not two parallel state surfaces. |

### Chosen Approach
DB-backed IA state + foldered narrative + merged lifecycle skills + atomic MCP mutations. Locked across Rounds 1–5 of operator polling in §2–§4.5. No Approaches list to compare — the doc IS the converged decision after 5 rounds. Synthesis: Postgres `ia_*` tables (F1–F15) replace `ia/backlog/*.yaml` + `id-counter.json` + `runtime-state.json` + `flock` lockfiles; `ia/projects/{slug}/index.md` + `stage-*.md` + `_closed/` becomes the only filesystem surface; new MCP tool surface (`stage_state`, `task_*`, `journal_*`, `fix_plan_*`) is the sole mutation entry; ship-stage Pass A drops per-task commits in favour of single stage-end commit; ~5 skills retire (`plan-review`, `opus-auditor`, `plan-reviewer-mechanical`, `plan-reviewer-semantic`, `stage-decompose`); ~3 skills merge (`stage-file-plan + stage-file-apply`, `plan-author + plan-digest`, `stage-closeout` folded into `ship-stage`).

### Architecture Decision
**Phase 2.5 deferred to `/master-plan-new`.** This refactor touches `ia/specs/architecture/interchange.md` (new "DB-backed IA state" section) + retires `ia/state/runtime-state.json` (currently described in `interchange.md` Local verification) + adds `ia_*` schema as new architectural inventory. Surfaces hit qualify for `arch_decisions` row, BUT writing the row + `arch_changelog_append` + `arch_drift_scan` is premature before the master plan exists (no `plan_slug` to scope drift scan against). `/master-plan-new` Phase 2.5 will author DEC-A{N} at the slug `architecture-lock-foldering-refactor` (or 3 plan-scoped rows if shape flips to carcass+section in master-plan authoring). Surface slugs to lock: `interchange/agent-ia`, `interchange/local-verification`, `layers/full-dependency-map` (DB layer added). Stop condition tracked: any drift scan failure at `/master-plan-new` time aborts plan creation per skill recipe.

### Architecture

```mermaid
flowchart LR
  subgraph FS[Filesystem narrative]
    INDEX[ia/projects/{slug}/index.md]
    STAGE[ia/projects/{slug}/stage-*.md]
    CLOSED[ia/projects/{slug}/_closed/]
  end

  subgraph SKILLS[Lifecycle skills]
    SF[stage-file<br/>merged]
    SA[stage-authoring<br/>merged]
    SS[ship-stage<br/>Pass A no-commit<br/>Pass B single commit]
    MPN[master-plan-new]
    PN[project-new]
  end

  subgraph MCP[MCP tool surface — territory-ia]
    READ[stage_state<br/>task_state<br/>stage_bundle<br/>task_bundle<br/>master_plan_state<br/>task_spec_body<br/>task_spec_section<br/>task_spec_search]
    MUT[task_insert<br/>task_status_flip<br/>task_spec_section_write<br/>stage_verification_flip<br/>stage_closeout_apply<br/>task_commit_record]
    JOUR[journal_append<br/>journal_get<br/>journal_search]
    FIX[fix_plan_write<br/>fix_plan_consume]
  end

  subgraph DB[Postgres ia_* schema]
    T1[ia_master_plans]
    T2[ia_stages]
    T3[ia_tasks<br/>+ body tsvector + GIN trgm]
    T4[ia_task_deps]
    T5[ia_task_spec_history]
    T6[ia_task_commits]
    T7[ia_stage_verifications]
    T8[ia_ship_stage_journal]
    T9[ia_fix_plan_tuples]
  end

  subgraph WEB[Web dashboard]
    API[Next.js API routes<br/>web/app/api/projects/...]
    UI[Read-only pages<br/>web/app/projects/...]
  end

  subgraph SNAP[Daily snapshot]
    SQL[ia/state/db-snapshot.sql<br/>metadata, plain]
    DUMP[ia/state/db-snapshot.dump<br/>body+journal, binary -Fc]
  end

  SF -->|reads| READ
  SF -->|writes| MUT
  SA -->|writes via task_spec_section_write| MUT
  SS -->|reads| READ
  SS -->|writes| MUT
  SS -->|appends| JOUR
  SS -->|consumes/writes| FIX
  MPN -->|writes| MUT
  MPN -->|writes carcass files| INDEX
  MPN -->|writes carcass files| STAGE
  PN -->|writes via task_insert| MUT

  READ --> DB
  MUT --> DB
  JOUR --> T8
  FIX --> T9

  MUT -->|stage_closeout_apply mv| CLOSED

  API -->|server-side calls| READ
  UI --> API

  DB -->|cron/CI| SNAP
```

**Entry points (where callers invoke):**
- `/stage-file {slug} {stage_id}` — operator invokes; merged stage-file skill writes `ia_tasks` rows via `task_insert`, initial bodies via `task_spec_section_write`, then calls `stage-authoring` inline (C8) which writes §Plan Digest per task.
- `/ship-stage {slug} {stage_id}` — operator invokes; Pass A loops tasks (implement + compile + NO commit + journal_append per phase); Pass B runs verify-loop + code-review (inline fix on critical, no plan-applier code-fix mode per E14) + closeout (`stage_closeout_apply` flips status + mv stage file to `_closed/`) + single stage commit per E13.
- `/master-plan-new {DOC_PATH}` — authors `index.md` + initial `stage-*.md` carcass files + writes `ia_master_plans` + `ia_stages` rows via MCP.
- `/project-new {ID}` — single-issue path; calls `task_insert` directly without stage parent.
- Web dashboard `GET /projects/{slug}` — server-side route handler calls `master_plan_state` MCP; renders.

**Exit (what callers receive):**
- All MCP tools return canonical shape `{ ok: bool, payload: {...}, meta?: { partial?: { succeeded, failed }, ... } }` per existing `territory-ia` server convention.
- Skills emit chain-level digests via stdout + write final `ia_ship_stage_journal` payload with `payload_kind: 'stage_complete'` per F7.
- Web API routes wrap MCP shape directly (F14) — no schema translation.

### Subsystem Impact

| Subsystem | Touch | Invariant risk | Breaking? | Mitigation |
|---|---|---|---|---|
| `ia/backlog/*.yaml` + `materialize-backlog.sh` | RETIRES (E2) | Inv 12, Inv 13 | BREAKING | Daily snapshot E10 + import script preserves history before drop; drop happens at Stage 5.x cleanup AFTER all consumers flipped |
| `ia/state/id-counter.json` + `reserve-id.sh` | RETIRES (E3) | Inv 13 (monotonic id source) | BREAKING | DB sequence per prefix + advisory lock (F10) replace flock; rule 13 of `invariants.md` rewrites at Stage 5.x |
| `ia/state/runtime-state.json` | RETIRES (E5=a) | flock-per-domain guardrail | BREAKING | `ia_ship_stage_journal` absorbs `runtime-state.json` + in-process `{CHAIN_JOURNAL}` + TECH-493 disk journal proposal |
| `.id-counter.lock` / `.closeout.lock` / `.materialize-backlog.lock` / `.runtime-state.lock` | RETIRE (E8) | flock-per-concurrency-domain guardrail | BREAKING | DB transactions + advisory locks + row-level `FOR UPDATE` per F10 |
| `ia/projects/*.md` (current flat shape) | REFOLDER (A1–A6) | Inv 12 (specs under `ia/projects/` for permanent domains) | BREAKING | `tools/scripts/fold-master-plan.ts` one-shot per plan; bulk commit (C13); guardrail "one folder per master plan" added |
| `ia/projects/{slug}/tasks/` subfolder | NEVER MATERIALIZE (E4=b) | none | additive | Task body lives in `ia_tasks.body` text + `body_tsv` GIN + trigram GIN; history via `ia_task_spec_history` (F5) |
| Skills `plan-review`, `opus-auditor`, `plan-reviewer-mechanical`, `plan-reviewer-semantic` | DROP (B3–B5) | none (skills not invariants) | BREAKING for chains referencing them | Move to `.claude/agents/_retired/` + `.claude/commands/_retired/`; skill-tools generator + `validate:skill-drift` catch stale wires |
| Skill `stage-decompose` | DROP (E16) | none | additive (already retired in practice per pre-pivot D9 lock) | Flow preserved via `design-explore` → `master-plan-new` → `master-plan-extend` |
| Skill `plan-applier` code-fix mode | DROP (E14) | none | BREAKING for code-reviewer chain | Code-reviewer applies fix inline; TECH-506 plan-apply pair retires fully |
| `§Stage File Plan` / `§Plan Author` / `§Audit` / `§Closeout Plan` / `§Code Fix Plan` persisted sections | DROP (B6, B7, C7, C10, F8) | `ia/rules/plan-apply-pair-contract.md` + `ia/rules/plan-digest-contract.md` | BREAKING for those rules | Update both rules at Stage 5.x; `mechanicalization_score` references purged |
| `web/app/projects/*` (new) | NEW SURFACE (E6=a, E7) | `ia/rules/web-backend-logic.md` | additive | Read-only first; actionable scope deferred to `docs/actionable-agent-dashboard-exploration.md` |
| `tools/mcp-ia-server/src/index.ts` | EXTEND HEAVILY (~15 new tools) | Universal-safety MCP-first directive | additive | Tool surface area documented in `ia/rules/ship-stage-journal-schema.md` (F7) for journal payloads + per-tool descriptors registered before skill flips (Stage 2.x must wait) |
| `validate:backlog-yaml` / `validate:dead-project-specs` / `validate:frontmatter` / `validate:claude-imports` | DROP (C14, E2 cascade) | `validate:all` chain | BREAKING for CI | Hard-gate set narrows; CI green threshold updated in same Stage 5.x commit |
| `ia/specs/architecture/interchange.md` | UPDATE | DEC-A11 (doc-home) | additive (new "DB-backed IA state" section) | Document Postgres `ia_*` schema + tool surface; Local verification subsection updated to reflect `runtime-state.json` retirement |
| `ia/specs/architecture/layers.md` | UPDATE | DEC-A11 | additive (new layer entry: IA state DB) | Add Postgres `ia_*` to dependency map |
| `ia/rules/orchestrator-vs-spec.md` | UPDATE | rules R1–R6 status flip matrix | BREAKING for matrix wording (file-flip → DB-flip) | Rewrite matrix referencing `task_status_flip` + `stage_closeout_apply` MCP tools |
| `ia/rules/project-hierarchy.md` | UPDATE | Stage/Task cardinality gate | additive (gate stays; persistence flips) | Note "stage-file enforces gate via DB query, not yaml walk" |
| `ia/rules/invariants.md` | UPDATE | Inv 13 + flock guardrail | BREAKING for rule 13 + 1 guardrail | Rewrite rule 13 (DB sequence replaces id-counter.json) + retire flock guardrail (DB transactions + advisory locks) |
| `ia/specs/glossary.md` entries: Backlog record, Project spec, Project hierarchy, closeout apply, Ship-stage dispatcher, Stage tail | UPDATE | terminology-consistency rule | BREAKING for definitions | Rewrite definitions in Stage 5.x cleanup; run `npm run generate:ia-indexes` after edits |
| `MEMORY.md` ephemeral pattern + `.claude/memory/{slug}.md` | UNCHANGED | none | additive | Orthogonal to plan/task DB state; ephemeral memory survives untouched |

### Implementation Points

#### Stage 1.x — DB schema + import infra (tracer slice)
- [ ] Hand-author refactor-plan carcass: `ia/projects/master-plan-foldering-refactor/index.md` + `stage-1.1-db-schema.md` … `stage-5.1-cleanup.md` BEFORE schema lands (bootstrap caveat — no DB yet).
- [ ] Author `ia/migrations/{ts}-ia-schema.sql` per F1–F15: CREATE TYPE (`task_status` ENUM, `stage_verdict` ENUM, journal `payload_kind`); CREATE TABLE `ia_master_plans`, `ia_stages` (composite PK), `ia_tasks` (`task_id text PK`, `body text`, `body_tsv tsvector GENERATED ALWAYS AS STORED`), `ia_task_deps` (join table, F3), `ia_task_spec_history` (full snapshot, F5), `ia_task_commits`, `ia_stage_verifications` (single latest row, E11), `ia_ship_stage_journal` (`payload_kind text` + `payload jsonb`, F7), `ia_fix_plan_tuples` (soft-delete `applied_at`, F8); CREATE INDEX `GIN(body_tsv)` + `GIN(body gin_trgm_ops)` LAST (bulk-load order F11).
- [ ] Author `tools/scripts/ia-db-migrate.ts` idempotent import (F12): wrap each run in transaction; detect existing rows + skip; bulk-load order per F11 (schema → master_plans → stages → tasks metadata → bodies → tsvector regen → GIN indexes last).
- [ ] Import scope (clarified per BLOCKING B1): `ia/backlog/*.yaml` + `ia/backlog-archive/*.yaml` → `ia_tasks` metadata rows (1 yaml = 1 row); existing `ia/projects/*.md` files (single-issue project specs, NOT master plans) → `ia_tasks.body` text column for the matching `task_id`; existing `ia/projects/master-plan-*.md` → decomposed into `ia_master_plans` + `ia_stages` rows (one master plan = one `ia_master_plans` row + N `ia_stages` rows parsed from `### Stage N.M` blocks).
- [ ] Bootstrap ordering (clarified per BLOCKING B2): (1) hand-author refactor-plan carcass folder; (2) Stage 1.1 lands schema; (3) Stage 1.2 import script runs against full backlog snapshot INCLUDING the hand-authored refactor-plan carcass (last in import order so DB rows reference live filesystem); (4) Stages 2–5 run via DB.
- [ ] Extend `npm run db:migrate` to include `ia_*` migration tree (E1b — shared `db:migrate`, `ia_*` prefix).
- [ ] Singleton `pg.Pool` at `territory-ia` MCP server boot (F15); all tools share.
- [ ] Implement first read tool `stage_state(slug, stage_id)` — DB query only, returns `{tasks, progress, blocker, next_pending, objectives, exit, commit_hashes_seen, verify: {verdict, commit_sha, ran_at}}`.
- [ ] Daily snapshot job (cron or CI): split plain SQL (metadata tables, diffable) + binary `-Fc` (body+journal dump) per F6, write to `ia/state/db-snapshot.sql` + `ia/state/db-snapshot.dump`; commit via cron user.
- Risk: F11 bulk-load order critical — GIN indexes after body insert; otherwise import wall-clock 10–100x worse. Mitigation: import-script integration test asserts index creation timestamp > body insert timestamps.

#### Stage 2.x — MCP tool surface
- [ ] Read suite: `task_state`, `stage_bundle`, `task_bundle`, `master_plan_state`, `task_spec_body`, `task_spec_section`, `task_spec_search`.
- [ ] Mutate suite: `task_insert` (uses DB sequence per prefix per E3, returns reserved `task_id`); `task_status_flip` (single transaction, row-level `FOR UPDATE` per F10); `task_spec_section_write` (sectioned atomic body update; writes `ia_task_spec_history` row per F5); `stage_verification_flip` (overwrite single row per E11); `stage_closeout_apply` (DB flip status → `archived` per F9 + filesystem `mv` stage file to `_closed/`); `task_commit_record`.
- [ ] Journal suite: `journal_append` (validates `payload_kind` against schema in `ia/rules/ship-stage-journal-schema.md` per F7); `journal_get`; `journal_search`.
- [ ] Fix-plan suite: `fix_plan_write`; `fix_plan_consume` (sets `applied_at` for soft-delete + 30-day TTL cleanup per F8).
- [ ] Re-implement `backlog_issue` / `backlog_list` / `backlog_search` as DB queries (existing tool schemas stable — storage flips only).
- [ ] Author `ia/rules/ship-stage-journal-schema.md` documenting per-`payload_kind` schemas: `phase_start`, `phase_complete`, `compile_check`, `verify_run`, `code_review`, `closeout_apply`, `stage_complete`.
- [ ] Restart MCP host after registering new tool descriptors (server caches schema at session start).
- Risk: tool schema must be stable BEFORE Stage 3.x skill flips — re-publish breaks subagent caches. Mitigation: tool-set frozen + integration tests green before Stage 2.x ships.

#### Stage 3.x — Skill flips
- [ ] Merge `stage-file-plan` + `stage-file-apply` → single `stage-file` (B1); update `ia/skills/stage-file/SKILL.md` frontmatter + body; run `npm run skill:sync:all`.
- [ ] Merge `plan-author` + `plan-digest` → single `stage-authoring` (B2); same regen pipeline.
- [ ] Drop `plan-review`, `opus-auditor`, `plan-reviewer-mechanical`, `plan-reviewer-semantic` (B3–B5) — move to `.claude/agents/_retired/` + `.claude/commands/_retired/`.
- [ ] Fold `stage-closeout` into `ship-stage` Pass B tail (C10) — drop `stage-closeout-planner` + `plan-applier` stage-closeout mode.
- [ ] Drop `stage-decompose` skill (E16).
- [ ] Drop `plan-applier` code-fix mode (E14) — code-reviewer applies fix inline; TECH-506 pair retires fully.
- [ ] Update `stage-file` to call `stage-authoring` inline (C8) — one command files + authors.
- [ ] `ship-stage` Pass A: implement + compile + NO commit; Pass B: verify-loop + code-review (inline fix on critical) + closeout + single stage commit per E13 (`feat({slug}-stage-X.Y): ...`).
- [ ] Drop `mechanicalization_score` from all skill bodies + templates + validators + `§Stage File Plan` / `§Plan Digest` references.
- [ ] Run `npm run skill:sync:all` to regenerate `.claude/agents/*` + `.claude/commands/*` + `.cursor/*` adapters.
- [ ] Update `ia/skills/_preamble/stable-block.md` if Tier 1 cache contents change; re-validate via `npm run validate:cache-block-sizing`.
- [ ] Update `validate:skill-drift` whitelist to reflect retired skill set.
- Risk: subagent prompt cache invalidation — Tier 1 cache block hash changes when stable-block edits land. Mitigation: schedule skill flip commits during low-activity window; document cache-warm cost in change log.

#### Stage 4.x — Web dashboard read surface
- [ ] Next.js API routes: `web/app/api/projects/[slug]/route.ts`, `web/app/api/projects/[slug]/stages/[id]/route.ts`, `web/app/api/projects/[slug]/tasks/[id]/route.ts` (E7).
- [ ] Routes call MCP tools server-side (F14) — no direct `pg` client in `web/`; introduce `web/lib/mcp-client.ts` thin wrapper if reachable from route handlers.
- [ ] Pages: `web/app/projects/[slug]/page.tsx` (master plan rollup card per `ia/specs/web-ui-design-system.md §4.6 PlanProgressCard`), `web/app/projects/[slug]/stages/[id]/page.tsx`, `web/app/projects/[slug]/tasks/[id]/page.tsx`.
- [ ] Read-only — no mutation buttons (E6=a); actionable deferred per `docs/actionable-agent-dashboard-exploration.md`.
- [ ] Render task body markdown from DB (`task_spec_body` MCP tool) — reuse existing markdown renderer in `web/lib/`.
- [ ] Apply `web-ui-design-system.md` tokens (`ds-*`, `web/lib/design-tokens.ts`).
- Risk: web/lib MCP-client wrapper may need server-only segregation (Next.js App Router). Mitigation: implement under `web/lib/server/` with `import 'server-only'` directive.

#### Stage 5.x — Cleanup
- [ ] Drop `tools/scripts/materialize-backlog.sh`, `tools/scripts/validate-backlog-yaml.mjs`, `tools/scripts/validate-dead-project-specs.mjs`, `tools/scripts/validate-frontmatter.mjs`, `tools/scripts/validate-claude-imports.mjs`.
- [ ] Retire `ia/backlog/` + `ia/backlog-archive/` directories (after E10 snapshot confirms history preserved).
- [ ] Retire `BACKLOG.md` + `BACKLOG-ARCHIVE.md` generated views; update `CLAUDE.md` §3 Task routing rows that reference them; replace with MCP `backlog_*` references.
- [ ] Remove `tools/scripts/reserve-id.sh` + `ia/state/id-counter.json`.
- [ ] Remove `.id-counter.lock` + `.closeout.lock` + `.materialize-backlog.lock` + `.runtime-state.lock`.
- [ ] Retire `ia/state/runtime-state.json`.
- [ ] Update `ia/specs/architecture/interchange.md`: new "DB-backed IA state" section (Postgres `ia_*` schema + MCP tool surface + journal payload kinds); Local verification subsection rewritten to drop `runtime-state.json` reference.
- [ ] Update `ia/specs/architecture/layers.md`: add Postgres `ia_*` to dependency map.
- [ ] Update `ia/rules/orchestrator-vs-spec.md`: status-flip matrix (R1, R2, R5, R6) references `task_status_flip` + `stage_closeout_apply` MCP tools, not `stage-file applier pass` / file mutations.
- [ ] Update `ia/rules/project-hierarchy.md`: Stage/Task cardinality gate enforcement note ("via DB query, not yaml walk"); update `Backlog record` glossary citation.
- [ ] Update `ia/specs/glossary.md`: rewrite `Backlog record`, `Project spec`, `Project hierarchy`, `closeout apply`, `Ship-stage dispatcher`, `Stage tail (open / incomplete)` definitions.
- [ ] Update `ia/rules/invariants.md`: rewrite rule 13 (DB sequence replaces `id-counter.json`); retire flock guardrail (DB transactions + advisory locks); MCP-first ordering note updated with new `task_*` / `stage_*` / `journal_*` / `fix_plan_*` tool families.
- [ ] Update `ia/rules/plan-apply-pair-contract.md` + `ia/rules/plan-digest-contract.md` to reflect retired pair tail surfaces (B6, B7, C7, C10).
- [ ] Run `npm run generate:ia-indexes` after glossary edits.
- [ ] Run `npm run validate:all` + `npm run verify:local` — green required before final cleanup commit.
- Risk: `validate:claude-imports` drops — `CLAUDE.md` `@`-imports drift gate retires; schedule manual audit checklist as compensating control.

**Deferred / out of scope:**
- `docs/` foldering (E18) — separate follow-up exploration.
- Actionable web dashboard (E6) — `docs/actionable-agent-dashboard-exploration.md` stub already created.
- Measurement harness + weekly friction triage cadence (operator-dismissed).
- `mechanicalization_score` concept (operator-dismissed, drop in Stage 3.x).
- `release-rollout` skill + skill-bug-log pattern (E17 — stays as-is).
- TECH-10309 prototype-first methodology Stage 1.4 validator gate consumption — that consumer integration runs separately; this refactor only provides the reviewed master plan as test fixture.

### Examples

**Example 1 — `task_spec_search` MCP invocation + response (most non-obvious new tool):**

```json
// Request
{
  "tool": "mcp__territory-ia__task_spec_search",
  "args": {
    "query": "stage_closeout_apply",
    "filters": { "status": ["in_progress", "in_review"], "slug": "master-plan-foldering-refactor" },
    "limit": 5
  }
}

// Response
{
  "ok": true,
  "payload": {
    "matches": [
      {
        "task_id": "TECH-10412",
        "title": "stage-closeout fold into ship-stage Pass B tail",
        "slug": "master-plan-foldering-refactor",
        "stage_id": "3.4",
        "status": "in_progress",
        "body_excerpt": "...folds <mark>stage_closeout_apply</mark> MCP call into ship-stage Pass B tail...",
        "rank": 0.847,
        "match_kind": "tsvector"
      },
      {
        "task_id": "TECH-10408",
        "title": "stage_closeout_apply MCP tool",
        "slug": "master-plan-foldering-refactor",
        "stage_id": "2.3",
        "status": "in_review",
        "body_excerpt": "...implements <mark>stage_closeout_apply(slug, stage_id)</mark> with DB flip...",
        "rank": 1.0,
        "match_kind": "tsvector"
      }
    ]
  },
  "meta": { "scanned": 47, "matched": 2 }
}
```

**Example 2 — `ia_ship_stage_journal` row with discriminated payload (F7):**

```sql
INSERT INTO ia_ship_stage_journal (session_id, task_id, payload_kind, payload, created_at)
VALUES (
  '01HXYZ-ship-stage-foldering-3.4',
  'TECH-10412',
  'compile_check',
  '{
    "exit_code": 0,
    "duration_ms": 8421,
    "files_compiled": 312,
    "warnings": 0,
    "stderr_tail": ""
  }'::jsonb,
  now()
);
```

Per `ia/rules/ship-stage-journal-schema.md`, `payload_kind = 'compile_check'` schema requires `exit_code` (int), `duration_ms` (int), `files_compiled` (int), `warnings` (int), `stderr_tail` (string ≤2000 chars). Schema validation lives in `journal_append` MCP tool.

**Example 3 — Edge case: idempotent re-run of import script (F12):**

Operator runs `tools/scripts/ia-db-migrate.ts` twice in a row (e.g. CI retry after transient failure):

- Run 1: schema CREATE statements execute; `ia_master_plans` rows inserted (3 master plans); `ia_tasks` rows inserted (847 tasks); `ia_task_spec_history` rows = 0 (initial import is row 0).
- Run 2: schema CREATE statements throw `relation already exists` — **caught + skipped**; row insert loop `SELECT 1 FROM ia_tasks WHERE task_id = $1` per row — already present, skipped; bodies re-checked via hash; if body markdown changed since Run 1 (e.g. operator manually edited `ia/projects/{id}.md` between runs), Run 2 detects diff + writes new `ia_tasks.body` value + appends `ia_task_spec_history` snapshot row (F5 full-snapshot semantics); else no-op.
- Expected stdout: `Import: 0 master_plans inserted (3 skipped), 0 stages inserted (47 skipped), 0 tasks inserted (847 skipped), 2 bodies updated, 2 history snapshots written.`
- Wrap each run in single transaction per F12; rollback on any error mid-run.

### Review Notes

NON-BLOCKING:
- N1: Subsystem Impact does not enumerate per-skill `SKILL.md` frontmatter fields touched — drop cascades to `tools/scripts/skill-tools/` generator. `validate:skill-drift` (in `validate:all`) catches stale wires automatically; worth tracking explicitly in Stage 3.x checklist row if any skill survives with frontmatter changes.
- N2: `MEMORY.md` + `.claude/memory/{slug}.md` ephemeral memory pattern unchanged by this refactor — confirmed orthogonal to plan/task DB state. No work item; flagged for clarity.
- N3: Phase 2.5 architecture-decision MCP writes deferred to `/master-plan-new` — downstream skill MUST poll for DEC slug, rationale, alternatives, surface_slugs at master-plan authoring time. Drift scan runs against this plan's `plan_slug = master-plan-foldering-refactor` after schema lands at Stage 1.1.

SUGGESTIONS:
- S1: `task_spec_search` example (Example 1 above) added as canonical fixture — most non-obvious new tool.
- S2: Iteration Roadmap rows now map 1:1 to §6 Stages 0→4 (renumbered as Stages 1.x→5.x in Roadmap to match `master-plan-new` Stage numbering convention which starts at 1.0 not 0.0). Tracer slice ↔ Stage 1.x parity preserved.

### Expansion metadata
- Date: 2026-05-04
- Model: claude-opus-4-7
- Approach selected: Locked synthesis across Rounds 1–5 (no Approaches list compared — doc is post-poll convergence; 41 lettered decisions A1–A6, B1–B8, C1–C15, E1–E18, F1–F15)
- Blocking items resolved: 2 (B1 import scope clarification; B2 bootstrap ordering explicit step list)
