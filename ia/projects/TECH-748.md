---
purpose: "TECH-748 — DAS §3 addendum: cross-tile passthrough (slope + flat archetype via ground.passthrough flag)."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T7.10.4
---
# TECH-748 — DAS §3 amendment — cross-tile passthrough pattern

> **Issue:** [TECH-748](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Add a new subsection to `docs/sprite-gen-art-design-system.md` §3 documenting the cross-tile passthrough pattern: (1) the existing slope-sprite "empty lot / natural-park-walkway" visual pattern used today; (2) the flat-archetype extension via the new `ground.passthrough: true` flag; (3) the rendering implications (skip `iso_ground_noise`, narrowest hue jitter). Written after TECH-745/746 so the doc reflects actual behaviour.

## 2. Goals and Non-Goals

### 2.1 Goals

1. §3 documents the existing slope-sprite passthrough pattern.
2. §3 documents the `ground.passthrough: true` flat-archetype extension.
3. §3 documents rendering implications (no noise; narrowest hue jitter; `value_jitter = 0`).

### 2.2 Non-Goals

1. Implementing the flag / composer branch — TECH-745/746.
2. Tests — TECH-747.
3. DAS sections outside §3.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Spec author | Learn the pattern + flag | §3 addendum scannable |
| 2 | Reviewer | Confirm doc-code alignment | Grep finds `passthrough`, slope, flat phrases |
| 3 | Future curator UI dev | Identify bridge tiles | §3 names the canonical flag |

## 4. Current State

### 4.1 Domain behavior

DAS §3 covers visual language and archetype classes but does not mention passthrough tiles.

### 4.2 Systems map

- `docs/sprite-gen-art-design-system.md` §3 — amend here.

### 4.3 Implementation investigation notes

Existing slope-sprite pattern is already in-use in the art pipeline — the doc should describe what's there today, then introduce the flat-archetype extension as a natural continuation.

## 5. Proposed Design

### 5.1 Target behavior

Insert a new subsection into DAS §3:

```markdown
### §3.N Cross-tile passthrough

Some tiles are designed to blend with their neighbors rather than assert their
own identity — "empty lot" tiles between buildings, walkway segments in a
natural park, or the continuation of a slope between two distinct slope
archetypes. These are *passthrough* tiles.

**Slope archetypes** have carried this pattern by construction since the
original art pipeline. A slope tile's ground plane aligns exactly with its
neighbors so the terrain reads continuous; no per-tile noise or colour
variation is introduced.

**Flat archetypes** pick up the same pattern via the `ground.passthrough`
flag:

```yaml
ground:
  material: grass
  passthrough: true
```

When `passthrough: true`, the composer:
1. Skips the `iso_ground_noise` pass (no per-tile grain).
2. Clamps `hue_jitter` to ≤0.01 (author values higher than this cap are silently narrowed).
3. Forces `value_jitter` to `0`.
4. Preserves the base material colour so adjacent tiles blend.

The default (`passthrough: false`) leaves all Stage 6.4 ground behaviour intact.
```

### 5.2 Architecture / implementation

- Docs-only change to DAS §3.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | One subsection covering both slope + flat | Single reference for the pattern | Two sections — rejected, fragmentation |
| 2026-04-23 | Include YAML snippet | Authors copy-paste | Prose only — rejected, harder to adopt |

## 7. Implementation Plan

### Phase 1 — Locate §3 insertion point

### Phase 2 — Write slope-pattern doc

### Phase 3 — Flat-archetype extension + rendering implications

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| §3 passthrough subsection present | Grep | `grep -i "cross-tile passthrough\|passthrough" docs/sprite-gen-art-design-system.md` | — |
| Slope pattern documented | Grep | `grep -i "slope" docs/sprite-gen-art-design-system.md` | In §3 passthrough context |
| Flat-archetype extension | Grep | `grep "ground.passthrough" docs/sprite-gen-art-design-system.md` | — |
| Rendering implications | Grep | `grep -E "iso_ground_noise\|hue_jitter" docs/sprite-gen-art-design-system.md` | In §3 context |

## 8. Acceptance Criteria

- [ ] §3 documents existing slope-sprite passthrough pattern (empty lot / natural-park-walkway).
- [ ] §3 documents `ground.passthrough: true` flat-archetype extension.
- [ ] §3 documents rendering implications (no noise; narrowest jitter).
- [ ] Grep check confirms `passthrough` + flat/slope phrases present.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|   |             |            |            |

## 10. Lessons Learned

- Capstone docs written last against actual behaviour — writing this before TECH-746 landed would have locked in the wrong jitter threshold or missed the `value_jitter = 0` detail.

## §Plan Digest

### §Goal

Close the Stage 7 addendum by writing DAS §3's cross-tile passthrough subsection — documenting the existing slope pattern, the flat-archetype extension, and the composer's concrete rendering rules.

### §Acceptance

- [ ] DAS §3 has a subsection titled "Cross-tile passthrough" (or equivalent)
- [ ] Subsection names both slope-sprite and flat-archetype contexts
- [ ] Subsection shows a YAML snippet with `ground.passthrough: true`
- [ ] Subsection lists rendering implications (no noise, `hue_jitter ≤ 0.01`, `value_jitter = 0`)
- [ ] Grep finds `passthrough`, `slope`, and `ground.passthrough` in DAS

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| grep_section | DAS §3 | "passthrough" phrase present | bash |
| grep_slope_mention | DAS §3 | "slope" present in passthrough context | bash |
| grep_flat_extension | DAS §3 | `ground.passthrough` literal in YAML block | bash |
| grep_render_implications | DAS §3 | `iso_ground_noise` + `hue_jitter` mentioned | bash |

### §Examples

See §5.1 above for the target Markdown subsection.

### §Mechanical Steps

#### Step 1 — Locate §3 insertion point

**Edits:** none.

**Gate:**

```bash
grep -n "^## §3" docs/sprite-gen-art-design-system.md
```

#### Step 2 — Slope pattern doc

**Edits:**

- `docs/sprite-gen-art-design-system.md` §3 — insert new subsection heading + slope-pattern paragraph.

**Gate:**

```bash
grep -i "slope" docs/sprite-gen-art-design-system.md | grep -i passthrough
```

#### Step 3 — Flat extension + rendering implications

**Edits:**

- Same file — append flat-archetype paragraph with YAML + implications list.

**Gate:**

```bash
grep "ground.passthrough" docs/sprite-gen-art-design-system.md
grep -E "iso_ground_noise" docs/sprite-gen-art-design-system.md
grep "hue_jitter" docs/sprite-gen-art-design-system.md
```

**MCP hints:** none.

## Open Questions (resolve before / during implementation)

1. Is §3 sectioned numerically today (§3.1, §3.2…) or flat? **Resolution:** inspect at merge time; use next available number or append as `### Cross-tile passthrough` if §3 is flat.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
