---
purpose: "TECH-776 — Fix mechanicalization-preflight regex for plan-digest rich format."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ""
task_key: ""
---
# TECH-776 — Fix mechanicalization-preflight regex for plan-digest rich format

> **Issue:** [TECH-776](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-24
> **Last updated:** 2026-04-24

## 1. Summary

`mechanicalization-preflight-lint` regex assumes pair-head key format (`file_path:` / `target_file:` / `pick:`). `plan-digest` rich-format output embeds paths in markdown code-backticks on `— **operation**:` lines — regex never matches, preflight flags `picks` field empty despite every edit tuple carrying anchored path. Forces advisory escape hatch in `plan-digest` Phase 5 (grid-asset Stage 3.3 canonical incident — see `ia/skills/plan-digest/SKILL.md` Phase 5 step 4).

## 2. Goals and Non-Goals

### 2.1 Goals

1. Preflight tool returns `pass: true` on valid rich-format §Plan Digest artifacts (per-Task slice + aggregate stage doc).
2. No false positive on `picks` field when every edit carries markdown-wrapped path anchor.
3. Existing pair-head (key-style) format still passes — regression-free across other artifact kinds.
4. Remove `plan-digest` Phase 5 step 4 advisory escape hatch once fix lands.

### 2.2 Non-Goals

1. Not rewriting rich-format digest template.
2. Not changing `plan_digest_lint` semantics.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Dispatch `/stage-file` chain without plan-digest Phase 5 halt on rich-format artifacts | Preflight pass=true; chain continues to plan-review |
| 2 | Developer | Preflight contract matches plan-digest rich format | `picks` regex matches markdown-anchored paths |

## 4. Current State

### 4.1 Domain behavior

Rich-format §Plan Digest emits edit tuples shaped `— **operation**: create` followed by backtick-wrapped path (e.g., `` `ia/projects/TECH-XXX.md` ``). Preflight regex at `tools/mcp-ia-server/src/tools/mechanicalization-preflight-lint.ts:116` — `/(?:file[_\s]?path|target[_\s]?file|pick)[:\s]+([A-Za-z0-9_./-]+)/gi` — requires bare key token + value; never matches markdown-anchored prose.

### 4.2 Systems map

- `tools/mcp-ia-server/src/tools/mechanicalization-preflight-lint.ts` — regex + rule eval.
- `ia/rules/mechanicalization-contract.md` — artifact-kind schema.
- `ia/skills/plan-digest/SKILL.md` Phase 5 — consumer (advisory escape hatch currently).
- `ia/templates/plan-digest-section.md` — rich-format template.

## 5. Proposed Design

### 5.1 Target behavior (product)

Preflight tool recognizes rich-format path anchors when `artifact_kind: "plan_digest"`. Other artifact kinds keep existing regex behavior.

### 5.2 Architecture / implementation

Two options — implementer picks:

- **5.2.a — extend unified regex:** add alternation matching backtick-wrapped path on `— **operation**:` lines (e.g., `/(?:^|\n)\s*— \*\*operation\*\*:\s+\w+\s*\n\s*[-*]?\s*`([^`]+)`/gm`).
- **5.2.b — artifact-kind branch:** keep existing regex for pair-head artifacts; add new `plan_digest` branch with rich-format regex (markdown-aware).

Prefer **5.2.b** — cleaner separation, lower regression risk.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-24 | File as TECH-776 w/ advisory escape hatch deployed first | Unblock grid-asset Stage 3.3 chain immediately; structural fix as follow-up | Block Stage 3.3 pending fix |

## 7. Implementation Plan

### Phase 1 — Regex fix

- [ ] Extend `mechanicalization-preflight-lint.ts` with `plan_digest` artifact-kind branch.
- [ ] Add regex case for markdown-anchored path on `— **operation**:` lines.
- [ ] Unit test on TECH-772..775 §Plan Digest fixtures (pass=true expected).
- [ ] Regression test on existing pair-head fixtures (pass=true preserved).

### Phase 2 — Skill cleanup

- [ ] Remove advisory escape hatch from `ia/skills/plan-digest/SKILL.md` Phase 5 step 4.
- [ ] Re-run Stage 3.3 plan-digest to confirm pass=true native (no advisory).
- [ ] validate:all green.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Preflight returns pass=true on rich-format §Plan Digest | Node unit test | `npm test -w tools/mcp-ia-server` | Add fixture under tools/mcp-ia-server/test/fixtures |
| Regression — pair-head format still passes | Node unit test | same | Existing fixtures preserved |
| Chain runs Stage 3.3 end-to-end w/o advisory | Agent report | `/stage-file` dry-rerun OR direct `mcp__territory-ia__mechanicalization_preflight_lint` on aggregate doc | Confirm advisory hatch removed |

## 8. Acceptance Criteria

- [ ] `mechanicalization_preflight_lint({artifact_kind: "plan_digest"})` returns pass=true on TECH-772..775 §Plan Digest + aggregate doc.
- [ ] No false picks-empty findings when edits carry markdown-wrapped path anchors.
- [ ] Pair-head artifact kinds unaffected (regression-free).
- [ ] Advisory escape hatch removed from plan-digest SKILL.md Phase 5.
- [ ] validate:all green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
