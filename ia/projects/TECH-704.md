---
purpose: "TECH-704 — Signature module core with L15 sample-size policy."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.2.1
---
# TECH-704 — Signature module core (`src/signature.py`) with L15 sample-size policy

> **Issue:** [TECH-704](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Author `tools/sprite-gen/src/signature.py` — the calibration core for Stage 6.2. Module exposes `compute_signature(class_name, folder_glob) -> dict` to summarize reference catalog sprites into the per-class JSON envelope, `validate_against(signature, rendered_img) -> ValidationReport` for generator validation, and `SignatureStaleError` for L3 staleness. L15 sample-size policy branches the `mode` field across three cases (0 → fallback, 1 → point-match, ≥2 → envelope).

## 2. Goals and Non-Goals

### 2.1 Goals

1. `compute_signature(class_name, folder_glob) -> dict` returns documented JSON dict.
2. `validate_against(signature, rendered_img) -> ValidationReport` returns `.ok` + `.failures`.
3. L15 sample-size policy: `0 → mode: fallback`, `1 → mode: point-match`, `>=2 → mode: envelope`.
4. L3 staleness guard: checksum mismatch raises `SignatureStaleError` with actionable message.
5. Unit tests alongside module validate all three L15 branches + staleness path.

### 2.2 Non-Goals (Out of Scope)

1. CLI wiring — belongs to TECH-705.
2. Fallback graph JSON authoring — TECH-705 writes `_fallback.json`.
3. Spec loader opt-out flag — TECH-706.
4. Parametrized calibration tests — TECH-707.
5. DAS §2.6 pointer — TECH-708.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Sprite-gen dev | Validate generator output against per-class envelope | `validate_against(sig, img).ok` True when inside envelope; False with failure list when drifted |
| 2 | Catalog curator | Regenerate signature after adding a reference sprite | `compute_signature` re-runs produce updated checksum + fields |
| 3 | CI | Fail fast if signature is stale | `SignatureStaleError("signature stale — run python3 -m src refresh-signatures <class>")` raised |

## 4. Current State

### 4.1 Domain behavior

Today: hand-coded scale-calibration tests (`tests/test_scale_calibration.py`) assert bbox ranges per spec. No per-class envelope, no extensibility to palette / silhouette / ground signatures. Stage 6 introduced the signature concept; Stage 6.2 builds the first implementation.

### 4.2 Systems map

- New `tools/sprite-gen/src/signature.py` (this task).
- Consumers: `tests/test_signature_calibration.py` (TECH-707), refresh CLI (TECH-705), composer render-time gate (Stage 6.5).
- Inputs: `Assets/Sprites/<class>/*.png`; PIL `Image` rendered by composer.
- Upstream dep: `src/spec.py::load_spec` reading `include_in_signature` (TECH-706) consumed once TECH-706 lands.

### 4.3 Implementation investigation notes (optional)

L15 fallback mode requires reading `signatures/_fallback.json` (authored in TECH-705). Module should accept an injected fallback graph path to keep unit tests hermetic.

## 5. Proposed Design

### 5.1 Target behavior (product)

```python
sig = compute_signature("residential_small", "Assets/Sprites/residential_small/*.png")
assert sig["class"] == "residential_small"
assert sig["mode"] in {"envelope", "point-match", "fallback"}
assert sig["source_checksum"].startswith("sha256:")

report = validate_against(sig, compose_sprite(load_spec("specs/building_residential_small.yaml")))
assert report.ok
```

### 5.2 Architecture / implementation

- `compute_signature(class_name, folder_glob, *, fallback_graph_path=None) -> dict` — loads source PNGs, computes sha256 of concatenated bytes, extracts per-sprite measurements (bbox, palette, silhouette peaks, ground dominant, decoration hints), summarizes per L15 branch.
- `validate_against(signature, rendered_img, *, source_folder=None) -> ValidationReport` — when `source_folder` supplied, recompute checksum and raise `SignatureStaleError` on mismatch; otherwise trust signature and only compare measurements.
- `ValidationReport` is a dataclass `{ok: bool, failures: list[str]}`.
- `SignatureStaleError(Exception)` — carries actionable refresh command.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-23 | Module returns plain `dict` (not dataclass) for signature | JSON round-trip symmetry; `json.dumps(sig)` works directly | Pydantic model — rejected, adds dep weight for a pure-data shape |
| 2026-04-23 | `ValidationReport` is dataclass, not tuple | Named fields survive refactors; test assertions read naturally | Tuple `(ok, failures)` — rejected, positional confusion |
| 2026-04-23 | `SignatureStaleError` message literally cites `python3 -m src refresh-signatures <class>` | Operators reading red CI should copy-paste the fix | Generic "stale" — rejected, forces log spelunking |

## 7. Implementation Plan

### Phase 1 — JSON shape + checksum helper

- [ ] Author `_json_shape()` + `_compute_checksum(paths)` helpers.
- [ ] Unit test: two PNG fixtures produce deterministic checksum.

### Phase 2 — Per-sprite measurement extractors

- [ ] Implement `_measure_bbox`, `_measure_palette`, `_measure_silhouette`, `_measure_ground`, `_measure_decoration_hints`.
- [ ] Unit tests with 1-sprite + 3-sprite fixtures.

### Phase 3 — L15 branches

- [ ] `_summarize(measurements, fallback_graph)` dispatches on `len(measurements)`: 0 → fallback lookup, 1 → point-match, ≥2 → min/max/mean envelope.
- [ ] Unit tests for each branch.

### Phase 4 — `validate_against` + `SignatureStaleError`

- [ ] Checksum recompute when `source_folder` provided.
- [ ] Envelope comparisons per field; failures collected into `ValidationReport.failures`.
- [ ] Unit test: stale checksum → `SignatureStaleError`; out-of-envelope → `ValidationReport(ok=False)`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| L15 branches | Python | `pytest tools/sprite-gen/tests/test_signature.py::test_l15 -q` | 3 unit tests (one per branch) |
| Staleness guard | Python | `pytest tools/sprite-gen/tests/test_signature.py::test_stale -q` | Raises `SignatureStaleError` on checksum drift |
| Validate-against happy path | Python | `pytest tools/sprite-gen/tests/test_signature.py::test_validate_ok -q` | `ValidationReport.ok is True` |
| Full suite | Python | `cd tools/sprite-gen && python3 -m pytest tests/ -q` | 221+ baseline (post-Stage-6.1) stays green |

## 8. Acceptance Criteria

- [ ] `compute_signature` returns the JSON dict shape specified in handoff §3 Stage 6.2.
- [ ] `validate_against` returns `ValidationReport`; raises `SignatureStaleError` on checksum mismatch.
- [ ] L15 branches all covered (fallback / point-match / envelope).
- [ ] Unit tests green; full suite green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- Signatures are pure data with a checksum — resist the urge to attach methods or behaviour. A `dict` plus pure functions is easier to snapshot, version, and lint.

## §Plan Digest

### §Goal

Ship `tools/sprite-gen/src/signature.py` exposing `compute_signature`, `validate_against`, `SignatureStaleError` with L15 sample-size branching (0 → fallback, 1 → point-match, ≥2 → envelope) and L3 staleness guard on `source_checksum`.

### §Acceptance

- [ ] `signature.py` module authored with documented public surface
- [ ] L15 branch `fallback` resolves target class via injected fallback graph
- [ ] L15 branch `point-match` emits single-sprite measurements verbatim
- [ ] L15 branch `envelope` emits min/max/mean summaries over ≥2 samples
- [ ] `validate_against` with stale `source_checksum` raises `SignatureStaleError`
- [ ] `cd tools/sprite-gen && python3 -m pytest tests/test_signature.py -q` green
- [ ] `cd tools/sprite-gen && python3 -m pytest tests/ -q` → 221+ passed (no regression)

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_l15_fallback | 0 source sprites + fallback graph | `sig["mode"] == "fallback"`, `sig["fallback_of"] == "<target>"` | pytest |
| test_l15_point_match | 1 source sprite | `sig["mode"] == "point-match"`, bbox fields are scalars not min/max | pytest |
| test_l15_envelope | ≥2 source sprites | `sig["mode"] == "envelope"`, `bbox.height.min`/`max`/`mean` present | pytest |
| test_stale_checksum | signature + source folder mutated after compute | `SignatureStaleError` raised with refresh cmd in message | pytest |
| test_validate_ok | signature + rendered img inside envelope | `ValidationReport(ok=True, failures=[])` | pytest |
| test_validate_fail | signature + rendered img outside envelope | `ValidationReport(ok=False, failures=[...])` | pytest |

### §Examples

Canonical signature JSON shape (reproduced verbatim from handoff §3 Stage 6.2):

```json
{
  "class": "residential_small",
  "refreshed_at": "2026-04-23",
  "source_count": 18,
  "source_checksum": "sha256:<concat of source bytes>",
  "mode": "envelope",
  "fallback_of": null,
  "bbox": { "height": {"min": 33, "max": 38, "mean": 35.1}, "y0": {"min": 12, "max": 15, "mean": 13.4}, "spans_full_width": true },
  "palette": { "wall_dominant": ["..."], "roof_dominant": ["..."], "top_20pct_band": ["..."] },
  "silhouette": { "peaks_above_diamond_top": {"freq": 0.78, "px_above_mean": 2.3}, "has_pitched_roof": {"freq": 0.94} },
  "ground": { "dominant": ["..."], "variance": {"hue_stddev": 0.01, "value_stddev": 0.02} },
  "decoration_hints": { "trees_per_tile_mean": 2.4, "grass_ratio_mean": 0.55 }
}
```

L15 sample-size policy (verbatim from handoff):

- `source_count == 0` → emit fallback signature with `mode: fallback`, copy from `_fallback.json` target class.
- `source_count == 1` → `mode: point-match`; signature fields = that sprite's measured values (no envelope).
- `source_count >= 2` → `mode: envelope`; normal min/max/mean summarization.

Target module skeleton:

```python
# tools/sprite-gen/src/signature.py
from dataclasses import dataclass
from pathlib import Path
from typing import Optional
import hashlib, json

class SignatureStaleError(Exception):
    """Raised when source_checksum drifts from recomputed value."""

@dataclass
class ValidationReport:
    ok: bool
    failures: list[str]

def compute_signature(
    class_name: str,
    folder_glob: str,
    *,
    fallback_graph_path: Optional[Path] = None,
) -> dict:
    sources = sorted(Path().glob(folder_glob))
    checksum = _compute_checksum(sources)
    measurements = [_measure(p) for p in sources]
    return _summarize(class_name, measurements, checksum, fallback_graph_path)

def validate_against(
    signature: dict,
    rendered_img,
    *,
    source_folder: Optional[Path] = None,
) -> ValidationReport:
    if source_folder is not None:
        _guard_staleness(signature, source_folder)
    return _compare_envelope(signature, rendered_img)
```

### §Mechanical Steps

#### Step 1 — Author module skeleton + JSON shape + checksum helper

**Goal:** Create `tools/sprite-gen/src/signature.py` with public surface + `_compute_checksum`.

**Edits:**

- `tools/sprite-gen/src/signature.py` — new file with class + dataclass + stub functions.

**Gate:**

```bash
cd tools/sprite-gen && python3 -c "from src.signature import compute_signature, validate_against, SignatureStaleError, ValidationReport; print('ok')"
```

**STOP:** ImportError → inspect module structure.

#### Step 2 — Per-sprite measurement extractors

**Goal:** Implement `_measure_bbox`, `_measure_palette`, `_measure_silhouette`, `_measure_ground`, `_measure_decoration_hints`.

**Edits:**

- `tools/sprite-gen/src/signature.py` — add measurement helpers.
- `tools/sprite-gen/tests/test_signature.py` — unit tests with 3-sprite fixture.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_signature.py -q
```

**STOP:** Measurement helpers return None or wrong types → fix before L15 branches.

#### Step 3 — L15 branches in `_summarize`

**Goal:** Dispatch on `len(measurements)` into fallback / point-match / envelope.

**Edits:**

- `tools/sprite-gen/src/signature.py` — `_summarize(class_name, measurements, checksum, fallback_graph_path)` with 3 branches.
- `tools/sprite-gen/tests/test_signature.py` — 3 parametrized tests (one per branch) using hermetic fallback graph.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_signature.py::test_l15 -q
```

**STOP:** Any branch emits wrong `mode` → inspect L15 policy.

#### Step 4 — `validate_against` + `SignatureStaleError`

**Goal:** Ship validation + staleness.

**Edits:**

- `tools/sprite-gen/src/signature.py` — `validate_against` + `SignatureStaleError`.
- `tools/sprite-gen/tests/test_signature.py` — happy-path + fail-path + stale tests.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_signature.py -q && python3 -m pytest tests/ -q
```

**STOP:** Regression in Stage 6.1 tests → revert signature touches; signature module is additive.

**MCP hints:** none — pure module authoring.

## Open Questions (resolve before / during implementation)

1. Palette measurement — k-means over RGB pixels, or dominant-colour histogram? **Resolution:** dominant-colour histogram first; k-means deferred unless unit tests show inadequate discrimination.
2. `decoration_hints.trees_per_tile_mean` — how to detect trees without segmentation? **Resolution:** Stage 6.2 emits hints only for classes where a reference segmentation exists; otherwise field is `null`.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
