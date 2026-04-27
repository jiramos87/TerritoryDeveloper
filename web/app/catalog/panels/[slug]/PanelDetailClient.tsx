"use client";

import { useEffect, useMemo, useState } from "react";

import PanelSlotColumn from "@/components/catalog/PanelSlotColumn";
import { type PanelChildRowState } from "@/components/catalog/PanelChildRow";
import type { EntityRefRow } from "@/components/catalog/EntityRefPicker";
import { slotOrder } from "@/lib/catalog/panel-slots-schema";
import type {
  CatalogPanelChildKind,
  CatalogPanelChildSetBody,
  CatalogPanelDto,
  CatalogPanelSlotSchemaEntry,
} from "@/types/api/catalog-api";

/**
 * Spine panel detail (TECH-1886 / Stage 8.1).
 *
 * Renders one column per archetype-declared slot per DEC-A27. Save POSTs the
 * full child tree to `/api/catalog/panels/[slug]/children` (DEC-A43 atomic
 * replace). Surfaces DEC-A48 envelope errors inline; cycle / count / kind
 * mismatches highlight the affected slot column red.
 */

type DetailApiPayload = {
  ok: "ok" | "error" | true;
  data?: CatalogPanelDto;
  error?: { code: string; message: string };
};

type ChildSetSuccess = {
  ok: true;
  data: { entity_id: string; rows_written: number; updated_at: string };
  audit_id: string | null;
};

type ChildSetError = {
  ok?: undefined;
  error: string;
  code: string;
  details?: { code?: string; details?: { slot_name?: string } } | null;
  current?: unknown;
};

type ChildSetPayload = ChildSetSuccess | ChildSetError;

export type PanelDetailSlotState = {
  name: string;
  schema: CatalogPanelSlotSchemaEntry | null;
  children: PanelChildRowState[];
};

type SlotState = PanelDetailSlotState;

/**
 * Build the DEC-A43 atomic-replace body from current panel + slot state.
 *
 * Exported for test coverage; mirrors `handleSave`'s in-flight body build.
 */
export function buildPanelChildSetBody(
  panel: CatalogPanelDto,
  slots: SlotState[],
): CatalogPanelChildSetBody {
  return {
    updated_at: panel.updated_at,
    slots: slots.map((s) => ({
      name: s.name,
      children: s.children.map((c, i) => ({
        child_entity_id: c.child_entity_id,
        child_kind: c.child_kind,
        order_idx: i,
        params_json: c.params_json,
      })),
    })),
  };
}

