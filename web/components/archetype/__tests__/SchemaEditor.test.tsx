import { describe, it, expect } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

import SchemaEditor from "@/components/archetype/SchemaEditor";
import type { JsonSchemaNode } from "@/lib/json-schema-form/types";

/**
 * Static-render coverage for `<SchemaEditor />` (TECH-2460).
 * Mirrors `ArchetypeParamsForm.test.tsx` shape — no jsdom; assert markup tokens.
 */
const EMPTY_SCHEMA: JsonSchemaNode = { type: "object", properties: {} };

const TWO_FIELD_SCHEMA: JsonSchemaNode = {
  type: "object",
  properties: {
    name: { type: "string", default: "alpha" },
    size: { type: "integer", minimum: 1, maximum: 32, default: 8 },
  },
};

const BAD_SLUG_SCHEMA: JsonSchemaNode = {
  type: "object",
  properties: {
    "Bad-Slug": { type: "string" },
  },
};

const ENUM_SCHEMA: JsonSchemaNode = {
  type: "object",
  properties: {
    mode: { type: "string", enum: ["a", "b", "c"], default: "a" },
  },
};

describe("<SchemaEditor /> shell", () => {
  it("renders empty-state hint when no fields", () => {
    const html = renderToStaticMarkup(
      <SchemaEditor value={EMPTY_SCHEMA} onChange={() => {}} />,
    );
    expect(html).toContain('data-testid="schema-editor"');
    expect(html).toContain('data-testid="schema-editor-empty"');
    expect(html).toContain('data-testid="schema-editor-add"');
    expect(html).toContain('data-testid="schema-editor-copy"');
    expect(html).toContain('data-testid="schema-editor-preview"');
  });

  it("renders one row per property", () => {
    const html = renderToStaticMarkup(
      <SchemaEditor value={TWO_FIELD_SCHEMA} onChange={() => {}} />,
    );
    expect(html).toContain('data-testid="field-row-name"');
    expect(html).toContain('data-testid="field-row-size"');
    // Default-input variants render per type.
    expect(html).toContain('data-testid="field-row-name-default-text"');
    expect(html).toContain('data-testid="field-row-size-default-num"');
    // Move + remove controls per row.
    expect(html).toContain('data-testid="field-row-name-up"');
    expect(html).toContain('data-testid="field-row-name-down"');
    expect(html).toContain('data-testid="field-row-name-remove"');
    expect(html).not.toContain('data-testid="schema-editor-empty"');
  });

  it("renders min/max controls for numeric fields only", () => {
    const html = renderToStaticMarkup(
      <SchemaEditor value={TWO_FIELD_SCHEMA} onChange={() => {}} />,
    );
    expect(html).toContain('data-testid="field-row-size-min"');
    expect(html).toContain('data-testid="field-row-size-max"');
    expect(html).not.toContain('data-testid="field-row-name-min"');
    expect(html).not.toContain('data-testid="field-row-name-max"');
  });

  it("renders pattern control for non-ref string fields", () => {
    const html = renderToStaticMarkup(
      <SchemaEditor value={TWO_FIELD_SCHEMA} onChange={() => {}} />,
    );
    expect(html).toContain('data-testid="field-row-name-pattern"');
    expect(html).not.toContain('data-testid="field-row-size-pattern"');
  });

  it("renders enum-default select when field has enum array", () => {
    const html = renderToStaticMarkup(
      <SchemaEditor value={ENUM_SCHEMA} onChange={() => {}} />,
    );
    expect(html).toContain('data-testid="field-row-mode-default-enum"');
    expect(html).not.toContain('data-testid="field-row-mode-default-text"');
  });
});

describe("<SchemaEditor /> live preview", () => {
  it("preview pre-block contains serialized current value", () => {
    const html = renderToStaticMarkup(
      <SchemaEditor value={TWO_FIELD_SCHEMA} onChange={() => {}} />,
    );
    // `\n` literals get escaped as part of HTML pretty-print; assert key tokens.
    expect(html).toContain("&quot;name&quot;");
    expect(html).toContain("&quot;size&quot;");
    expect(html).toContain("&quot;default&quot;: &quot;alpha&quot;");
    expect(html).toContain("&quot;minimum&quot;: 1");
    expect(html).toContain("&quot;maximum&quot;: 32");
  });
});

describe("<SchemaEditor /> validation surface", () => {
  it("renders error list when slug fails regex", () => {
    const html = renderToStaticMarkup(
      <SchemaEditor value={BAD_SLUG_SCHEMA} onChange={() => {}} />,
    );
    expect(html).toContain('data-testid="schema-editor-errors"');
    expect(html.toLowerCase()).toContain("slug");
  });

  it("hides error list when schema shape is valid", () => {
    const html = renderToStaticMarkup(
      <SchemaEditor value={TWO_FIELD_SCHEMA} onChange={() => {}} />,
    );
    expect(html).not.toContain('data-testid="schema-editor-errors"');
  });
});

describe("<SchemaEditor /> ref widget", () => {
  it("renders ref-field-picker when $widget=entity_ref", () => {
    const refSchema: JsonSchemaNode = {
      type: "object",
      properties: {
        parent: { type: "string", $widget: "entity_ref" },
      },
    };
    const html = renderToStaticMarkup(
      <SchemaEditor value={refSchema} onChange={() => {}} refAllowedKinds={["sprites"]} />,
    );
    expect(html).toContain('data-testid="ref-field-picker"');
    expect(html).not.toContain('data-testid="field-row-parent-default-text"');
  });
});
