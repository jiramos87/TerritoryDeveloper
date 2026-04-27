"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";

import type { CatalogArchetypeKindTag } from "@/types/api/catalog-api";

type ArchetypeListItem = {
  entity_id: string;
  slug: string;
  display_name: string;
  tags: string[];
  retired_at: string | null;
  current_published_version_id: string | null;
  updated_at: string;
  kind_tag: CatalogArchetypeKindTag | string | null;
};

type ApiPayload = {
  ok: "ok" | "error" | true;
  data?: { items: ArchetypeListItem[]; next_cursor?: string | null };
  error?: { code: string; message: string };
};

type ListFilter = "active" | "retired" | "all";

const FILTERS: ReadonlyArray<{ id: ListFilter; label: string }> = [
  { id: "active", label: "Active" },
  { id: "retired", label: "Retired" },
  { id: "all", label: "All" },
];

const KNOWN_KIND_TAGS: ReadonlyArray<CatalogArchetypeKindTag> = [
  "sprite",
  "asset",
  "button",
  "panel",
  "audio",
  "pool",
  "token",
];

/**
 * Archetypes catalog list (TECH-2459 / Stage 11.1). Replaces sentinel.
 * Groups rows by inferred sub-kind tag (`params_json.kind_tag`); unknown tags
 * surface under "Other". Filter chips for active|retired|all per DEC-A23.
 */
export default function ArchetypesCatalogPage() {
  const [filter, setFilter] = useState<ListFilter>("active");
  const [rows, setRows] = useState<ArchetypeListItem[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    fetch(`/api/catalog/archetypes?status=${filter}&limit=200`)
      .then((res) => res.json() as Promise<ApiPayload>)
      .then((payload) => {
        if (cancelled) return;
        if ((payload.ok !== "ok" && payload.ok !== true) || !payload.data) {
          setError(payload.error?.message ?? "Failed to load archetypes");
          setRows([]);
          setLoading(false);
          return;
        }
        setRows(payload.data.items);
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

  const grouped = useMemo(() => groupByKindTag(rows), [rows]);

  return (
    <div
      data-testid="archetype-list"
      className="flex flex-col gap-[var(--ds-spacing-md)]"
    >
      <header className="flex items-center justify-between">
        <h1 className="text-[length:var(--ds-font-size-h2)] font-semibold">Archetypes</h1>
        <Link
          href="/catalog/archetypes/new"
          data-testid="archetype-list-create-cta"
          className="rounded bg-[var(--ds-text-accent-info)] px-[var(--ds-spacing-md)] py-[var(--ds-spacing-xs)] text-[var(--ds-bg-canvas)]"
        >
          Create archetype
        </Link>
      </header>

      <div
        data-testid="archetype-list-filter-chips"
        role="tablist"
        className="flex gap-[var(--ds-spacing-xs)]"
      >
        {FILTERS.map((f) => {
          const active = f.id === filter;
          return (
            <button
              key={f.id}
              type="button"
              role="tab"
              aria-selected={active}
              data-testid={`archetype-list-filter-${f.id}`}
              onClick={() => setFilter(f.id)}
              className={
                active
                  ? "rounded border border-[var(--ds-text-accent-info)] bg-[var(--ds-bg-panel)] px-[var(--ds-spacing-sm)] py-[var(--ds-spacing-xs)] text-[var(--ds-text-primary)]"
                  : "rounded border border-[var(--ds-border-subtle)] px-[var(--ds-spacing-sm)] py-[var(--ds-spacing-xs)] text-[var(--ds-text-muted)]"
              }
            >
              {f.label}
            </button>
          );
        })}
      </div>

      {error ? (
        <p
          data-testid="archetype-list-error"
          role="alert"
          className="text-[var(--ds-text-accent-critical)]"
        >
          {error}
        </p>
      ) : null}

      {loading ? (
        <p data-testid="archetype-list-loading" className="text-[var(--ds-text-muted)]">
          Loading archetypes...
        </p>
      ) : null}

      {!loading && rows.length === 0 && !error ? (
        <p data-testid="archetype-list-empty" className="text-[var(--ds-text-muted)]">
          No archetypes match this filter.
        </p>
      ) : null}

      {grouped.map(({ tag, items }) => (
        <section
          key={tag}
          data-testid={`archetype-list-group-${tag}`}
          className="flex flex-col gap-[var(--ds-spacing-xs)]"
        >
          <h2 className="text-[length:var(--ds-font-size-h3)] font-semibold capitalize">
            {tag}
          </h2>
          <ul className="flex flex-col gap-[var(--ds-spacing-2xs)]">
            {items.map((row) => (
              <li
                key={row.entity_id}
                data-testid={`archetype-list-row-${row.slug}`}
                className="flex items-center justify-between rounded border border-[var(--ds-border-subtle)] px-[var(--ds-spacing-sm)] py-[var(--ds-spacing-xs)]"
              >
                <Link
                  href={`/catalog/archetypes/${row.entity_id}`}
                  className="flex flex-col"
                >
                  <span className="font-medium text-[var(--ds-text-primary)]">
                    {row.display_name}
                  </span>
                  <span className="text-[var(--ds-text-muted)]">{row.slug}</span>
                </Link>
                <div className="flex items-center gap-[var(--ds-spacing-xs)]">
                  {row.current_published_version_id ? (
                    <span
                      data-testid={`archetype-list-row-${row.slug}-published`}
                      className="rounded bg-[var(--ds-bg-panel)] px-[var(--ds-spacing-xs)] text-[var(--ds-text-accent-info)]"
                    >
                      Published
                    </span>
                  ) : (
                    <span
                      data-testid={`archetype-list-row-${row.slug}-draft`}
                      className="rounded bg-[var(--ds-bg-panel)] px-[var(--ds-spacing-xs)] text-[var(--ds-text-muted)]"
                    >
                      Draft only
                    </span>
                  )}
                  {row.retired_at ? (
                    <span
                      data-testid={`archetype-list-row-${row.slug}-retired`}
                      className="rounded bg-[var(--ds-bg-panel)] px-[var(--ds-spacing-xs)] text-[var(--ds-text-accent-warn)]"
                    >
                      Retired
                    </span>
                  ) : null}
                </div>
              </li>
            ))}
          </ul>
        </section>
      ))}
    </div>
  );
}

function groupByKindTag(
  items: ArchetypeListItem[],
): Array<{ tag: string; items: ArchetypeListItem[] }> {
  const buckets = new Map<string, ArchetypeListItem[]>();
  for (const tag of KNOWN_KIND_TAGS) buckets.set(tag, []);
  buckets.set("other", []);
  for (const it of items) {
    const t = typeof it.kind_tag === "string" && it.kind_tag.length > 0 ? it.kind_tag : "other";
    const bucket = buckets.get(t) ?? buckets.get("other")!;
    bucket.push(it);
  }
  return Array.from(buckets.entries())
    .filter(([, list]) => list.length > 0)
    .map(([tag, list]) => ({ tag, items: list }));
}
