/**
 * Envelope middleware + ErrorCode enum for Territory IA MCP server.
 *
 * ToolEnvelope<T>  — discriminated union wrapping tool handler output.
 * EnvelopeMeta     — optional metadata attached to success envelopes.
 * ErrorCode        — canonical string-union of all known error codes.
 * wrapTool()       — middleware that catches handler throws + normalises output.
 */

// ---------------------------------------------------------------------------
// EnvelopeMeta
// ---------------------------------------------------------------------------

export interface EnvelopeMeta {
  graph_generated_at?: string;
  graph_stale?: boolean;
  partial?: { succeeded: number; failed: number };
  // spec_section success fields — added in TECH-398 Phase 2 (Decision Log 2026-04-18).
  section_id?: string;
  line_range?: [number, number];
  truncated?: boolean;
  total_chars?: number;
}

// ---------------------------------------------------------------------------
// ErrorCode
// ---------------------------------------------------------------------------

/**
 * Canonical error codes for ToolEnvelope error responses.
 * Source: docs/mcp-lifecycle-tools-opus-4-7-audit-exploration.md §3.1
 * + `internal_error` added for bare-Error server-fault fallback (Decision Log).
 */
export type ErrorCode =
  | "spec_not_found"
  | "section_not_found"
  | "issue_not_found"
  | "term_not_found"
  | "db_unconfigured"
  | "db_error"
  | "timeout"
  | "bridge_timeout"
  | "compilation_failed"
  | "invalid_input"
  | "unauthorized_caller"
  | "rate_limited"
  | "internal_error"
  | "missing_locator_fields"
  | "plan_not_found"
  | "task_key_drift";

// ---------------------------------------------------------------------------
// ToolEnvelope<T>
// ---------------------------------------------------------------------------

export type ToolEnvelope<T> =
  | { ok: true; payload: T; meta?: EnvelopeMeta }
  | {
      ok: false;
      error: { code: ErrorCode; message: string; hint?: string; details?: unknown };
    };

// ---------------------------------------------------------------------------
// dbUnconfiguredError
// ---------------------------------------------------------------------------

/**
 * Typed throw-object for DB-not-configured branches.
 * Throw this inside a `wrapTool` handler; the middleware normalises it to
 * `{ ok: false, error: { code: "db_unconfigured", message, hint } }`.
 */
export function dbUnconfiguredError(): { code: "db_unconfigured"; message: string; hint: string } {
  return {
    code: "db_unconfigured" as const,
    message: "No database URL: set DATABASE_URL or config/postgres-dev.json.",
    hint: "Start Postgres on :5434",
  };
}

// ---------------------------------------------------------------------------
// wrapTool
// ---------------------------------------------------------------------------

/**
 * Wraps a tool handler so it always returns `ToolEnvelope<O>`.
 *
 * - Handler may return bare `O` (simple happy-path) or already-shaped
 *   `ToolEnvelope<O>` (for handlers that build partial-result envelopes).
 * - Thrown `{ code, message, hint?, details? }` objects are preserved.
 * - Bare `Error` throws and unknown values map to `internal_error`.
 */
export function wrapTool<I, O>(
  handler: (input: I) => Promise<O | ToolEnvelope<O>>,
): (input: I) => Promise<ToolEnvelope<O>> {
  return async (input: I): Promise<ToolEnvelope<O>> => {
    try {
      const result = await handler(input);
      // Pass through if handler already returned an envelope shape.
      if (result !== null && typeof result === "object" && "ok" in result) {
        return result as ToolEnvelope<O>;
      }
      return { ok: true, payload: result as O };
    } catch (err) {
      // Thrown { code, message, hint?, details? } — preserve code.
      if (
        err !== null &&
        typeof err === "object" &&
        "code" in (err as object) &&
        "message" in (err as object)
      ) {
        const e = err as { code: ErrorCode; message: string; hint?: string; details?: unknown };
        return {
          ok: false,
          error: {
            code: e.code,
            message: e.message,
            ...(e.hint !== undefined ? { hint: e.hint } : {}),
            ...(e.details !== undefined ? { details: e.details } : {}),
          },
        };
      }
      // Bare Error or unknown value — server-fault fallback.
      return {
        ok: false,
        error: {
          code: "internal_error",
          message: err instanceof Error ? err.message : String(err),
        },
      };
    }
  };
}
