/**
 * SpriteDiff render tests (TECH-3303 / Stage 14.3).
 *
 * Loads golden fixtures, renders via `react-dom/server`, asserts added /
 * removed / changed blocks surface expected fallback markers.
 *
 * @see web/components/diff/renderers/sprite.tsx
 */
import { describe, expect, it } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

import SpriteDiff from "@/components/diff/renderers/sprite";
import type { KindDiff } from "@/lib/diff/kind-schemas";

import addedFixture from "./fixtures/sprite-added.json" with { type: "json" };
import removedFixture from "./fixtures/sprite-removed.json" with { type: "json" };
import changedFixture from "./fixtures/sprite-changed.json" with { type: "json" };

describe("SpriteDiff (TECH-3303)", () => {
  it("renders added field names with green palette", () => {
    const html = renderToStaticMarkup(
      <SpriteDiff diff={addedFixture as KindDiff} />,
    );
    expect(html).toContain('data-testid="sprite-renderer"');
    expect(html).toContain('data-testid="added-block"');
    expect(html).toContain("thumbnail_path");
    expect(html).toContain("tags");
    expect(html).toContain("bg-green-50");
  });

  it("renders removed field names with red palette + strikethrough", () => {
    const html = renderToStaticMarkup(
      <SpriteDiff diff={removedFixture as KindDiff} />,
    );
    expect(html).toContain('data-testid="removed-block"');
    expect(html).toContain("thumbnail_path");
    expect(html).toContain("variants");
    expect(html).toContain("bg-red-50");
    expect(html).toContain("line-through");
  });

  it("routes changed fields to scalar / list / blob fallbacks via hint", () => {
    const html = renderToStaticMarkup(
      <SpriteDiff diff={changedFixture as KindDiff} />,
    );
    expect(html).toContain('data-testid="route-by-hint"');
    // scalar: name field
    expect(html).toContain("rooftop_old");
    expect(html).toContain("rooftop_new");
    // blob: image_path
    expect(html).toContain("sprites/old.png");
    expect(html).toContain("sprites/new.png");
    // list: tags (set diff renders + added / - removed lines)
    expect(html).toContain("+ c");
    expect(html).toContain("- b");
  });
});
