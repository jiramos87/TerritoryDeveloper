"use client";

/**
 * Snapshot history surface (TECH-2674 §Acceptance #6).
 *
 * Renders the `catalog_snapshot` history table with a header-level
 * "Export Snapshot" button (manual POST) + per-row retire action when
 * status='active'. Pure-render: derivation lives in the route + lib;
 * the component owns local fetch/POST state only (no business logic).
 *
 * Cursor pagination: the parent server page provides the initial page
 * (`initialItems` + `initialNextCursor`); the "Load more" button calls
 * GET with the carried cursor and appends.
 *
 * @see ia/projects/asset-pipeline/stage-13.1 — TECH-2674 §Plan Digest
 */

import { useCallback, useState, useTransition } from "react";

export type SnapshotHistoryRow = {
  id: string;
  hash: string;
  manifest_path: string;
  schema_version: number;
  status: "active" | "retired";
  entity_counts_json: Record<string, number>;
  created_at: string;
  retired_at: string | null;
  created_by: string | null;
};

export type SnapshotHistoryTableProps = {
  initialItems: SnapshotHistoryRow[];
  initialNextCursor: string | null;
};

function shortHash(hash: string): string {
  return hash.length > 12 ? `${hash.slice(0, 12)}…` : hash;
}

function formatTs(iso: string): string {
  // Best-effort UTC ISO trim — drops `.fffZ` for table density.
  return iso.replace(/\.\d{3}Z$/, "Z");
}

