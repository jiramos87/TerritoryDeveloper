### Stage 6.1 — Pivot hotfix + regression tighten


**Status:** Final — closed 2026-04-23 (3 tasks **TECH-701**..**TECH-703** archived; closeout residue repaired via `15d5f11`). Retroactive filing of the in-session pivot hotfix applied during the 2026-04-23 sprite-gen improvement session (`/tmp/sprite-gen-improvement-session.md` §3 Stage 6.1). The composer patch (`pivot_pad = 17 if spec.get("ground") != "none" else 0`) went live at `tools/sprite-gen/src/compose.py:256`; this stage produced the issue trail and tightened the regression suite. **Locks consumed:** L1 (pivot_pad=17 per DAS §2.1/§2.2). **Issues closed:** I1 (composer anchors buildings above ground diamond), I2 (regression loose).

**Objectives:** Lock the in-session pivot_pad patch behind a DAS-cited comment; replace the loose `10 <= y0 <= 16` scale-calibration bound with the tight DAS §2.3 envelope (`y1 == 48`, `content_h ∈ [32, 36]`); add a parametrized bbox regression to `tests/test_render_integration.py` covering every live spec under `tools/sprite-gen/specs/`.

**Exit:**

- `tools/sprite-gen/src/compose.py:256` — `pivot_pad = 17 if spec.get("ground") != "none" else 0`; `adjusted_y0 = y0 - pivot_pad - offset_z`; inline comment cites DAS §2.1 (diamond bottom at `y = canvas_h - 17`) + §2.2 (pivot 16 px from canvas bottom; +1 for PIL inclusive pixel indexing).
- `tools/sprite-gen/tests/test_scale_calibration.py` — assertions tightened to `y1 == 48` and `content_h ∈ [32, 36]`; loose `10 <= y0 <= 16` bound removed.
- `tools/sprite-gen/tests/test_render_integration.py` — parametrized `test_every_live_spec_has_bbox_below_diamond` across all `specs/*.yaml` in the tool tree; asserts bbox `(0, 15, 64, 48)` for every 1×1 live spec (`building_residential_small`, `building_residential_light_a|b|c`).
- `pytest tools/sprite-gen/tests/` exits 0 — 218+ tests green.

**Tasks:**


| Task   | Name                                                     | Issue        | Status | Intent                                                                                                                                                                                                                                                                                                                                                          |
| ------ | -------------------------------------------------------- | ------------ | ------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| T6.1.1 | Formalize pivot_pad patch + DAS-cited comment            | **TECH-701** | Done   | `tools/sprite-gen/src/compose.py:256` — confirm in-session hotfix (`pivot_pad = 17 if spec.get("ground") != "none" else 0`; `adjusted_y0 = y0 - pivot_pad - offset_z`); inline comment cites DAS §2.1 (diamond bottom y = canvas_h − 17) + §2.2 (pivot UV 16/canvas_h; +1 for PIL inclusive pixel indexing). Retroactive — code already landed in working tree. |
| T6.1.2 | Tighten `test_scale_calibration.py` regression bounds    | **TECH-702** | Done   | `tools/sprite-gen/tests/test_scale_calibration.py` — replace loose `10 <= y0 <= 16` with tight DAS §2.3 envelope: assert rendered bbox `y1 == 48`, `content_h ∈ [32, 36]`. House1-64 reference signature stays authoritative.                                                                                                                                   |
| T6.1.3 | Per-spec bbox regression in `test_render_integration.py` | **TECH-703** | Done   | `tools/sprite-gen/tests/test_render_integration.py` — parametrized fixture iterating `specs/*.yaml` (1×1 live specs only: `building_residential_small`, `building_residential_light_{a,b,c}`). For each spec: compose → assert bbox `(0, 15, 64, 48)`. Skip non-1×1 specs.                                                                                      |


#### §Stage File Plan



