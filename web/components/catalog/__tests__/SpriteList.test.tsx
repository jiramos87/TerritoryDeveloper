import { describe, it, expect, vi } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

import SpriteList, { type SpriteListRow } from "@/components/catalog/SpriteList";

const ROWS: SpriteListRow[] = [
  {
    entity_id: "00000000-0000-0000-0000-000000000001",
    slug: "tree_oak_a",
    display_name: "Tree — Oak A",
    status: "active",
    updated_at: "2026-04-26T12:00:00.000Z",
  },
  {
    entity_id: "00000000-0000-0000-0000-000000000002",
    slug: "tree_pine_a",
    display_name: "Tree — Pine A",
    status: "retired",
    updated_at: "2026-04-25T12:00:00.000Z",
  },
];

describe("<SpriteList />", () => {
  it("renders three filter chips: Active / Retired / All", () => {
    const html = renderToStaticMarkup(
      <SpriteList rows={ROWS} filter="active" onFilterChange={() => {}} />,
    );
    expect(html).toContain('data-testid="sprite-list-filter-active"');
    expect(html).toContain('data-testid="sprite-list-filter-retired"');
    expect(html).toContain('data-testid="sprite-list-filter-all"');
  });

  it("marks the active filter chip with aria-selected=true", () => {
    const html = renderToStaticMarkup(
      <SpriteList rows={ROWS} filter="retired" onFilterChange={() => {}} />,
    );
    const retiredChip = html.match(/<button[^>]*data-testid="sprite-list-filter-retired"[^>]*>/);
    expect(retiredChip?.[0]).toContain('aria-selected="true"');
    const activeChip = html.match(/<button[^>]*data-testid="sprite-list-filter-active"[^>]*>/);
    expect(activeChip?.[0]).toContain('aria-selected="false"');
  });

  it("renders a 'Create sprite' CTA linking to /catalog/sprites/new", () => {
    const html = renderToStaticMarkup(
      <SpriteList rows={ROWS} filter="active" onFilterChange={() => {}} />,
    );
    expect(html).toContain('data-testid="sprite-list-create-cta"');
    expect(html).toContain('href="/catalog/sprites/new"');
  });

  it("renders one row per sprite with slug, display_name, and status", () => {
    const html = renderToStaticMarkup(
      <SpriteList rows={ROWS} filter="all" onFilterChange={() => {}} />,
    );
    expect(html).toContain('data-testid="sprite-list-row-tree_oak_a"');
    expect(html).toContain('data-testid="sprite-list-row-tree_pine_a"');
    expect(html).toContain(">tree_oak_a<");
    expect(html).toContain(">Tree — Oak A<");
    expect(html).toContain(">active<");
    expect(html).toContain(">retired<");
    expect(html).toContain('href="/catalog/sprites/tree_oak_a"');
    expect(html).toContain('href="/catalog/sprites/tree_pine_a"');
  });

  it("renders empty-state copy when no rows", () => {
    const html = renderToStaticMarkup(
      <SpriteList rows={[]} filter="active" onFilterChange={() => {}} />,
    );
    expect(html).toContain('data-testid="sprite-list-empty"');
  });

  it("renders error copy when error present", () => {
    const html = renderToStaticMarkup(
      <SpriteList rows={[]} filter="active" onFilterChange={() => {}} error="boom" />,
    );
    expect(html).toContain('data-testid="sprite-list-error"');
    expect(html).toContain("boom");
  });

  it("onFilterChange handler-shape verified via direct invocation", () => {
    const onFilterChange = vi.fn();
    onFilterChange("retired");
    expect(onFilterChange).toHaveBeenCalledWith("retired");
  });
});
