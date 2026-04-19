---
purpose: "TECH-473 — Update remaining lifecycle skills — drop Phase layer + Stage MCP bundle contract (Stage 7 T7.6)."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/lifecycle-refactor-master-plan.md"
task_key: "T7.6"
---
# TECH-473 — Update remaining lifecycle skills — drop Phase layer + Stage MCP bundle contract (Stage 7 T7.6)

> **Issue:** [TECH-473](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-19
> **Last updated:** 2026-04-19

## 1. Summary

Edit 5 existing lifecycle SKILL.md bodies (project-spec-implement, verify-loop, ship-stage, stage-file, project-new) to remove all Phase-layer references (Phase bullets, Phase cardinality gate, per-Phase context reload). Replace with Stage-level MCP bundle contract — shared `domain-context-load` result loaded once per Stage; Sonnet never re-queries glossary/router within a Stage. Update lifecycle-stage enum references to new pair-head + pair-tail names from Stage 5 MCP work (TECH-458).

## 2. Goals and Non-Goals

### 2.1 Goals

1. Zero Phase-layer language across 5 bodies.
2. Stage MCP bundle contract section added to each (references `domain-context-load/SKILL.md`).
3. lifecycle_stage enum references match new 11-value set from TECH-458.

### 2.2 Non-Goals

1. `phases:` frontmatter audit (handled T7.12 / TECH-479).
2. Authoring new pair skills (T7.1–T7.4).

## 4. Current State

### 4.2 Systems map

- `ia/skills/project-spec-implement/SKILL.md`, `ia/skills/verify-loop/SKILL.md`, `ia/skills/ship-stage/SKILL.md`, `ia/skills/stage-file/SKILL.md`, `ia/skills/project-new/SKILL.md` — edit targets.
- TECH-446 hierarchy rule rewrite = schema base.
- TECH-458 MCP enum update = enum source.

## 7. Implementation Plan

### Phase 1 — Grep + drop Phase language

### Phase 2 — Insert Stage MCP bundle contract section per body

### Phase 3 — Validate

## 8. Acceptance Criteria

- [ ] 5 bodies carry zero Phase-layer references.
- [ ] Stage MCP bundle contract section present in each.
- [ ] lifecycle_stage enum references aligned.
- [ ] `npm run validate:all` exit 0.

## Open Questions

1. None — tooling only.
