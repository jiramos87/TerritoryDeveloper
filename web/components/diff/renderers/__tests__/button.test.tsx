/**
 * ButtonDiff render tests (TECH-3303 / Stage 14.3).
 *
 * @see web/components/diff/renderers/button.tsx
 */
import { describe, expect, it } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

import ButtonDiff from "@/components/diff/renderers/button";
import type { KindDiff } from "@/lib/diff/kind-schemas";

import addedFixture from "./fixtures/button-added.json" with { type: "json" };
import removedFixture from "./fixtures/button-removed.json" with { type: "json" };
import changedFixture from "./fixtures/button-changed.json" with { type: "json" };

describe("ButtonDiff (TECH-3303)", () => {
  it("renders added field names", () => {
    const html = renderToStaticMarkup(
      <ButtonDiff diff={addedFixture as KindDiff} />,
    );
    expect(html).toContain('data-testid="button-renderer"');
    expect(html).toContain("icon_sprite_id");
    expect(html).toContain("hover_token");
    expect(html).toContain("bg-green-50");
  });

  it("renders removed field names", () => {
    const html = renderToStaticMarkup(
      <ButtonDiff diff={removedFixture as KindDiff} />,
    );
    expect(html).toContain("pressed_token");
    expect(html).toContain("bg-red-50");
  });

  it("routes changed fields to scalar / list / token-as-scalar fallbacks", () => {
    const html = renderToStaticMarkup(
      <ButtonDiff diff={changedFixture as KindDiff} />,
    );
    expect(html).toContain('data-testid="route-by-hint"');
    // scalar: label
    expect(html).toContain("Save Changes");
    // list: states
    expect(html).toContain("disabled");
    // token hint without swatch override -> scalar fallback
    expect(html).toContain("tok_blue_old");
    expect(html).toContain("tok_blue_new");
    // button does NOT inject swatch chip
    expect(html).not.toContain('data-testid="token-swatch-chip"');
  });
});