export default function SnapshotHistoryTable({
  initialItems,
  initialNextCursor,
}: SnapshotHistoryTableProps) {
  const [items, setItems] = useState<SnapshotHistoryRow[]>(initialItems);
  const [nextCursor, setNextCursor] = useState<string | null>(
    initialNextCursor,
  );
  const [busy, startTransition] = useTransition();
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  const handleExport = useCallback(() => {
    setErrorMsg(null);
    startTransition(async () => {
      try {
        const res = await fetch("/api/catalog/snapshot", { method: "POST" });
        if (!res.ok) {
          setErrorMsg(`Export failed: ${res.status}`);
          return;
        }
        // Refresh page-1 list after a successful export.
        const listRes = await fetch("/api/catalog/snapshot");
        if (listRes.ok) {
          const body = (await listRes.json()) as {
            data: { items: SnapshotHistoryRow[]; nextCursor: string | null };
          };
          setItems(body.data.items);
          setNextCursor(body.data.nextCursor);
        }
      } catch (e) {
        setErrorMsg(`Export error: ${(e as Error).message}`);
      }
    });
  }, []);

  const handleRetire = useCallback((id: string) => {
    setErrorMsg(null);
    startTransition(async () => {
      try {
        const res = await fetch(`/api/catalog/snapshot/${id}/retire`, {
          method: "POST",
        });
        if (!res.ok) {
          setErrorMsg(`Retire failed: ${res.status}`);
          return;
        }
        // Mutate local row in place to flip the status chip.
        setItems((prev) =>
          prev.map((r) =>
            r.id === id
              ? { ...r, status: "retired", retired_at: new Date().toISOString() }
              : r,
          ),
        );
      } catch (e) {
        setErrorMsg(`Retire error: ${(e as Error).message}`);
      }
    });
  }, []);

  const handleLoadMore = useCallback(() => {
    if (nextCursor === null) return;
    setErrorMsg(null);
    startTransition(async () => {
      try {
        const url = `/api/catalog/snapshot?cursor=${encodeURIComponent(nextCursor)}`;
        const res = await fetch(url);
        if (!res.ok) {
          setErrorMsg(`Load more failed: ${res.status}`);
          return;
        }
        const body = (await res.json()) as {
          data: { items: SnapshotHistoryRow[]; nextCursor: string | null };
        };
        setItems((prev) => [...prev, ...body.data.items]);
        setNextCursor(body.data.nextCursor);
      } catch (e) {
        setErrorMsg(`Load more error: ${(e as Error).message}`);
      }
    });
  }, [nextCursor]);

  return (
    <div
      data-testid="snapshot-history-table"
      className="flex flex-col gap-[var(--ds-spacing-md)]"
    >
      <header className="flex items-center justify-between">
        <h1 className="text-[length:var(--ds-font-size-h2)] font-semibold">
          Snapshots
        </h1>
        <button
          type="button"
          data-testid="snapshot-export-cta"
          onClick={handleExport}
          disabled={busy}
          className="rounded bg-[var(--ds-text-accent-info)] px-[var(--ds-spacing-md)] py-[var(--ds-spacing-xs)] text-[var(--ds-bg-canvas)] disabled:opacity-50"
        >
          {busy ? "Exporting…" : "Export Snapshot"}
        </button>
      </header>

      {errorMsg !== null ? (
        <p
          role="alert"
          data-testid="snapshot-history-error"
          className="text-[var(--ds-text-accent-critical)]"
        >
          {errorMsg}
        </p>
      ) : null}

      <table
        className="w-full border-collapse text-[length:var(--ds-font-size-body)]"
        data-testid="snapshot-history-rows"
      >
        <thead>
          <tr className="border-b border-[var(--ds-border-subtle)] text-left text-[var(--ds-text-muted)]">
            <th className="py-[var(--ds-spacing-xs)] pr-[var(--ds-spacing-md)]">
              Hash
            </th>
            <th className="py-[var(--ds-spacing-xs)] pr-[var(--ds-spacing-md)]">
              Created
            </th>
            <th className="py-[var(--ds-spacing-xs)] pr-[var(--ds-spacing-md)]">
              Status
            </th>
            <th className="py-[var(--ds-spacing-xs)] pr-[var(--ds-spacing-md)]">
              Path
            </th>
            <th className="py-[var(--ds-spacing-xs)]">Actions</th>
          </tr>
        </thead>
        <tbody>
          {items.length === 0 ? (
            <tr>
              <td
                colSpan={5}
                className="py-[var(--ds-spacing-md)] text-center text-[var(--ds-text-muted)]"
              >
                No snapshots yet — click Export Snapshot to create one.
              </td>
            </tr>
          ) : (
            items.map((row) => (
              <tr
                key={row.id}
                data-testid={`snapshot-history-row-${row.id}`}
                className="border-b border-[var(--ds-border-subtle)]"
              >
                <td className="py-[var(--ds-spacing-xs)] pr-[var(--ds-spacing-md)] font-mono text-[var(--ds-text-primary)]">
                  {shortHash(row.hash)}
                </td>
                <td className="py-[var(--ds-spacing-xs)] pr-[var(--ds-spacing-md)] text-[var(--ds-text-muted)]">
                  {formatTs(row.created_at)}
                </td>
                <td className="py-[var(--ds-spacing-xs)] pr-[var(--ds-spacing-md)]">
                  <span
                    className={
                      row.status === "active"
                        ? "text-[var(--ds-text-accent-info)]"
                        : "text-[var(--ds-text-muted)]"
                    }
                  >
                    {row.status}
                  </span>
                </td>
                <td className="py-[var(--ds-spacing-xs)] pr-[var(--ds-spacing-md)] font-mono text-[var(--ds-text-muted)]">
                  {row.manifest_path}
                </td>
                <td className="py-[var(--ds-spacing-xs)]">
                  {row.status === "active" ? (
                    <button
                      type="button"
                      data-testid={`snapshot-retire-${row.id}`}
                      onClick={() => handleRetire(row.id)}
                      disabled={busy}
                      className="rounded border border-[var(--ds-border-subtle)] px-[var(--ds-spacing-sm)] py-[var(--ds-spacing-xs)] text-[var(--ds-text-primary)] disabled:opacity-50"
                    >
                      Retire
                    </button>
                  ) : null}
                </td>
              </tr>
            ))
          )}
        </tbody>
      </table>

      {nextCursor !== null ? (
        <button
          type="button"
          data-testid="snapshot-load-more"
          onClick={handleLoadMore}
          disabled={busy}
          className="self-start rounded border border-[var(--ds-border-subtle)] px-[var(--ds-spacing-md)] py-[var(--ds-spacing-xs)] text-[var(--ds-text-primary)] disabled:opacity-50"
        >
          Load more
        </button>
      ) : null}
    </div>
  );
}
