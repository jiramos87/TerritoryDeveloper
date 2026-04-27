"use client";

import Link from "next/link";

/**
 * Spine panel list (TECH-1886 / Stage 8.1). Mirrors `<ButtonList>` shape;
 * surfaces archetype slug + child count badges per row.
 */

export type PanelListRow = {
  entity_id: string;
  slug: string;
  display_name: string;
  archetype_entity_id: string | null;
  child_count: number;
  status: "active" | "retired";
  updated_at: string;
};

export type PanelListFilter = "active" | "retired" | "all";

export type PanelListProps = {
  rows: PanelListRow[];
  filter: PanelListFilter;
  onFilterChange: (next: PanelListFilter) => void;
  loading?: boolean;
  error?: string | null;
};

const FILTERS: ReadonlyArray<{ id: PanelListFilter; label: string }> = [
  { id: "active", label: "Active" },
  { id: "retired", label: "Retired" },
  { id: "all", label: "All" },
];

export default function PanelList({
  rows,
  filter,
  onFilterChange,
  loading,
  error,
}: PanelListProps) {
  return (
    <div data-testid="panel-list" className="flex flex-col gap-[var(--ds-spacing-md)]">
      <header className="flex items-center justify-between">
        <h1 className="text-[length:var(--ds-font-size-h2)] font-semibold">Panels</h1>
        <Link
          href="/catalog/panels/new"
          data-testid="panel-list-create-cta"
          className="rounded bg-[var(--ds-text-accent-info)] px-[var(--ds-spacing-md)] py-[var(--ds-spacing-xs)] text-[var(--ds-bg-canvas)]"
        >
          Create panel
        </Link>
      </header>

      <div
        data-testid="panel-list-filter-chips"
        role="tablist"
        className="flex gap-[var(--ds-spacing-xs)]"
      >
        {FILTERS.map((f) => {
          const active = f.id === filter;
          return (
            <button
              key={f.id}
              type="button"
              role="tab"
              aria-selected={active}
              data-testid={`panel-list-filter-${f.id}`}
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
        <p
          data-testid="panel-list-error"
          role="alert"
          className="text-[var(--ds-text-accent-critical)]"
        >
          {error}
        </p>
      ) : null}

      {loading ? (
        <p data-testid="panel-list-loading" className="text-[var(--ds-text-muted)]">
          Loading panels…
        </p>
      ) : null}

      {!loading && rows.length === 0 && !error ? (
        <p data-testid="panel-list-empty" className="text-[var(--ds-text-muted)]">
          No panels yet. Create one to get started.
        </p>
      ) : null}

      {rows.length > 0 ? (
        <ul
          data-testid="panel-list-rows"
          className="flex flex-col gap-[var(--ds-spacing-xs)]"
        >
          {rows.map((row) => (
            <li key={row.entity_id} data-testid={`panel-list-row-${row.slug}`}>
              <Link
                href={`/catalog/panels/${row.slug}`}
                data-testid={`panel-list-row-link-${row.slug}`}
                className="grid grid-cols-[1fr_2fr_auto_auto_auto] gap-[var(--ds-spacing-md)] rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] p-[var(--ds-spacing-sm)] hover:border-[var(--ds-text-accent-info)]"
              >
                <span
                  data-testid={`panel-list-row-slug-${row.slug}`}
                  className="font-mono text-[var(--ds-text-primary)]"
                >
                  {row.slug}
                </span>
                <span
                  data-testid={`panel-list-row-name-${row.slug}`}
                  className="text-[var(--ds-text-primary)]"
                >
                  {row.display_name}
                </span>
                <span
                  data-testid={`panel-list-row-children-${row.slug}`}
                  className="rounded px-[var(--ds-spacing-xs)] text-[var(--ds-text-muted)]"
                  title="Child count"
                >
                  {row.child_count} children
                </span>
                <span
                  data-testid={`panel-list-row-archetype-${row.slug}`}
                  className="rounded px-[var(--ds-spacing-xs)] text-[var(--ds-text-muted)]"
                >
                  {row.archetype_entity_id ? `archetype #${row.archetype_entity_id}` : "no archetype"}
                </span>
                <span
                  data-testid={`panel-list-row-status-${row.slug}`}
                  className={
                    row.status === "active"
                      ? "rounded px-[var(--ds-spacing-xs)] text-[var(--ds-text-accent-info)]"
                      : "rounded px-[var(--ds-spacing-xs)] text-[var(--ds-text-muted)]"
                  }
                >
                  {row.status}
                </span>
              </Link>
            </li>
          ))}
        </ul>
      ) : null}
    </div>
  );
}
