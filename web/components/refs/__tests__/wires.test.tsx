// 8 kind page wires — RefsTab mount + per-kind link href shape (TECH-3410 / Stage 14.4).
//
// Mirrors `web/components/versions/__tests__/wires.test.tsx` style:
// dynamic-imports each detail client (proves it parses with the new RefsTab
// import) + renders RefsTabView per kind asserting incoming/outgoing href
// shape matches `/catalog/{kind}/{id}` for each `EdgeRole` direction.
//
// Per-kind seed direction follows the 8-role union in `web/lib/refs/types.ts`:
//   sprite — incoming (button.sprite, asset.sprite, archetype.sprite)
//   asset — incoming (pool.asset, archetype.asset) + outgoing (asset.sprite)
//   button — outgoing (button.sprite)
//   panel — outgoing (panel.token)
//   audio — incoming (archetype.audio)
//   pool — outgoing (pool.asset)
//   token — incoming (panel.token, archetype.token)
//   archetype — outgoing (archetype.{asset,sprite,token,audio})
import { describe, expect, it } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

import RefsTabView from "@/components/refs/RefsTabView";
import type { CatalogRefEdgeRow } from "@/lib/repos/refs-repo";
import type { CatalogKind, EdgeRole } from "@/lib/refs/types";

interface WireSpec {
  kind: CatalogKind;
  clientPath: string;
  detailDataTestId: string;
  /** Direction of the seeded edge for the per-kind golden. */
  direction: "incoming" | "outgoing";
  /** Edge role for the seeded edge. */
  edge_role: EdgeRole;
  /** Other-side kind (the one the link points to in the rendered row). */
  other_kind: CatalogKind;
}

const WIRES: WireSpec[] = [
  {
    kind: "sprite",
    clientPath: "@/app/catalog/sprites/[slug]/SpriteDetailClient",
    detailDataTestId: "sprite-detail-loading",
    direction: "incoming",
    edge_role: "asset.sprite",
    other_kind: "asset",
  },
  {
    kind: "asset",
    clientPath: "@/app/catalog/assets/[slug]/AssetDetailClient",
    detailDataTestId: "asset-detail-loading",
    direction: "outgoing",
    edge_role: "asset.sprite",
    other_kind: "sprite",
  },
  {
    kind: "audio",
    clientPath: "@/app/catalog/audio/[slug]/AudioDetailClient",
    detailDataTestId: "audio-detail-loading",
    direction: "incoming",
    edge_role: "archetype.audio",
    other_kind: "archetype",
  },
  {
    kind: "button",
    clientPath: "@/app/catalog/buttons/[slug]/ButtonDetailClient",
    detailDataTestId: "button-detail-loading",
    direction: "outgoing",
    edge_role: "button.sprite",
    other_kind: "sprite",
  },
  {
    kind: "panel",
    clientPath: "@/app/catalog/panels/[slug]/PanelDetailClient",
    detailDataTestId: "panel-detail-loading",
    direction: "outgoing",
    edge_role: "panel.token",
    other_kind: "token",
  },
  {
    kind: "pool",
    clientPath: "@/app/catalog/pools/[slug]/PoolDetailClient",
    detailDataTestId: "pool-detail-loading",
    direction: "outgoing",
    edge_role: "pool.asset",
    other_kind: "asset",
  },
  {
    kind: "token",
    clientPath: "@/app/catalog/tokens/[slug]/TokenDetailClient",
    detailDataTestId: "token-detail-loading",
    direction: "incoming",
    edge_role: "panel.token",
    other_kind: "panel",
  },
  {
    kind: "archetype",
    clientPath: "@/app/catalog/archetypes/[id]/ArchetypeDetailClient",
    detailDataTestId: "archetype-detail-loading",
    direction: "outgoing",
    edge_role: "archetype.asset",
    other_kind: "asset",
  },
];

