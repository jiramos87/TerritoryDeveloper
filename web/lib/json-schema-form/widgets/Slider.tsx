"use client";

import type { WidgetProps } from "../registry";

/** Number slider with optional min/max/step from schema + hint (TECH-1673). */
export default function SliderWidget(props: WidgetProps) {
  const { schema, hint, value, path, onChange } = props;
  const min = schema.minimum;
  const max = schema.maximum;
  const step = hint?.step ?? schema.multipleOf ?? (schema.type === "integer" ? 1 : 0.01);
  const numValue = typeof value === "number" ? value : (schema.default as number | undefined) ?? min ?? 0;

  return (
    <input
      type="range"
      data-testid={`jsf-slider-${path}`}
      data-widget="slider"
      min={min}
      max={max}
      step={step}
      value={numValue}
      onChange={(e) => onChange(Number(e.currentTarget.value))}
    />
  );
}
