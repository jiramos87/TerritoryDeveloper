---
purpose: "TECH-723 — curate.py log-promote subcommand appending JSONL rows to curation/promoted.jsonl."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.5.1
---
# TECH-723 — curate.py log-promote → promoted.jsonl

> **Issue:** [TECH-723](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Add `log-promote <variant>` subcommand to `tools/sprite-gen/src/curate.py`. On invocation, it appends one JSON row to `curation/promoted.jsonl` carrying the rendered variant path, the sampled `vary:` values used to produce it, and measured bbox + palette stats taken from the rendered image. Idempotent append; prior rows untouched.

Verb `log-promote` (not `promote`) disambiguates from the existing `promote` subcommand (TECH-179) which copies PNG to `Assets/Sprites/Generated` + writes Unity `.meta` + optional catalog push — a different concern (shipping) from the curator feedback log (this task).

## 2. Goals and Non-Goals

### 2.1 Goals

1. `log-promote <variant>` appends exactly one JSON row to `curation/promoted.jsonl`.
2. Row schema: `{variant_path, vary_values, bbox, palette_stats, timestamp}`.
3. Idempotent: re-invoking on the same variant appends a new row (history preserved), never mutates prior rows.

### 2.2 Non-Goals

1. Rejection path — TECH-724.
2. Aggregator that consumes the file — TECH-725.
3. Shipping PNG to Unity or catalog push — already handled by existing `promote` verb (TECH-179).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Curator | Log a good variant | CLI prints path; row appears in `promoted.jsonl` |
| 2 | Sprite-gen dev | Row carries enough data to tighten envelope | All schema fields populated |
| 3 | Repo guardian | History preservation | Prior rows byte-identical after new append |

## 4. Current State

### 4.1 Domain behavior

`curate.py` today exposes `promote` (PNG → Unity ship + catalog, TECH-179) + `reject` (glob-delete of `out/{archetype}_v*.png`). No JSONL feedback log.

### 4.2 Systems map

- `tools/sprite-gen/src/curate.py` — CLI entry; `log-promote` subcommand is new.
- `curation/promoted.jsonl` — new append-only log file.

### 4.3 Implementation investigation notes

Row-writer + measurement helpers here are shared by TECH-724 (log-reject); keep them in a module-level helper so TECH-724 reuses directly.

## 5. Proposed Design

### 5.1 Target behavior

```bash
$ python3 -m src.curate log-promote renders/residential_small__variant3.png
[log-promote] appended row #7 → curation/promoted.jsonl
```

### 5.2 Architecture / implementation

- New `log-promote` subparser (sibling of existing `promote` / `reject`).
- `_measure_variant(path)` → `{bbox, palette_stats}` via Pillow + existing palette module.
- `_load_vary_values(path)` → parse `variant_idx` from filename (e.g. `residential_small__variant3.png` → `3`), load spec YAML, invoke `compose.sample_variant(spec, variant_idx)`, diff sampled-vs-base spec to extract the concrete `vary:` values that went into that render. Zero extra files on disk; leans on TECH-711's already-deterministic `sample_variant` (split seeds + `seed_scope`).
- `_append_jsonl(row, target_path)` → open with `a`, write `json.dumps(row)+"\n"`; create file if missing.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | Append-only JSONL | Trivial to diff + aggregate; history preserved by construction | SQLite — rejected, over-engineering |
| 2026-04-23 | `vary_values` re-sampled via `compose.sample_variant(spec, variant_idx)` | TECH-711 already made `sample_variant` deterministic (split seeds + `seed_scope`); re-sampling a deterministic function from the same inputs produces the same output — no drift possible. Zero extra files on disk; no Stage 6.3 retro-fit. | Emit `<variant>.meta.json` sidecar at render time — rejected, requires a Stage 6.3 retro-fit (sidecar writer never shipped there) and duplicates info already recoverable from `(spec, palette_seed, geometry_seed, variant_idx)`. |
| 2026-04-23 | Verb name `log-promote` (not `promote`) | Avoid collision with existing `promote` (TECH-179 PNG→Unity ship); keep shipping + logging as orthogonal curator verbs | `promote` — rejected, collision; `curate-accept` — rejected, redundant given parent module is `curate` |

## 7. Implementation Plan

### Phase 1 — CLI subcommand scaffold

### Phase 2 — Measurement helpers (bbox + palette stats)

### Phase 3 — JSONL writer + idempotency test

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Row appended | Python | `pytest tests/test_curate.py::test_log_promote_appends_row -q` | File gains one row |
| Schema populated | Python | `pytest tests/test_curate.py::test_log_promote_row_schema -q` | All fields non-null |
| Idempotency | Python | `pytest tests/test_curate.py::test_log_promote_history_preserved -q` | Re-invoke preserves prior rows |

## 8. Acceptance Criteria

- [ ] `log-promote <variant>` appends one JSON row to `curation/promoted.jsonl`.
- [ ] Row carries variant path + sampled `vary:` values + measured bbox / palette stats.
- [ ] Idempotent append; prior rows untouched.
- [ ] Unit test covers row shape + idempotency.
- [ ] Existing `promote` / `reject` verbs (TECH-179) keep current behaviour — no regression.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|   |             |            |            |

## 10. Lessons Learned

- Append-only JSONL is the smallest possible persistence surface for curation logs — easy to diff, easy to aggregate, impossible to silently overwrite.
- CLI verb-space collisions are cheaper to resolve at spec-review time than at implementation time — `log-` prefix carves an orthogonal namespace for feedback-log verbs.

## §Plan Digest

### §Goal

Log a rendered variant into a curator-approved JSONL feed that downstream aggregators can tighten the signature envelope against. Verb named `log-promote` to stay orthogonal to existing `promote` (PNG→Unity shipping, TECH-179).

### §Acceptance

- [ ] `python3 -m src.curate log-promote <variant>` exits 0 and appends exactly one row to `curation/promoted.jsonl`
- [ ] Row contains `variant_path`, `vary_values`, `bbox`, `palette_stats`, `timestamp` — all non-null
- [ ] Re-running on the same variant appends a new row; prior rows byte-identical
- [ ] `curation/promoted.jsonl` is auto-created if absent
- [ ] Existing `promote` / `reject` subcommands retain TECH-179 behaviour — no regression
- [ ] `pytest tools/sprite-gen/tests/test_curate.py -q` green

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_log_promote_appends_row | rendered variant + empty log | file has exactly 1 row | pytest |
| test_log_promote_row_schema | rendered variant | all schema keys populated | pytest |
| test_log_promote_history_preserved | 2× invocations | 2 rows; first row byte-identical | pytest |
| test_log_promote_creates_log_file | no existing log | file created on first call | pytest |

### §Examples

```python
# tools/sprite-gen/src/curate.py (excerpt)
import json, re, time
from pathlib import Path
from .compose import sample_variant
from .spec import load_spec

_PROMOTED = Path("curation/promoted.jsonl")
_VARIANT_RE = re.compile(r"(.+)__variant(\d+)\.png$")

def _resolve_spec_and_idx(variant_path: str) -> tuple[dict, int]:
    """Map `renders/{archetype}__variant{idx}.png` → (spec_dict, idx)."""
    name = Path(variant_path).name
    m = _VARIANT_RE.match(name)
    if not m:
        raise ValueError(f"not a variant PNG: {variant_path}")
    archetype, idx = m.group(1), int(m.group(2))
    return load_spec(Path(f"specs/{archetype}.yaml")), idx

def _load_vary_values(variant_path: str) -> dict:
    """Re-sample via deterministic `sample_variant` — zero extra files."""
    spec, idx = _resolve_spec_and_idx(variant_path)
    sampled = sample_variant(spec, idx)
    return _diff_vary_leaves(spec, sampled)

def _append_jsonl(row: dict, target: Path) -> None:
    target.parent.mkdir(parents=True, exist_ok=True)
    with target.open("a") as f:
        f.write(json.dumps(row) + "\n")

def log_promote(variant_path: str) -> None:
    bbox, palette_stats = _measure_variant(variant_path)
    vary_values = _load_vary_values(variant_path)
    row = {
        "variant_path": variant_path,
        "vary_values": vary_values,
        "bbox": bbox,
        "palette_stats": palette_stats,
        "timestamp": time.time(),
    }
    _append_jsonl(row, _PROMOTED)
    print(f"[log-promote] appended → {_PROMOTED}")
```

### §Mechanical Steps

#### Step 1 — CLI subparser

**Edits:**

- `tools/sprite-gen/src/curate.py` — add `log-promote` subparser (sibling of existing `promote` + `reject`).

**Gate:**

```bash
cd tools/sprite-gen && python3 -m src.curate log-promote --help
```

#### Step 2 — Measurement + re-sample helpers

**Edits:**

- Same file — `_measure_variant`, `_resolve_spec_and_idx` (filename parser), `_load_vary_values` (re-sample via `compose.sample_variant`), `_diff_vary_leaves` (pull concrete values from sampled spec).

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_curate.py::test_log_promote_row_schema -q
```

#### Step 3 — JSONL writer + idempotency

**Edits:**

- Same file — `_append_jsonl`; wire to `log_promote` entry.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_curate.py -q
```

**MCP hints:** none.

## Open Questions (resolve before / during implementation)

1. ~~Sidecar metadata file format — confirm from Stage 6.3 (TECH-711 variant loop).~~ **Resolved 2026-04-23:** Stage 6.3 never shipped a sidecar (scope check: master plan lines 893–1083; skill gap — master-plan-new / stage-file-planner didn't cross-check that Stage 6.5's TECH-723 depends on sidecar that Stage 6.3 doesn't emit — logged for `/skill-train`). TECH-723 switches to re-sample via `compose.sample_variant(spec, variant_idx)` — deterministic, zero-file. Filename pattern: `{archetype}__variant{idx}.png`; parser: `re.search(r"__variant(\d+)\.png$", path)`.
2. Spec lookup — given a variant PNG path, how to find the owning spec YAML? **Resolution:** path convention is `renders/{archetype}__variant{idx}.png`; spec lives at `specs/{archetype}.yaml`. Add `_resolve_spec_path(variant_path)` helper that mirrors this mapping.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
