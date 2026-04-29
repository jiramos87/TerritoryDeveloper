"use client";

import {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import { useRouter } from "next/navigation";

import type { SearchResultRow } from "@/lib/catalog/search-query";
import { useGlobalHotkey } from "@/lib/hooks/useGlobalHotkey";
import { useSearchDebounce } from "@/lib/hooks/useSearchDebounce";
import { SearchResultGroup } from "./SearchResultGroup";

const KIND_ORDER = [
  "sprite",
  "asset",
  "button",
  "panel",
  "audio",
  "pool",
  "token",
  "archetype",
] as const;

function routeForRow(row: SearchResultRow): string {
  if (row.kind === "archetype") return `/catalog/archetypes/${row.entity_id}`;
  return `/catalog/${row.kind}s/${row.slug}`;
}

const PANEL_ID = "search-results-panel";

export function SearchBar() {
  const [open, setOpen] = useState(false);
  const [q, setQ] = useState("");
  const inputRef = useRef<HTMLInputElement>(null);
  const router = useRouter();

  const { results, loading } = useSearchDebounce(q);

  const grouped = useMemo(() => {
    const map = new Map<string, SearchResultRow[]>();
    for (const row of results) {
      const list = map.get(row.kind) ?? [];
      list.push(row);
      map.set(row.kind, list);
    }
    return map;
  }, [results]);

  const flatRows = useMemo(
    () => KIND_ORDER.flatMap((k) => grouped.get(k) ?? []),
    [grouped],
  );

  const [activeIdx, setActiveIdx] = useState(-1);
  const activeRowId =
    activeIdx >= 0 && flatRows[activeIdx]
      ? `search-row-${flatRows[activeIdx]!.entity_id}`
      : undefined;

  const openPanel = useCallback(() => {
    setOpen(true);
    setTimeout(() => inputRef.current?.focus(), 0);
  }, []);

  const closePanel = useCallback(() => {
    setOpen(false);
    setQ("");
    setActiveIdx(-1);
  }, []);

  useGlobalHotkey({ onTrigger: openPanel, disabled: open });

  useEffect(() => {
    setActiveIdx(-1);
  }, [results]);

  useEffect(() => {
    if (!open) return;
    function onKeyDown(e: KeyboardEvent) {
      if (e.key === "Escape") { closePanel(); return; }
      if (e.key === "ArrowDown") {
        e.preventDefault();
        setActiveIdx((i) => Math.min(i + 1, flatRows.length - 1));
      }
      if (e.key === "ArrowUp") {
        e.preventDefault();
        setActiveIdx((i) => Math.max(i - 1, 0));
      }
      if (e.key === "Enter") {
        const row = flatRows[activeIdx];
        if (row) { router.push(routeForRow(row)); closePanel(); }
      }
    }
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [open, flatRows, activeIdx, router, closePanel]);

  if (!open) return null;

  return (
    <>
      <div
        className="fixed inset-0 z-40 bg-black/30"
        aria-hidden="true"
        onClick={closePanel}
      />
      <div
        role="dialog"
        aria-modal="true"
        aria-label="Catalog search"
        className="fixed left-1/2 top-[20%] z-50 w-full max-w-lg -translate-x-1/2 rounded-[var(--ds-radius-md)] border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] shadow-lg"
      >
        <input
          ref={inputRef}
          role="combobox"
          aria-autocomplete="list"
          aria-expanded={results.length > 0}
          aria-controls={PANEL_ID}
          aria-activedescendant={activeRowId}
          type="text"
          placeholder="Search catalog…"
          value={q}
          onChange={(e) => setQ(e.target.value)}
          data-testid="search-input"
          className="w-full rounded-t-[var(--ds-radius-md)] border-b border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] px-[var(--ds-spacing-md)] py-[var(--ds-spacing-sm)] text-[length:var(--ds-font-size-body)] text-[var(--ds-text-primary)] outline-none placeholder:text-[var(--ds-text-muted)]"
        />

        {loading && (
          <div className="px-[var(--ds-spacing-md)] py-[var(--ds-spacing-sm)] text-[length:var(--ds-font-size-body-sm)] text-[var(--ds-text-muted)]">
            Searching…
          </div>
        )}

        {!loading && q.trim().length > 0 && results.length === 0 && (
          <div className="px-[var(--ds-spacing-md)] py-[var(--ds-spacing-sm)] text-[length:var(--ds-font-size-body-sm)] text-[var(--ds-text-muted)]">
            No matches for &ldquo;{q}&rdquo;
          </div>
        )}

        {!loading && q.trim().length === 0 && (
          <div className="px-[var(--ds-spacing-md)] py-[var(--ds-spacing-sm)] text-[length:var(--ds-font-size-body-sm)] text-[var(--ds-text-muted)]">
            Type to search
          </div>
        )}

        {results.length > 0 && (
          <ul
            id={PANEL_ID}
            role="listbox"
            aria-label="Search results"
            data-testid="search-results"
            className="max-h-80 overflow-y-auto pb-[var(--ds-spacing-xs)]"
          >
            {KIND_ORDER.map((kind) => {
              const rows = grouped.get(kind) ?? [];
              return (
                <SearchResultGroup
                  key={kind}
                  kind={kind}
                  rows={rows}
                  selectedId={activeRowId ?? null}
                  onSelect={(row) => { router.push(routeForRow(row)); closePanel(); }}
                  idPrefix="search-row"
                />
              );
            })}
          </ul>
        )}
      </div>
    </>
  );
}
