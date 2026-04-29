/**
 * ArchetypeDiff render tests (TECH-3304 / Stage 14.3).
 *
 * Asserts nested-kind dispatch — `asset` / `sprite` / `audio` / `token`
 * hints in changed entries trigger their kind renderer wrapped in
 * `archetype-nested-block`.
 *
 * @see web/components/diff/renderers/archetype.tsx
 */
import { describe, expect, it } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

import ArchetypeDiff from "@/components/diff/renderers/archetype";
import type { KindDiff } from "@/lib/diff/kind-schemas";

import addedFixture from "./fixtures/archetype-added.json" with { type: "json" };
import removedFixture from "./fixtures/archetype-removed.json" with { type: "json" };
import changedFixture from "./fixtures/archetype-changed.json" with { type: "json" };

describe("ArchetypeDiff (TECH-3304)", () => {
  it("renders added field names", () => {
    const html = renderToStaticMarkup(
      <ArchetypeDiff diff={addedFixture as KindDiff} />,
    );
    expect(html).toContain('data-testid="archetype-renderer"');
    expect(html).toContain("asset_ref");
    expect(html).toContain("slots");
    expect(html).toContain("bg-green-50");
  });

  it("renders removed field names", () => {
    const html = renderToStaticMarkup(
      <ArchetypeDiff diff={removedFixture as KindDiff} />,
    );
    expect(html).toContain("audio_ref");
    expect(html).toContain("token_ref");
    expect(html).toContain("bg-red-50");
  });

  it("dispatches nested-kind hints (asset / sprite) to kind renderers", () => {
    const html = renderToStaticMarkup(
      <ArchetypeDiff diff={changedFixture as KindDiff} />,
    );
    expect(html).toContain('data-testid="archetype-nested-block-list"');
    // asset_ref nested -> AssetDiff fires
    expect(html).toContain('data-testid="asset-renderer"');
    expect(html).toContain('data-nested-kind="asset"');
    expect(html).toContain("asset_v1");
    expect(html).toContain("asset_v2");
    // sprite_ref nested -> SpriteDiff fires
    expect(html).toContain('data-testid="sprite-renderer"');
    expect(html).toContain('data-nested-kind="sprite"');
    expect(html).toContain("spr_v1");
    expect(html).toContain("spr_v2");
    // scalar fallback for name (non-nested hint)
    expect(html).toContain("house_arch_old");
    expect(html).toContain("house_arch_new");
    // list for slots
    expect(html).toContain("+ slot_b");
  });
});
