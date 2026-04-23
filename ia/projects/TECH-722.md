---
purpose: "TECH-722 — DAS §4.1 addendum documenting accent keys + iso_ground_noise density guardrail."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.4.8
---
# TECH-722 — DAS §4.1 addendum — accent keys + noise density

> **Issue:** [TECH-722](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Append to `docs/sprite-gen-art-design-system.md` §4.1 (Palette) three short subsections: (a) `accent_dark` / `accent_light` palette keys; (b) `iso_ground_noise` density range guardrail `0..0.15`; (c) forward-pointer to `signatures/` for authoring `vary.ground.*` bounds (TECH-719 output).

## 2. Goals and Non-Goals

### 2.1 Goals

1. §4.1 lists `accent_dark` / `accent_light` as optional per-material palette keys.
2. §4.1 documents the `iso_ground_noise` density guardrail `0..0.15`.
3. §4.1 forward-points to `signatures/` for `vary.ground.*` authoring.

### 2.2 Non-Goals

1. Rewriting §4.1; this is an addendum.
2. Documenting composer variant loop (covered in Stage 6.3 addendum R11.1 / TECH-714).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Artist | Learn about accent slots without reading code | §4.1 names both keys + shows JSON shape |
| 2 | Artist | Know density safe range | §4.1 states 0..0.15 guardrail |
| 3 | Spec author | Find signature bounds | §4.1 points to `signatures/` |

## 4. Current State

### 4.1 Domain behavior

§4.1 documents current palette schema (`ramp` per material). No mention of accent keys or noise-density limits; nothing about signature-derived jitter bounds.

### 4.2 Systems map

- `docs/sprite-gen-art-design-system.md` §4.1 — target.
- Cross-references: TECH-708 (DAS §2.6 signatures pointer, already shipped), TECH-716 (palette keys), TECH-717 (primitive), TECH-719 (signature extension).

### 4.3 Implementation investigation notes

Append three short subsections at the end of §4.1; keep style consistent with the Stage 6.3 addendum R11.1 that TECH-714 introduced.

## 5. Proposed Design

### 5.1 Target behavior

§4.1 table of contents grows by three bullet entries; three new subsections render under them with ≤8 lines each. Grep `accent_dark`, `0.15`, `signatures/` all return hits in §4.1.

### 5.2 Architecture / implementation

Pure docs edit; no code changes, no tests. Validation = grep checks.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | Addendum style (three short subsections) | Matches existing §4.1 voice + keeps diff reviewable | Full §4.1 rewrite — rejected, scope creep |
| 2026-04-23 | Forward-pointer, not inline signature schema | Single source of truth (TECH-704 JSON shape) lives in signatures reference | Duplicate schema inline — rejected, maintenance burden |

## 7. Implementation Plan

### Phase 1 — Locate §4.1 anchor

### Phase 2 — Append 3 short subsections

### Phase 3 — Grep check

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Accent keys present | Grep | `rg 'accent_dark' docs/sprite-gen-art-design-system.md` | ≥1 hit under §4.1 |
| Density guardrail present | Grep | `rg '0\.\.0\.15' docs/sprite-gen-art-design-system.md` | ≥1 hit under §4.1 |
| Signatures forward-pointer | Grep | `rg 'signatures/' docs/sprite-gen-art-design-system.md` | ≥1 hit under §4.1 |

## 8. Acceptance Criteria

- [ ] §4.1 lists `accent_dark` / `accent_light` as optional per-material palette keys.
- [ ] §4.1 documents noise density guardrail (`0..0.15`).
- [ ] §4.1 forward-points to `signatures/` for `vary.ground.*` authoring.
- [ ] Grep check: keys + guardrail present in §4.1.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|   |             |            |            |

## 10. Lessons Learned

- Short addenda with grep-able anchors beat long rewrites — easier to review, easier to keep in sync with code.

## §Plan Digest

### §Goal

Add three short subsections under DAS §4.1 so artists and spec authors learn about accent keys, noise density guardrails, and signature-derived jitter bounds from the design system doc rather than source comments.

### §Acceptance

- [ ] DAS §4.1 contains a subsection describing `accent_dark` / `accent_light` as optional per-material palette keys with short JSON example
- [ ] DAS §4.1 contains a subsection stating `iso_ground_noise` density range `0..0.15`
- [ ] DAS §4.1 contains a forward-pointer to `signatures/` explaining that `vary.ground.*` bounds may be derived from signatures (see TECH-704/719)
- [ ] `rg 'accent_dark' docs/sprite-gen-art-design-system.md` returns ≥1 hit in §4.1 context
- [ ] `rg '0\.\.0\.15' docs/sprite-gen-art-design-system.md` returns ≥1 hit in §4.1 context
- [ ] `rg 'signatures/' docs/sprite-gen-art-design-system.md` returns ≥1 hit in §4.1 context

### §Test Blueprint

| check | command | expected |
|-------|---------|----------|
| accent keys documented | `rg -n 'accent_dark' docs/sprite-gen-art-design-system.md` | ≥1 hit |
| density guardrail documented | `rg -n '0\.\.0\.15' docs/sprite-gen-art-design-system.md` | ≥1 hit |
| signatures forward-pointer | `rg -n 'signatures/' docs/sprite-gen-art-design-system.md` | ≥1 hit |

### §Examples

````markdown
<!-- appended under §4.1 in docs/sprite-gen-art-design-system.md -->

#### §4.1.x Accent keys (`accent_dark` / `accent_light`)

Each material entry may optionally declare `accent_dark` and `accent_light` RGB tuples. These are consumed by scatter primitives (e.g. `iso_ground_noise`) to texture a surface with colours that read as darker/lighter specks against the ramp. Absent keys → consumers no-op for that material.

```json
"grass_flat": {
  "ramp": [[34,110,58],[55,140,78],[82,170,98]],
  "accent_dark":  [22,84,42],
  "accent_light": [132,200,140]
}
```

#### §4.1.y `iso_ground_noise` density range

`iso_ground_noise` accepts a `density` parameter in the range `0..0.15`. Values outside this range are clamped. Zero density = no-op. The guardrail exists to prevent accent scatter from overpowering the ramp and breaking the silhouette-first reading of a building.

#### §4.1.z Signature-derived `vary.ground.*` bounds

Rather than hand-tuning jitter ranges, authors may consult the extracted `signatures/` JSON (shape defined in TECH-704, ground fields populated by TECH-719) to derive `vary.ground.hue_jitter` / `value_jitter` bounds from measured variance on reference sprites.
````

### §Mechanical Steps

#### Step 1 — Locate §4.1 anchor

**Edits:**

_Read-only._

**Gate:**

```bash
rg -n '^## 4\.1|^### 4\.1' docs/sprite-gen-art-design-system.md
```

#### Step 2 — Append 3 short subsections

**Edits:**

- `docs/sprite-gen-art-design-system.md` — append the three subsections at the end of §4.1.

**Gate:**

```bash
rg -n 'accent_dark' docs/sprite-gen-art-design-system.md
rg -n '0\.\.0\.15' docs/sprite-gen-art-design-system.md
rg -n 'signatures/' docs/sprite-gen-art-design-system.md
```

#### Step 3 — Cross-ref sanity

**Gate:**

```bash
rg -n 'TECH-704|TECH-719' docs/sprite-gen-art-design-system.md
```

**MCP hints:** none — docs-only.

## Open Questions (resolve before / during implementation)

1. Exact §4.1 anchor text — confirm during Phase 1. **Resolution:** grep; if §4.1 doesn't exist by that number, place addendum under the palette section and update ID.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
