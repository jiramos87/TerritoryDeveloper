### Stage 6.4 — Ground variation


**Status:** Final — closed 2026-04-23 (8 tasks **TECH-715**..**TECH-722** archived via `7dd80d7` + residual cleanup `0822391`). **Locks consumed:** L8 (`ground:` accepts string or object; back-compat by construction), L9 (`ground.`* joins `vary:` vocabulary; signature bounds jitter), L10 (new primitive `iso_ground_noise`; palette gains `accent_dark`/`accent_light`).

**Objectives:** Extend the ground surface beyond a single material string. Accept an object form `{material, materials, hue_jitter, value_jitter, texture}` on the spec (string form still valid; normalises to `{material: <str>}` with zero jitter / no texture). Wire the composer to sample hue/value jitter per variant and auto-insert an `iso_ground_noise` pass when `ground.texture` is set. Ship the new `iso_ground_noise` primitive + palette JSON `accent_dark`/`accent_light` extensions. Extend the signature extractor (TECH-704) so `ground.dominant` + `ground.variance` drive `vary.ground.`* bounds. Add `vary.ground.*` grammar (consumes L9). Land `tests/test_ground_variation.py`. DAS §4.1 addendum.

**Exit:**

- `tools/sprite-gen/src/spec.py` — `ground:` accepts string (normalise to `{material: <str>}`, zero jitter, no texture) or full object (`material` OR `materials: [...]`, `hue_jitter`, `value_jitter`, `texture: {primitive, density, palette}`); also accepts `vary.ground.{material, hue_jitter, value_jitter, texture.density}` inside `variants.vary`.
- `tools/sprite-gen/src/primitives/iso_ground_noise.py` — new primitive `iso_ground_noise(img, x0, y0, *, material, density, seed, palette)`; scatters accent pixels inside diamond mask only (density 0..0.15 guardrail).
- `tools/sprite-gen/palettes/*.json` — schema gains optional `accent_dark` / `accent_light` per material key (absent → noise primitive no-ops).
- `tools/sprite-gen/src/compose.py` — applies ground `hue_jitter` / `value_jitter` per variant (sampled from `palette_seed + i`) before rendering `iso_ground_diamond`; auto-inserts `iso_ground_noise` pass when `ground.texture` set (author does not hand-add to `composition:`).
- `tools/sprite-gen/src/signature.py` — extractor writes `ground.dominant` + `ground.variance` (consumed by `bootstrap-variants --from-signature` when deriving `vary.ground.`* bounds).
- `tools/sprite-gen/tests/test_ground_variation.py` — covers legacy string form (byte-identical), object form, material pool, jitter non-zero diff, noise primitive mask + density.
- `docs/sprite-gen-art-design-system.md` §4.1 addendum — documents `accent_dark` / `accent_light` palette keys + `iso_ground_noise` density range.
- `pytest tools/sprite-gen/tests/` exits 0.

**Tasks:**


