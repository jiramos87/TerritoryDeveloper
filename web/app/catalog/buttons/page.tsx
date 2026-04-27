"use client";

import { useEffect, useState } from "react";

import ButtonList, {
  type ButtonListFilter,
  type ButtonListRow,
} from "@/components/catalog/ButtonList";

type ApiPayload = {
  ok: "ok" | "error" | true;
  data?: {
    items: Array<{
      entity_id: string;
      slug: string;
      display_name: string;
      size_variant: string | null;
      action_id: string | null;
      retired_at: string | null;
      updated_at: string;
    }>;
    next_cursor?: string | null;
  };
  error?: { code: string; message: string };
};

/** Spine-aware buttons catalog list (TECH-1885 / Stage 8.1). */
export default function ButtonsCatalogPage() {
  const [filter, setFilter] = useState<ButtonListFilter>("active");
  const [rows, setRows] = useState<ButtonListRow[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    fetch(`/api/catalog/buttons?status=${filter}&limit=50`)
      .then((res) => res.json() as Promise<ApiPayload>)
      .then((payload) => {
        if (cancelled) return;
        if ((payload.ok !== "ok" && payload.ok !== true) || !payload.data) {
          setError(payload.error?.message ?? "Failed to load buttons");
          setRows([]);
          setLoading(false);
          return;
        }
        const next: ButtonListRow[] = payload.data.items.map((it) => ({
          entity_id: it.entity_id,
          slug: it.slug,
          display_name: it.display_name,
          size_variant: it.size_variant,
          action_id: it.action_id,
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
    <ButtonList
      rows={rows}
      filter={filter}
      onFilterChange={setFilter}
      loading={loading}
      error={error}
    />
  );
}