```yaml
- reserved_id: TECH-701
  title: Formalize pivot_pad patch + DAS-cited comment at compose.py:256
  priority: high
  issue_type: TECH
  notes: |
    Retroactive filing. In-session hotfix `pivot_pad = 17 if spec.get("ground") != "none" else 0`; `adjusted_y0 = y0 - pivot_pad - offset_z` already applied at `tools/sprite-gen/src/compose.py:256`. Task locks the wording + DAS §2.1/§2.2 citation in the inline comment so the invariant does not drift. No code path change.
  depends_on: []
  related:
    - TECH-702
    - TECH-703
  stub_body:
    summary: |
      Retroactive filing of pivot_pad hotfix. Composer anchors building primitives 17 px above canvas bottom when ground diamond is present; comment cites DAS §2.1 (diamond bottom 16 px + 1 inclusive pixel) + §2.2 (pivot UV = 16/canvas_h).
    goals: |
      1. Confirm `pivot_pad = 17 if spec.get("ground") != "none" else 0` at `compose.py:256`.
      2. Confirm `adjusted_y0 = y0 - pivot_pad - offset_z` at `compose.py:260`.
      3. Inline comment explicitly names DAS §2.1 + §2.2.
    systems_map: |
      `tools/sprite-gen/src/compose.py` (pivot_pad block around line 256); `docs/sprite-gen-art-design-system.md` §2.1 / §2.2 (source of truth for 17-px pad derivation).
    impl_plan_sketch: |
      Phase 1 — Read compose.py around the patch; verify DAS citation on the inline comment; adjust wording if drifted (no functional change). Gate: `pytest tools/sprite-gen/tests/ -q` stays green (218 tests).
- reserved_id: TECH-702
  title: Tighten test_scale_calibration.py regression bounds
  priority: high
  issue_type: TECH
  notes: |
    Replace loose `10 <= y0 <= 16` bound with the tight DAS §2.3 House1-64 envelope: `y1 == 48`, `content_h ∈ [32, 36]`. Closes the regression hole that let the original pivot bug ship.
  depends_on:
    - TECH-701
  related:
    - TECH-703
  stub_body:
    summary: |
      Scale-calibration regression tightened to the exact DAS §2.3 envelope from House1-64 (y1 = 48, content_h between 32 and 36 inclusive). No more loose y0 range.
    goals: |
      1. Add `assert y1 == 48` on rendered bbox.
      2. Add `assert 32 <= content_h <= 36`.
      3. Remove or deprecate the loose `10 <= y0 <= 16` check.
    systems_map: |
      `tools/sprite-gen/tests/test_scale_calibration.py` (`test_residential_small_bbox_y0_in_envelope` + neighbouring bbox checks); DAS §2.3 anchor metrics table.
    impl_plan_sketch: |
      Phase 1 — Rewrite `test_residential_small_bbox_*` functions with tight assertions referencing DAS §2.3. Gate: `pytest tools/sprite-gen/tests/test_scale_calibration.py -q` green against the current `building_residential_small.yaml` render.
- reserved_id: TECH-703
  title: Per-spec bbox regression in test_render_integration.py
  priority: high
  issue_type: TECH
  notes: |
    Parametrize a new `test_every_live_1x1_spec_bbox` across every `specs/*.yaml` in the sprite-gen tool tree; assert bbox equals exactly `(0, 15, 64, 48)` for each 1×1 live spec (`building_residential_small` + 3 `building_residential_light_{a,b,c}` variants). Closes I2 regression hole — any spec that anchors a building too high / too low fails at CI.
  depends_on:
    - TECH-701
  related:
    - TECH-702
  stub_body:
    summary: |
      Parametrized bbox regression proves the pivot hotfix holds for every live 1×1 spec, not only the canonical `building_residential_small`.
    goals: |
      1. Glob `tools/sprite-gen/specs/*.yaml`; collect 1×1 specs into a pytest parametrize.
      2. Render each spec via `compose_sprite(load_spec(path))`.
      3. Assert `rendered.getbbox() == (0, 15, 64, 48)` per spec.
    systems_map: |
      `tools/sprite-gen/tests/test_render_integration.py` (new parametrized test); `tools/sprite-gen/specs/` (glob source); `src/spec.py`, `src/compose.py` (render path).
    impl_plan_sketch: |
      Phase 1 — Add `@pytest.mark.parametrize("spec_path", _live_1x1_specs())` test; `_live_1x1_specs()` filters specs with `footprint: [1,1]`. Gate: `pytest tools/sprite-gen/tests/test_render_integration.py -q` green — 4+ parametrized cases.
```

**Dependency gate:** None. Independent hotfix filing ahead of Stage 7.

#### §Plan Fix — PASS (no drift)

> plan-review exit 0 — Stage 6.1 tasks **TECH-701**..**TECH-703** aligned with §3 Stage 6.1 block of `/tmp/sprite-gen-improvement-session.md`; no fix tuples. Aggregate doc: `docs/implementation/sprite-gen-stage-6.1-plan.md`. Downstream: file Stage 6.2.

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `ia/projects/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---