function row(spec: WireSpec, otherId: string, selfId: string): CatalogRefEdgeRow {
  if (spec.direction === "incoming") {
    return {
      src_kind: spec.other_kind,
      src_id: otherId,
      src_version_id: "1",
      dst_kind: spec.kind,
      dst_id: selfId,
      dst_version_id: "1",
      edge_role: spec.edge_role,
      created_at: "2026-04-29T00:00:00.000Z",
    };
  }
  return {
    src_kind: spec.kind,
    src_id: selfId,
    src_version_id: "1",
    dst_kind: spec.other_kind,
    dst_id: otherId,
    dst_version_id: "1",
    edge_role: spec.edge_role,
    created_at: "2026-04-29T00:00:00.000Z",
  };
}

describe("Catalog detail page wires — RefsTab (TECH-3410)", () => {
  it.each(WIRES)(
    "$kind — detail client module loads with RefsTab import",
    async ({ clientPath, detailDataTestId }) => {
      const mod = (await import(clientPath)) as { default: unknown };
      expect(typeof mod.default).toBe("function");
      expect(typeof detailDataTestId).toBe("string");
    },
  );

  it.each(WIRES)(
    "$kind — RefsTabView renders link to /catalog/$other_kind/{id} for $direction $edge_role",
    (spec) => {
      const otherId = "777";
      const selfId = "42";
      const seeded = row(spec, otherId, selfId);
      const empty = { rows: [], nextCursor: null, loading: false, error: null };
      const html = renderToStaticMarkup(
        <RefsTabView
          incoming={spec.direction === "incoming" ? { ...empty, rows: [seeded] } : empty}
          outgoing={spec.direction === "outgoing" ? { ...empty, rows: [seeded] } : empty}
        />,
      );
      expect(html).toContain('data-testid="refs-tab"');
      expect(html).toContain(`href="/catalog/${spec.other_kind}/${otherId}"`);
      expect(html).toContain(`data-edge-role="${spec.edge_role}"`);
      expect(html).toContain(
        `data-testid="refs-${spec.direction}-row"`,
      );
    },
  );

  it("8 distinct kind literals produce 8 wired detail clients (drift guard)", () => {
    const kinds = new Set(WIRES.map((w) => w.kind));
    expect(kinds.size).toBe(8);
    expect(kinds).toEqual(
      new Set([
        "sprite",
        "asset",
        "audio",
        "button",
        "panel",
        "pool",
        "token",
        "archetype",
      ]),
    );
  });

  it("each detail client source contains <RefsTab kind=\"{kind}\"", async () => {
    const fs = await import("node:fs");
    const path = await import("node:path");
    const root = path.resolve(__dirname, "..", "..", "..");
    const sources: Array<{ kind: CatalogKind; rel: string }> = [
      { kind: "sprite", rel: "app/catalog/sprites/[slug]/SpriteDetailClient.tsx" },
      { kind: "asset", rel: "app/catalog/assets/[slug]/AssetDetailClient.tsx" },
      { kind: "audio", rel: "app/catalog/audio/[slug]/AudioDetailClient.tsx" },
      { kind: "button", rel: "app/catalog/buttons/[slug]/ButtonDetailClient.tsx" },
      { kind: "panel", rel: "app/catalog/panels/[slug]/PanelDetailClient.tsx" },
      { kind: "pool", rel: "app/catalog/pools/[slug]/PoolDetailClient.tsx" },
      { kind: "token", rel: "app/catalog/tokens/[slug]/TokenDetailClient.tsx" },
      { kind: "archetype", rel: "app/catalog/archetypes/[id]/ArchetypeDetailClient.tsx" },
    ];
    for (const { kind, rel } of sources) {
      const p = path.join(root, rel);
      const src = fs.readFileSync(p, "utf8");
      expect(src).toMatch(
        new RegExp(`<RefsTab[^>]*kind=\"${kind}\"`),
      );
    }
  });
});
