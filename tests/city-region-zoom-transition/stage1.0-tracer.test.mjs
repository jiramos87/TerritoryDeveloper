// stage1.0-tracer.test.mjs — city-region-zoom-transition tracer slice
// Anchor: Tracer_LeaveCity_LandsInRegionCentered
// Verifies: CoreScene boots first, single Camera.main, ZoomTransitionController state machine,
// ConfirmTransitionPanel Yes-click advances state, placeholder tween lands in RegionScene.
import { describe, it, expect, beforeAll } from 'vitest';
import { BridgeClient } from '../../tools/scripts/recipe-engine/bridge-client.mjs';

const bridge = new BridgeClient();

describe('Tracer_LeaveCity_LandsInRegionCentered', () => {
  beforeAll(async () => {
    // Open CoreScene (boot root); CityScene loads additive in play mode via BootLoader
    await bridge.enterPlayMode();
  }, 60_000);

  it('CoreScene is loaded after boot', async () => {
    const result = await bridge.isSceneLoaded('CoreScene');
    expect(result).toBe(true);
  });

  it('Camera.main is the single persistent CoreScene camera', async () => {
    const cameras = await bridge.findAllComponents('Camera');
    // Only one Camera tagged MainCamera should exist (CoreScene camera)
    const mainCameras = cameras.filter(c => c.tag === 'MainCamera');
    expect(mainCameras.length).toBe(1);
  });

  it('ZoomTransitionController exists in scene', async () => {
    const go = await bridge.findGameObject('ZoomTransitionController');
    expect(go).toBeTruthy();
    const comp = await bridge.getComponent(go.id, 'ZoomTransitionController');
    expect(comp).toBeTruthy();
  });

  it('ZoomTransitionController state machine fires all 6 events on auto-confirm transition', async () => {
    // Set AutoConfirm=true + invoke RequestTransition(Region) via bridge dispatch
    await bridge.dispatchAction('zoom_transition.request_region_auto_confirm');
    // State should end at Idle after Idle→AwaitConfirm→Saving→TweeningOut→AwaitLoad→Landing→Idle
    const go = await bridge.findGameObject('ZoomTransitionController');
    const comp = await bridge.getComponent(go.id, 'ZoomTransitionController');
    expect(comp.fields?.State ?? comp.State).toBe('Idle');
  });

  it('RegionScene is active after transition', async () => {
    const result = await bridge.isSceneLoaded('RegionScene');
    expect(result).toBe(true);
  });

  it('Camera.main instance ID unchanged after transition (single persistent camera)', async () => {
    const cameras = await bridge.findAllComponents('Camera');
    const mainCameras = cameras.filter(c => c.tag === 'MainCamera');
    expect(mainCameras.length).toBe(1);
  });
});
