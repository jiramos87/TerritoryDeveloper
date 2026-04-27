"use client";

import { useMemo, useState } from "react";

import { resolveWidgetKind, widgetRegistry } from "@/lib/json-schema-form/registry";
import type { FieldHint, JsonSchemaNode, UiHints, ValidationError } from "@/lib/json-schema-form/types";
import { defaultValueOf, validate } from "@/lib/json-schema-form/validate";

export type ArchetypePreset = {
  id: string;
  name: string;
  value: unknown;
};

export type ArchetypeParamsFormProps = {
  schema: JsonSchemaNode;
  hints?: UiHints;
  value: unknown;
  presets?: ArchetypePreset[];
  onChange: (next: unknown) => void;
  onSubmit?: (value: unknown) => void;
  onSavePreset?: (name: string, value: unknown) => void;
};

/**
 * Schema-driven form (TECH-1673 / DEC-A45). Walks `schema.properties` (or
 * single-leaf schema) into the widget registry, runs live validation, and
 * surfaces a preset toolbar (Load / Save / Reset-to-defaults).
 *
 * Component is sprite-agnostic — every kind passes its own `params_schema` +
 * `ui_hints_json` and consumes the controlled-input contract.
 */
export default function ArchetypeParamsForm(props: ArchetypeParamsFormProps) {
  const { schema, hints, value, presets, onChange, onSubmit, onSavePreset } = props;
  const [presetName, setPresetName] = useState<string>("");

  const errors: ValidationError[] = useMemo(() => validate(schema, value).errors, [schema, value]);
  const errorByPath = useMemo(() => {
    const m = new Map<string, string>();
    for (const err of errors) m.set(err.path, err.message);
    return m;
  }, [errors]);

  function renderChild(args: {
    schema: JsonSchemaNode;
    hint?: FieldHint;
    value: unknown;
    path: string;
    onChange: (next: unknown) => void;
  }): React.ReactNode {
    const childHint = args.hint ?? hints?.[args.path];
    const kind = resolveWidgetKind(args.schema, childHint);
    const Widget = widgetRegistry[kind];
    const fieldError = errorByPath.get(args.path);
    return (
      <span data-testid={`jsf-field-${args.path}`} data-widget-kind={kind}>
        <Widget
          schema={args.schema}
          hint={childHint}
          value={args.value}
          path={args.path}
          onChange={args.onChange}
          renderChild={renderChild}
        />
        {fieldError ? (
          <span
            data-testid={`jsf-error-${args.path}`}
            role="alert"
            style={{ color: "var(--color-text-accent-critical)", fontSize: "var(--text-xs)" }}
          >
            {fieldError}
          </span>
        ) : null}
      </span>
    );
  }

  function handleLoadPreset(presetId: string) {
    const preset = presets?.find((p) => p.id === presetId);
    if (preset) onChange(preset.value);
  }

  function handleReset() {
    onChange(defaultValueOf(schema));
  }

  function handleSavePreset() {
    if (!onSavePreset || presetName.trim() === "") return;
    onSavePreset(presetName.trim(), value);
    setPresetName("");
  }

  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (errors.length === 0 && onSubmit) onSubmit(value);
  }

  // Top-level: object schemas render their property tree; single-leaf schemas
  // render the one widget at path "".
  const isObject = schema.type === "object" && schema.properties;

  return (
    <form
      data-testid="archetype-params-form"
      onSubmit={handleSubmit}
      style={{ display: "flex", flexDirection: "column", gap: "var(--ds-spacing-sm)" }}
    >
      <div data-testid="archetype-preset-toolbar" style={{ display: "flex", gap: "var(--ds-spacing-xs)", alignItems: "center" }}>
        <label style={{ fontSize: "var(--text-xs)" }}>Presets</label>
        <select
          data-testid="archetype-preset-load"
          defaultValue=""
          onChange={(e) => {
            const id = e.currentTarget.value;
            if (id) handleLoadPreset(id);
          }}
        >
          <option value="">Load preset…</option>
          {(presets ?? []).map((p) => (
            <option key={p.id} value={p.id}>
              {p.name}
            </option>
          ))}
        </select>
        <input
          type="text"
          data-testid="archetype-preset-name"
          placeholder="Preset name"
          value={presetName}
          onChange={(e) => setPresetName(e.currentTarget.value)}
        />
        <button
          type="button"
          data-testid="archetype-preset-save"
          disabled={!onSavePreset || presetName.trim() === ""}
          onClick={handleSavePreset}
        >
          Save preset
        </button>
        <button
          type="button"
          data-testid="archetype-preset-reset"
          onClick={handleReset}
        >
          Reset to defaults
        </button>
      </div>

      <div data-testid="archetype-form-body">
        {isObject
          ? Object.entries(schema.properties ?? {}).map(([key, childSchema]) =>
              renderChild({
                schema: childSchema,
                hint: hints?.[key],
                value: (value !== null && typeof value === "object" ? (value as Record<string, unknown>)[key] : undefined),
                path: key,
                onChange: (next) => {
                  const base = value !== null && typeof value === "object" ? (value as Record<string, unknown>) : {};
                  onChange({ ...base, [key]: next });
                },
              }),
            )
          : renderChild({
              schema,
              hint: hints?.[""],
              value,
              path: "",
              onChange,
            })}
      </div>

      <div data-testid="archetype-form-footer" style={{ display: "flex", gap: "var(--ds-spacing-xs)", alignItems: "center" }}>
        <button
          type="submit"
          data-testid="archetype-form-submit"
          disabled={errors.length > 0}
        >
          Submit
        </button>
        {errors.length > 0 ? (
          <span
            data-testid="archetype-form-error-count"
            role="status"
            style={{ color: "var(--color-text-accent-critical)", fontSize: "var(--text-xs)" }}
          >
            {errors.length} error{errors.length === 1 ? "" : "s"}
          </span>
        ) : null}
      </div>
    </form>
  );
}
