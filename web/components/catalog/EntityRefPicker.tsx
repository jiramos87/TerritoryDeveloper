"use client";

import { useEffect, useMemo, useRef, useState } from "react";

/**
 * Kind-filtered entity reference picker (TECH-1787).
 *
 * Searchable combobox listing `catalog_entity` rows filtered by `accepts_kind[]`
 * per DEC-A45. Renders green/red `version_pin` badge per DEC-A22 (red when
 * `current_published_version_id` is null OR target retired).
 *
 * Pure presentational on its props — parent owns the selected entity_id +
 * onChange. `value` is the full row when known (so callers can render outside
 * the dropdown without an extra fetch); `valueId` is the bare id fallback.
 *
 * @see ia/projects/asset-pipeline/stage-7.1.md — TECH-1787 §Plan Digest
 */

export type EntityRefRow = {
  entity_id: string;
  slug: string;
  display_name: string;
  kind: string;
  current_published_version_id: string | null;
  retired_at: string | null;
};

export type EntityRefPickerProps = {
  /** Kinds to filter against (`catalog_entity.kind`). */
  accepts_kind: string[];
  /** Current selection, or null when unset. Pass-through; parent owns state. */
  value: EntityRefRow | null;
  /**
   * Fallback when only the id is known (parent did not hydrate full row).
   * When non-null and `value` is null, picker renders an "unresolved" badge
   * with the bare id until the parent supplies the full row.
   */
  valueId?: string | null;
  /** Called with the chosen entity_id (or null on clear). */
  onChange: (entityId: string | null, row: EntityRefRow | null) => void;
  /** Optional label for the picker — rendered above the input. */
  label?: string;
  /** When true the picker is read-only (no dropdown). */
  disabled?: boolean;
  /** Test-id prefix override (defaults to `entity-ref-picker`). */
  testId?: string;
};

const DEBOUNCE_MS = 200;

type SearchPayload = {
  ok: "ok" | "error" | true;
  data?: { items: EntityRefRow[] };
  error?: { code: string; message: string };
};

/** Resolution badge — green when published version exists + entity not retired. */
function ResolutionBadge({ row, testIdBase }: { row: EntityRefRow; testIdBase: string }) {
  const resolved = row.current_published_version_id !== null && row.retired_at === null;
  if (resolved) {
    return (
      <span
        data-testid={`${testIdBase}-badge-resolved`}
        title={`Published v${row.current_published_version_id}`}
        className="rounded px-[var(--ds-spacing-xs)] text-[var(--ds-text-accent-info)]"
      >
        v{row.current_published_version_id}
      </span>
    );
  }
  const reason = row.retired_at !== null ? "retired" : "no published version";
  return (
    <span
      data-testid={`${testIdBase}-badge-unresolved`}
      title={`Unresolved: ${reason}`}
      role="status"
      className="rounded px-[var(--ds-spacing-xs)] text-[var(--ds-text-accent-critical)]"
    >
      unresolved
    </span>
  );
}

