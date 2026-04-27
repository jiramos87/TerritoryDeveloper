"use client";

import type { WidgetProps } from "../registry";
import { defaultValueOf } from "../validate";

/** Container widget — add/remove rows, recurses into `items` schema. */
export default function ArrayField(props: WidgetProps) {
  const { schema, value, path, onChange, renderChild } = props;
  const items = Array.isArray(value) ? value : [];
  const itemSchema = schema.items;

  function setAt(idx: number, next: unknown) {
    const copy = items.slice();
    copy[idx] = next;
    onChange(copy);
  }

  function removeAt(idx: number) {
    const copy = items.slice();
    copy.splice(idx, 1);
    onChange(copy);
  }

  function addRow() {
    const seed = itemSchema ? defaultValueOf(itemSchema) : undefined;
    onChange([...items, seed]);
  }

  return (
    <div data-testid={`jsf-array-${path}`} data-widget="array">
      {items.map((item, idx) => (
        <div key={idx} data-testid={`jsf-array-${path}-row-${idx}`} style={{ display: "flex", gap: "var(--ds-spacing-xs)" }}>
          {itemSchema && renderChild
            ? renderChild({
                schema: itemSchema,
                value: item,
                path: `${path}.${idx}`,
                onChange: (next) => setAt(idx, next),
              })
            : null}
          <button
            type="button"
            data-testid={`jsf-array-${path}-remove-${idx}`}
            onClick={() => removeAt(idx)}
          >
            Remove
          </button>
        </div>
      ))}
      <button type="button" data-testid={`jsf-array-${path}-add`} onClick={addRow}>
        Add row
      </button>
    </div>
  );
}
