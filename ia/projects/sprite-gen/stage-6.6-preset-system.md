### Stage 6.6 — Preset system


**Status:** Final — closed 2026-04-23 (7 tasks **TECH-730**..**TECH-736** archived via `71a3a4d`). **Locks consumed:** L13 (`preset: <name>` top-level key injects a base spec; author fields override; `vary:` block from preset is preserved — author may extend / override individual `vary.*` entries but not wipe the block).

**Objectives:** Let authors bootstrap a sprite from a named preset that already carries geometry, palette, placement, and `vary:` decisions. `tools/sprite-gen/presets/<name>.yaml` holds fully-valid specs (minus `id` / `output.name`). The loader recognises `preset: <name>`, injects the preset as base, applies author overrides, and preserves the preset's `vary:` block under a strict merge rule (union on axes; non-wipe on the block itself). Seed three presets — `suburban_house_with_yard`, `strip_mall_with_parking`, `row_houses_3x` — so Stage 6.6 ships with live consumers. Tests lock override + preservation + preset-referenced-twice determinism. DAS §6 addendum.

**Exit:**

- `tools/sprite-gen/src/spec.py` — `preset: <name>` top-level key resolves to `tools/sprite-gen/presets/<name>.yaml`, merged with author fields (author wins per-field); `vary:` merge rule preserves preset axes, allows author to add / override individual axes, and raises `SpecError` on author attempting to wipe the whole `vary:` block.
- `tools/sprite-gen/presets/suburban_house_with_yard.yaml` — fully-valid spec minus `id` / `output.name`.
- `tools/sprite-gen/presets/strip_mall_with_parking.yaml` — fully-valid spec minus `id` / `output.name`.
- `tools/sprite-gen/presets/row_houses_3x.yaml` — fully-valid spec minus `id` / `output.name`.
- `tools/sprite-gen/tests/test_preset_system.py` — override semantics; `vary:` preservation; preset-referenced-twice determinism; missing-preset error.
- `docs/sprite-gen-art-design-system.md` §6 addendum — preset contract, merge rule, seeded presets catalogue.
- `pytest tools/sprite-gen/tests/` exits 0.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T6.6.1 | Loader: `preset: <name>` inject + author override | **TECH-730** | Done | `tools/sprite-gen/src/spec.py` — new `preset: <name>` top-level key. Loader resolves to `tools/sprite-gen/presets/<name>.yaml`, parses as base, then applies author-provided fields as overrides (author wins per-field). Missing preset file → `SpecError` listing valid preset names. Consumes L13. |
| T6.6.2 | `vary:` block merge rule (union + non-wipe) | **TECH-731** | Done | `tools/sprite-gen/src/spec.py` — `vary:` merge: preset axes preserved by default; author may add new `vary.*` axes or override individual axis values; author writing `vary: {}` or `vary: null` to wipe the block raises `SpecError`. Ensures preset-driven variation can't be silently disabled. |
| T6.6.3 | Seed `presets/suburban_house_with_yard.yaml` | **TECH-732** | Done | `tools/sprite-gen/presets/suburban_house_with_yard.yaml` — fully-valid spec (minus `id` / `output.name`) tuned for `residential_small` class: house footprint + grass ground (texture on) + `vary:` block over roof / facade / ground. Renders without author overrides. |
| T6.6.4 | Seed `presets/strip_mall_with_parking.yaml` | **TECH-733** | Done | `tools/sprite-gen/presets/strip_mall_with_parking.yaml` — fully-valid spec (minus `id` / `output.name`) tuned for commercial strip: wide footprint + pavement ground + `vary:` block over facade + ground. Renders without author overrides. |
| T6.6.5 | Seed `presets/row_houses_3x.yaml` | **TECH-734** | Done | `tools/sprite-gen/presets/row_houses_3x.yaml` — fully-valid spec (minus `id` / `output.name`) using `tiled-row-3` slot (Stage 9 addendum) + shared grass ground + per-row `vary:` block. Renders without author overrides when Stage 9 addendum lands. |
| T6.6.6 | Tests: `test_preset_system.py` | **TECH-735** | Done | `tools/sprite-gen/tests/test_preset_system.py` — (a) author field wins merge; (b) author `vary.padding` doesn't erase preset `vary.roof`; (c) author `vary: null` raises; (d) preset referenced twice with same seed → byte-identical output; (e) missing preset → `SpecError` with valid list. |
| T6.6.7 | DAS §6 addendum — preset contract + catalogue | **TECH-736** | Done | `docs/sprite-gen-art-design-system.md` §6 — document `preset: <name>` key + merge rule + `vary:` preservation semantic + the three seeded presets. Forward-pointer to `presets/` dir for discoverability. |

#### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: TECH-730
  title: Loader — preset key inject + author override
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/spec.py` — new `preset: <name>` top-level key. Loader resolves to `tools/sprite-gen/presets/<name>.yaml`, parses as base, applies author-provided fields as overrides (author wins per-field). Missing preset → `SpecError` with valid list.
  depends_on:
    - TECH-709
    - TECH-710
    - TECH-711
    - TECH-712
    - TECH-713
    - TECH-714
  related:
    - TECH-731
  stub_body:
    summary: |
      Top-level `preset:` key resolves to a base YAML, merged with author overrides. Errors early + points to valid preset names.
    goals: |
      1. `preset: <name>` resolves + parses base YAML.
      2. Author fields override preset fields per-key.
      3. Missing preset → `SpecError` listing valid names.
    systems_map: |
      `tools/sprite-gen/src/spec.py`; consumes `tools/sprite-gen/presets/*.yaml` (TECH-732..734).
    impl_plan_sketch: |
      Phase 1 — Detect `preset` key; Phase 2 — Load base YAML; Phase 3 — Deep-merge author fields; Phase 4 — Error on missing preset.
- reserved_id: TECH-731
  title: vary block merge rule (union + non-wipe)
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/spec.py` — `vary:` merge: preset axes preserved by default; author may add / override individual axes; author attempting to wipe the block (`vary: null` or `vary: {}`) raises `SpecError`.
  depends_on:
    - TECH-730
  related: []
  stub_body:
    summary: |
      `vary:` merge preserves preset-supplied axes; author can extend or override per axis; wiping the block raises.
    goals: |
      1. Preset axes survive unless explicitly overridden per axis.
      2. Author new axes merge in (union).
      3. `vary: {}` / `vary: null` from author → `SpecError`.
    systems_map: |
      `tools/sprite-gen/src/spec.py`; consumer: `tests/test_preset_system.py` (TECH-735).
    impl_plan_sketch: |
      Phase 1 — Detect author `vary` shape; Phase 2 — Union merge with preset; Phase 3 — Wipe-guard raises.
- reserved_id: TECH-732
  title: Seed preset — suburban_house_with_yard
  priority: medium
  issue_type: TECH
  notes: |
    `tools/sprite-gen/presets/suburban_house_with_yard.yaml` — fully-valid spec (minus `id`/`output.name`) for `residential_small` class with grass ground (texture on) + `vary:` covering roof / facade / ground.
  depends_on:
    - TECH-730
    - TECH-731
    - TECH-715
    - TECH-718
  related:
    - TECH-735
  stub_body:
    summary: |
      First seed preset — residential_small with grass yard + ground texture + variation over roof / facade / ground.
    goals: |
      1. Renders cleanly with `preset: suburban_house_with_yard` and no author overrides.
      2. `vary:` covers ≥3 axes (roof, facade, ground).
      3. Ground uses Stage 6.4 object form (material + texture).
    systems_map: |
      `tools/sprite-gen/presets/suburban_house_with_yard.yaml` (new).
    impl_plan_sketch: |
      Phase 1 — Copy base from `building_residential_small.yaml`; Phase 2 — Strip `id` / `output.name`; Phase 3 — Add yard ground + `vary:`.
- reserved_id: TECH-733
  title: Seed preset — strip_mall_with_parking
  priority: medium
  issue_type: TECH
  notes: |
    `tools/sprite-gen/presets/strip_mall_with_parking.yaml` — fully-valid spec (minus `id`/`output.name`) for commercial strip: wide footprint + pavement ground + `vary:` over facade + ground.
  depends_on:
    - TECH-730
    - TECH-731
    - TECH-715
    - TECH-718
  related:
    - TECH-735
  stub_body:
    summary: |
      Second seed preset — commercial strip with pavement ground and variation over facade + ground.
    goals: |
      1. Renders cleanly with `preset: strip_mall_with_parking` and no author overrides.
      2. Pavement ground uses Stage 6.4 accent keys (TECH-716 seeded).
      3. `vary:` covers ≥2 axes (facade, ground).
    systems_map: |
      `tools/sprite-gen/presets/strip_mall_with_parking.yaml` (new).
    impl_plan_sketch: |
      Phase 1 — Scaffold spec with wide footprint; Phase 2 — Pavement ground + texture; Phase 3 — `vary:` block.
- reserved_id: TECH-734
  title: Seed preset — row_houses_3x
  priority: medium
  issue_type: TECH
  notes: |
    `tools/sprite-gen/presets/row_houses_3x.yaml` — fully-valid spec (minus `id`/`output.name`) using `tiled-row-3` slot (Stage 9 addendum) + shared grass ground + per-row `vary:` block.
  depends_on:
    - TECH-730
    - TECH-731
    - TECH-744
  related:
    - TECH-735
  stub_body:
    summary: |
      Third seed preset — row-houses 3x pattern riding Stage 9's parametric `tiled-row-N` slot.
    goals: |
      1. Renders cleanly with `preset: row_houses_3x` once Stage 9 addendum lands.
      2. Uses `tiled-row-3` slot (parametric slot grammar).
      3. Shared grass ground across row; `vary:` applies per-house.
    systems_map: |
      `tools/sprite-gen/presets/row_houses_3x.yaml` (new).
    impl_plan_sketch: |
      Phase 1 — Scaffold with `tiled-row-3`; Phase 2 — Shared ground; Phase 3 — Per-house `vary:`.
- reserved_id: TECH-735
  title: Tests — test_preset_system.py
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/tests/test_preset_system.py` — (a) author field wins merge; (b) author `vary.padding` doesn't erase preset `vary.roof`; (c) author `vary: null` raises; (d) preset-referenced-twice determinism; (e) missing preset → `SpecError` with valid list.
  depends_on:
    - TECH-730
    - TECH-731
    - TECH-732
    - TECH-733
    - TECH-734
  related: []
  stub_body:
    summary: |
      One test file locking preset loader behaviour end-to-end.
    goals: |
      1. Five named tests (override, vary preservation, vary wipe raises, determinism, missing preset).
      2. Uses each seeded preset at least once.
      3. Deterministic seeds throughout.
    systems_map: |
      `tools/sprite-gen/tests/test_preset_system.py`; consumers: spec loader (TECH-730/731).
    impl_plan_sketch: |
      Phase 1 — Override test; Phase 2 — Vary preservation + wipe guard; Phase 3 — Determinism + missing preset.
- reserved_id: TECH-736
  title: DAS §6 addendum — preset contract + catalogue
  priority: medium
  issue_type: TECH
  notes: |
    `docs/sprite-gen-art-design-system.md` §6 — document `preset: <name>` key + merge rule + `vary:` preservation + the three seeded presets. Forward-pointer to `presets/` dir.
  depends_on:
    - TECH-730
    - TECH-731
    - TECH-732
    - TECH-733
    - TECH-734
  related: []
  stub_body:
    summary: |
      Doc addendum — preset contract + merge rule + catalogue of three seeded presets.
    goals: |
      1. §6 documents `preset: <name>` grammar + resolution rule.
      2. §6 documents merge rule (author overrides; `vary:` union; wipe raises).
      3. §6 catalogues the three seeded presets with short descriptions.
    systems_map: |
      `docs/sprite-gen-art-design-system.md` §6.
    impl_plan_sketch: |
      Phase 1 — Locate §6; Phase 2 — Append contract + merge rule; Phase 3 — Catalogue table.
```

**Dependency gate:** Stage 6.3 merged (TECH-709..714) for `vary:` grammar that presets carry; Stage 6.4 merged (TECH-715/718) for ground object form used by seeded presets. `row_houses_3x` additionally waits on Stage 9 addendum (TECH-744 — parametric `tiled-row-N` slot) before it renders cleanly.

#### §Plan Fix — PASS (no drift)

> plan-review exit 0 — Stage 6.6 tasks **TECH-730**..**TECH-736** aligned with §3 Stage 6.6 block of `/tmp/sprite-gen-improvement-session.md`; lock L13 threaded through loader + merge rule + seeded presets. Aggregate doc: `docs/implementation/sprite-gen-stage-6.6-plan.md`. Downstream: file Stage 6.7.

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `docs/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---
