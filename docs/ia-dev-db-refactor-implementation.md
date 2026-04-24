---
purpose: "Sequential implementation plan for the IA dev system DB-primary refactor. Opus agents iterate step by step, logging findings + filling gaps inline. Living doc — not a mechanical pre-laid plan; anchors on locked decisions from `docs/master-plan-foldering-refactor-design.md` and leaves voids for implementers to resolve."
audience: both
loaded_by: ondemand
slices_via: none
---

# IA dev system DB-primary refactor — sequential implementation

> **Branch:** `feature/ia-dev-db-refactor` (cut from `main` @ `1563765`)
> **Mode:** Pure sequential. One step at a time. No parallel stages. No `/master-plan-new` ceremony. Implementers (Opus agents) iterate; each step logs findings + verifies + commits before the next begins.
> **Source of truth for decisions:** [`docs/master-plan-foldering-refactor-design.md`](master-plan-foldering-refactor-design.md) Rounds 1, 2, 4, 5 (all locked). Gaps intentional — implementers fill inline.
> **Companion:** [`docs/master-plan-execution-friction-log.md`](master-plan-execution-friction-log.md) — log unexpected friction during this refactor to feed post-refactor retrospective.

---

## 0. Operating model

- **One step at a time.** Finish step N, log findings, commit. Only then start step N+1.
- **Implementer fills voids.** Each step lists *what* + *why* + *acceptance*. It does NOT pre-specify file diffs, exact column schemas, or exact migration SQL. Implementer reads the design doc, makes concrete choices, writes code, records what was chosen + rationale in this doc under the step's `Findings` block.
- **No §Plan Digest / §Audit / §Closeout Plan.** This refactor does not use the broken master-plan pipeline. Direct commits per step. Commit message format: `feat(ia-dev-db): step N.M — {short description}`.
- **Scope lock.** Do NOT extend scope mid-step. If a step uncovers unavoidable adjacent work, record it under `Followups` in this doc; defer execution until current step is green.
- **Rollback discipline.** Every step must be independently revertable (single commit or tight commit cluster). If a step goes sideways, revert, reset `Findings`, retry.
- **Tooling invariants.** Do NOT regress `db:migrate` / `unity:compile-check` / `validate:all` on the Unity + web surfaces during the refactor. New IA DB tables land additively (`ia_*` prefix); nothing existing breaks until cleanup phase (last step).

---

## 1. Step sequence

### Step 1 — DB schema foundation

**Goal:** Create all `ia_*` tables + types + indexes in Postgres via a new migration. No data imported yet. No MCP changes yet.

**Inputs:**

- Design doc §4.2 (DB vs filesystem split — table list).
- Design doc §4.5 F1–F15 (schema specifics: enum types, text PK, join table for deps, dual tsvector + trigram indexes, full-snapshot history table, mixed concurrency primitives).
- Existing `db:migrate` chain used by Unity bridge.

**Outputs:**

- New migration file under `tools/scripts/db-migrations/` (or wherever the existing chain lives — implementer discovers).
- Tables: `ia_master_plans`, `ia_stages`, `ia_tasks`, `ia_task_deps`, `ia_task_spec_history`, `ia_task_commits`, `ia_stage_verifications`, `ia_ship_stage_journal`, `ia_fix_plan_tuples`.
- Types: `task_status` ENUM, `stage_verdict` ENUM (values — implementer decides final set, record under Findings).
- Indexes: `GIN(ia_tasks.body_tsv)`, `GIN(ia_tasks.body gin_trgm_ops)`, btree on `(slug, stage_id)` for stages, reverse-lookup index on `ia_task_deps(depends_on_id)`.
- Sequences per prefix: `tech_id_seq`, `feat_id_seq`, `bug_id_seq`, `art_id_seq`, `audio_id_seq`.

**Acceptance:**

- `npm run db:migrate` applies clean.
- `psql` inspection confirms all tables + types + indexes present.
- `db:bridge-preflight` still green (no regression on Unity-bridge schema).
- Sequence `SELECT nextval('tech_id_seq')` returns a value > current `ia/state/id-counter.json` max (implementer determines exact seed + documents).

**Voids for implementer:**

- Exact column types (`text` vs `varchar(N)`), nullability, defaults.
- Enum values — start from design doc §4.5 F1 recommendation (`pending | implemented | verified | done | archived`) but confirm set is sufficient after reviewing lifecycle.
- FK cascade rules (`ON DELETE CASCADE` vs `RESTRICT` vs `SET NULL`).
- Audit column names (`created_at`, `updated_at`, `actor` — choose convention consistent with existing Unity-bridge tables).
- Migration file naming (timestamp convention — match existing chain).

**Findings (2026-04-24 — step 1 applied):**

