"use client";

import type { CatalogTokenSpacingValue } from "@/types/api/catalog-api";

/**
 * Spacing token editor (TECH-2093 / Stage 10.1).
 *
 * @see ia/projects/asset-pipeline/stage-10.1 — TECH-2093 §Plan Digest
 */

export type SpacingTokenEditorProps = {
  value: CatalogTokenSpacingValue;
  onChange: (next: CatalogTokenSpacingValue) => void;
  disabled?: boolean;
};

export default function SpacingTokenEditor({ value, onChange, disabled }: SpacingTokenEditorProps) {
  return (
    <div data-testid="spacing-token-editor" className="flex flex-col gap-[var(--ds-spacing-sm)]">
      <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
        <span className="text-[var(--ds-text-muted)]">Spacing (px)</span>
        <input
          type="number"
          data-testid="spacing-token-editor-px"
          value={value.px}
          min={0}
          step={1}
          disabled={disabled}
          onChange={(e) => onChange({ px: Number.parseFloat(e.currentTarget.value) || 0 })}
          className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] px-[var(--ds-spacing-sm)] py-[var(--ds-spacing-xs)]"
        />
      </label>
    </div>
  );
}
