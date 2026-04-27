"use client";

import { useState } from "react";

import type { WidgetProps } from "../registry";

/** Container widget — collapsible header; recurses into `properties`. */
export default function ObjectField(props: WidgetProps) {
  const { schema, value, path, onChange, renderChild, hint } = props;
  const obj = value !== null && typeof value === "object" ? (value as Record<string, unknown>) : {};
  const [collapsed, setCollapsed] = useState<boolean>(Boolean(hint?.collapsed));

  const properties = schema.properties ?? {};

  function setProp(key: string, next: unknown) {
    onChange({ ...obj, [key]: next });
  }

  return (
    <fieldset data-testid={`jsf-object-${path}`} data-widget="object" style={{ border: "1px solid var(--ds-color-border)", padding: "var(--ds-spacing-sm)" }}>
      <legend>
        <button
          type="button"
          data-testid={`jsf-object-${path}-toggle`}
          onClick={() => setCollapsed((c) => !c)}
          style={{ background: "transparent", border: 0, cursor: "pointer" }}
        >
          {collapsed ? "▸" : "▾"} {schema.title ?? (path || "object")}
        </button>
      </legend>
      {collapsed
        ? null
        : Object.entries(properties).map(([key, childSchema]) => {
            const childPath = path === "" ? key : `${path}.${key}`;
            return (
              <div key={key} data-testid={`jsf-object-${path}-prop-${key}`} style={{ marginBottom: "var(--ds-spacing-xs)" }}>
                <label style={{ display: "block", fontSize: "var(--ds-font-size-sm)" }}>{childSchema.title ?? key}</label>
                {renderChild
                  ? renderChild({
                      schema: childSchema,
                      value: obj[key],
                      path: childPath,
                      onChange: (next) => setProp(key, next),
                    })
                  : null}
              </div>
            );
          })}
    </fieldset>
  );
}
