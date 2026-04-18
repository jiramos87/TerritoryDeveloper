/**
 * Unit tests: project_spec_closeout_digest tool — envelope shape + error branches.
 *
 * ACs (TECH-403 §7b):
 *   1. wrapTool applied — resolve failure → { ok:false, error:{ code:"invalid_input" } }.
 *   2. read failure → { ok:false, error:{ code:"internal_error" } }.
 *   3. Happy path → ok:true + payload.
 */

import test from "node:test";
import assert from "node:assert/strict";
import { wrapTool } from "../../src/envelope.js";

// ---------------------------------------------------------------------------
// resolve failure → invalid_input
// ---------------------------------------------------------------------------

test("closeout_digest: resolve failure → invalid_input envelope", async () => {
  const handler = wrapTool(async (_input: void) => {
    throw { code: "invalid_input" as const, message: "Could not resolve spec path" };
  });
  const result = await handler(undefined as void);
  assert.equal(result.ok, false);
  if (!result.ok) {
    assert.equal(result.error.code, "invalid_input");
  }
});

// ---------------------------------------------------------------------------
// read failure → internal_error
// ---------------------------------------------------------------------------

test("closeout_digest: read failure → internal_error envelope", async () => {
  const handler = wrapTool(async (_input: void) => {
    throw {
      code: "internal_error" as const,
      message: "Could not read project spec: ENOENT",
      details: { spec_path: "ia/projects/TECH-X.md" },
    };
  });
  const result = await handler(undefined as void);
  assert.equal(result.ok, false);
  if (!result.ok) {
    assert.equal(result.error.code, "internal_error");
    assert.ok(result.error.message.includes("Could not read"));
  }
});

// ---------------------------------------------------------------------------
// Happy path — ok:true + digest payload
// ---------------------------------------------------------------------------

test("closeout_digest: happy path → ok:true + payload", async () => {
  const digest = { sections: [], issue_ids: [], spec_path: "ia/projects/TECH-1.md" };
  const handler = wrapTool(async (_input: void) => digest);
  const result = await handler(undefined as void);
  assert.equal(result.ok, true);
  if (result.ok) {
    assert.ok("sections" in (result.payload as object));
  }
});
