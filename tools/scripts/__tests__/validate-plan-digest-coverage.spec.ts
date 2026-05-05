/**
 * validate-plan-digest-coverage.spec.ts — TECH-14103
 *
 * Unit tests for the plan-digest-coverage band classification logic.
 * Verifies that seeded-marker bodies are classified as `seeded`, not `missing`.
 */

import { describe, it, expect } from "vitest";

const SEEDED_MARKER = "<!-- seeded: backfill_v1 -->";

type Band = "covered" | "seeded" | "missing";

function classifyTaskBody(body: string | null, backfilled: boolean): Band {
  const trimmed = (body ?? "").trim();
  if (!trimmed) return "missing";
  if (trimmed.startsWith(SEEDED_MARKER) || backfilled) return "seeded";
  return "covered";
}

describe("classifyTaskBody — coverage band logic", () => {
  it("null body → missing", () => {
    expect(classifyTaskBody(null, false)).toBe("missing");
  });

  it("empty string → missing", () => {
    expect(classifyTaskBody("", false)).toBe("missing");
  });

  it("whitespace-only → missing", () => {
    expect(classifyTaskBody("   \n  ", false)).toBe("missing");
  });

  it("non-empty human body → covered", () => {
    expect(classifyTaskBody("## §Plan Digest\n### §Goal\nFoo", false)).toBe("covered");
  });

  it("body starting with seeded marker → seeded (not missing)", () => {
    const body = `${SEEDED_MARKER}\n## §Plan Digest`;
    expect(classifyTaskBody(body, false)).toBe("seeded");
  });

  it("backfilled=true even without marker → seeded", () => {
    expect(classifyTaskBody("## §Plan Digest\n### §Goal\nFoo", true)).toBe("seeded");
  });

  it("seeded marker body NOT classified as missing", () => {
    const body = `${SEEDED_MARKER}\n## §Plan Digest`;
    expect(classifyTaskBody(body, false)).not.toBe("missing");
  });
});

describe("seeded band carve-out integrity", () => {
  it("seeded tasks are in a SEPARATE band from missing — they do not fail coverage", () => {
    const tasks = [
      { body: `${SEEDED_MARKER}\n## §Plan Digest`, backfilled: true },
      { body: "## §Plan Digest\n### §Goal\nReal content", backfilled: false },
    ];
    const missingCount = tasks.filter(
      (t) => classifyTaskBody(t.body, t.backfilled) === "missing",
    ).length;
    const seededCount = tasks.filter(
      (t) => classifyTaskBody(t.body, t.backfilled) === "seeded",
    ).length;
    expect(missingCount).toBe(0);
    expect(seededCount).toBe(1);
  });
});
