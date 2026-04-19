---
purpose: "TECH-443 — Pre-refactor snapshot + manifest (Stage 1.1 Phase 2)."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/lifecycle-refactor-master-plan.md"
task_key: "T1.1.2"
---
# TECH-443 — Pre-refactor snapshot + manifest (Stage 1.1 Phase 2)

> **Issue:** [TECH-443](../../BACKLOG.md)
> **Status:** In Progress
> **Created:** 2026-04-18
> **Last updated:** 2026-04-18

## 1. Summary

Snapshot pre-refactor authoring surface so Stage 2.1 + 2.2 transforms can always re-read clean state. Copy all `ia/projects/*master-plan*.md`, `ia/backlog/*.yaml`, `ia/backlog-archive/*.yaml`, and open `ia/projects/{ISSUE_ID}.md` into `ia/state/pre-refactor-snapshot/` preserving relative paths. Write `manifest.json` listing files + counts + git SHA. Satisfies Stage 1.1 Exit bullets 2 + 3 of `lifecycle-refactor-master-plan.md`.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `ia/state/pre-refactor-snapshot/` directory present. Subdirs `ia/projects/`, `ia/backlog/`, `ia/backlog-archive/` mirror current layout.
2. Every `ia/projects/*master-plan*.md` copied verbatim into snapshot.
3. Every `ia/backlog/*.yaml` + `ia/backlog-archive/*.yaml` copied verbatim.
4. Every open `ia/projects/{ISSUE_ID}.md` (non-master-plan) copied verbatim.
5. `ia/state/pre-refactor-snapshot/manifest.json` written: `{git_sha, snapshot_at, counts: {master_plans, backlog_yaml, backlog_archive_yaml, project_specs}, files: [{relative_path, sha256}]}`.
6. Migration JSON updated to reference snapshot path (e.g. `snapshot_root: "ia/state/pre-refactor-snapshot/"` field).

### 2.2 Non-Goals (Out of Scope)

1. No transform of snapshot content — bit-for-bit copy.
2. No deletion of source files.
3. No M0 flip — TECH-442 owns initial M0 done state.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As migrate-master-plans.ts author, I want a stable pre-refactor snapshot so my transform script reads from frozen source even after partial in-place rewrites land mid-batch. | Script reads `ia/state/pre-refactor-snapshot/{rel}` not current path; idempotent re-runs. |

## 4. Current State

### 4.1 Domain behavior

No snapshot exists. Stage 2.1 transform script (T2.1.1) requires a frozen source to support crash-resume + idempotent re-runs.

### 4.2 Systems map

- `ia/state/pre-refactor-snapshot/` — new directory.
- `ia/state/lifecycle-refactor-migration.json` — updated `snapshot_root` reference.
- Downstream consumers: `tools/scripts/migrate-master-plans.ts` (Stage 2.1), T2.2.1 + T2.2.3 spec/yaml fold tasks (Stage 2.2).

## 5. Proposed Design

### 5.1 Target behavior (product)

After this task: snapshot dir contains a deterministic frozen copy of all in-scope files. `manifest.json` allows post-merge audit ("did snapshot really cover X?").

### 5.2 Architecture / implementation

1. Use `cp -R` or `rsync -a` to mirror source paths under `ia/state/pre-refactor-snapshot/`. Preserve relative paths so Stage 2.1 script can compute snapshot path = `snapshot_root + relative_path`.
2. Compute git SHA via `git rev-parse HEAD` at snapshot time — capture in manifest.
3. Compute `sha256` per file (optional but recommended for tamper detection — small file set, cheap).
4. Update migration JSON `snapshot_root` field.
5. Validate counts: `ls snapshot/ia/projects/*master-plan*.md | wc -l` == source count.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-18 | File Stage 1.1 with 1 task per Phase (T1.1.1 + T1.1.2) | Stage 1.1 phases hold 1 task each by design — sequential infra ops; rule rewrite in Stage 1.2 (T1.2.3) lifts cardinality from per-Phase to per-Stage where 2 tasks satisfies. | Merging snapshot into TECH-442 → couples branch creation to a slow IO op + clouds rollback boundary. |
| 2026-04-18 | Snapshot as flat directory (not tarball) | Stage 2.1 + 2.2 scripts read individual files — directory access cheaper + git-diffable. | Tarball — opaque + requires extraction step before each script run. |

## 7. Implementation Plan

### Phase 1 — Snapshot copy

- [ ] Enumerate source files (3 globs).
- [ ] Mirror into `ia/state/pre-refactor-snapshot/`.
- [ ] Verify counts.

### Phase 2 — Manifest + migration JSON update

- [ ] Compute git SHA + per-file sha256.
- [ ] Write `manifest.json`.
- [ ] Update `lifecycle-refactor-migration.json` `snapshot_root` field.
- [ ] `npm run validate:all` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Snapshot file count matches source | Bash | `diff <(find ia/projects -name '*master-plan*.md' \| sort) <(find ia/state/pre-refactor-snapshot/ia/projects -name '*master-plan*.md' \| sed 's,ia/state/pre-refactor-snapshot/,,' \| sort)` | Exit 0 = match. |
| Manifest parses + git SHA present | Node | `node -e "const m=JSON.parse(require('fs').readFileSync('ia/state/pre-refactor-snapshot/manifest.json')); if(!m.git_sha)process.exit(1)"` | Required field check. |
| IA validators green | Node | `npm run validate:all` | Same chain CI runs. |

## 8. Acceptance Criteria

- [ ] `ia/state/pre-refactor-snapshot/` exists with mirrored layout.
- [ ] All `ia/projects/*master-plan*.md`, `ia/backlog/*.yaml`, `ia/backlog-archive/*.yaml`, open `ia/projects/{ISSUE_ID}.md` copied verbatim.
- [ ] `manifest.json` lists every snapshot file + counts + git SHA.
- [ ] Migration JSON references `snapshot_root`.
- [ ] `npm run validate:all` exit 0.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.
