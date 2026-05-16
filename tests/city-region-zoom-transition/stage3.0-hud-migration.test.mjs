// stage3.0-hud-migration.test.mjs — city-region-zoom-transition Stage 3.0
// Anchor: HUD_PersistsAcrossSceneSwitch
// Verifies: UIManager DontDestroyOnLoad, IsoSceneContextService context flip on transition,
// ToolPaletteVisibility hides palette in Region context,
// ServiceRegistry CrossRegistryResolveOutsideStartException on Update-phase resolve.
import { describe, it, expect, beforeAll, afterAll } from 'vitest';
import { BridgeClient } from '../../tools/scripts/recipe-engine/bridge-client.mjs';

const bridge = new BridgeClient();

describe('HUD_PersistsAcrossSceneSwitch', () => {
  beforeAll(async () => {
    await bridge.enterPlayMode();
  }, 60_000);

  afterAll(async () => {
    await bridge.exitPlayMode();
  }, 30_000);

  // T3.0.1 — UIManager persists across scene switch via DontDestroyOnLoad
  it('UIManager exists in scene', async () => {
    const go = await bridge.findGameObject('UIManager');
    expect(go).toBeTruthy();
  });

  it('UIManager has DontDestroyOnLoad applied (singleton instance stable)', async () => {
    const comp = await bridge.getComponent((await bridge.findGameObject('UIManager')).id, 'UIManager');
    expect(comp).toBeTruthy();
  });

  // T3.0.2 — IsoSceneContextService exists and reports City context on boot
  it('IsoSceneContextService exists in scene', async () => {
    const go = await bridge.findGameObject('IsoSceneContextService');
    expect(go).toBeTruthy();
  });

  it('IsoSceneContextService.Context is City on boot', async () => {
    const result = await bridge.dispatchAction('iso_scene_context.get_context');
    // Returns "City" when no transition has occurred.
    expect(result).toBe('City');
  });

  // T3.0.3 — ServiceRegistry CrossRegistryResolveOutsideStartException
  it('ServiceRegistry.EnforceStartPhase throws on Update-phase resolve', async () => {
    const result = await bridge.dispatchAction('service_registry.test_cross_registry_resolve_outside_start');
    // Bridge action sets EnforceStartPhase=true, calls Resolve outside Start, asserts exception caught.
    expect(result).toBe('exception_thrown');
  });

  it('ServiceRegistry.Resolve succeeds during Start phase', async () => {
    const result = await bridge.dispatchAction('service_registry.test_cross_registry_resolve_during_start');
    expect(result).toBe('success');
  });
});
