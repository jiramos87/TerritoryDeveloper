"use client";

import type { BulkAction } from "@/lib/catalog/bulk-actions";

type Props = {
  action: BulkAction;
  count: number;
  onConfirm: () => void;
  onCancel: () => void;
};

const ACTION_LABEL: Record<BulkAction, string> = {
  retire: "Retire",
  restore: "Restore",
  publish: "Publish",
};

const ACTION_DESCRIPTION: Record<BulkAction, string> = {
  retire: "retired and hidden from active views",
  restore: "restored and made active again",
  publish: "queued for publishing",
};

export function BulkConfirmDialog({ action, count, onConfirm, onCancel }: Props) {
  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="bulk-dialog-title"
      className="fixed inset-0 z-50 flex items-center justify-center"
    >
      <div
        className="absolute inset-0 bg-black/40"
        aria-hidden="true"
        onClick={onCancel}
      />
      <div className="relative z-10 rounded-[var(--ds-radius-md)] bg-[var(--ds-bg-surface)] p-[var(--ds-spacing-xl)] shadow-lg w-80">
        <h2
          id="bulk-dialog-title"
          className="text-[var(--ds-text-primary)] font-semibold mb-[var(--ds-spacing-sm)]"
        >
          {ACTION_LABEL[action]} {count} {count === 1 ? "entity" : "entities"}?
        </h2>
        <p className="text-[var(--ds-text-secondary)] text-sm mb-[var(--ds-spacing-lg)]">
          {count} {count === 1 ? "entity" : "entities"} will be {ACTION_DESCRIPTION[action]}.
        </p>
        <div className="flex gap-[var(--ds-spacing-sm)] justify-end">
          <button
            type="button"
            onClick={onCancel}
            className="px-[var(--ds-spacing-md)] py-[var(--ds-spacing-xs)] rounded-[var(--ds-radius-sm)] text-[var(--ds-text-secondary)] bg-[var(--ds-bg-muted)] hover:bg-[var(--ds-bg-hover)]"
          >
            Cancel
          </button>
          <button
            type="button"
            onClick={onConfirm}
            data-testid="bulk-confirm-btn"
            className="px-[var(--ds-spacing-md)] py-[var(--ds-spacing-xs)] rounded-[var(--ds-radius-sm)] text-[var(--ds-text-on-accent)] bg-[var(--ds-color-accent)] hover:bg-[var(--ds-color-accent-hover)]"
          >
            {ACTION_LABEL[action]}
          </button>
        </div>
      </div>
    </div>
  );
}
