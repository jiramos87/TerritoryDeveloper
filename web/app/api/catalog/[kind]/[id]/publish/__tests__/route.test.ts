/**
 * Generic catalog publish route — body-validator unit tests (TECH-2571 / Stage 12.1).
 *
 * Pure-helper coverage for `validatePublishBody`. Full DB-integration coverage
 * deferred — would require the `_harness` pattern from `tests/api/catalog/buttons/`.
 *
 * @see ia/projects/asset-pipeline/stage-12.1 — TECH-2571 §Test Blueprint
 */

import { describe, expect, it } from "vitest";

import { validatePublishBody } from "@/app/api/catalog/[kind]/[id]/publish/route";

describe("validatePublishBody", () => {
  it("accepts well-formed body without justification", () => {
    const r = validatePublishBody({ versionId: "100" });
    expect(r.ok).toBe(true);
    if (r.ok) {
      expect(r.body.versionId).toBe("100");
      expect(r.body.justification).toBeUndefined();
    }
  });

  it("accepts well-formed body with justification", () => {
    const r = validatePublishBody({
      versionId: "42",
      justification: "warn override ack",
    });
    expect(r.ok).toBe(true);
    if (r.ok) {
      expect(r.body.versionId).toBe("42");
      expect(r.body.justification).toBe("warn override ack");
    }
  });

  it("rejects non-object body (null)", () => {
    const r = validatePublishBody(null);
    expect(r.ok).toBe(false);
    if (!r.ok) expect(r.reason).toMatch(/object/);
  });

  it("rejects non-object body (string)", () => {
    const r = validatePublishBody("nope");
    expect(r.ok).toBe(false);
  });

  it("rejects missing versionId", () => {
    const r = validatePublishBody({});
    expect(r.ok).toBe(false);
    if (!r.ok) expect(r.reason).toMatch(/versionId/);
  });

  it("rejects non-numeric versionId", () => {
    const r = validatePublishBody({ versionId: "abc" });
    expect(r.ok).toBe(false);
    if (!r.ok) expect(r.reason).toMatch(/numeric/);
  });

  it("rejects non-string versionId", () => {
    const r = validatePublishBody({ versionId: 123 });
    expect(r.ok).toBe(false);
  });

  it("rejects non-string justification", () => {
    const r = validatePublishBody({ versionId: "1", justification: 42 });
    expect(r.ok).toBe(false);
    if (!r.ok) expect(r.reason).toMatch(/justification/);
  });

  it("rejects unknown fields", () => {
    const r = validatePublishBody({ versionId: "1", extra: "field" });
    expect(r.ok).toBe(false);
    if (!r.ok) expect(r.reason).toMatch(/unknown/);
  });
});
