---
purpose: "TECH-724 — curate.py reject --reason subcommand appending JSONL rows to curation/rejected.jsonl with controlled vocab."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.5.2
---
# TECH-724 — curate.py reject --reason → rejected.jsonl

> **Issue:** [TECH-724](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Add `reject <variant> --reason <tag>` subcommand to `tools/sprite-gen/src/curate.py`. Row shape mirrors `promoted.jsonl` plus a `reason` field. Controlled reason vocabulary at ship time: `roof-too-shallow`, `roof-too-tall`, `facade-too-saturated`, `ground-too-uniform`. Invalid tags raise at the CLI boundary.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `reject <variant> --reason <tag>` appends one JSONL row to `curation/rejected.jsonl`.
2. Row shape = promoted row shape + `reason: <tag>`.
3. Invalid `<tag>` → non-zero exit + helpful error listing valid tags.
4. Reuses TECH-723 row-writer + measurement helpers (no duplication).

### 2.2 Non-Goals

1. Aggregator logic that maps reasons to `vary.*` zones — TECH-725.
2. Composer integration — TECH-726.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Curator | Reject with named reason | CLI appends row + prints confirmation |
| 2 | Curator | Mistyped reason caught early | Invalid tag → exit 1 with valid-tag list |
| 3 | Sprite-gen dev | Row carries same stats as promoted | Aggregator treats both inputs uniformly |

## 4. Current State

### 4.1 Domain behavior

After TECH-723 ships, `promote` exists. No rejection path.

### 4.2 Systems map

- `tools/sprite-gen/src/curate.py` — extend CLI.
- `curation/rejected.jsonl` — new append-only log file.

### 4.3 Implementation investigation notes

Keep reason vocabulary as a module-level tuple so it can be imported by TECH-725 and unit tests without re-declaring.

## 5. Proposed Design

### 5.1 Target behavior

```bash
$ python3 -m src.curate reject renders/residential_small__variant7.png --reason roof-too-shallow
[reject] appended row → curation/rejected.jsonl

$ python3 -m src.curate reject renders/... --reason blerg
error: invalid --reason: 'blerg'. Valid: roof-too-shallow, roof-too-tall, facade-too-saturated, ground-too-uniform
```

### 5.2 Architecture / implementation

- Module constant `REJECTION_REASONS = ("roof-too-shallow", "roof-too-tall", "facade-too-saturated", "ground-too-uniform")`.
- `reject` subparser validates `--reason` against the constant; `SystemExit(2)` on miss.
- Row builder reuses TECH-723 helpers + injects `reason` field.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | Controlled vocab at ship | Free-text reasons won't aggregate; carve-out map needs enumerable keys | Free-text — rejected, creates aggregator ambiguity |
| 2026-04-23 | Four initial tags | Covers the dominant "bad variant" failure modes observed in curation pilots | Exhaustive up-front set — rejected, over-specified |

## 7. Implementation Plan

### Phase 1 — Controlled vocab constant + `reject` subparser

### Phase 2 — Row writer reuses TECH-723 helpers

### Phase 3 — Unit test for invalid reason path

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Valid reason appends | Python | `pytest tests/test_curate.py::test_reject_valid_reason -q` | One row appended |
| Invalid reason exits | Python | `pytest tests/test_curate.py::test_reject_invalid_reason_exits -q` | Non-zero exit + stderr list |
| All four tags accepted | Python | `pytest tests/test_curate.py::test_reject_all_initial_reasons -q` | Parametrized over vocab |

## 8. Acceptance Criteria

- [ ] `reject <variant> --reason <tag>` appends JSONL row.
- [ ] Row shape mirrors `promoted.jsonl` plus `reason: <tag>`.
- [ ] Invalid `<tag>` → CLI error listing valid tags.
- [ ] Unit test covers all four initial reasons + invalid-reason error.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|   |             |            |            |

## 10. Lessons Learned

- Controlled vocabularies at the CLI boundary catch typos at curator time, not at aggregator time — cheaper to fix.

## §Plan Digest

### §Goal

Capture curator vetoes with a controlled-vocabulary reason tag so the signature aggregator can carve out `vary.*` zones pointing away from undesirable variant characteristics.

### §Acceptance

- [ ] `python3 -m src.curate reject <variant> --reason <tag>` appends one JSONL row when `<tag>` is in `REJECTION_REASONS`
- [ ] Row shape = promoted row + `reason: <tag>`
- [ ] `--reason <bad-tag>` exits with non-zero status and prints the valid tag list to stderr
- [ ] `REJECTION_REASONS` module-level constant importable from `src.curate` (used by TECH-725 + tests)
- [ ] `pytest tools/sprite-gen/tests/test_curate.py -q` green

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_reject_valid_reason | variant + `--reason roof-too-shallow` | 1 row appended with reason field | pytest |
| test_reject_invalid_reason_exits | variant + `--reason blerg` | SystemExit non-zero; stderr has vocab list | pytest |
| test_reject_all_initial_reasons | parametrize over 4 tags | all 4 append cleanly | pytest |
| test_reject_row_schema_matches_promote | compare schema sans `reason` | identical key set | pytest |

### §Examples

```python
# tools/sprite-gen/src/curate.py (excerpt)
REJECTION_REASONS = (
    "roof-too-shallow", "roof-too-tall",
    "facade-too-saturated", "ground-too-uniform",
)

_REJECTED = Path("curation/rejected.jsonl")

def reject(variant_path: str, reason: str) -> None:
    if reason not in REJECTION_REASONS:
        valid = ", ".join(REJECTION_REASONS)
        raise SystemExit(f"error: invalid --reason: {reason!r}. Valid: {valid}")
    bbox, palette_stats = _measure_variant(variant_path)
    vary_values = _load_vary_values(variant_path)
    row = {
        "variant_path": variant_path,
        "vary_values": vary_values,
        "bbox": bbox,
        "palette_stats": palette_stats,
        "reason": reason,
        "timestamp": time.time(),
    }
    _append_jsonl(row, _REJECTED)
```

### §Mechanical Steps

#### Step 1 — Vocab + subparser

**Edits:**

- `tools/sprite-gen/src/curate.py` — add constant + `reject` subparser with `--reason` choices (`argparse choices=REJECTION_REASONS`).

**Gate:**

```bash
cd tools/sprite-gen && python3 -m src.curate reject --help | grep -E 'roof-too-shallow'
```

#### Step 2 — Row writer reuse

**Edits:**

- Same file — `reject` body reuses TECH-723 helpers + adds `reason` field.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_curate.py::test_reject_valid_reason -q
```

#### Step 3 — Invalid-reason guard

**Edits:**

- Same file — pre-check in `reject` (argparse `choices` also guards; double-guard protects direct Python callers).

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_curate.py::test_reject_invalid_reason_exits -q
```

**MCP hints:** none.

## Open Questions (resolve before / during implementation)

1. Do we need `--note <free-text>` alongside `--reason` for curator commentary? **Resolution:** defer to Stage 6.5 curator dogfooding; unblocked by future superset row schema.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