| Task   | Name                                                     | Issue        | Status | Intent                                                                                                                                                                                                                                                                                                                                                                        |
| ------ | -------------------------------------------------------- | ------------ | ------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| T6.4.1 | Ground schema: string / object form loader normalization | **TECH-715** | Done  | `tools/sprite-gen/src/spec.py` — `ground:` accepts either string (normalises to `{material: <str>}`, zero jitter, no texture) or full object (`material` OR `materials: [...]`, optional `hue_jitter: float`, `value_jitter: float`, `texture: {primitive, density, palette}`). Back-compat: string form stays byte-identical. Consumes L8.                                   |
| T6.4.2 | Palette JSON `accent_dark` / `accent_light` keys         | **TECH-716** | Done  | `tools/sprite-gen/palettes/*.json` — schema gains optional `accent_dark` / `accent_light` per material key. Palette loader surfaces both; absent → consumer no-ops (noise primitive skips scatter). Seed default values for `grass_flat` + `pavement` so Stage 6.4 ships with at least 2 materials texturable. Consumes L10 (palette surface).                                |
| T6.4.3 | `iso_ground_noise` primitive                             | **TECH-717** | Done  | `tools/sprite-gen/src/primitives/iso_ground_noise.py` — new primitive `iso_ground_noise(img, x0, y0, *, material, density, seed, palette)`. Scatters accent pixels inside diamond mask only (no bleed onto building area). Density clamped 0..0.15 (guardrail). Deterministic under seed. Consumes L10 (primitive surface).                                                   |
| T6.4.4 | Composer ground jitter + texture auto-insert             | **TECH-718** | Done  | `tools/sprite-gen/src/compose.py` — before rendering `iso_ground_diamond`, apply `hue_jitter` / `value_jitter` sampled from `palette_seed + i` to the material's ramp. When `ground.texture` set, auto-insert an `iso_ground_noise` pass between the diamond and the first building primitive; author never hand-adds to `composition:`. Legacy string-form specs unchanged.  |
| T6.4.5 | Signature extractor `ground.`* extension                 | **TECH-719** | Done  | `tools/sprite-gen/src/signature.py` — extend extractor to populate `ground.dominant` (dominant palette on ground-only band of reference sprite) + `ground.variance` (hue_stddev + value_stddev across samples). Matches JSON shape from TECH-704 spec. Consumed by `bootstrap-variants --from-signature` to derive `vary.ground.`* bounds. Consumes L9 upstream.              |
| T6.4.6 | `vary.ground.*` grammar                                  | **TECH-720** | Done  | `tools/sprite-gen/src/spec.py` — accept `vary.ground.{material: {values: [...]}, hue_jitter: {min, max}, value_jitter: {min, max}, texture: {density: {min, max}}}` inside `variants.vary`. Loader validates range objects (from TECH-710). Composer variant loop samples these from `palette_seed + i`. Consumes L9 (vary surface).                                          |
| T6.4.7 | Tests: `test_ground_variation.py`                        | **TECH-721** | Done  | `tools/sprite-gen/tests/test_ground_variation.py` — covers (a) legacy string form byte-identical, (b) object form round-trip, (c) `materials: [...]` pool renders one of each seeded, (d) non-zero jitter produces per-variant diffs (`!= 0` pixel diff), (e) zero jitter produces byte-identical variants, (f) noise primitive: mask clipped to diamond + density monotonic. |
| T6.4.8 | DAS §4.1 addendum — palette accent keys + noise density  | **TECH-722** | Done  | `docs/sprite-gen-art-design-system.md` §4.1 — document `accent_dark` / `accent_light` palette keys + `iso_ground_noise` density range (0..0.15 guardrail). Forward-pointer to `signatures/` for authoring `vary.ground.`* bounds.                                                                                                                                             |


#### §Stage File Plan



```yaml
- reserved_id: TECH-715
  title: Ground schema — string / object form loader normalization
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/spec.py` — `ground:` accepts string (normalises to `{material: <str>}`, zero jitter, no texture) or full object (`material` OR `materials: [...]`, optional `hue_jitter: float`, `value_jitter: float`, `texture: {primitive, density, palette}`). Back-compat: string form stays byte-identical. Consumes L8.
  depends_on:
    - TECH-709
    - TECH-710
    - TECH-711
    - TECH-712
    - TECH-713
    - TECH-714
  related:
    - TECH-718
    - TECH-720
  stub_body:
    summary: |
      Normalise `ground:` at the loader so the composer reads one object shape regardless of author form. Byte-identical output for legacy string specs.
    goals: |
      1. String form normalises to `{material: <str>}`, zero jitter, no texture.
      2. Object form round-trips with defaults filled in.
      3. Explicit `materials: [...]` accepted (pool for variant sampling).
    systems_map: |
      `tools/sprite-gen/src/spec.py`; consumers: composer (TECH-718), vary loader (TECH-720).
    impl_plan_sketch: |
      Phase 1 — Detect str vs dict; emit object; Phase 2 — Fill defaults; Phase 3 — Unit tests.
- reserved_id: TECH-716
  title: Palette JSON accent_dark / accent_light keys
  priority: medium
  issue_type: TECH
  notes: |
    `tools/sprite-gen/palettes/*.json` — schema gains optional `accent_dark` / `accent_light` per material key. Palette loader surfaces both; absent → noise primitive no-ops. Seed `grass_flat` + `pavement` so Stage 6.4 ships with texturable materials.
  depends_on:
    - TECH-714
  related:
    - TECH-717
  stub_body:
    summary: |
      Extend palette schema with optional accent keys; seed two materials so noise primitive has consumers at Stage 6.4 close.
    goals: |
      1. Palette loader surfaces `accent_dark` / `accent_light` (or None).
      2. `grass_flat` + `pavement` seeded with both keys in the active palette.
      3. Existing palette entries unchanged.
    systems_map: |
      `tools/sprite-gen/palettes/*.json`; `tools/sprite-gen/src/palette.py` (loader).
    impl_plan_sketch: |
      Phase 1 — Loader surfaces optional keys; Phase 2 — Seed values in active palette JSON; Phase 3 — Unit test for absence → None.
