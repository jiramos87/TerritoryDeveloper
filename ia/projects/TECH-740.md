---
purpose: "TECH-740 — DAS §12 stub: Animation (reserved); documents output.animation.* + animate: keys + v1 permitted values."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.7.4
---
# TECH-740 — DAS §12 stub — Animation (reserved; not yet implemented)

> **Issue:** [TECH-740](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Insert a new section §12 "Animation (reserved; not yet implemented)" into `docs/sprite-gen-art-design-system.md`. The stub documents the reserved `output.animation.*` keys, the per-primitive `animate:` key, the v1 permitted values (`enabled: false`, `animate: none`), and a forward pointer to the future animation milestone. Written after TECH-737/738 so the doc reflects actual behaviour.

## 2. Goals and Non-Goals

### 2.1 Goals

1. §12 documents `output.animation.*` reserved keys.
2. §12 documents per-primitive `animate:` with permitted v1 value list.
3. §12 forward-points to the future animation milestone.

### 2.2 Non-Goals

1. Implementing the reservation — TECH-737/738.
2. Documenting actual animation semantics — deferred.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Spec author | Learn what's reserved vs. implemented | §12 table enumerates both categories |
| 2 | Future animation dev | Find the seam | §12 points at TECH-737/738 + forward milestone |
| 3 | Reviewer | Confirm doc-code alignment | Grep finds reserved keys + permitted values |

## 4. Current State

### 4.1 Domain behavior

DAS currently has no §12; animation is unmentioned.

### 4.2 Systems map

- `docs/sprite-gen-art-design-system.md` §12 — new section.

### 4.3 Implementation investigation notes

Insert at end of document (or wherever the §N numbering naturally continues). Match existing DAS conventions: one intro paragraph, a reserved-keys table, a permitted-values list, a forward pointer.

## 5. Proposed Design

### 5.1 Target behavior

```markdown
## §12 Animation (reserved; not yet implemented)

The v1 composer does not render animation. The spec grammar nonetheless
reserves two surfaces so future animation work can land without migrating
existing specs:

### Reserved keys

| Scope | Key | v1 permitted values | Notes |
|-------|-----|---------------------|-------|
| Spec-level | `output.animation.enabled` | `false` | `true` raises `SpecError`. |
| Spec-level | `output.animation.frames` | any int | Preserved, not interpreted. |
| Spec-level | `output.animation.fps` | any int | Preserved, not interpreted. |
| Spec-level | `output.animation.loop` | `true`/`false` | Preserved, not interpreted. |
| Spec-level | `output.animation.phase_offset` | any int | Preserved, not interpreted. |
| Spec-level | `output.animation.layers` | list[str] | Preserved, not interpreted. |
| Primitive | `animate` | `none` | Any other value raises `NotImplementedError`. |

### Forward pointer

Animation will land in a future stage (reference milestone TBD). When it
does, `enabled: true` + non-`none` `animate:` values become the entry
points. Specs that already carry `enabled: false` / `animate: none` today
will continue to render unchanged.
```

### 5.2 Architecture / implementation

- Docs-only.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | Single §12 section (not split) | Keeps reserved surface scannable in one place | Per-surface sections — rejected, fragmentation |
| 2026-04-23 | Table + forward pointer | Matches DAS conventions | Prose — rejected, harder to skim |

## 7. Implementation Plan

### Phase 1 — Insert §12 heading

### Phase 2 — Reserved-keys table

### Phase 3 — v1 permitted values + forward pointer

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| §12 heading | Grep | `grep -E "^## §12" docs/sprite-gen-art-design-system.md` | — |
| Reserved keys | Grep | `grep -E "output.animation\|animate" docs/sprite-gen-art-design-system.md` | — |
| Forward pointer | Grep | `grep -i "future\|milestone" docs/sprite-gen-art-design-system.md` | In §12 context |

## 8. Acceptance Criteria

- [ ] DAS §12 exists with heading "Animation (reserved; not yet implemented)".
- [ ] Reserved keys documented (`output.animation.*`, per-primitive `animate:`).
- [ ] v1 permitted values enumerated (`enabled: false`, `animate: none`).
- [ ] Forward pointer to future animation milestone present.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|   |             |            |            |

## 10. Lessons Learned

- Reservation docs written last (after TECH-737/738) catch drift — writing them first would encode pre-implementation assumptions.

## §Plan Digest

### §Goal

Add DAS §12 — a single reference stub for the animation reservation — so spec authors and future animation devs know the seam before the implementation lands.

### §Acceptance

- [ ] §12 heading "Animation (reserved; not yet implemented)" present in DAS
- [ ] Reserved-keys table lists `output.animation.enabled`, sibling keys, and `animate:`
- [ ] v1 permitted values called out (`enabled: false`, `animate: none`)
- [ ] Forward-pointer paragraph references a future milestone
- [ ] Grep for literal `DAS §12` in `tools/sprite-gen/src/` hits at least TECH-737/738 error sites

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| grep_section_heading | DAS | `## §12` line present | bash |
| grep_reserved_keys | DAS | `output.animation` and `animate` both appear in §12 | bash |
| grep_v1_values | DAS | `enabled: false` and `animate: none` appear in §12 | bash |
| grep_source_backreference | `tools/sprite-gen/src/` | `DAS §12` string appears in source error messages | bash |

### §Examples

See §5.1 above for the target Markdown.

### §Mechanical Steps

#### Step 1 — Insert §12 heading

**Edits:**

- `docs/sprite-gen-art-design-system.md` — append `## §12 Animation (reserved; not yet implemented)` with intro paragraph.

**Gate:**

```bash
grep -E "^## §12" docs/sprite-gen-art-design-system.md
```

#### Step 2 — Reserved-keys table

**Edits:**

- Same file — insert table covering `output.animation.*` + `animate:`.

**Gate:**

```bash
grep -E "output.animation" docs/sprite-gen-art-design-system.md
grep -E "animate" docs/sprite-gen-art-design-system.md
```

#### Step 3 — Permitted values + forward pointer

**Edits:**

- Same file — close §12 with `enabled: false` / `animate: none` call-out + forward paragraph.

**Gate:**

```bash
grep -E "enabled: false" docs/sprite-gen-art-design-system.md
grep -E "animate: none" docs/sprite-gen-art-design-system.md
grep -iE "future|milestone" docs/sprite-gen-art-design-system.md
```

**MCP hints:** none.

## Open Questions (resolve before / during implementation)

1. Does DAS already have §11? **Resolution:** inspect at merge time; if yes, §12 follows naturally; if not, use next available number and update this spec.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
