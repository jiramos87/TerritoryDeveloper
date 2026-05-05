# ship-protocol Stage 4 — drift findings + path decision

## Context

`ship-protocol` Stage 4 = `ship-final` skill (close master-plan version, write `version-close` journal row, tag `{slug}-v{N}`). Tasks `TECH-12643/12644/12645`.

Pass A landed (3 tasks → `implemented` in DB). Pass B verify-loop ran `validate:all` → red. Verify failure root cause was **NOT Stage 4 code** — preexisting drift on `main` HEAD that Pass A inherited cumulatively.

Worktree state at decision time: dirty (Stage 4 mutations.ts / ia-db-writes.ts / web UI / theme / parse-cache / BACKLOG.md / backlog-sections.json + 3 untracked surfaces under `.claude/` `.cursor/`).

## Drift items (preexisting, NOT Stage-4-introduced)

### A. `validate:mcp-readme` — 23 tools registered in src, missing from README

Baseline confirmed via `git stash` check (122 src / 99 docs **before** Stage 4 vs 124 src / 101 docs **after** = same delta of 23). Stage 4 added 2 entries (`master_plan_close` + `master_plan_version_create`) — those are documented. Drift = older tools that landed without README backfill:

```
arch_surface_write
claim_heartbeat
claims_sweep
intent_lint
master_plan_lock_arch
master_plan_sections
next_migration_id
red_stage_proof_capture
red_stage_proof_finalize
red_stage_proof_get
red_stage_proof_list
seams_run
section_claim
section_claim_release
section_closeout_apply
stage_claim
stage_claim_release
stage_decompose_apply
task_batch_insert
task_bundle_batch
task_diff_anomaly_scan
task_intent_glossary_align
task_status_flip_batch
```

### B. `validate:web` — 3 eslint errors

| File | Line | Rule | Description |
| --- | --- | --- | --- |
| `web/components/catalog/SearchBar.tsx` | 78 | `react-hooks/set-state-in-effect` | `setActiveIdx(-1)` called synchronously inside `useEffect([results])` |
| `web/lib/hooks/useSearchDebounce.ts` | 52 | `react-hooks/set-state-in-effect` | `setState({...})` called synchronously inside fetch effect when query is empty |
| `web/lib/hooks/__tests__/useSearchDebounce.test.ts` | 33 | `prefer-const` | `let t1 = setTimeout(...)` never reassigned |

(16 warnings co-existed; not gates — only the 3 errors block lint.)

## Path comparison

| | Path A — separate fix commit | Path B — fold into Stage 4 |
| --- | --- | --- |
| **Commit boundary** | `fix(web,mcp): drift cleanup` precedes Stage 4 | Single `feat(ship-protocol-stage-4)` carries hygiene + feature |
| **Diff size** | Stage 4 commit narrow, reviewable | Stage 4 commit ~+200 lines (23 README entries + 3 lint fixes) on top of feature |
| **Bisect fidelity** | Each commit one concern → easier `git bisect` | Mixed concern → bisect points at hygiene + feature simultaneously |
| **Risk of mis-attribution** | Low — drift visibly attributed to old code | High — future readers blame Stage 4 for setState pattern |
| **Re-run gate** | `/ship-stage` resume → Pass A skipped (statuses already `implemented`), Pass B sees green `validate:all` | Same |
| **Effort** | One extra commit (mechanical) | None |

## Decision

**Path A** chosen — preserves Stage 4 commit boundary, keeps each concern isolable, narrows blast radius for review and potential revert.

## Fix scope (Path A commit)

`fix(web,mcp): drift cleanup before ship-protocol-stage-4`

- README backfill: 23 missing tool docstrings under their semantic sections (arch / claim / red-stage-proof / seams / section / stage / task).
- Web lint: 3 errors → fix in place (lift state out of effect / use `const`).
- This findings doc (`docs/ship-protocol-stage-4-drift-findings.md`).

Worktree handling: Stage 4 modifications stay in worktree. Drift fix files staged + committed selectively (lint files clean at HEAD; README has Stage 4 +2 lines that get folded into drift commit since they are also doc-hygiene; mutations.ts / ia-db-writes.ts / etc. left dirty for Stage 4 commit).

## Re-ship sequence

1. `fix(web,mcp): drift cleanup` — committed first.
2. `/stage-authoring ship-protocol Stage 4` — main session, no subagent. Refresh §Plan Digest sections in DB.
3. `/ship-stage ship-protocol Stage 4` — main session via `ship-stage-main-session` skill. Resume gate skips Pass A. Pass B runs verify-loop on cumulative HEAD diff = green → closeout + commit `feat(ship-protocol-stage-4)`.

## Lessons

- `validate:all` red on preexisting drift can cascade-block Pass B even when Stage code is green. Future Stage starts should run a `validate:all` baseline check before Pass A to catch inherited drift early.
- README backfill on new MCP tools needs to happen at PR / merge time, not deferred — the 23-tool gap accumulated silently across multiple stages.
- Path A pattern reusable: any time Pass B reveals preexisting `validate:*` red, drop a `fix(...)` commit BEFORE re-running ship-stage; never bypass the gate.
