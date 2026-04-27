import { describe, expect, it } from "vitest";

import { diffSchemas } from "@/lib/archetype/diff-schemas";
import { validateMigrationHint } from "@/lib/archetype/migration-hint-validator";
import type { JsonSchemaNode } from "@/lib/json-schema-form/types";

const OLD: JsonSchemaNode = {
  type: "object",
  properties: {
    color: { type: "string" },
    legacy: { type: "integer" },
  },
};

const NEW: JsonSchemaNode = {
  type: "object",
  properties: {
    colour: { type: "string" },
    fresh: { type: "integer" },
  },
};

const DIFF = diffSchemas(OLD, NEW);

describe("validateMigrationHint", () => {
  it("ok=false when removed field has no rule", () => {
    const r = validateMigrationHint(DIFF, {}, NEW);
    expect(r.ok).toBe(false);
    if (!r.ok) {
      expect(r.errors.find((e) => e.path === "removed.color")).toBeDefined();
      expect(r.errors.find((e) => e.path === "removed.legacy")).toBeDefined();
    }
  });

  it("ok=true when every removed field is covered + types match", () => {
    const r = validateMigrationHint(
      DIFF,
      {
        rename: [{ from: "color", to: "colour" }],
        drop: [{ slug: "legacy" }],
        default: [{ slug: "fresh", value: 0 }],
      },
      NEW,
    );
    expect(r).toEqual({ ok: true });
  });

  it("rejects rename.to that is not an added field", () => {
    const r = validateMigrationHint(
      DIFF,
      {
        rename: [{ from: "color", to: "ghost" }],
        drop: [{ slug: "legacy" }],
      },
      NEW,
    );
    expect(r.ok).toBe(false);
    if (!r.ok) {
      expect(r.errors.find((e) => e.path === "rename.color")).toBeDefined();
    }
  });

  it("rejects rename.from that is not a removed field", () => {
    const r = validateMigrationHint(
      DIFF,
      {
        rename: [{ from: "phantom", to: "colour" }],
        drop: [{ slug: "legacy" }, { slug: "color" }],
      },
      NEW,
    );
    expect(r.ok).toBe(false);
    if (!r.ok) {
      expect(r.errors.find((e) => e.path === "rename.phantom")).toBeDefined();
    }
  });

  it("rejects default value that mismatches target type", () => {
    const r = validateMigrationHint(
      DIFF,
      {
        rename: [{ from: "color", to: "colour" }],
        drop: [{ slug: "legacy" }],
        default: [{ slug: "fresh", value: "not-int" }],
      },
      NEW,
    );
    expect(r.ok).toBe(false);
    if (!r.ok) {
      expect(r.errors.find((e) => e.path === "default.fresh")).toBeDefined();
    }
  });

  it("rejects default.slug that is not an added field", () => {
    const r = validateMigrationHint(
      DIFF,
      {
        rename: [{ from: "color", to: "colour" }],
        drop: [{ slug: "legacy" }],
        default: [{ slug: "ghost", value: 0 }],
      },
      NEW,
    );
    expect(r.ok).toBe(false);
    if (!r.ok) {
      expect(r.errors.find((e) => e.path === "default.ghost")).toBeDefined();
    }
  });

  it("rejects drop.slug that is not a removed field", () => {
    const r = validateMigrationHint(
      DIFF,
      {
        rename: [{ from: "color", to: "colour" }],
        drop: [{ slug: "legacy" }, { slug: "phantom" }],
      },
      NEW,
    );
    expect(r.ok).toBe(false);
    if (!r.ok) {
      expect(r.errors.find((e) => e.path === "drop.phantom")).toBeDefined();
    }
  });

  it("rename.to type-mismatch flagged", () => {
    const oldS: JsonSchemaNode = {
      type: "object",
      properties: { src: { type: "integer" } },
    };
    const newS: JsonSchemaNode = {
      type: "object",
      properties: { dst: { type: "string" } },
    };
    const diff = diffSchemas(oldS, newS);
    const r = validateMigrationHint(
      diff,
      { rename: [{ from: "src", to: "dst" }] },
      newS,
    );
    expect(r.ok).toBe(false);
    if (!r.ok) {
      expect(
        r.errors.find((e) => e.message.includes("type string does not match integer")),
      ).toBeDefined();
    }
  });
});
