// TECH-2094 / Stage 10.1 — <PanelPreview /> SSR shape + recursion + depth cap.

import { describe, expect, test } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

import PanelPreview, { type PanelPreviewChild } from "@/components/preview/PanelPreview";

const SLOTS = [
  { slot: "header", label: "Header" },
  { slot: "body", label: "Body" },
  { slot: "footer", label: "Footer" },
];

describe("<PanelPreview /> SSR shape (TECH-2094)", () => {
  test("renders one DOM column per slot definition", () => {
    const html = renderToStaticMarkup(
      <PanelPreview slots={SLOTS} panelChildren={[]} />,
    );
    for (const s of SLOTS) {
      expect(html).toContain(`data-testid="panel-preview-slot-${s.slot}"`);
      expect(html).toContain(s.label);
    }
  });

  test("renders children sorted by order_idx within slot", () => {
    const children: PanelPreviewChild[] = [
      { slot: "body", order_idx: 1, display_name: "Second" },
      { slot: "body", order_idx: 0, display_name: "First" },
      { slot: "header", order_idx: 0, display_name: "Title" },
    ];
    const html = renderToStaticMarkup(
      <PanelPreview slots={SLOTS} panelChildren={children} />,
    );
    const idxFirst = html.indexOf("First");
    const idxSecond = html.indexOf("Second");
    expect(idxFirst).toBeGreaterThan(-1);
    expect(idxSecond).toBeGreaterThan(idxFirst);
    expect(html).toContain("Title");
  });

  test("recursive nested panel rendering inherits slot defs + bumps depth", () => {
    const children: PanelPreviewChild[] = [
      {
        slot: "body",
        order_idx: 0,
        display_name: "Nested",
        nested: [
          { slot: "header", order_idx: 0, display_name: "Inner title" },
        ],
      },
    ];
    const html = renderToStaticMarkup(
      <PanelPreview slots={SLOTS} panelChildren={children} />,
    );
    expect(html).toContain('data-depth="0"');
    expect(html).toContain('data-depth="1"');
    expect(html).toContain("Inner title");
  });

  test("depth cap at 6 short-circuits recursion", () => {
    // Build a 7-deep chain: depth 0..5 nest; depth 6 should hit the cap.
    function chain(depth: number): PanelPreviewChild {
      const display_name = `level-${depth}`;
      if (depth === 0) return { slot: "body", order_idx: 0, display_name };
      return {
        slot: "body",
        order_idx: 0,
        display_name,
        nested: [chain(depth - 1)],
      };
    }
    const children: PanelPreviewChild[] = [chain(6)];
    const html = renderToStaticMarkup(
      <PanelPreview slots={SLOTS} panelChildren={children} />,
    );
    expect(html).toContain('data-testid="panel-preview-recursion-cap"');
  });
});
