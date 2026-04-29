/**
 * AssetDiff render tests (TECH-3303 / Stage 14.3).
 *
 * @see web/components/diff/renderers/asset.tsx
 */
import { describe, expect, it } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

import AssetDiff from "@/components/diff/renderers/asset";
import type { KindDiff } from "@/lib/diff/kind-schemas";

import addedFixture from "./fixtures/asset-added.json" with { type: "json" };
import removedFixture from "./fixtures/asset-removed.json" with { type: "json" };
import changedFixture from "./fixtures/asset-changed.json" with { type: "json" };

describe("AssetDiff (TECH-3303)", () => {
  it("renders added field names", () => {
    const html = renderToStaticMarkup(
      <AssetDiff diff={addedFixture as KindDiff} />,
    );
    expect(html).toContain('data-testid="asset-renderer"');
    expect(html).toContain("sprite_id");
    expect(html).toContain("tags");
    expect(html).toContain("bg-green-50");
  });

  it("renders removed field names", () => {
    const html = renderToStaticMarkup(
      <AssetDiff diff={removedFixture as KindDiff} />,
    );
    expect(html).toContain("zone_subtype_ids");
    expect(html).toContain("bg-red-50");
    expect(html).toContain("line-through");
  });

  it("routes changed fields to scalar / sprite-as-scalar / list fallbacks", () => {
    const html = renderToStaticMarkup(
      <AssetDiff diff={changedFixture as KindDiff} />,
    );
    expect(html).toContain('data-testid="route-by-hint"');
    // sprite hint -> falls back to scalar (no override in asset renderer)
    expect(html).toContain("spr_house_v1");
    expect(html).toContain("spr_house_v2");
    // scalar
    expect(html).toContain("house_old");
    expect(html).toContain("house_new");
    // list
    expect(html).toContain("downtown");
  });
});
