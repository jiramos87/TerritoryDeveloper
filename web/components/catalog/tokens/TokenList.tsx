"use client";

import Link from "next/link";

import { TOKEN_KINDS } from "@/lib/catalog/token-detail-schema";
import type { CatalogTokenKind } from "@/types/api/catalog-api";

/**
 * Spine token list (TECH-2093 / Stage 10.1). Mirrors `<ButtonList>` shape.
 * Surfaces token_kind chip + retired status; kind dropdown filters server-side.
 */

export type TokenListRow = {
  entity_id: string;
  slug: string;
  display_name: string;
  token_kind: CatalogTokenKind | null;
  status: "active" | "retired";
  updated_at: string;
};

export type TokenListFilter = "active" | "retired" | "all";
export type TokenListKindFilter = CatalogTokenKind | "all";

export type TokenListProps = {
  rows: TokenListRow[];
  filter: TokenListFilter;
  kindFilter: TokenListKindFilter;
  onFilterChange: (next: TokenListFilter) => void;
  onKindFilterChange: (next: TokenListKindFilter) => void;
  loading?: boolean;
  error?: string | null;
};

const FILTERS: ReadonlyArray<{ id: TokenListFilter; label: string }> = [
  { id: "active", label: "Active" },
  { id: "retired", label: "Retired" },
  { id: "all", label: "All" },
];

export default function TokenList({
  rows,
  filter,
  kindFilter,
  onFilterChange,
  onKindFilterChange,
  loading,
  error,
}: TokenListProps) {
  return (
    <div data-testid="token-list" className="flex flex-col gap-[var(--ds-spacing-md)]">
      <header className="flex items-center justify-between">
        <h1 className="text-[length:var(--ds-font-size-h2)] font-semibold">Tokens</h1>
        <Link
          href="/catalog/tokens/new"
          data-testid="token-list-create-cta"
          className="rounded bg-[var(--ds-text-accent-info)] px-[var(--ds-spacing-md)] py-[var(--ds-spacing-xs)] text-[var(--ds-bg-canvas)]"
        >
          Create token
        </Link>
      </header>

      <div className="flex items-center gap-[var(--ds-spacing-md)]">
        <div data-testid="token-list-filter-chips" role="tablist" className="flex gap-[var(--ds-spacing-xs)]">
          {FILTERS.map((f) => {
            const active = f.id === filter;
            return (
              <button
                key={f.id}
                type="button"
                role="tab"
                aria-selected={active}
                data-testid={`token-list-filter-${f.id}`}
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

        <label className="flex items-center gap-[var(--ds-spacing-xs)]">
          <span className="text-[var(--ds-text-muted)]">Kind</span>
          <select
            data-testid="token-list-kind-filter"
            value={kindFilter}
            onChange={(e) => onKindFilterChange(e.currentTarget.value as TokenListKindFilter)}
            className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] px-[var(--ds-spacing-sm)] py-[var(--ds-spacing-xs)]"
          >
            <option value="all">all</option>
            {TOKEN_KINDS.map((k) => (
              <option key={k} value={k}>
                {k}
              </option>
            ))}
          </select>
        </label>
      </div>

      {error ? (
        <p data-testid="token-list-error" role="alert" className="text-[var(--ds-text-accent-critical)]">
          {error}
        </p>
      ) : null}

      {loading ? (
        <p data-testid="token-list-loading" className="text-[var(--ds-text-muted)]">
          Loading tokens…
        </p>
      ) : null}

      {!loading && rows.length === 0 && !error ? (
        <p data-testid="token-list-empty" className="text-[var(--ds-text-muted)]">
          No tokens yet. Create one to get started.
        </p>
      ) : null}

      {rows.length > 0 ? (
        <ul data-testid="token-list-rows" className="flex flex-col gap-[var(--ds-spacing-xs)]">
          {rows.map((row) => (
            <li key={row.entity_id} data-testid={`token-list-row-${row.slug}`}>
              <Link
                href={`/catalog/tokens/${row.slug}`}
                data-testid={`token-list-row-link-${row.slug}`}
                className="grid grid-cols-[1fr_2fr_auto_auto_auto] gap-[var(--ds-spacing-md)] rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] p-[var(--ds-spacing-sm)] hover:border-[var(--ds-text-accent-info)]"
              >
                <span data-testid={`token-list-row-slug-${row.slug}`} className="font-mono text-[var(--ds-text-primary)]">
                  {row.slug}
                </span>
                <span className="text-[var(--ds-text-primary)]">{row.display_name}</span>
                <span data-testid={`token-list-row-kind-${row.slug}`} className="text-[var(--ds-text-muted)]">
                  {row.token_kind ?? "—"}
                </span>
                <span
                  data-testid={`token-list-row-status-${row.slug}`}
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
