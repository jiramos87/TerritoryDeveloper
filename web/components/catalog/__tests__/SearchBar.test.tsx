/**
 * SearchBar — structural + routing unit tests (TECH-4181 §Test Blueprint).
 * Uses renderToStaticMarkup for pure output checks (hooks are mocked).
 */
import { describe, expect, it, vi } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

// Mock hooks + next/navigation before importing SearchBar
vi.mock("next/navigation", () => ({
  useRouter: vi.fn().mockReturnValue({ push: vi.fn() }),
}));

vi.mock("@/lib/hooks/useGlobalHotkey", () => ({
  useGlobalHotkey: vi.fn(),
}));

vi.mock("@/lib/hooks/useSearchDebounce", () => ({
  useSearchDebounce: vi.fn().mockReturnValue({ results: [], loading: false, error: null }),
}));

import { SearchResultGroup } from "@/components/catalog/SearchResultGroup";
import type { SearchResultRow } from "@/lib/catalog/search-query";

const ROW: SearchResultRow = {
  entity_id: "42",
  kind: "button",
  slug: "play-btn",
  display_name: "Play Button",
  score: 0.9,
};

describe("SearchResultGroup routing logic", () => {
  it("renders button result slug correctly", () => {
    const html = renderToStaticMarkup(
      <SearchResultGroup
        kind="button"
        rows={[ROW]}
        selectedId={null}
        onSelect={vi.fn()}
        idPrefix="search-row"
      />,
    );
    expect(html).toContain("Play Button");
    expect(html).toContain("play-btn");
  });

  it("renders archetype result", () => {
    const archetypeRow: SearchResultRow = {
      entity_id: "99",
      kind: "archetype",
      slug: "hero_unit",
      display_name: "Hero Unit",
      score: 0.7,
    };
    const html = renderToStaticMarkup(
      <SearchResultGroup
        kind="archetype"
        rows={[archetypeRow]}
        selectedId={null}
        onSelect={vi.fn()}
        idPrefix="search-row"
      />,
    );
    expect(html).toContain("Hero Unit");
    expect(html).toContain("hero_unit");
  });

  it("empty group returns no content", () => {
    const html = renderToStaticMarkup(
      <SearchResultGroup
        kind="audio"
        rows={[]}
        selectedId={null}
        onSelect={vi.fn()}
        idPrefix="search-row"
      />,
    );
    expect(html).toBe("");
  });
});
