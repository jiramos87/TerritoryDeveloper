"use client";

import { useEffect, useState } from "react";

/**
 * Pinned-entity preview for archetype version bump (TECH-2461).
 * Fetches `/api/catalog/archetypes/[slug]/version/[versionId]/pin-count` on mount.
 */
export type PinCountPreviewProps = {
  slug: string;
  versionId: string;
};

type ApiPayload = {
  ok: "ok" | "error" | true;
  data?: { count: number };
  error?: { code: string; message: string };
};

export default function PinCountPreview({ slug, versionId }: PinCountPreviewProps) {
  const [count, setCount] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    fetch(
      `/api/catalog/archetypes/${encodeURIComponent(slug)}/versions/${encodeURIComponent(versionId)}/pin-count`,
    )
      .then((res) => res.json() as Promise<ApiPayload>)
      .then((payload) => {
        if (cancelled) return;
        if ((payload.ok !== "ok" && payload.ok !== true) || !payload.data) {
          setError(payload.error?.message ?? "Failed to load pin count");
          return;
        }
        setCount(payload.data.count);
        setError(null);
      })
      .catch((err: unknown) => {
        if (cancelled) return;
        setError(err instanceof Error ? err.message : "Network error");
      });
    return () => {
      cancelled = true;
    };
  }, [slug, versionId]);

  if (error) {
    return (
      <p
        data-testid="pin-count-preview-error"
        role="alert"
        className="text-[var(--ds-text-accent-critical)]"
      >
        {error}
      </p>
    );
  }
  if (count == null) {
    return (
      <p data-testid="pin-count-preview-loading" className="text-[var(--ds-text-muted)]">
        Loading pinned count...
      </p>
    );
  }
  return (
    <p data-testid="pin-count-preview" className="text-[var(--ds-text-muted)]">
      {count} pinned {count === 1 ? "entity" : "entities"} on the parent version.
    </p>
  );
}
