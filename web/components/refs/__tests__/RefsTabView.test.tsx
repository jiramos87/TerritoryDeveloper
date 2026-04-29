/**
 * RefsTabView static-render tests (TECH-3409 / Stage 14.4).
 *
 * No jsdom — uses `react-dom/server` `renderToStaticMarkup` per existing
 * component test pattern. Covers two-column render, anchor href shape,
 * empty/loading/error states per side, load-more visibility, edge-role
 * data attribute, and `refsLinkHref` helper.
 *
 * View asserted to be render-only via grep guard (no useState/useEffect/fetch).
 *
 * @see web/components/refs/RefsTabView.tsx
 */
import { describe, expect, it } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";
import { readFileSync } from "node:fs";
import path from "node:path";

import RefsTabView, { refsLinkHref } from "@/components/refs/RefsTabView";
import type { CatalogRefEdgeRow } from "@/lib/repos/refs-repo";

function row(overrides: Partial<CatalogRefEdgeRow> = {}): CatalogRefEdgeRow {
  return {
    src_kind: "panel",
    src_id: "10",
    src_version_id: "100",
    dst_kind: "token",
    dst_id: "20",
    dst_version_id: "200",
    edge_role: "panel.token",
    created_at: "2026-04-29T00:00:00.000Z",
    ...overrides,
  };
}

const EMPTY_SIDE = {
  rows: [],
  nextCursor: null,
  loading: false,
  error: null,
};

describe("refsLinkHref", () => {
  it("emits /catalog/{kind}/{id}", () => {
    expect(refsLinkHref("panel", "42")).toBe("/catalog/panel/42");
    expect(refsLinkHref("archetype", "9")).toBe("/catalog/archetype/9");
    expect(refsLinkHref("token", "100")).toBe("/catalog/token/100");
  });
});

describe("<RefsTabView />", () => {
  it("renders incoming + outgoing columns with rows + anchor href + edge-role attribute", () => {
    const html = renderToStaticMarkup(
      <RefsTabView
        incoming={{
          rows: [
            row({
              src_kind: "panel",
              src_id: "10",
              dst_kind: "token",
              dst_id: "20",
              edge_role: "panel.token",
            }),
          ],
          nextCursor: null,
          loading: false,
          error: null,
        }}
        outgoing={{
          rows: [
            row({
              src_kind: "token",
              src_id: "20",
              dst_kind: "asset",
              dst_id: "30",
              edge_role: "asset.sprite",
            }),
          ],
          nextCursor: null,
          loading: false,
          error: null,
        }}
      />,
    );
    expect(html).toContain('data-testid="refs-tab"');
    expect(html).toContain('data-testid="refs-incoming-column"');
    expect(html).toContain('data-testid="refs-outgoing-column"');
    expect(html).toContain('data-testid="refs-incoming-row"');
    expect(html).toContain('data-testid="refs-outgoing-row"');
    expect(html).toContain('data-edge-role="panel.token"');
    expect(html).toContain('data-edge-role="asset.sprite"');
    expect(html).toContain('href="/catalog/panel/10"');
    expect(html).toContain('href="/catalog/asset/30"');
  });

  it("renders empty-state per side independently (incoming has rows, outgoing empty)", () => {
    const html = renderToStaticMarkup(
      <RefsTabView
        incoming={{
          rows: [row({ src_kind: "panel", src_id: "1" })],
          nextCursor: null,
          loading: false,
          error: null,
        }}
        outgoing={EMPTY_SIDE}
      />,
    );
    expect(html).toContain('data-testid="refs-incoming-row"');
    expect(html).not.toContain('data-testid="refs-incoming-empty"');
    expect(html).toContain('data-testid="refs-outgoing-empty"');
    expect(html).not.toContain('data-testid="refs-outgoing-row"');
  });

  it("renders loading state per side (initial fetch, rows empty)", () => {
    const html = renderToStaticMarkup(
      <RefsTabView
        incoming={{ rows: [], nextCursor: null, loading: true, error: null }}
        outgoing={EMPTY_SIDE}
      />,
    );
    expect(html).toContain('data-testid="refs-incoming-loading"');
    expect(html).toContain('data-testid="refs-outgoing-empty"');
  });

  it("renders error state per side with retry button", () => {
    const html = renderToStaticMarkup(
      <RefsTabView
        incoming={{
          rows: [],
          nextCursor: null,
          loading: false,
          error: "boom",
        }}
        outgoing={EMPTY_SIDE}
      />,
    );
    expect(html).toContain('data-testid="refs-incoming-error"');
    expect(html).toContain('data-testid="refs-incoming-retry"');
  });

  it("shows Load more button per side when nextCursor non-null", () => {
    const html = renderToStaticMarkup(
      <RefsTabView
        incoming={{
          rows: [row({ src_id: "1" })],
          nextCursor: "abc",
          loading: false,
          error: null,
        }}
        outgoing={{
          rows: [row({ dst_id: "9" })],
          nextCursor: null,
          loading: false,
          error: null,
        }}
      />,
    );
    expect(html).toContain('data-testid="refs-incoming-load-more"');
    expect(html).not.toContain('data-testid="refs-outgoing-load-more"');
  });

  it("renders both empty when no edges", () => {
    const html = renderToStaticMarkup(
      <RefsTabView incoming={EMPTY_SIDE} outgoing={EMPTY_SIDE} />,
    );
    expect(html).toContain('data-testid="refs-incoming-empty"');
    expect(html).toContain('data-testid="refs-outgoing-empty"');
  });

  it("renders header text 'Incoming refs' + 'Outgoing refs'", () => {
    const html = renderToStaticMarkup(
      <RefsTabView incoming={EMPTY_SIDE} outgoing={EMPTY_SIDE} />,
    );
    expect(html).toContain("Incoming refs");
    expect(html).toContain("Outgoing refs");
  });
});

describe("RefsTabView source guard", () => {
  it("view file contains no fetch / useState / useEffect references", () => {
    const file = readFileSync(
      path.resolve(__dirname, "..", "RefsTabView.tsx"),
      "utf8",
    );
    expect(file).not.toMatch(/\bfetch\(/);
    expect(file).not.toMatch(/\buseState\b/);
    expect(file).not.toMatch(/\buseEffect\b/);
  });
});
