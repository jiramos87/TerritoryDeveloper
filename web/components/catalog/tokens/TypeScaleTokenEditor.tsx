"use client";

import type { CatalogTokenTypeScaleValue } from "@/types/api/catalog-api";

/**
 * Type-scale token editor (TECH-2093 / Stage 10.1).
 * Presentational; parent owns value + onChange.
 *
 * @see ia/projects/asset-pipeline/stage-10.1 — TECH-2093 §Plan Digest
 */

export type TypeScaleTokenEditorProps = {
  value: CatalogTokenTypeScaleValue;
  onChange: (next: CatalogTokenTypeScaleValue) => void;
  disabled?: boolean;
};

export default function TypeScaleTokenEditor({ value, onChange, disabled }: TypeScaleTokenEditorProps) {
  function patch(p: Partial<CatalogTokenTypeScaleValue>) {
    onChange({ ...value, ...p });
  }
  return (
    <div data-testid="type-scale-token-editor" className="flex flex-col gap-[var(--ds-spacing-sm)]">
      <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
        <span className="text-[var(--ds-text-muted)]">Font family</span>
        <input
          type="text"
          data-testid="type-scale-token-editor-font-family"
          value={value.font_family}
          disabled={disabled}
          onChange={(e) => patch({ font_family: e.currentTarget.value })}
          className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] px-[var(--ds-spacing-sm)] py-[var(--ds-spacing-xs)] font-mono"
        />
      </label>
      <div className="grid grid-cols-2 gap-[var(--ds-spacing-sm)]">
        <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
          <span className="text-[var(--ds-text-muted)]">Size (px)</span>
          <input
            type="number"
            data-testid="type-scale-token-editor-size"
            value={value.size_px}
            min={0}
            disabled={disabled}
            onChange={(e) => patch({ size_px: Number.parseFloat(e.currentTarget.value) })}
            className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] px-[var(--ds-spacing-sm)] py-[var(--ds-spacing-xs)]"
          />
        </label>
        <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
          <span className="text-[var(--ds-text-muted)]">Line height</span>
          <input
            type="number"
            data-testid="type-scale-token-editor-line-height"
            value={value.line_height}
            step={0.05}
            min={0}
            disabled={disabled}
            onChange={(e) => patch({ line_height: Number.parseFloat(e.currentTarget.value) })}
            className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] px-[var(--ds-spacing-sm)] py-[var(--ds-spacing-xs)]"
          />
        </label>
      </div>
    </div>
  );
}
