import { describe, it, expect } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

import EntityListDetailLayout from "@/components/catalog/EntityListDetailLayout";

describe("<EntityListDetailLayout />", () => {
  it("renders both slots in their respective panes", () => {
    const html = renderToStaticMarkup(
      <EntityListDetailLayout
        listSlot={<div data-testid="list">L</div>}
        detailSlot={<div data-testid="detail">D</div>}
      />,
    );
    expect(html).toContain('data-testid="entity-list-pane"');
    expect(html).toContain('data-testid="entity-detail-pane"');
    expect(html).toContain('data-testid="list"');
    expect(html).toContain('data-testid="detail"');

    const listPaneIdx = html.indexOf('data-testid="entity-list-pane"');
    const detailPaneIdx = html.indexOf('data-testid="entity-detail-pane"');
    const listIdx = html.indexOf('data-testid="list"');
    const detailIdx = html.indexOf('data-testid="detail"');

    // List slot lives inside list pane (between list pane open and detail pane open)
    expect(listIdx).toBeGreaterThan(listPaneIdx);
    expect(listIdx).toBeLessThan(detailPaneIdx);
    // Detail slot lives after detail pane open
    expect(detailIdx).toBeGreaterThan(detailPaneIdx);
  });

  it("propagates selectedId via data attribute for active-state styling", () => {
    const html = renderToStaticMarkup(
      <EntityListDetailLayout
        listSlot={null}
        detailSlot={null}
        selectedId="sprite-42"
      />,
    );
    expect(html).toContain('data-selected-id="sprite-42"');
  });

  it("contains no kind-specific tokens (sprite/panel/audio/pool)", () => {
    const html = renderToStaticMarkup(
      <EntityListDetailLayout listSlot={null} detailSlot={null} />,
    );
    expect(html.toLowerCase()).not.toContain("sprite");
    expect(html.toLowerCase()).not.toContain("panel-kind");
    expect(html.toLowerCase()).not.toContain("audio");
    expect(html.toLowerCase()).not.toContain("pool");
  });
});
