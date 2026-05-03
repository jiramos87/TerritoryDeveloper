# UI Visual Fidelity Layer — Post-MVP Extensions

> Out-of-scope items extracted from `docs/ui-visual-fidelity-layer-exploration.md`
> §"Deferred / out of scope" (lines ~339-346).
>
> Master plan: `ui-visual-fidelity-layer` (DB-backed). When any item below is
> picked up, route through `/master-plan-extend ui-visual-fidelity-layer {this-doc}`
> per `ia/skills/master-plan-extend/SKILL.md`.
>
> **Created:** 2026-04-30
>
> **Status:** Landing pad — no items active.

---

## Deferred items

### 1. Pixel-diff infra

What: framebuffer screenshot diff harness gating bake parity vs reference panel
PNGs.
Why deferred: Approach D rejected as MVP gate — bake-time conformance checks
(`frame_visual_present` / `spacing_match` / `button_states_wired`) cover parity
without screenshot infra cost.
Trigger: visual regressions slip past conformance bridge in 2+ panels post-MVP,
or per-pixel art-direction lock requested for shipped HUD.

### 2. ScriptableObject animated state machine

What: SO-driven hover/press/disabled state graph per button archetype.
Why deferred: Approach 6 rejected per Q4 — palette ramp + SpriteState slot
wiring on `ThemedButton` covers MVP state surface in one bake pass.
Trigger: button states need timed transitions or multi-stage choreography
(e.g. press → hold → release with intermediate frames) beyond ColorBlock +
SpriteState.

### 3. Global `catalog_token_spacing` table

What: dedicated spacing-token catalog row table parallel to palette / sprite
tables.
Why deferred: per Q3 — IR detail (`paddingX` / `paddingY` / `spacing` fields)
absorbs spacing values inline; no global table churn.
Trigger: ≥3 panels need shared spacing tokens with cross-panel reuse, or
spacing audit shows IR detail drift across `info-panel` / `pause-menu` /
`settings-screen`.

### 4. Per-panel motion library

What: panel-scoped curve presets beyond shared `motion_curve_slug` catalog
(e.g. `pause-menu/slide-in`, `settings-screen/fade-stack`).
Why deferred: shared `motion_curve_slug` transitions cover MVP motion surface;
per-panel libraries add catalog churn without proven need.
Trigger: panel-specific motion choreography (multi-element stagger, sequenced
reveals) requested by design beyond single-curve transitions.

### 5. Stage 19.3 `wire_asset_from_catalog` runtime resolver

What: runtime catalog → asset binding resolver (vs current bake-time wire).
Why deferred: blocked on asset-pipeline plan — runtime resolver depends on
asset-pipeline catalog publish + addressable build wiring not yet shipped.
Trigger: asset-pipeline lands runtime catalog publish surface, or hot-swap
panel theming requested at runtime (e.g. live theme switch without rebake).

### 6. Legacy `UnityEngine.UI.Text` migration to TMP for existing HUD

What: replace remaining `UnityEngine.UI.Text` instances in shipped HUD with
TextMeshPro per fidelity layer's TMP-only invariant.
Why deferred: existing HUD ships pre-fidelity-layer; migration touches live
prefabs outside `ui-visual-fidelity-layer` master plan scope.
Trigger: HUD enters fidelity bake pass (per-panel rollout reaches HUD), or
font asset bind audit flags `Text` instances as conformance violations.
