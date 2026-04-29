/**
 * PanelDiff render tests (TECH-3304 / Stage 14.3).
 *
 * @see web/components/diff/renderers/panel.tsx
 */
import { describe, expect, it } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

import PanelDiff from "@/components/diff/renderers/panel";
import type { KindDiff } from "@/lib/diff/kind-schemas";

import addedFixture from "./fixtures/panel-added.json" with { type: "json" };
import removedFixture from "./fixtures/panel-removed.json" with { type: "json" };
import changedFixture from "./fixtures/panel-changed.json" with { type: "json" };

describe("PanelDiff (TECH-3304)", () => {
  it("renders added field names with green palette", () => {
    const html = renderToStaticMarkup(
      <PanelDiff diff={addedFixture as KindDiff} />,
    );
    expect(html).toContain('data-testid="panel-renderer"');
    expect(html).toContain("background_token");
    expect(html).toContain("child_button_ids");
    expect(html).toContain("bg-green-50");
  });

  it("renders removed field names with red palette", () => {
    const html = renderToStaticMarkup(
      <PanelDiff diff={removedFixture as KindDiff} />,
    );
    expect(html).toContain("border_token");
    expect(html).toContain("slot_archetype_ids");
    expect(html).toContain("bg-red-50");
  });

  it("routes changed fields to scalar / token-as-scalar / list fallbacks", () => {
    const html = renderToStaticMarkup(
      <PanelDiff diff={changedFixture as KindDiff} />,
    );
    expect(html).toContain('data-testid="route-by-hint"');
    // token hint without override -> scalar
    expect(html).toContain("tok_panel_bg_old");
    expect(html).toContain("tok_panel_bg_new");
    // panel does NOT inject swatch chip
    expect(html).not.toContain('data-testid="token-swatch-chip"');
    // scalar
    expect(html).toContain("Inventory Panel");
    // list
    expect(html).toContain("+ btn_b");
  });
});
