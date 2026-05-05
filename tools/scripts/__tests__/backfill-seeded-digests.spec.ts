/**
 * backfill-seeded-digests.spec.ts — TECH-14103
 *
 * Unit tests for the band classifier in backfill-seeded-digests.ts.
 * Tests 3-stage classifier (missing / partial / present_complete) plus
 * seeded-marker recognition.
 */

import { describe, it, expect } from "vitest";
import { classifyBand, type TaskRow } from "../backfill-seeded-digests.js";

const SEEDED_MARKER = "<!-- seeded: backfill_v1 -->";

function makeTask(partial: Partial<TaskRow>): TaskRow {
  return {
    task_id: "TECH-999",
    slug: "test-plan",
    stage_id: "1",
    body: null,
    backfilled: false,
    ...partial,
  };
}

describe("classifyBand — 3-stage classifier", () => {
  it("empty task list → missing", () => {
    expect(classifyBand([])).toBe("missing");
  });

  it("all tasks have empty body → missing", () => {
    const tasks = [
      makeTask({ task_id: "TECH-1", body: null }),
      makeTask({ task_id: "TECH-2", body: "" }),
      makeTask({ task_id: "TECH-3", body: "   " }),
    ];
    expect(classifyBand(tasks)).toBe("missing");
  });

  it("all tasks have non-empty body → present_complete", () => {
    const tasks = [
      makeTask({ task_id: "TECH-1", body: "## §Plan Digest\n### §Goal\nFoo" }),
      makeTask({ task_id: "TECH-2", body: "## §Plan Digest\n### §Goal\nBar" }),
    ];
    expect(classifyBand(tasks)).toBe("present_complete");
  });

  it("mixed empty + non-empty → partial", () => {
    const tasks = [
      makeTask({ task_id: "TECH-1", body: "## §Plan Digest\n### §Goal\nFoo" }),
      makeTask({ task_id: "TECH-2", body: null }),
    ];
    expect(classifyBand(tasks)).toBe("partial");
  });

  it("seeded body (starts with marker) → treated as non-empty → present_complete if all seeded", () => {
    const tasks = [
      makeTask({ task_id: "TECH-1", body: `${SEEDED_MARKER}\n## §Plan Digest` }),
    ];
    // seeded bodies are non-empty → present_complete (classifier doesn't know seeded vs human)
    // The seeded classification is the validator's job
    expect(classifyBand(tasks)).toBe("present_complete");
  });

  it("one seeded + one empty → partial", () => {
    const tasks = [
      makeTask({ task_id: "TECH-1", body: `${SEEDED_MARKER}\n## §Plan Digest` }),
      makeTask({ task_id: "TECH-2", body: "" }),
    ];
    expect(classifyBand(tasks)).toBe("partial");
  });
});

describe("seeded marker byte-exact contract", () => {
  it("marker string has no leading/trailing whitespace", () => {
    expect(SEEDED_MARKER).toBe(SEEDED_MARKER.trim());
  });

  it("marker starts with <!-- and ends with -->", () => {
    expect(SEEDED_MARKER.startsWith("<!--")).toBe(true);
    expect(SEEDED_MARKER.endsWith("-->")).toBe(true);
  });

  it("marker contains 'seeded: backfill_v1'", () => {
    expect(SEEDED_MARKER).toContain("seeded: backfill_v1");
  });
});
