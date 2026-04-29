/**
 * TokenDiff render tests (TECH-3303 / Stage 14.3).
 *
 * Verifies SSR token swatch path: `CSS.supports` is unavailable in
 * `react-dom/server` Node env, so `isCssColor` SSR-guards to scalar
 * fallback. Client mount re-renders with swatch chip when CSS is parseable.
 *
 * Tests assert:
 * - added/removed blocks render
 * - changed token-hinted fields fall back to scalar in SSR
 * - non-color token-hinted fields ALWAYS scalar
 *
 * @see web/components/diff/renderers/token.tsx
 */
import { describe, expect, it } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

import TokenDiff from "@/components/diff/renderers/token";
import type { KindDiff } from "@/lib/diff/kind-schemas";

import addedFixture from "./fixtures/token-added.json" with { type: "json" };
import removedFixture from "./fixtures/token-removed.json" with { type: "json" };
import changedFixture from "./fixtures/token-changed.json" with { type: "json" };

describe("TokenDiff (TECH-3303)", () => {
  it("renders added field names", () => {
    const html = renderToStaticMarkup(
      <TokenDiff diff={addedFixture as KindDiff} />,
    );
    expect(html).toContain('data-testid="token-renderer"');
    expect(html).toContain("hex");
    expect(html).toContain("tags");
    expect(html).toContain("bg-green-50");
  });

  it("renders removed field names", () => {
    const html = renderToStaticMarkup(
      <TokenDiff diff={removedFixture as KindDiff} />,
    );
    expect(html).toContain("rgb");
    expect(html).toContain("bg-red-50");
  });

  it("renders changed token + scalar + list fields (SSR scalar fallback for token hint)", () => {
    const html = renderToStaticMarkup(
      <TokenDiff diff={changedFixture as KindDiff} />,
    );
    expect(html).toContain('data-testid="route-by-hint"');
    // value field is token-hinted color; SSR falls back to scalar
    expect(html).toContain("#ff0000");
    expect(html).toContain("#00ff00");
    // scalar: name
    expect(html).toContain("color_primary");
    expect(html).toContain("color_brand_primary");
    // list: tags (set diff: + primary added)
    expect(html).toContain("+ primary");
  });

  it("token-hinted non-color value path falls back to scalar text", () => {
    // Test the TokenSwatchPair branch where neither side is CSS color
    // (description field in fixture: "old_token" -> "not-a-color" with hint=token)
    const html = renderToStaticMarkup(
      <TokenDiff diff={changedFixture as KindDiff} />,
    );
    expect(html).toContain("old_token");
    expect(html).toContain("not-a-color");
  });

  it("renders TokenSwatchPair for color hint when CSS is mocked", () => {
    // Stub global CSS so isCssColor returns true on hex strings
    type CssLike = { supports: (prop: string, val: string) => boolean };
    const origGlobalCss = (globalThis as unknown as { CSS?: CssLike }).CSS;
    (globalThis as unknown as { CSS: CssLike }).CSS = {
      supports: (prop: string, val: string) =>
        prop === "color" && /^#[0-9a-f]{3,8}$/i.test(val),
    };
    try {
      const html = renderToStaticMarkup(
        <TokenDiff diff={changedFixture as KindDiff} />,
      );
      // value field "#ff0000" -> "#00ff00" should render swatch chips
      expect(html).toContain('data-testid="token-swatch-diff"');
      expect(html).toContain('data-testid="token-swatch-chip"');
      expect(html).toContain('data-testid="token-swatch-color"');
      expect(html).toContain("#ff0000");
      expect(html).toContain("#00ff00");
      // description field is non-color even when CSS available
      // (its values "old_token" / "not-a-color" don't match hex regex)
      // -> still renders inside TokenSwatchPair but as <code> fallback
      expect(html).toContain("old_token");
      expect(html).toContain("not-a-color");
    } finally {
      if (origGlobalCss === undefined) {
        delete (globalThis as unknown as { CSS?: CssLike }).CSS;
      } else {
        (globalThis as unknown as { CSS: CssLike }).CSS = origGlobalCss;
      }
    }
  });
});
