---
purpose: "TECH-706 — Spec loader include_in_signature per-sprite override."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.2.3
---
# TECH-706 — Spec loader `include_in_signature` per-sprite override

> **Issue:** [TECH-706](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Extend `tools/sprite-gen/src/spec.py` to accept an optional top-level boolean `include_in_signature` (default `true`). The signature refresh pipeline (TECH-705) must skip sprites whose source YAML opts out via `include_in_signature: false`. Back-compat by construction — any spec without the key behaves exactly as today.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `load_spec` surfaces the flag (default `true`).
2. `compute_signature` (TECH-704) filters source sprites via `load_spec(sprite_yaml).include_in_signature`.
3. Existing specs (no flag) produce byte-identical render output.

### 2.2 Non-Goals

1. Per-sprite tagging at the PNG level — YAML-only.
2. Retroactive flag migration — new specs opt in as needed; older specs stay at default `true`.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Catalog curator | Keep an experimental spec out of the signature envelope | `include_in_signature: false` excludes from `refresh-signatures` ingestion |

## 4. Current State

### 4.1 Domain behavior

`load_spec` returns a dict with top-level `class`, `footprint`, `composition`, etc. No `include_in_signature` field today.

### 4.2 Systems map

- `tools/sprite-gen/src/spec.py::load_spec` (modify).
- `tools/sprite-gen/src/signature.py::compute_signature` (consumer — reads the flag when iterating source sprites).

### 4.3 Implementation investigation notes

Signature ingestion reads PNGs, not YAMLs, today. Implementation nuance: we need a sibling-YAML lookup per PNG (`Assets/Sprites/<class>/<stem>.yaml` or a manifest). **Decision:** signature pipeline reads a manifest map `Assets/Sprites/<class>/manifest.json` (if present) mapping PNG stem → spec path; otherwise all PNGs default to included. Implementation of the manifest lives in TECH-705 (CLI) but the flag is authored in the YAML and surfaced by this task.

## 5. Proposed Design

### 5.1 Target behavior

```yaml
# specs/building_residential_experimental.yaml
class: residential_small
include_in_signature: false
footprint: [1, 1]
composition:
  - ...
```

`python3 -m src render building_residential_experimental` renders normally.
`python3 -m src refresh-signatures residential_small` skips it.

### 5.2 Architecture / implementation

- `spec.py` schema: optional key `include_in_signature: bool` (default `True`).
- `load_spec` returns the parsed dict; callers read the key.
- No enforcement at render time (render ignores the flag).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | Flag at top level, not under `building:` | Signature-ingestion is orthogonal to building shape | Nest under `build:` — rejected, confuses scope |
| 2026-04-23 | Default `True`; no forced migration | Zero churn for existing specs | Default `False` and opt-in — rejected, breaks implicit assumption |

## 7. Implementation Plan

### Phase 1 — Loader surface

- [ ] Add `include_in_signature` to the spec schema (default True).
- [ ] Unit test: load sample with + without the flag.

### Phase 2 — Wire into signature ingestion

- [ ] `compute_signature` filter — defer to TECH-705 (manifest lookup lives there) but document the contract here.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Loader default | Python | `pytest tests/test_spec.py::test_include_in_signature_default -q` | Returns `True` when key absent |
| Loader explicit false | Python | `pytest tests/test_spec.py::test_include_in_signature_false -q` | Returns `False` |
| Render unchanged | Python | `cd tools/sprite-gen && python3 -m pytest tests/ -q` | 221+ green |

## 8. Acceptance Criteria

- [ ] `load_spec` surfaces `include_in_signature` (default `True`).
- [ ] Unit tests for default + explicit `False`.
- [ ] Full suite green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- Keep flags that don't affect render output clearly named (`include_in_signature`) so future readers don't spelunk composer paths looking for behaviour.

## §Plan Digest

### §Goal

Surface optional top-level `include_in_signature: bool` (default `True`) in `tools/sprite-gen/src/spec.py::load_spec` so TECH-705's signature refresh can skip opted-out sprites without touching render behaviour.

### §Acceptance

- [ ] `load_spec(path)["include_in_signature"]` returns `True` when key absent
- [ ] `load_spec(path)["include_in_signature"]` returns `False` when explicitly set
- [ ] `cd tools/sprite-gen && python3 -m pytest tests/test_spec.py -q` → green
- [ ] `cd tools/sprite-gen && python3 -m pytest tests/ -q` → 221+ passed (no render regression)

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_include_in_signature_default | spec YAML without key | `spec["include_in_signature"] is True` | pytest |
| test_include_in_signature_explicit_false | spec YAML with `include_in_signature: false` | `spec["include_in_signature"] is False` | pytest |
| test_render_unchanged | existing specs | render bytes identical to pre-change | parametrized over live specs |

### §Examples

```yaml
# specs/building_residential_experimental.yaml
class: residential_small
include_in_signature: false
footprint: [1, 1]
composition:
  - primitive: iso_cube
    w: 1
    d: 1
    h_px: 28
    material: wall_brick_red
```

Loader excerpt:

```python
# tools/sprite-gen/src/spec.py
def load_spec(path: Path) -> dict:
    data = yaml.safe_load(Path(path).read_text())
    data.setdefault("include_in_signature", True)
    return data
```

### §Mechanical Steps

#### Step 1 — Add schema field with default

**Goal:** `load_spec` returns dict with `include_in_signature` key guaranteed.

**Edits:**

- `tools/sprite-gen/src/spec.py` — `data.setdefault("include_in_signature", True)` after yaml load.

**Gate:**

```bash
cd tools/sprite-gen && python3 -c "from src.spec import load_spec; s = load_spec('specs/building_residential_small.yaml'); assert s['include_in_signature'] is True; print('ok')"
```

#### Step 2 — Unit tests

**Goal:** Cover default + explicit `False`.

**Edits:**

- `tools/sprite-gen/tests/test_spec.py` — two new tests (author sample specs inline or use tmp_path).

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_spec.py -q
```

#### Step 3 — Full suite regression

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/ -q
```

**STOP:** Any render test red → unlikely (non-render field), but revert and investigate before merge.

**MCP hints:** none — loader-only.

## Open Questions (resolve before / during implementation)

1. Do we want the flag reachable via the CLI render path too (e.g. warn when rendering an opted-out spec)? **Resolution:** no — render ignores the flag; only signature pipeline cares.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
