/**
 * PoolDiff render tests (TECH-3304 / Stage 14.3).
 *
 * Asserts list-heavy `members` field always renders as `ListFieldDiff`
 * (set diff with + added / - removed) inside `pool-members-block`.
 *
 * @see web/components/diff/renderers/pool.tsx
 */
import { describe, expect, it } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

import PoolDiff from "@/components/diff/renderers/pool";
import type { KindDiff } from "@/lib/diff/kind-schemas";

import addedFixture from "./fixtures/pool-added.json" with { type: "json" };
import removedFixture from "./fixtures/pool-removed.json" with { type: "json" };
import changedFixture from "./fixtures/pool-changed.json" with { type: "json" };

describe("PoolDiff (TECH-3304)", () => {
  it("renders added field names", () => {
    const html = renderToStaticMarkup(
      <PoolDiff diff={addedFixture as KindDiff} />,
    );
    expect(html).toContain('data-testid="pool-renderer"');
    expect(html).toContain("members");
    expect(html).toContain("tags");
    expect(html).toContain("bg-green-50");
  });

  it("renders removed field names", () => {
    const html = renderToStaticMarkup(
      <PoolDiff diff={removedFixture as KindDiff} />,
    );
    expect(html).toContain("predicate");
    expect(html).toContain("bg-red-50");
  });

  it("renders members as list-heavy block + other fields via fallback", () => {
    const html = renderToStaticMarkup(
      <PoolDiff diff={changedFixture as KindDiff} />,
    );
    expect(html).toContain('data-testid="pool-members-block"');
    // members list diff: +asset_c / -asset_b
    expect(html).toContain("+ asset_c");
    expect(html).toContain("- asset_b");
    // scalar fallback for name
    expect(html).toContain("tree_pool_old");
    expect(html).toContain("tree_pool_new");
    // tags list
    expect(html).toContain("+ outdoor");
  });
});
