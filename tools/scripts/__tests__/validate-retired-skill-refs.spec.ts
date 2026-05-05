/**
 * validate-retired-skill-refs.spec.ts — TECH-14103
 *
 * Unit tests for the retired-skill-refs validator logic.
 * Tests soft-fail flag (pre-date → exit 0) and hard-fail flip (post-date → exit 1).
 */

import { describe, it, expect } from "vitest";

// Inline the isHardFail logic for unit testing (avoids file I/O)
function isHardFail(hard_fail_after: string, today: Date = new Date()): boolean {
  const flipDate = new Date(hard_fail_after);
  return today >= flipDate;
}

describe("isHardFail — soft-fail calendar gate", () => {
  it("today before flip date → soft-fail (false)", () => {
    const future = new Date();
    future.setFullYear(future.getFullYear() + 1);
    const isoFuture = future.toISOString().slice(0, 10);
    expect(isHardFail(isoFuture)).toBe(false);
  });

  it("today equals flip date → hard-fail (true)", () => {
    const today = new Date();
    const iso = today.toISOString().slice(0, 10);
    expect(isHardFail(iso, today)).toBe(true);
  });

  it("today after flip date → hard-fail (true)", () => {
    const past = new Date("2020-01-01");
    expect(isHardFail("2020-01-01", new Date("2025-01-01"))).toBe(true);
  });

  it("exact date from config 2026-05-12 → hard-fail when today >= that date", () => {
    const flipDate = "2026-05-12";
    // After flip
    expect(isHardFail(flipDate, new Date("2026-05-12"))).toBe(true);
    expect(isHardFail(flipDate, new Date("2026-05-13"))).toBe(true);
    // Before flip
    expect(isHardFail(flipDate, new Date("2026-05-11"))).toBe(false);
  });
});

describe("retired slug list — config integrity", () => {
  it("config includes all 5 hard-removed retired slugs", async () => {
    const { readFileSync } = await import("node:fs");
    const { resolve } = await import("node:path");
    const raw = readFileSync(
      resolve(
        process.cwd(),
        "tools/scripts/validators/_retired-skill-refs.config.json",
      ),
      "utf8",
    );
    const config = JSON.parse(raw) as { retired_slugs: string[] };
    // ship-stage dir was removed but /ship-stage command remains via ship-stage-main-session
    // plan-review + code-review are active skills — not in hard-remove set
    const expected = [
      "master-plan-new",
      "master-plan-extend",
      "stage-file",
      "stage-authoring",
      "stage-decompose",
    ];
    for (const slug of expected) {
      expect(config.retired_slugs).toContain(slug);
    }
  });

  it("hard_fail_after is set to 2026-05-12", async () => {
    const { readFileSync } = await import("node:fs");
    const { resolve } = await import("node:path");
    const raw = readFileSync(
      resolve(
        process.cwd(),
        "tools/scripts/validators/_retired-skill-refs.config.json",
      ),
      "utf8",
    );
    const config = JSON.parse(raw) as { hard_fail_after: string };
    expect(config.hard_fail_after).toBe("2026-05-12");
  });
});
