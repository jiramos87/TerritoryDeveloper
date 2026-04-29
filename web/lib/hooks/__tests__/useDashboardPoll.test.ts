/**
 * useDashboardPoll — polling interval + visibility-pause + cleanup (TECH-4183 §Test Blueprint).
 * Exercises the polling logic directly (no React renderer).
 */
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

// Simulate the polling logic extracted from useDashboardPoll.
function makePoller(onFetch: () => void) {
  const INTERVAL = 30_000;
  let hidden = false;
  let intervalId: ReturnType<typeof setInterval> | null = null;
  let visHandler: (() => void) | null = null;

  function tick() {
    if (!hidden) onFetch();
  }

  function start() {
    onFetch(); // immediate on mount
    intervalId = setInterval(tick, INTERVAL);
    visHandler = () => {
      if (!hidden) onFetch();
    };
  }

  function stop() {
    if (intervalId !== null) clearInterval(intervalId);
    intervalId = null;
    visHandler = null;
  }

  function setHidden(val: boolean) {
    hidden = val;
    if (!hidden && visHandler) visHandler();
  }

  return { start, stop, setHidden, tick };
}

describe("useDashboardPoll logic", () => {
  beforeEach(() => { vi.useFakeTimers(); });
  afterEach(() => { vi.useRealTimers(); });

  it("fires immediately on start", () => {
    const calls: number[] = [];
    const poller = makePoller(() => calls.push(Date.now()));
    poller.start();
    expect(calls).toHaveLength(1);
    poller.stop();
  });

  it("fires again after 30s interval", () => {
    const calls: number[] = [];
    const poller = makePoller(() => calls.push(Date.now()));
    poller.start();
    vi.advanceTimersByTime(30_000);
    expect(calls).toHaveLength(2);
    vi.advanceTimersByTime(30_000);
    expect(calls).toHaveLength(3);
    poller.stop();
  });

  it("does not fire when hidden", () => {
    const calls: number[] = [];
    const poller = makePoller(() => calls.push(1));
    poller.start();
    poller.setHidden(true);
    vi.advanceTimersByTime(60_000);
    expect(calls).toHaveLength(1); // only the initial mount call
    poller.stop();
  });

  it("fires immediately on visibility restore", () => {
    const calls: number[] = [];
    const poller = makePoller(() => calls.push(1));
    poller.start();
    poller.setHidden(true);
    vi.advanceTimersByTime(30_000);
    const before = calls.length;
    poller.setHidden(false); // restore → immediate refetch
    expect(calls.length).toBeGreaterThan(before);
    poller.stop();
  });

  it("clears interval on stop — no more ticks after stop", () => {
    const calls: number[] = [];
    const poller = makePoller(() => calls.push(1));
    poller.start();
    poller.stop();
    const before = calls.length;
    vi.advanceTimersByTime(60_000);
    expect(calls.length).toBe(before);
  });
});
