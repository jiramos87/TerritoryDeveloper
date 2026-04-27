"use client";

import Link from "next/link";

/**
 * Spine pool list (TECH-1788). Mirrors `<AssetList>` shape; adds member-count
 * badge per row.
 */

export type PoolListRow = {
  entity_id: string;
  slug: string;
  display_name: string;
  owner_category: string | null;
  member_count: number;
  status: "active" | "retired";
  updated_at: string;
};

export type PoolListFilter = "active" | "retired" | "all";

export type PoolListProps = {
  rows: PoolListRow[];
  filter: PoolListFilter;
  onFilterChange: (next: PoolListFilter) => void;
  loading?: boolean;
  error?: string | null;
};

const FILTERS: ReadonlyArray<{ id: PoolListFilter; label: string }> = [
  { id: "active", label: "Active" },
  { id: "retired", label: "Retired" },
  { id: "all", label: "All" },
];

export default function PoolList({ rows, filter, onFilterChange, loading, error }: PoolListProps) {
  return (
    <div data-testid="pool-list" className="flex flex-col gap-[var(--ds-spacing-md)]">
      <header className="flex items-center justify-between">
        <h1 className="text-[length:var(--ds-font-size-h2)] font-semibold">Pools</h1>
        <Link
          href="/catalog/pools/new"
          data-testid="pool-list-create-cta"
          className="rounded bg-[var(--ds-text-accent-info)] px-[var(--ds-spacing-md)] py-[var(--ds-spacing-xs)] text-[var(--ds-bg-canvas)]"
        >
          Create pool
        </Link>
      </header>

      <div data-testid="pool-list-filter-chips" role="tablist" className="flex gap-[var(--ds-spacing-xs)]">
        {FILTERS.map((f) => {
          const active = f.id === filter;
          return (
            <button
              key={f.id}
              type="button"
              role="tab"
              aria-selected={active}
              data-testid={`pool-list-filter-${f.id}`}
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
        <p data-testid="pool-list-error" role="alert" className="text-[var(--ds-text-accent-critical)]">
          {error}
        </p>
      ) : null}

      {loading ? (
        <p data-testid="pool-list-loading" className="text-[var(--ds-text-muted)]">
          Loading pools…
        </p>
      ) : null}

      {!loading && rows.length === 0 && !error ? (
        <p data-testid="pool-list-empty" className="text-[var(--ds-text-muted)]">
          No pools yet. Create one to get started.
        </p>
      ) : null}

      {rows.length > 0 ? (
        <ul data-testid="pool-list-rows" className="flex flex-col gap-[var(--ds-spacing-xs)]">
          {rows.map((row) => (
            <li key={row.entity_id} data-testid={`pool-list-row-${row.slug}`}>
              <Link
                href={`/catalog/pools/${row.slug}`}
                data-testid={`pool-list-row-link-${row.slug}`}
                className="grid grid-cols-[1fr_2fr_auto_auto_auto_auto] gap-[var(--ds-spacing-md)] rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] p-[var(--ds-spacing-sm)] hover:border-[var(--ds-text-accent-info)]"
              >
                <span data-testid={`pool-list-row-slug-${row.slug}`} className="font-mono text-[var(--ds-text-primary)]">
                  {row.slug}
                </span>
                <span className="text-[var(--ds-text-primary)]">{row.display_name}</span>
                <span className="text-[var(--ds-text-muted)]">{row.owner_category ?? "—"}</span>
                <span data-testid={`pool-list-row-members-${row.slug}`} className="text-[var(--ds-text-muted)]">
                  {row.member_count} members
                </span>
                <span
                  data-testid={`pool-list-row-status-${row.slug}`}
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
