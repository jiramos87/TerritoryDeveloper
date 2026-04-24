### Stage 6.7 — Animation schema reservation (tiny)


**Status:** Final — closed 2026-04-23 (4 tasks **TECH-737**..**TECH-740** archived via `36fbca5`). **Locks consumed:** L16 (reserve animation schema today; implementation deferred).

**Objectives:** Reserve the animation schema in the spec grammar without implementing any frame-based rendering. Spec loader accepts `output.animation:` reserved block but the only permitted value in v1 is `enabled: false`; anything else raises. Per-primitive `animate: none` key is accepted; any other value raises `NotImplementedError("Animation deferred; see DAS §12")`. DAS §12 gets a new "Animation (reserved; not yet implemented)" stub documenting the reserved keys. Independent stage — no code-path dependency.

**Exit:**

- `tools/sprite-gen/src/spec.py` — recognises `output.animation:` block; validates `enabled: false` only (other values → `SpecError`); permits reserved sibling keys (`frames`, `fps`, `loop`, `phase_offset`, `layers`) without interpreting them.
- `tools/sprite-gen/src/primitives/*.py` (or `compose.py` per-primitive dispatch) — accepts `animate: none`; any other value raises `NotImplementedError("Animation deferred; see DAS §12")`.
- `tools/sprite-gen/tests/test_animation_reservation.py` — reserved block accepted; `enabled: true` raises; `animate: flicker` raises `NotImplementedError`.
- `docs/sprite-gen-art-design-system.md` §12 — new stub "Animation (reserved; not yet implemented)" documenting the reserved schema + acceptable v1 values.
- `pytest tools/sprite-gen/tests/` exits 0.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T6.7.1 | Spec loader: reserved `output.animation:` block | **TECH-737** | Done | `tools/sprite-gen/src/spec.py` — recognise top-level `output.animation:` dict; validate only `enabled: false` passes; raise `SpecError` on `enabled: true` (reserved but not implemented). Sibling keys `frames`, `fps`, `loop`, `phase_offset`, `layers` accepted without interpretation. Consumes L16. |
| T6.7.2 | Per-primitive `animate:` reservation | **TECH-738** | Done | Composer / primitive dispatch — accepts `animate: none` on any decoration entry; any other value raises `NotImplementedError("Animation deferred; see DAS §12")`. Centralised check so every primitive inherits the guard. |
| T6.7.3 | Tests: `test_animation_reservation.py` | **TECH-739** | Done | `tools/sprite-gen/tests/test_animation_reservation.py` — (a) `enabled: false` block parses cleanly; (b) `enabled: true` raises `SpecError`; (c) primitive with `animate: none` renders; (d) `animate: flicker` raises `NotImplementedError` with "DAS §12" in message. |
| T6.7.4 | DAS §12 stub — "Animation (reserved; not yet implemented)" | **TECH-740** | Done | `docs/sprite-gen-art-design-system.md` §12 — new stub documents reserved keys (`output.animation.*`, per-primitive `animate:`), enumerates v1 permitted values (`enabled: false`, `animate: none`), and forward-points to future animation milestone. |

#### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: TECH-737
  title: Spec loader — reserved output.animation block
  priority: medium
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/spec.py` — recognise top-level `output.animation:` dict; validate only `enabled: false`; reserved siblings (`frames`, `fps`, `loop`, `phase_offset`, `layers`) accepted without interpretation; `enabled: true` raises `SpecError`.
  depends_on: []
  related:
    - TECH-738
  stub_body:
    summary: |
      Accept `output.animation:` reserved block in spec grammar; `enabled: false` is the only permitted runtime value in v1.
    goals: |
      1. `output.animation:` block parses without breaking v1 composer.
      2. `enabled: false` passes; `enabled: true` raises `SpecError`.
      3. Reserved siblings accepted and preserved in the resolved spec.
    systems_map: |
      `tools/sprite-gen/src/spec.py`; consumers: `tests/test_animation_reservation.py` (TECH-739).
    impl_plan_sketch: |
      Phase 1 — Detect `output.animation`; Phase 2 — Validate `enabled`; Phase 3 — Tolerate reserved siblings.
- reserved_id: TECH-738
  title: Per-primitive animate reservation
  priority: medium
  issue_type: TECH
  notes: |
    Composer / primitive dispatch — accepts `animate: none`; any other value raises `NotImplementedError("Animation deferred; see DAS §12")`. Single shared guard so every primitive inherits the check.
  depends_on:
    - TECH-737
  related:
    - TECH-739
  stub_body:
    summary: |
      Per-primitive `animate:` key accepts `none`; any other value raises `NotImplementedError` with DAS §12 pointer.
    goals: |
      1. Centralised guard — every primitive inherits without duplication.
      2. `animate: none` is a no-op passthrough.
      3. Any other value raises with actionable DAS pointer.
    systems_map: |
      Composer / primitive dispatch in `tools/sprite-gen/src/compose.py` (or equivalent).
    impl_plan_sketch: |
      Phase 1 — Centralise check in composer dispatch; Phase 2 — Raise on unknown values.
- reserved_id: TECH-739
  title: Tests — test_animation_reservation.py
  priority: medium
  issue_type: TECH
  notes: |
    `tools/sprite-gen/tests/test_animation_reservation.py` — (a) reserved block parses; (b) `enabled: true` raises `SpecError`; (c) `animate: none` primitive renders; (d) `animate: flicker` raises `NotImplementedError` with "DAS §12" in message.
  depends_on:
    - TECH-737
    - TECH-738
  related: []
  stub_body:
    summary: |
      One test file locking the reservation contract end-to-end.
    goals: |
      1. Four named tests green.
      2. Error paths assert message content (not just type).
      3. `pytest tools/sprite-gen/tests/ -q` green overall.
    systems_map: |
      `tools/sprite-gen/tests/test_animation_reservation.py` (new).
    impl_plan_sketch: |
      Phase 1 — Spec-block parse/raise tests; Phase 2 — Primitive animate no-op + raise tests.
- reserved_id: TECH-740
  title: DAS §12 stub — Animation (reserved; not yet implemented)
  priority: low
  issue_type: TECH
  notes: |
    `docs/sprite-gen-art-design-system.md` §12 — new stub "Animation (reserved; not yet implemented)" documenting reserved keys + v1 permitted values + forward pointer.
  depends_on:
    - TECH-737
    - TECH-738
  related: []
  stub_body:
    summary: |
      Doc stub reserving DAS §12 for future animation work.
    goals: |
      1. §12 documents `output.animation.*` reserved keys.
      2. §12 documents per-primitive `animate:` with permitted v1 value list.
      3. §12 forward-points to future animation milestone.
    systems_map: |
      `docs/sprite-gen-art-design-system.md` §12.
    impl_plan_sketch: |
      Phase 1 — Insert §12 heading; Phase 2 — Reserved-keys table; Phase 3 — v1 permitted values + forward pointer.
```

**Dependency gate:** None. Independent stage; can ship alongside or ahead of Stage 6.6.

#### §Plan Fix — PASS (no drift)

