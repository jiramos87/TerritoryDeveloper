// stage8.0-sibling-slot.test.mjs — city-region-zoom-transition Stage 8.0
// Anchor: CellRenderer_ContractAndEvent
//
// Contract harness for sibling exploration region-scale-city-blocks.
// To use as acceptance gate:
//   1. Sibling registers its own IRegionCellRenderer impl into ServiceRegistry-Region BEFORE RegionManager.Start.
//   2. Run this test file — both assertions must stay GREEN against the sibling impl.
//   3. The same assertions that pass for BrownDiamondCellRenderer must pass for the sibling impl.
//
// Bridge action ids referenced below must be wired in AgentTestModeBatchRunner or a
// dedicated BridgeActionHandler in the RegionScene.

import { describe, it, expect, beforeAll, afterAll } from 'vitest';
import { BridgeClient } from '../../tools/scripts/recipe-engine/bridge-client.mjs';

const bridge = new BridgeClient();

// CellRenderer_ContractAndEvent — top-level anchor
describe('CellRenderer_ContractAndEvent', () => {
  beforeAll(async () => {
    await bridge.enterPlayMode();
  }, 60_000);

  afterAll(async () => {
    await bridge.exitPlayMode();
  }, 30_000);

  // T8.0.1 — IRegionCellRenderer.Render() called N times during stream-in
  it('IRegionCellRenderer.Render called N times (N = visible cell count) during stream-in', async () => {
    // Bridge: trigger CellStreamingPipeline.StreamCenterOut for a known budget, return render call count.
    // Action resets BrownDiamondCellRenderer.RenderCallCount, runs stream-in for visible set,
    // returns { render_call_count: number, visible_cell_count: number }.
    const result = await bridge.dispatchAction('region_cell_renderer.assert_render_call_count');
    expect(result).toBeTruthy();
    expect(typeof result.render_call_count).toBe('number');
    expect(typeof result.visible_cell_count).toBe('number');
    // Render() must be called exactly once per visible cell.
    expect(result.render_call_count).toBe(result.visible_cell_count);
    expect(result.visible_cell_count).toBeGreaterThan(0);
  });

  // T8.0.2 — PlayerCityDataUpdated fires exactly once on simulated city growth in Region context
  it('PlayerCityDataUpdated fires once on simulated city growth tick while in Region context', async () => {
    // Bridge: set IsoSceneContext = Region, call RegionManager.NotifyCityEvolved(state), return event_fire_count.
    // Returns { event_fire_count: number, rerender_cell_count: number, context_was_region: boolean }.
    const result = await bridge.dispatchAction('region_cell_renderer.assert_city_data_updated_event');
    expect(result).toBeTruthy();
    expect(result.context_was_region).toBe(true);
    // Event fires exactly once.
    expect(result.event_fire_count).toBe(1);
    // Re-render triggered for player 2x2 footprint (4 cells).
    expect(result.rerender_cell_count).toBe(4);
  });

  // T8.0.3 — PlayerCityDataUpdated does NOT fire when context is City
  it('PlayerCityDataUpdated does NOT fire when IsoSceneContext == City', async () => {
    // Bridge: set IsoSceneContext = City, call RegionManager.NotifyCityEvolved(state), return event_fire_count.
    // Returns { event_fire_count: number, context_was_region: boolean }.
    const result = await bridge.dispatchAction('region_cell_renderer.assert_no_event_in_city_context');
    expect(result).toBeTruthy();
    expect(result.context_was_region).toBe(false);
    // Guard: event must NOT fire in City context.
    expect(result.event_fire_count).toBe(0);
  });
});
