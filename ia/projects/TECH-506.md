---
purpose: "TECH-506 — B4 unified plan-applier consolidation (retire 3 per-pair appliers)."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/lifecycle-refactor-master-plan.md"
task_key: "T10.5"
phases:
  - "Phase 1 — Author unified skill + agent"
  - "Phase 2 — Retire legacy appliers"
  - "Phase 3 — Update commands + contract"
---
# TECH-506 — B4 unified plan-applier consolidation (retire 3 per-pair appliers)

> **Issue:** [TECH-506](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-19
> **Last updated:** 2026-04-20

## 1. Summary

Author `ia/skills/plan-applier/SKILL.md` as unified Sonnet literal-applier reading any `§*Fix Plan` / `§Stage Closeout Plan` tuple shape (`{operation, target_path, target_anchor, payload}`). Retire three legacy per-pair applier skills (`plan-fix-apply`, `code-fix-apply`, `stage-closeout-apply`) and their agents. Update pair-head skills + slash commands to dispatch `plan-applier`. Resolves legacy Open Q11.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `ia/skills/plan-applier/SKILL.md` present with dispatch table + escalation contract.
2. `.claude/agents/plan-applier.md` present (Sonnet, caveman, uniform tools frontmatter).
3. 3 retired skills + 3 retired agents moved to `_retired/` with tombstone headers.
4. `/plan-review`, `/code-review`, `/closeout` command dispatcher files point to `plan-applier`.
5. `ia/rules/plan-apply-pair-contract.md` references unified applier.
6. `npm run validate:all` green.

### 2.2 Non-Goals (Out of Scope)

1. F5 tools uniformity validator (T10.4 scope — prerequisite).
2. Changing plan-head Opus skills (only tail changed here).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Single applier for all Plan-Apply pair seams | plan-applier SKILL present with dispatch table |

## 4. Current State

### 4.1 Domain behavior

Three separate Sonnet appliers: `plan-fix-apply`, `code-fix-apply`, `stage-closeout-apply`. Divergent logic; maintenance overhead. Open Q11 unresolved.

### 4.2 Systems map

Creates: ia/skills/plan-applier/SKILL.md, .claude/agents/plan-applier.md.
Retires: ia/skills/{plan-fix-apply,code-fix-apply,stage-closeout-apply}/ → ia/skills/_retired/.
Retires: .claude/agents/{plan-fix-applier,code-fix-applier,stage-closeout-applier}.md → .claude/agents/_retired/.
Edits: .claude/commands/{plan-review,code-review,closeout}.md.
Edits: ia/rules/plan-apply-pair-contract.md.

### 4.3 Implementation investigation notes (optional)

Dispatch table keyed on operation type: fs_edit, glossary_row, backlog_archive, id_purge, spec_delete, status_flip, digest_emit. Escalate to Opus on anchor ambiguity. Bounded 1 retry on transient write failure.

## 5. Proposed Design

### 5.1 Target behavior (product)

Unified literal-applier reads any **Plan-Apply pair** tuple shape `{operation, target_path, target_anchor, payload}`. Dispatches per operation type. Single escalation contract. Resolves Open Q11.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

1. Author `ia/skills/plan-applier/SKILL.md` — Phase 1 (parse tuples), Phase 2 (dispatch), Phase 3 (validate), Phase 4 (return).
2. Author `.claude/agents/plan-applier.md` — Sonnet, caveman, uniform tools list.
3. Move 3 skills to `_retired/`; add tombstone header.
4. Move 3 agents to `_retired/`; add tombstone header.
5. Edit `/plan-review`, `/code-review`, `/closeout` commands to dispatch plan-applier.
6. Edit `ia/rules/plan-apply-pair-contract.md`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-19 | Unified applier | Reduces drift; resolves Q11 | Keep 3 separate appliers |

## 7. Implementation Plan

### Phase 1 — Author unified skill + agent

- [x] Author `ia/skills/plan-applier/SKILL.md` with dispatch table.
- [x] Author `.claude/agents/plan-applier.md`.

### Phase 2 — Retire legacy appliers

- [x] Move `plan-fix-apply`, `code-fix-apply`, `stage-closeout-apply` skills to `_retired/` with tombstones.
- [x] Move `plan-fix-applier`, `code-fix-applier`, `stage-closeout-applier` agents to `_retired/` with tombstones.

### Phase 3 — Update commands + contract

- [x] Edit `/plan-review`, `/code-review`, `/closeout` dispatchers.
- [x] Edit `ia/rules/plan-apply-pair-contract.md`.
- [ ] `npm run validate:all` green (pre-commit validators: agent-tools-uniformity, cache-block-sizing, agent-tools, mcp-readme; full `validate:all` blocked on `validate:telemetry-schema` in this workspace).

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| validate:all | Node | `npm run validate:all` | Tooling only |

## 8. Acceptance Criteria

- [x] `ia/skills/plan-applier/SKILL.md` present with dispatch table + escalation contract.
- [x] `.claude/agents/plan-applier.md` present (Sonnet, caveman, uniform tools frontmatter).
- [x] 3 retired skills + 3 retired agents moved to `_retired/` with tombstone headers.
- [x] `/plan-review`, `/code-review`, `/closeout` command dispatcher files point to `plan-applier`.
- [x] `ia/rules/plan-apply-pair-contract.md` references unified applier.
- [ ] `npm run validate:all` green (see Phase 3 note).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

- …

## §Plan Author

### §Audit Notes

- Risk: `.claude/skills/*` symlinks + `ia/skills/_retired/` moves — broken symlink or duplicate skill name breaks host discovery. Mitigation: move then `validate:all`; grep for stale `plan-fix-apply` / `code-fix-apply` / `stage-closeout-apply` path refs.
- Risk: `cursor-skill-*` rules + `AGENTS.md` still cite legacy applier names. Mitigation: sweep `ia/rules/`, `.cursor/rules/`, `CLAUDE.md` in same PR as command dispatchers.
- Ambiguity: operation-type enum vs future tuple kinds — Resolution: single dispatch table in `plan-applier` SKILL; new operation = new row + validator note in **Plan-Apply pair** contract.
- Invariant touch: none (tooling / IA only).

### §Examples

| Tuple source section | `operation` (example) | Expected handling | Notes |
|---------------------|-------------------------|-------------------|-------|
| `§Plan Fix` under master-plan Stage block | `replace_section` | FS edit at `target_anchor` | Same shape as legacy `plan-fix-apply` |
| `§Code Fix Plan` in Task spec | `replace_section` / multi-op | FS edit + optional follow-up | Verbatim order preserved |
| `§Stage Closeout Plan` under master-plan Stage | `archive_record`, `delete_file`, `id_purge` | BACKLOG yaml + spec delete + digest | Same shape as legacy `stage-closeout-apply` |
| Malformed tuple (missing `target_anchor`) | — | Escalate to Opus; no silent skip | Bounded 1 retry on transient I/O only |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| plan_applier_skill_present | `ia/skills/plan-applier/SKILL.md` | Dispatch table + escalation + idempotent re-run contract | manual + `npm run validate:all` |
| retired_skill_tombstones | paths under `ia/skills/_retired/{plan-fix-apply,code-fix-apply,stage-closeout-apply}/` | Tombstone header points to `plan-applier` | manual |
| retired_agent_tombstones | `.claude/agents/_retired/*applier*.md` | Tombstone + no live agent basename collision | manual |
| command_dispatchers_point_to_plan_applier | `.claude/commands/plan-review.md`, `code-review.md`, `closeout.md` | Subagent `plan-applier` (not legacy applier) | grep + manual |
| pair_contract_updated | `ia/rules/plan-apply-pair-contract.md` | Unified `plan-applier` documented | manual |
| validate_all_green | repo root | `npm run validate:all` exit 0 | node |

### §Acceptance

- [ ] `ia/skills/plan-applier/SKILL.md` documents tuple shape, dispatch table, Opus escalation on anchor miss, 1-retry rule for transient failures.
- [ ] `.claude/agents/plan-applier.md` exists; `tools:` frontmatter matches F5 uniformity from T10.4.
- [ ] Three skills + three agents in `_retired/` with tombstone headers; no duplicate live basenames.
- [ ] `/plan-review`, `/code-review`, `/closeout` dispatchers reference `plan-applier`.
- [ ] `ia/rules/plan-apply-pair-contract.md` names unified applier as pair-tail for applicable seams.
- [ ] `npm run validate:all` green.

### §Findings

- Glossary **Plan-Apply pair** row still lists legacy pair-tail skill names — forward implementation uses `plan-applier`; glossary row update belongs to closeout migration (do not edit glossary in this task).

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes. One paragraph: what shipped, what worked, what to watch._

## §Code Review

_pending — populated by `/code-review`. Verdict: PASS | minor (fix-in-place / deferred) | critical (writes `§Code Fix Plan` below)._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed. Sonnet `plan-applier` reads tuples + applies + re-enters `/verify-loop`._
