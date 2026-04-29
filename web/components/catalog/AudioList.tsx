"use client";

import Link from "next/link";
import { BulkActionBar } from "./BulkActionBar";
import type { BulkSelectionProp } from "./SpriteList";

export type AudioListRow = {
  entity_id: string;
  slug: string;
  display_name: string;
  status: "active" | "retired";
  duration_ms: number | null;
  loudness_lufs: number | null;
  updated_at: string;
};

export type AudioListFilter = "active" | "retired" | "all";

export type AudioListProps = {
  rows: AudioListRow[];
  filter: AudioListFilter;
  onFilterChange: (next: AudioListFilter) => void;
  loading?: boolean;
  error?: string | null;
  bulkSelection?: BulkSelectionProp;
};

const FILTERS: ReadonlyArray<{ id: AudioListFilter; label: string }> = [
  { id: "active", label: "Active" },
  { id: "retired", label: "Retired" },
  { id: "all", label: "All" },
];

/**
 * Audio list view — filter chips + row table (TECH-1958).
 *
 * Pure presentational; parent owns fetch + filter state. Mirrors the
 * sprite-list shape (TECH-1672) so the catalog feels consistent across
 * `kind`s.
 */
export default function AudioList({
  rows,
  filter,
  onFilterChange,
  loading,
  error,
  bulkSelection,
}: AudioListProps) {
  const visibleCount = rows.length;
  const allIds = rows.map((r) => r.entity_id);

  return (
    <section
      data-testid="audio-list"
      className="flex flex-col gap-[var(--ds-spacing-md)]"
    >
      <header className="flex items-baseline justify-between">
        <h1 className="text-[length:var(--ds-font-size-h2)] font-semibold">
          Audio
        </h1>
        <span className="text-[var(--ds-text-muted)]">{visibleCount} entries</span>
      </header>

      <div
        data-testid="audio-list-filter-chips"
        role="tablist"
        aria-label="Audio status filter"
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
              data-testid={`audio-list-filter-${f.id}`}
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
          data-testid="audio-list-error"
          role="alert"
          className="text-[var(--ds-text-accent-critical)]"
        >
          {error}
        </p>
      ) : null}

      {loading ? (
        <p
          data-testid="audio-list-loading"
          className="text-[var(--ds-text-muted)]"
        >
          Loading audio entities…
        </p>
      ) : null}

      {!loading && rows.length === 0 && !error ? (
        <p
          data-testid="audio-list-empty"
          className="text-[var(--ds-text-muted)]"
        >
          No audio entities yet.
        </p>
      ) : null}

      {rows.length > 0 ? (
        <>
          <table data-testid="audio-list-table" className="w-full text-left border-collapse">
            <thead>
              <tr className="text-[var(--ds-text-muted)] text-[length:var(--ds-font-size-caption)]">
                {bulkSelection && (
                  <th className="py-[var(--ds-spacing-xs)] w-6">
                    <input
                      type="checkbox"
                      aria-label="Select all audio"
                      checked={allIds.length > 0 && allIds.every((id) => bulkSelection.selected.has(id))}
                      onChange={() => bulkSelection.toggleAll(allIds)}
                    />
                  </th>
                )}
                <th className="py-[var(--ds-spacing-xs)]">Slug</th>
                <th className="py-[var(--ds-spacing-xs)]">Display name</th>
                <th className="py-[var(--ds-spacing-xs)]">Status</th>
                <th className="py-[var(--ds-spacing-xs)]">Duration</th>
                <th className="py-[var(--ds-spacing-xs)]">Loudness (LUFS)</th>
                <th className="py-[var(--ds-spacing-xs)]">Updated</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => (
                <tr key={row.entity_id} data-testid={`audio-list-row-${row.slug}`} className="border-t border-[var(--ds-border-subtle)]">
                  {bulkSelection && (
                    <td className="py-[var(--ds-spacing-xs)]">
                      <input
                        type="checkbox"
                        aria-label={`Select ${row.display_name}`}
                        checked={bulkSelection.selected.has(row.entity_id)}
                        onChange={() => bulkSelection.toggle(row.entity_id)}
                      />
                    </td>
                  )}
                  <td className="py-[var(--ds-spacing-xs)]">
                    <Link href={`/catalog/audio/${row.slug}`} data-testid={`audio-list-row-link-${row.slug}`} className="underline">{row.slug}</Link>
                  </td>
                  <td className="py-[var(--ds-spacing-xs)]">{row.display_name}</td>
                  <td data-testid={`audio-list-row-status-${row.slug}`} className="py-[var(--ds-spacing-xs)]">{row.status}</td>
                  <td className="py-[var(--ds-spacing-xs)]">{row.duration_ms !== null ? `${row.duration_ms} ms` : "—"}</td>
                  <td className="py-[var(--ds-spacing-xs)]">{row.loudness_lufs !== null ? row.loudness_lufs.toFixed(1) : "—"}</td>
                  <td className="py-[var(--ds-spacing-xs)] text-[var(--ds-text-muted)] text-[length:var(--text-xs)]">{row.updated_at ? row.updated_at.slice(0, 10) : "—"}</td>
                </tr>
              ))}
            </tbody>
          </table>
          {bulkSelection && (
            <BulkActionBar selectedIds={Array.from(bulkSelection.selected)} onClear={bulkSelection.clear} />
          )}
        </>
      ) : null}
    </section>
  );
}
