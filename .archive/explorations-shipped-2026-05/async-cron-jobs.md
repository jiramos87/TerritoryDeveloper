---
slug: async-cron-jobs
target_version: 1
parent_plan_id: null
notes: "Move every protocol write (audit logs, index regen, cache warming, drift-lint, glossary backlinks, change-log appends, status flips, view refreshes) onto fire-and-forget cron-driven job tables. One dedicated table per job kind (no shared discriminator). Each kind has its own crontab.guru-style schedule + handler script. Agent enqueues + continues; never awaits handler. Reuse claims-sweep cron precedent."
stages: []
tasks:
  - prefix: TECH
    depends_on: []
    digest_outline: "Placeholder — stages not yet authored."
    touched_paths: []
    kind: code
---

# Async cron jobs — exploration

Caveman-tech default per `ia/rules/agent-output-caveman.md`.

---

## 1. Problem

Ship protocol pipeline (`design-explore → ship-plan → ship-cycle → ship-final`) burns wall-clock + tokens on writes the agent dispatches synchronously. Every `master_plan_change_log_append`, `journal_append`, `materialize-backlog.sh`, `generate:ia-indexes`, `task_commit_record`, `validate:drift-lint`, `glossary-backlink-enrich` round-trip costs 1–10 s + ~500–2k tokens of conversation noise.

Goal: agent dispatches a row insert + moves on. Cron worker + handler picks up the row out-of-band. Zero await on the agent side. One job kind = one table = one handler script = one cron expression.

---

## 2. Existing infra to reuse

| Surface | Status | Reuse |
|---|---|---|
| `claims-sweep-tick.mjs` | runs `* * * * *` via system cron | shape template for every new tick script |
| `docs/parallel-carcass-claims-sweep-ops.md` | linux + macOS cron / launchd snippets | clone shape per kind |
| `ia_section_claims` / `ia_stage_claims` | live (mig 0049/0052) | per-slug serialization for handlers that touch shared plan state |
| `claim_heartbeat_timeout_minutes` config row | live | reused for stale-row sweep across new tables |
| `journal_append` MCP precedent | live | shape for per-kind `*_enqueue` tools |

`job_queue` (mig 0027, kind-discriminated) **stays put** for render-run pipeline. New cron-job tables are separate per user direction.

---

## 3. One table per kind

Every job kind gets its own table. Schema fitted to that kind's payload — no `payload_json` blob shared across kinds.

### 3.1 Common columns (every table)

```sql
job_id            uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
status            text        NOT NULL DEFAULT 'queued',
enqueued_at       timestamptz NOT NULL DEFAULT now(),
started_at        timestamptz NULL,
finished_at       timestamptz NULL,
heartbeat_at      timestamptz NULL,
error             text        NULL,
attempts          int         NOT NULL DEFAULT 0,
idempotency_key   text        NULL,
-- kind-specific columns below
CONSTRAINT {table}_status_check CHECK (status IN ('queued','running','done','failed'))
```

Plus per-table:
- `CREATE INDEX {table}_claim_idx ON {table} (status, enqueued_at)` — FIFO claim path.
- `CREATE UNIQUE INDEX {table}_idem_idx ON {table} (idempotency_key) WHERE idempotency_key IS NOT NULL` — dedupe.

### 3.2 Per-kind schema sketch

