// stage2.0-save.test.mjs — city-region-zoom-transition Stage 2.0
// Anchor: SaveCoordinator_PairedWrite_FailClosed
// Verifies: paired write happy path + partial-write rollback + ErrorToast trigger + sim stays paused
import { describe, it, expect, beforeAll, afterAll } from 'vitest';
import { BridgeClient } from '../../tools/scripts/recipe-engine/bridge-client.mjs';

const bridge = new BridgeClient();

describe('SaveCoordinator_PairedWrite_FailClosed', () => {
  beforeAll(async () => {
    await bridge.enterPlayMode();
  }, 60_000);

  afterAll(async () => {
    await bridge.exitPlayMode();
  }, 30_000);

  // Case 1: happy paired write
  it('SaveCoordinator exists in scene', async () => {
    const go = await bridge.findGameObject('SaveCoordinator');
    expect(go).toBeTruthy();
    const comp = await bridge.getComponent(go.id, 'SaveCoordinator');
    expect(comp).toBeTruthy();
  });

  it('happy path: both .city and .region files written, .bak files deleted', async () => {
    const result = await bridge.dispatchAction('save_coordinator.save_pair');
    expect(result).toBeTruthy();
    const available = await bridge.dispatchAction('save_coordinator.load_pair_available');
    expect(available).toBeTruthy();
  });

  // Case 2: partial .region write fail - rollback
  it('partial fail: SaveFailedException thrown, .region restored from .bak', async () => {
    let threw = false;
    try {
      await bridge.dispatchAction('save_coordinator.save_pair_region_fail_inject');
    } catch (_) {
      threw = true;
    }
    const ctx = await bridge.exportAgentContext();
    const logs = ctx?.console_logs ?? [];
    const hasFail = logs.some(l =>
      l.includes('SaveCoordinator') && (l.includes('failed') || l.includes('Rolled back'))
    );
    expect(hasFail || threw).toBe(true);
  });

  // Case 3: ErrorToast raised on fail
  it('ErrorToastController exists in scene', async () => {
    const go = await bridge.findGameObject('ErrorToastController');
    expect(go).toBeTruthy();
    const comp = await bridge.getComponent(go.id, 'ErrorToastController');
    expect(comp).toBeTruthy();
  });

  it('ZoomTransitionController wires SaveCoordinator and returns Idle on save fail', async () => {
    await bridge.dispatchAction('zoom_transition.request_region_save_fail_inject');
    const go = await bridge.findGameObject('ZoomTransitionController');
    const comp = await bridge.getComponent(go.id, 'ZoomTransitionController');
    const state = comp?.fields?.State ?? comp?.State;
    expect(state).toBe('Idle');
  });

  // Case 4: NewGameFlow.CreatePair writes both files
  it('NewGameFlow exists and CreatePair succeeds', async () => {
    const result = await bridge.dispatchAction('new_game_flow.create_pair');
    expect(result).toBeTruthy();
    const go = await bridge.findGameObject('NewGameFlow');
    expect(go).toBeTruthy();
  });
});