> plan-review exit 0 — Stage 6.7 tasks **TECH-737**..**TECH-740** aligned with §3 Stage 6.7 block of `/tmp/sprite-gen-improvement-session.md`; lock L16 threaded through spec loader + per-primitive guard + doc stub. Aggregate doc: `docs/implementation/sprite-gen-stage-6.7-plan.md`. Downstream: Stage 9 addendum (`tiled-row-N`) next.

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `docs/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---

### Stage 9 addendum — Parametric `tiled-row-N` / `tiled-column-N`


**Status:** Final — closed 2026-04-23 (4 tasks **TECH-741**..**TECH-744** archived via `3d776cd`). **Issues closed:** I7. **Filing hint:** amend Stage 9 T9.2 before it becomes an issue — this block stands in until Stage 9 is itself filed with full task YAMLs.

**Objectives:** Upgrade the Stage 9 slot grammar from fixed names (`tiled-row-3`, `tiled-row-4`, `tiled-column-3`) to a parametric form: `tiled-row-N` / `tiled-column-N` for any `N ≥ 2`. `resolve_slot` distributes N buildings evenly across the relevant axis while respecting footprint. Unblocks `MediumResidentialBuilding-2-128.png` (5-house row) as T9.3's visual target, and — cross-stage — is what makes `row_houses_3x` (TECH-734 / Stage 6.6) render cleanly.

**Exit:**

- `tools/sprite-gen/src/slots.py` — `tiled-(row|column)-N` name grammar parsed via regex; `N < 2` or non-int `N` raises `SpecError`.
- `tools/sprite-gen/src/slots.py` — `resolve_slot("tiled-row-N", footprint, idx, count)` distributes `count` buildings evenly across the row axis with integer-pixel anchors; ditto for `tiled-column-N`.
- `tools/sprite-gen/tests/test_parametric_slots.py` — parse valid + invalid names; distribute for N ∈ {2, 3, 4, 5}; anchors equal-spaced + integer-pixel.
- `docs/sprite-gen-art-design-system.md` §5 R11 amended — table row for parametric slot grammar (replaces hard-coded `tiled-row-3/4`).
- `pytest tools/sprite-gen/tests/` exits 0.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T9.add.1 | Slot name grammar — `tiled-(row\|column)-N` parser | **TECH-741** | Done | `tools/sprite-gen/src/slots.py` — parse slot name via regex `^tiled-(row\|column)-(\d+)$`; capture axis + `N`; validate `N ≥ 2`; otherwise raise `SpecError` with the offending name. Hard-coded names from T9.2 stay accepted transitionally (alias through parser). |
| T9.add.2 | `resolve_slot` distribute N evenly across axis | **TECH-742** | Done | `tools/sprite-gen/src/slots.py` — `resolve_slot(name, footprint, idx, count)` returns `(x_px, y_px)` for the `idx`-th of `count` buildings, equal-spaced along the named axis. Integer-pixel anchors (no subpixel). Footprint respected so anchors stay inside the ground diamond. |
| T9.add.3 | Tests — `test_parametric_slots.py` | **TECH-743** | Done | `tools/sprite-gen/tests/test_parametric_slots.py` — (a) parser accepts `tiled-row-2..5` + `tiled-column-2..5`; (b) `tiled-row-1` raises; (c) `tiled-foo-3` raises; (d) distribute equal-spaced integer-pixel anchors for N ∈ {2,3,4,5}; (e) column axis mirrored. |
| T9.add.4 | DAS §5 R11 amendment — parametric slot grammar | **TECH-744** | Done | `docs/sprite-gen-art-design-system.md` §5 R11 — replace hard-coded `tiled-row-3/4` entries with a parametric row documenting `tiled-(row\|column)-N` for `N ≥ 2`. Forward-pointer to `row_houses_3x` preset (TECH-734) as a consumer. Capstone — merges last to reflect actual parser. |

#### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: TECH-741
  title: Slot name grammar — tiled-(row|column)-N parser
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/slots.py` — parse slot name via regex; capture axis + `N`; validate `N ≥ 2`; otherwise raise `SpecError`. Hard-coded legacy names accepted transitionally (alias through parser).
  depends_on: []
  related:
    - TECH-742
  stub_body:
    summary: |
      Parametric slot name parser: `tiled-(row|column)-N` for N ≥ 2.
    goals: |
      1. Regex parse captures axis ∈ {row, column} and `N`.
      2. `N < 2` or non-int raises `SpecError`.
      3. Hard-coded legacy names (`tiled-row-3`, etc.) alias through the parser.
    systems_map: |
      `tools/sprite-gen/src/slots.py`; consumer: `resolve_slot` (TECH-742).
    impl_plan_sketch: |
      Phase 1 — Regex + capture; Phase 2 — N ≥ 2 validation; Phase 3 — Legacy alias.
