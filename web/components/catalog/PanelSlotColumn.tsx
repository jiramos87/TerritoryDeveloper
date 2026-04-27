"use client";

import { useState } from "react";

import EntityRefPicker, { type EntityRefRow } from "@/components/catalog/EntityRefPicker";
import PanelChildRow, { type PanelChildRowState } from "@/components/catalog/PanelChildRow";
import type { CatalogPanelSlotSchemaEntry } from "@/types/api/catalog-api";

/**
 * Per-slot column (TECH-1886). Header shows slot.name + count + [min, max];
 * body renders existing children sorted by order_idx; footer renders an
 * `<EntityRefPicker accepts_kind={slot.accepts}>` + Add button → appends
 * pending child with order_idx = max+1.
 */

export type PanelSlotColumnProps = {
  slotName: string;
  schema: CatalogPanelSlotSchemaEntry | null;
  rows: PanelChildRowState[];
  errorHighlight: boolean;
  onAddChild: (
    slotName: string,
    childEntityId: string | null,
    childKind: string,
    row: EntityRefRow | null,
  ) => void;
  onMoveChild: (slotName: string, index: number, dir: -1 | 1) => void;
  onDeleteChild: (slotName: string, index: number) => void;
  onParamsChange: (slotName: string, index: number, next: Record<string, unknown>) => void;
};

export default function PanelSlotColumn({
  slotName,
  schema,
  rows,
  errorHighlight,
  onAddChild,
  onMoveChild,
  onDeleteChild,
  onParamsChange,
}: PanelSlotColumnProps) {
  const [pickerId, setPickerId] = useState<string | null>(null);
  const [pickerRow, setPickerRow] = useState<EntityRefRow | null>(null);

  const min = schema?.min ?? 0;
  const max = schema?.max;
  const accepts = schema?.accepts_kind ?? [];
  const range = max != null ? `[${min}, ${max}]` : `[${min}, ∞]`;

  function handleAdd() {
    if (pickerId == null || pickerRow == null) return;
    onAddChild(slotName, pickerId, pickerRow.kind, pickerRow);
    setPickerId(null);
    setPickerRow(null);
  }

  return (
    <section
      data-testid={`panel-slot-column-${slotName}`}
      className={
        errorHighlight
          ? "flex min-w-[280px] flex-col gap-[var(--ds-spacing-sm)] rounded border border-[var(--ds-text-accent-critical)] bg-[var(--ds-bg-panel)] p-[var(--ds-spacing-sm)]"
          : "flex min-w-[280px] flex-col gap-[var(--ds-spacing-sm)] rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] p-[var(--ds-spacing-sm)]"
      }
    >
      <header className="flex items-center justify-between">
        <h3
          data-testid={`panel-slot-column-${slotName}-name`}
          className="text-[length:var(--ds-font-size-h3)] font-semibold"
        >
          {slotName}
        </h3>
        <span
          data-testid={`panel-slot-column-${slotName}-count`}
          className="text-[var(--ds-text-muted)]"
        >
          {rows.length} / {range}
        </span>
      </header>
      <span
        data-testid={`panel-slot-column-${slotName}-accepts`}
        className="text-xs text-[var(--ds-text-muted)]"
      >
        accepts: {accepts.length > 0 ? accepts.join(", ") : "any"}
      </span>

      <div
        data-testid={`panel-slot-column-${slotName}-children`}
        className="flex flex-col gap-[var(--ds-spacing-xs)]"
      >
        {rows.length === 0 ? (
          <p
            data-testid={`panel-slot-column-${slotName}-empty`}
            className="text-[var(--ds-text-muted)]"
          >
            No children.
          </p>
        ) : null}
        {rows.map((c, i) => (
          <PanelChildRow
            key={`${slotName}-${i}`}
            slotName={slotName}
            index={i}
            child={c}
            isFirst={i === 0}
            isLast={i === rows.length - 1}
            onMoveUp={() => onMoveChild(slotName, i, -1)}
            onMoveDown={() => onMoveChild(slotName, i, 1)}
            onDelete={() => onDeleteChild(slotName, i)}
            onParamsChange={(next) => onParamsChange(slotName, i, next)}
          />
        ))}
      </div>

      <footer className="flex flex-col gap-[var(--ds-spacing-xs)] border-t border-[var(--ds-border-subtle)] pt-[var(--ds-spacing-sm)]">
        <EntityRefPicker
          testId={`panel-slot-column-${slotName}-picker`}
          label="Add child"
          accepts_kind={accepts.length > 0 ? accepts : ["button", "panel", "label", "sprite"]}
          value={pickerRow}
          valueId={pickerId}
          onChange={(id, row) => {
            setPickerId(id);
            setPickerRow(row);
          }}
        />
        <button
          type="button"
          data-testid={`panel-slot-column-${slotName}-add`}
          onClick={handleAdd}
          disabled={pickerId == null || pickerRow == null}
          className={
            pickerId != null
              ? "rounded bg-[var(--ds-text-accent-info)] px-[var(--ds-spacing-md)] py-[var(--ds-spacing-xs)] text-[var(--ds-bg-canvas)]"
              : "rounded border border-[var(--ds-border-subtle)] px-[var(--ds-spacing-md)] py-[var(--ds-spacing-xs)] text-[var(--ds-text-muted)]"
          }
        >
          Add
        </button>
      </footer>
    </section>
  );
}
