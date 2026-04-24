### Stage 6.3 — Placement + variant randomness + split seeds


**Status:** Final — closed 2026-04-23 (6 tasks **TECH-709**..**TECH-714** archived via `7da3749`). **Locks consumed:** L5 (Spec gains `building.footprint_px`, `building.padding`, `building.align`), L6 (`variants:` becomes block `{count, vary, seed_scope}` with legacy scalar back-compat), L7 (`bootstrap-variants --from-signature` CLI; never auto-rewrites), L14 (split seeds `palette_seed` + `geometry_seed`).

**Objectives:** Grow the spec schema so authors can express non-centered placement, per-axis variation ranges, and split seeds for palette vs geometry independence. Wire the composer to honour the new fields via a `resolve_building_box` helper and a variant sampling loop. Add a `bootstrap-variants --from-signature` CLI that reads `signatures/<class>.signature.json` and writes sensible `vary:` defaults into a spec (opt-in; never auto-rewrites). Land three new test files covering placement combinatorics, geometric variants determinism, and split-seed independence.

**Exit:**

- `tools/sprite-gen/src/spec.py` — accepts `building.footprint_px`, `building.padding: {n,e,s,w}`, `building.align ∈ {center, sw, ne, nw, se, custom}` (default `center`); `variants:` block `{count, vary, seed_scope}` with legacy scalar `variants: N` back-compat; top-level `palette_seed` + `geometry_seed` (legacy scalar `seed: N` fans to both when split seeds absent).
- `tools/sprite-gen/src/compose.py` — new helper `resolve_building_box(spec) -> (bx, by, offset_x, offset_y)` honouring footprint_px / ratio / align / padding; variant loop samples each `vary:` range deterministically from `geometry_seed + i`; palette samples from `palette_seed + i`; `variants.seed_scope` default `palette` preserves legacy behaviour.
- `tools/sprite-gen/src/__main__.py` — new subcommand `python3 -m src bootstrap-variants <stem> --from-signature` reads `signatures/<class>.signature.json` and writes sensible `vary:` defaults into the named spec; opt-in only; never auto-rewrites during render.
- `tools/sprite-gen/tests/test_building_placement.py` — matrix over footprint_px / ratio / padding / align; asserts resolved mass bbox per case.
- `tools/sprite-gen/tests/test_variants_geometric.py` — same spec + `vary:` produces 4 variants with pairwise distinct bboxes; identical outputs across runs with same seeds.
- `tools/sprite-gen/tests/test_split_seeds.py` — freezing `palette_seed` varies only geometry when `geometry_seed` advances, and vice versa.
- `docs/sprite-gen-art-design-system.md` R11 addendum — new placement fields + split seed semantics + `vary:` grammar.
- `pytest tools/sprite-gen/tests/` exits 0.

**Tasks:**


