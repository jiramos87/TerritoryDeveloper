import { describe, expect, it } from "vitest";

import { DEFAULT_LIMIT, MAX_LIMIT, MIN_SCORE, VALID_KINDS } from "@/lib/catalog/search-query";

describe("search-query constants", () => {
  it("VALID_KINDS contains all 8 catalog kinds", () => {
    const expected = [
      "sprite",
      "asset",
      "button",
      "panel",
      "pool",
      "token",
      "archetype",
      "audio",
    ];
    for (const k of expected) {
      expect(VALID_KINDS.has(k)).toBe(true);
    }
    expect(VALID_KINDS.size).toBe(8);
  });

  it("DEFAULT_LIMIT is positive and ≤ MAX_LIMIT", () => {
    expect(DEFAULT_LIMIT).toBeGreaterThan(0);
    expect(DEFAULT_LIMIT).toBeLessThanOrEqual(MAX_LIMIT);
  });

  it("MAX_LIMIT is 100", () => {
    expect(MAX_LIMIT).toBe(100);
  });

  it("MIN_SCORE is a positive fraction", () => {
    expect(MIN_SCORE).toBeGreaterThan(0);
    expect(MIN_SCORE).toBeLessThan(1);
  });
});
