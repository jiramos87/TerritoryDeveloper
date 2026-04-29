"use client";

import Link from "next/link";
import { BulkActionBar } from "./BulkActionBar";
import type { BulkSelectionProp } from "./SpriteList";

/**
 * Spine button list (TECH-1885 / Stage 8.1). Mirrors `<PoolList>` shape;
 * surfaces size_variant + action_id badges per row.
 */

export type ButtonListRow = {
  entity_id: string;
  slug: string;
  display_name: string;
  size_variant: string | null;
  action_id: string | null;
  status: "active" | "retired";
  updated_at: string;
};

export type ButtonListFilter = "active" | "retired" | "all";

export type ButtonListProps = {
  rows: ButtonListRow[];
  filter: ButtonListFilter;
  onFilterChange: (next: ButtonListFilter) => void;
  loading?: boolean;
  error?: string | null;
  bulkSelection?: BulkSelectionProp;
};

const FILTERS: ReadonlyArray<{ id: ButtonListFilter; label: string }> = [
  { id: "active", label: "Active" },
  { id: "retired", label: "Retired" },
  { id: "all", label: "All" },
];

export default function ButtonList({ rows, filter, onFilterChange, loading, error, bulkSelection }: ButtonListProps) {
  const allIds = rows.map((r) => r.entity_id);
  return (
    <div data-testid="button-list" className="flex flex-col gap-[var(--ds-spacing-md)]">
      <header className="flex items-center justify-between">
        <h1 className="text-[length:var(--ds-font-size-h2)] font-semibold">Buttons</h1>
        <Link
          href="/catalog/buttons/new"
          data-testid="button-list-create-cta"
          className="rounded bg-[var(--ds-text-accent-info)] px-[var(--ds-spacing-md)] py-[var(--ds-spacing-xs)] text-[var(--ds-bg-canvas)]"
        >
          Create button
        </Link>
      </header>

      <div data-testid="button-list-filter-chips" role="tablist" className="flex gap-[var(--ds-spacing-xs)]">
        {FILTERS.map((f) => {
          const active = f.id === filter;
          return (
            <button
              key={f.id}
              type="button"
              role="tab"
              aria-selected={active}
              data-testid={`button-list-filter-${f.id}`}
              onClick={() => onFilterChange(f.id)}
              className={
                active
                  ? "rounded border border-[var(--ds-text-accent-info)] bg-[var(--ds-bg-panel)] px-[var(--ds-spacing-sm)] py-[var(--ds-spacing-xs)] text-[var(--ds-text-primary)]"
                  : "rounded border border-[var(--ds-border-subtle)] px-[var(--ds-spacing-sm)] py-[var(--ds-spacing-xs)] text-[var(--ds-text-muted)]"
              }
            >
              {f.label}
            </button>
          );
        })}
      </div>

      {error ? (
        <p data-testid="button-list-error" role="alert" className="text-[var(--ds-text-accent-critical)]">
          {error}
        </p>
      ) : null}

      {loading ? (
        <p data-testid="button-list-loading" className="text-[var(--ds-text-muted)]">
          Loading buttons…
        </p>
      ) : null}

      {!loading && rows.length === 0 && !error ? (
        <p data-testid="button-list-empty" className="text-[var(--ds-text-muted)]">
          No buttons yet. Create one to get started.
        </p>
      ) : null}

      {rows.length > 0 ? (
        <>
          {bulkSelection && (
            <div className="flex items-center gap-[var(--ds-spacing-xs)] pb-[var(--ds-spacing-xs)]">
              <input
                type="checkbox"
                aria-label="Select all buttons"
                checked={allIds.length > 0 && allIds.every((id) => bulkSelection.selected.has(id))}
                onChange={() => bulkSelection.toggleAll(allIds)}
              />
              <span className="text-[var(--ds-text-muted)] text-sm">Select all</span>
            </div>
          )}
          <ul data-testid="button-list-rows" className="flex flex-col gap-[var(--ds-spacing-xs)]">
            {rows.map((row) => (
              <li key={row.entity_id} data-testid={`button-list-row-${row.slug}`} className="flex items-center gap-[var(--ds-spacing-xs)]">
                {bulkSelection && (
                  <input
                    type="checkbox"
                    aria-label={`Select ${row.display_name}`}
                    checked={bulkSelection.selected.has(row.entity_id)}
                    onChange={() => bulkSelection.toggle(row.entity_id)}
                  />
                )}
                <Link
                  href={`/catalog/buttons/${row.slug}`}
                  data-testid={`button-list-row-link-${row.slug}`}
                  className="grid flex-1 grid-cols-[1fr_2fr_auto_auto_auto_auto] gap-[var(--ds-spacing-md)] rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] p-[var(--ds-spacing-sm)] hover:border-[var(--ds-text-accent-info)]"
                >
                  <span data-testid={`button-list-row-slug-${row.slug}`} className="font-mono text-[var(--ds-text-primary)]">
                    {row.slug}
                  </span>
                  <span className="text-[var(--ds-text-primary)]">{row.display_name}</span>
                  <span data-testid={`button-list-row-size-${row.slug}`} className="text-[var(--ds-text-muted)]">
                    {row.size_variant ?? "—"}
                  </span>
                  <span data-testid={`button-list-row-action-${row.slug}`} className="font-mono text-[var(--ds-text-muted)]">
                    {row.action_id && row.action_id.length > 0 ? row.action_id : "—"}
                  </span>
                  <span
                    data-testid={`button-list-row-status-${row.slug}`}
                    className={
                      row.status === "active"
                        ? "text-[var(--ds-text-accent-info)]"
                        : "text-[var(--ds-text-muted)]"
                    }
                  >
                    {row.status}
                  </span>
                  <span className="text-[var(--ds-text-muted)] text-[length:var(--text-xs)]">{row.updated_at}</span>
                </Link>
              </li>
            ))}
          </ul>
          {bulkSelection && (
            <BulkActionBar
              selectedIds={Array.from(bulkSelection.selected)}
              onClear={bulkSelection.clear}
            />
          )}
        </>
      ) : null}
    </div>
  );
}
