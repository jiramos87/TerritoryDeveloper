# cron-jobs-ops — Async Cron Supervisor

Supervisor: `tools/cron-server/index.ts`. Sibling to `web/`.

---

## Boot

```bash
# Boot web + cron supervisor in parallel
npm run dev:all

# Boot cron-server alone (debug)
npx tsx tools/cron-server/index.ts
```

Supervisor loads `DATABASE_URL` from repo `.env` (falls back to `postgresql://postgres:postgres@localhost:5434/territory_ia_dev`).

---

## Per-kind cadence table

| Kind | Queue table | Destination table | Cadence | MCP enqueue tool |
|---|---|---|---|---|
| `audit-log` | `cron_audit_log_jobs` | `ia_master_plan_change_log` | `* * * * *` (every 1 min) | `cron_audit_log_enqueue` |
| `journal-append` | `cron_journal_append_jobs` | `ia_ship_stage_journal` | `* * * * *` (every 1 min) | `cron_journal_append_enqueue` |
| `task-commit-record` | `cron_task_commit_record_jobs` | `ia_task_commits` | `* * * * *` (every 1 min) | `cron_task_commit_record_enqueue` |
| `stage-verification-flip` | `cron_stage_verification_flip_jobs` | `ia_stage_verifications` | `* * * * *` (every 1 min) | `cron_stage_verification_flip_enqueue` |
| `arch-changelog-append` | `cron_arch_changelog_append_jobs` | `ia_arch_changelog` | `* * * * *` (every 1 min) | `cron_arch_changelog_append_enqueue` |
| `materialize-backlog` | `cron_materialize_backlog_jobs` | (filesystem — `BACKLOG.md`) | `*/2 * * * *` (every 2 min) | `cron_materialize_backlog_enqueue` |
| `regen-indexes` | `cron_regen_indexes_jobs` | (filesystem — `data/*.json`) | `*/5 * * * *` (every 5 min) | `cron_regen_indexes_enqueue` |

---

## Enqueue pattern (agent side)

ship-cycle Phase 4 (journal checkpoint):
```json
// cron_journal_append_enqueue
{
  "session_id": "{SESSION_ID}",
  "task_id": "{TASK_ID}",
  "slug": "{SLUG}",
  "stage_id": "{STAGE_ID}",
  "phase": "ship-cycle.4.per_task",
  "payload_kind": "phase_checkpoint",
  "payload": { "phase_id": "..." }
}
```

ship-cycle Phase 7 (stage-closed audit):
```json
// cron_audit_log_enqueue
{
  "slug": "{SLUG}",
  "audit_kind": "stage_closed",
  "body": "...",
  "stage_id": "{STAGE_ID}",
  "commit_sha": "{SHA}"
}
```

Both tools return `{job_id, status:'queued'}` in < 100 ms (single INSERT).

---

## Troubleshoot

### Claim stuck (row stays `queued`)

```bash
# Check supervisor is running
ps aux | grep "cron-server"

# Check row status
PGPASSWORD=postgres psql -h localhost -p 5434 -U postgres territory_ia_dev \
  -c "SELECT job_id, status, enqueued_at, attempts, error FROM cron_audit_log_jobs ORDER BY enqueued_at DESC LIMIT 10;"
```

### Heartbeat stale (row stuck in `running`)

A row stuck `running` with `heartbeat_at` > 5 min old = supervisor crashed mid-tick.
Reset manually:

```sql
UPDATE cron_audit_log_jobs
SET status = 'queued', started_at = NULL, heartbeat_at = NULL
WHERE status = 'running' AND heartbeat_at < now() - interval '5 minutes';
```

### Handler failed (row in `failed`)

Check `error` column for exception message. Re-queue by resetting status:

```sql
UPDATE cron_audit_log_jobs SET status = 'queued', error = NULL WHERE job_id = '<job_id>';
```

---

## Enqueue patterns — Stage 3 kinds

`project-new-apply` enqueues materialize-backlog instead of shelling inline:
```json
// cron_materialize_backlog_enqueue
{ "triggered_by": "project-new-apply" }
```

`arch-drift-scan` Phase 5 enqueues audit-log instead of `master_plan_change_log_append`:
```json
// cron_audit_log_enqueue
{ "slug": "{SLUG}", "audit_kind": "arch_drift_scan", "body": "...", "stage_id": "{STAGE_ID}" }
```

`section_closeout_apply` post-tx enqueues section_done change_log async:
```json
// cron_audit_log_jobs row (enqueued internally, not via MCP tool)
{ "audit_kind": "section_done", "body": "{...}" }
```

---

## Stage history

| Stage | Kinds live |
|---|---|
| Stage 1 (tracer) | `audit-log` (→ `ia_master_plan_change_log`), `journal-append` (→ `ia_ship_stage_journal`) |
| Stage 2 | + `task-commit-record` (→ `ia_task_commits`), `stage-verification-flip` (→ `ia_stage_verifications`), `arch-changelog-append` (→ `ia_arch_changelog`) |
| Stage 3 | + `materialize-backlog` (→ `BACKLOG.md` filesystem), `regen-indexes` (→ `data/*.json` filesystem) |
