import { NextResponse } from "next/server";

/**
 * JSON error body for ` /api/catalog/*` routes.
 * @see `ia/projects/TECH-642.md`
 */
export type CatalogErrorCode =
  | "bad_request"
  | "not_found"
  | "conflict"
  | "internal"
  | "unique_violation"
  | "foreign_key_violation";

type CatalogErrorBody = {
  error: string;
  code: CatalogErrorCode;
  details?: unknown;
  /** Fresh row for optimistic-lock 409 (TECH-644). */
  current?: unknown;
};

/**
 * No stack traces in the response body; log server-side only.
 */
export function catalogJsonError(
  status: 400 | 404 | 409 | 500,
  code: CatalogErrorCode,
  message: string,
  init?: { details?: unknown; current?: unknown; logContext?: string },
) {
  const { details, current, logContext } = init ?? {};
  if (logContext) {
    console.error(`[catalog-api]`, status, code, message, logContext, details ?? "");
  } else {
    console.error(`[catalog-api]`, status, code, message);
  }
  const body: CatalogErrorBody = { error: message, code };
  if (details !== undefined) body.details = details;
  if (current !== undefined) body.current = current;
  return NextResponse.json(body, { status });
}

function isPgError(err: unknown): err is { code: string; message: string; detail?: string } {
  return (
    typeof err === "object" &&
    err !== null &&
    "code" in err &&
    typeof (err as { code: unknown }).code === "string"
  );
}

/**
 * Map Postgres error codes to HTTP; fall back to 500.
 */
export function responseFromPostgresError(err: unknown, fallback: string) {
  if (!isPgError(err)) {
    console.error(`[catalog-api] unexpected error`, err);
    return catalogJsonError(500, "internal", fallback, { logContext: "non-pg" });
  }
  const c = err.code;
  if (c === "23505") {
    return catalogJsonError(409, "unique_violation", "Unique constraint violation", {
      details: err.detail,
      logContext: err.message,
    });
  }
  if (c === "23503") {
    return catalogJsonError(400, "foreign_key_violation", "Foreign key constraint violation", {
      details: err.detail,
      logContext: err.message,
    });
  }
  if (c === "22P02") {
    return catalogJsonError(400, "bad_request", "Invalid data format", { logContext: err.message });
  }
  console.error(`[catalog-api] pg error`, c, err.message);
  return catalogJsonError(500, "internal", fallback, { logContext: err.message });
}
