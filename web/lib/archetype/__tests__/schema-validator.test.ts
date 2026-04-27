import { describe, expect, it } from "vitest";

import {
  FIELD_SLUG_RE,
  validateSchemaShape,
} from "@/lib/archetype/schema-validator";

describe("FIELD_SLUG_RE", () => {
  it.each([
    ["x", true],
    ["foo_bar", true],
    ["abc123", true],
    ["A", false],
    ["1leading", false],
    ["", false],
    ["bad-slug", false],
  ])("%s -> %s", (slug, ok) => {
    expect(FIELD_SLUG_RE.test(slug)).toBe(ok);
  });
});

describe("validateSchemaShape", () => {
  it("accepts empty schema", () => {
    expect(validateSchemaShape({})).toEqual({ ok: true });
  });

  it("accepts well-formed string field", () => {
    const r = validateSchemaShape({
      type: "object",
      properties: {
        name: { type: "string", default: "hello" },
      },
    });
    expect(r).toEqual({ ok: true });
  });

  it("rejects bad slug", () => {
    const r = validateSchemaShape({
      properties: { "Bad-Slug": { type: "string" } },
    });
    expect(r.ok).toBe(false);
    if (!r.ok) expect(r.errors[0]!.message).toMatch(/slug/);
  });

  it("rejects missing type", () => {
    const r = validateSchemaShape({
      properties: { foo: {} },
    });
    expect(r.ok).toBe(false);
    if (!r.ok) expect(r.errors[0]!.message).toMatch(/type required/);
  });

  it("rejects empty enum", () => {
    const r = validateSchemaShape({
      properties: { mode: { type: "string", enum: [] } },
    });
    expect(r.ok).toBe(false);
    if (!r.ok)
      expect(r.errors.find((e) => e.message.includes("enum"))).toBeDefined();
  });

  it("rejects default that mismatches type", () => {
    const r = validateSchemaShape({
      properties: { count: { type: "integer", default: "not-int" } },
    });
    expect(r.ok).toBe(false);
    if (!r.ok)
      expect(r.errors.find((e) => e.path.endsWith("default"))).toBeDefined();
  });

  it("rejects default not in enum", () => {
    const r = validateSchemaShape({
      properties: {
        mode: { type: "string", enum: ["a", "b"], default: "c" },
      },
    });
    expect(r.ok).toBe(false);
    if (!r.ok)
      expect(r.errors.some((e) => e.message.includes("default not in enum"))).toBe(
        true,
      );
  });

  it("accepts integer default that is an integer", () => {
    expect(
      validateSchemaShape({
        properties: { count: { type: "integer", default: 5 } },
      }),
    ).toEqual({ ok: true });
  });

  it("rejects non-integer default for integer field", () => {
    const r = validateSchemaShape({
      properties: { count: { type: "integer", default: 1.5 } },
    });
    expect(r.ok).toBe(false);
  });

  it("rejects min greater than max", () => {
    const r = validateSchemaShape({
      properties: { x: { type: "number", minimum: 10, maximum: 5 } },
    });
    expect(r.ok).toBe(false);
    if (!r.ok)
      expect(r.errors.some((e) => e.message.includes("minimum greater"))).toBe(
        true,
      );
  });

  it("rejects malformed properties container", () => {
    const r = validateSchemaShape({
      properties: [] as unknown as Record<string, never>,
    });
    expect(r.ok).toBe(false);
  });
});
