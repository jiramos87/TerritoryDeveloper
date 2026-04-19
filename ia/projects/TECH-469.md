---
purpose: "TECH-469 — Stage-file pair skills — planner + applier split (Stage 7 T7.2)."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/lifecycle-refactor-master-plan.md"
task_key: "T7.2"
---
# TECH-469 — Stage-file pair skills — planner + applier split (Stage 7 T7.2)

> **Issue:** [TECH-469](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-19
> **Last updated:** 2026-04-19

## 1. Summary

Split monolithic `ia/skills/stage-file/SKILL.md` into Opus `stage-file-plan` pair-head + Sonnet `stage-file-apply` pair-tail per Plan-Apply pair pattern. Planner authors `§Stage File Plan` structured tuple list (one entry per Task); applier materializes id + yaml + spec stub + BACKLOG row per tuple. Shared Stage MCP bundle loaded once by planner; applier never re-queries glossary/router within a Stage. Compress mode relocates to sibling `ia/skills/stage-compress/SKILL.md` cold path. Planner batches Stage-wide Depends-on verification via single `backlog_list` call vs per-task `backlog_issue`.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `ia/skills/stage-file-plan/SKILL.md` — Opus pair-head; reads orchestrator Stage block + context bundle; emits `§Stage File Plan` tuples (`{reserved_id, title, priority, notes, depends_on, related, stub_body}`).
2. `ia/skills/stage-file-apply/SKILL.md` — Sonnet pair-tail; iterates tuples → `reserve-id.sh` → yaml → spec stub → master-plan task-table row → `materialize-backlog.sh` → `validate:dead-project-specs`. Skips Depends-on re-query — planner already verified.
3. Seam registered in `plan-apply-pair-contract.md`.
4. Legacy `ia/skills/stage-file/SKILL.md` either tombstoned or repurposed as dispatcher (decision during implement).
5. `ia/skills/stage-compress/SKILL.md` sibling authored — Compress mode audit / merge-plan / harvest-and-close logic extracted from File-mode hot path; ~80 lines of cold-path prose relocated. Mode detection moves to `/stage-file` command dispatcher layer.
6. Planner emits single `mcp__territory-ia__backlog_list` filter call per Stage for all Tasks' Depends-on ids (vs N × `backlog_issue`). Tuple body carries verified dep set; applier reads without re-query.

### 2.2 Non-Goals

1. `/stage-file` command rewire (handled T7.8 / TECH-475) — except mode-detection hook point which this task documents.
2. Agent markdown split (handled T7.7 / TECH-474).

## 4. Current State

### 4.2 Systems map

- `ia/skills/stage-file/SKILL.md` — current monolithic body (this task splits it; ~272 lines total, ~80 of which are Compress mode cold path).
- `tools/scripts/reserve-id.sh` + `tools/scripts/materialize-backlog.sh` — id + view materialization.
- `ia/rules/plan-apply-pair-contract.md` — seam target.
- `ia/skills/domain-context-load/SKILL.md` — shared Stage bundle loader.
- `mcp__territory-ia__backlog_list` — batch dep verification target (replaces N × `backlog_issue`).

## 7. Implementation Plan

### Phase 1 — Author stage-file-plan SKILL.md (Opus pair-head)

- Define `§Stage File Plan` tuple shape; document anchor resolution rules.
- Document `backlog_list` batch call contract: single filter across all Task Depends-on ids per Stage; cached in tuple list; no per-task re-query.

### Phase 2 — Author stage-file-apply SKILL.md (Sonnet pair-tail)

- Iterator contract + validator gate + escalation rules (id-counter lock failure, materialize failure).
- Applier skips `backlog_issue` per-task dep verification — reads verified set from planner tuple metadata.

### Phase 3 — Author stage-compress SKILL.md (Compress-mode sibling)

- Extract Compress mode audit / merge-plan / harvest-and-close logic from legacy `stage-file/SKILL.md` into standalone sibling.
- Mode-detection dispatcher contract: `/stage-file` command inspects Stage task-status counts, routes to stage-file-plan (File mode) or stage-compress (Compress mode); no cold-path prose loaded in File-mode hot path.

### Phase 4 — Seam registration + legacy skill disposition + validate

## 8. Acceptance Criteria

- [ ] Planner + applier SKILL.md files present.
- [ ] `§Stage File Plan` tuple shape documented in planner body + pair-contract rule.
- [ ] Pair seam row in `plan-apply-pair-contract.md`.
- [ ] `phases:` frontmatter on both.
- [ ] `ia/skills/stage-compress/SKILL.md` present; File-mode hot path no longer loads Compress prose.
- [ ] Planner emits one `backlog_list` filter call per Stage; tuple body carries verified dep set.
- [ ] Legacy `ia/skills/stage-file/SKILL.md` disposition documented.
- [ ] `npm run validate:all` exit 0.

## Open Questions

1. Legacy `ia/skills/stage-file/` path: keep as dispatcher shim (calls planner then applier; inspects mode) or fully tombstone under `_retired/`? Tooling-only — resolve during implement per skill-dispatcher convention. Lean dispatcher shim — Compress/File mode detection needs a command-layer anchor.
