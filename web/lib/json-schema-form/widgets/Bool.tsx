"use client";

import type { WidgetProps } from "../registry";

export default function BoolWidget(props: WidgetProps) {
  const { value, path, onChange, schema } = props;
  const checked = value === undefined ? Boolean(schema.default) : Boolean(value);
  return (
    <input
      type="checkbox"
      data-testid={`jsf-bool-${path}`}
      data-widget="bool"
      checked={checked}
      onChange={(e) => onChange(e.currentTarget.checked)}
    />
  );
}
