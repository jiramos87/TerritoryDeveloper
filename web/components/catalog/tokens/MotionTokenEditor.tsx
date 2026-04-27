"use client";

import type { CatalogTokenMotionValue } from "@/types/api/catalog-api";

/**
 * Motion token editor (TECH-2093 / Stage 10.1).
 * Curve enum + duration + optional 4-tuple cubic-bezier.
 *
 * @see ia/projects/asset-pipeline/stage-10.1 — TECH-2093 §Plan Digest
 */

const CURVES: ReadonlyArray<CatalogTokenMotionValue["curve"]> = [
  "linear",
  "ease-in",
  "ease-out",
  "ease-in-out",
  "cubic-bezier",
];

export type MotionTokenEditorProps = {
  value: CatalogTokenMotionValue;
  onChange: (next: CatalogTokenMotionValue) => void;
  disabled?: boolean;
};

export default function MotionTokenEditor({ value, onChange, disabled }: MotionTokenEditorProps) {
  const cb: [number, number, number, number] = value.cubic_bezier ?? [0, 0, 1, 1];
  function setCurve(curve: CatalogTokenMotionValue["curve"]) {
    if (curve === "cubic-bezier") onChange({ ...value, curve, cubic_bezier: cb });
    else {
      const next: CatalogTokenMotionValue = { curve, duration_ms: value.duration_ms };
      onChange(next);
    }
  }
  function setCb(idx: 0 | 1 | 2 | 3, raw: number) {
    const next: [number, number, number, number] = [cb[0], cb[1], cb[2], cb[3]];
    next[idx] = Number.isNaN(raw) ? 0 : raw;
    onChange({ ...value, curve: "cubic-bezier", cubic_bezier: next });
  }
  return (
    <div data-testid="motion-token-editor" className="flex flex-col gap-[var(--ds-spacing-sm)]">
      <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
        <span className="text-[var(--ds-text-muted)]">Curve</span>
        <select
          data-testid="motion-token-editor-curve"
          value={value.curve}
          disabled={disabled}
          onChange={(e) => setCurve(e.currentTarget.value as CatalogTokenMotionValue["curve"])}
          className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] px-[var(--ds-spacing-sm)] py-[var(--ds-spacing-xs)]"
        >
          {CURVES.map((c) => (
            <option key={c} value={c}>
              {c}
            </option>
          ))}
        </select>
      </label>

      <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
        <span className="text-[var(--ds-text-muted)]">Duration (ms)</span>
        <input
          type="number"
          data-testid="motion-token-editor-duration"
          value={value.duration_ms}
          min={0}
          disabled={disabled}
          onChange={(e) =>
            onChange({ ...value, duration_ms: Number.parseFloat(e.currentTarget.value) || 0 })
          }
          className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] px-[var(--ds-spacing-sm)] py-[var(--ds-spacing-xs)]"
        />
      </label>

      {value.curve === "cubic-bezier" ? (
        <div data-testid="motion-token-editor-cubic-bezier-row" className="grid grid-cols-4 gap-[var(--ds-spacing-sm)]">
          {([0, 1, 2, 3] as const).map((i) => (
            <label key={i} className="flex flex-col gap-[var(--ds-spacing-xs)]">
              <span className="text-[var(--ds-text-muted)]">cb[{i}]</span>
              <input
                type="number"
                data-testid={`motion-token-editor-cb-${i}`}
                value={cb[i]}
                step={0.05}
                disabled={disabled}
                onChange={(e) => setCb(i, Number.parseFloat(e.currentTarget.value))}
                className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] px-[var(--ds-spacing-sm)] py-[var(--ds-spacing-xs)]"
              />
            </label>
          ))}
        </div>
      ) : null}
    </div>
  );
}
