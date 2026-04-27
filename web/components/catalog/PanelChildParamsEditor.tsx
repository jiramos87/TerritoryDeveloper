"use client";

import { useState } from "react";

/**
 * JSON textarea for one panel-child `params_json` blob (TECH-1886).
 *
 * Parse-on-blur validation per DEC-A27; bubbles `onChange(parsedObj | null)`
 * to parent. Inline error rendered in red when JSON unparseable. Free-text
 * editor — full schema-driven form deferred per Plan Digest.
 */

export type PanelChildParamsEditorProps = {
  testId: string;
  value: Record<string, unknown>;
  onChange: (next: Record<string, unknown>) => void;
};

export default function PanelChildParamsEditor({
  testId,
  value,
  onChange,
}: PanelChildParamsEditorProps) {
  const [text, setText] = useState<string>(() => JSON.stringify(value ?? {}, null, 2));
  const [error, setError] = useState<string | null>(null);

  function handleBlur() {
    try {
      const parsed = JSON.parse(text) as unknown;
      if (typeof parsed !== "object" || parsed === null || Array.isArray(parsed)) {
        setError("params_json must be a JSON object");
        return;
      }
      setError(null);
      onChange(parsed as Record<string, unknown>);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Invalid JSON");
    }
  }

  return (
    <div className="flex flex-col gap-[var(--ds-spacing-xs)]">
      <textarea
        data-testid={testId}
        value={text}
        onChange={(e) => setText(e.currentTarget.value)}
        onBlur={handleBlur}
        rows={4}
        className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] px-[var(--ds-spacing-sm)] py-[var(--ds-spacing-xs)] font-mono text-xs"
      />
      {error ? (
        <span
          data-testid={`${testId}-error`}
          role="alert"
          className="text-[var(--ds-text-accent-critical)]"
        >
          {error}
        </span>
      ) : null}
    </div>
  );
}
