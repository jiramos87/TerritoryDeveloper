"use client";

import Link from "next/link";

/**
 * Spine-aware asset list (TECH-1786). Mirrors `<SpriteList>` shape — pure
 * presentational, parent owns fetch + filter state.
 */

export type AssetListRow = {
  entity_id: string;
  slug: string;
  display_name: string;
  category: string | null;
  status: "active" | "retired";
  updated_at: string;
};

export type AssetListFilter = "active" | "retired" | "all";

export type AssetListProps = {
  rows: AssetListRow[];
  filter: AssetListFilter;
  onFilterChange: (next: AssetListFilter) => void;
  loading?: boolean;
  error?: string | null;
};

const FILTERS: ReadonlyArray<{ id: AssetListFilter; label: string }> = [
  { id: "active", label: "Active" },
  { id: "retired", label: "Retired" },
  { id: "all", label: "All" },
];

export default function AssetList({ rows, filter, onFilterChange, loading, error }: AssetListProps) {
  return (
    <div data-testid="asset-list" className="flex flex-col gap-[var(--ds-spacing-md)]">
      <header className="flex items-center justify-between">
        <h1 className="text-[length:var(--ds-font-size-h2)] font-semibold">Assets</h1>
        <Link
          href="/catalog/assets/new"
          data-testid="asset-list-create-cta"
          className="rounded bg-[var(--ds-text-accent-info)] px-[var(--ds-spacing-md)] py-[var(--ds-spacing-xs)] text-[var(--ds-bg-canvas)]"
        >
          Create asset
        </Link>
      </header>

      <div data-testid="asset-list-filter-chips" role="tablist" className="flex gap-[var(--ds-spacing-xs)]">
        {FILTERS.map((f) => {
          const active = f.id === filter;
          return (
            <button
              key={f.id}
              type="button"
              role="tab"
              aria-selected={active}
              data-testid={`asset-list-filter-${f.id}`}
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
        <p data-testid="asset-list-error" role="alert" className="text-[var(--ds-text-accent-critical)]">
          {error}
        </p>
      ) : null}

      {loading ? (
        <p data-testid="asset-list-loading" className="text-[var(--ds-text-muted)]">
          Loading assets…
        </p>
      ) : null}

      {!loading && rows.length === 0 && !error ? (
        <p data-testid="asset-list-empty" className="text-[var(--ds-text-muted)]">
          No assets yet. Create one to get started.
        </p>
      ) : null}

      {rows.length > 0 ? (
        <ul data-testid="asset-list-rows" className="flex flex-col gap-[var(--ds-spacing-xs)]">
          {rows.map((row) => (
            <li key={row.entity_id} data-testid={`asset-list-row-${row.slug}`}>
              <Link
                href={`/catalog/assets/${row.slug}`}
                data-testid={`asset-list-row-link-${row.slug}`}
                className="grid grid-cols-[1fr_2fr_auto_auto_auto] gap-[var(--ds-spacing-md)] rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] p-[var(--ds-spacing-sm)] hover:border-[var(--ds-text-accent-info)]"
              >
                <span data-testid={`asset-list-row-slug-${row.slug}`} className="font-mono text-[var(--ds-text-primary)]">
                  {row.slug}
                </span>
                <span className="text-[var(--ds-text-primary)]">{row.display_name}</span>
                <span className="text-[var(--ds-text-muted)]">{row.category ?? "—"}</span>
                <span
                  data-testid={`asset-list-row-status-${row.slug}`}
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
      ) : null}
    </div>
  );
}
