"use client";

import { useEffect, useState } from "react";

import SpriteList, {
  type SpriteListFilter,
  type SpriteListRow,
} from "@/components/catalog/SpriteList";

type ApiPayload = {
  ok: "ok" | "error";
  data?: {
    items: Array<{
      entity_id: string;
      slug: string;
      display_name: string;
      retired_at: string | null;
      updated_at: string;
    }>;
    next_cursor?: string | null;
  };
  error?: { code: string; message: string };
};

/** Sprites catalog list (TECH-1672). */
export default function SpritesCatalogPage() {
  const [filter, setFilter] = useState<SpriteListFilter>("active");
  const [rows, setRows] = useState<SpriteListRow[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    fetch(`/api/catalog/sprites?status=${filter}&limit=50`)
      .then((res) => res.json() as Promise<ApiPayload>)
      .then((payload) => {
        if (cancelled) return;
        if (payload.ok !== "ok" || !payload.data) {
          setError(payload.error?.message ?? "Failed to load sprites");
          setRows([]);
          setLoading(false);
          return;
        }
        const next: SpriteListRow[] = payload.data.items.map((it) => ({
          entity_id: it.entity_id,
          slug: it.slug,
          display_name: it.display_name,
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
    <SpriteList
      rows={rows}
      filter={filter}
      onFilterChange={setFilter}
      loading={loading}
      error={error}
    />
  );
}