- reserved_id: TECH-742
  title: resolve_slot distribute N evenly across axis
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/slots.py` — `resolve_slot(name, footprint, idx, count)` returns `(x_px, y_px)` for the `idx`-th of `count` buildings, equal-spaced along the named axis. Integer-pixel anchors; footprint respected.
  depends_on:
    - TECH-741
  related:
    - TECH-743
  stub_body:
    summary: |
      Distribute N buildings evenly along the row or column axis with integer-pixel anchors.
    goals: |
      1. `resolve_slot` accepts `(name, footprint, idx, count)`.
      2. Anchors are equal-spaced integers along the named axis.
      3. Anchors stay inside the ground diamond (footprint-aware).
    systems_map: |
      `tools/sprite-gen/src/slots.py`; consumer: composer building-dispatch + TECH-734 preset.
    impl_plan_sketch: |
      Phase 1 — Axis dispatch; Phase 2 — Equal-space math; Phase 3 — Integer-pixel clamp.
- reserved_id: TECH-743
  title: Tests — test_parametric_slots.py
  priority: high
  issue_type: TECH
  notes: |
    `tools/sprite-gen/tests/test_parametric_slots.py` — parser accept + reject paths; distribute for N ∈ {2,3,4,5}; anchors equal-spaced integer-pixel; column mirrored.
  depends_on:
    - TECH-741
    - TECH-742
  related: []
  stub_body:
    summary: |
      One test file locking parser + distributor end-to-end.
    goals: |
      1. Parser accept/reject asserted.
      2. Distribute correctness for N ∈ {2,3,4,5}.
      3. Column axis mirror of row.
    systems_map: |
      `tools/sprite-gen/tests/test_parametric_slots.py` (new).
    impl_plan_sketch: |
      Phase 1 — Parser tests; Phase 2 — Distribute tests (row); Phase 3 — Column mirror.
- reserved_id: TECH-744
  title: DAS §5 R11 amendment — parametric slot grammar
  priority: medium
  issue_type: TECH
  notes: |
    `docs/sprite-gen-art-design-system.md` §5 R11 — replace hard-coded `tiled-row-3/4` entries with a parametric row documenting `tiled-(row|column)-N` for `N ≥ 2`. Forward-pointer to `row_houses_3x` preset (TECH-734).
  depends_on:
    - TECH-741
    - TECH-742
    - TECH-743
  related:
    - TECH-734
  stub_body:
    summary: |
      Doc capstone — replace hard-coded slot rows with the parametric grammar.
    goals: |
      1. §5 R11 documents `tiled-(row|column)-N` with `N ≥ 2`.
      2. Hard-coded row entries removed or redirected.
      3. Forward pointer to `row_houses_3x` preset (TECH-734).
    systems_map: |
      `docs/sprite-gen-art-design-system.md` §5 R11.
    impl_plan_sketch: |
      Phase 1 — Locate R11; Phase 2 — Replace rows; Phase 3 — Forward pointer.
```

**Dependency gate:** None for the addendum itself. Consumer chain: TECH-734 (`row_houses_3x`, Stage 6.6) depends on TECH-744 — renders cleanly only once the addendum lands. Stage 9 master block T9.2 will fold this grammar when filed proper.

#### §Plan Fix — PASS (no drift)

