"use client";

import { useMemo } from "react";

import FieldTypeRow from "./FieldTypeRow";

import { validateSchemaShape } from "@/lib/archetype/schema-validator";
import type { JsonSchemaNode } from "@/lib/json-schema-form/types";

/**
 * Controlled archetype `params_json` schema editor (TECH-2460).
 * Holds row-array of `(slug, JsonSchemaNode)` projected onto `properties`.
 * Live preview pane mirrors `value`; copy-to-clipboard on click.
 */

export type SchemaEditorProps = {
  value: JsonSchemaNode;
  onChange: (next: JsonSchemaNode) => void;
  refAllowedKinds?: ReadonlyArray<string>;
};

const DEFAULT_NEW_SLUG = "new_field";
const DEFAULT_NEW_NODE: JsonSchemaNode = { type: "string" };

export default function SchemaEditor({
  value,
  onChange,
  refAllowedKinds = [],
}: SchemaEditorProps) {
  const props = useMemo(() => value.properties ?? {}, [value.properties]);
  const rows = useMemo(() => Object.entries(props), [props]);
  const validation = useMemo(() => validateSchemaShape(value), [value]);

  const writeRows = (next: ReadonlyArray<[string, JsonSchemaNode]>) => {
    const nextProps: Record<string, JsonSchemaNode> = {};
    for (const [k, v] of next) nextProps[k] = v;
    onChange({ ...value, type: value.type ?? "object", properties: nextProps });
  };

  const handleAdd = () => {
    let slug = DEFAULT_NEW_SLUG;
    let i = 1;
    while (props[slug] !== undefined) slug = `${DEFAULT_NEW_SLUG}_${++i}`;
    writeRows([...rows, [slug, { ...DEFAULT_NEW_NODE }]]);
  };

  const handleSlugChange = (oldSlug: string, nextSlug: string) => {
    writeRows(rows.map(([k, v]) => (k === oldSlug ? [nextSlug, v] : [k, v])));
  };

  const handleNodeChange = (slug: string, next: JsonSchemaNode) => {
    writeRows(rows.map(([k, v]) => (k === slug ? [k, next] : [k, v])));
  };

  const handleRemove = (slug: string) => {
    writeRows(rows.filter(([k]) => k !== slug));
  };

  const handleMove = (slug: string, dir: -1 | 1) => {
    const idx = rows.findIndex(([k]) => k === slug);
    if (idx < 0) return;
    const j = idx + dir;
    if (j < 0 || j >= rows.length) return;
    const next = rows.slice();
    const tmp = next[idx]!;
    next[idx] = next[j]!;
    next[j] = tmp;
    writeRows(next);
  };

  const handleCopy = () => {
    if (typeof navigator !== "undefined" && navigator.clipboard) {
      void navigator.clipboard.writeText(JSON.stringify(value, null, 2));
    }
  };

  return (
    <div data-testid="schema-editor" className="grid gap-[var(--ds-spacing-md)] md:grid-cols-2">
      <div className="flex flex-col gap-[var(--ds-spacing-sm)]">
        <header className="flex items-center justify-between">
          <h2 className="text-[length:var(--ds-font-size-h3)] font-semibold">Fields</h2>
          <button
            type="button"
            data-testid="schema-editor-add"
            onClick={handleAdd}
            className="rounded bg-[var(--ds-text-accent-info)] px-[var(--ds-spacing-md)] py-[var(--ds-spacing-xs)] text-[var(--ds-bg-canvas)]"
          >
            Add field
          </button>
        </header>
        {rows.length === 0 ? (
          <p data-testid="schema-editor-empty" className="text-[var(--ds-text-muted)]">
            No fields yet — add one to start.
          </p>
        ) : null}
        <div className="flex flex-col gap-[var(--ds-spacing-xs)]">
          {rows.map(([slug, node]) => (
            <FieldTypeRow
              key={slug}
              slug={slug}
              node={node}
              onSlugChange={(next) => handleSlugChange(slug, next)}
              onNodeChange={(next) => handleNodeChange(slug, next)}
              onMoveUp={() => handleMove(slug, -1)}
              onMoveDown={() => handleMove(slug, 1)}
              onRemove={() => handleRemove(slug)}
              refAllowedKinds={refAllowedKinds}
            />
          ))}
        </div>
        {!validation.ok ? (
          <ul
            data-testid="schema-editor-errors"
            role="alert"
            className="rounded border border-[var(--ds-text-accent-critical)] p-[var(--ds-spacing-xs)] text-[var(--ds-text-accent-critical)]"
          >
            {validation.errors.map((err, i) => (
              <li key={i}>
                <code>{err.path}</code>: {err.message}
              </li>
            ))}
          </ul>
        ) : null}
      </div>
      <div className="flex flex-col gap-[var(--ds-spacing-xs)]">
        <header className="flex items-center justify-between">
          <h2 className="text-[length:var(--ds-font-size-h3)] font-semibold">Preview</h2>
          <button
            type="button"
            data-testid="schema-editor-copy"
            onClick={handleCopy}
            className="rounded border border-[var(--ds-border-subtle)] px-[var(--ds-spacing-sm)] py-[var(--ds-spacing-xs)]"
          >
            Copy
          </button>
        </header>
        <pre
          data-testid="schema-editor-preview"
          className="overflow-auto rounded bg-[var(--ds-bg-panel)] p-[var(--ds-spacing-sm)] text-[length:var(--ds-font-size-sm)]"
        >
          {JSON.stringify(value, null, 2)}
        </pre>
      </div>
    </div>
  );
}
