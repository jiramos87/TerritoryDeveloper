"use client";

/**
 * RefsTab — references / refs-graph tab (TECH-3409 / Stage 14.4).
 *
 * Client component that fetches `/api/catalog/{kind}/{entityId}/refs` for both
 * incoming + outgoing edges with independent cursor state per side. Fires both
 * fetches in parallel on mount; per-side "Load more" + retry handlers.
 *
 * Wired by 8 catalog kind detail pages via TECH-3410 (Sprite/Asset/Button/
 * Panel/Audio/Pool/Token/Archetype). Mirrors VersionsTab seam pattern verbatim.
 *
 * @see ia/projects/asset-pipeline/stage-14.4 — TECH-3409 §Plan Digest
 * @see web/components/refs/RefsTabView.tsx — view layer
 * @see web/lib/repos/refs-repo.ts — backing endpoint shape
 */
import { useCallback, useEffect, useState } from "react";

import type { CatalogKind } from "@/lib/refs/types";
import type {
  CatalogRefEdgeRow,
  ListRefsResult,
} from "@/lib/repos/refs-repo";

import RefsTabView from "./RefsTabView";

export type RefsSide = "incoming" | "outgoing";

export interface RefsTabProps {
  entityId: string;
  kind: CatalogKind;
}

interface RefsEnvelope {
  ok: boolean;
  data?: {
    incoming: ListRefsResult;
    outgoing: ListRefsResult;
  };
  error?: string;
  code?: string;
}

export const PAGE_SIZE = 20;

/**
 * Build the refs endpoint URL for a given (kind, entityId, cursor, side) tuple.
 * Pure helper — exported for unit-test coverage.
 */
export function buildRefsUrl(
  kind: CatalogKind,
  entityId: string,
  cursor: string | null,
  side: RefsSide,
  pageSize: number = PAGE_SIZE,
): string {
  const params = new URLSearchParams({
    limit: String(pageSize),
    side,
  });
  if (cursor != null) params.set("cursor", cursor);
  return `/api/catalog/${kind}/${entityId}/refs?${params.toString()}`;
}

/**
 * Pure response unwrap — separates fetch envelope handling from React state.
 * Returns `{ok: true, data}` on success or `{ok: false, error}` on shape
 * failure (HTTP not-ok, `body.ok !== true`, missing `data`).
 */
export function unwrapRefsResponse(
  httpOk: boolean,
  body: RefsEnvelope,
):
  | { ok: true; data: { incoming: ListRefsResult; outgoing: ListRefsResult } }
  | { ok: false; error: string } {
  if (!httpOk || body.ok !== true || body.data == null) {
    return { ok: false, error: body.error ?? "Failed to load refs." };
  }
  return { ok: true, data: body.data };
}

interface SideState {
  rows: CatalogRefEdgeRow[];
  nextCursor: string | null;
  loading: boolean;
  error: string | null;
}

const INITIAL_SIDE: SideState = {
  rows: [],
  nextCursor: null,
  loading: true,
  error: null,
};

export default function RefsTab({ entityId, kind }: RefsTabProps) {
  const [incoming, setIncoming] = useState<SideState>(INITIAL_SIDE);
  const [outgoing, setOutgoing] = useState<SideState>(INITIAL_SIDE);

  const fetchIncoming = useCallback(
    async (cursor: string | null, append: boolean) => {
      setIncoming((prev) => ({ ...prev, loading: true, error: null }));
      try {
        const url = buildRefsUrl(kind, entityId, cursor, "incoming");
        const res = await fetch(url, { method: "GET" });
        const body = (await res.json()) as RefsEnvelope;
        const out = unwrapRefsResponse(res.ok, body);
        if (!out.ok) {
          setIncoming((prev) => ({ ...prev, loading: false, error: out.error }));
          return;
        }
        const page = out.data.incoming;
        setIncoming((prev) => ({
          rows: append ? [...prev.rows, ...page.rows] : page.rows,
          nextCursor: page.nextCursor,
          loading: false,
          error: null,
        }));
      } catch (e) {
        const msg = e instanceof Error ? e.message : "Failed to load refs.";
        setIncoming((prev) => ({ ...prev, loading: false, error: msg }));
      }
    },
    [entityId, kind],
  );

  const fetchOutgoing = useCallback(
    async (cursor: string | null, append: boolean) => {
      setOutgoing((prev) => ({ ...prev, loading: true, error: null }));
      try {
        const url = buildRefsUrl(kind, entityId, cursor, "outgoing");
        const res = await fetch(url, { method: "GET" });
        const body = (await res.json()) as RefsEnvelope;
        const out = unwrapRefsResponse(res.ok, body);
        if (!out.ok) {
          setOutgoing((prev) => ({ ...prev, loading: false, error: out.error }));
          return;
        }
        const page = out.data.outgoing;
        setOutgoing((prev) => ({
          rows: append ? [...prev.rows, ...page.rows] : page.rows,
          nextCursor: page.nextCursor,
          loading: false,
          error: null,
        }));
      } catch (e) {
        const msg = e instanceof Error ? e.message : "Failed to load refs.";
        setOutgoing((prev) => ({ ...prev, loading: false, error: msg }));
      }
    },
    [entityId, kind],
  );

  const fetchSide = useCallback(
    (side: RefsSide, cursor: string | null, append: boolean): Promise<void> => {
      return side === "incoming"
        ? fetchIncoming(cursor, append)
        : fetchOutgoing(cursor, append);
    },
    [fetchIncoming, fetchOutgoing],
  );

  useEffect(() => {
    // Mirror sibling VersionsTab pattern (Stage 14.2): mount-time fetches kick
    // both sides in parallel. The internal `setIncoming/setOutgoing` calls
    // happen synchronously inside the async fetch fns to flip `loading`/clear
    // `error` before the network roundtrip — flagged by
    // `react-hooks/set-state-in-effect` despite matching the sibling tab's
    // accepted pattern. Disable per-effect (no other call sites on this line).
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void fetchIncoming(null, false);
    void fetchOutgoing(null, false);
  }, [fetchIncoming, fetchOutgoing]);

  const handleLoadMore = useCallback(
    (side: RefsSide) => {
      const state = side === "incoming" ? incoming : outgoing;
      if (state.nextCursor == null || state.loading) return;
      void fetchSide(side, state.nextCursor, true);
    },
    [fetchSide, incoming, outgoing],
  );

  const handleRetry = useCallback(
    (side: RefsSide) => {
      const setState = side === "incoming" ? setIncoming : setOutgoing;
      setState(INITIAL_SIDE);
      void fetchSide(side, null, false);
    },
    [fetchSide],
  );

  return (
    <RefsTabView
      incoming={incoming}
      outgoing={outgoing}
      onLoadMore={handleLoadMore}
      onRetry={handleRetry}
    />
  );
}
