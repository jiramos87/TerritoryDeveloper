/**
 * `diffVersions` unit tests (TECH-3300 / Stage 14.3).
 *
 * 8 kinds × 5 cases (identical / added-only / removed-only / changed+hint /
 * unknown-field-fallback) + 2 token-specific swatch hint cases = 42 vitest
 * cases. Pure module — no DB / fetch / React imports.
 *
 * @see web/lib/diff/diff-versions.ts
 */
import { describe, expect, test } from "vitest";

import { diffVersions } from "@/lib/diff/diff-versions";
import type { CatalogKind } from "@/lib/refs/types";

const KINDS: CatalogKind[] = [
  "sprite",
  "asset",
  "button",
  "panel",
  "pool",
  "token",
  "archetype",
  "audio",
];

describe("diffVersions (TECH-3300) — base shape per kind", () => {
  for (const kind of KINDS) {
    describe(kind, () => {
      test("identical input → empty diff", () => {
        const payload = { name: "x", tags: ["a", "b"] };
        const diff = diffVersions(kind, payload, payload);
        expect(diff).toEqual({ added: [], removed: [], changed: [] });
      });

      test("only `to` has key X → added", () => {
        const diff = diffVersions(kind, {}, { name: "x" });
        expect(diff.added).toEqual(["name"]);
        expect(diff.removed).toEqual([]);
        expect(diff.changed).toEqual([]);
      });

      test("only `from` has key X → removed", () => {
        const diff = diffVersions(kind, { name: "x" }, {});
        expect(diff.added).toEqual([]);
        expect(diff.removed).toEqual(["name"]);
        expect(diff.changed).toEqual([]);
      });

      test("both have unknown key Y with diff value → changed with hint=scalar", () => {
        const diff = diffVersions(
          kind,
          { y_unknown_field: "old" },
          { y_unknown_field: "new" },
        );
        expect(diff.changed.length).toBe(1);
        expect(diff.changed[0]!.field).toBe("y_unknown_field");
        expect(diff.changed[0]!.hint).toBe("scalar");
        expect(diff.changed[0]!.before).toBe("old");
        expect(diff.changed[0]!.after).toBe("new");
      });
    });
  }
});

describe("diffVersions — token kind swatch hint", () => {
  test("token kind, value field changed → hint='token'", () => {
    const diff = diffVersions("token", { value: "#ff0000" }, { value: "#00ff00" });
    expect(diff.changed).toEqual([
      { field: "value", before: "#ff0000", after: "#00ff00", hint: "token" },
    ]);
  });

  test("token kind, hex field changed → hint='token'", () => {
    const diff = diffVersions("token", { hex: "#aaa" }, { hex: "#bbb" });
    expect(diff.changed[0]!.hint).toBe("token");
  });
});

describe("diffVersions — kind-specific hint propagation", () => {
  test("sprite.image_path changed → hint='blob'", () => {
    const diff = diffVersions("sprite", { image_path: "a.png" }, { image_path: "b.png" });
    expect(diff.changed[0]!.hint).toBe("blob");
  });

  test("asset.sprite_id changed → hint='sprite' (nested-kind)", () => {
    const diff = diffVersions("asset", { sprite_id: 1 }, { sprite_id: 2 });
    expect(diff.changed[0]!.hint).toBe("sprite");
  });

  test("button.hover_token changed → hint='token'", () => {
    const diff = diffVersions("button", { hover_token: "primary" }, { hover_token: "danger" });
    expect(diff.changed[0]!.hint).toBe("token");
  });

  test("panel.background_token changed → hint='token'", () => {
    const diff = diffVersions(
      "panel",
      { background_token: "neutral_100" },
      { background_token: "neutral_200" },
    );
    expect(diff.changed[0]!.hint).toBe("token");
  });

  test("pool.members list changed → hint='list'", () => {
    const diff = diffVersions("pool", { members: ["a"] }, { members: ["a", "b"] });
    expect(diff.changed[0]!.hint).toBe("list");
  });

  test("audio.audio_path changed → hint='blob'", () => {
    const diff = diffVersions("audio", { audio_path: "old.mp3" }, { audio_path: "new.mp3" });
    expect(diff.changed[0]!.hint).toBe("blob");
  });

  test("archetype.asset_ref changed → hint='asset' (nested-kind)", () => {
    const diff = diffVersions(
      "archetype",
      { asset_ref: { id: 1 } },
      { asset_ref: { id: 2 } },
    );
    expect(diff.changed[0]!.hint).toBe("asset");
  });
});

describe("diffVersions — equality + ordering", () => {
  test("structurally equal nested objects → no change", () => {
    const diff = diffVersions(
      "sprite",
      { meta: { a: 1, b: [2, 3] } },
      { meta: { a: 1, b: [2, 3] } },
    );
    expect(diff.changed).toEqual([]);
  });

  test("alpha-sort on added / removed / changed", () => {
    const from = { c_old: 1, b_changed: "x" };
    const to = { z_new: 2, a_new: 3, b_changed: "y" };
    const diff = diffVersions("sprite", from, to);
    expect(diff.added).toEqual(["a_new", "z_new"]);
    expect(diff.removed).toEqual(["c_old"]);
    expect(diff.changed.map((c) => c.field)).toEqual(["b_changed"]);
  });

  test("nested array element diff → changed (no recursion into element diff)", () => {
    const diff = diffVersions("sprite", { tags: ["a", "b"] }, { tags: ["a", "c"] });
    expect(diff.changed[0]!.field).toBe("tags");
    expect(diff.changed[0]!.hint).toBe("list");
  });
});
