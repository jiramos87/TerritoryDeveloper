---
purpose: "TECH-712 — CLI bootstrap-variants --from-signature."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/sprite-gen-master-plan.md
task_key: T6.3.4
---
# TECH-712 — CLI `bootstrap-variants --from-signature`

> **Issue:** [TECH-712](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-23
> **Last updated:** 2026-04-23

## 1. Summary

New CLI subcommand `python3 -m src bootstrap-variants <stem> --from-signature`. Reads `tools/sprite-gen/signatures/<class>.signature.json` (class derived from the spec's `class` field), writes sensible `vary:` defaults into the named spec (e.g. `vary.roof.h_px` mined from the signature's silhouette band). Opt-in only; never auto-rewrites during render. Consumes L7.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `python3 -m src bootstrap-variants <stem> --from-signature` exits 0 when signature exists.
2. Writes `vary:` block into `tools/sprite-gen/specs/<stem>.yaml`.
3. Preserves author-authored `vary.*` keys (merge, not overwrite).
4. Exits non-zero with helpful message when signature missing.

### 2.2 Non-Goals

1. Auto-running on render — explicit CLI only.
2. Generating specs from scratch — assumes spec exists.
3. Bootstrapping non-`vary:` fields (class, composition, etc.).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Spec author | Seed a `vary:` block from class signature | Running CLI writes `vary.roof.h_px: {min, max}` into spec |
| 2 | Spec author | Don't clobber my own vary overrides | CLI merges; author keys win on conflict |

## 4. Current State

### 4.1 Domain behavior

No bootstrap helper today; authors hand-derive `vary:` ranges.

### 4.2 Systems map

- `tools/sprite-gen/src/__main__.py` — target.
- Reads: `tools/sprite-gen/signatures/<class>.signature.json`.
- Writes: `tools/sprite-gen/specs/<stem>.yaml` (in place).

### 4.3 Implementation investigation notes

YAML round-trip must preserve comments. **Decision:** use `ruamel.yaml` if already a dep; otherwise plain `yaml.safe_dump` with a note that comments are stripped on bootstrap (authors re-add if needed). Verify dep first.

## 5. Proposed Design

### 5.1 Target behavior

```bash
$ python3 -m src bootstrap-variants building_residential_small --from-signature
bootstrapped vary: for building_residential_small (from signatures/residential_small.signature.json)
  + vary.roof.h_px = {min: 6, max: 14} (derived from silhouette.peaks_above_diamond_top)
  + vary.footprint_ratio = {w: {min: 0.4, max: 0.5}} (derived from bbox.height)
preserved author-authored keys: vary.padding
```

### 5.2 Architecture / implementation

- Load spec + signature.
- Derive candidate `vary:` entries from signature fields (rules table in `_derive_vary_from_signature`).
- Merge under `variants.vary`; author keys take precedence on conflict.
- Dump back to spec path.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives |
|------|----------|-----------|--------------|
| 2026-04-23 | Merge (author wins) | Lets iterative authoring layer CLI seeds + author tweaks | Overwrite — rejected, destroys author work |
| 2026-04-23 | Comments stripped on bootstrap; doc it | Simpler dep story | Pull `ruamel.yaml` — deferred; flag in open questions |

## 7. Implementation Plan

### Phase 1 — Subparser + stem resolution

- [ ] Wire argparse.

### Phase 2 — Derive vary from signature

- [ ] Rules table: silhouette band → roof.h_px range; bbox height → footprint_ratio.d range; palette → (no vary; palette_seed handles it).

### Phase 3 — Non-destructive write

- [ ] Deep merge; author keys win.

### Phase 4 — Error handling

- [ ] Missing signature → helpful message mentioning `refresh-signatures` CLI.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Signature present | Shell | `python3 -m src bootstrap-variants building_residential_small --from-signature && grep "vary" tools/sprite-gen/specs/building_residential_small.yaml` | ≥1 hit |
| Author keys preserved | Python | unit test: spec with `vary.padding` authored; CLI run; assert padding unchanged | pytest |
| Missing signature | Shell | `python3 -m src bootstrap-variants <unknown> --from-signature; echo $?` | Exit !=0; message mentions `refresh-signatures` |

## 8. Acceptance Criteria

- [ ] CLI subcommand works with `--from-signature`.
- [ ] Author keys preserved on merge.
- [ ] Missing signature exits non-zero with helpful message.
- [ ] Does not run during `render`.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- Opt-in CLI helpers beat auto-rewrite — authors want control over when generated defaults land.

## §Plan Digest

### §Goal

Ship `python3 -m src bootstrap-variants <stem> --from-signature` that reads the class signature, derives sensible `vary:` defaults, and merges them into the named spec (author keys win). Never runs during render.

### §Acceptance

- [ ] CLI exits 0 when spec + signature both exist
- [ ] `vary:` block written into `tools/sprite-gen/specs/<stem>.yaml`
- [ ] Author-authored `vary.*` keys preserved on merge
- [ ] Exits non-zero with actionable message when signature missing
- [ ] Render path does NOT invoke bootstrap

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| test_bootstrap_writes_vary | spec with empty vary + valid signature | `vary.roof.h_px`/`vary.footprint_ratio` present | pytest + tmp_path |
| test_bootstrap_preserves_author_keys | spec with `vary.padding: {n: {min:0,max:5}}` + signature | `vary.padding` preserved after CLI | pytest |
| test_bootstrap_missing_signature | spec + deleted signature | CLI exits non-zero; message contains `refresh-signatures` | subprocess run |
| render_not_invoking_bootstrap | live spec render | spec file unchanged on disk | shell hash check |

### §Examples

```python
# tools/sprite-gen/src/__main__.py
def _cmd_bootstrap_variants(stem: str, from_signature: bool) -> int:
    if not from_signature:
        print("error: --from-signature is currently required", file=sys.stderr)
        return 2
    spec_path = Path("tools/sprite-gen/specs") / f"{stem}.yaml"
    spec = yaml.safe_load(spec_path.read_text())
    class_name = spec["class"]
    sig_path = Path("tools/sprite-gen/signatures") / f"{class_name}.signature.json"
    if not sig_path.exists():
        print(
            f"error: signature missing for class {class_name!r}. "
            f"Run: python3 -m src refresh-signatures {class_name}",
            file=sys.stderr,
        )
        return 1
    signature = json.loads(sig_path.read_text())
    derived_vary = _derive_vary_from_signature(signature)
    spec.setdefault("variants", {"count": 1, "vary": {}, "seed_scope": "palette"})
    spec["variants"].setdefault("vary", {})
    _deep_merge_preserve_author(spec["variants"]["vary"], derived_vary)
    spec_path.write_text(yaml.safe_dump(spec, sort_keys=False))
    print(f"bootstrapped vary: for {stem} (from {sig_path.name})")
    return 0
```

### §Mechanical Steps

#### Step 1 — Wire subparser

**Edits:**

- `tools/sprite-gen/src/__main__.py` — register `bootstrap-variants` subparser with `<stem>` + `--from-signature`.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m src bootstrap-variants --help
```

#### Step 2 — Implement derivation + merge

**Edits:**

- `tools/sprite-gen/src/__main__.py` — `_derive_vary_from_signature` + `_deep_merge_preserve_author`.

**Gate:**

```bash
cd tools/sprite-gen && python3 -m pytest tests/test_bootstrap_variants.py -q
```

#### Step 3 — Missing-signature error path

**Gate:**

```bash
python3 -m src bootstrap-variants building_residential_small --from-signature >/dev/null; echo $?
```

Expect 0 when signature present; 1 when absent with helpful message.

#### Step 4 — Render invariance

**Gate:**

```bash
cd tools/sprite-gen && sha256sum specs/building_residential_small.yaml > /tmp/before.txt && python3 -m src render building_residential_small && sha256sum specs/building_residential_small.yaml > /tmp/after.txt && diff /tmp/before.txt /tmp/after.txt
```

Expect no diff (render does not invoke bootstrap).

**MCP hints:** none — pure CLI + YAML edit.

## Open Questions (resolve before / during implementation)

1. YAML comment preservation — do we require `ruamel.yaml`? **Resolution:** defer; document that comments are stripped on bootstrap; authors can re-add.
2. Which signature fields map to which `vary:` axes? **Resolution:** minimum viable mapping (silhouette → roof.h_px; bbox.height → footprint_ratio.d); expand in later stages.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
