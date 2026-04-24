### Stage 6.2 — Art Signatures per class


**Status:** Final — closed 2026-04-23 (5 tasks **TECH-704**..**TECH-708** archived via `959ab1a`). **Locks consumed:** L2 (Calibration = summarized Art Signatures per class; runtime never reads raw sprites), L3 (signature JSON carries `source_checksum`; stale raises actionable refresh), L4 (Spec YAML `include_in_signature: false` per-sprite override), L15 (sample-size policy: 0 → fallback, 1 → point-match, ≥2 → envelope).

**Objectives:** Replace ad-hoc scale-calibration with per-class calibration signatures committed under `tools/sprite-gen/signatures/<class>.signature.json`. Build a `src/signature.py` module that (a) extracts bbox / palette / silhouette / ground / decoration-hints summaries from `Assets/Sprites/<class>/*.png`, (b) validates generator output against the envelope, (c) fails fast with `SignatureStaleError` when `source_checksum` drifts. Introduce CLI `refresh-signatures [class?]` to regenerate summaries on demand. Replace `test_scale_calibration.py` with parametrized `test_signature_calibration.py` once `residential_small.signature.json` lands.

**Exit:**

- `tools/sprite-gen/src/signature.py` — `compute_signature(class_name, folder_glob) -> dict`; `validate_against(signature, rendered_img) -> ValidationReport`; `SignatureStaleError` on checksum mismatch; implements L15 sample-size branches (`source_count == 0 → mode: fallback`, `== 1 → point-match`, `>= 2 → envelope`).
- `tools/sprite-gen/src/__main__.py` (or `src/cli.py`) — new subcommand `python3 -m src refresh-signatures [class?]`; writes / rewrites `signatures/<class>.signature.json`.
- `tools/sprite-gen/signatures/` — dir scaffold with `_fallback.json` fallback-class graph (e.g. `residential_small → residential_row`) and `residential_small.signature.json` bootstrap (computed from `Assets/Sprites/residential_small/*.png`; ≥2 samples → envelope mode).
- `tools/sprite-gen/src/spec.py` — accepts `include_in_signature: false` on spec-level override (per-sprite exclusion from signature ingestion).
- `tools/sprite-gen/tests/test_signature_calibration.py` — parametrized over every `signatures/*.signature.json`; asserts `validate_against(signature, compose_sprite(load_spec(<class canonical spec>)))` returns `.ok == True`.
- `tools/sprite-gen/tests/test_scale_calibration.py` deprecated (file deleted or reduced to a `pytest.mark.skip("superseded by test_signature_calibration")` stub) once `residential_small.signature.json` lands; TECH-702's tight-bound assertions absorbed into the signature envelope.
- `docs/sprite-gen-art-design-system.md` §2.6 new pointer block — "Calibration signatures are the canonical runtime calibration source; see `tools/sprite-gen/signatures/` + `src/signature.py`."
- `pytest tools/sprite-gen/tests/` exits 0 — 221+ tests green (TECH-703 baseline + at least one new signature calibration case).

**Tasks:**


| Task   | Name                                                                       | Issue        | Status | Intent                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      |
| ------ | -------------------------------------------------------------------------- | ------------ | ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| T6.2.1 | Signature module core (`src/signature.py`)                                 | **TECH-704** | Done   | New module with `compute_signature(class_name, folder_glob) -> dict`, `validate_against(signature, rendered_img) -> ValidationReport`, `SignatureStaleError`. JSON shape per handoff §3 Stage 6.2 spec (class / refreshed_at / source_count / source_checksum / mode / bbox / palette / silhouette / ground / decoration_hints). L15 sample-size policy: `0 → mode: fallback` (copy from `_fallback.json` target class), `1 → mode: point-match` (single-sprite values), `>=2 → mode: envelope` (min/max/mean). L3 staleness guard: `validate_against` recomputes checksum and raises `SignatureStaleError("signature stale — run python3 -m src refresh-signatures <class>")` on mismatch. |
| T6.2.2 | CLI `refresh-signatures` + `signatures/` scaffold                          | **TECH-705** | Done   | New subcommand `python3 -m src refresh-signatures [class?]`; writes or rewrites `tools/sprite-gen/signatures/<class>.signature.json`. Create `tools/sprite-gen/signatures/` dir with `_fallback.json` (fallback-class graph per L15), plus bootstrap `residential_small.signature.json` computed from `Assets/Sprites/residential_small/*.png` (≥2 samples → envelope mode). Committed to git.                                                                                                                                                                                                                                                                                              |
| T6.2.3 | Spec loader `include_in_signature: false` override                         | **TECH-706** | Done   | `tools/sprite-gen/src/spec.py` — accept optional top-level `include_in_signature: <bool>` (default `true`) on YAML specs. Signature refresh skips sprites whose source YAML opts out. Default preserves existing behaviour; no migration needed.                                                                                                                                                                                                                                                                                                                                                                                                                                            |
| T6.2.4 | `tests/test_signature_calibration.py` + retire `test_scale_calibration.py` | **TECH-707** | Done   | New parametrized test iterating every `signatures/*.signature.json`; runs `validate_against(signature, compose_sprite(load_spec(<class canonical spec>)))` and asserts `.ok == True`. Once `residential_small.signature.json` lands + the parametrized case is green, delete `tests/test_scale_calibration.py` (or replace with `pytest.mark.skip("superseded by test_signature_calibration")`). Full suite still exits 0.                                                                                                                                                                                                                                                                  |
| T6.2.5 | DAS §2.6 pointer block                                                     | **TECH-708** | Done   | `docs/sprite-gen-art-design-system.md` — add §2.6 "Calibration signatures are the canonical runtime calibration source. See `tools/sprite-gen/signatures/` + `src/signature.py`." Brief; forward-pointer only.                                                                                                                                                                                                                                                                                                                                                                                                                                                                              |


