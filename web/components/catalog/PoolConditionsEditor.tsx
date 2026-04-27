"use client";

import { useMemo, useState } from "react";

import { POOL_PREDICATE_VOCAB, POOL_PREDICATE_KEYS } from "@/lib/catalog/pool-predicate-vocab";

/**
 * Per-row pool member conditions editor (TECH-1788).
 *
 * Structured form: pick a predicate from canonical vocab → enter value →
 * row added to `conditions_json`. Raw JSON textarea fallback for power
 * users; round-trip preserves unknown keys outside the vocab.
 *
 * @see ia/projects/asset-pipeline/stage-7.1.md — TECH-1788 §Plan Digest
 */

export type PoolConditionsEditorProps = {
  value: Record<string, unknown>;
  onChange: (next: Record<string, unknown>) => void;
  testId?: string;
  disabled?: boolean;
};

function parseValueForKey(key: string, raw: string): unknown {
  const entry = POOL_PREDICATE_VOCAB.find((v) => v.key === key);
  if (!entry) return raw;
  if (entry.type === "int") {
    const n = Number.parseInt(raw, 10);
    return Number.isFinite(n) ? n : 0;
  }
  if (entry.type === "tag_list") {
    return raw
      .split(",")
      .map((t) => t.trim())
      .filter((t) => t !== "");
  }
  return raw;
}

function stringifyValue(v: unknown): string {
  if (Array.isArray(v)) return v.join(", ");
  return v == null ? "" : String(v);
}

export default function PoolConditionsEditor({ value, onChange, testId, disabled }: PoolConditionsEditorProps) {
  const baseTestId = testId ?? "pool-conditions-editor";
  const [pendingKey, setPendingKey] = useState<string>(POOL_PREDICATE_VOCAB[0]?.key ?? "");
  const [pendingValue, setPendingValue] = useState<string>("");
  const [rawMode, setRawMode] = useState<boolean>(false);
  const [rawText, setRawText] = useState<string>(() => JSON.stringify(value ?? {}, null, 2));
  const [rawError, setRawError] = useState<string | null>(null);

  const entries = useMemo(() => Object.entries(value ?? {}), [value]);

  function addRow() {
    if (pendingKey === "" || pendingValue === "") return;
    const next: Record<string, unknown> = { ...value, [pendingKey]: parseValueForKey(pendingKey, pendingValue) };
    onChange(next);
    setPendingValue("");
  }
  function removeRow(k: string) {
    const next: Record<string, unknown> = { ...value };
    delete next[k];
    onChange(next);
  }
  function applyRaw() {
    try {
      const parsed = JSON.parse(rawText);
      if (typeof parsed !== "object" || parsed == null || Array.isArray(parsed)) {
        setRawError("Conditions JSON must be an object");
        return;
      }
      setRawError(null);
      onChange(parsed as Record<string, unknown>);
    } catch (err) {
      setRawError(err instanceof Error ? err.message : "Invalid JSON");
    }
  }

  return (
    <div data-testid={baseTestId} className="flex flex-col gap-[var(--ds-spacing-xs)]">
      <ul data-testid={`${baseTestId}-rows`} className="flex flex-col gap-[var(--ds-spacing-xs)]">
        {entries.length === 0 ? (
          <li data-testid={`${baseTestId}-empty`} className="text-[var(--ds-text-muted)]">
            (no conditions)
          </li>
        ) : (
          entries.map(([k, v]) => {
            const inVocab = POOL_PREDICATE_KEYS.has(k);
            return (
              <li key={k} data-testid={`${baseTestId}-row-${k}`} className="flex items-center gap-[var(--ds-spacing-xs)]">
                <span className="font-mono text-[var(--ds-text-primary)]">{k}</span>
                <span className="text-[var(--ds-text-muted)]">{stringifyValue(v)}</span>
                {!inVocab ? (
                  <span data-testid={`${baseTestId}-row-${k}-unknown`} className="text-[var(--ds-text-accent-warn)]">
                    unknown vocab
                  </span>
                ) : null}
                {!disabled ? (
                  <button
                    type="button"
                    data-testid={`${baseTestId}-remove-${k}`}
                    onClick={() => removeRow(k)}
                    className="text-[var(--ds-text-muted)]"
                  >
                    Remove
                  </button>
                ) : null}
              </li>
            );
          })
        )}
      </ul>

      {!disabled ? (
        <div data-testid={`${baseTestId}-add`} className="flex items-center gap-[var(--ds-spacing-xs)]">
          <select
            data-testid={`${baseTestId}-vocab-select`}
            value={pendingKey}
            onChange={(e) => setPendingKey(e.currentTarget.value)}
          >
            {POOL_PREDICATE_VOCAB.map((entry) => (
              <option key={entry.key} value={entry.key}>
                {entry.label}
              </option>
            ))}
          </select>
          <input
            type="text"
            data-testid={`${baseTestId}-vocab-value`}
            value={pendingValue}
            onChange={(e) => setPendingValue(e.currentTarget.value)}
            placeholder="value"
          />
          <button type="button" data-testid={`${baseTestId}-vocab-add`} onClick={addRow}>
            Add predicate
          </button>
        </div>
      ) : null}

      {!disabled ? (
        <div data-testid={`${baseTestId}-raw-toggle-wrap`}>
          <button
            type="button"
            data-testid={`${baseTestId}-raw-toggle`}
            onClick={() => {
              setRawMode((m) => {
                const next = !m;
                if (next) setRawText(JSON.stringify(value ?? {}, null, 2));
                return next;
              });
            }}
            className="text-[var(--ds-text-accent-info)]"
          >
            {rawMode ? "Hide raw JSON" : "Edit raw JSON"}
          </button>
        </div>
      ) : null}

      {rawMode && !disabled ? (
        <div data-testid={`${baseTestId}-raw-wrap`} className="flex flex-col gap-[var(--ds-spacing-xs)]">
          <textarea
            data-testid={`${baseTestId}-raw-text`}
            value={rawText}
            onChange={(e) => setRawText(e.currentTarget.value)}
            rows={6}
            className="font-mono"
          />
          <div className="flex items-center gap-[var(--ds-spacing-xs)]">
            <button type="button" data-testid={`${baseTestId}-raw-apply`} onClick={applyRaw}>
              Apply raw JSON
            </button>
            {rawError ? (
              <span data-testid={`${baseTestId}-raw-error`} role="alert" className="text-[var(--ds-text-accent-critical)]">
                {rawError}
              </span>
            ) : null}
          </div>
        </div>
      ) : null}
    </div>
  );
}
