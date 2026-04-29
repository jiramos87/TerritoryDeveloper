"use client";

import { useEffect, useState } from "react";

import AssetDetail from "@/components/catalog/AssetDetail";
import RefsTab from "@/components/refs/RefsTab";
import VersionsTab from "@/components/versions/VersionsTab";
import type { CatalogAssetSpineDto, CatalogAssetSpinePatchBody } from "@/types/api/catalog-api";

type DetailApiPayload = {
  ok: "ok" | "error" | true;
  data?: CatalogAssetSpineDto;
  error?: { code: string; message: string };
};

/** Spine asset detail client (TECH-1786). Fetch GET, render `<AssetDetail>`, wire PATCH. */
export default function AssetDetailClient({ slug }: { slug: string }) {
  const [asset, setAsset] = useState<CatalogAssetSpineDto | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [saveError, setSaveError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    fetch(`/api/catalog/assets-spine/${slug}`)
      .then((res) => res.json() as Promise<DetailApiPayload>)
      .then((payload) => {
        if (cancelled) return;
        if ((payload.ok !== "ok" && payload.ok !== true) || !payload.data) {
          setLoadError(payload.error?.message ?? "Asset not found");
          setAsset(null);
          setLoading(false);
          return;
        }
        setAsset(payload.data);
        setLoadError(null);
        setLoading(false);
      })
      .catch((err: unknown) => {
        if (cancelled) return;
        setLoadError(err instanceof Error ? err.message : "Network error");
        setAsset(null);
        setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [slug]);

  function handleSave(patch: CatalogAssetSpinePatchBody) {
    setSaveError(null);
    fetch(`/api/catalog/assets-spine/${slug}`, {
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
        setAsset(payload.data);
      })
      .catch((err: unknown) => {
        setSaveError(err instanceof Error ? err.message : "Network error");
      });
  }

  if (loading) {
    return (
      <p data-testid="asset-detail-loading" className="text-[var(--ds-text-muted)]">
        Loading asset…
      </p>
    );
  }
  if (loadError || !asset) {
    return (
      <p data-testid="asset-detail-error" role="alert" className="text-[var(--ds-text-accent-critical)]">
        {loadError ?? "Asset not found"}
      </p>
    );
  }
  return (
    <>
      <AssetDetail asset={asset} onSave={handleSave} saveError={saveError} />
      <VersionsTab entityId={asset.entity_id} kind="asset" />
      <RefsTab entityId={asset.entity_id} kind="asset" />
    </>
  );
}
