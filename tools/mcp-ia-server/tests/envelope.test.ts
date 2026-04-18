/**
 * Unit tests for wrapTool middleware + ToolEnvelope contract in envelope.ts.
 *
 * ACs:
 *   1. Happy path — bare O return → { ok: true, payload: O }.
 *   2. Envelope passthrough — already-shaped envelope returned verbatim.
 *   3. Bare Error throw → internal_error.
 *   4. Typed throw preserves code + optional hint/details.
 *   5. meta passthrough — EnvelopeMeta preserved untouched.
 */

import test from "node:test";
import assert from "node:assert/strict";
import { wrapTool, type ToolEnvelope } from "../src/envelope.js";

// ---------------------------------------------------------------------------
// AC1 — Happy path
// ---------------------------------------------------------------------------

test("happy path — bare O return yields ok:true + payload", async () => {
  const wrapped = wrapTool(async (_input: void) => "hello");
  const result = await wrapped(undefined as void);
  assert.equal(result.ok, true);
  if (result.ok) {
    assert.equal(result.payload, "hello");
    assert.equal(result.meta, undefined);
  }
});

// ---------------------------------------------------------------------------
// AC2 — Envelope passthrough
// ---------------------------------------------------------------------------

test("envelope passthrough — already-shaped envelope returned verbatim", async () => {
  const prebuilt: ToolEnvelope<string> = {
    ok: false,
    error: { code: "db_error", message: "x" },
  };
  const wrapped = wrapTool(async (_input: void) => prebuilt as unknown as string);
  const result = await wrapped(undefined as void);
  assert.equal(result.ok, false);
  if (!result.ok) {
    assert.equal(result.error.code, "db_error");
    assert.equal(result.error.message, "x");
  }
});

// ---------------------------------------------------------------------------
// AC3 — Bare Error → internal_error
// ---------------------------------------------------------------------------

test("bare Error maps to internal_error", async () => {
  const wrapped = wrapTool(async (_input: void): Promise<string> => {
    throw new Error("boom");
  });
  const result = await wrapped(undefined as void);
  assert.equal(result.ok, false);
  if (!result.ok) {
    assert.equal(result.error.code, "internal_error");
    assert.equal(result.error.message, "boom");
  }
});

// ---------------------------------------------------------------------------
// AC4 — Typed throw preserves code + optional fields
// ---------------------------------------------------------------------------

test("typed throw preserves code + optional hint/details", async () => {
  const wrapped = wrapTool(async (_input: void): Promise<number> => {
    throw { code: "db_unconfigured", message: "m", hint: "h", details: { k: 1 } };
  });
  const result = await wrapped(undefined as void);
  assert.equal(result.ok, false);
  if (!result.ok) {
    assert.equal(result.error.code, "db_unconfigured");
    assert.equal(result.error.message, "m");
    assert.equal(result.error.hint, "h");
    assert.deepEqual(result.error.details, { k: 1 });
  }
});

test("typed throw without hint/details — envelope omits both keys", async () => {
  const wrapped = wrapTool(async (_input: void): Promise<number> => {
    throw { code: "db_unconfigured", message: "no extras" };
  });
  const result = await wrapped(undefined as void);
  assert.equal(result.ok, false);
  if (!result.ok) {
    assert.equal(result.error.code, "db_unconfigured");
    assert.equal(result.error.message, "no extras");
    assert.equal("hint" in result.error, false);
    assert.equal("details" in result.error, false);
  }
});

// ---------------------------------------------------------------------------
// AC5 — meta passthrough
// ---------------------------------------------------------------------------

test("meta passthrough — EnvelopeMeta preserved untouched", async () => {
  const meta = { graph_stale: true, partial: { succeeded: 1, failed: 2 } };
  const wrapped = wrapTool(async (_input: void): Promise<ToolEnvelope<number>> => {
    return { ok: true, payload: 42, meta };
  });
  const result = await wrapped(undefined as void);
  assert.equal(result.ok, true);
  if (result.ok) {
    assert.equal(result.payload, 42);
    assert.deepEqual(result.meta, meta);
  }
});
