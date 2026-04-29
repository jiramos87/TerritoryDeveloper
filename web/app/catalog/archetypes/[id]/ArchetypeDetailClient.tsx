"use client";

import Link from "next/link";
import { useEffect, useState } from "react";

import RefsTab from "@/components/refs/RefsTab";
import VersionsTab from "@/components/versions/VersionsTab";
import type {
  CatalogArchetype,
  CatalogArchetypeVersionWithPinCount,
} from "@/types/api/catalog-api";

type DetailPayload = {
  ok: "ok" | "error" | true;
  data?: {
    archetype: CatalogArchetype;
    versions: CatalogArchetypeVersionWithPinCount[];
  };
  error?: { code: string; message: string };
};

/**
 * Archetype detail (TECH-2459 / Stage 11.1). Renders current published version
 * params plus version history table with pinned-entity counts (DEC-A46 blast
 * radius preview before retire).
 */
export default function ArchetypeDetailClient({ entityId }: { entityId: string }) {
  const [archetype, setArchetype] = useState<CatalogArchetype | null>(null);
  const [versions, setVersions] = useState<CatalogArchetypeVersionWithPinCount[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    fetch(`/api/catalog/archetypes/by-id/${encodeURIComponent(entityId)}`)
      .then((res) => res.json() as Promise<DetailPayload>)
      .then((payload) => {
        if (cancelled) return;
        if ((payload.ok !== "ok" && payload.ok !== true) || !payload.data) {
          setError(payload.error?.message ?? "Failed to load archetype");
          setArchetype(null);
          setVersions([]);
          setLoading(false);
          return;
        }
        setArchetype(payload.data.archetype);
        setVersions(payload.data.versions);
        setError(null);
        setLoading(false);
      })
      .catch((err: unknown) => {
        if (cancelled) return;
        setError(err instanceof Error ? err.message : "Network error");
        setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [entityId]);

  if (loading) {
    return (
      <p data-testid="archetype-detail-loading" className="text-[var(--ds-text-muted)]">
        Loading archetype...
      </p>
    );
  }
  if (error) {
    return (
      <p
        data-testid="archetype-detail-error"
        role="alert"
        className="text-[var(--ds-text-accent-critical)]"
      >
        {error}
      </p>
    );
  }
  if (!archetype) {
    return (
      <p data-testid="archetype-detail-empty" className="text-[var(--ds-text-muted)]">
        Archetype not found.
      </p>
    );
  }

  return (
    <div
      data-testid="archetype-detail"
      className="flex flex-col gap-[var(--ds-spacing-md)]"
    >
      <header className="flex items-center justify-between">
        <div className="flex flex-col">
          <h1 className="text-[length:var(--ds-font-size-h2)] font-semibold">
            {archetype.display_name}
          </h1>
          <span className="text-[var(--ds-text-muted)]">{archetype.slug}</span>
        </div>
        <div className="flex items-center gap-[var(--ds-spacing-xs)]">
          {archetype.retired_at ? (
            <span
              data-testid="archetype-detail-retired"
              className="rounded bg-[var(--ds-bg-panel)] px-[var(--ds-spacing-xs)] text-[var(--ds-text-accent-warn)]"
            >
              Retired
            </span>
          ) : null}
          <Link
            href={`/catalog/archetypes/${entityId}/version/new`}
            data-testid="archetype-detail-bump-cta"
            className="rounded bg-[var(--ds-text-accent-info)] px-[var(--ds-spacing-md)] py-[var(--ds-spacing-xs)] text-[var(--ds-bg-canvas)]"
          >
            Bump version
          </Link>
        </div>
      </header>

      <section
        data-testid="archetype-detail-current"
        className="flex flex-col gap-[var(--ds-spacing-xs)]"
      >
        <h2 className="text-[length:var(--ds-font-size-h3)] font-semibold">
          Current published version
        </h2>
        {archetype.current_version ? (
          <div className="rounded border border-[var(--ds-border-subtle)] p-[var(--ds-spacing-sm)]">
            <div className="flex items-center justify-between">
              <span className="font-medium">
                v{archetype.current_version.version_number}
              </span>
              <span className="text-[var(--ds-text-muted)]">
                {archetype.current_version.status}
              </span>
            </div>
            <pre
              data-testid="archetype-detail-current-params"
              className="mt-[var(--ds-spacing-xs)] overflow-auto rounded bg-[var(--ds-bg-panel)] p-[var(--ds-spacing-xs)] text-[length:var(--ds-font-size-sm)]"
            >
              {JSON.stringify(archetype.current_version.params_json, null, 2)}
            </pre>
          </div>
        ) : (
          <p
            data-testid="archetype-detail-no-published"
            className="text-[var(--ds-text-muted)]"
          >
            No published version yet — only draft.
          </p>
        )}
      </section>

      <section
        data-testid="archetype-detail-versions"
        className="flex flex-col gap-[var(--ds-spacing-xs)]"
      >
        <h2 className="text-[length:var(--ds-font-size-h3)] font-semibold">
          Version history
        </h2>
        <table className="w-full border-collapse">
          <thead>
            <tr className="border-b border-[var(--ds-border-subtle)] text-left">
              <th className="py-[var(--ds-spacing-xs)]">Version</th>
              <th className="py-[var(--ds-spacing-xs)]">Status</th>
              <th className="py-[var(--ds-spacing-xs)]">Pinned entities</th>
              <th className="py-[var(--ds-spacing-xs)]">Updated</th>
            </tr>
          </thead>
          <tbody>
            {versions.map((v) => (
              <tr
                key={v.version_id}
                data-testid={`archetype-detail-version-${v.version_number}`}
                className="border-b border-[var(--ds-border-subtle)]"
              >
                <td className="py-[var(--ds-spacing-xs)]">v{v.version_number}</td>
                <td className="py-[var(--ds-spacing-xs)]">{v.status}</td>
                <td
                  className="py-[var(--ds-spacing-xs)]"
                  data-testid={`archetype-detail-version-${v.version_number}-pin-count`}
                >
                  {v.pinned_entity_count}
                </td>
                <td className="py-[var(--ds-spacing-xs)] text-[var(--ds-text-muted)]">
                  {new Date(v.updated_at).toLocaleString()}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {versions.length === 0 ? (
          <p
            data-testid="archetype-detail-versions-empty"
            className="text-[var(--ds-text-muted)]"
          >
            No versions yet.
          </p>
        ) : null}
      </section>

      <VersionsTab entityId={entityId} kind="archetype" />
      <RefsTab entityId={entityId} kind="archetype" />
    </div>
  );
}
