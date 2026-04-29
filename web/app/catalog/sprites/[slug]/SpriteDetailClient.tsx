"use client";

import { useEffect, useState } from "react";

import SpriteDetail, { type SpriteDetailView } from "@/components/catalog/SpriteDetail";
import type { SpriteEditFormValue } from "@/components/catalog/SpriteEditForm";
import RefsTab from "@/components/refs/RefsTab";
import VersionsTab from "@/components/versions/VersionsTab";

type DetailApiPayload = {
  ok: "ok" | "error";
  data?: {
    entity_id: string;
    slug: string;
    display_name: string;
    tags: string[];
    retired_at: string | null;
    current_published_version_id: string | null;
    pixels_per_unit: number | null;
    pivot_x: number | null;
    pivot_y: number | null;
    source_uri: string | null;
  };
  error?: { code: string; message: string };
};

export default function SpriteDetailClient({ slug }: { slug: string }) {
  const [sprite, setSprite] = useState<SpriteDetailView | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [saveError, setSaveError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    fetch(`/api/catalog/sprites/${slug}`)
      .then((res) => res.json() as Promise<DetailApiPayload>)
      .then((payload) => {
        if (cancelled) return;
        if (payload.ok !== "ok" || !payload.data) {
          setLoadError(payload.error?.message ?? "Sprite not found");
          setSprite(null);
          setLoading(false);
          return;
        }
        const d = payload.data;
        setSprite({
          entity_id: d.entity_id,
          slug: d.slug,
          display_name: d.display_name,
          tags: d.tags,
          retired_at: d.retired_at,
          current_published_version_id: d.current_published_version_id,
          sprite_detail: {
            pixels_per_unit: d.pixels_per_unit ?? 16,
            pivot_x: d.pivot_x ?? 0.5,
            pivot_y: d.pivot_y ?? 0.5,
            source_uri: d.source_uri,
          },
        });
        setLoadError(null);
        setLoading(false);
      })
      .catch((err: unknown) => {
        if (cancelled) return;
        setLoadError(err instanceof Error ? err.message : "Network error");
        setSprite(null);
        setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [slug]);

  function handleSave(patch: {
    display_name: string;
    tags: string[];
    sprite_detail: SpriteEditFormValue["sprite_detail"];
  }) {
    setSaveError(null);
    fetch(`/api/catalog/sprites/${slug}`, {
      method: "PATCH",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(patch),
    })
      .then((res) => res.json())
      .then((payload: DetailApiPayload) => {
        if (payload.ok !== "ok" || !payload.data) {
          setSaveError(payload.error?.message ?? "Save failed");
          return;
        }
        const d = payload.data;
        setSprite({
          entity_id: d.entity_id,
          slug: d.slug,
          display_name: d.display_name,
          tags: d.tags,
          retired_at: d.retired_at,
          current_published_version_id: d.current_published_version_id,
          sprite_detail: {
            pixels_per_unit: d.pixels_per_unit ?? 16,
            pivot_x: d.pivot_x ?? 0.5,
            pivot_y: d.pivot_y ?? 0.5,
            source_uri: d.source_uri,
          },
        });
      })
      .catch((err: unknown) => {
        setSaveError(err instanceof Error ? err.message : "Network error");
      });
  }

  if (loading) {
    return (
      <p data-testid="sprite-detail-loading" className="text-[var(--ds-text-muted)]">
        Loading sprite…
      </p>
    );
  }
  if (loadError || !sprite) {
    return (
      <p data-testid="sprite-detail-error" role="alert" className="text-[var(--ds-text-accent-critical)]">
        {loadError ?? "Sprite not found"}
      </p>
    );
  }
  return (
    <>
      <SpriteDetail sprite={sprite} onSave={handleSave} saveError={saveError} />
      <VersionsTab entityId={sprite.entity_id} kind="sprite" />
      <RefsTab entityId={sprite.entity_id} kind="sprite" />
    </>
  );
}
