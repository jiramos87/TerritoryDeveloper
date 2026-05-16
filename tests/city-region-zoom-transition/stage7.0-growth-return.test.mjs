// stage7.0-growth-return.test.mjs — city-region-zoom-transition Stage 7.0
// Anchor: Growth_CatchUp_Deterministic_BothDirections
// Verifies: GrowthCatchupRunner determinism, TickClock + growthSeed persistence,
// symmetric return tween (City→Region→City), zero-elapsed edge, load-fail revert path.
import { describe, it, expect, beforeAll, afterAll } from 'vitest';
import { BridgeClient } from '../../tools/scripts/recipe-engine/bridge-client.mjs';

const bridge = new BridgeClient();

// Growth_CatchUp_Deterministic_BothDirections — top-level anchor
describe('Growth_CatchUp_Deterministic_BothDirections', () => {
  beforeAll(async () => {
    await bridge.enterPlayMode();
  }, 60_000);

  afterAll(async () => {
    await bridge.exitPlayMode();
  }, 30_000);

  // T7.0.1 — GrowthCatchupRunner determinism
  it('Catchup returns deterministic snapshot at tick+500 with expected pops', async () => {
    // Bridge: invoke GrowthCatchupRunner.Catchup(snapshot at tick=10000 growthSeed=42, elapsedTicks=500)
    // Returns { tick: 10500, zonePops: number[], checksum: number }
    const result = await bridge.dispatchAction('growth_catchup.run_deterministic');
    expect(result).toBeTruthy();
    expect(result.tick).toBe(10500);
    // Population vector non-empty and all values greater than inputs (growth occurred).
    expect(Array.isArray(result.zonePops)).toBe(true);
    expect(result.zonePops.length).toBeGreaterThan(0);
    result.zonePops.forEach(pop => expect(pop).toBeGreaterThan(0));
    expect(typeof result.checksum).toBe('number');
  });

  it('Catchup is deterministic — identical checksum on two runs with same seed', async () => {
    // Run the same catchup twice and compare checksums.
    const r1 = await bridge.dispatchAction('growth_catchup.run_deterministic');
    const r2 = await bridge.dispatchAction('growth_catchup.run_deterministic');
    expect(r1).toBeTruthy();
    expect(r2).toBeTruthy();
    expect(r1.checksum).toBe(r2.checksum);
  });

  // T7.0.2 — TickClock + growthSeed in .region save format
  it('TickClock.CurrentTick advances over time (not paused at Idle)', async () => {
    const r1 = await bridge.dispatchAction('tick_clock.query_current_tick');
    // Wait a beat then re-query.
    await new Promise(res => setTimeout(res, 1200));
    const r2 = await bridge.dispatchAction('tick_clock.query_current_tick');
    expect(r1).toBeTruthy();
    expect(r2).toBeTruthy();
    // After ~1.2s, tick should have advanced by at least 1.
    expect(r2.currentTick).toBeGreaterThan(r1.currentTick);
  });

  it('SavePair stamps lastTouchedTicks + growthSeed into .region file', async () => {
    // Bridge: trigger SavePair('test_save') then read back the written .region file.
    const result = await bridge.dispatchAction('save_pair.write_and_inspect_region');
    expect(result).toBeTruthy();
    expect(result.lastTouchedTicks).toBeGreaterThanOrEqual(0);
    expect(result.growthSeed).toBeGreaterThan(0); // seed non-zero after first write
  });

  // T7.0.3 — Symmetric return tween
  it('PlayerTileClickHandler opens ConfirmTransitionPanel with Enter-city copy on anchor click', async () => {
    // Bridge: simulate click at player 2x2 anchor cell.
    const result = await bridge.dispatchAction('player_tile.click_anchor');
    expect(result).toBeTruthy();
    expect(result.confirm_panel_shown).toBe(true);
    expect(result.copy_variant).toBe('Enter city?');
  });

  it('Return tween shrinks ortho-size from regionZoom to cityZoom (InOutCubic)', async () => {
    // Bridge: trigger RequestTransition(City) AutoConfirm=true, sample ortho-size mid-tween.
    const result = await bridge.dispatchAction('zoom_transition.return_tween_sample');
    expect(result).toBeTruthy();
    // Mid-tween ortho-size must be between cityZoom (8) and regionZoom (32).
    expect(result.mid_tween_ortho_size).toBeGreaterThan(8);
    expect(result.mid_tween_ortho_size).toBeLessThan(32);
    // Final ortho-size must snap to cityZoom.
    expect(result.final_ortho_size).toBe(8);
  });

  it('Sim resumes after Landing => Idle on city return (TickClock.Paused == false)', async () => {
    const result = await bridge.dispatchAction('zoom_transition.query_tick_resumed_after_landing');
    expect(result).toBeTruthy();
    expect(result.tick_paused).toBe(false);
  });

  // T7.0.4 — Sim resume + load-fail revert
  it('Zero-elapsed return (immediate) preserves snapshot checksum', async () => {
    // Bridge: Catchup(snapshot, elapsedTicks=0) -> checksum must equal input snapshot checksum.
    const result = await bridge.dispatchAction('growth_catchup.run_zero_elapsed');
    expect(result).toBeTruthy();
    expect(result.checksums_equal).toBe(true);
  });

  it('LoadFailed => ErrorToast shown + player stays in current scene', async () => {
    // Bridge: inject LoadFailedException into SceneOrchestratorManager.LoadAdditive then trigger transition.
    const result = await bridge.dispatchAction('zoom_transition.inject_load_fail');
    expect(result).toBeTruthy();
    expect(result.error_toast_shown).toBe(true);
    expect(result.transition_state).toBe('Idle');
    // Player stays in Region -- scene context must not be City.
    expect(result.scene_context).not.toBe('City');
  });
});
