/**
 * Catalog error envelope (DEC-A48).
 *
 * Every `/api/catalog/*` mutate route emits this discriminated union on failure
 * and every catalog page renders `<ErrorBanner>` consuming it. The shape is
 * authoring-console wide so that 409 stale-fingerprint, 422 lint_blocked, and
 * queue_full retry hints flow through one type-safe pipeline.
 *
 * @see TECH-1617 — Error envelope renderer
 * @see TECH-1616 — Optimistic concurrency middleware (consumes `stale` shape)
 * @see docs/asset-pipeline-architecture.md §DEC-A48
 */

export type ErrorCode =
  | "validation"
  | "stale"
  | "forbidden"
  | "not_found"
  | "conflict"
  | "lint_blocked"
  | "queue_full"
  | "internal";

export type RetryHint = {
  after_seconds?: number;
  reason?: string;
};

/** Per-code `details` typing — each variant carries its own evidence shape. */
export type ErrorDetails =
  | { fields: ReadonlyArray<{ field: string; message: string }> } // validation
  | { current_payload: unknown; current_updated_at: string }       // stale
  | { failed_gate_ids: ReadonlyArray<string> }                     // lint_blocked
  | { conflicting_id?: string; reason?: string }                   // conflict
  | Record<string, unknown>;                                       // forbidden / not_found / internal

export type ErrorBody = {
  code: ErrorCode;
  message: string;
  details?: ErrorDetails;
};

export type ErrorEnvelope = {
  ok: false;
  error: ErrorBody;
  retry_hint?: RetryHint;
};

/* ------------------------------------------------------------------ */
/* Builder                                                             */
/* ------------------------------------------------------------------ */

type ValidationOptions = { fields: ReadonlyArray<{ field: string; message: string }> };
type StaleOptions = { current_payload: unknown; current_updated_at: string };
type LintBlockedOptions = { failed_gate_ids: ReadonlyArray<string> };
type ConflictOptions = { conflicting_id?: string; reason?: string };
type QueueFullOptions = { retry_after_seconds?: number; retry_reason?: string };
type EmptyOptions = Record<string, never>;

export type MakeErrorOptions = {
  validation: ValidationOptions;
  stale: StaleOptions;
  forbidden: EmptyOptions;
  not_found: EmptyOptions;
  conflict: ConflictOptions;
  lint_blocked: LintBlockedOptions;
  queue_full: QueueFullOptions;
  internal: EmptyOptions;
};

/**
 * Type-safe envelope builder. Each `code` constrains the shape of `options`
 * through the {@link MakeErrorOptions} mapping; missing required option fields
 * surface at compile time (e.g. `makeError("validation", "...")` without
 * `fields` is a type error).
 */
export function makeError<TCode extends ErrorCode>(
  code: TCode,
  message: string,
  options?: MakeErrorOptions[TCode],
): ErrorEnvelope {
  const envelope: ErrorEnvelope = { ok: false, error: { code, message } };
  if (!options) return envelope;

  if (code === "queue_full") {
    const opts = options as QueueFullOptions;
    if (opts.retry_after_seconds !== undefined || opts.retry_reason !== undefined) {
      envelope.retry_hint = {
        ...(opts.retry_after_seconds !== undefined ? { after_seconds: opts.retry_after_seconds } : {}),
        ...(opts.retry_reason !== undefined ? { reason: opts.retry_reason } : {}),
      };
    }
    return envelope;
  }

  // For the remaining codes the `options` payload is the `details` payload.
  envelope.error.details = options as ErrorDetails;
  return envelope;
}

/* ------------------------------------------------------------------ */
/* Type guard                                                          */
/* ------------------------------------------------------------------ */

/**
 * Cheap discriminated-union narrowing for client fetch handlers.
 *
 * Returns `true` only when the value is a non-null object with `ok === false`
 * and a string `error.code`. Use to gate `<ErrorBanner>` rendering when a fetch
 * could resolve to either an `ok: true` data envelope or an `ok: false` error.
 */
export function isErrorEnvelope(value: unknown): value is ErrorEnvelope {
  if (typeof value !== "object" || value === null) return false;
  const candidate = value as { ok?: unknown; error?: { code?: unknown } };
  if (candidate.ok !== false) return false;
  if (typeof candidate.error !== "object" || candidate.error === null) return false;
  return typeof candidate.error.code === "string";
}
