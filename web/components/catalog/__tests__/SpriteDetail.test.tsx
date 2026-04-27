import { describe, it, expect } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

import SpriteDetail, { type SpriteDetailView } from "@/components/catalog/SpriteDetail";

const SPRITE_DRAFT: SpriteDetailView = {
  entity_id: "00000000-0000-0000-0000-000000000001",
  slug: "tree_oak_a",
  display_name: "Tree — Oak A",
  tags: ["tree"],
  retired_at: null,
  current_published_version_id: null,
  sprite_detail: {
    pixels_per_unit: 16,
    pivot_x: 0.5,
    pivot_y: 0.5,
    source_uri: null,
  },
};

const SPRITE_PUBLISHED: SpriteDetailView = {
  ...SPRITE_DRAFT,
  current_published_version_id: "11111111-1111-1111-1111-111111111111",
};

const SPRITE_RETIRED: SpriteDetailView = {
  ...SPRITE_DRAFT,
  retired_at: "2026-04-26T12:00:00.000Z",
};

const TAB_ORDER = ["edit", "versions", "references", "lints", "audit"] as const;

describe("<SpriteDetail />", () => {
  it("renders the five-tab strip in canonical order", () => {
    const html = renderToStaticMarkup(<SpriteDetail sprite={SPRITE_DRAFT} onSave={() => {}} />);
    const indexes = TAB_ORDER.map((k) => html.indexOf(`data-testid="entity-edit-tab-${k}"`));
    expect(indexes.every((i) => i >= 0)).toBe(true);
    for (let i = 1; i < indexes.length; i++) {
      expect(indexes[i]).toBeGreaterThan(indexes[i - 1]!);
    }
  });

  it("renders the Edit tab live (sprite-edit-form present); other tabs render placeholder copy", () => {
    const html = renderToStaticMarkup(<SpriteDetail sprite={SPRITE_DRAFT} onSave={() => {}} />);
    expect(html).toContain('data-testid="sprite-edit-form"');
    expect(html).toContain('data-testid="sprite-detail-placeholder-versions"');
    expect(html).toContain('data-testid="sprite-detail-placeholder-references"');
    expect(html).toContain('data-testid="sprite-detail-placeholder-lints"');
    expect(html).toContain('data-testid="sprite-detail-placeholder-audit"');
    expect(html).toContain("Owned by Stage");
  });

  it("freezes slug input when sprite has a current_published_version_id", () => {
    const html = renderToStaticMarkup(<SpriteDetail sprite={SPRITE_PUBLISHED} onSave={() => {}} />);
    const slugTag = html.match(/<input[^>]*data-testid="sprite-edit-slug"[^>]*>/);
    expect(slugTag?.[0]).toMatch(/readOnly=""|readonly=""/);
  });

  it("renders retired badge when sprite has retired_at", () => {
    const html = renderToStaticMarkup(<SpriteDetail sprite={SPRITE_RETIRED} onSave={() => {}} />);
    expect(html).toContain('data-testid="sprite-detail-retired-badge"');
    expect(html).toContain(">retired<");
  });

  it("surfaces the slug + display_name in the header", () => {
    const html = renderToStaticMarkup(<SpriteDetail sprite={SPRITE_DRAFT} onSave={() => {}} />);
    expect(html).toContain('data-testid="sprite-detail-slug"');
    expect(html).toContain(">tree_oak_a<");
    expect(html).toContain(">Tree — Oak A<");
  });
});