| Table | Cron schedule | Kind-specific columns |
|---|---|---|
| `cron_audit_log_jobs` | `* * * * *` (every minute) | `slug text NOT NULL`, `version int NOT NULL`, `audit_kind text NOT NULL`, `body jsonb NOT NULL` |
| `cron_journal_append_jobs` | `* * * * *` | `session_id uuid`, `slug text`, `stage_id text`, `phase text`, `payload_kind text`, `payload jsonb` |
| `cron_task_commit_record_jobs` | `* * * * *` | `task_id uuid NOT NULL`, `commit_sha text NOT NULL`, `commit_kind text NOT NULL`, `slug text`, `stage_id text` |
| `cron_stage_verification_flip_jobs` | `* * * * *` | `slug text`, `stage_id text`, `verdict text`, `commit_sha text`, `actor text` |
| `cron_status_flip_jobs` | `* * * * *` | `entity_kind text` (`task`/`stage`/`plan`), `entity_id text`, `target_status text`, `actor text` |
| `cron_materialize_backlog_jobs` | `*/2 * * * *` | `triggered_by text` (skill or operator) |
| `cron_regen_indexes_jobs` | `*/5 * * * *` | `scope text` (`all` / `glossary` / `specs`) |
| `cron_glossary_backlinks_jobs` | `*/5 * * * *` | `slug text`, `plan_id uuid` |
| `cron_anchor_reindex_jobs` | `*/5 * * * *` | `paths text[]` (changed spec paths) |
| `cron_drift_lint_jobs` | `*/10 * * * *` | `commit_sha text`, `slug text` |
| `cron_health_rollup_jobs` | `*/15 * * * *` | (no extra cols — sweeps all plans) |
| `cron_cross_impact_scan_jobs` | `0 * * * *` (hourly) | `slug text` |
| `cron_cache_warm_jobs` | `*/5 * * * *` | `cache_kind text`, `key text`, `slug text` |
| `cron_matview_refresh_jobs` | `0 * * * *` | `matview_name text` |
| `cron_master_plan_bundle_apply_jobs` | `* * * * *` | `slug text`, `version int`, `bundle_jsonb jsonb` |
| `cron_stage_closeout_jobs` | `* * * * *` | `slug text`, `stage_id text` |
| `cron_red_stage_proof_finalize_jobs` | `*/30 * * * *` | `proof_id uuid` |

Cadence rule of thumb: hot critical-path inserts (audit, journal, status-flip, commit-record, bundle-apply, closeout) → `* * * * *`. Index/regen work → `*/5` or `*/10`. Heavy crawl jobs → hourly.

### 3.3 Migration set

One migration per table — narrow + revertable. Numbered after `0087`:
- `0088_cron_audit_log_jobs.sql`
- `0089_cron_journal_append_jobs.sql`
- `0090_cron_task_commit_record_jobs.sql`
- ... (one per kind)

Stage 1 ships only the 3–4 highest-traffic kinds; rest land in later stages.

---

## 4. Cron tick scripts (one per kind)

Every kind owns a runner under `tools/scripts/cron/{kind}-tick.mjs`. Mirrors `claims-sweep-tick.mjs` shape:

```js
// tools/scripts/cron/audit-log-tick.mjs
export async function runTick() {
  const claimed = await pg.query(`
    UPDATE cron_audit_log_jobs
       SET status='running', started_at=now(), heartbeat_at=now(), attempts=attempts+1
     WHERE job_id IN (
       SELECT job_id FROM cron_audit_log_jobs
        WHERE status='queued'
        ORDER BY enqueued_at
        FOR UPDATE SKIP LOCKED
        LIMIT 50
     )
     RETURNING job_id, slug, version, audit_kind, body
  `);
  for (const row of claimed.rows) {
    try {
      await handler.run(row);
      await pg.query(`UPDATE cron_audit_log_jobs SET status='done', finished_at=now() WHERE job_id=$1`, [row.job_id]);
    } catch (e) {
      await pg.query(`UPDATE cron_audit_log_jobs SET status='failed', error=$1, finished_at=now() WHERE job_id=$2`, [String(e), row.job_id]);
    }
  }
}
```

Handler logic for each kind lives at `tools/scripts/cron/handlers/{kind}.mjs` — single `runHandler(row)` export. File-tree convention; validator (`validate-cron-handler-coverage.mjs`) asserts every queue table has matching tick + handler files.

`npm run` aliases per kind:

```json
"cron:audit-log:tick": "node tools/scripts/cron/audit-log-tick.mjs",
"cron:journal-append:tick": "node tools/scripts/cron/journal-append-tick.mjs",
"cron:status-flip:tick": "node tools/scripts/cron/status-flip-tick.mjs",
... // one per kind
```

---

## 5. Cron activation

System `cron` (linux `/etc/cron.d/territory-jobs` or macOS launchd) carries the schedule. Per-kind crontab entries:

