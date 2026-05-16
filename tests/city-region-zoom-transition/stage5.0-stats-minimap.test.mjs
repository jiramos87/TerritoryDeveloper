// stage5.0-stats-minimap.test.mjs — city-region-zoom-transition Stage 5.0
// Anchor: StatsPanel_AndMinimap_OnLanding
// Verifies: WelcomeStatsPanel 4-field population/treasury/elapsed/dormant on region landing,
// MinimapController mode-switch shows cached texture (no blank-flash frame).
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

  // T5.0.1 — WelcomeStatsPanel exists and has 4 field labels wired
  it('WelcomeStatsPanelController exists in scene', async () => {
    const go = await bridge.findGameObject('WelcomeStatsPanelController');
    expect(go).toBeTruthy();
  });

  it('WelcomeStatsPanel shows all 4 fields on region landing', async () => {
    // Bridge action: drive ZoomTransitionController to Landing state with Region target,
    // return JSON with {population, treasury, elapsed, dormant}.
    const result = await bridge.dispatchAction('welcome_stats.assert_fields_on_landing');
    const data = JSON.parse(result);
    // population = sum across known cells (>= 0)
    expect(data.population).toBeGreaterThanOrEqual(0);
    // treasury = placeholder '--'
    expect(data.treasury).toBe('--');
    // elapsed = ticks >= 0
    expect(data.elapsed).toBeGreaterThanOrEqual(0);
    // dormant = knownCities - 1 (>= 0)
    expect(data.dormant).toBeGreaterThanOrEqual(0);
  });

  it('WelcomeStatsPanel is visible after landing state', async () => {
    // Bridge action: check IsVisible on WelcomeStatsPanelController after landing.
    const result = await bridge.dispatchAction('welcome_stats.assert_panel_visible');
    expect(result).toBe('visible');
  });

  // T5.0.2 — MinimapController persists across scene loads (DontDestroyOnLoad)
  it('MiniMapController exists in scene', async () => {
    const go = await bridge.findGameObject('MiniMapController');
    expect(go).toBeTruthy();
  });

  it('MiniMapController.Awake applies DontDestroyOnLoad (single instance)', async () => {
    // Bridge action: count MiniMapController instances -- must be exactly 1.
    const result = await bridge.dispatchAction('minimap.assert_single_instance');
    expect(result).toBe('single_instance');
  });

  // T5.0.3 — Mode-switch cached texture no blank-flash
  it('MinimapController shows cached city texture during region regen (no blank-flash)', async () => {
    // Bridge action: switch mode City->Region, capture texture pointer at frame T and T+1.
    // Returns 'cached_shown' if texture pointer non-null for all sampled frames.
    const result = await bridge.dispatchAction('minimap.assert_cached_on_mode_switch');
    expect(result).toBe('cached_shown');
  });

  it('MinimapController mode is Region after IsoSceneContext=Region', async () => {
    // Bridge action: set IsoSceneContextService.Context = Region, read MiniMapController.CurrentMode.
    const result = await bridge.dispatchAction('minimap.assert_mode_region_after_context_change');
    expect(result).toBe('mode_region');
  });
});
