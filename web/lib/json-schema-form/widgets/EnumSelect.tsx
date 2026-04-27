"use client";

import type { WidgetProps } from "../registry";

/** Enum widget — renders <select> by default; radio when hint.style==='radio'. */
export default function EnumWidget(props: WidgetProps) {
  const { schema, hint, value, path, onChange } = props;
  const options = schema.enum ?? [];

  if (hint?.style === "radio") {
    return (
      <fieldset data-testid={`jsf-enum-${path}`} data-widget="enum-radio" style={{ border: 0, padding: 0 }}>
        {options.map((opt) => (
          <label key={String(opt)} style={{ marginRight: "var(--ds-spacing-md)" }}>
            <input
              type="radio"
              name={path}
              value={String(opt)}
              checked={value === opt}
              onChange={() => onChange(opt)}
            />
            {String(opt)}
          </label>
        ))}
      </fieldset>
    );
  }

  return (
    <select
      data-testid={`jsf-enum-${path}`}
      data-widget="enum"
      value={value === undefined || value === null ? "" : String(value)}
      onChange={(e) => {
        const raw = e.currentTarget.value;
        const matched = options.find((o) => String(o) === raw);
        onChange(matched ?? raw);
      }}
    >
      <option value="" disabled>
        Select…
      </option>
      {options.map((opt) => (
        <option key={String(opt)} value={String(opt)}>
          {String(opt)}
        </option>
      ))}
    </select>
  );
}
