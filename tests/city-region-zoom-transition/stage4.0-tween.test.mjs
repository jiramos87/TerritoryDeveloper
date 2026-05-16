// stage4.0-tween.test.mjs — city-region-zoom-transition Stage 4.0
// Anchor: Tween_GeometricCrossfade_LiveCityRender
// Verifies: PrimeTween ortho-size InOutCubic 1.5–5s window,
// CrossfadeTriggerEvaluator fires once when footprint ⊆ anchor,
// spinner visible when tween ≥3s,
// 5s cap aborts → ErrorToast(LoadFailed).
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

  // T4.0.1 — PrimeTween InOutCubic ortho-size tween in [1.5, 5.0]s window
  it('ZoomTransitionController exists with camera reference', async () => {
    const go = await bridge.findGameObject('ZoomTransitionController');
    expect(go).toBeTruthy();
    const comp = await bridge.getComponent(go.id, 'ZoomTransitionController');
    expect(comp).toBeTruthy();
  });

  it('Tween duration is within [1.5, 5.0]s adaptive clamp', async () => {
    // Dispatch a tween-start action; bridge returns the computed duration.
    const result = await bridge.dispatchAction('zoom_transition.get_tween_duration');
    const duration = parseFloat(result);
    expect(duration).toBeGreaterThanOrEqual(1.5);
    expect(duration).toBeLessThanOrEqual(5.0);
  });

  // T4.0.2 — City SpriteRenderers remain live during tween (cullingMask not toggled)
  it('CityScene SpriteRenderers remain enabled during TweeningOut', async () => {
    // Bridge action samples cullingMask mid-tween and returns 'city_visible' | 'city_hidden'.
    const result = await bridge.dispatchAction('zoom_transition.assert_city_visible_during_tween');
    expect(result).toBe('city_visible');
  });

  // T4.0.3 — CrossfadeTriggerEvaluator fires exactly once
  it('CrossfadeTriggerEvaluator exists in scene', async () => {
    const go = await bridge.findGameObject('CrossfadeTriggerEvaluator');
    expect(go).toBeTruthy();
  });

  it('CrossfadeTriggerEvaluator fires region fade exactly once when footprint ⊆ anchor', async () => {
    // Bridge action: invoke ShouldFireRegionFade twice with contained rects → returns fire count.
    const result = await bridge.dispatchAction('crossfade_evaluator.test_fire_once');
    // Should fire on first call, not on second.
    expect(result).toBe('fired_once');
  });

  // T4.0.4 — Spinner at ≥3s; 5s cap aborts
  it('TweenSpinnerController exists in scene', async () => {
    const go = await bridge.findGameObject('TweenSpinnerController');
    expect(go).toBeTruthy();
  });

  it('Spinner overlay becomes visible when tween elapsed ≥3s', async () => {
    // Bridge action: stub TweenElapsed at 3.1s, sample spinner visibility.
    const result = await bridge.dispatchAction('tween_spinner.test_spinner_at_3s');
    expect(result).toBe('spinner_visible');
  });

  it('5s cap aborts transition and fires ErrorToast(LoadFailed)', async () => {
    // Bridge action: stub slow-load, drive TweenElapsed past 5s, capture abort signal.
    const result = await bridge.dispatchAction('zoom_transition.test_5s_cap_abort');
    // Returns 'load_failed_toast_shown' when cap fires + toast shown + state reverted to Idle.
    expect(result).toBe('load_failed_toast_shown');
  });
});
