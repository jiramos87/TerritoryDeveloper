---
purpose: "TECH-705 — CLI refresh-signatures + signatures/ dir scaffold + residential_small bootstrap."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.2.2
---
# TECH-705 — CLI `refresh-signatures` + `signatures/` scaffold + residential_small bootstrap

> **Issue:** [TECH-705](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

Wire a `python3 -m src refresh-signatures [class?]` CLI that generates/refreshes `tools/sprite-gen/signatures/<class>.signature.json` from reference sprites in `Assets/Sprites/<class>/`. Scaffolds the `signatures/` directory with `_fallback.json` (fallback-class graph for L15) and a bootstrap `residential_small.signature.json` (≥2 samples → `mode: envelope`). Commits all JSON so CI reads the same snapshot.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `python3 -m src refresh-signatures` regenerates every `signatures/*.signature.json`.
2. `python3 -m src refresh-signatures <class>` regenerates one class.
3. `tools/sprite-gen/signatures/_fallback.json` authored with at least `residential_small → residential_row` mapping.
4. `tools/sprite-gen/signatures/residential_small.signature.json` generated + committed (envelope mode).

### 2.2 Non-Goals

1. Signature module internals — TECH-704.
2. Per-sprite opt-out flag — TECH-706.
3. Parametrized calibration tests — TECH-707.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Sprite-gen dev | Regenerate signatures after adding reference sprites | `python3 -m src refresh-signatures residential_small` rewrites JSON; diff visible |
| 2 | CI | Read canonical signatures from git | `signatures/residential_small.signature.json` present on `master` |

## 4. Current State

### 4.1 Domain behavior

No `signatures/` dir exists today; no CLI entry point. Calibration is per-test hand-coded in `tests/test_scale_calibration.py`.

### 4.2 Systems map

- `tools/sprite-gen/src/__main__.py` (or `src/cli.py`) — argparse entry.
- `tools/sprite-gen/signatures/` — new dir (this task creates it).
- Depends on `src/signature.py` from TECH-704.
- Reference sprites: `Assets/Sprites/residential_small/*.png` (ingestion source).

### 4.3 Implementation investigation notes (optional)

`_fallback.json` only needs the classes we might hit with 0 samples today — safe to seed with `residential_small → residential_row` and extend later.

## 5. Proposed Design

### 5.1 Target behavior

```bash
$ python3 -m src refresh-signatures
refreshed residential_small  (envelope, source_count=18)

$ python3 -m src refresh-signatures residential_small
refreshed residential_small  (envelope, source_count=18)

$ cat tools/sprite-gen/signatures/residential_small.signature.json
{ "class": "residential_small", "mode": "envelope", ... }
```

### 5.2 Architecture / implementation

- CLI subcommand parses `[class?]`; calls `signature.compute_signature(class_name, f"Assets/Sprites/{class_name}/*.png")`.
- Writes via `json.dump(sig, f, indent=2, sort_keys=True)` for stable git diffs.
- When no class arg provided, iterates every `signatures/*.signature.json` and refreshes each.
- `_fallback.json` is a simple `{class_name: fallback_target}` dict; `compute_signature` reads it when `source_count == 0`.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | Commit `*.signature.json` to git | CI needs a deterministic snapshot; avoids "signatures drift between local and CI" | Generate at build time — rejected, adds runtime dep on reference catalog |
| 2026-04-23 | `_fallback.json` is flat dict, not nested graph | Two-deep chains (residential_small → residential_row → residential_tall) resolved on consumer side | Full DAG — rejected, overkill for 9-class taxonomy |

## 7. Implementation Plan

### Phase 1 — CLI subcommand

- [ ] Wire `refresh-signatures` into `src/__main__.py` argparse.
- [ ] Invoke `signature.compute_signature` + write JSON.

### Phase 2 — `signatures/` dir + `_fallback.json`

- [ ] `mkdir tools/sprite-gen/signatures/`.
- [ ] Author `_fallback.json` with `residential_small → residential_row` (extensible).

### Phase 3 — Bootstrap `residential_small.signature.json`

- [ ] Run `python3 -m src refresh-signatures residential_small` against live `Assets/Sprites/residential_small/*.png`.
- [ ] Verify `mode: envelope`, `source_count >= 2`, commit.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| CLI one-class | Shell | `python3 -m src refresh-signatures residential_small && test -f tools/sprite-gen/signatures/residential_small.signature.json` | Exit 0 |
| CLI all-classes | Shell | `python3 -m src refresh-signatures` | Rewrites every class in `signatures/` |
| Envelope mode | JSON | `jq -r .mode tools/sprite-gen/signatures/residential_small.signature.json` | `envelope` |
| Full suite | Python | `cd tools/sprite-gen && python3 -m pytest tests/ -q` | 221+ green |

## 8. Acceptance Criteria

- [ ] CLI subcommand works with + without class arg.
- [ ] `signatures/_fallback.json` committed.
- [ ] `signatures/residential_small.signature.json` committed with `mode: envelope`.
- [ ] Full pytest green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- Committed JSON snapshots beat runtime regeneration — pin them, diff them, version them.

## §Plan Digest

### §Goal

Ship operator surface for signatures: one CLI entry (`python3 -m src refresh-signatures [class?]`), a `signatures/` dir with `_fallback.json` fallback graph, and a committed `residential_small.signature.json` so TECH-707 parametrize has a real envelope to assert against.

### §Acceptance

- [ ] `python3 -m src refresh-signatures` (no args) refreshes every signature in `signatures/`
- [ ] `python3 -m src refresh-signatures residential_small` refreshes one class
- [ ] `tools/sprite-gen/signatures/_fallback.json` committed with `residential_small → residential_row`
- [ ] `tools/sprite-gen/signatures/residential_small.signature.json` committed, `mode == "envelope"`
- [ ] `cd tools/sprite-gen && python3 -m pytest tests/ -q` → 221+ passed

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_cli_one_class | `python3 -m src refresh-signatures residential_small` | exit 0, JSON written | shell in CI |
| test_cli_all_classes | `python3 -m src refresh-signatures` | exit 0, every signature refreshed | shell in CI |
| test_fallback_json_shape | `signatures/_fallback.json` | valid JSON dict, contains `residential_small` key | `python -c "json.load(...)"` |
| test_bootstrap_envelope | `signatures/residential_small.signature.json` | `mode == "envelope"`, `source_count >= 2` | pytest |

### §Examples

`_fallback.json` (authored):

```json
{
  "residential_small": "residential_row",
  "residential_light": "residential_small"
}
```

CLI entry shape:

```python
# tools/sprite-gen/src/__main__.py (or src/cli.py)
def _cmd_refresh_signatures(class_arg: Optional[str]) -> int:
    sig_dir = Path("tools/sprite-gen/signatures")
    fallback = sig_dir / "_fallback.json"
    targets = [class_arg] if class_arg else _existing_classes(sig_dir)
    for cls in targets:
        sig = compute_signature(
            cls,
            f"Assets/Sprites/{cls}/*.png",
            fallback_graph_path=fallback,
        )
        out_path = sig_dir / f"{cls}.signature.json"
        out_path.write_text(json.dumps(sig, indent=2, sort_keys=True) + "\n")
        print(f"refreshed {cls}  ({sig['mode']}, source_count={sig['source_count']})")
    return 0
```

### §Mechanical Steps

#### Step 1 — Wire CLI subcommand

**Goal:** Add `refresh-signatures` to argparse dispatcher.

**Edits:**

- `tools/sprite-gen/src/__main__.py` — register subparser; implement `_cmd_refresh_signatures`.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m src refresh-signatures --help
```

**STOP:** `ModuleNotFoundError: src.signature` → TECH-704 not in tree; confirm dep.

#### Step 2 — Scaffold `signatures/` + `_fallback.json`

**Goal:** Create dir + author fallback graph.

**Edits:**

- Create `tools/sprite-gen/signatures/` directory.
- Write `tools/sprite-gen/signatures/_fallback.json`.

**Gate:**

```bash
python3 -c "import json; json.load(open('tools/sprite-gen/signatures/_fallback.json'))"
```

**STOP:** JSON parse error → fix syntax before bootstrap.

#### Step 3 — Bootstrap `residential_small.signature.json`

**Goal:** Run CLI against live catalog; commit output.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m src refresh-signatures residential_small && jq -r .mode signatures/residential_small.signature.json
```

Expect: `envelope`.

**STOP:** `mode == "point-match"` → check `Assets/Sprites/residential_small/` has ≥2 PNGs; `mode == "fallback"` → 0 PNGs, investigate catalog path.

#### Step 4 — Full-suite regression

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/ -q
```

**STOP:** Any red outside signature territory → revert CLI wiring.

**MCP hints:** none — pure CLI + file scaffold.

## Open Questions (resolve before / during implementation)

1. Does `Assets/Sprites/residential_small/*.png` exist today with ≥2 samples? **Resolution:** verify pre-implementation; if not, seed catalog with 2 reference PNGs or adjust glob pattern.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
