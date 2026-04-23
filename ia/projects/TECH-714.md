---
purpose: "TECH-714 — DAS R11 addendum: placement + split seeds + vary grammar."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.3.6
---
# TECH-714 — DAS R11 addendum: placement + split seeds + `vary:` grammar

> **Issue:** [TECH-714](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Extend `docs/sprite-gen-art-design-system.md` R11 with (a) new placement fields `building.footprint_px`, `padding`, `align`; (b) split seed semantics (`palette_seed`, `geometry_seed`, legacy `seed` fan-out); (c) `vary:` grammar (range objects + `seed_scope`). Doc-only.

## 2. Goals and Non-Goals

### 2.1 Goals

1. R11 addendum documents placement fields.
2. R11 addendum documents split seeds + legacy fan-out.
3. R11 addendum documents `vary:` grammar.

### 2.2 Non-Goals

1. Schema or composer changes — TECH-709/710/711.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | New contributor | Find authoritative spec syntax | R11 documents every new field from Stage 6.3 |

## 4. Current State

### 4.1 Domain behavior

R11 documents the current spec schema (class / footprint / composition / variants scalar form / seed scalar). No block-variants, no split seeds, no placement fields.

### 4.2 Systems map

- `docs/sprite-gen-art-design-system.md` R11 section.

### 4.3 Implementation investigation notes

Keep existing R11 content; append a §R11.1 "Stage 6.3 additions" subsection (or equivalent heading) so git history of the original R11 stays intact.

## 5. Proposed Design

### 5.1 Target content

```markdown
### R11.1 Stage 6.3 additions (placement + split seeds + vary grammar)

**Placement (`building:`):**

- `footprint_px: [bx, by]` — pixel-exact footprint; wins over `footprint_ratio`.
- `padding: { n, e, s, w }` — asymmetric empty space per side in px; default all 0.
- `align: center | sw | ne | nw | se | custom` — anchor; default `center`.

**Split seeds (top-level):**

- `palette_seed: int` + `geometry_seed: int` — independent seeds for palette vs geometry samples.
- Legacy scalar `seed: int` fans to both when split seeds absent.

**`vary:` grammar (under `variants.vary`):**

- Range: `{min, max}` (numeric), `{values: [...]}` (categorical).
- Scope: `variants.seed_scope ∈ {palette, geometry, palette+geometry}`; default `palette` preserves legacy behaviour.
```

### 5.2 Architecture / implementation

Doc-only.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | Append as §R11.1 | Preserves R11 git history | Rewrite R11 — rejected, conflates original + addendum |

## 7. Implementation Plan

### Phase 1 — Draft §R11.1

### Phase 2 — Grep-check for new fields

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Addendum present | Grep | `grep -n "R11.1" docs/sprite-gen-art-design-system.md` | ≥1 hit |
| Placement documented | Grep | `grep -E "footprint_px|padding|align" docs/sprite-gen-art-design-system.md` | All 3 hit |
| Split seeds documented | Grep | `grep -E "palette_seed|geometry_seed" docs/sprite-gen-art-design-system.md` | Both hit |
| `vary:` documented | Grep | `grep -E "seed_scope|vary:" docs/sprite-gen-art-design-system.md` | Both hit |

## 8. Acceptance Criteria

- [ ] §R11.1 drafted.
- [ ] Placement, split seeds, `vary:` grammar each covered.
- [ ] Grep checks all pass.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- Preserve original doc sections; append additions with clear version / stage markers for future audits.

## §Plan Digest

### §Goal

Append DAS §R11.1 — documents Stage 6.3 surface additions (placement fields, split seeds with legacy fan-out, `vary:` grammar with `seed_scope`).

### §Acceptance

- [ ] `docs/sprite-gen-art-design-system.md` contains `R11.1` heading
- [ ] All 3 placement fields (`footprint_px`, `padding`, `align`) named and described
- [ ] Split seeds (`palette_seed`, `geometry_seed`) + legacy fan-out documented
- [ ] `vary:` range grammar + `seed_scope` values enumerated
- [ ] Doc-only change; no test impact

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| grep_r11_1_present | DAS file | ≥1 hit on `R11.1` | shell |
| grep_placement_fields | DAS file | hits on `footprint_px`, `padding`, `align` | shell |
| grep_split_seeds | DAS file | hits on `palette_seed`, `geometry_seed` | shell |
| grep_vary_grammar | DAS file | hits on `vary:`, `seed_scope` | shell |

### §Examples

Target addendum:

```markdown
### R11.1 Stage 6.3 additions (placement + split seeds + vary grammar)

**Placement (under `building:`):**

- `footprint_px: [bx, by]` — pixel-exact footprint; wins over `footprint_ratio`.
- `padding: { n, e, s, w }` — asymmetric empty space per side, px; default all 0.
- `align: center | sw | ne | nw | se | custom` — anchor; default `center`.

**Split seeds (top-level):**

- `palette_seed: int`, `geometry_seed: int` — independent seeds for palette vs geometry samples.
- Legacy scalar `seed: int` fans to both when split seeds absent.

**`vary:` grammar (under `variants.vary`):**

- Numeric range: `{min, max}`.
- Categorical: `{values: [...]}`.
- Scope: `variants.seed_scope ∈ {palette, geometry, palette+geometry}`; default `palette` preserves legacy behaviour.
```

### §Mechanical Steps

#### Step 1 — Locate R11

**Gate:**

```bash
grep -n "^### R11" docs/sprite-gen-art-design-system.md
```

Find the end of the existing R11 block.

#### Step 2 — Insert §R11.1

**Edits:**

- `docs/sprite-gen-art-design-system.md` — append `### R11.1 Stage 6.3 additions` after the original R11 block.

**Gate:**

```bash
grep -n "R11.1 Stage 6.3 additions" docs/sprite-gen-art-design-system.md
```

**STOP:** 0 hits → insert failed.

#### Step 3 — Grep checks

**Gate:**

```bash
grep -E "footprint_px|palette_seed|seed_scope" docs/sprite-gen-art-design-system.md
```

**MCP hints:** none — pure doc edit.

## Open Questions (resolve before / during implementation)

1. Should R11.1 include an end-to-end example spec? **Resolution:** yes — one compact example at the end of §R11.1 so readers see the full shape.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
