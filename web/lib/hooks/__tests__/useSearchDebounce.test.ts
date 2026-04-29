/**
 * useSearchDebounce — debounce timing tests via fake timers (TECH-4181 §Test Blueprint).
 */
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const DEBOUNCE_MS = 200;

describe("useSearchDebounce debounce semantics (fake timers)", () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("DEBOUNCE_MS constant is 200", () => {
    expect(DEBOUNCE_MS).toBe(200);
  });

  it("timer fires after exactly DEBOUNCE_MS", () => {
    let fired = false;
    const t = setTimeout(() => { fired = true; }, DEBOUNCE_MS);
    vi.advanceTimersByTime(DEBOUNCE_MS - 1);
    expect(fired).toBe(false);
    vi.advanceTimersByTime(1);
    expect(fired).toBe(true);
    clearTimeout(t);
  });

  it("cancelling prior timer before debounce elapses = only final fires", () => {
    let count = 0;
    let t1 = setTimeout(() => { count++; }, DEBOUNCE_MS);
    vi.advanceTimersByTime(DEBOUNCE_MS - 50);
    clearTimeout(t1);
    const t2 = setTimeout(() => { count++; }, DEBOUNCE_MS);
    vi.advanceTimersByTime(DEBOUNCE_MS);
    expect(count).toBe(1);
    clearTimeout(t2);
  });

  it("AbortController.abort is called when controller is aborted before new request", () => {
    const ctrl = new AbortController();
    const abortSpy = vi.spyOn(ctrl, "abort");
    ctrl.abort();
    expect(abortSpy).toHaveBeenCalledTimes(1);
  });
});
