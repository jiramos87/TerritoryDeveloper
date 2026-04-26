"use client";

import { useState } from "react";

import type { ErrorCode, ErrorEnvelope } from "@/lib/error-envelope";

/**
 * Default user-facing message per ErrorCode (DEC-A48 + Stage 5.1 §Pending
 * Decisions). Page-copy English exception: user-facing strings stay full
 * English even though the broader IA prose is caveman.
 */
export const DEFAULT_ERROR_MESSAGES: Record<ErrorCode, string> = {
  validation:
    "One or more fields are invalid. Review the highlighted entries and try again.",
  stale: "Someone else updated this record. Reload to see their changes.",
  forbidden: "You do not have permission to perform this action.",
  not_found: "The record you requested no longer exists.",
  conflict:
    "This action conflicts with another record (slug taken, retired ref, or cycle).",
  lint_blocked:
    "Saving is blocked by failing lint gates. Resolve the listed gates first.",
  queue_full: "The render queue is at capacity. Retry shortly.",
  internal: "Something went wrong on our side. Please try again or contact support.",
};

/** Severity tier per code, mapped to a `ds-*` palette token. */
export const ERROR_SEVERITY: Record<ErrorCode, "critical" | "warn" | "info"> = {
  validation: "critical",
  forbidden: "critical",
  not_found: "critical",
  conflict: "critical",
  internal: "critical",
  stale: "warn",
  lint_blocked: "warn",
  queue_full: "info",
};

const SEVERITY_BORDER_VAR: Record<"critical" | "warn" | "info", string> = {
  critical: "var(--ds-text-accent-critical)",
  warn: "var(--ds-text-accent-warn)",
  info: "var(--ds-text-accent-info)",
};

export type ErrorBannerProps = {
  envelope: ErrorEnvelope | null;
  /** Per-code copy override; default copy applies for codes not present. */
  messageOverride?: Partial<Record<ErrorCode, string>>;
  /** Force the retry CTA on even when the envelope omits `retry_hint`. */
  showRetry?: boolean;
  onRetry?: () => void;
  onDismiss?: () => void;
};

/**
 * Kind-agnostic banner consumed by every catalog page (TECH-1614). Renders
 * `null` body when `envelope === null` so pages can render unconditionally.
 *
 * @see TECH-1617 — Error envelope renderer
 * @see DEC-A48
 */
export default function ErrorBanner({
  envelope,
  messageOverride,
  showRetry = false,
  onRetry,
  onDismiss,
}: ErrorBannerProps) {
  const [dismissed, setDismissed] = useState(false);

  if (envelope === null || dismissed) return null;

  const { error, retry_hint } = envelope;
  const code = error.code;
  const severity = ERROR_SEVERITY[code];
  const message = messageOverride?.[code] ?? error.message ?? DEFAULT_ERROR_MESSAGES[code];
  const shouldShowRetry = showRetry || retry_hint !== undefined;

  return (
    <div
      role="alert"
      data-error-code={code}
      data-severity={severity}
      style={{
        borderLeft: `4px solid ${SEVERITY_BORDER_VAR[severity]}`,
        background: "var(--ds-bg-panel)",
        color: "var(--ds-text-primary)",
        padding: "var(--ds-spacing-md)",
        margin: "var(--ds-spacing-sm) 0",
        display: "flex",
        flexDirection: "column",
        gap: "var(--ds-spacing-xs)",
      }}
    >
      <div style={{ display: "flex", justifyContent: "space-between", gap: "var(--ds-spacing-md)" }}>
        <p style={{ margin: 0, fontSize: "var(--ds-font-size-body-sm)" }}>{message}</p>
        <button
          type="button"
          aria-label="Dismiss"
          onClick={() => {
            setDismissed(true);
            onDismiss?.();
          }}
          style={{
            background: "transparent",
            border: 0,
            color: "var(--ds-text-muted)",
            cursor: "pointer",
            padding: "0 var(--ds-spacing-xs)",
          }}
        >
          ×
        </button>
      </div>

      {error.details ? <ErrorDetailsBlock code={code} details={error.details} /> : null}

      {shouldShowRetry ? (
        <button
          type="button"
          onClick={() => onRetry?.()}
          style={{
            alignSelf: "flex-start",
            background: "var(--ds-bg-canvas)",
            border: `1px solid ${SEVERITY_BORDER_VAR[severity]}`,
            color: "var(--ds-text-primary)",
            padding: "var(--ds-spacing-xs) var(--ds-spacing-md)",
            cursor: "pointer",
          }}
        >
          {retry_hint?.after_seconds !== undefined
            ? `Retry in ${retry_hint.after_seconds}s`
            : "Retry"}
        </button>
      ) : null}
    </div>
  );
}

function ErrorDetailsBlock({
  code,
  details,
}: {
  code: ErrorCode;
  details: Record<string, unknown>;
}) {
  if (code === "validation" && Array.isArray((details as { fields?: unknown }).fields)) {
    const fields = (details as { fields: ReadonlyArray<{ field: string; message: string }> }).fields;
    return (
      <ul style={{ margin: 0, paddingLeft: "var(--ds-spacing-md)", color: "var(--ds-text-muted)" }}>
        {fields.map((f) => (
          <li key={f.field}>
            <strong>{f.field}</strong>: {f.message}
          </li>
        ))}
      </ul>
    );
  }
  if (
    code === "lint_blocked" &&
    Array.isArray((details as { failed_gate_ids?: unknown }).failed_gate_ids)
  ) {
    const ids = (details as { failed_gate_ids: ReadonlyArray<string> }).failed_gate_ids;
    return (
      <div style={{ display: "flex", flexWrap: "wrap", gap: "var(--ds-spacing-xs)" }}>
        {ids.map((id) => (
          <span
            key={id}
            style={{
              background: "var(--ds-bg-canvas)",
              border: "1px solid var(--ds-border-subtle)",
              padding: "var(--ds-spacing-2xs) var(--ds-spacing-xs)",
              fontSize: "var(--ds-font-size-caption)",
            }}
          >
            {id}
          </span>
        ))}
      </div>
    );
  }
  return null;
}