export default function EntityRefPicker({
  accepts_kind,
  value,
  valueId,
  onChange,
  label,
  disabled,
  testId,
}: EntityRefPickerProps) {
  const tid = testId ?? "entity-ref-picker";
  const [open, setOpen] = useState<boolean>(false);
  const [query, setQuery] = useState<string>("");
  const [items, setItems] = useState<EntityRefRow[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);
  const [highlight, setHighlight] = useState<number>(0);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const cancelRef = useRef<boolean>(false);

  const kindParam = useMemo(() => accepts_kind.join(","), [accepts_kind]);

  useEffect(() => {
    if (!open) return;
    if (debounceRef.current) clearTimeout(debounceRef.current);
    cancelRef.current = false;
    debounceRef.current = setTimeout(() => {
      if (cancelRef.current) return;
      setLoading(true);
      setError(null);
      const url = `/api/catalog/entities?kind=${encodeURIComponent(kindParam)}&q=${encodeURIComponent(query)}&limit=50`;
      fetch(url)
        .then((res) => res.json() as Promise<SearchPayload>)
        .then((payload) => {
          if (cancelRef.current) return;
          if ((payload.ok !== "ok" && payload.ok !== true) || !payload.data) {
            setError(payload.error?.message ?? "Failed to load entities");
            setItems([]);
            setLoading(false);
            return;
          }
          setItems(payload.data.items);
          setHighlight(0);
          setLoading(false);
        })
        .catch((err: unknown) => {
          if (cancelRef.current) return;
          setError(err instanceof Error ? err.message : "Network error");
          setItems([]);
          setLoading(false);
        });
    }, DEBOUNCE_MS);
    return () => {
      cancelRef.current = true;
      if (debounceRef.current) clearTimeout(debounceRef.current);
    };
  }, [open, query, kindParam]);

  function commit(row: EntityRefRow | null) {
    onChange(row?.entity_id ?? null, row);
    setOpen(false);
    setQuery("");
  }

  function handleKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
    if (disabled) return;
    if (e.key === "ArrowDown") {
      e.preventDefault();
      if (!open) {
        setOpen(true);
        return;
      }
      setHighlight((h) => Math.min(items.length - 1, h + 1));
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      setHighlight((h) => Math.max(0, h - 1));
    } else if (e.key === "Enter") {
      e.preventDefault();
      if (open && items[highlight]) commit(items[highlight]);
    } else if (e.key === "Escape") {
      setOpen(false);
    }
  }

  return (
    <div data-testid={tid} className="flex flex-col gap-[var(--ds-spacing-xs)]">
      {label ? (
        <span data-testid={`${tid}-label`} className="text-[var(--ds-text-muted)]">
          {label}
        </span>
      ) : null}

      {value ? (
        <div data-testid={`${tid}-resolved-row`} className="flex items-center gap-[var(--ds-spacing-xs)]">
          <span data-testid={`${tid}-resolved-display-name`} className="text-[var(--ds-text-primary)]">
            {value.display_name}
          </span>
          <span data-testid={`${tid}-resolved-slug`} className="font-mono text-[var(--ds-text-muted)]">
            {value.slug}
          </span>
          <ResolutionBadge row={value} testIdBase={tid} />
          {!disabled ? (
            <>
              <button
                type="button"
                data-testid={`${tid}-clear`}
                onClick={() => commit(null)}
                className="text-[var(--ds-text-muted)]"
              >
                Clear
              </button>
              <button
                type="button"
                data-testid={`${tid}-change`}
                onClick={() => setOpen(true)}
                className="text-[var(--ds-text-accent-info)]"
              >
                Change
              </button>
            </>
          ) : null}
        </div>
      ) : valueId ? (
        <div data-testid={`${tid}-unresolved-id-row`} className="flex items-center gap-[var(--ds-spacing-xs)]">
          <span className="font-mono text-[var(--ds-text-muted)]">id={valueId}</span>
          <span data-testid={`${tid}-badge-unresolved`} role="status" className="text-[var(--ds-text-accent-critical)]">
            unresolved
          </span>
          {!disabled ? (
            <button type="button" data-testid={`${tid}-change`} onClick={() => setOpen(true)}>
              Change
            </button>
          ) : null}
        </div>
      ) : !disabled ? (
        <button
          type="button"
          data-testid={`${tid}-open`}
          onClick={() => setOpen(true)}
          className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] px-[var(--ds-spacing-sm)] py-[var(--ds-spacing-xs)] text-[var(--ds-text-muted)]"
        >
          Pick {accepts_kind.join("/")}
        </button>
      ) : (
        <span data-testid={`${tid}-empty-disabled`} className="text-[var(--ds-text-muted)]">
          —
        </span>
      )}

      {open && !disabled ? (
        <div
          data-testid={`${tid}-popover`}
          role="dialog"
          aria-label="Entity picker"
          className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] p-[var(--ds-spacing-sm)]"
        >
          <input
            type="text"
            data-testid={`${tid}-search`}
            value={query}
            placeholder={`Search ${accepts_kind.join("/")}…`}
            onChange={(e) => setQuery(e.currentTarget.value)}
            onKeyDown={handleKeyDown}
            autoFocus
            className="w-full rounded border border-[var(--ds-border-subtle)] px-[var(--ds-spacing-xs)] py-[var(--ds-spacing-xs)]"
          />
          {error ? (
            <p data-testid={`${tid}-error`} role="alert" className="text-[var(--ds-text-accent-critical)]">
              {error}
            </p>
          ) : null}
          {loading ? (
            <p data-testid={`${tid}-loading`} className="text-[var(--ds-text-muted)]">
              Loading…
            </p>
          ) : null}
          {!loading && !error && items.length === 0 ? (
            <p data-testid={`${tid}-empty`} className="text-[var(--ds-text-muted)]">
              No matches
            </p>
          ) : null}
          {items.length > 0 ? (
            <ul role="listbox" data-testid={`${tid}-listbox`} className="flex flex-col gap-[var(--ds-spacing-xs)]">
              {items.map((row, idx) => (
                <li
                  key={row.entity_id}
                  role="option"
                  aria-selected={idx === highlight}
                  data-testid={`${tid}-option-${row.slug}`}
                  onMouseDown={(e) => {
                    e.preventDefault();
                    commit(row);
                  }}
                  onMouseEnter={() => setHighlight(idx)}
                  className={
                    idx === highlight
                      ? "rounded bg-[var(--ds-bg-canvas)] px-[var(--ds-spacing-xs)] py-[var(--ds-spacing-xs)]"
                      : "px-[var(--ds-spacing-xs)] py-[var(--ds-spacing-xs)]"
                  }
                >
                  <span className="text-[var(--ds-text-primary)]">{row.display_name}</span>{" "}
                  <span className="font-mono text-[var(--ds-text-muted)]">{row.slug}</span>{" "}
                  <ResolutionBadge row={row} testIdBase={tid} />
                </li>
              ))}
            </ul>
          ) : null}
        </div>
      ) : null}
    </div>
  );
}
