"use client";

import { useEffect, useState } from "react";

import type { EntityRefSearchRow } from "@/types/api/catalog-api";

/**
 * Asset multi-select modal (TECH-1788, reused by TECH-1789).
 *
 * Presents `kind=asset` rows from `GET /api/catalog/entities?kind=asset`.
 * Parent owns the dialog open state + receives the chosen rows on submit.
 *
 * @see ia/projects/asset-pipeline/stage-7.1.md — TECH-1788 §Plan Digest
 */

export type AssetMultiSelectModalProps = {
  open: boolean;
  /** Entity ids already in scope — these rows are pre-checked + non-removable. */
  alreadySelected?: string[];
  onSubmit: (rows: EntityRefSearchRow[]) => void;
  onCancel: () => void;
};

type EntitiesResponse = {
  ok: true | "ok" | "error";
  data?: { items: EntityRefSearchRow[] };
  error?: { code: string; message: string };
};

export default function AssetMultiSelectModal({ open, alreadySelected, onSubmit, onCancel }: AssetMultiSelectModalProps) {
  const [query, setQuery] = useState<string>("");
  const [rows, setRows] = useState<EntityRefSearchRow[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);
  const [picked, setPicked] = useState<Record<string, boolean>>({});

  useEffect(() => {
    if (!open) return;
    let cancelled = false;
    const params = new URLSearchParams({ kind: "asset", limit: "50" });
    if (query.trim() !== "") params.set("q", query.trim());
    const debounced = setTimeout(() => {
      if (cancelled) return;
      setLoading(true);
      fetch(`/api/catalog/entities?${params.toString()}`)
        .then((res) => res.json() as Promise<EntitiesResponse>)
        .then((payload) => {
          if (cancelled) return;
          if ((payload.ok !== "ok" && payload.ok !== true) || !payload.data) {
            setError(payload.error?.message ?? "Search failed");
            setRows([]);
            setLoading(false);
            return;
          }
          setRows(payload.data.items);
          setError(null);
          setLoading(false);
        })
        .catch((err: unknown) => {
          if (cancelled) return;
          setError(err instanceof Error ? err.message : "Network error");
          setRows([]);
          setLoading(false);
        });
    }, 200);
    return () => {
      cancelled = true;
      clearTimeout(debounced);
    };
  }, [open, query]);

  if (!open) return null;

  const alreadySet = new Set(alreadySelected ?? []);

  function toggle(id: string) {
    if (alreadySet.has(id)) return;
    setPicked((cur) => ({ ...cur, [id]: !cur[id] }));
  }
  function submit() {
    const chosen = rows.filter((r) => picked[r.entity_id] === true);
    onSubmit(chosen);
    setPicked({});
    setQuery("");
  }

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-label="Select assets"
      data-testid="asset-multi-select-modal"
      className="fixed inset-0 z-50 flex items-center justify-center bg-[rgba(0,0,0,0.4)]"
    >
      <div className="flex w-[640px] max-w-[90vw] flex-col gap-[var(--ds-spacing-sm)] rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] p-[var(--ds-spacing-md)]">
        <h2 className="text-[length:var(--ds-font-size-h3)] font-semibold">Add assets</h2>
        <input
          type="text"
          data-testid="asset-multi-select-query"
          value={query}
          onChange={(e) => setQuery(e.currentTarget.value)}
          placeholder="Search by slug or display name"
        />
        {error ? (
          <p data-testid="asset-multi-select-error" role="alert" className="text-[var(--ds-text-accent-critical)]">
            {error}
          </p>
        ) : null}
        {loading ? (
          <p data-testid="asset-multi-select-loading" className="text-[var(--ds-text-muted)]">
            Loading…
          </p>
        ) : null}
        <ul data-testid="asset-multi-select-rows" className="flex max-h-[40vh] flex-col gap-[var(--ds-spacing-xs)] overflow-y-auto">
          {rows.length === 0 && !loading ? (
            <li data-testid="asset-multi-select-empty" className="text-[var(--ds-text-muted)]">
              No matches.
            </li>
          ) : null}
          {rows.map((r) => {
            const already = alreadySet.has(r.entity_id);
            return (
              <li key={r.entity_id} data-testid={`asset-multi-select-row-${r.slug}`} className="flex items-center gap-[var(--ds-spacing-xs)]">
                <input
                  type="checkbox"
                  data-testid={`asset-multi-select-checkbox-${r.slug}`}
                  checked={already || picked[r.entity_id] === true}
                  disabled={already}
                  onChange={() => toggle(r.entity_id)}
                />
                <span className="font-mono text-[var(--ds-text-muted)]">{r.slug}</span>
                <span className="text-[var(--ds-text-primary)]">{r.display_name}</span>
                {already ? (
                  <span data-testid={`asset-multi-select-already-${r.slug}`} className="text-[var(--ds-text-muted)]">
                    already member
                  </span>
                ) : null}
              </li>
            );
          })}
        </ul>
        <div className="flex justify-end gap-[var(--ds-spacing-sm)]">
          <button type="button" data-testid="asset-multi-select-cancel" onClick={onCancel}>
            Cancel
          </button>
          <button type="button" data-testid="asset-multi-select-submit" onClick={submit}>
            Add selected
          </button>
        </div>
      </div>
    </div>
  );
}
