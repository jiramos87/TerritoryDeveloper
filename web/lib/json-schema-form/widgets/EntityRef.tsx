"use client";

import { useEffect, useState } from "react";

import type { WidgetProps } from "../registry";

type LookupItem = { slug: string; label?: string };

/**
 * Entity-reference typeahead — consumes `hint.kind` to scope
 * `/api/catalog/{kind}` lookups. MVP renders a debounced text input + suggestion
 * list; networking happens via fetch when `hint.kind` provided. When `hint.kind`
 * is absent, falls back to a plain text input (still emits `onChange`).
 */
export default function EntityRefWidget(props: WidgetProps) {
  const { hint, value, path, onChange } = props;
  const kind = hint?.kind;
  const [query, setQuery] = useState(typeof value === "string" ? value : "");
  const [items, setItems] = useState<LookupItem[]>([]);

  useEffect(() => {
    if (!kind || query.length < 2) return;
    let cancelled = false;
    const id = setTimeout(() => {
      fetch(`/api/catalog/${kind}?q=${encodeURIComponent(query)}&limit=10`)
        .then((res) => (res.ok ? res.json() : { ok: "error" }))
        .then((payload) => {
          if (cancelled) return;
          const list = (payload?.data?.items ?? []) as LookupItem[];
          setItems(list);
        })
        .catch(() => {
          /* swallow — items list stays as last result */
        });
    }, 200);
    return () => {
      cancelled = true;
      clearTimeout(id);
    };
  }, [kind, query]);

  // When kind/query becomes invalid, drop suggestions via render-time derivation.
  const visibleItems = !kind || query.length < 2 ? [] : items;

  return (
    <span data-testid={`jsf-entity-ref-${path}`} data-widget="entity-ref" data-kind={kind ?? ""}>
      <input
        type="text"
        list={`jsf-entity-ref-list-${path}`}
        value={query}
        placeholder={kind ? `Search ${kind}…` : "Slug"}
        onChange={(e) => {
          const next = e.currentTarget.value;
          setQuery(next);
          onChange(next);
        }}
      />
      <datalist id={`jsf-entity-ref-list-${path}`}>
        {visibleItems.map((item) => (
          <option key={item.slug} value={item.slug}>
            {item.label ?? item.slug}
          </option>
        ))}
      </datalist>
    </span>
  );
}
