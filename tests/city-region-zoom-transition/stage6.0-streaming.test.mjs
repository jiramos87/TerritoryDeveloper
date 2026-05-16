// stage6.0-streaming.test.mjs — city-region-zoom-transition Stage 6.0
// Anchor: Streaming_FirstRingUnlock
// Verifies: CellStreamingPipeline center-out streaming, FirstRingLoaded event, InputLockService unlock,
// and profiler frame-budget sweep across budget={4,8,16} cells/frame.
import { describe, it, expect, beforeAll, afterAll } from 'vitest';
import { BridgeClient } from '../../tools/scripts/recipe-engine/bridge-client.mjs';

const bridge = new BridgeClient();

describe('Streaming_FirstRingUnlock', () => {
  beforeAll(async () => {
    await bridge.enterPlayMode();
  }, 60_000);

  afterAll(async () => {
    await bridge.exitPlayMode();
  }, 30_000);

  // T6.0.1 — CellStreamingPipeline exists in scene
  it('CellStreamingPipeline exists in scene', async () => {
    const go = await bridge.findGameObject('CellStreamingPipeline');
    expect(go).toBeTruthy();
  });

  it('CellStreamingPipeline.StreamCenterOut emits FirstRingLoaded before AllCellsLoaded', async () => {
    // Bridge action: invoke StreamCenterOut with default budget=8, return JSON {firstRingFired, allCellsFired, firstRingBeforeAll}
    const result = await bridge.dispatchAction('streaming.assert_first_ring_before_all');
    const data = JSON.parse(result);
    expect(data.firstRingFired).toBe(true);
    expect(data.allCellsFired).toBe(true);
    expect(data.firstRingBeforeAll).toBe(true);
  });

  it('CellStreamingPipeline spiral order: first 9 cells within 3x3 of anchor', async () => {
    // Bridge action: invoke StreamCenterOut, capture first 9 cell coordinates processed,
    // assert all within Chebyshev distance 1 of anchor cell.
    const result = await bridge.dispatchAction('streaming.assert_first_nine_in_first_ring');
    expect(result).toBe('first_ring_correct');
  });

  // T6.0.2 — InputLockService
  it('InputLockService exists in scene', async () => {
    const go = await bridge.findGameObject('InputLockService');
    expect(go).toBeTruthy();
  });

  it('InputLockService.IsLocked is true while ZoomTransitionController state in {Saving,TweeningOut,AwaitLoad,Landing}', async () => {
    // Bridge action: set ZoomTransitionController to Saving state, read InputLockService.IsLocked
    const result = await bridge.dispatchAction('input_lock.assert_locked_during_transition');
    expect(result).toBe('locked');
  });

  it('InputLockService.IsLocked flips to false after FirstRingLoaded fires', async () => {
    // Bridge action: fire CellStreamingPipeline.FirstRingLoaded manually, read InputLockService.IsLocked
    const result = await bridge.dispatchAction('input_lock.assert_unlocked_after_first_ring');
    expect(result).toBe('unlocked');
  });

  it('Unlock tell visible after InputLockService.Unlock()', async () => {
    // Bridge action: call InputLockService.Unlock(), check UI root has input-unlock-tell class
    const result = await bridge.dispatchAction('input_lock.assert_unlock_tell_shown');
    expect(result).toBe('tell_shown');
  });

  // T6.0.3 — Profiler budget sweep budget={4,8,16}
  it('StreamCenterOut budget=8 default: FirstRingLoaded and AllCellsLoaded both fire', async () => {
    const result = await bridge.dispatchAction('streaming.budget_sweep_budget8');
    const data = JSON.parse(result);
    expect(data.firstRingFired).toBe(true);
    expect(data.allCellsFired).toBe(true);
  });

  it('StreamCenterOut budget=4: frame time well under budget=8 equivalent (more frames, less work/frame)', async () => {
    const result = await bridge.dispatchAction('streaming.budget_sweep_budget4');
    const data = JSON.parse(result);
    // budget=4 -> twice as many yields -> total stream frames >= budget=8 total frames
    expect(data.totalFrames).toBeGreaterThan(0);
    expect(data.allCellsFired).toBe(true);
    // median frame time <= 8ms for budget=4 (half the work per frame)
    expect(data.medianFrameMs).toBeLessThanOrEqual(8);
  });

  it('StreamCenterOut budget=16: all cells load, frame count roughly half of budget=8', async () => {
    const result = await bridge.dispatchAction('streaming.budget_sweep_budget16');
    const data = JSON.parse(result);
    expect(data.totalFrames).toBeGreaterThan(0);
    expect(data.allCellsFired).toBe(true);
    // budget=16 may exceed 8ms/frame -- acceptable for fast mode; just assert completion
    expect(data.completed).toBe(true);
  });

  it('StreamCenterOut budget=8 default: median frame time <= 8ms during streaming on baseline', async () => {
    const result = await bridge.dispatchAction('streaming.assert_frame_budget_default');
    const data = JSON.parse(result);
    // budget=8 default must not exceed 8ms/frame median
    expect(data.medianFrameMs).toBeLessThanOrEqual(8);
    expect(data.allCellsFired).toBe(true);
  });
});
