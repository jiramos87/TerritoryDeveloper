/**
 * journal_append payload_kind='version_close' validator — TECH-12645.
 *
 * Boundary check inside `journal_append` tool registration. Required shape:
 *   {plan_slug:string, version:int>=1, tag:string, sha:string,
 *    validate_all_result:{ok:boolean, scripts:string[]},
 *    sections_closed:string[]}
 *
 * Mismatch → throws `{code: 'payload_validation_failed', message}`.
 * Other payload_kind values pass-through (no schema enforcement).
 */

import test from "node:test";
import assert from "node:assert/strict";
import { validateVersionClosePayload } from "../../src/tools/ia-db-writes.js";

const VALID_PAYLOAD = {
  plan_slug: "ship-protocol",
  version: 1,
  tag: "ship-protocol-v1",
  sha: "abc1234",
  validate_all_result: { ok: true, scripts: ["validate:all"] },
  sections_closed: ["arch", "ship-final"],
};

test("H1: full valid payload → no throw", () => {
  assert.doesNotThrow(() => validateVersionClosePayload({ ...VALID_PAYLOAD }));
});

test("E1: missing plan_slug → payload_validation_failed", () => {
  assert.throws(
    () => validateVersionClosePayload({ ...VALID_PAYLOAD, plan_slug: "" }),
    (e: unknown) => {
      const err = e as { code?: string; message?: string };
      return err.code === "payload_validation_failed" && /plan_slug/.test(err.message ?? "");
    },
  );
});

test("E2: version=0 → payload_validation_failed", () => {
  assert.throws(
    () => validateVersionClosePayload({ ...VALID_PAYLOAD, version: 0 }),
    (e: unknown) => (e as { code?: string }).code === "payload_validation_failed",
  );
});

test("E3: missing tag → payload_validation_failed", () => {
  const { tag, ...rest } = VALID_PAYLOAD;
  void tag;
  assert.throws(
    () => validateVersionClosePayload(rest as Record<string, unknown>),
    (e: unknown) =>
      (e as { code?: string; message?: string }).code === "payload_validation_failed" &&
      /tag/.test((e as { message?: string }).message ?? ""),
  );
});

test("E4: validate_all_result.ok non-boolean → payload_validation_failed", () => {
  assert.throws(
    () =>
      validateVersionClosePayload({
        ...VALID_PAYLOAD,
        validate_all_result: { ok: "yes", scripts: [] },
      }),
    (e: unknown) =>
      (e as { code?: string; message?: string }).code === "payload_validation_failed" &&
      /validate_all_result\.ok/.test((e as { message?: string }).message ?? ""),
  );
});

test("E5: validate_all_result.scripts non-string array → payload_validation_failed", () => {
  assert.throws(
    () =>
      validateVersionClosePayload({
        ...VALID_PAYLOAD,
        validate_all_result: { ok: true, scripts: [1, 2] },
      }),
    (e: unknown) =>
      (e as { code?: string; message?: string }).code === "payload_validation_failed" &&
      /scripts/.test((e as { message?: string }).message ?? ""),
  );
});

test("E6: sections_closed not array → payload_validation_failed", () => {
  assert.throws(
    () =>
      validateVersionClosePayload({
        ...VALID_PAYLOAD,
        sections_closed: "arch,ship-final" as unknown as string[],
      }),
    (e: unknown) =>
      (e as { code?: string; message?: string }).code === "payload_validation_failed" &&
      /sections_closed/.test((e as { message?: string }).message ?? ""),
  );
});

test("H2: empty sections_closed array OK (validator only checks element type)", () => {
  assert.doesNotThrow(() =>
    validateVersionClosePayload({ ...VALID_PAYLOAD, sections_closed: [] }),
  );
});

test("H3: empty validate_all_result.scripts array OK", () => {
  assert.doesNotThrow(() =>
    validateVersionClosePayload({
      ...VALID_PAYLOAD,
      validate_all_result: { ok: false, scripts: [] },
    }),
  );
});
