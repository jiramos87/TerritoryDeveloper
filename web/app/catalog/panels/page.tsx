"use client";

import { useEffect, useState } from "react";

import PanelList, {
  type PanelListFilter,
  type PanelListRow,
} from "@/components/catalog/PanelList";

type ApiPayload = {
  ok: "ok" | "error" | true;
  data?: {
    items: Array<{
      entity_id: string;
      slug: string;
      display_name: string;
      archetype_entity_id: string | null;
      child_count: number;
      retired_at: string | null;
      updated_at: string;
    }>;
    next_cursor?: string | null;
  };
  error?: { code: string; message: string };
};

/** Spine-aware panels catalog list (TECH-1886 / Stage 8.1). */
export default function PanelsCatalogPage() {
  const [filter, setFilter] = useState<PanelListFilter>("active");
  const [rows, setRows] = useState<PanelListRow[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    fetch(`/api/catalog/panels?status=${filter}&limit=50`)
      .then((res) => res.json() as Promise<ApiPayload>)
      .then((payload) => {
        if (cancelled) return;
        if ((payload.ok !== "ok" && payload.ok !== true) || !payload.data) {
          setError(payload.error?.message ?? "Failed to load panels");
          setRows([]);
          setLoading(false);
          return;
        }
        const next: PanelListRow[] = payload.data.items.map((it) => ({
          entity_id: it.entity_id,
          slug: it.slug,
          display_name: it.display_name,
          archetype_entity_id: it.archetype_entity_id,
          child_count: it.child_count,
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
    <PanelList
      rows={rows}
      filter={filter}
      onFilterChange={setFilter}
      loading={loading}
      error={error}
    />
  );
}
