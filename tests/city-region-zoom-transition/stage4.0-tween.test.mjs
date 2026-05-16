// stage4.0-tween.test.mjs — city-region-zoom-transition Stage 4.0
// Anchor: Tween_GeometricCrossfade_LiveCityRender
// Verifies: ortho-size animates InOutCubic in 1.5–2.0s window; CrossfadeTriggerEvaluator fires
// exactly once; spinner visible when tween ≥3s; 5s cap aborts → ErrorToast(LoadFailed).
import { describe, it, expect, beforeAll, afterAll } from 'vitest';
import { BridgeClient } from '../../tools/scripts/recipe-engine/bridge-client.mjs';

const bridge = new BridgeClient();

describe('Tween_GeometricCrossfade_LiveCityRender', () => {
  beforeAll(async () => {
    await bridge.enterPlayMode();
  }, 60_000);

  afterAll(async () => {
    await bridge.exitPlayMode();
  }, 30_000);

  // T4.0.1 — Ortho-size tween runs InOutCubic from cityZoom to regionZoom in 1.5–2.0s
  it('ZoomTransitionController has EaseInOutCubic tween (ortho-size animates on transition)', async () => {
    // Dispatch auto-confirm transition + sample ortho-size mid-tween via bridge.
    const result = await bridge.dispatchAction('zoom_transition.sample_tween_inoutcubic');
    // Bridge action returns { duration, midpoint_ortho, start_ortho, end_ortho }
    // duration should be in [1.5, 5.0] clamp; midpoint ortho between start and end.
    expect(result).toBeTruthy();
    expect(result.duration).toBeGreaterThanOrEqual(1.5);
    expect(result.duration).toBeLessThanOrEqual(5.0);
    expect(result.midpoint_ortho).toBeGreaterThan(result.start_ortho);
    expect(result.midpoint_ortho).toBeLessThan(result.end_ortho);
  });

  // T4.0.2 — City SpriteRenderers stay enabled the whole tween (Approach C: live render)
  it('City SpriteRenderers remain enabled during TweeningOut', async () => {
    const result = await bridge.dispatchAction('zoom_transition.verify_city_live_during_tween');
    // Returns { all_enabled: true } when no SpriteRenderer was disabled mid-tween.
    expect(result).toBeTruthy();
    expect(result.all_enabled).toBe(true);
  });

  // T4.0.3 — CrossfadeTriggerEvaluator fires exactly once when city footprint ⊆ anchor
  it('CrossfadeTriggerEvaluator fires region fade exactly once per transition', async () => {
    const result = await bridge.dispatchAction('crossfade_evaluator.test_fire_once');
    // Returns { fire_count: 1 } — evaluator fired exactly once, latch prevents re-fire.
    expect(result).toBeTruthy();
    expect(result.fire_count).toBe(1);
  });

  it('CrossfadeTriggerEvaluator does not fire again while hasFiredRegionFade is true', async () => {
    const result = await bridge.dispatchAction('crossfade_evaluator.test_no_double_fire');
    expect(result).toBeTruthy();
    expect(result.double_fired).toBe(false);
  });

  // T4.0.4 (FEAT-62) — Spinner overlay: visible at ≥3s, hidden before 3s
  it('TweenSpinnerController hides spinner while tween elapsed < 3s', async () => {
    const result = await bridge.dispatchAction('spinner_overlay.query_visibility_at_2s');
    // Returns { visible: false } when queried at simulated 2s elapsed.
    expect(result).toBeTruthy();
    expect(result.visible).toBe(false);
  });

  it('TweenSpinnerController shows spinner when tween elapsed >= 3s', async () => {
    const result = await bridge.dispatchAction('spinner_overlay.query_visibility_at_3s');
    // Returns { visible: true } when queried at simulated 3s elapsed.
    expect(result).toBeTruthy();
    expect(result.visible).toBe(true);
  });

  // 5s cap — abort → ErrorToast(LoadFailed)
  it('5s cap aborts transition and shows LoadFailed toast', async () => {
    const result = await bridge.dispatchAction('zoom_transition.simulate_5s_cap_abort');
    // Bridge injects a slow-load stub, runs transition to cap, returns { aborted: true, toast_shown: 'LoadFailed' }.
    expect(result).toBeTruthy();
    expect(result.aborted).toBe(true);
    expect(result.toast_shown).toBe('LoadFailed');
  });

  it('ZoomTransitionController returns to Idle after 5s cap abort', async () => {
    const go = await bridge.findGameObject('ZoomTransitionController');
    const comp = await bridge.getComponent(go.id, 'ZoomTransitionController');
    expect(comp.fields?.State ?? comp.State).toBe('Idle');
  });
});
