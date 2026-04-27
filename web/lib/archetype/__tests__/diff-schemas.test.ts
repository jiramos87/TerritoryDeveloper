import { describe, expect, it } from "vitest";

import { diffSchemas, levenshtein } from "@/lib/archetype/diff-schemas";
import type { JsonSchemaNode } from "@/lib/json-schema-form/types";

describe("levenshtein", () => {
  it.each([
    ["", "", 0],
    ["a", "a", 0],
    ["abc", "abd", 1],
    ["abc", "axyz", 3],
    ["kitten", "sitting", 3],
    ["", "abc", 3],
    ["abc", "", 3],
  ])("%s vs %s -> %s", (a, b, d) => {
    expect(levenshtein(a, b)).toBe(d);
  });
});

describe("diffSchemas", () => {
  it("pure-additive diff", () => {
    const oldS: JsonSchemaNode = { type: "object", properties: { a: { type: "string" } } };
    const newS: JsonSchemaNode = {
      type: "object",
      properties: {
        a: { type: "string" },
        b: { type: "integer" },
      },
    };
    const r = diffSchemas(oldS, newS);
    expect(r.added).toEqual([{ slug: "b", type: "integer" }]);
    expect(r.removed).toEqual([]);
    expect(r.renamed_candidates).toEqual([]);
  });

  it("pure-removal diff", () => {
    const oldS: JsonSchemaNode = {
      type: "object",
      properties: { a: { type: "string" }, b: { type: "string" } },
    };
    const newS: JsonSchemaNode = { type: "object", properties: { a: { type: "string" } } };
    const r = diffSchemas(oldS, newS);
    expect(r.added).toEqual([]);
    expect(r.removed).toEqual([{ slug: "b", type: "string" }]);
    expect(r.renamed_candidates).toEqual([]);
  });

  it("rename heuristic hits when slug close + type matches", () => {
    const oldS: JsonSchemaNode = {
      type: "object",
      properties: { color: { type: "string" } },
    };
    const newS: JsonSchemaNode = {
      type: "object",
      properties: { colour: { type: "string" } },
    };
    const r = diffSchemas(oldS, newS);
    expect(r.renamed_candidates).toEqual([
      { from: "color", to: "colour", type: "string", distance: 1 },
    ]);
  });

  it("rename heuristic misses when types differ", () => {
    const oldS: JsonSchemaNode = {
      type: "object",
      properties: { count: { type: "integer" } },
    };
    const newS: JsonSchemaNode = {
      type: "object",
      properties: { count_str: { type: "string" } },
    };
    const r = diffSchemas(oldS, newS);
    expect(r.renamed_candidates).toEqual([]);
  });

  it("rename heuristic misses when distance exceeds 3", () => {
    const oldS: JsonSchemaNode = {
      type: "object",
      properties: { foo: { type: "string" } },
    };
    const newS: JsonSchemaNode = {
      type: "object",
      properties: { wholly_different_slug: { type: "string" } },
    };
    const r = diffSchemas(oldS, newS);
    expect(r.renamed_candidates).toEqual([]);
  });

  it("empty schemas produce empty diff", () => {
    expect(diffSchemas({}, {})).toEqual({
      added: [],
      removed: [],
      renamed_candidates: [],
    });
  });

  it("all-renamed diff yields candidates for every removed field", () => {
    const oldS: JsonSchemaNode = {
      type: "object",
      properties: { foo: { type: "string" }, bar: { type: "integer" } },
    };
    const newS: JsonSchemaNode = {
      type: "object",
      properties: { fooz: { type: "string" }, baar: { type: "integer" } },
    };
    const r = diffSchemas(oldS, newS);
    expect(r.added.map((x) => x.slug).sort()).toEqual(["baar", "fooz"]);
    expect(r.removed.map((x) => x.slug).sort()).toEqual(["bar", "foo"]);
    expect(r.renamed_candidates).toEqual([
      { from: "bar", to: "baar", type: "integer", distance: 1 },
      { from: "foo", to: "fooz", type: "string", distance: 1 },
    ]);
  });

  it("picks closest candidate when multiple matches exist", () => {
    const oldS: JsonSchemaNode = {
      type: "object",
      properties: { sz: { type: "integer" } },
    };
    const newS: JsonSchemaNode = {
      type: "object",
      properties: {
        size: { type: "integer" },
        sx: { type: "integer" },
      },
    };
    const r = diffSchemas(oldS, newS);
    // Both "size" (distance 2) and "sx" (distance 1) match; closest = sx.
    expect(r.renamed_candidates).toEqual([
      { from: "sz", to: "sx", type: "integer", distance: 1 },
    ]);
  });
});
