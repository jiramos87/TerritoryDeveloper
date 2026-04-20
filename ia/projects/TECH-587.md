---
purpose: "TECH-587 — debug-sorting-order SKILL body."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/unity-agent-bridge-master-plan.md"
task_key: "T1.3.1"
phases:
  - "Author SKILL.md body"
  - "Cross-check triggers + glossary anchors"
---
# TECH-587 — debug-sorting-order SKILL body

> **Issue:** [TECH-587](../../BACKLOG.md)
> **Status:** Done
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

New Cursor skill documents end-to-end sorting-order debug: bridge exports, isometric geography §7
authority via spec_section, and agent comparison loop.

## 2. Goals and Non-Goals

### 2.1 Goals

1. SKILL.md lists triggers and prerequisites (DATABASE_URL, Unity Editor, REPO_ROOT).
2. Recipe covers unity_export_sorting_debug and unity_export_cell_chunk plus spec_section geo §7.
3. Checklist matches close-dev-loop style (BUG-28 reference pattern) for before/after comparison.
4. No ia/skills clone; Cursor path under .claude/skills per orchestrator header.

### 2.2 Non-Goals (Out of Scope)

1. MCP server code changes (separate backlog if Zod gaps found).
2. Symlink wiring (TECH-588).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As an agent, I want a single skill recipe for sorting debug so that I can compare exports against §7 | SKILL.md complete per §8 |

## 4. Current State

### 4.1 Domain behavior

No `.claude/skills/debug-sorting-order/SKILL.md` yet; ide-bridge-evidence covers generic bridge evidence.

### 4.2 Systems map

- .claude/skills/debug-sorting-order/SKILL.md — new
- docs/unity-ide-agent-bridge-analysis.md — Design Expansion cross-link optional
- ia/specs/isometric-geography-system.md §7 — sorting formula authority
- tools/mcp-ia-server — unity_export_* tool names

### 4.3 Implementation investigation notes (optional)

Follow `.claude/skills` pattern used by sibling skills (symlink from ia/skills if present).

## 5. Proposed Design

### 5.1 Target behavior (product)

Skill text is accurate for agent-led sorting diagnosis using bridge + geography spec.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Markdown-only SKILL under `.claude/skills/`; optional symlink handled in TECH-588.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-20 | Cursor-only packaging | Orchestrator locks `.claude/skills` path | Duplicate under ia/skills |

## 7. Implementation Plan

### Phase 1 — Skill body

- [x] Author SKILL.md sections (purpose, triggers, prerequisites, phased recipe)
- [x] Wire glossary terms: IDE agent bridge, unity_bridge_command, spec_section
- [x] Add symlink row in ia/skills README or note defer to TECH-588

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| SKILL exists + readable | Manual | Path `.claude/skills/debug-sorting-order/SKILL.md` | Doc-only |
| Repo IA green | Node | `npm run validate:all` | If touched indexed paths |

## 8. Acceptance Criteria

- [x] SKILL.md lists triggers and prerequisites (DATABASE_URL, Unity Editor, REPO_ROOT)
- [x] Recipe covers unity_export_sorting_debug and unity_export_cell_chunk plus spec_section geo §7
- [x] Checklist matches close-dev-loop style (BUG-28 reference pattern) for before/after comparison
- [x] No ia/skills clone; Cursor path under .claude/skills per orchestrator header

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

-

## §Plan Author

### §Audit Notes

- Risk: SKILL must stay Cursor-local (`.claude/skills`) per orchestrator; do not duplicate body under `ia/skills/` unless symlink pattern demands it.
- Risk: `unity_export_sorting_debug` / `unity_export_cell_chunk` names must match `tools/mcp-ia-server` registerTool strings — drift breaks recipe.
- Invariant: no direct `gridArray` / `cellArray` in SKILL text (agents follow `GridManager.GetCell` via bridge exports only).
- Ambiguity: prior “comparison loop” specs used seed-cell before/after pattern — describe that pattern without embedding backlog ids in durable SKILL prose.

### §Examples

| Step | MCP / bridge action | Spec slice | Notes |
|------|---------------------|------------|-------|
| 1 | `unity_bridge_command` kind `export_sorting_debug` with seed params | — | Capture JSON before change |
| 2 | `spec_section` key `geo` §7 | isometric §7 excerpt | Compare formula terms vs export |
| 3 | `unity_export_cell_chunk` bounded bounds | — | Cross-check cell ids vs sorting debug |
| 4 | Re-run after fix | — | Same seeds — compare structured diff |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| skill_exists | path `.claude/skills/debug-sorting-order/SKILL.md` | file present + non-empty triggers | manual |
| trigger_tokens | grep SKILL for `unity_export` | matches MCP tool names | node or manual |
| validate_all | doc-only paths | `npm run validate:all` exit 0 | node |

### §Acceptance

- [ ] §Audit Notes risks addressed or explicitly deferred in §Findings
- [ ] §Examples table covers bridge → spec §7 → compare loop
- [ ] §Test Blueprint lists at least one node-level check for validate:all

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
