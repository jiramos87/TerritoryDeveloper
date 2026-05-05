/**
 * validate-seeded-task-stale.spec.ts — TECH-14103
 *
 * Unit tests for seeded-task-stale 30-day threshold logic.
 */

import { describe, it, expect } from "vitest";

const STALE_DAYS = 30;

function isStale(created_at: string, today: Date = new Date()): boolean {
  const created = new Date(created_at);
  const diffMs = today.getTime() - created.getTime();
  const diffDays = diffMs / (1000 * 60 * 60 * 24);
  return diffDays > STALE_DAYS;
}

describe("isStale — 30-day threshold", () => {
  it("task created today → not stale", () => {
    const today = new Date("2026-05-05T12:00:00Z");
    expect(isStale("2026-05-05T12:00:00Z", today)).toBe(false);
  });

  it("task created 29 days ago → not stale", () => {
    const today = new Date("2026-05-05T12:00:00Z");
    const created = new Date(today.getTime() - 29 * 24 * 60 * 60 * 1000);
    expect(isStale(created.toISOString(), today)).toBe(false);
  });

  it("task created exactly 30 days ago → not stale (threshold is >30)", () => {
    const today = new Date("2026-05-05T12:00:00Z");
    const created = new Date(today.getTime() - 30 * 24 * 60 * 60 * 1000);
    expect(isStale(created.toISOString(), today)).toBe(false);
  });

  it("task created 31 days ago → stale", () => {
    const today = new Date("2026-05-05T12:00:00Z");
    const created = new Date(today.getTime() - 31 * 24 * 60 * 60 * 1000);
    expect(isStale(created.toISOString(), today)).toBe(true);
  });

  it("task created today → not stale (regression: seeding day should never warn)", () => {
    const today = new Date();
    const iso = today.toISOString();
    expect(isStale(iso, today)).toBe(false);
  });
});
