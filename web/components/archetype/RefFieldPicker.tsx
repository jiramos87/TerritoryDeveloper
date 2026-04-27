"use client";

import { useEffect, useState } from "react";

/**
 * Entity ref-picker for archetype `params_json` `ref` field default (TECH-2460).
 * Browses `catalog_entity` filtered by allowed kinds; emits selected entity_id.
 */

export type RefFieldPickerProps = {
  allowedKinds: ReadonlyArray<string>;
  value: string | null;
  onChange: (next: string | null) => void;
  disabled?: boolean;
};

type EntityRow = {
  entity_id: string;
  slug: string;
  display_name: string;
  kind: string;
};

type ApiPayload = {
  ok: "ok" | "error" | true;
  data?: { items: EntityRow[] };
  error?: { code: string; message: string };
};

export default function RefFieldPicker({
  allowedKinds,
  value,
  onChange,
  disabled,
}: RefFieldPickerProps) {
  const [rows, setRows] = useState<EntityRow[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    const params = new URLSearchParams();
    for (const k of allowedKinds) params.append("kind", k);
    fetch(`/api/catalog/entity-search?${params.toString()}`)
      .then((res) => res.json() as Promise<ApiPayload>)
      .then((payload) => {
        if (cancelled) return;
        if ((payload.ok !== "ok" && payload.ok !== true) || !payload.data) {
          setError(payload.error?.message ?? "Failed to load entities");
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
        setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [allowedKinds]);

  return (
    <div data-testid="ref-field-picker" className="flex flex-col gap-[var(--ds-spacing-2xs)]">
      {error ? (
        <p
          role="alert"
          className="text-[var(--ds-text-accent-critical)]"
          data-testid="ref-field-picker-error"
        >
          {error}
        </p>
      ) : null}
      <select
        data-testid="ref-field-picker-select"
        disabled={disabled || loading}
        value={value ?? ""}
        onChange={(e) => onChange(e.target.value === "" ? null : e.target.value)}
        className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-canvas)] px-[var(--ds-spacing-xs)] py-[var(--ds-spacing-2xs)]"
      >
        <option value="">— none —</option>
        {rows.map((r) => (
          <option key={r.entity_id} value={r.entity_id}>
            {r.display_name} ({r.kind} / {r.slug})
          </option>
        ))}
      </select>
    </div>
  );
}
