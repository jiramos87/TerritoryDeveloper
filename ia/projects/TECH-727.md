---
purpose: "TECH-727 — .needs_review JSON sidecar emitted when composer gate exhausts retries without meeting floor."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.5.5
---
# TECH-727 — .needs_review sidecar on floor-miss

> **Issue:** [TECH-727](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

When the composer gate (TECH-726) exhausts retries without meeting the scoring floor, emit a `<sprite>.needs_review.json` sidecar adjacent to the best-scoring rendered variant. Sidecar carries the final score, an envelope snapshot, all attempted seeds, and the list of failing zones. Curator tooling and CI consume the sidecar to surface low-confidence renders without blocking the pipeline.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Sidecar file named `<sprite>.needs_review.json` adjacent to the rendered sprite.
2. Contents: `{final_score, envelope_snapshot, attempted_seeds, failing_zones}`.
3. Absent when variant meets floor within retries.

### 2.2 Non-Goals

1. Curator UI / CI consumption — future; only the producer lives here.
2. Gate retry logic itself — TECH-726.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Curator | Spot low-confidence renders | Sidecar exists for exhausted retries |
| 2 | CI | Surface low-confidence renders without failing build | Sidecar discoverable + readable JSON |
| 3 | Repo guardian | Non-blocking | Pipeline doesn't error on floor-miss |

## 4. Current State

### 4.1 Domain behavior

After TECH-726 ships, the gate exhaustion branch yields the best-scoring variant. No sidecar.

### 4.2 Systems map

- `tools/sprite-gen/src/compose.py` — add sidecar writer on exhaustion branch.

### 4.3 Implementation investigation notes

Sidecar schema is versioned (`schema_version: 1`) so consumers can evolve without breaking.

## 5. Proposed Design

### 5.1 Target behavior

```
renders/residential_small__variant3.png           # rendered (best-scoring)
renders/residential_small__variant3.needs_review.json   # sidecar, present only on floor-miss
```

Sidecar content:

```json
{
  "schema_version": 1,
  "final_score": 0.42,
  "envelope_snapshot": {"roof": {"h_px": {"min": 8, "max": 14}}, "...": "..."},
  "attempted_seeds": [100, 106, 112, 118, 124],
  "failing_zones": ["roof.h_px"]
}
```

### 5.2 Architecture / implementation

- Dataclass `NeedsReviewSidecar(schema_version, final_score, envelope_snapshot, attempted_seeds, failing_zones)`.
- Writer called only on the exhaustion branch of TECH-726's retry loop.
- File path = `<variant_path>.replace(".png", ".needs_review.json")`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | JSON sidecar next to sprite | Obvious to find; no separate registry to maintain | Central ledger — rejected, duplicates info |
| 2026-04-23 | Schema version `1` from day one | Consumers evolve independently | Unversioned — rejected, no forward path |

## 7. Implementation Plan

### Phase 1 — Sidecar dataclass

### Phase 2 — Writer on floor-miss branch

### Phase 3 — Absence test on floor-met branch

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Sidecar on exhaust | Python | test in TECH-728 | File exists; JSON-parseable |
| Schema fields | Python | test in TECH-728 | All four fields populated |
| Absence on pass | Python | test in TECH-728 | No `.needs_review.json` next to passing variant |

## 8. Acceptance Criteria

- [ ] File name `<sprite>.needs_review.json` adjacent to rendered sprite.
- [ ] Contents: final score, envelope snapshot, attempted seeds, failing zones.
- [ ] Absent when variant meets floor within retries.
- [ ] Unit test covers presence/absence branches.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|   |             |            |            |

## 10. Lessons Learned

- Non-blocking sidecars let a quality gate ship before it is perfectly calibrated — the human-in-the-loop replaces the missing confidence.

## §Plan Digest

### §Goal

Emit a versioned JSON sidecar next to the rendered variant whenever the composer gate exhausts retries without meeting the floor, so curators and CI can surface low-confidence renders without blocking the pipeline.

### §Acceptance

- [ ] On retry exhaustion, `<variant_path>.needs_review.json` is written in the same directory as the variant PNG
- [ ] Sidecar JSON has `schema_version: 1`, `final_score`, `envelope_snapshot`, `attempted_seeds`, `failing_zones`
- [ ] On floor-met runs, no sidecar is written
- [ ] Sidecar is non-blocking: composer returns successfully; pipeline continues
- [ ] `pytest tools/sprite-gen/tests/test_curation_loop.py -q` green

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_sidecar_on_exhaustion | spec that never meets floor | sidecar file exists + valid JSON | pytest |
| test_sidecar_schema | exhausted run | all 5 fields populated | pytest |
| test_no_sidecar_on_pass | spec that meets floor | no sidecar file | pytest |
| test_sidecar_lists_all_attempted_seeds | N=5, all fail | 5 seeds in sidecar | pytest |

### §Examples

```python
# tools/sprite-gen/src/compose.py (excerpt)
from dataclasses import dataclass, asdict
from pathlib import Path

@dataclass
class NeedsReviewSidecar:
    schema_version: int
    final_score: float
    envelope_snapshot: dict
    attempted_seeds: list[int]
    failing_zones: list[str]

def _write_needs_review(variant_path: str, best: dict, envelope: dict,
                        attempts: list[int]) -> None:
    sidecar = NeedsReviewSidecar(
        schema_version=1,
        final_score=best["score"],
        envelope_snapshot=envelope,
        attempted_seeds=attempts,
        failing_zones=best["failing_zones"],
    )
    out = Path(variant_path).with_suffix(".needs_review.json")
    out.write_text(json.dumps(asdict(sidecar), indent=2))
```

### §Mechanical Steps

#### Step 1 — Dataclass

**Edits:**

- `tools/sprite-gen/src/compose.py` — add `NeedsReviewSidecar` dataclass.

**Gate:**

```bash
cd tools/sprite-gen && python3 -c "from src.compose import NeedsReviewSidecar; print('ok')"
```

#### Step 2 — Writer on exhaustion branch

**Edits:**

- Same file — hook into TECH-726's `else:` (for/else loop exhaustion).

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_curation_loop.py::test_sidecar_on_exhaustion tests/test_curation_loop.py::test_sidecar_schema -q
```

#### Step 3 — Absence on pass

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_curation_loop.py::test_no_sidecar_on_pass -q
```

**MCP hints:** none.

## Open Questions (resolve before / during implementation)

1. Should sidecar include the rejection `reason` that carved the failing zone? **Resolution:** defer — TECH-725's carve-out table is stable input; sidecar consumer can join via `failing_zones` → reason lookup.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
