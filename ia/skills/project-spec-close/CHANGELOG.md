# project-spec-close — Changelog

Per-skill friction / bug log. Not auto-loaded by SKILL.md — read on demand.

## 2026-04-18 — step 9 left stale yaml in ia/backlog/ after archival

**Status:** fixed

**Symptom:** After closeout, `ia/backlog/{ISSUE_ID}.yaml` remained on disk (never deleted), while `ia/backlog-archive/{ISSUE_ID}.yaml` was correctly written. Next `materialize-backlog.mjs` run picked up the open-status yaml → rendered issue in BACKLOG.md with a `Spec:` path pointing to the now-deleted spec file → `validate:dead-project-specs` error. Additionally, `backlog-sections.json` manifest was not updated, so archived issues also bled in via the `allIssuesMap` merge even after open yaml was removed.

**Root cause:** Step 9 said "Move" but agents interpreted it as "copy to archive + update archive yaml" without deleting the source. The manifest was never cleaned on closeout. `materialize-backlog.mjs` used `allIssuesMap` (open ∪ closed) for both backlog and archive manifest reconstruction, so archived yaml was visible to open-backlog rendering.

**Fix:**
1. Step 9 rewritten with explicit `rm ia/backlog/{ISSUE_ID}.yaml` + verify deleted + remove from `ia/state/backlog-sections.json`.
2. `materialize-backlog.mjs` `reconstruct()` now takes an `issueMap` param: backlog manifest → `openMap`, archive manifest → `closedMap` (no cross-bleed).

**Rollout row:** standalone

**Tracker aggregator:** standalone

---

## 2026-04-18 — flock missing on macOS blocks materialize-backlog.sh during closeout

**Status:** pending

**Symptom:**
`tools/scripts/materialize-backlog.sh` exits non-zero with `flock: command not found` on macOS; closeout step 9 fails, requires manual intervention on every macOS dev machine run — compounds in `/ship-stage` (N closeouts per stage) and `/release-rollout` (many per umbrella).

**Root cause:**
`materialize-backlog.sh` hard-requires `flock(1)` (util-linux, absent from default BSD/macOS userland) for its `.materialize-backlog.lock` concurrency guard with no portability check or macOS fallback path (`shlock`, `mkdir`-based lock, or node-only `proper-lockfile`).

**Fix:**
pending — add macOS portability guard to `materialize-backlog.sh`; workaround applied per-task during first `/ship-stage` production run: run `node tools/scripts/materialize-backlog.mjs` directly (no concurrency guard; acceptable in single-user local dev).

**Rollout row:** standalone

**Tracker aggregator:** standalone

---
