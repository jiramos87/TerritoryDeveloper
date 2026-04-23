---
purpose: "TECH-729 — DAS §5 addendum documenting curation JSONL schema, reason→axis map, gate contract, and sidecar semantics."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.5.7
---
# TECH-729 — DAS §5 addendum — curation loop + floor + sidecar

> **Issue:** [TECH-729](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Append (or extend) DAS §5 with the full Stage 6.5 curation-loop contract: promotion/rejection JSONL schema, envelope aggregator rule, rejection-reason → `vary.*` zone carve-out map, composer score-and-retry contract (retry cap, scoring heuristic), and `.needs_review` sidecar semantics.

## 2. Goals and Non-Goals

### 2.1 Goals

1. §5 documents JSONL schema for `promoted.jsonl` and `rejected.jsonl` rows.
2. §5 publishes the `REJECTION_REASONS` → `vary.*` axis/bound carve-out map.
3. §5 documents `.needs_review` sidecar semantics + schema.

### 2.2 Non-Goals

1. Rewriting §5 end-to-end — this is an addendum; keep existing content untouched.
2. Documenting curator UI / CI pipeline.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Curator | Learn rejection vocab without reading code | §5 lists all four initial reasons |
| 2 | Sprite-gen dev | Understand carve-out map | §5 publishes reason → axis/bound table |
| 3 | Consumer of sidecars | Know schema | §5 documents `.needs_review` JSON fields |

## 4. Current State

### 4.1 Domain behavior

DAS §5 exists (topic varies — confirm during Phase 1); no curation loop documentation.

### 4.2 Systems map

- `docs/sprite-gen-art-design-system.md` §5 — target.
- Cross-references: TECH-723 (promoted schema), TECH-724 (rejection vocab), TECH-725 (aggregator), TECH-726 (gate), TECH-727 (sidecar).

### 4.3 Implementation investigation notes

Style consistent with earlier addenda (Stage 6.2 §2.6, Stage 6.3 R11.1, Stage 6.4 §4.1). Tables preferred over prose.

## 5. Proposed Design

### 5.1 Target behavior

Grep anchors added: `promoted.jsonl`, `rejected.jsonl`, `REASON_AXIS_MAP`, `needs_review`.

### 5.2 Architecture / implementation

Pure docs edit; no code. Validation = grep.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | Tabular schema + reason map | Easy to scan + maintain | Prose — rejected, harder to keep in sync |
| 2026-04-23 | Sidecar schema documented alongside gate | One place for the whole loop | Per-sidecar schema doc — rejected, fragmentation |

## 7. Implementation Plan

### Phase 1 — Locate §5 anchor

### Phase 2 — Append subsections (JSONL schema, reason map, gate contract, sidecar)

### Phase 3 — Grep + cross-ref check

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| JSONL schema present | Grep | `rg 'promoted\.jsonl' docs/sprite-gen-art-design-system.md` | ≥1 hit |
| Reason map present | Grep | `rg 'roof-too-shallow' docs/sprite-gen-art-design-system.md` | ≥1 hit |
| Sidecar semantics | Grep | `rg 'needs_review' docs/sprite-gen-art-design-system.md` | ≥1 hit |
| TECH refs | Grep | `rg 'TECH-723\|TECH-725\|TECH-727' docs/sprite-gen-art-design-system.md` | ≥3 hits |

## 8. Acceptance Criteria

- [ ] §5 documents JSONL schema for promoted / rejected rows.
- [ ] §5 publishes rejection-reason → `vary.*` zone carve-out map.
- [ ] §5 documents `.needs_review` sidecar semantics.
- [ ] Grep check: all three subsections present.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|   |             |            |            |

## 10. Lessons Learned

- A single §N addendum covering the full loop beats N scattered subsections — one anchor to remember, one anchor to grep.

## §Plan Digest

### §Goal

Append DAS §5 with the complete Stage 6.5 curation-loop contract so artists, tool authors, and consumers of `.needs_review` sidecars learn the schema + vocab + semantics from the design system doc.

### §Acceptance

- [ ] DAS §5 has a subsection documenting the `promoted.jsonl` row schema (fields: `variant_path`, `vary_values`, `bbox`, `palette_stats`, `timestamp`)
- [ ] DAS §5 has a subsection documenting the `rejected.jsonl` row schema (promoted schema + `reason`)
- [ ] DAS §5 has a table listing `REJECTION_REASONS` → `vary.*` axis + bound (the `REASON_AXIS_MAP`)
- [ ] DAS §5 has a subsection describing composer score-and-retry contract (retry cap, scoring heuristic, floor)
- [ ] DAS §5 has a subsection describing `.needs_review.json` sidecar semantics + schema (from TECH-727)
- [ ] `rg 'promoted\.jsonl|rejected\.jsonl|needs_review' docs/sprite-gen-art-design-system.md` returns hits

### §Test Blueprint

| check | command | expected |
|-------|---------|----------|
| JSONL schema | `rg -n 'promoted\.jsonl' docs/sprite-gen-art-design-system.md` | ≥1 hit |
| Rejection vocab + map | `rg -n 'roof-too-shallow' docs/sprite-gen-art-design-system.md` | ≥1 hit |
| Sidecar semantics | `rg -n 'needs_review' docs/sprite-gen-art-design-system.md` | ≥1 hit |
| Gate contract | `rg -n 'retry_cap' docs/sprite-gen-art-design-system.md` | ≥1 hit |

### §Examples

````markdown
<!-- appended under DAS §5 -->

#### §5.x Curation log schema

`curation/promoted.jsonl` and `curation/rejected.jsonl` are append-only JSONL logs. One row per curator action.

| Field | Type | Description |
|-------|------|-------------|
| `variant_path` | string | Path to rendered variant |
| `vary_values` | object | Sampled `vary:` values for this variant |
| `bbox` | object | Measured bbox of building mass |
| `palette_stats` | object | Palette stats (dominant, mean hue/value) |
| `timestamp` | number | Unix seconds |
| `reason` | string | (rejected only) one of `REJECTION_REASONS` |

#### §5.y Rejection reasons → vary.* zone map

| Reason | `vary.*` axis | Bound |
|--------|---------------|-------|
| `roof-too-shallow` | `roof.h_px` | `min` |
| `roof-too-tall`    | `roof.h_px` | `max` |
| `facade-too-saturated` | `facade.saturation` | `max` |
| `ground-too-uniform`   | `ground.hue_jitter` | `min` |

#### §5.z Composer score-and-retry contract

The composer gates each variant: sample from envelope → render → score → re-sample up to `retry_cap` times (default 5). Scoring = `1.0 − normalized_distance_from_centroid`; carved-zone hits are hard-fail (score 0). On retry exhaustion the composer emits the best-scoring variant and writes a `.needs_review.json` sidecar (see §5.aa).

#### §5.aa `.needs_review` sidecar semantics

When the gate exhausts retries without meeting the floor, a `<variant>.needs_review.json` sidecar is written adjacent to the variant. Schema version 1:

| Field | Type | Description |
|-------|------|-------------|
| `schema_version` | int | 1 |
| `final_score` | float | Best-attempt score |
| `envelope_snapshot` | object | Envelope at render time |
| `attempted_seeds` | int[] | All seeds tried |
| `failing_zones` | string[] | Carved zones hit |
````

### §Mechanical Steps

#### Step 1 — Locate §5

**Edits:**

_Read-only._

**Gate:**

```bash
rg -n '^## 5|^### 5' docs/sprite-gen-art-design-system.md
```

#### Step 2 — Append subsections

**Edits:**

- `docs/sprite-gen-art-design-system.md` — append four subsections at end of §5.

**Gate:**

```bash
rg -n 'promoted\.jsonl' docs/sprite-gen-art-design-system.md
rg -n 'roof-too-shallow' docs/sprite-gen-art-design-system.md
rg -n 'needs_review' docs/sprite-gen-art-design-system.md
rg -n 'retry_cap' docs/sprite-gen-art-design-system.md
```

#### Step 3 — Cross-refs sanity

**Gate:**

```bash
rg -n 'TECH-723|TECH-725|TECH-727' docs/sprite-gen-art-design-system.md
```

**MCP hints:** none — docs-only.

## Open Questions (resolve before / during implementation)

1. If §5 doesn't exist by that number yet, what anchor? **Resolution:** grep during Phase 1; place addendum under nearest "Quality" / "Curation" heading; update ID mapping if needed.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
