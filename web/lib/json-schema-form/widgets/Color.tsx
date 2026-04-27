"use client";

import type { WidgetProps } from "../registry";

const HEX_RE = /^#[0-9a-fA-F]{6}$/;

/** Hex color picker with text fallback (TECH-1673). */
export default function ColorWidget(props: WidgetProps) {
  const { value, path, onChange, schema } = props;
  const fallback = typeof schema.default === "string" ? schema.default : "#000000";
  const current = typeof value === "string" && HEX_RE.test(value) ? value : fallback;
  const textValue = typeof value === "string" ? value : current;

  return (
    <span data-testid={`jsf-color-${path}`} data-widget="color" style={{ display: "inline-flex", gap: "var(--ds-spacing-xs)" }}>
      <input
        type="color"
        data-testid={`jsf-color-${path}-picker`}
        value={current}
        onChange={(e) => onChange(e.currentTarget.value)}
      />
      <input
        type="text"
        data-testid={`jsf-color-${path}-text`}
        value={textValue}
        onChange={(e) => onChange(e.currentTarget.value)}
        placeholder="#rrggbb"
        style={{ width: "8ch" }}
      />
    </span>
  );
}
