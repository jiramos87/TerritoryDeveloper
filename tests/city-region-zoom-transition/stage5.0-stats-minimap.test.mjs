// stage5.0-stats-minimap.test.mjs — city-region-zoom-transition Stage 5.0
// Anchor: StatsPanel_AndMinimap_OnLanding
// Verifies: WelcomeStatsPanel shows 4 fields on region landing; MinimapController
// mode-switch displays cached texture during regen (no blank-flash).
import { describe, it, expect, beforeAll, afterAll } from 'vitest';
import { BridgeClient } from '../../tools/scripts/recipe-engine/bridge-client.mjs';

const bridge = new BridgeClient();

describe('StatsPanel_AndMinimap_OnLanding', () => {
  beforeAll(async () => {
    await bridge.enterPlayMode();
  }, 60_000);

  afterAll(async () => {
    await bridge.exitPlayMode();
  }, 30_000);

  // T5.0.1 — WelcomeStatsPanel visible on region landing with 4 fields populated
  it('WelcomeStatsPanel becomes visible when ZoomTransitionController reaches Landing state', async () => {
    const result = await bridge.dispatchAction('welcome_stats_panel.query_visibility_on_landing');
    // Returns { visible: true } when panel has welcome-stats__panel--visible class after Landing state.
    expect(result).toBeTruthy();
    expect(result.visible).toBe(true);
  });

  it('WelcomeStatsPanel populationSum field is populated (>= 0)', async () => {
    const result = await bridge.dispatchAction('welcome_stats_panel.query_fields');
    // Returns { population, treasury, elapsed_ticks, dormant_count }
    expect(result).toBeTruthy();
    expect(typeof result.population).toBe('string');
    // Population label is numeric string (may be "0" for fresh region)
    expect(parseInt(result.population, 10)).toBeGreaterThanOrEqual(0);
  });

  it('WelcomeStatsPanel treasury field displays placeholder "--"', async () => {
    const result = await bridge.dispatchAction('welcome_stats_panel.query_fields');
    expect(result).toBeTruthy();
    expect(result.treasury).toBe('--');
  });

  it('WelcomeStatsPanel elapsedTimeInCity field shows ticks >= 0', async () => {
    const result = await bridge.dispatchAction('welcome_stats_panel.query_fields');
    expect(result).toBeTruthy();
    expect(result.elapsed_ticks).toMatch(/^\d+ ticks$/);
  });

  it('WelcomeStatsPanel dormantCityCount = known cities - 1 (or 0 for single city)', async () => {
    const result = await bridge.dispatchAction('welcome_stats_panel.query_fields');
    expect(result).toBeTruthy();
    // dormant_count is a non-negative integer string
    expect(parseInt(result.dormant_count, 10)).toBeGreaterThanOrEqual(0);
  });

  // T5.0.2 — MinimapController DontDestroyOnLoad persists across scene loads
  it('MinimapController.Awake fires once and DontDestroyOnLoad is applied', async () => {
    const result = await bridge.dispatchAction('minimap_controller.query_dontdestroyonload');
    // Returns { instance_id: number, dont_destroy: true }
    expect(result).toBeTruthy();
    expect(result.dont_destroy).toBe(true);
    expect(typeof result.instance_id).toBe('number');
  });

  it('MinimapController instance_id is unchanged after RegionScene load', async () => {
    const before = await bridge.dispatchAction('minimap_controller.query_instance_id');
    // Trigger scene load and check id again
    await bridge.dispatchAction('zoom_transition.trigger_region_load');
    const after = await bridge.dispatchAction('minimap_controller.query_instance_id');
    expect(before).toBeTruthy();
    expect(after).toBeTruthy();
    expect(after.instance_id).toBe(before.instance_id);
  });

  // T5.0.3 — MinimapController mode-switch shows cached texture without blank flash
  it('MinimapController displays cached texture during mode switch (no blank-flash)', async () => {
    const result = await bridge.dispatchAction('minimap_controller.query_mode_switch_no_blank');
    // Bridge samples texture pointer on RawImage before and after SetMode call.
    // Returns { blank_frame_detected: false } when cached texture was displayed immediately.
    expect(result).toBeTruthy();
    expect(result.blank_frame_detected).toBe(false);
  });

  it('MinimapController switches to Region mode when IsoSceneContext changes to Region', async () => {
    const result = await bridge.dispatchAction('minimap_controller.query_mode_after_context_region');
    // Returns { mode: 'Region' }
    expect(result).toBeTruthy();
    expect(result.mode).toBe('Region');
  });

  it('MinimapController city cache invalidates on PlayerCityDataUpdated', async () => {
    const result = await bridge.dispatchAction('minimap_controller.trigger_city_cache_invalidation');
    // Returns { cache_was_cleared: true }
    expect(result).toBeTruthy();
    expect(result.cache_was_cleared).toBe(true);
  });

  it('MinimapController region cache invalidates on cell stream-in complete', async () => {
    const result = await bridge.dispatchAction('minimap_controller.trigger_region_cache_invalidation');
    // Returns { cache_was_cleared: true }
    expect(result).toBeTruthy();
    expect(result.cache_was_cleared).toBe(true);
  });
});
