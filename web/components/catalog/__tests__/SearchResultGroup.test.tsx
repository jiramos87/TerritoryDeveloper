import { describe, expect, it, vi } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

import { SearchResultGroup } from "@/components/catalog/SearchResultGroup";
import type { SearchResultRow } from "@/lib/catalog/search-query";

const ROW: SearchResultRow = {
  entity_id: "1",
  kind: "sprite",
  slug: "hero-sprite",
  display_name: "Hero Sprite",
  score: 0.8,
};

describe("<SearchResultGroup />", () => {
  it("renders null when rows array is empty", () => {
    const html = renderToStaticMarkup(
      <SearchResultGroup
        kind="sprite"
        rows={[]}
        selectedId={null}
        onSelect={vi.fn()}
        idPrefix="search-row"
      />,
    );
    expect(html).toBe("");
  });

  it("renders kind heading and count badge when rows present", () => {
    const html = renderToStaticMarkup(
      <SearchResultGroup
        kind="sprite"
        rows={[ROW, { ...ROW, entity_id: "2", slug: "enemy-sprite", display_name: "Enemy Sprite" }]}
        selectedId={null}
        onSelect={vi.fn()}
        idPrefix="search-row"
      />,
    );
    expect(html).toContain("sprite");
    expect(html).toContain("2 results");
  });

  it("renders one row per item", () => {
    const html = renderToStaticMarkup(
      <SearchResultGroup
        kind="sprite"
        rows={[ROW]}
        selectedId={null}
        onSelect={vi.fn()}
        idPrefix="search-row"
      />,
    );
    expect(html).toContain("Hero Sprite");
    expect(html).toContain("hero-sprite");
  });
});
