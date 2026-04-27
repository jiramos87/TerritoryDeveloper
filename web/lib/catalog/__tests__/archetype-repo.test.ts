import { describe, expect, it } from "vitest";

import {
  archetypeSlugRegex,
  validateArchetypeCreateBody,
  validateArchetypePatchBody,
  validateVersionPatchBody,
} from "@/lib/catalog/archetype-repo";

describe("archetypeSlugRegex", () => {
  it.each([
    ["sprite_archetype", true],
    ["a__b", true],
    ["abc", true],
    ["with9_digits", true],
    ["AbC", false],
    ["1leading", false],
    ["ab", false],
    ["with-dash", false],
    ["", false],
  ])("%s -> %s", (slug, ok) => {
    expect(archetypeSlugRegex.test(slug)).toBe(ok);
  });
});

describe("validateArchetypeCreateBody", () => {
  it("accepts well-formed body", () => {
    expect(
      validateArchetypeCreateBody({
        slug: "sprite_button",
        display_name: "Sprite Button",
        tags: ["sprite"],
        kind_tag: "sprite",
      }),
    ).toEqual({ ok: true });
  });

  it("rejects bad slug", () => {
    const r = validateArchetypeCreateBody({ slug: "Bad", display_name: "x" });
    expect(r.ok).toBe(false);
    if (r.ok === false) expect(r.reason).toMatch(/slug/);
  });

  it("rejects empty display_name", () => {
    const r = validateArchetypeCreateBody({ slug: "good_slug", display_name: "  " });
    expect(r.ok).toBe(false);
    if (r.ok === false) expect(r.reason).toMatch(/display_name/);
  });
});

describe("validateArchetypePatchBody", () => {
  it("accepts updated_at-only body", () => {
    expect(
      validateArchetypePatchBody({ updated_at: new Date().toISOString() }),
    ).toEqual({ ok: true });
  });

  it("rejects missing updated_at", () => {
    const r = validateArchetypePatchBody({} as unknown as Parameters<typeof validateArchetypePatchBody>[0]);
    expect(r.ok).toBe(false);
    if (r.ok === false) expect(r.reason).toMatch(/updated_at/);
  });

  it("rejects unknown fields", () => {
    const r = validateArchetypePatchBody({
      updated_at: new Date().toISOString(),
      foo: "bar",
    } as unknown as Parameters<typeof validateArchetypePatchBody>[0]);
    expect(r.ok).toBe(false);
    if (r.ok === false) expect(r.reason).toMatch(/unknown fields/);
  });

  it("rejects bad new slug shape", () => {
    const r = validateArchetypePatchBody({
      updated_at: new Date().toISOString(),
      slug: "Bad",
    });
    expect(r.ok).toBe(false);
    if (r.ok === false) expect(r.reason).toMatch(/slug/);
  });
});

describe("validateVersionPatchBody", () => {
  it("accepts well-formed body", () => {
    expect(
      validateVersionPatchBody({
        updated_at: new Date().toISOString(),
        params_json: { fields: [] },
      }),
    ).toEqual({ ok: true });
  });

  it("rejects missing params_json", () => {
    const r = validateVersionPatchBody({
      updated_at: new Date().toISOString(),
    } as unknown as Parameters<typeof validateVersionPatchBody>[0]);
    expect(r.ok).toBe(false);
    if (r.ok === false) expect(r.reason).toMatch(/params_json/);
  });

  it("rejects non-object params_json", () => {
    const r = validateVersionPatchBody({
      updated_at: new Date().toISOString(),
      params_json: null as unknown as Record<string, unknown>,
    });
    expect(r.ok).toBe(false);
    if (r.ok === false) expect(r.reason).toMatch(/params_json/);
  });

  it("accepts migration_hint_json=null (clear)", () => {
    expect(
      validateVersionPatchBody({
        updated_at: new Date().toISOString(),
        params_json: {},
        migration_hint_json: null,
      }),
    ).toEqual({ ok: true });
  });
});