```cron
# /etc/cron.d/territory-jobs
* * * * *      <user> cd /repo && npm run cron:audit-log:tick           >> /var/log/cron/audit-log.log 2>&1
* * * * *      <user> cd /repo && npm run cron:journal-append:tick      >> /var/log/cron/journal-append.log 2>&1
* * * * *      <user> cd /repo && npm run cron:status-flip:tick         >> /var/log/cron/status-flip.log 2>&1
* * * * *      <user> cd /repo && npm run cron:task-commit-record:tick  >> /var/log/cron/task-commit-record.log 2>&1
* * * * *      <user> cd /repo && npm run cron:bundle-apply:tick        >> /var/log/cron/bundle-apply.log 2>&1
* * * * *      <user> cd /repo && npm run cron:stage-closeout:tick      >> /var/log/cron/stage-closeout.log 2>&1
*/2 * * * *    <user> cd /repo && npm run cron:materialize-backlog:tick >> /var/log/cron/materialize-backlog.log 2>&1
*/5 * * * *    <user> cd /repo && npm run cron:regen-indexes:tick       >> /var/log/cron/regen-indexes.log 2>&1
*/5 * * * *    <user> cd /repo && npm run cron:glossary-backlinks:tick  >> /var/log/cron/glossary-backlinks.log 2>&1
*/5 * * * *    <user> cd /repo && npm run cron:anchor-reindex:tick      >> /var/log/cron/anchor-reindex.log 2>&1
*/5 * * * *    <user> cd /repo && npm run cron:cache-warm:tick          >> /var/log/cron/cache-warm.log 2>&1
*/10 * * * *   <user> cd /repo && npm run cron:drift-lint:tick          >> /var/log/cron/drift-lint.log 2>&1
*/15 * * * *   <user> cd /repo && npm run cron:health-rollup:tick       >> /var/log/cron/health-rollup.log 2>&1
0 * * * *      <user> cd /repo && npm run cron:cross-impact-scan:tick   >> /var/log/cron/cross-impact-scan.log 2>&1
0 * * * *      <user> cd /repo && npm run cron:matview-refresh:tick     >> /var/log/cron/matview-refresh.log 2>&1
*/30 * * * *   <user> cd /repo && npm run cron:red-stage-proof:tick     >> /var/log/cron/red-stage-proof.log 2>&1
```

macOS launchd plists per-kind under `~/Library/LaunchAgents/dev.bacayo.cron-{kind}.plist` — `StartInterval` (60 / 120 / 300 / 600 / 900 / 1800 / 3600 seconds).

Ops doc clone: `docs/cron-jobs-ops.md` (one section per kind).

---

## 6. Enqueue contract — fire and forget

Every kind exposes one MCP tool: `cron_{kind}_enqueue(...)`. Single insert, returns `{job_id}`, no await on handler. Agent skill flow becomes:

**Before** (sync):
```
master_plan_change_log_append(slug, version, 'stage_closed', body)  // 1.5s round-trip
journal_append(payload_kind='phase_checkpoint', payload={...})       // 1.0s
materialize-backlog.sh                                                // 4.0s
master_plan_state(slug)  // sync read for next phase
```

**After** (fire-and-forget):
```
cron_audit_log_enqueue(slug, version, 'stage_closed', body)          // 50ms insert
cron_journal_append_enqueue(payload_kind='phase_checkpoint', ...)    // 50ms insert
cron_materialize_backlog_enqueue(triggered_by='ship-cycle')          // 50ms insert
master_plan_state(slug)  // direct read — agent moves on
```

Total ceremony round-trip drops 6+ s → ~150 ms.

Idempotency: every enqueue accepts optional `idempotency_key`. Skill computes `sha256(skill + slug + stage_id + phase + entity_id)` → re-enqueue returns existing `job_id` (partial unique index gates duplicates).

### 6.1 Reads stay direct (not queued)

Reads against canonical tables (`master_plan_state`, `task_state`, `stage_bundle`, etc.) remain sync MCP calls — only **writes** go through queue tables. Reads see whatever state has settled. If a write is still queued, the read sees pre-write state — which is fine because the agent that enqueued doesn't need to read its own write back inside the same skill turn (that's the whole point of "fire and forget").

Cross-skill reads (e.g. ship-cycle Phase 0 reads stage_bundle from prior ship-plan) accept settling lag. With `* * * * *` cadence on hot kinds, max lag is ~60 s. For tight chains (ship-plan → ship-cycle), operator typically waits longer than that anyway between invocations.

---

## 7. Concurrency guardrails

