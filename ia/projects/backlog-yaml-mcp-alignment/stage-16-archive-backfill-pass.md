### Stage 16 — Late-hardening + archive backfill (deferred) / Archive backfill pass

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Extend `tools/scripts/backfill-parent-plan-locator.sh` + `.mjs` driver with archive-mode scan (`ia/backlog-archive/*.yaml`) + proper `--skip-unresolvable` handling of plan-missing AND task_key-missing edge cases (N5). Archive records generally won't have full-plan context; backfill skips gracefully + logs per-reason counts.

**Exit:**

- Backfill driver accepts `--archive` flag → scans `ia/backlog-archive/*.yaml` instead of open dir.
- `--skip-unresolvable` handles both edge cases: (a) plan path missing from disk (archived + plan later deleted), (b) task_key suffix absent from title (archive-only records + pre-locator vintage). Each skip reason logged separately.
- Archive pass runs clean on current `ia/backlog-archive/*.yaml`; log reports resolved / skipped-plan-missing / skipped-task-key-missing counts.
- `--dry-run` supported for archive mode (preview skip reasons).
- Doc in `docs/parent-plan-locator-fields-exploration.md` (append) or this master plan Handoff — archive backfill is one-shot; no re-run expected unless plans move.
- Phase 1 — Archive-mode flag + skip-reason logging.
- Phase 2 — Dry-run + fixture tests + doc.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T16.1 | Add `--archive` flag to backfill driver | _pending_ | _pending_ | Edit `tools/scripts/backfill-parent-plan-locator.mjs` — `--archive` flag swaps the yaml-dir glob from `ia/backlog/*.yaml` to `ia/backlog-archive/*.yaml`. Default stays open-dir. Shell wrapper (`backfill-parent-plan-locator.sh`) passes the flag through. |
| T16.2 | Per-reason skip logging | _pending_ | _pending_ | Extend `--skip-unresolvable` behavior — track + log separately: `plan-missing` (parent_plan path not on disk), `task-key-missing` (title has no `(Stage X.Y Phase Z)` suffix + no other coord source). Per-run summary reports both counts + a combined resolved count. |
| T16.3 | Dry-run + fixture tests for archive mode | _pending_ | _pending_ | Extend `tools/scripts/test-fixtures/backfill-locator/archive/` — fixtures for archive-resolved, plan-missing-skip, task-key-missing-skip. Harness asserts count outputs + reason breakdown + dry-run emits preview without writes. |
| T16.4 | Document archive backfill handoff | _pending_ | _pending_ | Append section to `docs/parent-plan-locator-fields-exploration.md` Handoff OR to this master plan's Handoff (under Step 6) — archive backfill is one-shot; document the expected skip counts on current repo state; note re-run only needed if plans move / rename. |

---
