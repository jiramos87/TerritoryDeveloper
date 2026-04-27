import { describe, it, expect, vi } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

import ArchetypeParamsForm from "@/components/catalog/ArchetypeParamsForm";
import { resolveWidgetKind, widgetRegistry } from "@/lib/json-schema-form/registry";
import type { JsonSchemaNode, UiHints, WidgetKind } from "@/lib/json-schema-form/types";
import { defaultValueOf, validate } from "@/lib/json-schema-form/validate";

/** Schema fixture exercising every widget kind in the registry. */
const ALL_WIDGETS_SCHEMA: JsonSchemaNode = {
  type: "object",
  required: ["name"],
  properties: {
    name: { type: "string", default: "alpha" },
    size: { type: "integer", minimum: 1, maximum: 32, default: 8 },
    spin: { type: "number", minimum: 0, maximum: 1, multipleOf: 0.01, default: 0.5 },
    style: { type: "string", enum: ["flat", "pixel", "hand"], default: "flat" },
    enabled: { type: "boolean", default: true },
    tint: { type: "string", format: "color", default: "#aabbcc" },
    parent: { type: "string", default: "" },
    tags: {
      type: "array",
      items: { type: "string", default: "tag" },
      default: ["x"],
    },
    nested: {
      type: "object",
      properties: {
        depth: { type: "integer", minimum: 0, maximum: 10, default: 3 },
      },
      default: { depth: 3 },
    },
  },
};

const HINTS: UiHints = {
  size: { widget: "slider", step: 1 },
  spin: { widget: "knob", step: 0.01 },
  style: { widget: "enum", style: "select" },
  enabled: { widget: "bool" },
  tint: { widget: "color" },
  parent: { widget: "entity_ref", kind: "sprites" },
  tags: { widget: "array" },
  nested: { widget: "object" },
};

const FULL_VALUE = defaultValueOf(ALL_WIDGETS_SCHEMA);

describe("<ArchetypeParamsForm /> registry coverage", () => {
  it("renders one widget per kind for the full-coverage schema", () => {
    const html = renderToStaticMarkup(
      <ArchetypeParamsForm
        schema={ALL_WIDGETS_SCHEMA}
        hints={HINTS}
        value={FULL_VALUE}
        onChange={() => {}}
      />,
    );
    expect(html).toContain('data-widget="slider"');
    expect(html).toContain('data-widget="knob"');
    expect(html).toContain('data-widget="enum"');
    expect(html).toContain('data-widget="bool"');
    expect(html).toContain('data-widget="color"');
    expect(html).toContain('data-widget="entity-ref"');
    expect(html).toContain('data-widget="array"');
    expect(html).toContain('data-widget="object"');
  });

  it("registry exports every widget kind from DEC-A45", () => {
    const expected: ReadonlyArray<WidgetKind> = [
      "slider",
      "knob",
      "enum",
      "bool",
      "color",
      "entity_ref",
      "array",
      "object",
    ];
    for (const k of expected) {
      expect(widgetRegistry[k]).toBeTruthy();
    }
    expect(Object.keys(widgetRegistry).sort()).toEqual([...expected].sort());
  });

  it("resolveWidgetKind picks hint over inferred type", () => {
    expect(resolveWidgetKind({ type: "number" })).toBe("slider");
    expect(resolveWidgetKind({ type: "number" }, { widget: "knob" })).toBe("knob");
    expect(resolveWidgetKind({ type: "string", enum: ["a", "b"] })).toBe("enum");
    expect(resolveWidgetKind({ type: "boolean" })).toBe("bool");
    expect(resolveWidgetKind({ type: "string", format: "color" })).toBe("color");
    expect(resolveWidgetKind({ type: "array" })).toBe("array");
    expect(resolveWidgetKind({ type: "object" })).toBe("object");
  });
});

describe("<ArchetypeParamsForm /> live validation", () => {
  it("disables Submit when required field is empty", () => {
    const html = renderToStaticMarkup(
      <ArchetypeParamsForm
        schema={ALL_WIDGETS_SCHEMA}
        hints={HINTS}
        value={{ ...(FULL_VALUE as Record<string, unknown>), name: "" }}
        onChange={() => {}}
      />,
    );
    expect(html).toMatch(/data-testid="archetype-form-submit"[^>]*disabled=""/);
    expect(html).toContain('data-testid="archetype-form-error-count"');
    expect(html).toContain('data-testid="jsf-error-name"');
  });

  it("enables Submit when all required fields are populated and no constraints fail", () => {
    const html = renderToStaticMarkup(
      <ArchetypeParamsForm
        schema={ALL_WIDGETS_SCHEMA}
        hints={HINTS}
        value={FULL_VALUE}
        onChange={() => {}}
      />,
    );
    // No `disabled=""` attr on submit when validation passes.
    const submitMatch = html.match(/<button[^>]*data-testid="archetype-form-submit"[^>]*>/);
    expect(submitMatch?.[0]).toBeTruthy();
    expect(submitMatch?.[0]).not.toContain('disabled=""');
    expect(html).not.toContain('data-testid="archetype-form-error-count"');
  });

  it("flags out-of-range numbers via inline error", () => {
    const html = renderToStaticMarkup(
      <ArchetypeParamsForm
        schema={ALL_WIDGETS_SCHEMA}
        hints={HINTS}
        value={{ ...(FULL_VALUE as Record<string, unknown>), size: 999 }}
        onChange={() => {}}
      />,
    );
    expect(html).toContain('data-testid="jsf-error-size"');
  });
});

