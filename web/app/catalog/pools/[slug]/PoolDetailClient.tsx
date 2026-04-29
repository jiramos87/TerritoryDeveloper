"use client";

import { useEffect, useState } from "react";

import PoolDetail from "@/components/catalog/PoolDetail";
import RefsTab from "@/components/refs/RefsTab";
import VersionsTab from "@/components/versions/VersionsTab";
import type { CatalogPoolDto, CatalogPoolPatchBody } from "@/types/api/catalog-api";

type DetailApiPayload = {
  ok: "ok" | "error" | true;
  data?: CatalogPoolDto;
  error?: { code: string; message: string };
};

/** Spine pool detail client (TECH-1788). Fetch GET, render `<PoolDetail>`, wire PATCH. */
export default function PoolDetailClient({ slug }: { slug: string }) {
  const [pool, setPool] = useState<CatalogPoolDto | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [saveError, setSaveError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    fetch(`/api/catalog/pools/${slug}`)
      .then((res) => res.json() as Promise<DetailApiPayload>)
      .then((payload) => {
        if (cancelled) return;
        if ((payload.ok !== "ok" && payload.ok !== true) || !payload.data) {
          setLoadError(payload.error?.message ?? "Pool not found");
          setPool(null);
          setLoading(false);
          return;
        }
        setPool(payload.data);
        setLoadError(null);
        setLoading(false);
      })
      .catch((err: unknown) => {
        if (cancelled) return;
        setLoadError(err instanceof Error ? err.message : "Network error");
        setPool(null);
        setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [slug]);

  function handleSave(patch: CatalogPoolPatchBody) {
    setSaveError(null);
    fetch(`/api/catalog/pools/${slug}`, {
      method: "PATCH",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(patch),
    })
      .then((res) => res.json() as Promise<DetailApiPayload>)
      .then((payload) => {
        if ((payload.ok !== "ok" && payload.ok !== true) || !payload.data) {
          setSaveError(payload.error?.message ?? "Save failed");
          return;
        }
        setPool(payload.data);
      })
      .catch((err: unknown) => {
        setSaveError(err instanceof Error ? err.message : "Network error");
      });
  }

  if (loading) {
    return (
      <p data-testid="pool-detail-loading" className="text-[var(--ds-text-muted)]">
        Loading pool…
      </p>
    );
  }
  if (loadError || !pool) {
    return (
      <p data-testid="pool-detail-error" role="alert" className="text-[var(--ds-text-accent-critical)]">
        {loadError ?? "Pool not found"}
      </p>
    );
  }
  return (
    <>
      <PoolDetail pool={pool} onSave={handleSave} saveError={saveError} />
      <VersionsTab entityId={pool.entity_id} kind="pool" />
      <RefsTab entityId={pool.entity_id} kind="pool" />
    </>
  );
}
