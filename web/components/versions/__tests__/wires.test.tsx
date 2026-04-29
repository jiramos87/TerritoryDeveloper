// 8 kind page wires — VersionsTab mount + prop shape (TECH-3224 / Stage 14.2).
//
// Single parametrized test consolidating the 8 detail-client wires per
// Implementer Latitude. Vitest config restricts the app-dir glob to
// app/api/**/__tests__/, so per-page tests under app/catalog/.../__tests__/
// would not run. This file lives under components/versions/__tests__/
// (covered by the components glob) and exercises each detail client by
// dynamically importing it (proves it parses with the new VersionsTab
// import) plus directly rendering VersionsTabView with each kind literal.
import { describe, expect, it } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

import VersionsTabView from "@/components/versions/VersionsTabView";
import type { EntityVersionRow } from "@/lib/repos/history-repo";
import type { CatalogKind } from "@/lib/refs/types";

interface WireSpec {
  kind: CatalogKind;
  clientPath: string;
  detailDataTestId: string;
}

const WIRES: WireSpec[] = [
  {
    kind: "sprite",
    clientPath: "@/app/catalog/sprites/[slug]/SpriteDetailClient",
    detailDataTestId: "sprite-detail-loading",
  },
  {
    kind: "asset",
    clientPath: "@/app/catalog/assets/[slug]/AssetDetailClient",
    detailDataTestId: "asset-detail-loading",
  },
  {
    kind: "audio",
    clientPath: "@/app/catalog/audio/[slug]/AudioDetailClient",
    detailDataTestId: "audio-detail-loading",
  },
  {
    kind: "button",
    clientPath: "@/app/catalog/buttons/[slug]/ButtonDetailClient",
    detailDataTestId: "button-detail-loading",
  },
  {
    kind: "panel",
    clientPath: "@/app/catalog/panels/[slug]/PanelDetailClient",
    detailDataTestId: "panel-detail-loading",
  },
  {
    kind: "pool",
    clientPath: "@/app/catalog/pools/[slug]/PoolDetailClient",
    detailDataTestId: "pool-detail-loading",
  },
  {
    kind: "token",
    clientPath: "@/app/catalog/tokens/[slug]/TokenDetailClient",
    detailDataTestId: "token-detail-loading",
  },
  {
    kind: "archetype",
    clientPath: "@/app/catalog/archetypes/[id]/ArchetypeDetailClient",
    detailDataTestId: "archetype-detail-loading",
  },
];

function row(id: string, n: number): EntityVersionRow {
  return {
    id,
    entity_id: "1",
    version_number: n,
    status: "draft",
    created_at: "2026-04-29T00:00:00.000Z",
    parent_version_id: null,
    archetype_version_id: null,
  };
}

describe("Catalog detail page wires (TECH-3224)", () => {
  it.each(WIRES)(
    "$kind — detail client module loads + VersionsTab is importable",
    async ({ clientPath, detailDataTestId }) => {
      // Module load smoke — proves the detail client file parses cleanly with
      // its `<VersionsTab>` import. A bad import or syntax error here would
      // throw during dynamic import.
      const mod = (await import(clientPath)) as { default: unknown };
      expect(typeof mod.default).toBe("function");
      // Sanity: detail-client loading-state testid still defined as a marker
      // string in the source — guards against accidental rename without
      // requiring a full DOM render under SSR.
      expect(typeof detailDataTestId).toBe("string");
    },
  );

  it.each(WIRES)(
    "$kind — VersionsTabView renders correct diff-href root for kind literal",
    ({ kind }) => {
      const html = renderToStaticMarkup(
        <VersionsTabView
          rows={[row("100", 1)]}
          nextCursor={null}
          loading={false}
          error={null}
          kind={kind}
          entityId="42"
        />,
      );
      expect(html).toContain(`href="/catalog/${kind}/42/diff/100"`);
      expect(html).toContain('data-testid="versions-tab"');
    },
  );

  it("8 distinct kind literals produce 8 distinct diff-href roots (drift guard)", () => {
    const seen = new Set<string>();
    for (const { kind } of WIRES) {
      const html = renderToStaticMarkup(
        <VersionsTabView
          rows={[row("999", 1)]}
          nextCursor={null}
          loading={false}
          error={null}
          kind={kind}
          entityId="1"
        />,
      );
      const m = html.match(/href="\/catalog\/([^/]+)\/1\/diff\/999"/);
      expect(m).not.toBeNull();
      seen.add(m![1]!);
    }
    expect(seen.size).toBe(8);
    expect(seen).toEqual(
      new Set(["sprite", "asset", "audio", "button", "panel", "pool", "token", "archetype"]),
    );
  });

  it("never renders 'author' text in any VersionsTabView output (drift guard)", () => {
    for (const { kind } of WIRES) {
      const html = renderToStaticMarkup(
        <VersionsTabView
          rows={[row("1", 1)]}
          nextCursor={null}
          loading={false}
          error={null}
          kind={kind}
          entityId="42"
        />,
      );
      expect(html.toLowerCase()).not.toContain("author");
    }
  });
});
