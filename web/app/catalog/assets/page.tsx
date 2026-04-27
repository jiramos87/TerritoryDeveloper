"use client";

import { useEffect, useState } from "react";

import AssetList, {
  type AssetListFilter,
  type AssetListRow,
} from "@/components/catalog/AssetList";

type ApiPayload = {
  ok: "ok" | "error" | true;
  data?: {
    items: Array<{
      entity_id: string;
      slug: string;
      display_name: string;
      category: string | null;
      retired_at: string | null;
      updated_at: string;
    }>;
    next_cursor?: string | null;
  };
  error?: { code: string; message: string };
};

/** Spine-aware assets catalog list (TECH-1786). */
export default function AssetsCatalogPage() {
  const [filter, setFilter] = useState<AssetListFilter>("active");
  const [rows, setRows] = useState<AssetListRow[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    fetch(`/api/catalog/assets-spine?status=${filter}&limit=50`)
      .then((res) => res.json() as Promise<ApiPayload>)
      .then((payload) => {
        if (cancelled) return;
        if ((payload.ok !== "ok" && payload.ok !== true) || !payload.data) {
          setError(payload.error?.message ?? "Failed to load assets");
          setRows([]);
          setLoading(false);
          return;
        }
        const next: AssetListRow[] = payload.data.items.map((it) => ({
          entity_id: it.entity_id,
          slug: it.slug,
          display_name: it.display_name,
          category: it.category,
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
    <AssetList
      rows={rows}
      filter={filter}
      onFilterChange={setFilter}
      loading={loading}
      error={error}
    />
  );
}
