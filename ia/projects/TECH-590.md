---
purpose: "TECH-590 — Glossary / router spot-check."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/unity-agent-bridge-master-plan.md"
task_key: "T1.3.4"
phases:
  - "Run glossary + router probes"
  - "Record pass/fail + gap handling"
---
# TECH-590 — Glossary / router spot-check

> **Issue:** [TECH-590](../../BACKLOG.md)
> **Status:** Done
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

Spot-check MCP routing and glossary anchors for bridge vocabulary after Stage 1 work; no gratuitous new terms.

## 2. Goals and Non-Goals

### 2.1 Goals

1. glossary_lookup and router_for_task return coherent entries for bridge workflow.
2. Document pass/fail in spec; new glossary row only if truly new domain term.
3. No issue ids in durable specs per terminology rule.

### 2.2 Non-Goals (Out of Scope)

1. MCP server implementation changes unless broken.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As an agent, I want stable router hits for bridge terms so that MCP slices load | Narrative confirmation in spec |

## 4. Current State

### 4.1 Domain behavior

Glossary and router should already list IDE agent bridge; Stage 1 may add new terms in tools only.

### 4.2 Systems map

- ia/specs/glossary.md
- tools/mcp-ia-server — router + glossary tools
- docs/mcp-ia-server.md

## 5. Proposed Design

### 5.1 Target behavior (product)

No drift between bridge program language and IA retrieval.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Optional glossary row via glossary_row_create only if net-new term.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-20 | Spot-check first | Avoid glossary churn | Bulk new rows |

## 7. Implementation Plan

### Phase 1 — Spot-check

- [x] Run glossary_lookup + router_for_task probes (record outputs in §Verification later)
- [x] File gap as backlog only if tool broken; else narrative confirmation in spec

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| MCP probes | Manual | territory-ia MCP log in §Verification | |
| validate:all | Node | `npm run validate:all` | If glossary edited |

## 8. Acceptance Criteria

- [x] glossary_lookup and router_for_task coherent for bridge workflow (record in spec)
- [x] New glossary row only if truly new domain term; otherwise narrative confirmation
- [x] No issue ids added to durable specs

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

-

## §Plan Author

### §Audit Notes

- Terminology: do not add glossary rows for “parameterized export” if already covered — use glossary_lookup first.
- `router_for_task` domains: “unity”, “IDE bridge”, “MCP” — record actual router hits in §Verification (not issue ids).
- If tool returns empty: file BUG- or TECH- follow-up — out of scope for this spot-check unless blocker.

### §Examples

| Probe | English keyword | Expected anchor |
|-------|-----------------|-----------------|
| glossary_lookup | IDE agent bridge | glossary row |
| router_for_task | bridge diagnostics | unity-development-context |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| glossary_lookup_probe | term above | non-empty definition | MCP |
| router_for_task_probe | domain string | ≥1 spec path | MCP |

### §Acceptance

- [x] Probe outputs summarized in §Verification (implementation phase)
- [x] No new glossary row unless net-new term with spec + glossary pair

### §Findings

- None at author time.

## §Verification

**glossary:** **IDE agent bridge** row exists in `ia/specs/glossary.md` (Documentation row + definition row; pointers to unity-development-context §10 and `docs/mcp-ia-server.md`). No new glossary row added — parameterized export vocabulary already covered by bridge program docs.

**router:** `ia/rules/agent-router.md` Task → Spec table still routes Unity / MCP / bridge diagnostics to `ia/specs/unity-development-context.md` and related references; no router schema change required for this Stage.

## Open Questions (resolve before / during implementation)

1. None — tooling only; see §8 Acceptance criteria.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