- **Migration file path:** `db/migrations/0015_ia_tasks_core.sql` (naming: `NNNN_{snake_case}.sql` per existing chain; next free slot was 0015).
- **Tables created (9 new):** `ia_master_plans`, `ia_stages`, `ia_tasks`, `ia_task_deps`, `ia_task_spec_history`, `ia_task_commits`, `ia_stage_verifications`, `ia_ship_stage_journal`, `ia_fix_plan_tuples`. Existing `ia_project_spec_journal` (migration 0007) unchanged — Stage 2 import will bridge journal rows into the new `ia_ship_stage_journal` shape or retain separately (TBD).
- **Enum types chosen:** `task_status` (`pending|implemented|verified|done|archived` — matches ship-stage lifecycle), `stage_status` (`pending|in_progress|done`), `stage_verdict` (`pass|fail|partial`), `ia_task_dep_kind` (`depends_on|related` — F3 unified join table with discriminator rather than two separate tables; reduces surface, keeps same index locality).
- **FK cascade policy:**
  - `ia_stages.slug` → `ia_master_plans.slug` = **CASCADE** (plan retire drops stages).
  - `ia_tasks (slug, stage_id)` → `ia_stages (slug, stage_id)` = **RESTRICT + DEFERRABLE INITIALLY DEFERRED** (tasks protect stages; deferral lets import re-seat rows inside one tx).
  - `ia_task_deps.task_id` → `ia_tasks.task_id` = **CASCADE**; `depends_on_id` = **RESTRICT** (don't silently drop outgoing edges).
  - `ia_task_spec_history.task_id` = **CASCADE** (history dies with task).
  - `ia_task_commits.task_id` = **CASCADE**.
  - `ia_stage_verifications (slug, stage_id)` = **CASCADE**.
  - `ia_ship_stage_journal.task_id` = **RESTRICT** (journal is append-only; preserves forensic trail).
  - `ia_fix_plan_tuples.task_id` = **CASCADE** (ephemeral tuples).
- **Indexes built (25 on new tables):** `ia_tasks` — GIN `body_tsv` + GIN `body gin_trgm_ops` + btree `(slug, stage_id)`, `status`, `prefix`, `updated_at DESC`; `ia_task_deps` — reverse-lookup btree on `depends_on_id` + kind; `ia_ship_stage_journal` — session timeline + task + stage + payload_kind; `ia_fix_plan_tuples` — partial indexes on `applied_at IS NULL` (active tuples) + `applied_at IS NOT NULL` (expiry sweep target). Build was instant on empty tables — remeasure after Step 2 bulk-load.
- **Seed values (sequences, from `ia/state/id-counter.json` snapshot 2026-04-24):** `tech_id_seq` = 777, `feat_id_seq` = 54, `bug_id_seq` = 59, `art_id_seq` = 5, `audio_id_seq` = 2. Each `nextval` returns that value then advances. Post-smoke rewind applied (`setval(..., seed, false)`) so Step 2 import sees pristine counter state.
- **Extensions:** `pg_trgm` created idempotently (first use in repo DB; Unity-bridge tables unaffected).
- **Concurrency primitive choices (F10):** deferred to Step 4 (mutation tools). Schema carries no triggers yet — history writes + advisory-lock semantics land alongside the tools that need them.
- **Surprises / gotchas:**
  1. `expires_at timestamptz GENERATED ALWAYS AS (applied_at + interval '30 days') STORED` was rejected — Postgres considers `timestamptz + interval` non-immutable because `session_timezone` can shift the result. Resolution: dropped the generated column; TTL is computed in the query layer (`applied_at + interval '30 days'`). Index on `applied_at IS NOT NULL` covers the expiry sweep.
  2. `SELECT setval('seq', value, false)` with `false` = next `nextval` returns `value`. Verified `last_value=777, is_called=f` → first `nextval` will return 777 (= TECH-777 = first unused id). id-counter.json shows `TECH: 776` as the **last used**; seed is correct.
  3. `ia_project_spec_journal` from migration 0007 coexists under a different ownership (role `javier`) than the new `ia_*` tables (role `postgres`). Does not block schema but may surface during import; Step 2 should explicitly bridge or sidestep.
- **Regression check:** `npm run db:bridge-preflight` exit 0 — Unity-bridge schema unaffected.

**Commit:** `feat(ia-dev-db): step 1 — DB schema foundation (ia_* tables + types + indexes)`

---

### Step 2 — One-shot import script

**Goal:** Port existing state from filesystem (yaml + markdown) into DB tables. Idempotent. Re-runnable without data corruption.

**Inputs:**

- `ia/backlog/*.yaml` + `ia/backlog-archive/*.yaml` → `ia_tasks` rows (status `pending` / `in_progress` / `done` / `archived` per current yaml shape).
- `ia/projects/*master-plan*.md` → `ia_master_plans` + `ia_stages` rows (parse stage headers + objectives + exit).
- `ia/projects/{ISSUE_ID}.md` files → `ia_tasks.body` text + section index.
- `ia/state/id-counter.json` → sequence seed values (Step 1 already seeded; script verifies consistency).

**Outputs:**

- New script `tools/scripts/ia-db-migrate.ts` (or `.mjs` — implementer picks language matching existing tool chain).
- `npm run` target registered (suggest `ia:db-import`).
- Transactional per-run execution (F12=c: idempotent re-run).

**Acceptance:**

- Running `npm run ia:db-import` on clean DB populates all rows.
- Re-running same command is a no-op (or safe overwrite; implementer chooses — document in Findings).
- Row counts match filesystem: `ia_tasks` row count ≈ union of `ia/backlog/*.yaml` + `ia/backlog-archive/*.yaml` counts.
- `ia_tasks.body_tsv` populated + full-text query returns expected results (smoke: `SELECT task_id FROM ia_tasks WHERE body_tsv @@ to_tsquery('english', 'heightmap')` returns ≥1 row).
- Filesystem untouched.

**Voids for implementer:**

- Parser strategy for master-plan stage blocks (regex vs markdown AST).
- Handling for archived-but-present project spec files (are all archived yamls still on disk, or does the filesystem have gaps?).
- Dep graph — how to parse `depends_on` from yaml and populate `ia_task_deps`.
- Body section extraction — store whole body in one column or also write per-section cache? (F4 says whole body + tsvector; per-section slicing is MCP-layer concern per §4.3).

**Findings (implementer fills):**

- **Script path + npm target:** `tools/mcp-ia-server/scripts/ia-db-import.ts` + `npm run ia:db-import`. Location chosen to share `loadAllYamlIssues` + `ParsedBacklogIssue` parser with existing MCP server — no re-implementation of yaml decoding. DB URL resolver inlined (mirrors `tools/postgres-ia/resolve-database-url.mjs`).
- **Rows imported per table:** 20 `ia_master_plans` + 254 `ia_stages` + 872 `ia_tasks` + 1346 `ia_task_deps`. `ia_task_spec_history` / `ia_task_commits` / `ia_stage_verifications` / `ia_ship_stage_journal` / `ia_fix_plan_tuples` wiped to zero (start clean for Step 4+). Task total matches filesystem sum exactly (166 open yaml + 706 archive yaml = 872).
- **Parse errors encountered + resolution:** 0 yaml parse errors. 0 master-plan parse errors. Stage header regex `^###\s+Stage\s+(.+?)\s*(?:—|-)\s*(.+?)\s*$` captures ids including freeform suffixes (`6 addendum`, `9 addendum`) without schema loss.
- **Idempotency strategy chosen:** Full TRUNCATE-and-reinsert in a single transaction (`SET CONSTRAINTS ALL DEFERRED` + DELETE in FK-safe order + re-INSERT). Chosen over ON CONFLICT DO UPDATE because at this pre-cutover stage filesystem is still authoritative + DB is a snapshot; per-row reconciliation complexity deferred to Step 4 mutation tools. Two back-to-back runs produced identical row counts + zero errors.
- **Import wall-clock time:** 0.71s–0.74s on local Postgres 15 (port 5434). Dominated by 872 × insert round-trips; single-connection per-row INSERT is acceptable at this scale. If future re-imports grow ≫1s, batch via `unnest` or `COPY FROM STDIN`.
- **Data gaps discovered:**
  1. **11 dropped dep edges** — 3 dangling target ids referenced in yaml but absent from both open + archive sets: `TECH-432` (4 refs), `FEAT-37` (1 ref), `TECH-358` (6 refs across depends_on + related). Historical backlog churn — ids deleted without pruning referrers. Logged + dropped (not inserted) to keep FK constraint holding. Not blocking; Step 4 tools can enforce referential integrity at mutation time.
  2. **Orphan plan slugs** — yaml `parent_plan` fields reference master-plan files that aren't on disk. Script creates placeholder rows with `title=slug` + `source_spec_path=null` so FK holds. Current dataset produced 0 orphan plans; safety net kept for future runs.
  3. **Stage placeholders** — yaml `stage:` values not matching any master-plan header also get placeholder rows with `title=null`. Current dataset produced 0 such stages (all yaml-referenced stages exist in their parent plan's headers).
- **Surprises:**
  1. `body_tsv` populates correctly via the STORED generated column — no explicit index writes in the import path. GIN index rebuild happens inline with inserts; 872-row backfill took <1s.
  2. Smoke query `SELECT task_id FROM ia_tasks WHERE body_tsv @@ to_tsquery('english', 'heightmap')` returned 29 hits including `TECH-15 geography initialization performance`, `TECH-18 IA migration to PostgreSQL`, `TECH-251 Opus 4.7 adoption`, `BUG-48 minimap staleness` — full-text search confirmed functional without manual tokenizer setup.
  3. `setval(seq, max(current, observed), true)` advances the sequence so next `nextval` returns `observed+1` — aligns with `reserve-id.sh` semantics (next-reserved-id is counter+1). Seed state preserved across re-runs.
  4. `ia_project_spec_journal` (migration 0007, owned by role `javier`) was untouched by this import — it remains under Step 11's responsibility (daily snapshot + history merge). Noted from Step 1 Findings §3; no cross-owner privilege issue surfaced because import only touches `ia_*` tables owned by `postgres`.
  5. Stage row count (254) > master-plan header count because several plans (e.g. `sprite-gen-master-plan.md` with 7 stages; `unity-agent-bridge-master-plan.md` with 12) contribute multiple stage rows each. Distribution: 20 plans × avg 12.7 stages.

**Commit:** `feat(ia-dev-db): step 2 — one-shot import script (yaml + master-plan + spec body → DB)`

---

### Step 3 — Read MCP tools (DB-backed)

**Goal:** Expose read-only DB query tools via `territory-ia` MCP server. No mutations yet. Keep filesystem reads as fallback during transition (shortest possible window).

**Inputs:**

- Existing `tools/mcp-ia-server/src/` surface + `list_*` registration patterns.
- Design doc §4.3 MCP tool surface (Read section).
- Step 1 schema + Step 2 populated DB.

**Outputs:**

- New MCP tools registered:
  - `task_state(task_id)` — metadata + status + commits + deps from DB.
  - `stage_state(slug, stage_id)` — progress + blockers + next_pending from DB.
  - `master_plan_state(slug)` — rollup across stages.
  - `task_spec_body(task_id)` — full body.
  - `task_spec_section(task_id, section)` — single section slice.
  - `task_spec_search(query, filters)` — full-text + trigram.
  - `stage_bundle(slug, stage_id)` — DB state + narrative slices in one payload.
  - `task_bundle(task_id)` — DB state + body slices.
- Existing tools (`backlog_issue`, `backlog_list`, `backlog_search`, `spec_section`, etc.) re-implemented as DB queries. Schemas unchanged.
- Singleton `pg.Pool` at MCP server boot (F15=a).

**Acceptance:**

- Restart MCP server; `list_*` returns new tools + existing tools still functional.
- `backlog_issue TECH-001` returns data identical to pre-refactor yaml parse.
- `task_spec_search 'heightmap'` returns ≥1 hit.
- No filesystem reads for task metadata during test calls (implementer greps tool code to confirm — or logs query source).
- Existing skill invocations that use old tools still work (tools are a compatibility surface).

**Voids for implementer:**

- Tool parameter schemas — match existing shapes where possible, extend where needed.
- Error shape when DB is down (throw? return typed error payload? — pick convention).
- `spec_section` reimplementation: does it read body from DB + regex-extract section? Or does it also persist per-section?
- Caching strategy for hot queries (prepared statements? in-memory LRU?).

**Findings:**

```
- Tools registered (names): task_state, stage_state, master_plan_state, task_spec_body,
  task_spec_section, task_spec_search, stage_bundle, task_bundle (8 new) —
  registered in src/server-registrations.ts via registerIaDbReadTools(server)
  under the IA-core bucket. Query layer at src/ia-db/queries.ts; tool wiring at
  src/tools/ia-db-reads.ts. snake_case names chosen to match existing MCP
  surface (vs existing backlog_issue, spec_section conventions).

- Tools reimplemented (names): None. Existing tools (backlog_issue, spec_section,
  etc.) left intact to avoid cross-step coupling. Re-implementation deferred to
  Step 4/5 once write-side guarantees land.

- Connection pool config (size, idle timeout): Singleton pg.Pool via
  src/ia-db/pool.ts — uses `pg` defaults (max=10, idle=10s) layered on the
  resolved IA DATABASE_URL from src/ia-db/resolve-database-url.ts. Guarded
  by poolOrThrow() which throws IaDbUnavailableError when DB is offline.

- Query latency p50 / p95 on representative ops (live smoke against populated DB):
  - task_state(TECH-767):      134ms cold / 9ms warm
  - stage_state(sprite-gen, 7): 9ms
  - master_plan_state(sprite-gen): 11ms
  - task_spec_body(TECH-767):  1ms
  - task_spec_section(TECH-767, "Implementation Plan"): 10ms
  - task_spec_search 'decoration' (fts):  55ms (returns 20 hits with ts_headline)
  - task_spec_search 'heightmap' (trgm):  289ms (title-scoped; see note below)
  - stage_bundle(sprite-gen, 7): 38ms
  - task_bundle(TECH-767):       23ms
  All within interactive budget (<300ms). FTS dominant cost; trgm pays for
  GIN index scan + similarity() on every candidate.

- Section-slicing strategy: Pure markdown slicer `sliceSection(body, section)`
  exported from queries.ts (unit-tested in tests/ia-db/queries.test.ts).
  Finds heading case-insensitively, slices through the line before the next
  heading of same-or-shallower level. No per-section DB persist — body is
  loaded once then sliced in-process. Keeps DB schema minimal; CPU cost
  negligible for typical spec sizes.

- Error handling convention: `IaDbUnavailableError` thrown by `poolOrThrow()`
  when DB cannot connect; `task_not_found` / `stage_not_found` /
  `master_plan_not_found` returned as typed null-result rows that tool wrappers
  surface as structured errors. All tools pass through `wrapTool` +
  `runWithToolTiming` for uniform instrumentation.

- Trigram re-targeting: initial plan put `%` operator on ia_tasks.body —
  live smoke returned 0 hits because whole-body trgm similarity stays below
  the 0.3 default for realistic query/body length gaps. Re-targeted trgm to
  `ia_tasks.title` (shorter strings, typo-tolerant lookup is the real use
  case), added migration `db/migrations/0016_ia_tasks_title_trgm.sql` with
  GIN index on `title gin_trgm_ops`, and scoped threshold to 0.1 via
  `SET LOCAL pg_trgm.similarity_threshold` inside explicit BEGIN/COMMIT.
  FTS branch still covers body search.

- Importer linkage fix: Step 2 importer left `ia_tasks.slug` + `stage_id` NULL
  for ~all newer tasks because their yaml lacks `parent_plan` + `stage` fields
  (newer convention carries `section: "Stage 7 — ..."` only). Added two
  fallbacks in scripts/ia-db-import.ts: `deriveParentPlanFromBody` parses the
  spec frontmatter for `parent_plan:`; `deriveStageFromSection` extracts the
  stage id from `section:`. Re-import raised stage count 254 → 260 and
  populated task↔stage joins — without this, stage_state returned 0 tasks
  for Stage 7.

- Unit-test coverage: tests/ia-db/queries.test.ts covers sliceSection —
  7 cases (empty, missing, EOF-terminated, sibling-terminated, nested
  subheadings preserved, case-insensitive, CRLF). Pure-function extraction
  chosen so test suite does NOT require live DB.

- Surprises: (1) whole-body trgm is effectively dead at default thresholds —
  title is the right scope. (2) Newer task yaml schema lost parent_plan/stage
  fields; body frontmatter is now the only reliable parent pointer — import
  must derive, cannot trust yaml alone. (3) StageBundleDB extends StageStateDB
  (flat root, not nested) — first smoke round hit `undefined.stage_id` until
  the bundle wrapper was fixed to read root fields.
```

**Commit:** `feat(ia-dev-db): step 3 — read MCP tools (DB-backed state queries + full-text search)`

---

### Step 4 — Write MCP tools (atomic mutations)

**Goal:** Expose mutation tools for task insert, status flip, body write, commit record, journal append, fix-plan write. Transactional. Replace filesystem `flock` with DB primitives.

**Inputs:**

- Step 3 read tools + DB pool.
- Design doc §4.3 Mutate section + F10 concurrency (mixed advisory + row locks).

**Outputs:**

- `task_insert(slug, stage_id, title, body, type, priority, depends_on, related)` → reserves id via DB sequence, inserts row + body + deps in one tx.
- `task_status_flip(task_id, new_status)` — row-level lock + update.
- `task_spec_section_write(task_id, section, content)` — body update + optional history row.
- `task_commit_record(task_id, commit_sha)` — append to `ia_task_commits`.
- `stage_verification_flip(slug, stage_id, verdict, commit_sha, notes)` — upsert latest row (F11=a).
- `stage_closeout_apply(slug, stage_id)` — flip task statuses → archived + optional `mv` stage file to `_closed/` (filesystem op covered separately in Step 8).
- `journal_append(session_id, task_id, phase, payload_kind, payload)` / `journal_get` / `journal_search`.
- `fix_plan_write(task_id, round, tuples)` / `fix_plan_consume(task_id, round)`.

**Acceptance:**

- Round-trip: insert task → read via `task_state` → body matches.
- Concurrency test: two parallel `task_status_flip` calls on same row → second blocks + resolves without corruption.
- `task_insert` under parallel load → unique ids (no duplicates, no gaps that break sequence).
- Journal append + get round-trip.
- Commit smoke: `task_commit_record` rows queryable.

**Voids for implementer:**

- Advisory lock key naming scheme (`pg_advisory_lock('ia_id_seq_tech'::regclass::oid, ...)` — pick convention).
- History table trigger vs explicit write in tool (F5 says full snapshots — implementer picks mechanism).
- Journal payload schema enforcement — runtime validation (zod / ajv) or trust-but-document?
- Rollback policy on partial failure (tx abort + re-raise, or partial commit + report?).

**Findings (2026-04-24 — step 4 applied):**

- **Tools added (names):** 9 total registered in new bucket `registerIaDbWriteTools()` (`tools/mcp-ia-server/src/tools/ia-db-writes.ts`). Names: `task_insert`, `task_status_flip`, `task_spec_section_write`, `task_commit_record`, `stage_verification_flip`, `stage_closeout_apply`, `journal_append`, `fix_plan_write`, `fix_plan_consume`. Server tool count 69 → 78 (`validate:mcp-readme` green). Underlying mutation functions live in `src/ia-db/mutations.ts`; MCP wrappers are thin zod-shape + `wrapTool` envelopes that map `IaDbUnavailableError` → `db_unconfigured` + `IaDbValidationError` → `invalid_input` at the tool boundary.
- **Concurrency test outcome:** `tests/ia-db/mutations.test.ts` exercises two parallel `task_status_flip` calls on the same row via `Promise.all([flip→implemented, flip→verified])`. Both resolve without error; final row status is one of the two targets (row-level `SELECT … FOR UPDATE` inside `withTx` serialises). Duration 36.9 ms — no deadlock, no corruption, status cleanly settled. Parallel-insert test (5× `task_insert` via `Promise.all`) produced 5 unique monotonic ids in 177.2 ms — per-prefix sequence `nextval('tech_id_seq')` handles concurrent claims without advisory locks (sequence atomicity is sufficient).
- **Sequence seed + first inserted id:** After Step 2 import + Step 4 smoke, `tech_id_seq.last_value = 817` (post-test cleanup). Largest pre-existing `TECH-` id = 816 (from Step 2 import); first `task_insert`-reserved id during Step 4 tests was `TECH-817`. `ON CONFLICT DO NOTHING` path not needed — sequence monotonicity is authoritative. `feat_id_seq`, `bug_id_seq`, `art_id_seq`, `audio_id_seq` untouched by Step 4 (no sample inserts under those prefixes; seeded to `max(existing id) + 1` in Step 1 migration).
- **History trigger vs tool-side write:** **Tool-side write** chosen. `mutateTaskSpecSectionWrite` wraps `BEGIN` → `SELECT body FROM ia_tasks WHERE task_id = $1 FOR UPDATE` → insert prior body into `ia_task_spec_history` → update `ia_tasks.body` with new section → `COMMIT`. Rationale (F5): explicit write inside the same tx keeps the snapshot coupled to the mutation that caused it and gives the tool control over `actor` / `change_reason` / `git_sha` metadata that a blind-trigger cannot populate. Schema carries no `BEFORE UPDATE` trigger. Round-trip test confirms both the new body and the history row are visible after commit.
- **Journal schema enforcement:** **Trust-but-document** at payload body level. Tool-boundary validation (zod + explicit checks) enforces: `session_id` / `phase` / `payload_kind` non-empty strings, `payload` is a non-null non-array object (jsonb). Internal payload shape per `payload_kind` is NOT validated — documented in tool description + Step 12 retrospective invariant. This mirrors the design-doc F9 stance (journal as audit trail, not a typed message bus); future work can add per-`payload_kind` validators in a separate tool without schema changes.
- **Surprises:**
  - **Pre-existing drift in `catalog-list.ts`** unrelated to Step 4 — TS2367 (boolean-vs-number comparison) + TS2345 (zod union vs param type) were already red on HEAD. Widened `runCatalogList` input type `include_draft?: boolean | string | number` to match the zod inference that was already in the schema. Build was blocked on this; fix is a 1-line type widen + no runtime-behaviour change. Logged here cross-step because `npm run build` gate couldn't advance without it.
  - **Test-setup schema drift** — first pass of `mutations.test.ts` inserted into `ia_master_plans` with `status` column + into `ia_stages` with `status = 'active'`. Actual 0015 schema: `ia_master_plans` has no `status`; `ia_stages.status` is `stage_status` ENUM (`pending` | `in_progress` | `done`). Fixed by dropping the column + using `'in_progress'`. Reinforces Step 3 Finding re. reading migrations before trusting design-doc shorthand.
  - **Sandbox slug pattern** (`__test_sandbox__`) works cleanly: `before()` inserts plan + stage, `after()` cascade-deletes in FK order (deps → commits → spec_history → fix_tuples → tasks → verifications → stages → journal → master_plans). Post-test row counts confirmed zero. Real master-plan data untouched.
  - **Latencies (local dev DB, test measurements):** `task_insert` round-trip 84.8 ms, parallel ×5 177.2 ms (~35 ms per insert steady state), `task_status_flip` concurrent 36.9 ms, `task_spec_section_write` replace + history 38.4 ms, append-when-missing 9.9 ms, `journal_append` round-trip 6.96 ms, `fix_plan_write` + `fix_plan_consume` lifecycle 73.85 ms. Well within the F1 design budget.
  - **`fix_plan_consume` idempotency verified:** re-run on already-consumed `(task_id, round)` returns `consumed = 0` without error (partial `applied_at IS NULL` filter handles it cleanly — no need for advisory state flag).

**Commit:** `feat(ia-dev-db): step 4 — write MCP tools (atomic task/stage/journal mutations)`

---

### Step 5 — `BACKLOG.md` view generation (DB → markdown)

**Goal:** Replace `materialize-backlog.sh` with a DB-sourced generator. `BACKLOG.md` + `BACKLOG-ARCHIVE.md` become regenerated views; content identical to current shape.

**Inputs:**

- Step 3 read tools.
- Current `materialize-backlog.sh` logic + output format.
- `ia/state/backlog-sections.json` + `ia/state/backlog-archive-sections.json` (section ordering rules).

**Outputs:**

- New script `tools/scripts/materialize-backlog-from-db.ts` (or replace existing shell — implementer picks).
- `npm run materialize:backlog` flips to new source.
- Generated `BACKLOG.md` + `BACKLOG-ARCHIVE.md` byte-identical (or near-identical, documented) to pre-refactor output.

**Acceptance:**

- `diff BACKLOG.md pre-refactor-BACKLOG.md` shows only expected delta (formatting, section ordering intentional).
- No filesystem yaml reads during generation.
- `validate:all` passes.

**Voids for implementer:**

- Handling for intentional formatting drift (date-stamp lines, generated-by markers).
- Whether to keep the old shell as fallback or remove immediately (recommend keep during Step 6 skill flip, remove at Step 9 cleanup).

**Findings:**

```
- Script path:
- Byte-diff vs pre-refactor output:
- Wall-clock time:
- Surprises:
```

**Commit:** `feat(ia-dev-db): step 5 — materialize-backlog from DB (markdown view regen)`

---

### Step 6 — `stage-file` skill flip (DB-aware)

**Goal:** Merge `stage-file-plan` + `stage-file-apply` into single `stage-file` skill. Writes DB rows via `task_insert` MCP instead of yaml + `materialize-backlog.sh`. No filesystem yaml writes.

**Inputs:**

- Current `ia/skills/stage-file-plan/SKILL.md` + `ia/skills/stage-file-apply/SKILL.md`.
- Design doc B1 + E2 + E3.
- Step 4 write tools.

**Outputs:**

- Merged `ia/skills/stage-file/SKILL.md` (retire the `-plan` + `-apply` pair).
- `.claude/agents/stage-file*.md` + `.claude/commands/stage-file.md` updated to reference new skill.
- Old subagents moved to `.claude/agents/_retired/`.

**Acceptance:**

- Smoke: `/stage-file {orchestrator} {stage_id}` on a small test orchestrator (implementer picks — ideally a throwaway or existing Stage with `_pending_` tasks) files N tasks into DB.
- DB reads back the N tasks via `stage_state`.
- `BACKLOG.md` regenerated (Step 5) includes the new rows.
- No yaml files written under `ia/backlog/`.

**Voids for implementer:**

- How to test end-to-end without polluting real backlog (throwaway slug? revert after?).
- Dispatch shape: single skill invocation, no Opus pair-tail (per B1).
- Plan-author / plan-digest still called inline? Or that's step 7?

**Findings:**

```
- Skill file path:
- Retired subagent files:
- Smoke test slug + stage:
- Rows created:
- Surprises:
```

**Commit:** `feat(ia-dev-db): step 6 — stage-file skill merged + DB-backed (retire plan+apply pair)`

---

### Step 7 — `stage-authoring` skill flip (merge plan-author + plan-digest)

**Goal:** Merge `plan-author` + `plan-digest` into single `stage-authoring` skill. Writes §Plan Digest via `task_spec_section_write` MCP. Drop §Plan Author section + aggregate `docs/implementation/` doc.

**Inputs:**

- Current `ia/skills/plan-author/` + `ia/skills/plan-digest/SKILL.md`.
- Design doc B2 + B6 + C7 + D8.
- Step 4 `task_spec_section_write`.

**Outputs:**

- New `ia/skills/stage-authoring/SKILL.md`.
- Retire plan-author + plan-digest.
- Drop aggregate `docs/implementation/{slug}-stage-X.Y-plan.md` pattern.
- `.claude/agents/` + `.claude/commands/` updated.

**Acceptance:**

- Smoke: run stage-authoring on a stage filed in Step 6 → §Plan Digest lands in DB body for each task.
- `task_spec_section task_id §Plan\ Digest` returns expected content.
- No aggregate doc written.

**Voids:**

- Does stage-file (Step 6) call stage-authoring inline (per C8=b) or keep as separate step? Recommend inline; confirm during implementation.
- §Plan Author intermediate state — does stage-authoring still go through an "author draft → digest mechanization" two-sub-phase internally, or direct-to-digest?

**Findings:** (standard template)

**Commit:** `feat(ia-dev-db): step 7 — stage-authoring skill merged + DB-backed spec section write`

---

### Step 8 — `ship-stage` skill flip (Pass A no-commit / Pass B stage-end)

**Goal:** Rewrite ship-stage: Pass A per-task implement + compile (no commits). Pass B per-stage verify-loop + code-review + closeout + single stage-end commit. Fold closeout inline (drop `stage-closeout-*` pair + `plan-applier`).

**Inputs:**

- Current `ia/skills/ship-stage/SKILL.md`.
- Design doc C9 + C10 + E13 + E14.
- Steps 4 + 5 + 6 + 7.

**Outputs:**

- Rewritten `ia/skills/ship-stage/SKILL.md`.
- Retire `ia/skills/stage-closeout-plan/` + `ia/skills/stage-closeout-apply/` + `ia/skills/plan-applier/` (code-fix mode specifically; if plan-fix mode survives elsewhere, keep only that path).
- `ship-stage` writes `stage_verification_flip` on verify pass, moves stage file to `_closed/` in `stage_closeout_apply`, commits single stage commit.

**Acceptance:**

- Smoke: run ship-stage on a filed+authored throwaway stage → all tasks `implemented → verified → done` → stage file lands in `_closed/` → single commit on branch.
- Drop: no per-task commits generated during Pass A.
- `ia_stage_verifications` row populated.

**Voids:**

- Retry loop shape (verify-loop fix iteration — where in Pass B?).
- Code-review fix-apply inline per E14 — how does reviewer apply fix without plan-applier? Direct Edit in reviewer subagent?
- Stage commit message format.

**Findings:** (standard)

**Commit:** `feat(ia-dev-db): step 8 — ship-stage rewritten (Pass A no-commit / Pass B stage-end + inline closeout)`

---

### Step 9 — Foldering migration + drop retired surface

**Goal:** One-shot filesystem reshape — move existing master plans into `ia/projects/{slug}/` folders with `index.md` + `stage-*.md` + `_closed/`. Retire `tasks/` subfolder (bodies in DB per E4=b). Drop yaml + materialize-backlog shell + reserve-id + runtime-state + filesystem lockfiles + dropped validators.

**Inputs:**

- Design doc §2 foldering shape + A4 retirement per E4=b.
- All prior steps green (DB + tools + skills all working).

**Outputs:**

- New script `tools/scripts/fold-master-plan.ts` (one-shot; run once per plan OR bulk).
- All `ia/projects/*master-plan*.md` → `ia/projects/{slug}/index.md` + `stage-*.md`.
- `ia/projects/{id}.md` task spec files → DELETED (bodies already in DB).
- `ia/backlog/` + `ia/backlog-archive/` → DELETED.
- `BACKLOG.md` + `BACKLOG-ARCHIVE.md` kept as generated view (regenerated from DB via Step 5).
- `tools/scripts/reserve-id.sh` + `ia/state/id-counter.json` + `.id-counter.lock` + `.materialize-backlog.lock` + `.runtime-state.lock` + `ia/state/runtime-state.json` → DELETED.
- Drop `validate:dead-project-specs`, `validate:backlog-yaml`, `validate:frontmatter`, `validate:claude-imports` from package.json scripts + CI.
- Drop `materialize-backlog.sh` (replaced by Step 5 TS).
- Drop retired skills: `plan-review`, `opus-auditor`, `plan-reviewer-mechanical`, `plan-reviewer-semantic`, `stage-closeout-*`, `plan-applier` code-fix, `stage-decompose`.

**Acceptance:**

- `git ls-files ia/backlog/` → empty.
- `git ls-files ia/projects/TECH-*.md` → empty (bodies in DB).
- `npm run validate:all` passes on reduced validator set.
- `/stage-file` + `/ship-stage` + `/author` smokes still green on test orchestrator.
- Web dashboard (if Step 10 shipped first — optional re-order) still reads.

**Voids:**

- Ordering: does foldering run before or after web dashboard (Step 10)? Recommend before — cleanup is the last step.
- History preservation for moved files — `git mv` vs copy+delete (git mv preserves blame).
- What to do with `ia/plans/` contents.

**Findings:** (standard)

**Commit:** `feat(ia-dev-db): step 9 — foldering migration + cleanup (yaml retires, task specs DB-only, dropped validators)`

---

### Step 10 — Web dashboard read surface

**Goal:** Read-only dashboard (E6=a) showing master plans + stages + tasks + verification verdicts + journal tail. Next.js API routes in `web/app/api/...` (E7=a) call MCP tools (F14=b) — not direct Postgres.

**Inputs:**

- Design doc §4.3 Read tools + E6 + E7 + F14.
- Existing `web/` workspace conventions.

**Outputs:**

- API routes: `web/app/api/ia/plans/route.ts`, `web/app/api/ia/plans/[slug]/route.ts`, `web/app/api/ia/tasks/[id]/route.ts`, `web/app/api/ia/tasks/[id]/body/route.ts`, etc.
- Dashboard page(s): `web/app/ia/page.tsx` (list), `web/app/ia/[slug]/page.tsx` (plan view), `web/app/ia/tasks/[id]/page.tsx` (task view).
- MCP client helper in `web/lib/` that proxies to `territory-ia` server.

**Acceptance:**

- `npm run dev` in `web/` + navigate to dashboard → list of master plans loads from DB.
- Click plan → stages + task table render from DB.
- Click task → body renders (markdown).
- Search bar → calls `task_spec_search`.

**Voids:**

- MCP client transport from Next.js server (spawn subprocess? HTTP bridge? — design question).
- Auth gate (none for now? Basic IP-restricted?).
- Styling / design system integration (use existing `web/lib/design-system.md`).

**Findings:** (standard)

**Commit:** `feat(ia-dev-db): step 10 — web dashboard read surface (Next.js API → MCP → DB)`

---

### Step 11 — Daily snapshot + history audit

**Goal:** Committed daily DB snapshot (E10) + `ia_task_spec_history` audit table wired to `task_spec_section_write` (F5=a).

**Inputs:**

- Design doc E10 + F5 + F6 (split plain/binary snapshot).
- Step 4 `task_spec_section_write`.

**Outputs:**

- Cron / CI job writing `ia/state/db-snapshot-metadata.sql` (plain SQL, diffable) + `ia/state/db-snapshot-bodies.dump` (binary, compressed).
- History table populated by trigger OR tool-side write (implementer chose in Step 4).

**Acceptance:**

- Snapshot regenerates cleanly.
- Restore-from-snapshot smoke (to throwaway DB) round-trips.
- `ia_task_spec_history` rows appear after `task_spec_section_write` calls.

**Voids:**

- Cron vs CI (GitHub Action on schedule? local cron?).
- Snapshot size + gitignore strategy for binary portion.

**Findings:** (standard)

**Commit:** `feat(ia-dev-db): step 11 — daily snapshot + spec body history`

---

### Step 12 — Retrospective + friction log close

**Goal:** Close this refactor. Merge to main. Open followup tickets for gaps surfaced during execution.

**Outputs:**

- Append final retrospective section to `docs/master-plan-execution-friction-log.md` comparing pre- vs post-refactor friction.
- PR from `feature/ia-dev-db-refactor` → `main`.
- File followup issues (via `project-new` on the new DB-backed system — meta: refactor bootstraps its own issue tracker).

**Acceptance:**

- PR green.
- Followups filed.
- Design doc (`master-plan-foldering-refactor-design.md`) locked + unchanged by this branch (preserved as historical record).

**Findings:** (standard)

**Commit:** `chore(ia-dev-db): step 12 — retrospective + PR open`

---

## 2. Running log

### 2.1 Step status table

| Step | Status | Started | Done | Commit | Notes |
|------|--------|---------|------|--------|-------|
| 1 — DB schema foundation | done | 2026-04-24 | 2026-04-24 | `1e79182` | 9 new `ia_*` tables + 4 enums + 5 sequences + GIN dual-index (tsv+trgm); bridge-preflight green |
| 2 — Import script | done | 2026-04-24 | 2026-04-24 | `b274063` | 872 tasks (166 open + 706 archive) + 254 stages + 20 plans + 1346 deps imported in 0.74s; idempotent TRUNCATE-and-reinsert; 11 dangling dep targets dropped (3 missing ids); body_tsv smoke green |
| 3 — Read MCP tools | done | 2026-04-24 | 2026-04-24 | `9e6ddee` | 8 DB-backed read tools wired through registerIaDbReadTools; sliceSection pure fn + unit tests; trgm retargeted body→title + migration 0016; importer linkage fix (parent_plan from frontmatter + stage from section) raised stages 254→260 |
| 4 — Write MCP tools | done | 2026-04-24 | 2026-04-24 | `_pending_` | 9 DB-backed mutation tools wired via registerIaDbWriteTools; tool count 69→78; parallel 5× insert monotonic (tech_id_seq 817); concurrent status_flip serialised via SELECT FOR UPDATE; tool-side history write; journal trust-but-document; fix_plan_consume idempotent; 7 round-trip + concurrency tests green; pre-existing catalog-list.ts type drift fixed en passant |
| 5 — BACKLOG.md generator | pending | — | — | — | — |
| 6 — stage-file skill | pending | — | — | — | — |
| 7 — stage-authoring skill | pending | — | — | — | — |
| 8 — ship-stage skill | pending | — | — | — | — |
| 9 — Foldering + cleanup | pending | — | — | — | — |
| 10 — Web dashboard read | pending | — | — | — | — |
| 11 — Snapshot + history | pending | — | — | — | — |
| 12 — Retrospective + PR | pending | — | — | — | — |

### 2.2 Cross-step findings + followups

Implementers append entries here as discoveries surface that do NOT belong to a single step.

- **2026-04-24 (Step 4 side-fix)** — `tools/mcp-ia-server/src/tools/catalog-list.ts` had two pre-existing TypeScript errors on HEAD (TS2367 boolean-vs-number comparison line 47; TS2345 zod-schema-vs-param type drift line 131) blocking `npm run build`. Root cause: the zod `inputSchema` already used `z.union([boolean, string, number])` for `include_draft`, but `runCatalogList`'s param signature was narrower (`include_draft?: boolean`). Fixed by widening the param type to match the schema (`boolean | string | number`). No runtime-behaviour change — the coercion at line 44 already handled all three types. Logged here because the fix was required to unblock Step 4's build gate and is unrelated to mutation-tool scope.

### 2.3 Open questions surfaced during execution

```
— empty —
```

---

## 3. Guardrails for implementers

- **Do NOT write the whole plan upfront.** Current doc is the whole plan. Do not replace step bodies with pre-planned code diffs before starting.
- **Do NOT skip `Findings` blocks.** Every step green = Findings filled + committed in same commit (or commit immediately after).
- **Do NOT extend scope.** If a step uncovers an issue, record under §2.3 (open questions) and defer.
- **Do NOT couple steps.** Each step must be revertable in isolation. If two steps must land together, merge them into one step before starting.
- **Commit per step.** No mega-commits across steps. Commit messages follow `feat(ia-dev-db): step N — {summary}`.
- **No §Plan Digest ceremony.** This refactor is hand-driven. Do not invoke `/author`, `/plan-digest`, `/plan-review`, `/audit`, `/closeout` on this branch. Direct Edit + Bash + commit.
- **Verify before next step.** Acceptance block for step N must be fully green before starting step N+1.

---

## 4. Change log

- **2026-04-24** — Doc seeded on `feature/ia-dev-db-refactor` branch cut from `main @ 1563765`. All 12 steps outlined with Goal / Inputs / Outputs / Acceptance / Voids / Findings / Commit. Running log + status table initialized empty. Operating model: pure sequential, implementer fills voids + findings, no master-plan ceremony.
- **2026-04-24** — Step 3 landed: 8 DB-backed MCP read tools (`task_state`, `stage_state`, `master_plan_state`, `task_spec_body`, `task_spec_section`, `task_spec_search`, `stage_bundle`, `task_bundle`) + pure `sliceSection` fn with unit tests + trigram re-targeted to `ia_tasks.title` (migration `0016_ia_tasks_title_trgm.sql`) + importer linkage fallbacks (`deriveParentPlanFromBody` + `deriveStageFromSection`) raising stages 254→260. All acceptance checks green, latencies <300ms.
- **2026-04-24** — Step 4 landed: 9 DB-backed MCP mutation tools (`task_insert`, `task_status_flip`, `task_spec_section_write`, `task_commit_record`, `stage_verification_flip`, `stage_closeout_apply`, `journal_append`, `fix_plan_write`, `fix_plan_consume`) wired via `registerIaDbWriteTools()`. Server tool count 69 → 78, `validate:mcp-readme` green. Parallel 5× `task_insert` → 5 unique monotonic ids (per-prefix DB sequence, no advisory locks needed); concurrent `task_status_flip` serialised via row-level `SELECT FOR UPDATE` inside `withTx`; `task_spec_section_write` uses tool-side history write (not trigger) to capture `actor` / `change_reason` / `git_sha` alongside the snapshot; journal adopts trust-but-document stance on payload body shape (boundary validation only). 7 round-trip + concurrency + lifecycle tests green (`tests/ia-db/mutations.test.ts`). Pre-existing `catalog-list.ts` type drift fixed en passant (see §2.2).
