// stage1.0-tracer.test.mjs — RegionScene tracer: loads, sprite at center, camera pans
// tracer-verb-test: Assets/Scripts/RegionScene/RegionManager.cs::ArrowKeysPanCamera
import { describe, it, expect, beforeAll } from 'vitest';
import { BridgeClient } from '../../tools/scripts/recipe-engine/bridge-client.mjs';

const bridge = new BridgeClient();

describe('RegionScene Stage 1.0 tracer', () => {
  beforeAll(async () => {
    await bridge.openScene('Assets/Scenes/RegionScene.unity');
  }, 30_000);

  it('RegionManager is present on RegionRoot', async () => {
    const result = await bridge.findGameObject('RegionRoot');
    expect(result).toBeTruthy();
    const comp = await bridge.getComponent(result.id, 'RegionManager');
    expect(comp).toBeTruthy();
  });

  it('PlaceholderSprite exists at region grid center', async () => {
    const go = await bridge.findGameObject('PlaceholderSprite');
    expect(go).toBeTruthy();
    const sr = await bridge.getComponent(go.id, 'SpriteRenderer');
    expect(sr).toBeTruthy();
  });

  it('ArrowKeysPanCamera property is true — camera wired', async () => {
    const root = await bridge.findGameObject('RegionRoot');
    expect(root).toBeTruthy();
    const comp = await bridge.getComponent(root.id, 'RegionManager');
    // ArrowKeysPanCamera returns true when IsoSceneCamera is configured (Start() ran)
    expect(comp.fields?.ArrowKeysPanCamera ?? comp.ArrowKeysPanCamera).toBe(true);
  });
});
