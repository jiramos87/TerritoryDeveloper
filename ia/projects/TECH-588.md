---
purpose: "TECH-588 — Symlink + skill index."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/unity-agent-bridge-master-plan.md"
task_key: "T1.3.2"
phases:
  - "Compare existing symlink convention"
  - "Add symlink or document no-op"
---
# TECH-588 — Symlink + skill index

> **Issue:** [TECH-588](../../BACKLOG.md)
> **Status:** Done
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

Align repository skill wiring so debug-sorting-order is discoverable from both ia/skills and
.claude/skills per existing symlink pattern.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Symlink exists if and only if sibling skills use same pattern.
2. README row added only when table already lists packaged skills; otherwise document skip in task report.
3. No duplicate SKILL bodies — single source path documented.

### 2.2 Non-Goals (Out of Scope)

1. Authoring SKILL body (TECH-587).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Maintainer | As a repo owner, I want one canonical skill path so that Cursor and IA indexes stay aligned | Symlink or documented no-op |

## 4. Current State

### 4.1 Domain behavior

Other skills may use `.claude/skills` → `ia/skills` symlinks; pattern must match.

### 4.2 Systems map

- .claude/skills/ — Cursor symlink targets
- ia/skills/README.md — optional index row
- ia/skills/debug-sorting-order/ — symlink target if created

## 5. Proposed Design

### 5.1 Target behavior (product)

Single discoverable path documented in task report.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Mirror existing symlinks; minimal README edit.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-20 | Follow sibling pattern | Consistency | Duplicate files |

## 7. Implementation Plan

### Phase 1 — Wiring

- [x] Compare existing .claude/skills → ia/skills symlinks
- [x] Add symlink or record explicit no-op with reason
- [x] Patch README minimally if required by convention

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Symlink or rationale | Manual | Task report + git path | Doc-only |
| validate:all | Node | `npm run validate:all` | If README indexed |

## 8. Acceptance Criteria

- [x] Symlink exists if and only if sibling skills use same pattern
- [x] README row added only when table already lists packaged skills; otherwise document skip in task report
- [x] No duplicate SKILL bodies — single source path documented

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

-

## §Plan Author

### §Audit Notes

- Risk: Duplicate SKILL bodies if symlink + copy both exist — prefer symlink-only per `AGENTS.md` `.claude/skills` pattern.
- Symlink direction: confirm existing sibling skills (e.g. `ia/skills/*` → `.claude/skills/*`) before creating; reverse breaks Cursor resolution.
- README: `ia/skills/README.md` only if table already lists packaged skills — avoid orphan rows.

### §Examples

| Convention | Action | Evidence |
|------------|--------|----------|
| `ls -la .claude/skills` shows `ia/skills` targets | Add matching symlink for `debug-sorting-order` | git status + path |
| No sibling symlinks | Document no-op in §Verification with reason | — |
| README index exists | Single row with link to SKILL.md | diff |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| symlink_or_rationale | repo tree | symlink or documented skip | manual |
| readme_minimal | README diff | ≤15 lines or no-op | manual |

### §Acceptance

- [ ] Symlink decision documented (created or skipped with reason)
- [ ] README touched only when index pattern requires it

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