| Risk | Mitigation |
|---|---|
| Two parallel sessions enqueue same write | per-table `idempotency_key` partial unique index |
| Worker crashes mid-job → orphan `running` row | per-table heartbeat sweep tick (`cron-stale-sweep-tick.mjs` runs `*/5 * * * *`) flips stale `running` rows past `claim_heartbeat_timeout_minutes` back to `queued` |
| Same-slug rollup races | handler entry: `SELECT pg_advisory_xact_lock(hashtext('cron:'||slug))` — serial per slug, parallel across slugs |
| Job storm overwhelms tick | per-tick `LIMIT 50` cap + per-kind tick interval = natural rate limit |
| Failed job blocks queue | `attempts` column + per-kind retry budget in `carcass_config` (default 3) → past budget, `status='failed'` (dead-letter) |
| Same kind invoked at wrong cadence (e.g. agent enqueues during `materialize-backlog` run) | claim path uses `FOR UPDATE SKIP LOCKED` — concurrent enqueue lands in next tick |
| Section/stage claim closes mid-handler | handler asserts claim still open at row mutation; closed claim → log `cron_stale_claim` audit row + skip |
| Cron daemon down (laptop asleep) | jobs queue forever (`status='queued'`); resume on next `cron` wake. Idempotent inserts safe across long pauses |
| Dependent jobs (`stage_closeout` after `bundle_apply`) | per-kind handler reads upstream completion via SQL probe before proceeding; if upstream not yet `done` → row stays `queued` until next tick (no FK between queue tables — soft dependency) |

---

## 8. Observability

Per-kind read query:

```sql
SELECT status, count(*)
  FROM cron_audit_log_jobs
 WHERE enqueued_at > now() - interval '1 hour'
 GROUP BY status;
```

Cross-kind dashboard view (lazy union — added in late stage):

```sql
CREATE VIEW cron_jobs_all AS
  SELECT 'audit_log'         AS kind, job_id, status, enqueued_at, finished_at, error FROM cron_audit_log_jobs
  UNION ALL
  SELECT 'journal_append'    AS kind, job_id, status, enqueued_at, finished_at, error FROM cron_journal_append_jobs
  UNION ALL
  SELECT 'status_flip'       AS kind, job_id, status, enqueued_at, finished_at, error FROM cron_status_flip_jobs
  ... // one row per table
```

Web dashboard route reads view, renders status distribution + recent failures + queue depth per kind.

Audit-fail signal: every handler failure also inserts row into `cron_audit_log_jobs` with `audit_kind='cron_job_failed'` (cross-kind audit funnel; closes the loop).

---

## 9. Migration / rollout

Prototype-first methodology. Each stage = narrow tracer + 2–4 kinds.

### Stage 1 (tracer)

- Migrations: `0088_cron_audit_log_jobs.sql` + `0089_cron_journal_append_jobs.sql`.
- Scripts: `audit-log-tick.mjs` + `journal-append-tick.mjs` + their handlers.
- Cron entries (linux + macOS launchd snippets in ops doc).
- MCP tools: `cron_audit_log_enqueue` + `cron_journal_append_enqueue`.
- Wire **2 call sites**: `master_plan_change_log_append(stage_closed)` in `ship-cycle` Phase 7 + `journal_append(phase_checkpoint)` in `ship-cycle` Phase 4.
- Red-stage tracer: ship a fixture stage end-to-end. Assert (a) Phase 7 enqueue completes in <100 ms, (b) `cron_audit_log_jobs` row appears with `status='done'` within 90 s, (c) `master_plan_change_log` row materialized by handler.
- Ops doc: `docs/cron-jobs-ops.md` (Stage 1 sections).

### Stage 2

- Add `cron_status_flip_jobs` + `cron_task_commit_record_jobs` + `cron_stage_verification_flip_jobs`.
- Migrate ship-cycle Phase 4 + Phase 6 + Phase 8 status / commit / verification writes.
- Validator: `validate-cron-handler-coverage.mjs` blocks new queue tables without matching tick + handler.

### Stage 3

- Add `cron_materialize_backlog_jobs` + `cron_regen_indexes_jobs` + `cron_stage_closeout_jobs` + `cron_master_plan_bundle_apply_jobs`.
- Split current atomic `stage_closeout_apply` into core (sync) + tail (queued).

### Stage 4

- Add `cron_glossary_backlinks_jobs` + `cron_anchor_reindex_jobs` + `cron_cache_warm_jobs`.
- Migrate ship-plan Phase 7.5 + post-spec-edit anchor reindex + MCP cache warming.

### Stage 5

- Add `cron_drift_lint_jobs` + `cron_health_rollup_jobs` + `cron_cross_impact_scan_jobs` + `cron_matview_refresh_jobs` + `cron_red_stage_proof_finalize_jobs`.
- Move read-only validators out of `validate:all` synchronous chain.

### Stage 6 (closeout)

