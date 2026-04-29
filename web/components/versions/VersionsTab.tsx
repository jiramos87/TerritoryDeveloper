"use client";

/**
 * VersionsTab — entity-version timeline tab (TECH-3223 / Stage 14.2).
 *
 * Client component that fetches `/api/catalog/{kind}/{entityId}/versions` and
 * renders an infinite-scroll-style timeline (explicit "Load more" button).
 * Owns fetch + cursor state; delegates pure rendering to `VersionsTabView`.
 *
 * Wired by the 8 catalog kind detail pages (TECH-3224). Diff links target the
 * Stage 14.3 typed-diff route which does not exist yet — placeholder href.
 *
 * @see ia/projects/asset-pipeline/stage-14.2 — TECH-3223 §Plan Digest
 * @see web/components/versions/VersionsTabView.tsx — view layer
 * @see web/lib/repos/history-repo.ts — backing endpoint shape
 */
import { useCallback, useEffect, useState } from "react";

import type { CatalogKind } from "@/lib/refs/types";
import type {
  EntityVersionRow,
  ListVersionsResult,
} from "@/lib/repos/history-repo";

import VersionsTabView from "./VersionsTabView";

export interface VersionsTabProps {
  entityId: string;
  kind: CatalogKind;
}

interface VersionsEnvelope {
  ok: boolean;
  data?: ListVersionsResult;
  error?: string;
  code?: string;
}

export const PAGE_SIZE = 20;

/**
 * Build the versions endpoint URL for a given (kind, entityId, cursor) tuple.
 * Pure helper — exported for unit-test coverage of cursor pagination shape.
 */
export function buildVersionsUrl(
  kind: CatalogKind,
  entityId: string,
  cursor: string | null,
  pageSize: number = PAGE_SIZE,
): string {
  const params = new URLSearchParams({ limit: String(pageSize) });
  if (cursor != null) params.set("cursor", cursor);
  return `/api/catalog/${kind}/${entityId}/versions?${params.toString()}`;
}

/**
 * Pure response unwrap — separates fetch envelope handling from React state.
 * Returns `{ok: true, data}` on success or `{ok: false, error}` on any
 * shape failure (HTTP not-ok, `body.ok !== true`, or missing `data`).
 */
export function unwrapVersionsResponse(
  httpOk: boolean,
  body: VersionsEnvelope,
): { ok: true; data: ListVersionsResult } | { ok: false; error: string } {
  if (!httpOk || body.ok !== true || body.data == null) {
    return { ok: false, error: body.error ?? "Failed to load versions." };
  }
  return { ok: true, data: body.data };
}

export default function VersionsTab({ entityId, kind }: VersionsTabProps) {
  const [rows, setRows] = useState<EntityVersionRow[]>([]);
  const [nextCursor, setNextCursor] = useState<string | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  const fetchPage = useCallback(
    async (cursor: string | null, append: boolean) => {
      setLoading(true);
      setError(null);
      try {
        const url = buildVersionsUrl(kind, entityId, cursor);
        const res = await fetch(url, { method: "GET" });
        const body = (await res.json()) as VersionsEnvelope;
        const out = unwrapVersionsResponse(res.ok, body);
        if (!out.ok) {
          setError(out.error);
          return;
        }
        setNextCursor(out.data.nextCursor);
        setRows((prev) => (append ? [...prev, ...out.data.rows] : out.data.rows));
      } catch (e) {
        setError(e instanceof Error ? e.message : "Failed to load versions.");
      } finally {
        setLoading(false);
      }
    },
    [entityId, kind],
  );

  useEffect(() => {
    void fetchPage(null, false);
  }, [fetchPage]);

  const handleLoadMore = useCallback(() => {
    if (nextCursor == null || loading) return;
    void fetchPage(nextCursor, true);
  }, [fetchPage, nextCursor, loading]);

  const handleRetry = useCallback(() => {
    setRows([]);
    setNextCursor(null);
    void fetchPage(null, false);
  }, [fetchPage]);

  return (
    <VersionsTabView
      rows={rows}
      nextCursor={nextCursor}
      loading={loading}
      error={error}
      kind={kind}
      entityId={entityId}
      onLoadMore={handleLoadMore}
      onRetry={handleRetry}
    />
  );
}