- reserved_id: TECH-717
  title: iso_ground_noise primitive
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/primitives/iso_ground_noise.py` — new primitive `iso_ground_noise(img, x0, y0, *, material, density, seed, palette)`. Scatters accent pixels inside diamond mask only (no bleed onto building area). Density clamped 0..0.15. Deterministic under seed.
  depends_on:
    - TECH-716
  related:
    - TECH-718
  stub_body:
    summary: |
      Scatter-pixel primitive that textures the ground diamond with palette accent colours. Self-masks to diamond shape; respects density guardrail; deterministic under seed.
    goals: |
      1. Primitive signature matches composer dispatch contract.
      2. Scatter confined to diamond mask (verified via pixel accounting).
      3. Density 0..0.15 hard-clamp.
      4. Seed determinism: same (x0, y0, material, density, seed, palette) → identical pixels.
    systems_map: |
      `tools/sprite-gen/src/primitives/iso_ground_noise.py`; register in `src/primitives/__init__.py` + `src/compose.py::_DISPATCH`.
    impl_plan_sketch: |
      Phase 1 — Diamond mask from `iso_ground_diamond` geometry; Phase 2 — Pixel scatter from `random.Random(seed)`; Phase 3 — Density clamp + palette accent lookup.
- reserved_id: TECH-718
  title: Composer ground jitter + texture auto-insert
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/compose.py` — before rendering `iso_ground_diamond`, apply `hue_jitter` / `value_jitter` sampled from `palette_seed + i` to the material's ramp. When `ground.texture` set, auto-insert an `iso_ground_noise` pass between the diamond and the first building primitive. Legacy string-form specs unchanged.
  depends_on:
    - TECH-715
    - TECH-717
  related:
    - TECH-720
  stub_body:
    summary: |
      Composer honours jitter + texture fields. Legacy byte-identical guard still passes.
    goals: |
      1. Jitter sampled from `palette_seed + i`; zero jitter → byte-identical vs baseline.
      2. `ground.texture` set → noise pass auto-inserted.
      3. Author never hand-adds `iso_ground_noise` to `composition:`.
    systems_map: |
      `tools/sprite-gen/src/compose.py`; depends on `src/primitives/iso_ground_noise.py` (TECH-717).
    impl_plan_sketch: |
      Phase 1 — Jitter helper on palette ramp; Phase 2 — Wire into `iso_ground_diamond` call; Phase 3 — Conditional noise-pass insertion; Phase 4 — Byte-identical legacy regression.
- reserved_id: TECH-719
  title: Signature extractor ground.* extension
  priority: medium
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/signature.py` — extend extractor to populate `ground.dominant` (dominant palette on ground-only band of reference sprite) + `ground.variance` (hue_stddev + value_stddev across samples). Matches JSON shape from TECH-704. Consumed by `bootstrap-variants --from-signature` to derive `vary.ground.`* bounds.
  depends_on:
    - TECH-714
  related:
    - TECH-712
    - TECH-720
  stub_body:
    summary: |
      Signature extractor now fills the `ground` block — lets `bootstrap-variants` propose data-driven jitter ranges instead of hand-tuned guesses.
    goals: |
      1. `ground.dominant` populated from ground-band pixels.
      2. `ground.variance.hue_stddev` + `value_stddev` populated.
      3. L15 policy respected (fallback mode leaves ground fields null).
    systems_map: |
      `tools/sprite-gen/src/signature.py`; consumers: `src/__main__.py::bootstrap-variants` (TECH-712 extension).
    impl_plan_sketch: |
      Phase 1 — Ground-band isolation on reference sprite; Phase 2 — Palette dominant + stddev math; Phase 3 — JSON shape verification against TECH-704 spec.
- reserved_id: TECH-720
  title: vary.ground.* grammar
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/spec.py` — accept `vary.ground.{material: {values: [...]}, hue_jitter: {min, max}, value_jitter: {min, max}, texture: {density: {min, max}}}` inside `variants.vary`. Loader validates range objects (TECH-710). Composer variant loop samples these from `palette_seed + i`.
  depends_on:
    - TECH-710
    - TECH-715
  related:
    - TECH-718
  stub_body:
    summary: |
      Extend the `vary:` grammar with a ground axis so variants can randomise material / jitter / noise density without hand-rolling a per-spec loop.
    goals: |
      1. Loader validates `vary.ground.`* range objects.
      2. Composer samples these from `palette_seed + i`.
      3. Back-compat: specs without `vary.ground` unchanged.
    systems_map: |
      `tools/sprite-gen/src/spec.py`; consumers: composer variant loop (TECH-718).
    impl_plan_sketch: |
      Phase 1 — Extend `_normalize_variants` validation to accept ground axis; Phase 2 — Composer samples during variant iteration; Phase 3 — Unit tests.