- `cron_jobs_all` cross-kind view.
- Web dashboard.
- Stale-sweep runner (`cron-stale-sweep-tick.mjs`) + retry policy + dead-letter UI.
- Validators: `validate-cron-handler-coverage`, `validate-cron-cadence-coverage` (every `cron_*_jobs` table has crontab entry).

---

## 10. Open decisions (poll user)

These need user input before turning this exploration into a master plan.

1. **Worker host.** Single repo cron (laptop/dev box) vs deployed Node service vs both?
2. **Cron daemon source.** System `cron` (linux/macOS launchd) per claims-sweep precedent, or migrate to a single supervisor process running `node-cron` to consolidate logs?
3. **Stale-sweep cadence.** One unified `cron-stale-sweep-tick.mjs` running `*/5 * * * *` across every queue table, or per-table sweep?
4. **Idempotency-key composition.** Centralize as `sha256(skill||slug||stage_id||phase||entity_id||payload_kind)`, or let each enqueue MCP tool define its own?
5. **Retry policy.** Fixed `max_attempts=3` per kind (config row), or per-kind override (some kinds safe to retry forever, e.g. `materialize-backlog`)?
6. **Failure surfacing.** On dead-letter, block next agent session via `master_plan_state` flag, surface only via dashboard, or both?
7. **MCP tool naming.** `cron_audit_log_enqueue` (one per kind) vs umbrella `cron_enqueue(kind, payload)` that just routes to the right table? User leaned to per-kind tables — same per-kind for tools? Confirm.
8. **Cron cadence ownership.** Cadences declared in this doc + ops doc, or also stored in DB (`cron_kind_registry(kind, schedule)`) for runtime introspection?
9. **Backwards-compat window.** Keep current sync MCP tools (`master_plan_change_log_append`, `journal_append`, etc.) live during migration as fallback, or hard-cut per stage?
10. **`master_plan_bundle_apply` queueing.** This is the one heavy write where downstream skills (ship-cycle Phase 0) read back the plan structure. Queue + accept ~60 s settling lag, or keep this one sync?

---

## 11. Estimated wall-clock impact

Per ship-cycle stage (5 tasks):

| Phase | Sync today | After fire-and-forget |
|---|---|---|
| Pass A emit + flips | 30 s | 30 s (status-flip enqueue ~50 ms each — net same) |
| Pass B verify-loop | 60 s | 60 s |
| Pass B closeout (stage_closeout_apply core) | 12 s | 3 s (sync core) + tail queued |
| Pass B commit | 5 s | 4 s |
| `master_plan_change_log_append` | 2 s | 0.05 s |
| `journal_append` | 2 s | 0.05 s |
| `task_commit_record` × 5 | 3 s | 0.25 s |
| `stage_verification_flip` | 1 s | 0.05 s |
| Phase 9 chain digest emit | 1 s | 1 s |
| **Agent wall-clock** | **~116 s** | **~99 s** (−15%, ~17 s saved) |

Per ship-plan invocation (10 tasks, drift-lint + glossary-enrich + anchor-reindex):

| Site | Sync today | After |
|---|---|---|
| `master_plan_bundle_apply` | 8 s | 0.05 s (if queued) — or 8 s (sync override per decision 10) |
| `validate:drift-lint` post-bundle | 6 s | 0.05 s |
| `glossary-backlink-enrich` | 4 s | 0.05 s |
| anchor reindex | 5 s | 0.05 s |
| `master_plan_change_log_append` (drift summary) | 2 s | 0.05 s |
| **Saved** | — | **~25 s** per plan |

Token saving more meaningful: every sync MCP roundtrip carries ~500–2k tokens of conversation noise. Eliminating ~15 such calls per ship-cycle = ~10–30k tokens. Compounds across multi-stage plans.

---

## 12. Next action

Run `/design-explore docs/explorations/async-cron-jobs.md` to:

1. Resolve open decisions 1–10 via `AskUserQuestion` polls.
2. Confirm per-kind table list + cadence map.
3. Expand Stage 1 tracer into red-stage proof + task list.
4. Produce master-plan handoff YAML.

Then `/master-plan-new` from sealed handoff.

---

## Changelog

- 2026-05-06 — Initial exploration. Per-kind queue tables (no shared `job_queue`). Crontab.guru-style schedules per kind. All writes fire-and-forget. Reuse claims-sweep cron pattern. 10 open decisions.
