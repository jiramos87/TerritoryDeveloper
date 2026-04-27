"use client";

import { useEffect, useState } from "react";

import PoolList, {
  type PoolListFilter,
  type PoolListRow,
} from "@/components/catalog/PoolList";

type ApiPayload = {
  ok: "ok" | "error" | true;
  data?: {
    items: Array<{
      entity_id: string;
      slug: string;
      display_name: string;
      owner_category: string | null;
      member_count: number;
      retired_at: string | null;
      updated_at: string;
    }>;
    next_cursor?: string | null;
  };
  error?: { code: string; message: string };
};

/** Spine-aware pools catalog list (TECH-1788). */
export default function PoolsCatalogPage() {
  const [filter, setFilter] = useState<PoolListFilter>("active");
  const [rows, setRows] = useState<PoolListRow[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    fetch(`/api/catalog/pools?status=${filter}&limit=50`)
      .then((res) => res.json() as Promise<ApiPayload>)
      .then((payload) => {
        if (cancelled) return;
        if ((payload.ok !== "ok" && payload.ok !== true) || !payload.data) {
          setError(payload.error?.message ?? "Failed to load pools");
          setRows([]);
          setLoading(false);
          return;
        }
        const next: PoolListRow[] = payload.data.items.map((it) => ({
          entity_id: it.entity_id,
          slug: it.slug,
          display_name: it.display_name,
          owner_category: it.owner_category,
          member_count: it.member_count,
          status: it.retired_at ? "retired" : "active",
          updated_at: it.updated_at,
        }));
        setRows(next);
        setError(null);
        setLoading(false);
      })
      .catch((err: unknown) => {
        if (cancelled) return;
        setError(err instanceof Error ? err.message : "Network error");
        setRows([]);
        setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [filter]);

  return (
    <PoolList
      rows={rows}
      filter={filter}
      onFilterChange={setFilter}
      loading={loading}
      error={error}
    />
  );
}