> plan-review exit 0 — Stage 9 addendum tasks **TECH-741**..**TECH-744** aligned with §3 Stage 9 addendum block of `/tmp/sprite-gen-improvement-session.md`; parametric `tiled-(row|column)-N` grammar threaded through parser + resolver + tests + doc. TECH-744 is the capstone consumed by TECH-734 (Stage 6.6 `row_houses_3x`). Aggregate doc: `docs/implementation/sprite-gen-stage-9-addendum-plan.md`. Downstream: file Stage 7 addendum (cross-tile passthrough).

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `docs/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---

### Stage 7 addendum — Cross-tile passthrough pattern


**Status:** Final — closed 2026-04-23 (4 tasks **TECH-745**..**TECH-748** archived via `bbfeff9`). **Locks consumed:** L17. **Filing hint:** amend Stage 7 decoration authoring guidance — this block stands in until Stage 7 is itself merged proper.

**Objectives:** Document the existing slope-sprite "empty lot / natural-park-walkway passthrough" pattern (where adjacent tiles visually continue through a neighbor-blending bridge) and extend it to flat archetypes via a new `ground.passthrough: true` flag. When true, the composer inhibits `iso_ground_noise` and clamps `hue_jitter` to its narrowest value so the tile reads as a seamless continuation of its neighbors.

**Exit:**

- `tools/sprite-gen/src/spec.py` — accepts `ground.passthrough: bool` (default `false`); validates type.
- `tools/sprite-gen/src/compose.py` (or ground render path) — when `passthrough=true`: skip `iso_ground_noise`, force `hue_jitter ≤ 0.01`, preserve base material colour so neighbor tiles blend.
- `tools/sprite-gen/tests/test_ground_passthrough.py` — flag parses; render skips noise + clamps jitter; byte-difference vs. `passthrough=false` non-zero but bounded.
- `docs/sprite-gen-art-design-system.md` §3 — new subsection documenting the existing slope pattern + the flat-archetype extension with the new flag.
- `pytest tools/sprite-gen/tests/` exits 0.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T7.10.1 | Spec schema: `ground.passthrough` flag | **TECH-745** | Done | `tools/sprite-gen/src/spec.py` — accept `ground.passthrough: bool` sibling of `material`; default `false`; non-bool raises `SpecError`. Consumes L17. |
| T7.10.2 | Composer: inhibit noise + clamp jitter | **TECH-746** | Done | `tools/sprite-gen/src/compose.py` — ground render path checks `spec.ground.passthrough`; when true: skip `iso_ground_noise` call; force `hue_jitter = min(hue_jitter, 0.01)`; `value_jitter = 0`. Base material colour preserved so neighbor tiles blend. |
| T7.10.3 | Tests — `test_ground_passthrough.py` | **TECH-747** | Done | `tools/sprite-gen/tests/test_ground_passthrough.py` — (a) flag parses; (b) non-bool raises; (c) `passthrough=true` render skips noise (visual diff vs. baseline); (d) `hue_jitter` clamped even if author sets higher; (e) `passthrough=false` (default) unchanged. |
| T7.10.4 | DAS §3 amendment — passthrough pattern | **TECH-748** | Done | `docs/sprite-gen-art-design-system.md` §3 — new subsection "Cross-tile passthrough" documenting the existing slope-sprite "empty lot / natural-park-walkway" pattern + the flat-archetype extension via `ground.passthrough: true`. Explains rendering implications (no noise; narrowest jitter). |

#### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: TECH-745
  title: Spec schema — ground.passthrough flag
  priority: medium
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/spec.py` — accept `ground.passthrough: bool` sibling of `material`; default `false`; non-bool raises `SpecError`.
  depends_on: []
  related:
    - TECH-746
  stub_body:
    summary: |
      Add `ground.passthrough: bool` flag with validation and default.
    goals: |
      1. `ground.passthrough: true|false` parses.
      2. Default value `false` when absent.
      3. Non-bool value raises `SpecError`.
    systems_map: |
      `tools/sprite-gen/src/spec.py`; consumer: composer ground path (TECH-746).
    impl_plan_sketch: |
      Phase 1 — Type guard on ground block; Phase 2 — Default propagation.
