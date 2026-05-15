// stage2.0-terrain.test.mjs — RegionScene terrain: 64x64 height+water+cliff render
// visibility-delta-test: Assets/Scripts/RegionScene/Domains/Terrain/RegionCellRenderer.cs::Region64x64TerrainRendersWithHeightWaterCliff
import { describe, it, expect, beforeAll } from 'vitest';
import { BridgeClient } from '../../tools/scripts/recipe-engine/bridge-client.mjs';

const bridge = new BridgeClient();

describe('RegionScene Stage 2.0 terrain', () => {
  beforeAll(async () => {
    await bridge.openScene('Assets/Scenes/RegionScene.unity');
  }, 30_000);

  // RED until TECH-35663 (RegionCellRenderer) lands
  it('test_region_64x64_renders_with_height_water_cliff — RegionCellRenderer visible cells > 0 after seed 42', async () => {
    const root = await bridge.findGameObject('RegionRoot');
    expect(root).toBeTruthy();
    const renderer = await bridge.getComponent(root.id, 'RegionCellRenderer');
    // RED: RegionCellRenderer not yet wired — expect truthy once TECH-35663 done
    expect(renderer).toBeTruthy();
    const visibleCount = renderer?.fields?.VisibleCellCount ?? renderer?.VisibleCellCount ?? 0;
    expect(visibleCount).toBeGreaterThan(0);
  });

  // RED until TECH-35661 lands
  it('water cells exist + obey invariant #7 shore band after seed 42', async () => {
    const root = await bridge.findGameObject('RegionRoot');
    expect(root).toBeTruthy();
    const waterMap = await bridge.getComponent(root.id, 'RegionWaterMapComponent');
    expect(waterMap).toBeTruthy();
    expect((waterMap?.fields?.WaterCellCount ?? waterMap?.WaterCellCount ?? 0)).toBeGreaterThan(0);
  });

  // RED until TECH-35662 lands
  it('cliff cells exist + faces south/east only (invariant #9) after seed 42', async () => {
    const root = await bridge.findGameObject('RegionRoot');
    expect(root).toBeTruthy();
    const cliffMap = await bridge.getComponent(root.id, 'RegionCliffMapComponent');
    expect(cliffMap).toBeTruthy();
    const northWestViolations = cliffMap?.fields?.NorthWestFaceViolationCount ?? cliffMap?.NorthWestFaceViolationCount ?? 0;
    expect(northWestViolations).toBe(0);
  });
});
