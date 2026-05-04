/**
 * TECH-10897 — Vitest suite for red-stage-anchor-resolver.ts
 *
 * Covers 4 grammar forms: happy path + ≥1 error path each.
 */

import * as path from "node:path";
import { fileURLToPath } from "node:url";
import { describe, it, expect, beforeAll } from "vitest";
import {
  resolveAnchor,
  RedStageAnchorParseError,
} from "../red-stage-anchor-resolver.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "../../..");

// Use a real file we know exists in the repo as the test path anchor.
const EXISTING_PATH = "tools/lib/red-stage-anchor-resolver.ts";
const MISSING_PATH = "tools/lib/__nonexistent_file_for_test__.ts";

beforeAll(() => {
  // Point resolver at repo root.
  process.env.IA_REPO_ROOT = REPO_ROOT;
});

describe("tracer-verb-test grammar", () => {
  it("tracerVerbHappy — returns correct discriminant", () => {
    const result = resolveAnchor(
      `tracer-verb-test:${EXISTING_PATH}::myTestMethod`,
    );
    expect(result).toEqual({
      kind: "tracer-verb-test",
      path: EXISTING_PATH,
      method: "myTestMethod",
    });
  });

  it("tracerVerbPathMissing — throws RedStageAnchorParseError", () => {
    expect(() =>
      resolveAnchor(`tracer-verb-test:${MISSING_PATH}::myMethod`),
    ).toThrow(RedStageAnchorParseError);
  });
});

describe("visibility-delta-test grammar", () => {
  it("visibilityDeltaHappy — returns correct discriminant", () => {
    const result = resolveAnchor(
      `visibility-delta-test:${EXISTING_PATH}::CIRedOnEmptyRedStageProofBlock`,
    );
    expect(result).toEqual({
      kind: "visibility-delta-test",
      path: EXISTING_PATH,
      method: "CIRedOnEmptyRedStageProofBlock",
    });
  });

  it("visibilityDeltaPathMissing — throws RedStageAnchorParseError", () => {
    expect(() =>
      resolveAnchor(`visibility-delta-test:${MISSING_PATH}::someMethod`),
    ).toThrow(RedStageAnchorParseError);
  });
});

describe("BUG-NNNN grammar", () => {
  it("bugReproHappy — returns correct discriminant with bug_id", () => {
    const result = resolveAnchor(`BUG-3210:${EXISTING_PATH}::reproTest`);
    expect(result).toEqual({
      kind: "bug-repro",
      bug_id: "BUG-3210",
      path: EXISTING_PATH,
      method: "reproTest",
    });
  });

  it("bugReproMalformedId — throws RedStageAnchorParseError for BUG-abc", () => {
    expect(() =>
      resolveAnchor(`BUG-abc:${EXISTING_PATH}::reproTest`),
    ).toThrow(RedStageAnchorParseError);
  });

  it("bugReproMalformedId — error message names expected grammar", () => {
    try {
      resolveAnchor(`BUG-abc:${EXISTING_PATH}::reproTest`);
    } catch (e) {
      expect(e).toBeInstanceOf(RedStageAnchorParseError);
      expect((e as Error).message).toMatch(/BUG-\\d\+|BUG-NNNN/);
    }
  });
});

describe("n/a literal", () => {
  it("naLiteral — returns {kind: 'na'} without path validation", () => {
    const result = resolveAnchor("n/a");
    expect(result).toEqual({ kind: "na" });
  });
});

describe("malformed inputs", () => {
  it("malformedMissingDoubleColon — throws with grammar-form message", () => {
    expect(() =>
      resolveAnchor("tracer-verb-test:tools/lib/some-file.ts"),
    ).toThrow(RedStageAnchorParseError);
  });

  it("malformedMissingDoubleColon — error names expected grammar", () => {
    try {
      resolveAnchor("tracer-verb-test:tools/lib/some-file.ts");
    } catch (e) {
      expect(e).toBeInstanceOf(RedStageAnchorParseError);
      expect((e as Error).message).toMatch(/grammar/i);
    }
  });

  it("unknownPrefix — throws with unrecognized prefix message", () => {
    expect(() => resolveAnchor("foobar:some/path.ts::method")).toThrow(
      RedStageAnchorParseError,
    );
  });

  it("noColon — throws on anchor with no colon at all", () => {
    expect(() => resolveAnchor("noColonAtAll")).toThrow(
      RedStageAnchorParseError,
    );
  });
});
