"use client";

import RefFieldPicker from "./RefFieldPicker";

import type { JsonSchemaNode, JsonSchemaType } from "@/lib/json-schema-form/types";

/**
 * Per-row editor for one archetype `params_json` field (TECH-2460).
 * Dispatches default + validation rule UI by `type`; ref-picker via $widget.
 */

export type FieldTypeRowProps = {
  slug: string;
  node: JsonSchemaNode;
  onSlugChange: (next: string) => void;
  onNodeChange: (next: JsonSchemaNode) => void;
  onMoveUp: () => void;
  onMoveDown: () => void;
  onRemove: () => void;
  refAllowedKinds?: ReadonlyArray<string>;
};

const TYPES: ReadonlyArray<{ id: JsonSchemaType | "ref"; label: string }> = [
  { id: "string", label: "String" },
  { id: "integer", label: "Integer" },
  { id: "number", label: "Number" },
  { id: "boolean", label: "Boolean" },
  { id: "ref", label: "Ref" },
];

export default function FieldTypeRow({
  slug,
  node,
  onSlugChange,
  onNodeChange,
  onMoveUp,
  onMoveDown,
  onRemove,
  refAllowedKinds = [],
}: FieldTypeRowProps) {
  const isRef = node.$widget === "entity_ref";
  const typeId: string = isRef ? "ref" : (node.type as string) ?? "string";
  const isEnum = Array.isArray(node.enum);

  const handleTypeChange = (next: string) => {
    if (next === "ref") {
      onNodeChange({ type: "string", $widget: "entity_ref" });
      return;
    }
    const t = next as JsonSchemaType;
    onNodeChange({ type: t });
  };

  return (
    <div
      data-testid={`field-row-${slug}`}
      className="flex flex-col gap-[var(--ds-spacing-xs)] rounded border border-[var(--ds-border-subtle)] p-[var(--ds-spacing-sm)]"
    >
      <div className="flex items-center gap-[var(--ds-spacing-xs)]">
        <label className="flex flex-col text-[length:var(--ds-font-size-sm)]">
          <span className="text-[var(--ds-text-muted)]">Slug</span>
          <input
            data-testid={`field-row-${slug}-slug`}
            value={slug}
            onChange={(e) => onSlugChange(e.target.value)}
            className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-canvas)] px-[var(--ds-spacing-xs)] py-[var(--ds-spacing-2xs)]"
          />
        </label>
        <label className="flex flex-col text-[length:var(--ds-font-size-sm)]">
          <span className="text-[var(--ds-text-muted)]">Type</span>
          <select
            data-testid={`field-row-${slug}-type`}
            value={typeId}
            onChange={(e) => handleTypeChange(e.target.value)}
            className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-canvas)] px-[var(--ds-spacing-xs)] py-[var(--ds-spacing-2xs)]"
          >
            {TYPES.map((t) => (
              <option key={t.id} value={t.id}>
                {t.label}
              </option>
            ))}
          </select>
        </label>
        <div className="ml-auto flex items-center gap-[var(--ds-spacing-2xs)]">
          <button
            type="button"
            data-testid={`field-row-${slug}-up`}
            onClick={onMoveUp}
            className="rounded border border-[var(--ds-border-subtle)] px-[var(--ds-spacing-xs)] py-[var(--ds-spacing-2xs)]"
          >
            Up
          </button>
          <button
            type="button"
            data-testid={`field-row-${slug}-down`}
            onClick={onMoveDown}
            className="rounded border border-[var(--ds-border-subtle)] px-[var(--ds-spacing-xs)] py-[var(--ds-spacing-2xs)]"
          >
            Down
          </button>
          <button
            type="button"
            data-testid={`field-row-${slug}-remove`}
            onClick={onRemove}
            className="rounded border border-[var(--ds-text-accent-critical)] px-[var(--ds-spacing-xs)] py-[var(--ds-spacing-2xs)] text-[var(--ds-text-accent-critical)]"
          >
            Remove
          </button>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-[var(--ds-spacing-xs)]">
        {/* Default */}
        <label className="flex flex-col text-[length:var(--ds-font-size-sm)]">
          <span className="text-[var(--ds-text-muted)]">Default</span>
          {isRef ? (
            <RefFieldPicker
              allowedKinds={refAllowedKinds}
              value={typeof node.default === "string" ? node.default : null}
              onChange={(v) => onNodeChange({ ...node, default: v ?? undefined })}
            />
          ) : node.type === "boolean" ? (
            <input
              type="checkbox"
              data-testid={`field-row-${slug}-default-bool`}
              checked={!!node.default}
              onChange={(e) => onNodeChange({ ...node, default: e.target.checked })}
            />
          ) : isEnum ? (
            <select
              data-testid={`field-row-${slug}-default-enum`}
              value={(node.default as string | number | undefined) ?? ""}
              onChange={(e) =>
                onNodeChange({
                  ...node,
                  default: node.type === "string" ? e.target.value : Number(e.target.value),
                })
              }
              className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-canvas)] px-[var(--ds-spacing-xs)] py-[var(--ds-spacing-2xs)]"
            >
              <option value="">— none —</option>
              {(node.enum ?? []).map((v) => (
                <option key={String(v)} value={String(v)}>
                  {String(v)}
                </option>
              ))}
            </select>
          ) : node.type === "integer" || node.type === "number" ? (
            <input
              type="number"
              data-testid={`field-row-${slug}-default-num`}
              step={node.type === "integer" ? 1 : "any"}
              value={(node.default as number | undefined) ?? ""}
              onChange={(e) =>
                onNodeChange({
                  ...node,
                  default: e.target.value === "" ? undefined : Number(e.target.value),
                })
              }
              className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-canvas)] px-[var(--ds-spacing-xs)] py-[var(--ds-spacing-2xs)]"
            />
          ) : (
            <input
              type="text"
              data-testid={`field-row-${slug}-default-text`}
              value={(node.default as string | undefined) ?? ""}
              onChange={(e) =>
                onNodeChange({
                  ...node,
                  default: e.target.value === "" ? undefined : e.target.value,
                })
              }
              className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-canvas)] px-[var(--ds-spacing-xs)] py-[var(--ds-spacing-2xs)]"
            />
          )}
        </label>

        {/* Validation rules */}
        {(node.type === "integer" || node.type === "number") ? (
          <div className="flex gap-[var(--ds-spacing-xs)]">
            <label className="flex flex-1 flex-col text-[length:var(--ds-font-size-sm)]">
              <span className="text-[var(--ds-text-muted)]">Min</span>
              <input
                type="number"
                data-testid={`field-row-${slug}-min`}
                value={node.minimum ?? ""}
                onChange={(e) =>
                  onNodeChange({
                    ...node,
                    minimum: e.target.value === "" ? undefined : Number(e.target.value),
                  })
                }
                className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-canvas)] px-[var(--ds-spacing-xs)] py-[var(--ds-spacing-2xs)]"
              />
            </label>
            <label className="flex flex-1 flex-col text-[length:var(--ds-font-size-sm)]">
              <span className="text-[var(--ds-text-muted)]">Max</span>
              <input
                type="number"
                data-testid={`field-row-${slug}-max`}
                value={node.maximum ?? ""}
                onChange={(e) =>
                  onNodeChange({
                    ...node,
                    maximum: e.target.value === "" ? undefined : Number(e.target.value),
                  })
                }
                className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-canvas)] px-[var(--ds-spacing-xs)] py-[var(--ds-spacing-2xs)]"
              />
            </label>
          </div>
        ) : node.type === "string" && !isRef ? (
          <label className="flex flex-col text-[length:var(--ds-font-size-sm)]">
            <span className="text-[var(--ds-text-muted)]">Pattern (regex)</span>
            <input
              type="text"
              data-testid={`field-row-${slug}-pattern`}
              value={node.format ?? ""}
              onChange={(e) =>
                onNodeChange({
                  ...node,
                  format: e.target.value === "" ? undefined : e.target.value,
                })
              }
              className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-canvas)] px-[var(--ds-spacing-xs)] py-[var(--ds-spacing-2xs)]"
            />
          </label>
        ) : null}
      </div>
    </div>
  );
}
