"use client";

import type { WidgetProps } from "../registry";

/**
 * Rotary knob — visual variant of slider for compact param panels (TECH-1673).
 * MVP renders a number input with role=slider so screen readers + tests can
 * still drive it; future visual rotary lives in the same component contract.
 */
export default function KnobWidget(props: WidgetProps) {
  const { schema, hint, value, path, onChange } = props;
  const min = schema.minimum ?? 0;
  const max = schema.maximum ?? 1;
  const step = hint?.step ?? schema.multipleOf ?? 0.01;
  const numValue = typeof value === "number" ? value : (schema.default as number | undefined) ?? min;

  return (
    <input
      type="number"
      role="slider"
      aria-valuemin={min}
      aria-valuemax={max}
      aria-valuenow={numValue}
      data-testid={`jsf-knob-${path}`}
      data-widget="knob"
      min={min}
      max={max}
      step={step}
      value={numValue}
      onChange={(e) => onChange(Number(e.currentTarget.value))}
    />
  );
}