| Task   | Name                                                            | Issue        | Status | Intent                                                                                                                                                                                                                                                                                                                                                                                                                                               |
| ------ | --------------------------------------------------------------- | ------------ | ------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| T6.3.1 | Placement schema: `building.footprint_px` / `padding` / `align` | **TECH-709** | Done  | `tools/sprite-gen/src/spec.py` — accept optional `building.footprint_px: [bx, by]` (wins over `footprint_ratio` when both present), `building.padding: {n, e, s, w}` in px (default all 0), `building.align ∈ {center, sw, ne, nw, se, custom}` (default `center`). Back-compat: existing specs without these fields render byte-identical. Consumes L5.                                                                                             |
| T6.3.2 | `variants:` block + split seeds normalization                   | **TECH-710** | Done  | `tools/sprite-gen/src/spec.py` — accept `variants: {count, vary, seed_scope}` object; legacy scalar `variants: N` normalises to `{count: N, vary: {}, seed_scope: palette}`. Accept top-level `palette_seed: int` + `geometry_seed: int`; legacy scalar `seed: N` fans out to both when split seeds absent. `seed_scope` default `palette` preserves legacy behaviour. Consumes L6, L14.                                                             |
| T6.3.3 | Composer `resolve_building_box` + variant loop sampling         | **TECH-711** | Done  | `tools/sprite-gen/src/compose.py` — new helper `resolve_building_box(spec) -> (bx, by, offset_x, offset_y)` encapsulating footprint_px / ratio / align / padding math (pure function, unit-tested). Variant loop samples each `vary:` range deterministically from `geometry_seed + i` for geometry axes and `palette_seed + i` for palette. Centering falls out of SE-corner anchor math; no geometry change when `align: center` and padding zero. |
| T6.3.4 | CLI `bootstrap-variants --from-signature`                       | **TECH-712** | Done  | `tools/sprite-gen/src/__main__.py` — new subcommand `python3 -m src bootstrap-variants <stem> --from-signature`. Reads `signatures/<class>.signature.json` (class derived from spec), writes sensible `vary:` defaults into the named spec (e.g. `vary.roof.h_px` from signature's silhouette band). Opt-in; never auto-rewrites during render. Consumes L7.                                                                                         |
| T6.3.5 | Tests: placement + variants + split seeds                       | **TECH-713** | Done  | `tools/sprite-gen/tests/test_building_placement.py` — matrix over footprint_px / ratio / padding / align combinations; asserts resolved mass bbox per case. `test_variants_geometric.py` — same spec + `vary:` → 4 variants pairwise-distinct bboxes; identical outputs across runs with same seeds. `test_split_seeds.py` — freezing `palette_seed` varies geometry only when `geometry_seed` advances, and vice versa.                             |
| T6.3.6 | DAS R11 addendum                                                | **TECH-714** | Done  | `docs/sprite-gen-art-design-system.md` — extend R11 with new placement fields (`building.footprint_px`, `padding`, `align`), split seed semantics (`palette_seed`, `geometry_seed`, legacy `seed` fan-out), and `vary:` grammar (range objects + `seed_scope`).                                                                                                                                                                                      |


#### §Stage File Plan



```yaml
- reserved_id: TECH-709
  title: Placement schema — building.footprint_px / padding / align
  priority: high
  issue_type: TECH
  notes: |
    Extend `tools/sprite-gen/src/spec.py` to accept optional `building.footprint_px: [bx, by]` (wins over `footprint_ratio` when both present), `building.padding: {n, e, s, w}` in px (default all 0), `building.align ∈ {center, sw, ne, nw, se, custom}` (default `center`). Back-compat: specs without these fields render byte-identical. Consumes L5.
  depends_on:
    - TECH-704
    - TECH-705
    - TECH-706
    - TECH-707
    - TECH-708
  related:
    - TECH-710
    - TECH-711
  stub_body:
    summary: |
      Introduces pixel-exact building placement on the 64-px canvas — footprint_px, asymmetric padding, and alignment anchor. No composer change in this task (pure schema); composer wiring lives in TECH-711.
    goals: |
      1. `building.footprint_px: [bx, by]` accepted and surfaced via `load_spec`.
      2. `building.padding: {n, e, s, w}` accepted; default `{0, 0, 0, 0}`.
      3. `building.align` accepted with values `{center, sw, ne, nw, se, custom}`; default `center`.
      4. Back-compat: existing specs produce byte-identical output.
    systems_map: |
      `tools/sprite-gen/src/spec.py`; consumers: `src/compose.py::resolve_building_box` (TECH-711), `tests/test_building_placement.py` (TECH-713).
    impl_plan_sketch: |
      Phase 1 — Schema validation + defaults in loader; Phase 2 — Unit tests for default/explicit cases; Phase 3 — Full suite regression.
- reserved_id: TECH-710
  title: variants block + split seeds loader normalization
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/spec.py` accepts `variants: {count, vary, seed_scope}` object; legacy scalar `variants: N` normalises to `{count: N, vary: {}, seed_scope: palette}`. Top-level `palette_seed: int` + `geometry_seed: int`; legacy scalar `seed: N` fans to both when split seeds absent. `seed_scope` default `palette` preserves legacy behaviour. Consumes L6, L14.
  depends_on:
    - TECH-704
    - TECH-705
    - TECH-706
    - TECH-707
    - TECH-708
  related:
    - TECH-709
    - TECH-711
  stub_body:
    summary: |
      Normalise the variants + split-seed surface in one loader pass. Back-compat by construction: every legacy shape maps cleanly to the new object shape with documented defaults.
    goals: |
      1. `variants: {count, vary, seed_scope}` accepted; scalar `variants: N` still works.
      2. `palette_seed` + `geometry_seed` accepted; legacy `seed: N` fans to both.
      3. `seed_scope` default `palette` preserved.
    systems_map: |
      `tools/sprite-gen/src/spec.py`; consumers: `src/compose.py` variant loop (TECH-711), `tests/test_variants_geometric.py` + `tests/test_split_seeds.py` (TECH-713).
    impl_plan_sketch: |
      Phase 1 — normalise scalar → object in loader; Phase 2 — Fan-out legacy `seed`; Phase 3 — Unit tests for each legacy shape + object shape.
- reserved_id: TECH-711
  title: Composer resolve_building_box helper + variant loop sampling
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/compose.py` — new pure helper `resolve_building_box(spec) -> (bx, by, offset_x, offset_y)` encapsulating footprint_px / ratio / align / padding math. Variant loop samples each `vary:` range deterministically from `geometry_seed + i` for geometry axes and `palette_seed + i` for palette. Centering falls out of SE-corner anchor math; no geometry change when `align: center` and padding zero.
  depends_on:
    - TECH-709
    - TECH-710
  related:
    - TECH-713
  stub_body:
    summary: |
      Composer honours placement + variant fields with a pure helper + seed-split sampler. Back-compat: existing specs render byte-identical thanks to default `align: center` + zero padding + `seed_scope: palette`.
    goals: |
      1. `resolve_building_box(spec)` returns `(bx, by, offset_x, offset_y)` consistent across all placement combos.
      2. Variant loop uses `geometry_seed + i` for geometry, `palette_seed + i` for palette.
      3. Legacy specs render byte-identical (zero diff against baseline).
    systems_map: |
      `tools/sprite-gen/src/compose.py` (new helper + variant loop). Consumers: `tests/test_building_placement.py` + `tests/test_variants_geometric.py` (TECH-713).
    impl_plan_sketch: |
      Phase 1 — Author `resolve_building_box` + unit tests; Phase 2 — Wire into composer pre-render; Phase 3 — Variant loop split-seed sampling; Phase 4 — Byte-identical regression on live specs.
- reserved_id: TECH-712
  title: CLI bootstrap-variants --from-signature
  priority: medium
  issue_type: TECH
  notes: |
    New subcommand `python3 -m src bootstrap-variants <stem> --from-signature`. Reads `signatures/<class>.signature.json` (class derived from spec), writes sensible `vary:` defaults into the named spec (e.g. `vary.roof.h_px` from signature silhouette band). Opt-in; never auto-rewrites during render. Consumes L7.
  depends_on:
    - TECH-710
  related:
    - TECH-705
  stub_body:
    summary: |
      Let authors bootstrap a `vary:` block from their class signature — saves them deriving ranges by hand. Opt-in only.
    goals: |
      1. CLI subcommand parses `<stem>` + `--from-signature` flag.
      2. Reads `signatures/<class>.signature.json` for the spec's class.
      3. Writes `vary:` block into the spec (preserving author-authored keys; never overwrites).
    systems_map: |
      `tools/sprite-gen/src/__main__.py` (new subcommand); `tools/sprite-gen/signatures/` (read-only); target: `tools/sprite-gen/specs/<stem>.yaml`.
    impl_plan_sketch: |
      Phase 1 — Wire subparser; Phase 2 — Derive `vary:` defaults from signature envelope fields; Phase 3 — Non-destructive write (merge, don't overwrite).
- reserved_id: TECH-713
  title: Tests — placement + variants + split seeds
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/tests/test_building_placement.py` — matrix over footprint_px / ratio / padding / align combinations; asserts resolved mass bbox per case. `test_variants_geometric.py` — same spec + `vary:` → 4 variants pairwise-distinct bboxes; identical outputs across runs with same seeds. `test_split_seeds.py` — freezing `palette_seed` varies only geometry when `geometry_seed` advances, and vice versa.
  depends_on:
    - TECH-711
    - TECH-712
  related: []
  stub_body:
    summary: |
      Three new test files pin placement combinatorics + variant determinism + split-seed independence. Full matrix coverage so drift surfaces in CI.
    goals: |
      1. `test_building_placement.py` exercises ≥12 combos (4 aligns × 3 padding profiles).
      2. `test_variants_geometric.py` asserts 4 distinct bboxes + reproducibility.
      3. `test_split_seeds.py` asserts seed-split independence.
    systems_map: |
      `tools/sprite-gen/tests/test_building_placement.py` + `test_variants_geometric.py` + `test_split_seeds.py` (all new); depends on composer helper (TECH-711) + CLI bootstrap (TECH-712).
    impl_plan_sketch: |
      Phase 1 — placement matrix; Phase 2 — geometric variants determinism; Phase 3 — split-seed independence; Phase 4 — Full suite regression.
- reserved_id: TECH-714
  title: DAS R11 addendum — placement + split seeds + vary grammar
  priority: medium
  issue_type: TECH
  notes: |
    `docs/sprite-gen-art-design-system.md` R11 addendum — new placement fields (`building.footprint_px`, `padding`, `align`), split seed semantics (`palette_seed`, `geometry_seed`, legacy `seed` fan-out), and `vary:` grammar (range objects + `seed_scope`).
  depends_on:
    - TECH-709
    - TECH-710
  related:
    - TECH-711
  stub_body:
    summary: |
      Doc-only addendum: makes R11 the canonical reference for the new schema surface.
    goals: |
      1. R11 documents placement fields.
      2. R11 documents split seeds + legacy fan-out.
      3. R11 documents `vary:` grammar (range objects + `seed_scope`).
    systems_map: |
      `docs/sprite-gen-art-design-system.md` R11 section.
    impl_plan_sketch: |
      Phase 1 — Draft addendum text; Phase 2 — Cross-link to spec files under `tools/sprite-gen/specs/`; Phase 3 — Grep-check for updated fields.
```

**Dependency gate:** Stage 6.2 merged (TECH-704..708). L12 stage order lock. `bootstrap-variants --from-signature` (TECH-712) specifically depends on `signatures/` directory existing (TECH-705) and `variants:` block loader (TECH-710).

#### §Plan Fix — PASS (no drift)

> plan-review exit 0 — Stage 6.3 tasks **TECH-709**..**TECH-714** aligned with §3 Stage 6.3 block of `/tmp/sprite-gen-improvement-session.md`; locks L5/L6/L7/L14 mapped one-to-one. Aggregate doc: `docs/implementation/sprite-gen-stage-6.3-plan.md`. Downstream: file Stage 6.4.

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `docs/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---
