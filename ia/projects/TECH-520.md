---
purpose: "TECH-520 — PreCompact digest script — schema + runtime-state."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/session-token-latency-master-plan.md"
task_key: "T5.1.1"
---
# TECH-520 — PreCompact digest script — schema + runtime-state

> **Issue:** [TECH-520](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

PreCompact hook script emits `.claude/context-pack.md` digesting runtime-state + active
Stage block. Core deliverable of Stage 5.1 Phase 1. Shell-only, <200 ms, deterministic.
Sibling to Stage 3.1 compact-summary.sh (Stop/PostCompact counterpart).

## 2. Goals and Non-Goals

### 2.1 Goals

1. `tools/scripts/claude-hooks/context-pack.sh` executable, shebang bash, `set -uo pipefail`.
2. Reads `.claude/runtime-state.json` + active Stage block via same narrow regex `/ship-stage` Phase 0 uses.
3. Emits §2-schema sections: Active focus, Relevant surfaces, Loaded context sources.
4. PreCompact hook entry wired in `.claude/settings.json`.
5. Pack gitignored (session-ephemeral).
6. Graceful partial failure: exit 0 on malformed inputs; SCHEMA MISMATCH marker present.

### 2.2 Non-Goals (Out of Scope)

1. Telemetry or tool-usage sections (handled in TECH-521).
2. Re-injection wiring in session-start-prewarm.sh (handled in TECH-522).
3. Integration test protocol (handled in TECH-523).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | On `/compact`, context-pack.sh runs and writes `.claude/context-pack.md` with active task + stage + relevant surfaces | Pack file exists post-compact; Active focus and Relevant surfaces sections populated |
| 2 | Developer | Malformed `.claude/runtime-state.json` does not crash hook | exit 0; SCHEMA MISMATCH marker present in pack |

## 4. Current State

### 4.1 Domain behavior

No PreCompact digest script exists. After `/compact`, all active-task context is lost from the agent's working memory unless re-read from source files on next SessionStart.

### 4.2 Systems map

New: `tools/scripts/claude-hooks/context-pack.sh`.
Touches: `.claude/settings.json` (hooks array), `.gitignore`.
Reads: `.claude/runtime-state.json` (Stage 3.1 T3.1.2), active master plan Stage block,
`ia/projects/session-token-latency-master-plan.md`.
Writes: `.claude/context-pack.md` (gitignored).
No Unity / C# / runtime surface touched.

### 4.3 Implementation investigation notes (optional)

Stage block regex: same narrow pattern used by `/ship-stage` Phase 0 — matches `#### Stage {N}.{M}` header + extracts Exit criteria first 5 bullets + Relevant surfaces first 20 lines.

## 5. Proposed Design

### 5.1 Target behavior (product)

On PreCompact event: script reads `.claude/runtime-state.json`, extracts 5 keys (`active_task_id`, `active_stage`, `queued_test_scenario_id`, `last_verify_exit_code`, `last_bridge_preflight_exit_code`), parses active Stage block, writes `.claude/context-pack.md` per extensions doc §2 schema.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Phase 1 — Digest script + hook wire.
1. Draft `context-pack.sh` skeleton (shebang, `set -uo pipefail`, schema header emit).
2. Parse `runtime-state.json` via jq; extract 5 keys; fallback to "unknown" per key.
3. Regex-extract active Stage block from master plan (Stage header + Exit criteria first 5 + Relevant surfaces first 20 lines).
4. Emit §2 schema sections to `.claude/context-pack.md`.
5. Add PreCompact hook entry to `.claude/settings.json`.
6. Add `.claude/context-pack.md` to `.gitignore`.
7. Smoke-test: synthetic `runtime-state.json` + malformed variant both exit 0.
8. `npm run validate:all`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-20 | Shell-only, no `claude -p` subprocess | Keeps compact path <200 ms, deterministic | Node script — heavier, slower |

## 7. Implementation Plan

### Phase 1 — Digest script + hook wire

- [ ] Author `context-pack.sh` (shebang, pipefail, schema emit).
- [ ] Parse `runtime-state.json` via jq with per-key fallbacks.
- [ ] Regex-extract active Stage block (header + exit criteria + relevant surfaces).
- [ ] Emit §2-schema sections to `.claude/context-pack.md`.
- [ ] Add PreCompact hook entry to `.claude/settings.json`.
- [ ] Add `.claude/context-pack.md` to `.gitignore`.
- [ ] Smoke-test synthetic + malformed `runtime-state.json`; both exit 0.
- [ ] `npm run validate:all` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| IA tooling changes (settings.json, gitignore, scripts) | Node | `npm run validate:all` | Chains validate:dead-project-specs, test:ia, validate:fixtures |
| context-pack.sh exits 0 on malformed runtime-state.json | Shell smoke | Synthetic malformed input → exit code check | Manual; no CI harness |

## 8. Acceptance Criteria

- [ ] `tools/scripts/claude-hooks/context-pack.sh` exists, executable, shebang bash, `set -uo pipefail`.
- [ ] Reads `.claude/runtime-state.json` + active Stage block; emits §2-schema sections.
- [ ] PreCompact hook entry wired in `.claude/settings.json`.
- [ ] Pack gitignored (`.claude/context-pack.md` in `.gitignore`).
- [ ] Graceful partial failure: exit 0 on malformed inputs; SCHEMA MISMATCH marker present.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## §Plan Author

### §Audit Notes

- Risk: PreCompact hook latency exceeds 200 ms budget → stalls `/compact` UX. Mitigation: shell-only (no `claude -p`), single jq pass per key, bounded regex read (first 5 exit bullets + 20 relevant-surface lines), no loops over master-plan file.
- Risk: malformed `.claude/runtime-state.json` (partial write mid-session, schema drift) crashes hook → blocks compact. Mitigation: `set -uo pipefail` (NOT `-e`) + every `jq` call guarded with `|| echo "unknown"` + emit `# Context pack — SCHEMA MISMATCH` marker + `exit 0`.
- Risk: Stage-block regex drift between `context-pack.sh` and `/ship-stage` Phase 0 → inconsistent orientation. Mitigation: extract shared grep/sed snippet into comment block citing ship-stage Phase 0 source line; smoke-test both against same master-plan fixture.
- Risk: `.claude/context-pack.md` accidentally committed → pollutes history + leaks session-local state. Mitigation: `.gitignore` entry added in same commit as script; `git check-ignore` smoke-test.
- Ambiguity: which master plan is "active" when multiple orchestrators open. Resolution: read `active_task_id` from runtime-state → resolve `parent_plan` from `ia/projects/{id}.md` front-matter (already present in Stage 3.1 T3.1.2 schema).

### §Examples

| Input | Expected output | Notes |
|-------|-----------------|-------|
| Well-formed `runtime-state.json` with all 5 keys populated | `.claude/context-pack.md` with Active focus / Relevant surfaces / Loaded context sources sections populated from state + Stage block | Happy path |
| `runtime-state.json` missing `queued_test_scenario_id` key | Pack emitted; that field renders as `unknown` | Per-key jq fallback |
| Malformed JSON (truncated mid-object) | `exit 0`; pack contains `# Context pack — SCHEMA MISMATCH` marker; no partial pack from prior run mixed in | Graceful degradation |
| `active_task_id: null` (no active task) | Pack emitted; Active focus section says `no active task`; Relevant surfaces empty list | Idle-session case |
| Master plan has no matching `#### Stage X.Y` header | Pack emitted; Stage block section says `stage not located: {stage_id}` | Regex miss — non-fatal |
| Hook triggered with `.claude/` absent (fresh clone) | `exit 0`; no pack written; stderr silent | Pre-Stage-3.1 compat |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| context_pack_happy_path | fixture `runtime-state.json` + master plan copy | pack file exists; contains all 3 schema sections; `active_task_id` value present | manual shell |
| context_pack_malformed_state | truncated JSON fixture | exit code 0; `SCHEMA MISMATCH` marker in pack; no jq error on stdout | manual shell |
| context_pack_missing_keys | JSON with only 2 of 5 keys | exit 0; 3 missing keys render as `unknown` | manual shell |
| context_pack_stage_regex | master-plan fixture with known Stage X.Y | Relevant surfaces section lists first 20 lines of Stage block verbatim | manual shell |
| context_pack_gitignore | fresh write of pack | `git check-ignore .claude/context-pack.md` exits 0 | manual shell |
| context_pack_settings_wire | `.claude/settings.json` post-edit | `jq '.hooks.PreCompact'` returns entry referencing `context-pack.sh` | manual shell |
| validate_all | post-implementation | `npm run validate:all` green | node |

### §Acceptance

- [ ] `context-pack.sh` executable + shebang `#!/usr/bin/env bash` + `set -uo pipefail` (NOT `-e`).
- [ ] All 5 runtime-state keys extracted with `jq ... // "unknown"` fallback per key.
- [ ] Stage block regex matches `/ship-stage` Phase 0 pattern (Stage header + exit criteria first 5 + relevant surfaces first 20).
- [ ] `.claude/settings.json` PreCompact hook entry references `tools/scripts/claude-hooks/context-pack.sh`.
- [ ] `.claude/context-pack.md` line present in `.gitignore`.
- [ ] Malformed `runtime-state.json` → exit 0 + `SCHEMA MISMATCH` marker.
- [ ] `npm run validate:all` green.

### §Findings

_none — tooling-only scope; shell-only path; no Unity / C# surface touched._


## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes. One paragraph: what shipped, what worked, what to watch._

## §Code Review

_pending — populated by `/code-review`. Verdict: PASS | minor (fix-in-place / deferred) | critical (writes `§Code Fix Plan` below)._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed. Sonnet `code-fix-apply` reads tuples + applies + re-enters `/verify-loop`._
