"use client";

import { useEffect, useState } from "react";

/**
 * Token ripple banner (TECH-2093 / Stage 10.1).
 *
 * Renders the "Editing this changes N entities" row above token detail surfaces
 * per DEC-A44. Banner is present regardless of count value (Acceptance row 6 of
 * TECH-2093). N comes from GET `/api/catalog/tokens/[slug]/ripple-count`. On
 * Stage 10.1 baseline `catalog_ref_edge` is not materialized so the route stub
 * returns `{count: 0}`; banner shape is forward-compatible — Stage 14.1 will
 * flip the count without touching this component.
 *
 * @see ia/projects/asset-pipeline/stage-10.1 — TECH-2093 §Plan Digest
 */

export type RippleBannerProps = {
  slug: string;
};

type Payload = {
  ok?: unknown;
  data?: { count?: number };
  error?: { code: string; message: string };
};

export default function RippleBanner({ slug }: RippleBannerProps) {
  const [count, setCount] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    fetch(`/api/catalog/tokens/${slug}/ripple-count`)
      .then((res) => res.json() as Promise<Payload>)
      .then((payload) => {
        if (cancelled) return;
        if (typeof payload?.data?.count !== "number") {
          setError(payload?.error?.message ?? "Ripple count unavailable");
          setCount(null);
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
  }, [slug]);

  return (
    <div
      data-testid="ripple-banner"
      role="status"
      className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] p-[var(--ds-spacing-sm)] text-[var(--ds-text-muted)]"
    >
      {error ? (
        <span data-testid="ripple-banner-error">{error}</span>
      ) : count === null ? (
        <span data-testid="ripple-banner-loading">Computing impact…</span>
      ) : (
        <span data-testid="ripple-banner-count">
          Editing this changes <strong data-testid="ripple-banner-n">{count}</strong> entities.
        </span>
      )}
    </div>
  );
}