- reserved_id: TECH-721
  title: Tests — test_ground_variation.py
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/tests/test_ground_variation.py` — covers (a) legacy string form byte-identical, (b) object form round-trip, (c) `materials: [...]` pool renders one of each seeded, (d) non-zero jitter produces per-variant diffs, (e) zero jitter produces byte-identical variants, (f) noise primitive: mask clipped to diamond + density monotonic.
  depends_on:
    - TECH-718
    - TECH-720
  related: []
  stub_body:
    summary: |
      One test file covering every surface touched by Stage 6.4. Catches jitter leakage, noise-mask bleed, and materials pool non-determinism.
    goals: |
      1. Six named cases (legacy, object, pool, non-zero jitter, zero jitter, noise mask).
      2. Reproducibility: same seeds → identical output across runs.
      3. Full suite `pytest tools/sprite-gen/tests/ -q` green.
    systems_map: |
      `tools/sprite-gen/tests/test_ground_variation.py` (new); consumers: `src/compose.py` + `src/primitives/iso_ground_noise.py`.
    impl_plan_sketch: |
      Phase 1 — Legacy + object form; Phase 2 — Pool; Phase 3 — Jitter diffs; Phase 4 — Noise primitive mask / density.
- reserved_id: TECH-722
  title: DAS §4.1 addendum — accent keys + noise density
  priority: medium
  issue_type: TECH
  notes: |
    `docs/sprite-gen-art-design-system.md` §4.1 — document `accent_dark` / `accent_light` palette keys + `iso_ground_noise` density range (0..0.15 guardrail). Forward-pointer to `signatures/` for authoring `vary.ground.*` bounds.
  depends_on:
    - TECH-716
    - TECH-717
  related:
    - TECH-708
  stub_body:
    summary: |
      Doc addendum that closes the loop — authors learn about accent keys and noise density limits in the design system doc, not in code comments.
    goals: |
      1. §4.1 lists `accent_dark` / `accent_light` as optional per-material palette keys.
      2. §4.1 documents noise density guardrail 0..0.15.
      3. §4.1 forward-points to `signatures/` for `vary.ground.*` authoring.
    systems_map: |
      `docs/sprite-gen-art-design-system.md` §4.1.
    impl_plan_sketch: |
      Phase 1 — Locate §4.1 table; Phase 2 — Append 3 short subsections; Phase 3 — Grep check.
```

**Dependency gate:** Stage 6.2 merged (TECH-704..708) + Stage 6.3 merged (TECH-709..714). L12 stage order lock. Signature extension (TECH-719) specifically extends TECH-704's extractor.

#### §Plan Fix — PASS (no drift)

> plan-review exit 0 — Stage 6.4 tasks **TECH-715**..**TECH-722** aligned with §3 Stage 6.4 block of `/tmp/sprite-gen-improvement-session.md`; locks L8/L9/L10 mapped one-to-one. Aggregate doc: `docs/implementation/sprite-gen-stage-6.4-plan.md`. Downstream: file Stage 6.5.

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `ia/projects/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> Scope: Stage 6.4 — Ground variation (TECH-715..722). All 8 tasks PASS code-review + audit. 330/330 tests green. Ready for archive.

```yaml
stage: "6.4"
status_flip:
  - file: ia/projects/sprite-gen-master-plan.md
    targets:
      - {row: T6.4.1, field: Status, from: Draft, to: Done}
      - {row: T6.4.2, field: Status, from: Draft, to: Done}
      - {row: T6.4.3, field: Status, from: Draft, to: Done}
      - {row: T6.4.4, field: Status, from: Draft, to: Done}
      - {row: T6.4.5, field: Status, from: Draft, to: Done}
      - {row: T6.4.6, field: Status, from: Draft, to: Done}
      - {row: T6.4.7, field: Status, from: Draft, to: Done}
      - {row: T6.4.8, field: Status, from: Draft, to: Done}
      - {row: "Stage 6.4", field: Status, from: Draft, to: Final}

archive_records:
  - id: TECH-715
    src: ia/backlog/TECH-715.yaml
    dst: ia/backlog-archive/TECH-715.yaml
  - id: TECH-716
    src: ia/backlog/TECH-716.yaml
    dst: ia/backlog-archive/TECH-716.yaml
  - id: TECH-717
    src: ia/backlog/TECH-717.yaml
    dst: ia/backlog-archive/TECH-717.yaml
  - id: TECH-718
    src: ia/backlog/TECH-718.yaml
    dst: ia/backlog-archive/TECH-718.yaml
  - id: TECH-719
    src: ia/backlog/TECH-719.yaml
    dst: ia/backlog-archive/TECH-719.yaml
  - id: TECH-720
    src: ia/backlog/TECH-720.yaml
    dst: ia/backlog-archive/TECH-720.yaml
  - id: TECH-721
    src: ia/backlog/TECH-721.yaml
    dst: ia/backlog-archive/TECH-721.yaml
  - id: TECH-722
    src: ia/backlog/TECH-722.yaml
    dst: ia/backlog-archive/TECH-722.yaml

delete_specs: []  # executed at closeout 2026-04-23; spec files removed

validation_gate:
  - cmd: "bash tools/scripts/materialize-backlog.sh"
  - cmd: "npm run validate:all"
```

---
