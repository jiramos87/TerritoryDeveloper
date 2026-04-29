/**
 * Hint-table coverage + `hintFor` fallback assertions (TECH-3300 / Stage 14.3).
 *
 * @see web/lib/diff/kind-schemas.ts
 */
import { describe, expect, test } from "vitest";

import { hintFor, type FieldHint } from "@/lib/diff/kind-schemas";
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

describe("hintFor (TECH-3300)", () => {
  test("unknown field falls back to 'scalar' for every kind", () => {
    for (const kind of KINDS) {
      const hint: FieldHint = hintFor(kind, "definitely_not_a_real_field");
      expect(hint).toBe("scalar");
    }
  });

  test("token kind: color-bearing fields hint 'token'", () => {
    expect(hintFor("token", "value")).toBe("token");
    expect(hintFor("token", "hex")).toBe("token");
    expect(hintFor("token", "rgb")).toBe("token");
    expect(hintFor("token", "hsl")).toBe("token");
  });

  test("token kind: list field hints 'list'; non-color text falls back to scalar", () => {
    expect(hintFor("token", "tags")).toBe("list");
    expect(hintFor("token", "name")).toBe("scalar");
    expect(hintFor("token", "description")).toBe("scalar");
  });

  test("sprite kind: image_path hints 'blob'; tags hints 'list'", () => {
    expect(hintFor("sprite", "image_path")).toBe("blob");
    expect(hintFor("sprite", "thumbnail_path")).toBe("blob");
    expect(hintFor("sprite", "tags")).toBe("list");
  });

  test("asset kind: sprite_id hints 'sprite' (nested-kind ref)", () => {
    expect(hintFor("asset", "sprite_id")).toBe("sprite");
    expect(hintFor("asset", "tags")).toBe("list");
  });

  test("button kind: hover_token / pressed_token hint 'token'", () => {
    expect(hintFor("button", "hover_token")).toBe("token");
    expect(hintFor("button", "pressed_token")).toBe("token");
    expect(hintFor("button", "states")).toBe("list");
  });

  test("panel kind: background_token / border_token hint 'token'", () => {
    expect(hintFor("panel", "background_token")).toBe("token");
    expect(hintFor("panel", "border_token")).toBe("token");
    expect(hintFor("panel", "child_button_ids")).toBe("list");
  });

  test("pool kind: members hints 'list'", () => {
    expect(hintFor("pool", "members")).toBe("list");
    expect(hintFor("pool", "member_asset_ids")).toBe("list");
  });

  test("archetype kind: nested-kind sub-payload hints", () => {
    expect(hintFor("archetype", "asset")).toBe("asset");
    expect(hintFor("archetype", "sprite")).toBe("sprite");
    expect(hintFor("archetype", "audio")).toBe("audio");
    expect(hintFor("archetype", "token")).toBe("token");
    expect(hintFor("archetype", "asset_ref")).toBe("asset");
    expect(hintFor("archetype", "slots")).toBe("list");
  });

  test("audio kind: audio_path hints 'blob'", () => {
    expect(hintFor("audio", "audio_path")).toBe("blob");
    expect(hintFor("audio", "waveform_path")).toBe("blob");
    expect(hintFor("audio", "tags")).toBe("list");
  });
});