- reserved_id: TECH-746
  title: Composer — inhibit noise + clamp jitter on passthrough tiles
  priority: medium
  issue_type: TECH
  notes: |
    `tools/sprite-gen/src/compose.py` — ground render path: when `passthrough=true`, skip `iso_ground_noise`; clamp `hue_jitter ≤ 0.01`; `value_jitter = 0`. Base material colour preserved.
  depends_on:
    - TECH-745
  related:
    - TECH-747
  stub_body:
    summary: |
      Passthrough tiles render as neighbor-blending bridges — no noise, narrowest jitter.
    goals: |
      1. `iso_ground_noise` skipped when passthrough=true.
      2. `hue_jitter` clamped to ≤0.01; `value_jitter` forced to 0.
      3. Base material colour preserved so neighbors blend.
    systems_map: |
      `tools/sprite-gen/src/compose.py` ground render path; consumer: `tests/test_ground_passthrough.py` (TECH-747).
    impl_plan_sketch: |
      Phase 1 — Branch on passthrough; Phase 2 — Skip noise call; Phase 3 — Clamp jitter.
- reserved_id: TECH-747
  title: Tests — test_ground_passthrough.py
  priority: medium
  issue_type: TECH
  notes: |
    `tools/sprite-gen/tests/test_ground_passthrough.py` — (a) flag parses; (b) non-bool raises; (c) passthrough render skips noise (visual diff vs. baseline); (d) `hue_jitter` clamp enforced; (e) `passthrough=false` unchanged.
  depends_on:
    - TECH-745
    - TECH-746
  related: []
  stub_body:
    summary: |
      One test file locking passthrough semantics end-to-end.
    goals: |
      1. Five named tests green.
      2. Visual-diff tests use bounded byte-count difference (not exact pixel).
      3. Default-false path byte-identical to pre-addendum baseline.
    systems_map: |
      `tools/sprite-gen/tests/test_ground_passthrough.py` (new).
    impl_plan_sketch: |
      Phase 1 — Flag parse / raise tests; Phase 2 — Render skip-noise test; Phase 3 — Jitter clamp + default-unchanged tests.
- reserved_id: TECH-748
  title: DAS §3 amendment — cross-tile passthrough pattern
  priority: low
  issue_type: TECH
  notes: |
    `docs/sprite-gen-art-design-system.md` §3 — new subsection documenting existing slope-sprite passthrough pattern + flat-archetype extension via `ground.passthrough: true`; rendering implications (no noise; narrowest jitter).
  depends_on:
    - TECH-745
    - TECH-746
  related: []
  stub_body:
    summary: |
      Doc amendment — passthrough pattern for slope + flat archetypes.
    goals: |
      1. §3 documents existing slope-sprite passthrough pattern.
      2. §3 documents `ground.passthrough: true` flat-archetype extension.
      3. §3 documents rendering implications (no noise; narrowest jitter).
    systems_map: |
      `docs/sprite-gen-art-design-system.md` §3.
    impl_plan_sketch: |
      Phase 1 — Locate §3 insertion point; Phase 2 — Write slope-pattern doc; Phase 3 — Flat-archetype extension + rendering implications.
```

**Dependency gate:** None. Independent stage addendum; can ship alongside or ahead of Stage 7 proper. Stage 7 master block (when filed) folds this in as authoring guidance on decoration placement.

#### §Plan Fix — PASS (no drift)

> plan-review exit 0 — Stage 7 addendum tasks **TECH-745**..**TECH-748** aligned with §3 Stage 7 addendum block of `/tmp/sprite-gen-improvement-session.md`; lock L17 threaded through schema flag + composer inhibit/clamp + tests + DAS §3 doc. Aggregate doc: `docs/implementation/sprite-gen-stage-7-addendum-plan.md`. Downstream: handoff exhausted — all 9 stages filed.

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `docs/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---
