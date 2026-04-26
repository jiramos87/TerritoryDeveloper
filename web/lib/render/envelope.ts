/**
 * envelope.ts — DEC-A48 response envelope helpers for `/api/render/*`.
 *
 *   success: `{ ok: true, data, audit_id? }`
 *   failure: `{ ok: false, error: { code, message, details? }, retry_hint? }`
 *
 * `withAudit` already wraps the success envelope (and stamps `audit_id`).
 * These helpers cover the failure paths the route emits directly (validation,
 * forbidden, not_found, queue_full, internal).
 */
import { NextResponse } from "next/server";

export type RenderErrorCode =
  | "validation"
  | "forbidden"
  | "not_found"
  | "conflict"
  | "queue_full"
  | "internal";

export type RenderErrorBody = {
  ok: false;
  error: { code: RenderErrorCode; message: string; details?: unknown };
  retry_hint?: { after_seconds: number };
};

export function renderError(
  status: 400 | 403 | 404 | 409 | 429 | 500,
  code: RenderErrorCode,
  message: string,
  init?: { details?: unknown; retry_hint?: { after_seconds: number } },
): NextResponse<RenderErrorBody> {
  const body: RenderErrorBody = { ok: false, error: { code, message } };
  if (init?.details !== undefined) body.error.details = init.details;
  if (init?.retry_hint !== undefined) body.retry_hint = init.retry_hint;
  return NextResponse.json(body, { status });
}
