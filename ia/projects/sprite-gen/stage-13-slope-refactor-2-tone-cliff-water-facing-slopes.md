### Stage 13 — Slope refactor — 2-tone cliff + water-facing slopes


**Status:** Draft — 2026-04-23.

**Objectives:** Replace `iso_stepped_foundation` as the default under-building foundation with a cleaner `iso_slope_wedge` primitive (2-tone brown cliff sides per DAS §5 R8). Unlock water-facing slopes (17 variants) per L9. Update `slopes.yaml` to cover both land and water slope sets.

**Exit:**

- `iso_slope_wedge(fx, fy, slope_id, material='earth_brown')` — renders 2-tone brown cliff wedge under a tilted grass top. Reads per-corner Z table from `slopes.yaml`.
- `slopes.yaml` extended with 17 water-facing variants (adds `-water` suffix; renders water strip along low edge using `water_deep` bright color).
- Composer default: when `spec.terrain != 'flat'`, uses `iso_slope_wedge` (not `iso_stepped_foundation`).
- Legacy `iso_stepped_foundation` kept as opt-in (`spec.foundation_primitive: iso_stepped_foundation`) for multi-floor buildings that need stepping.
- Regression tests: all 34 (17 land + 17 water) slope variants render without crash; bbox matches existing `Slopes/*.png` counterparts within ±3 px.

**Tasks:**


| Task  | Name                          | Issue     | Status | Intent                                                                                                                                                |
| ----- | ----------------------------- | --------- | ------ | ----------------------------------------------------------------------------------------------------------------------------------------------------- |
| T13.1 | `iso_slope_wedge` primitive   | *pending* | pending  | Renders tilted grass top + 2-tone brown side faces; handles all 17 land slope codes. Palette key `earth_brown` (2-tone, no bright).                   |
| T13.2 | `slopes.yaml` water extension | *pending* | pending  | Add 17 `*-water` variants; each carries `water_strip_edges` metadata telling the primitive where to paint the water strip.                            |
| T13.3 | Water-strip rendering         | *pending* | pending  | `iso_slope_wedge` reads `water_strip_edges`, paints water_deep bright color on the low edge.                                                          |
| T13.4 | Composer default swap         | *pending* | pending  | `compose.py`: when `terrain != 'flat'`, use `iso_slope_wedge` by default; legacy `iso_stepped_foundation` accessible via `spec.foundation_primitive`. |
| T13.5 | 34-variant regression test    | *pending* | pending  | `tests/test_slopes_matrix.py` — parametrized test across 34 slope codes; render + bbox + dominant color vs hand-drawn reference.                      |


**Dependency gate:** Stage 6 archived.

#### §Stage File Plan

> Opus `stage-file-plan` writes structured `{operation, target_path, target_anchor, payload}` tuples here per pending Task. Sonnet `stage-file-apply` reads tuples and materializes BACKLOG rows + spec stubs. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_pending — populated by `/audit {{this-doc}} Stage {{N.M}}` once all Tasks reach Done post-verify._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---
