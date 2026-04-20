---
purpose: "TECH-591 — Close Dev Loop doc alignment."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/unity-agent-bridge-master-plan.md"
task_key: "T1.3.5"
phases:
  - "Author doc subsection"
  - "validate:all"
---
# TECH-591 — Close Dev Loop doc alignment

> **Issue:** [TECH-591](../../BACKLOG.md)
> **Status:** Done
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

Align durable docs so agents understand when close-dev-loop, debug_context_bundle, and export sugar
tools apply — consistent with unity-ide-agent-bridge analysis §7.1 / §10-B.

## 2. Goals and Non-Goals

### 2.1 Goals

1. One subsection links close-dev-loop skill to bridge evidence paths without contradiction.
2. debug_context_bundle vs sugar tools relationship explicit.
3. npm run validate:all green after doc edits.

### 2.2 Non-Goals (Out of Scope)

1. Changing Verification policy thresholds (separate issue).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As an agent, I want clear doc hierarchy for bridge vs close-debug so that I pick the right skill | Subsection in policy or MCP doc |

## 4. Current State

### 4.1 Domain behavior

Agent-led verification policy and MCP doc both mention bridge; risk of overlap or contradiction.

### 4.2 Systems map

- docs/agent-led-verification-policy.md
- docs/mcp-ia-server.md
- docs/unity-ide-agent-bridge-analysis.md — §7.1 narrative

## 5. Proposed Design

### 5.1 Target behavior (product)

Single narrative for supersession of registry staging vs debug_context_bundle.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Short subsection + cross-links; no issue ids in body.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-20 | Prefer policy or MCP anchor | One subsection | Duplicate |

## 7. Implementation Plan

### Phase 1 — Doc patch

- [x] Choose policy vs MCP doc anchor for subsection
- [x] Add cross-links to ide-bridge-evidence + close-dev-loop skills
- [x] validate:all

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Doc links resolve | Node | `npm run validate:all` | |
| Markdown structure | Manual | Review subsection | |

## 8. Acceptance Criteria

- [x] Subsection links close-dev-loop to bridge evidence paths without contradiction
- [x] debug_context_bundle vs sugar tools relationship explicit
- [x] npm run validate:all green after doc edits

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

-

## §Plan Author

### §Audit Notes

- Risk: Duplicate Verification policy text — prefer one subsection + cross-links to `close-dev-loop` skill + `ide-bridge-evidence`.
- Narrative: `debug_context_bundle` (rich payload) vs thin `unity_export_*` sugar — clarify when each is appropriate (token cost vs full context).
- Do not cite backlog issue ids in `docs/` — per terminology rule.

### §Examples

| Doc surface | Content |
|-------------|---------|
| `docs/agent-led-verification-policy.md` | Short paragraph: when to run close-dev-loop vs bridge batch |
| `docs/mcp-ia-server.md` | Cross-link to `ia/skills/close-dev-loop/SKILL.md` + bridge tools |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| doc_links | `npm run validate:all` | exit 0 | node |
| markdown_links | grep `](` in edited section | targets exist | manual |

### §Acceptance

- [ ] Supersession narrative (registry staging vs bundle) explicit in one place
- [ ] validate:all green

### §Findings

- None at author time.

## Open Questions (resolve before / during implementation)

1. None — tooling only; see §8 Acceptance criteria.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
