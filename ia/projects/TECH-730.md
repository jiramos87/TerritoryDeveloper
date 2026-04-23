---
purpose: "TECH-730 — spec.py preset key loader: resolves preset: <name>, merges author overrides, raises on missing."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.6.1
---
# TECH-730 — Loader: `preset: <name>` inject + author override

> **Issue:** [TECH-730](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Introduce a top-level `preset: <name>` key in sprite-gen spec YAML. When present, the loader reads `tools/sprite-gen/presets/<name>.yaml` as a base spec and merges author-supplied fields on top (author wins per-field). Missing preset → `SpecError` listing valid preset names. Consumes lock **L13**.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `preset: <name>` resolves to `tools/sprite-gen/presets/<name>.yaml` and parses as base.
2. Author-provided fields override the preset per-key (deep merge for nested dicts; scalar replace elsewhere).
3. Missing preset → `SpecError` whose message lists all valid preset names on disk.

### 2.2 Non-Goals

1. `vary:` block merge rule — TECH-731.
2. Seed preset files — TECH-732/733/734.
3. DAS addendum — TECH-736.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Spec author | Bootstrap a spec from `suburban_house_with_yard` | `preset:` key resolves; sprite renders |
| 2 | Spec author | Override preset field | Author `output.name` wins over preset |
| 3 | Spec author | Typo a preset name | `SpecError` prints valid choices |

## 4. Current State

### 4.1 Domain behavior

`spec.py` parses a single YAML top-to-bottom with no cross-file resolution.

### 4.2 Systems map

- `tools/sprite-gen/src/spec.py` — loader entry (`load_spec(path) → Spec`).
- `tools/sprite-gen/presets/` — new directory consumed here; populated by TECH-732..734.

### 4.3 Implementation investigation notes

Deep-merge must respect dict-vs-scalar asymmetry (author scalar replaces preset dict, author dict deep-merges into preset dict). `vary:` is the one block that needs special treatment — leave it to TECH-731.

## 5. Proposed Design

### 5.1 Target behavior

```yaml
# author spec
preset: suburban_house_with_yard
id: my_house_01
output:
  name: my_house_01.png
```

Loader reads `presets/suburban_house_with_yard.yaml`, merges `id` + `output.name` from author; returns resolved `Spec`.

### 5.2 Architecture / implementation

- `load_spec(path)` detects top-level `preset:` key.
- `_load_preset(name)` reads `tools/sprite-gen/presets/<name>.yaml`; missing → `SpecError` with `list(glob('presets/*.yaml'))` names.
- `_deep_merge(base, overlay)` — dict deep-merge; scalar / list replace.
- `vary:` block passthrough (TECH-731 hooks in).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | Top-level `preset:` key (not `$extend`) | Matches DAS vocabulary; short + scannable | `extends:` / `$ref` — rejected as noisier |
| 2026-04-23 | Presets live in `tools/sprite-gen/presets/` | Co-located with generator; no cross-repo path | `~/.sprite-gen/presets` — rejected, per-user state is a smell |

## 7. Implementation Plan

### Phase 1 — Detect `preset:` key

### Phase 2 — Load base YAML + deep-merge overrides

### Phase 3 — Error path with valid-names listing

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Preset resolves | Python | `pytest tests/test_preset_system.py::test_preset_resolves -q` | TECH-735 |
| Author overrides win | Python | `pytest tests/test_preset_system.py::test_author_override -q` | TECH-735 |
| Missing preset error | Python | `pytest tests/test_preset_system.py::test_missing_preset_lists_valid -q` | TECH-735 |

## 8. Acceptance Criteria

- [ ] `preset: <name>` resolves to `tools/sprite-gen/presets/<name>.yaml`.
- [ ] Author-provided fields override preset fields per-key.
- [ ] Missing preset raises `SpecError` listing valid preset names on disk.
- [ ] Unit tests cover resolve + override + missing-preset error (in TECH-735).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|   |             |            |            |

## 10. Lessons Learned

- The preset key is the highest-leverage ergonomic lever for sprite-gen — a clean loader here cascades into short author specs for the rest of the stack.

## §Plan Digest

### §Goal

Allow an author spec to name a preset via `preset: <name>`; the loader substitutes the preset as base and layers author overrides on top, erroring early on typos.

### §Acceptance

- [ ] Spec with `preset: suburban_house_with_yard` + no author overrides resolves and renders
- [ ] Spec with `preset: X` + author `output.name: foo.png` picks author's `output.name`
- [ ] Spec with `preset: not_real` raises `SpecError` whose message contains every `.yaml` stem in `presets/`
- [ ] Deep merge — author `output: {name: foo}` merges into preset `output: {name, pivot}` preserving preset `pivot`
- [ ] `pytest tools/sprite-gen/tests/test_preset_system.py -q` green for this task's subset

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_preset_resolves | spec `{preset: X}` + preset file | spec fields == preset fields | pytest |
| test_author_override | spec `{preset: X, output.name: foo}` | `output.name == foo`, rest from preset | pytest |
| test_author_deep_merge | preset `output: {name, pivot}` + author `output: {name}` | pivot from preset, name from author | pytest |
| test_missing_preset_lists_valid | spec `{preset: ghost}` + two real presets | SpecError msg contains both names | pytest |

### §Examples

```python
# tools/sprite-gen/src/spec.py (excerpt)
from pathlib import Path
import yaml

_PRESET_DIR = Path(__file__).parent.parent / "presets"

class SpecError(Exception): ...

def _load_preset(name: str) -> dict:
    path = _PRESET_DIR / f"{name}.yaml"
    if not path.exists():
        valid = sorted(p.stem for p in _PRESET_DIR.glob("*.yaml"))
        raise SpecError(f"unknown preset '{name}'. valid: {valid}")
    return yaml.safe_load(path.read_text())

def _deep_merge(base: dict, overlay: dict) -> dict:
    out = dict(base)
    for k, v in overlay.items():
        if isinstance(v, dict) and isinstance(out.get(k), dict):
            out[k] = _deep_merge(out[k], v)
        else:
            out[k] = v
    return out

def load_spec(path: Path) -> dict:
    data = yaml.safe_load(Path(path).read_text())
    if "preset" in data:
        base = _load_preset(data.pop("preset"))
        data = _deep_merge(base, data)
    return data
```

### §Mechanical Steps

#### Step 1 — `_load_preset` + error path

**Edits:**

- `tools/sprite-gen/src/spec.py` — add `_load_preset(name)` with valid-names listing on miss.

**Gate:**

```bash
cd tools/sprite-gen && python3 -c "from src.spec import _load_preset; _load_preset('ghost')" 2>&1 | grep -i "unknown preset"
```

#### Step 2 — `_deep_merge` helper

**Edits:**

- Same file — `_deep_merge(base, overlay)`; dict deep-merge, scalar replace.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_preset_system.py::test_author_deep_merge -q
```

#### Step 3 — Wire into `load_spec`

**Edits:**

- Same file — detect top-level `preset:` in `load_spec`; pop key; merge.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_preset_system.py -q -k "resolve or override"
```

**MCP hints:** none.

## Open Questions (resolve before / during implementation)

1. Does the author-supplied `preset:` itself survive into the resolved spec dict? **Resolution:** no — `pop` it before merge so downstream validators don't see an unknown top-level key.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
