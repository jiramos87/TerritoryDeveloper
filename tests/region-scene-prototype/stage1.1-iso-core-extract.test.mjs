// stage1.1-iso-core-extract.test.mjs — IsoSceneCore service extraction: camera, culler, tick bus
// tracer-verb-test: Assets/Scripts/Domains/IsoSceneCore/Services/IsoSceneCamera.cs::CameraServiceMirrorsGridManagerPan
import { describe, it, expect, beforeAll } from 'vitest';
import { BridgeClient } from '../../tools/scripts/recipe-engine/bridge-client.mjs';

const bridge = new BridgeClient();

describe('IsoSceneCore Stage 1.1 — ISO core extraction', () => {
  beforeAll(async () => {
    await bridge.openScene('Assets/Scenes/CityScene.unity');
  }, 30_000);

  it('IsoSceneCamera service registered in ServiceRegistry', async () => {
    // Red at T1.1.1 (no registration yet); green at T1.1.2 (CameraController registers)
    const result = await bridge.invokeMethod('ServiceRegistry', 'ResolveByName', ['IsoSceneCamera']);
    expect(result).toBeTruthy();
  });

  it('Camera pan parity — camera moves right after RightArrow tick', async () => {
    // Red at T1.1.1; green at T1.1.2
    const before = await bridge.getCameraPosition();
    await bridge.simulateKeyPress('RightArrow', 0.1);
    const after = await bridge.getCameraPosition();
    expect(after.x).toBeGreaterThan(before.x);
  });

  it('IsoSceneChunkCuller visible_cells count > 0 after camera tick', async () => {
    // Red until T1.1.3
    const result = await bridge.invokeMethod('ServiceRegistry', 'ResolveByName', ['IsoSceneChunkCuller']);
    expect(result).toBeTruthy();
    const count = result?.visibleCellCount ?? result?.fields?.visibleCellCount;
    expect(count).toBeGreaterThan(0);
  });

  it('IsoSceneTickBus has subscribers after CityScene warmup', async () => {
    // Red until T1.1.4
    const result = await bridge.invokeMethod('ServiceRegistry', 'ResolveByName', ['IsoSceneTickBus']);
    expect(result).toBeTruthy();
    const hasSubs = result?.hasSubscribers ?? result?.fields?.hasSubscribers;
    expect(hasSubs).toBe(true);
  });
});
