---
purpose: "TECH-521 — Digest script — telemetry + tool-usage + size cap."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/session-token-latency-master-plan.md"
task_key: "T5.1.2"
---
# TECH-521 — Digest script — telemetry + tool-usage + size cap

> **Issue:** [TECH-521](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

Second half of Phase 1 digest authoring. Adds telemetry + memoization sections and
enforces 300-line cap with block-boundary truncation. Keeps Relevant surfaces
untouched (hard invariant — cited surfaces must always survive).

## 2. Goals and Non-Goals

### 2.1 Goals

1. Last tool outputs section populated from `.claude/telemetry/{session-id}.jsonl` tail -10.
2. Recent memoized calls section populated from `.claude/tool-usage.jsonl` if present; omitted silently if absent.
3. 300-line cap enforced via awk at blank-line block boundaries (no mid-line cuts).
4. Drop order: oldest Recent decisions → oldest Open questions.
5. Relevant surfaces block never truncated.
6. Truncation marker emitted when cap fires.

### 2.2 Non-Goals (Out of Scope)

1. Script skeleton + hook wiring (handled in TECH-520).
2. Re-injection wiring (handled in TECH-522).
3. Integration test (handled in TECH-523).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | context-pack.md includes last 10 tool actions for orientation | Last tool outputs section present with ≤10 rows |
| 2 | Developer | Pack never exceeds 300 lines regardless of session length | awk truncation fires when over cap; Relevant surfaces intact |

## 4. Current State

### 4.1 Domain behavior

TECH-520 delivers basic digest. No telemetry or memoized-calls sections. No size cap enforcement.

### 4.2 Systems map

Touches: `tools/scripts/claude-hooks/context-pack.sh` (extends T5.1.1 deliverable).
Reads: `.claude/telemetry/{session-id}.jsonl` (Stage 1 output), `.claude/tool-usage.jsonl` (Stage 4.1 soft dep).
Writes: `.claude/context-pack.md`.
No Unity / C# / runtime surface touched.

## 5. Proposed Design

### 5.1 Target behavior (product)

Append `Last tool outputs` section via `tail -10 | jq -c '{name, exit, ts}'` from telemetry jsonl. If `tool-usage.jsonl` exists, append `Recent memoized calls` (top 10 `{tool_name, args_hash_short, result_hash_short, ts}`). Enforce 300-line cap: awk pass counts lines; if >300, drop oldest Recent decisions block, recount, repeat with Open questions if still over.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Phase 1 — Telemetry + cap.
1. Append Last tool outputs section via `tail -10 + jq -c` pipe.
2. Guard `tool-usage.jsonl` with `[ -f ... ]`; if present, jq top-10.
3. Author awk block-boundary truncation pass: count lines; if >300, drop oldest Recent decisions block, recount, repeat with Open questions.
4. Emit `_[...truncated N oldest decisions]_` marker on drop.
5. Smoke-test: oversized synthetic pack → truncation fires; Relevant surfaces intact.
6. `npm run validate:all`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-20 | Block-boundary truncation (not line-count truncation) | Mid-line cuts would break section structure | Hard line limit — breaks YAML/structured sections |

## 7. Implementation Plan

### Phase 1 — Telemetry + size cap extension

- [ ] Append `Last tool outputs` section (tail -10 + jq pipe from telemetry jsonl).
- [ ] Soft-guard `tool-usage.jsonl`; append `Recent memoized calls` if present.
- [ ] Author awk block-boundary 300-line truncation pass.
- [ ] Emit truncation marker `_[...truncated N oldest decisions]_`.
- [ ] Smoke-test: oversized synthetic pack → truncation fires, Relevant surfaces intact.
- [ ] `npm run validate:all` green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| IA tooling changes | Node | `npm run validate:all` | Chains validate:dead-project-specs, test:ia, validate:fixtures |
| 300-line cap smoke | Shell smoke | Synthetic oversized pack → awk → line count check | Manual; Relevant surfaces row count must be unchanged |

## 8. Acceptance Criteria

- [ ] Last tool outputs section populated from `.claude/telemetry/{session-id}.jsonl` tail -10.
- [ ] Recent memoized calls section populated from `.claude/tool-usage.jsonl` if present; omitted silently if absent.
- [ ] 300-line cap enforced via awk at blank-line block boundaries (no mid-line cuts).
- [ ] Drop order: oldest Recent decisions → oldest Open questions.
- [ ] Relevant surfaces block never truncated.
- [ ] Truncation marker emitted when cap fires.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## §Plan Author

### §Audit Notes

- Risk: awk truncation cuts mid-line or mid-YAML → corrupts pack consumers. Mitigation: block-boundary detection on blank-line delimiters only; never split non-blank runs; unit-test with synthetic oversized fixture.
- Risk: Relevant surfaces block accidentally dropped on drop-order regression. Mitigation: hard-gate awk pass — explicit allowlist skip for `## Relevant surfaces` block until all Recent decisions + Open questions blocks exhausted; if still over cap after exhausting, emit overflow warning + stop (do NOT truncate Relevant surfaces).
- Risk: telemetry tail `-10` on an empty or missing jsonl produces malformed pack section. Mitigation: `[ -f ... ]` guard + `wc -l` check → emit `(no telemetry)` placeholder instead of empty section.
- Risk: `tool-usage.jsonl` soft-dep (Stage 4.1 T4.1.1) lands late; premature reference breaks. Mitigation: soft-guard with `[ -f ... ]` — section omitted silently when absent, no stderr.
- Ambiguity: does truncation marker count toward 300-line cap? Resolution: marker emitted AFTER line count; not subject to cap re-check.

### §Examples

| Input | Expected output | Notes |
|-------|-----------------|-------|
| Pack 250 lines pre-cap, 0 decisions dropped | No truncation marker; pack emitted as-is | Under cap |
| Pack 380 lines with 5 Recent decisions blocks | Drop oldest 2 decisions → 305 lines → drop 1 more → 298 lines; `_[...truncated 3 oldest decisions]_` marker | Drop-order rule |
| Pack 420 lines, 1 decision + 6 Open questions | Drop the 1 decision → still 405 → drop 4 oldest questions → 298; marker `_[...truncated 1 oldest decisions]_` then pass repeats for questions | Multi-pass |
| Empty `.claude/telemetry/{session}.jsonl` | `Last tool outputs` section emitted with `(no telemetry)` placeholder | Empty-file guard |
| Missing `.claude/tool-usage.jsonl` | `Recent memoized calls` section omitted entirely; no stderr | Soft-dep absent |
| Oversized pack, Relevant surfaces = 250 lines | All decisions + questions dropped; Relevant surfaces intact; overflow warning on stderr; pack over cap but correct | Hard invariant |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| telemetry_tail_populated | fixture jsonl with 15 rows | Last tool outputs section has 10 rows, each `{name, exit, ts}` | manual shell |
| telemetry_missing_file | no jsonl | `(no telemetry)` placeholder, no stderr | manual shell |
| tool_usage_present | fixture tool-usage.jsonl | Recent memoized calls section top-10 entries | manual shell |
| tool_usage_absent | no tool-usage.jsonl | section omitted silently | manual shell |
| truncation_decisions_only | synthetic 380-line pack | 3 oldest decisions dropped; marker present; ≤300 lines | manual shell |
| truncation_decisions_then_questions | 420-line pack, 1 decision + 6 questions | decisions dropped first, then questions; composite marker | manual shell |
| relevant_surfaces_protected | 500-line pack, 250 in Relevant surfaces | decisions + questions emptied; Relevant surfaces intact; stderr overflow warning | manual shell |
| validate_all | post-implementation | `npm run validate:all` green | node |

### §Acceptance

- [ ] `Last tool outputs` section present; sourced from `.claude/telemetry/{session-id}.jsonl` tail -10.
- [ ] `Recent memoized calls` section present iff `.claude/tool-usage.jsonl` exists; silent when absent.
- [ ] awk block-boundary truncation fires at >300 lines; drops oldest Recent decisions first, then oldest Open questions.
- [ ] `Relevant surfaces` block NEVER truncated — verified via protected-fixture test.
- [ ] Truncation marker `_[...truncated N oldest decisions]_` emitted on drop.
- [ ] `npm run validate:all` green.

### §Findings

_none — extends TECH-520 script; same shell-only trust boundary; no new surfaces._


## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes. One paragraph: what shipped, what worked, what to watch._

## §Code Review

_pending — populated by `/code-review`. Verdict: PASS | minor (fix-in-place / deferred) | critical (writes `§Code Fix Plan` below)._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed. Sonnet `code-fix-apply` reads tuples + applies + re-enters `/verify-loop`._