#### §Stage File Plan



```yaml
- reserved_id: TECH-704
  title: Signature module core (src/signature.py) with L15 sample-size policy
  priority: high
  issue_type: TECH
  notes: |
    New module `tools/sprite-gen/src/signature.py` exposing `compute_signature(class_name, folder_glob) -> dict`, `validate_against(signature, rendered_img) -> ValidationReport`, and `SignatureStaleError`. JSON shape per handoff §3 Stage 6.2 (class / refreshed_at / source_count / source_checksum / mode / fallback_of / bbox / palette / silhouette / ground / decoration_hints). L15 branches: source_count 0 → fallback, 1 → point-match, ≥2 → envelope. L3 staleness guard: recompute checksum on validate; mismatch raises `SignatureStaleError("signature stale — run python3 -m src refresh-signatures <class>")`.
  depends_on:
    - TECH-701
    - TECH-702
    - TECH-703
  related: []
  stub_body:
    summary: |
      Core signature module: extracts bbox / palette / silhouette / ground / decoration-hints summaries from a class folder of reference sprites; validates rendered images against the envelope; fails fast on stale checksum.
    goals: |
      1. `compute_signature(class_name, folder_glob)` returns the documented JSON dict.
      2. `validate_against(signature, rendered_img)` returns `ValidationReport(ok, failures)`; raises `SignatureStaleError` on checksum mismatch.
      3. L15 sample-size policy fully wired: 0 → fallback, 1 → point-match, ≥2 → envelope.
    systems_map: |
      New `tools/sprite-gen/src/signature.py`; consumers: `tests/test_signature_calibration.py` (T6.2.4), `src/__main__.py` refresh CLI (T6.2.2), eventual composer render-time gate (Stage 6.5).
    impl_plan_sketch: |
      Phase 1 — JSON shape + checksum helper; Phase 2 — extractor (bbox/palette/silhouette/ground/decoration_hints); Phase 3 — L15 branches + fallback graph resolution; Phase 4 — `validate_against` + `SignatureStaleError`. Gate: `pytest tools/sprite-gen/tests/test_signature.py -q` (new unit tests live with the module).
- reserved_id: TECH-705
  title: CLI refresh-signatures + signatures/ scaffold + residential_small bootstrap
  priority: high
  issue_type: TECH
  notes: |
    New CLI subcommand `python3 -m src refresh-signatures [class?]`. Creates/updates `tools/sprite-gen/signatures/<class>.signature.json`. Scaffolds `tools/sprite-gen/signatures/` dir with `_fallback.json` fallback-class graph and bootstrap `residential_small.signature.json` (source: `Assets/Sprites/residential_small/*.png`; ≥2 samples → envelope mode). All JSON committed to git so CI reads same snapshot.
  depends_on:
    - TECH-704
  related:
    - TECH-706
  stub_body:
    summary: |
      Ship the operator surface for signatures — one CLI entry point, one dir of canonical JSON, one fallback graph. `residential_small.signature.json` lands with the stage so downstream tests have a real envelope to assert against.
    goals: |
      1. `python3 -m src refresh-signatures` regenerates every signature in `signatures/`.
      2. `python3 -m src refresh-signatures <class>` regenerates one class.
      3. `signatures/_fallback.json` + `signatures/residential_small.signature.json` committed.
    systems_map: |
      `tools/sprite-gen/src/__main__.py` (or new `src/cli.py`); `tools/sprite-gen/signatures/`; `Assets/Sprites/<class>/*.png` (ingestion source); depends on `src/signature.py` from TECH-704.
    impl_plan_sketch: |
      Phase 1 — Wire subcommand into existing argparse; Phase 2 — `signatures/` dir + `_fallback.json` seeded with residential_small → residential_row; Phase 3 — Bootstrap `residential_small.signature.json` by running `refresh-signatures residential_small` against live catalog.
- reserved_id: TECH-706
  title: Spec loader include_in_signature per-sprite override
  priority: medium
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/spec.py` accepts optional top-level boolean `include_in_signature` (default `true`). Signature refresh ingestion skips sprites whose source YAML opts out via `include_in_signature: false`. Back-compat by construction — existing specs unchanged.
  depends_on:
    - TECH-704
  related:
    - TECH-705
  stub_body:
    summary: |
      Per-sprite opt-out so experimental / reference / deprecated specs don't contaminate class envelopes.
    goals: |
      1. `load_spec` surfaces `include_in_signature` (default `true`).
      2. Refresh pipeline (T6.2.2) filters out opted-out specs.
    systems_map: |
      `tools/sprite-gen/src/spec.py` (loader); `src/signature.py::compute_signature` (consumer, reads flag via `load_spec` when iterating).
    impl_plan_sketch: |
      Phase 1 — Add field to spec schema + loader; Phase 2 — filter in `compute_signature` source iteration. Gate: unit test with one opt-out spec confirms exclusion.
- reserved_id: TECH-707
  title: tests/test_signature_calibration.py parametrized + retire test_scale_calibration
  priority: high
  issue_type: TECH
  notes: |
    New parametrized test iterating every `signatures/*.signature.json`; for each class: `validate_against(signature, compose_sprite(load_spec(<class canonical spec>)))` must return `.ok == True`. Once green, delete or skip `tests/test_scale_calibration.py` — TECH-702 tight bounds now live in the signature envelope.
  depends_on:
    - TECH-704
    - TECH-705
  related:
    - TECH-702
  stub_body:
    summary: |
      Replaces per-spec bbox regression with full signature validation; auto-covers new classes as their signature JSON lands.
    goals: |
      1. `test_signature_calibration[residential_small]` green against live signature.
      2. `test_scale_calibration.py` retired (deleted or pytest.mark.skip).
      3. Full suite `pytest tools/sprite-gen/tests/ -q` exits 0 with same or higher test count.
    systems_map: |
      `tools/sprite-gen/tests/test_signature_calibration.py` (new); `tools/sprite-gen/tests/test_scale_calibration.py` (retired); `signatures/residential_small.signature.json` (reference envelope).
    impl_plan_sketch: |
      Phase 1 — Parametrize over `signatures/*.signature.json` glob; Phase 2 — Drop `test_scale_calibration.py`; Phase 3 — Run full suite.
- reserved_id: TECH-708
  title: DAS §2.6 pointer — signatures are canonical calibration source
  priority: medium
  issue_type: TECH
  notes: |
    Add DAS §2.6 section: "Calibration signatures are the canonical runtime calibration source. See `tools/sprite-gen/signatures/` + `src/signature.py`." Forward-pointer; no re-documentation of the JSON shape (keep authoritative spec in signature module docstring).
  depends_on:
    - TECH-704
  related: []
  stub_body:
    summary: |
      DAS §2.6 delta — point readers at signatures/ as the canonical calibration source. No detailed schema duplication; trust the module docstring.
    goals: |
      1. DAS §2.6 new section authored.
      2. Pointer cites `tools/sprite-gen/signatures/` + `src/signature.py`.
    systems_map: |
      `docs/sprite-gen-art-design-system.md`.
    impl_plan_sketch: |
      Phase 1 — Insert §2.6 block after existing §2.5 (or wherever the current §2 chain ends); commit as doc-only change.
```

**Dependency gate:** Stage 6.1 merged (TECH-701..703). L12 stage order lock.

#### §Plan Fix — PASS (no drift)

> plan-review exit 0 — Stage 6.2 tasks **TECH-704**..**TECH-708** aligned with §3 Stage 6.2 block of `/tmp/sprite-gen-improvement-session.md`; JSON shape (L20 verbatim) + L15 sample-size policy carried into TECH-704. Aggregate doc: `docs/implementation/sprite-gen-stage-6.2-plan.md`. Downstream: file Stage 6.3.

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `ia/projects/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---
