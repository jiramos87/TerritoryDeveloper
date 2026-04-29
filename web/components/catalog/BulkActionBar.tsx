"use client";

import { useState } from "react";
import type { BulkAction } from "@/lib/catalog/bulk-actions";
import { BulkConfirmDialog } from "./BulkConfirmDialog";

type Props = {
  selectedIds: string[];
  onClear: () => void;
  onSuccess?: (action: BulkAction, updated: number) => void;
};

const ACTIONS: { value: BulkAction; label: string }[] = [
  { value: "retire", label: "Retire" },
  { value: "restore", label: "Restore" },
  { value: "publish", label: "Publish" },
];

export function BulkActionBar({ selectedIds, onClear, onSuccess }: Props) {
  const [action, setAction] = useState<BulkAction>("retire");
  const [confirming, setConfirming] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (selectedIds.length === 0) return null;

  async function handleConfirm() {
    setConfirming(false);
    setLoading(true);
    setError(null);
    try {
      const res = await fetch("/api/catalog/bulk", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ action, entity_ids: selectedIds }),
      });
      const json = (await res.json()) as { ok?: boolean; data?: { updated: number }; error?: string };
      if (!res.ok || !json.ok) {
        setError(json.error ?? "Bulk action failed");
        return;
      }
      onSuccess?.(action, json.data?.updated ?? 0);
      onClear();
    } catch {
      setError("Network error — bulk action failed");
    } finally {
      setLoading(false);
    }
  }

  return (
    <>
      <div
        role="toolbar"
        aria-label="Bulk actions"
        data-testid="bulk-action-bar"
        className="sticky bottom-0 z-30 flex items-center gap-[var(--ds-spacing-sm)] px-[var(--ds-spacing-lg)] py-[var(--ds-spacing-sm)] bg-[var(--ds-bg-surface)] border-t border-[var(--ds-border-default)] shadow-md"
      >
        <span className="text-[var(--ds-text-secondary)] text-sm">
          {selectedIds.length} selected
        </span>
        <select
          value={action}
          onChange={(e) => setAction(e.target.value as BulkAction)}
          aria-label="Bulk action"
          disabled={loading}
          className="rounded-[var(--ds-radius-sm)] border border-[var(--ds-border-default)] bg-[var(--ds-bg-input)] text-[var(--ds-text-primary)] px-[var(--ds-spacing-sm)] py-[var(--ds-spacing-xs)] text-sm"
        >
          {ACTIONS.map((a) => (
            <option key={a.value} value={a.value}>
              {a.label}
            </option>
          ))}
        </select>
        <button
          type="button"
          onClick={() => setConfirming(true)}
          disabled={loading}
          data-testid="bulk-apply-btn"
          className="px-[var(--ds-spacing-md)] py-[var(--ds-spacing-xs)] rounded-[var(--ds-radius-sm)] text-[var(--ds-text-on-accent)] bg-[var(--ds-color-accent)] hover:bg-[var(--ds-color-accent-hover)] text-sm disabled:opacity-50"
        >
          {loading ? "Applying…" : "Apply"}
        </button>
        <button
          type="button"
          onClick={onClear}
          disabled={loading}
          aria-label="Clear selection"
          className="ml-auto text-[var(--ds-text-secondary)] hover:text-[var(--ds-text-primary)] text-sm"
        >
          Clear
        </button>
        {error && (
          <span role="alert" className="text-[var(--ds-color-error)] text-xs">
            {error}
          </span>
        )}
      </div>
      {confirming && (
        <BulkConfirmDialog
          action={action}
          count={selectedIds.length}
          onConfirm={handleConfirm}
          onCancel={() => setConfirming(false)}
        />
      )}
    </>
  );
}
