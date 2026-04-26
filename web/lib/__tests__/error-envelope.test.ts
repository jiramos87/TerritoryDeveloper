import { describe, it, expect } from "vitest";

import {
  isErrorEnvelope,
  makeError,
  type ErrorCode,
  type ErrorEnvelope,
} from "@/lib/error-envelope";

describe("makeError", () => {
  it("validation — wraps fields under details", () => {
    const env = makeError("validation", "Some fields invalid", {
      fields: [{ field: "slug", message: "required" }],
    });
    expect(env).toEqual({
      ok: false,
      error: {
        code: "validation",
        message: "Some fields invalid",
        details: { fields: [{ field: "slug", message: "required" }] },
      },
    });
  });

  it("stale — wraps current_payload + current_updated_at under details", () => {
    const env = makeError("stale", "Server fresher", {
      current_payload: { foo: 1 },
      current_updated_at: "2026-04-26T00:00:00.000Z",
    });
    expect(env.ok).toBe(false);
    expect(env.error.code).toBe("stale");
    expect(env.error.details).toEqual({
      current_payload: { foo: 1 },
      current_updated_at: "2026-04-26T00:00:00.000Z",
    });
  });

  it("queue_full — promotes retry_after_seconds to top-level retry_hint", () => {
    const env = makeError("queue_full", "Queue at capacity", {
      retry_after_seconds: 30,
      retry_reason: "rate_limit",
    });
    expect(env.retry_hint).toEqual({ after_seconds: 30, reason: "rate_limit" });
    expect(env.error.code).toBe("queue_full");
    // queue_full does NOT carry options as `details`.
    expect(env.error.details).toBeUndefined();
  });

  it("queue_full — omits retry_hint when no options provided", () => {
    const env = makeError("queue_full", "Queue at capacity");
    expect(env.retry_hint).toBeUndefined();
    expect(env.error.details).toBeUndefined();
  });

  it("lint_blocked — wraps failed_gate_ids under details", () => {
    const env = makeError("lint_blocked", "Gates failing", {
      failed_gate_ids: ["sprite_palette_match", "panel_padding_min"],
    });
    expect(env.error.details).toEqual({
      failed_gate_ids: ["sprite_palette_match", "panel_padding_min"],
    });
  });

  it("forbidden — bare envelope without details", () => {
    const env = makeError("forbidden", "No permission");
    expect(env).toEqual({
      ok: false,
      error: { code: "forbidden", message: "No permission" },
    });
  });

  it("not_found — bare envelope", () => {
    const env = makeError("not_found", "Missing record");
    expect(env.error.details).toBeUndefined();
  });

  it("internal — bare envelope", () => {
    const env = makeError("internal", "Server fault");
    expect(env.error.code).toBe("internal");
  });

  it("conflict — wraps conflicting_id + reason under details", () => {
    const env = makeError("conflict", "Slug taken", {
      conflicting_id: "asset:42",
      reason: "slug_collision",
    });
    expect(env.error.details).toEqual({
      conflicting_id: "asset:42",
      reason: "slug_collision",
    });
  });

  it("preserves the ErrorCode union exhaustively", () => {
    const codes: ErrorCode[] = [
      "validation",
      "stale",
      "forbidden",
      "not_found",
      "conflict",
      "lint_blocked",
      "queue_full",
      "internal",
    ];
    expect(codes.length).toBe(8);
  });
});

describe("isErrorEnvelope", () => {
  it("returns true for a valid envelope", () => {
    const env: ErrorEnvelope = { ok: false, error: { code: "internal", message: "oops" } };
    expect(isErrorEnvelope(env)).toBe(true);
  });

  it("returns false for an ok envelope", () => {
    expect(isErrorEnvelope({ ok: true, data: { id: 1 } })).toBe(false);
  });

  it("returns false for null and primitives", () => {
    expect(isErrorEnvelope(null)).toBe(false);
    expect(isErrorEnvelope(undefined)).toBe(false);
    expect(isErrorEnvelope("error")).toBe(false);
    expect(isErrorEnvelope(42)).toBe(false);
  });

  it("returns false when error.code is missing or non-string", () => {
    expect(isErrorEnvelope({ ok: false, error: {} })).toBe(false);
    expect(isErrorEnvelope({ ok: false, error: { code: 42 } })).toBe(false);
    expect(isErrorEnvelope({ ok: false })).toBe(false);
  });
});
