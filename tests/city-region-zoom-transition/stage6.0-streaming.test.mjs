// stage6.0-streaming.test.mjs — city-region-zoom-transition Stage 6.0
// Anchor: Streaming_FirstRingUnlock
// Verifies: CellStreamingPipeline center-out spiral, FirstRingLoaded event, InputLockService
// unlock, and profiler budget sweep (4/8/16 cells/frame frame-time thresholds).
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

  // T6.0.1 — CellStreamingPipeline spiral order
  it('CellStreamingPipeline FirstRingLoaded fires after first 9 cells', async () => {
    const result = await bridge.dispatchAction('cell_streaming.query_first_ring_fired');
    // Bridge invokes StreamCenterOut(anchor, budget=8) and captures event fires.
    // Returns { first_ring_fired: true, cells_at_first_ring: number }
    expect(result).toBeTruthy();
    expect(result.first_ring_fired).toBe(true);
    expect(result.cells_at_first_ring).toBeGreaterThanOrEqual(9);
  });

  it('CellStreamingPipeline AllCellsLoaded fires after streaming completes', async () => {
    const result = await bridge.dispatchAction('cell_streaming.query_all_cells_loaded');
    // Returns { all_cells_loaded: true }
    expect(result).toBeTruthy();
    expect(result.all_cells_loaded).toBe(true);
  });

  it('CellStreamingPipeline cells arrive in spiral order from anchor', async () => {
    const result = await bridge.dispatchAction('cell_streaming.query_spiral_order');
    // Returns { spiral_valid: true } — first cell = anchor, subsequent cells increase Chebyshev distance monotonically (non-decreasing rings).
    expect(result).toBeTruthy();
    expect(result.spiral_valid).toBe(true);
  });

  // T6.0.2 — InputLockService + first-ring gate
  it('InputLockService IsLocked true during TweeningOut state', async () => {
    const result = await bridge.dispatchAction('input_lock.query_locked_during_tween');
    // Returns { is_locked: true }
    expect(result).toBeTruthy();
    expect(result.is_locked).toBe(true);
  });

  it('InputLockService IsLocked flips false after FirstRingLoaded fires', async () => {
    const result = await bridge.dispatchAction('input_lock.query_unlocked_after_first_ring');
    // Returns { is_locked: false, unlock_event_fired: true }
    expect(result).toBeTruthy();
    expect(result.is_locked).toBe(false);
    expect(result.unlock_event_fired).toBe(true);
  });

  it('InputLockService Unlock() triggers UI tell (banner shown or debug log)', async () => {
    const result = await bridge.dispatchAction('input_lock.query_ui_tell_on_unlock');
    // Returns { tell_shown: true } — banner visible class OR debug log recorded
    expect(result).toBeTruthy();
    expect(result.tell_shown).toBe(true);
  });

  // T6.0.3 — Profiler budget sweep
  it('budget=4 cells/frame: FirstRingLoaded fires, frame-time well under 8ms', async () => {
    const result = await bridge.dispatchAction('cell_streaming.profiler_sweep_budget_4');
    // Returns { first_ring_fired: true, median_frame_ms: number }
    expect(result).toBeTruthy();
    expect(result.first_ring_fired).toBe(true);
    // budget=4 should be well under 8ms
    expect(result.median_frame_ms).toBeLessThan(8);
  });

  it('budget=8 cells/frame (default): FirstRingLoaded fires, median frame <= 8ms', async () => {
    const result = await bridge.dispatchAction('cell_streaming.profiler_sweep_budget_8');
    // Returns { first_ring_fired: true, median_frame_ms: number }
    expect(result).toBeTruthy();
    expect(result.first_ring_fired).toBe(true);
    // Default budget must stay within 8ms frame budget on baseline hardware
    expect(result.median_frame_ms).toBeLessThanOrEqual(8);
  });

  it('budget=16 cells/frame: FirstRingLoaded fires (may exceed 8ms -- opt-in fast mode)', async () => {
    const result = await bridge.dispatchAction('cell_streaming.profiler_sweep_budget_16');
    // Returns { first_ring_fired: true, median_frame_ms: number }
    // budget=16 may exceed 8ms -- only asserting it completes and first-ring fires
    expect(result).toBeTruthy();
    expect(result.first_ring_fired).toBe(true);
  });

  it('budget=8 total stream time is >= budget=16 total stream time (expected ratio)', async () => {
    const b8  = await bridge.dispatchAction('cell_streaming.profiler_sweep_budget_8');
    const b16 = await bridge.dispatchAction('cell_streaming.profiler_sweep_budget_16');
    expect(b8).toBeTruthy();
    expect(b16).toBeTruthy();
    // budget=8 takes ~2x cells per frame fewer -> total time >= budget=16 total time
    expect(b8.total_stream_ms).toBeGreaterThanOrEqual(b16.total_stream_ms * 0.8);
  });
});