export default function PanelDetailClient({ slug }: { slug: string }) {
  const [panel, setPanel] = useState<CatalogPanelDto | null>(null);
  const [slots, setSlots] = useState<SlotState[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [errorSlot, setErrorSlot] = useState<string | null>(null);
  const [saving, setSaving] = useState<boolean>(false);

  useEffect(() => {
    let cancelled = false;
    fetch(`/api/catalog/panels/${slug}`)
      .then((res) => res.json() as Promise<DetailApiPayload>)
      .then((payload) => {
        if (cancelled) return;
        if ((payload.ok !== "ok" && payload.ok !== true) || !payload.data) {
          setLoadError(payload.error?.message ?? "Panel not found");
          setPanel(null);
          setLoading(false);
          return;
        }
        applyDto(payload.data);
        setLoadError(null);
        setLoading(false);
      })
      .catch((err: unknown) => {
        if (cancelled) return;
        setLoadError(err instanceof Error ? err.message : "Network error");
        setPanel(null);
        setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [slug]);

  function applyDto(dto: CatalogPanelDto) {
    setPanel(dto);
    const schema = dto.panel_detail?.slots_schema ?? null;
    const childSlotNames = dto.slots.map((s) => s.name);
    const order = slotOrder(schema, childSlotNames);
    const next: SlotState[] = order.map((name) => {
      const fromDto = dto.slots.find((s) => s.name === name);
      const slotSchema =
        fromDto?.schema ?? (schema != null ? schema[name] ?? null : null);
      const children: PanelChildRowState[] =
        fromDto?.children.map((c) => ({
          child_entity_id: c.child_entity_id,
          child_kind: c.child_kind,
          order_idx: c.order_idx,
          params_json: c.params_json,
          resolved: c.resolved
            ? { slug: c.resolved.slug, display_name: c.resolved.display_name }
            : null,
        })) ?? [];
      return { name, schema: slotSchema, children };
    });
    setSlots(next);
    setSaveError(null);
    setErrorSlot(null);
  }

  const totalChildren = useMemo(
    () => slots.reduce((sum, s) => sum + s.children.length, 0),
    [slots],
  );

  function updateSlot(slotName: string, mutate: (next: PanelChildRowState[]) => PanelChildRowState[]) {
    setSlots((prev) =>
      prev.map((s) =>
        s.name === slotName ? { ...s, children: mutate(s.children) } : s,
      ),
    );
  }

  function handleAddChild(
    slotName: string,
    childEntityId: string | null,
    childKindStr: string,
    row: EntityRefRow | null,
  ) {
    updateSlot(slotName, (cur) => {
      const order_idx = cur.length;
      const next: PanelChildRowState = {
        child_entity_id: childEntityId,
        child_kind: childKindStr as CatalogPanelChildKind,
        order_idx,
        params_json: {},
        resolved: row ? { slug: row.slug, display_name: row.display_name } : null,
      };
      return [...cur, next];
    });
  }

  function handleMoveChild(slotName: string, index: number, dir: -1 | 1) {
    updateSlot(slotName, (cur) => {
      const next = [...cur];
      const target = index + dir;
      if (target < 0 || target >= next.length) return cur;
      [next[index]!, next[target]!] = [next[target]!, next[index]!];
      return next.map((c, i) => ({ ...c, order_idx: i }));
    });
  }

  function handleDeleteChild(slotName: string, index: number) {
    updateSlot(slotName, (cur) => {
      const next = cur.filter((_, i) => i !== index);
      return next.map((c, i) => ({ ...c, order_idx: i }));
    });
  }

  function handleParamsChange(
    slotName: string,
    index: number,
    next: Record<string, unknown>,
  ) {
    updateSlot(slotName, (cur) =>
      cur.map((c, i) => (i === index ? { ...c, params_json: next } : c)),
    );
  }

  function buildBody(): CatalogPanelChildSetBody | null {
    if (panel == null) return null;
    return buildPanelChildSetBody(panel, slots);
  }

  function handleSave() {
    if (panel == null) return;
    const body = buildBody();
    if (body == null) return;
    setSaving(true);
    setSaveError(null);
    setErrorSlot(null);
    fetch(`/api/catalog/panels/${slug}/children`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    })
      .then((res) => res.json() as Promise<ChildSetPayload>)
      .then((payload) => {
        setSaving(false);
        if (payload.ok === true) {
          // Re-fetch to refresh updated_at + resolutions.
          return fetch(`/api/catalog/panels/${slug}`)
            .then((res) => res.json() as Promise<DetailApiPayload>)
            .then((p) => {
              if ((p.ok === "ok" || p.ok === true) && p.data) applyDto(p.data);
            });
        }
        const err = payload as ChildSetError;
        setSaveError(err.error ?? "Save failed");
        const slotName = err.details?.details?.slot_name;
        if (typeof slotName === "string") setErrorSlot(slotName);
        return undefined;
      })
      .catch((err: unknown) => {
        setSaving(false);
        setSaveError(err instanceof Error ? err.message : "Network error");
      });
  }

  if (loading) {
    return (
      <p data-testid="panel-detail-loading" className="text-[var(--ds-text-muted)]">
        Loading panel…
      </p>
    );
  }
  if (loadError || panel == null) {
    return (
      <p
        data-testid="panel-detail-error"
        role="alert"
        className="text-[var(--ds-text-accent-critical)]"
      >
        {loadError ?? "Panel not found"}
      </p>
    );
  }

  return (
    <div data-testid="panel-detail" className="flex flex-col gap-[var(--ds-spacing-md)]">
      <header className="flex items-center justify-between">
        <h1 className="text-[length:var(--ds-font-size-h2)] font-semibold">
          Panel: <span className="font-mono">{panel.slug}</span>
        </h1>
        <button
          type="button"
          data-testid="panel-detail-save"
          disabled={saving}
          onClick={handleSave}
          className={
            saving
              ? "rounded border border-[var(--ds-border-subtle)] px-[var(--ds-spacing-md)] py-[var(--ds-spacing-xs)] text-[var(--ds-text-muted)]"
              : "rounded bg-[var(--ds-text-accent-info)] px-[var(--ds-spacing-md)] py-[var(--ds-spacing-xs)] text-[var(--ds-bg-canvas)]"
          }
        >
          Save
        </button>
      </header>

      <p data-testid="panel-detail-summary" className="text-[var(--ds-text-muted)]">
        {totalChildren} children across {slots.length} slot(s).
      </p>

      {saveError ? (
        <p
          data-testid="panel-detail-save-error"
          role="alert"
          className="text-[var(--ds-text-accent-critical)]"
        >
          {saveError}
        </p>
      ) : null}

      <div
        data-testid="panel-detail-slots"
        className="flex gap-[var(--ds-spacing-md)] overflow-x-auto"
      >
        {slots.length === 0 ? (
          <p
            data-testid="panel-detail-no-slots"
            className="text-[var(--ds-text-muted)]"
          >
            No slots declared by this panel&apos;s archetype.
          </p>
        ) : null}
        {slots.map((s) => (
          <PanelSlotColumn
            key={s.name}
            slotName={s.name}
            schema={s.schema}
            rows={s.children}
            errorHighlight={errorSlot === s.name}
            onAddChild={handleAddChild}
            onMoveChild={handleMoveChild}
            onDeleteChild={handleDeleteChild}
            onParamsChange={handleParamsChange}
          />
        ))}
      </div>
    </div>
  );
}
