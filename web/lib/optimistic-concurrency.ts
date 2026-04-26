import { NextResponse, type NextRequest } from "next/server";

import { makeError, type ErrorEnvelope } from "@/lib/error-envelope";

/**
 * Optimistic-concurrency decorator (DEC-A38).
 *
 * Wraps a Next.js route handler so the request only proceeds when the caller's
 * `If-Match` fingerprint (or `expected_updated_at` body fallback) matches the
 * current row's `updated_at`. On mismatch the decorator returns a `409` envelope
 * with `current_payload` so the client modal can render the 3-way diff
 * (TECH-1616).
 *
 * Composition contract: caller mounts `withOptimisticConcurrency` *after*
 * auth/capability middleware so unauthorized requests never reach
 * `loadCurrent`.
 *
 * @see TECH-1616 — Optimistic concurrency middleware + 409 handler
 * @see DEC-A38 / DEC-A48
 */

export type RouteHandler = (req: NextRequest) => Promise<Response>;

export type AuditEmitter = (event: {
  action: "catalog.entity.save_conflict";
  target_kind?: string;
  target_id?: string;
  payload: Record<string, unknown>;
}) => Promise<void>;

export type LoadCurrent = (
  req: NextRequest,
) => Promise<{ updated_at: Date; payload: unknown } | null>;

export type WithOptimisticConcurrencyOptions = {
  loadCurrent: LoadCurrent;
  /** Optional audit emitter; receives `catalog.entity.save_conflict` on every 409. */
  auditEmitter?: AuditEmitter;
  /** Logical entity kind for audit payload (e.g. `"asset"`, `"sprite"`). */
  targetKind?: string;
  /** Resolves the row id for the audit payload from the request, when knowable. */
  resolveTargetId?: (req: NextRequest) => string | undefined;
};

/* ------------------------------------------------------------------ */
/* Fingerprint helpers (exported for unit + downstream consumers)      */
/* ------------------------------------------------------------------ */

/** Format a TIMESTAMPTZ as canonical ISO 8601 string (DEC-A38 etag shape). */
export function formatFingerprint(updated_at: Date): string {
  return updated_at.toISOString();
}

/** Parse an `If-Match` header / `expected_updated_at` body field. */
export function parseFingerprint(raw: string | null | undefined): Date | null {
  if (!raw) return null;
  const cleaned = raw.replace(/^"|"$/g, "").trim(); // strip optional ETag quoting
  const ms = Date.parse(cleaned);
  if (!Number.isFinite(ms)) return null;
  return new Date(ms);
}

/** Strict millisecond equality between two parsed fingerprints. */
function fingerprintsMatch(a: Date, b: Date): boolean {
  return a.getTime() === b.getTime();
}

/* ------------------------------------------------------------------ */
/* Body-fallback helper                                                */
/* ------------------------------------------------------------------ */

/**
 * Read `expected_updated_at` from the request body. Clones the request so the
 * downstream handler can re-read the body. Returns `null` when the body is not
 * JSON or the field is missing.
 */
async function readBodyFingerprint(req: NextRequest): Promise<string | null> {
  const contentType = req.headers.get("content-type") ?? "";
  if (!contentType.toLowerCase().includes("application/json")) return null;
  // Clone before reading: NextRequest.json() consumes the body.
  let cloned: NextRequest;
  try {
    cloned = req.clone() as NextRequest;
  } catch {
    return null;
  }
  try {
    const body = (await cloned.json()) as { expected_updated_at?: unknown } | null;
    if (body && typeof body === "object" && typeof body.expected_updated_at === "string") {
      return body.expected_updated_at;
    }
    return null;
  } catch {
    return null;
  }
}

/* ------------------------------------------------------------------ */
/* Envelope helpers                                                    */
/* ------------------------------------------------------------------ */

function envelopeResponse(envelope: ErrorEnvelope, status: number): Response {
  return NextResponse.json(envelope, { status });
}

/* ------------------------------------------------------------------ */
/* Decorator                                                           */
/* ------------------------------------------------------------------ */

export function withOptimisticConcurrency(
  handler: RouteHandler,
  options: WithOptimisticConcurrencyOptions,
): RouteHandler {
  return async (req: NextRequest) => {
    const headerRaw = req.headers.get("if-match");
    const bodyRaw = headerRaw ? null : await readBodyFingerprint(req);
    const provided = parseFingerprint(headerRaw ?? bodyRaw);

    if (!provided) {
      return envelopeResponse(
        makeError(
          "validation",
          "Missing If-Match header or expected_updated_at body field.",
          {
            fields: [
              {
                field: "If-Match",
                message: "Required for optimistic concurrency.",
              },
            ],
          },
        ),
        400,
      );
    }

    const current = await options.loadCurrent(req);
    if (current === null) {
      return envelopeResponse(
        makeError("not_found", "Record not found."),
        404,
      );
    }

    if (!fingerprintsMatch(provided, current.updated_at)) {
      const envelope = makeError("stale", "Record updated by another author.", {
        current_payload: current.payload,
        current_updated_at: formatFingerprint(current.updated_at),
      });

      if (options.auditEmitter) {
        try {
          await options.auditEmitter({
            action: "catalog.entity.save_conflict",
            target_kind: options.targetKind,
            target_id: options.resolveTargetId?.(req),
            payload: {
              provided_updated_at: formatFingerprint(provided),
              current_updated_at: formatFingerprint(current.updated_at),
            },
          });
        } catch (err) {
          // Audit failure must not mask the 409 to the client.
          console.error("[optimistic-concurrency] audit emitter failed", err);
        }
      }

      return envelopeResponse(envelope, 409);
    }

    return handler(req);
  };
}