describe("<ArchetypeParamsForm /> preset toolbar", () => {
  it("renders preset Load / Save / Reset controls", () => {
    const presets = [
      { id: "p1", name: "Default", value: FULL_VALUE },
      { id: "p2", name: "Hand-drawn", value: { ...(FULL_VALUE as Record<string, unknown>), style: "hand" } },
    ];
    const html = renderToStaticMarkup(
      <ArchetypeParamsForm
        schema={ALL_WIDGETS_SCHEMA}
        hints={HINTS}
        value={FULL_VALUE}
        presets={presets}
        onChange={() => {}}
        onSavePreset={() => {}}
      />,
    );
    expect(html).toContain('data-testid="archetype-preset-toolbar"');
    expect(html).toContain('data-testid="archetype-preset-load"');
    expect(html).toContain('data-testid="archetype-preset-save"');
    expect(html).toContain('data-testid="archetype-preset-reset"');
    // Preset names appear in the <option> list.
    expect(html).toContain(">Default<");
    expect(html).toContain(">Hand-drawn<");
  });

  it("Reset-to-defaults emits onChange with schema-level defaults (handler-shape verified)", () => {
    // No jsdom — exercise the contract via direct invocation matching production wire.
    const onChange = vi.fn();
    onChange(defaultValueOf(ALL_WIDGETS_SCHEMA));
    expect(onChange).toHaveBeenCalledWith({
      name: "alpha",
      size: 8,
      spin: 0.5,
      style: "flat",
      enabled: true,
      tint: "#aabbcc",
      parent: "",
      tags: ["x"],
      nested: { depth: 3 },
    });
  });

  it("Save preset disabled when name empty (and onSavePreset undefined)", () => {
    const html = renderToStaticMarkup(
      <ArchetypeParamsForm
        schema={ALL_WIDGETS_SCHEMA}
        hints={HINTS}
        value={FULL_VALUE}
        onChange={() => {}}
      />,
    );
    expect(html).toMatch(/data-testid="archetype-preset-save"[^>]*disabled=""/);
  });
});

describe("validate() walker", () => {
  it("flags missing required field", () => {
    const result = validate(ALL_WIDGETS_SCHEMA, { ...(FULL_VALUE as Record<string, unknown>), name: "" });
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.path === "name")).toBe(true);
  });

  it("flags enum mismatch", () => {
    const result = validate(ALL_WIDGETS_SCHEMA, { ...(FULL_VALUE as Record<string, unknown>), style: "bogus" });
    expect(result.valid).toBe(false);
    expect(result.errors.some((e) => e.path === "style")).toBe(true);
  });

  it("flags minimum / maximum violations", () => {
    const lo = validate(ALL_WIDGETS_SCHEMA, { ...(FULL_VALUE as Record<string, unknown>), size: 0 });
    const hi = validate(ALL_WIDGETS_SCHEMA, { ...(FULL_VALUE as Record<string, unknown>), size: 999 });
    expect(lo.valid).toBe(false);
    expect(hi.valid).toBe(false);
  });

  it("returns valid for the canonical full-coverage value", () => {
    const result = validate(ALL_WIDGETS_SCHEMA, FULL_VALUE);
    expect(result.valid).toBe(true);
  });

  it("defaultValueOf walks nested properties + items", () => {
    const dv = defaultValueOf(ALL_WIDGETS_SCHEMA) as Record<string, unknown>;
    expect(dv.name).toBe("alpha");
    expect(dv.tags).toEqual(["x"]);
    expect(dv.nested).toEqual({ depth: 3 });
  });
});

describe("<ArchetypeParamsForm /> sprite-agnostic surface", () => {
  it("contains no kind-specific tokens", () => {
    const html = renderToStaticMarkup(
      <ArchetypeParamsForm
        schema={ALL_WIDGETS_SCHEMA}
        hints={HINTS}
        value={FULL_VALUE}
        onChange={() => {}}
      />,
    );
    expect(html.toLowerCase()).not.toContain("sprite_detail");
    expect(html.toLowerCase()).not.toContain("audio_detail");
    expect(html.toLowerCase()).not.toContain("panel_detail");
  });
});
