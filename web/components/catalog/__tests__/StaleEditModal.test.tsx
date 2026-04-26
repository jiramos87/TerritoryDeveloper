import { describe, it, expect } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

import StaleEditModal, {
  buildPathTable,
  applyPicks,
} from "@/components/catalog/StaleEditModal";

/**
 * Vitest runs in node environment with no jsdom — interactive flows are
 * verified by exercising the exported pure helpers (`buildPathTable`,
 * `applyPicks`) directly. Static-markup assertions cover the DOM scaffolding
 * the modal renders on first paint (dialog role, CTA testids, header text).
 */

describe("<StaleEditModal /> static markup", () => {
  it("renders dialog scaffold with the three primary CTAs", () => {
    const html = renderToStaticMarkup(
      <StaleEditModal
        loaded={{ name: "old" }}
        current={{ name: "new" }}
        pending={{ name: "draft" }}
        currentUpdatedAt="2026-04-26T12:00:00.000Z"
        onReload={() => {}}
        onResave={() => {}}
      />,
    );
    expect(html).toContain('data-testid="stale-edit-modal"');
    expect(html).toContain('role="dialog"');
    expect(html).toContain('aria-modal="true"');
    expect(html).toContain('data-testid="stale-modal-reload"');
    expect(html).toContain('data-testid="stale-modal-toggle-diff"');
    expect(html).toContain('data-testid="stale-modal-resave"');
    expect(html).toContain("Save conflict");
  });

  it("does not render the diff table on initial render (Show diff toggle collapsed)", () => {
    const html = renderToStaticMarkup(
      <StaleEditModal
        loaded={{ name: "old" }}
        current={{ name: "new" }}
        pending={{ name: "draft" }}
        currentUpdatedAt="2026-04-26T12:00:00.000Z"
        onReload={() => {}}
        onResave={() => {}}
      />,
    );
    expect(html).not.toContain('data-testid="stale-modal-diff-table"');
  });
});

describe("buildPathTable", () => {
  it("renders three labelled columns (loaded / current / pending) per changed path", () => {
    const loaded = { name: "old", count: 1 };
    const current = { name: "new", count: 1 };
    const pending = { name: "old", count: 5 };

    const rows = buildPathTable(loaded, current, pending);
    const byPath = Object.fromEntries(rows.map((r) => [r.path, r]));

    expect(byPath["name"]).toMatchObject({
      path: "name",
      loadedValue: "old",
      currentValue: "new",
      pendingValue: "old",
      currentChanged: true,
      pendingChanged: false,
    });
    expect(byPath["count"]).toMatchObject({
      path: "count",
      loadedValue: 1,
      currentValue: 1,
      pendingValue: 5,
      currentChanged: false,
      pendingChanged: true,
    });
  });

  it("returns empty when all three payloads are deeply equal", () => {
    const v = { a: 1, b: { c: [1, 2] } };
    expect(buildPathTable(v, v, v)).toEqual([]);
  });

  it("captures paths only changed on one side", () => {
    const loaded = { a: 1, b: 2 };
    const current = { a: 9, b: 2 };
    const pending = { a: 1, b: 3 };

    const rows = buildPathTable(loaded, current, pending);
    const byPath = Object.fromEntries(rows.map((r) => [r.path, r]));
    expect(byPath["a"].currentChanged).toBe(true);
    expect(byPath["a"].pendingChanged).toBe(false);
    expect(byPath["b"].currentChanged).toBe(false);
    expect(byPath["b"].pendingChanged).toBe(true);
  });
});

describe("applyPicks (per-field merge)", () => {
  it("keeps pending value when no pick is recorded for a path", () => {
    const merged = applyPicks(
      { name: "draft", count: 5 },
      { name: "new", count: 1 },
      {},
    );
    expect(merged).toEqual({ name: "draft", count: 5 });
  });

  it("overlays the current value at paths picked as 'current'", () => {
    const merged = applyPicks(
      { name: "draft", count: 5 },
      { name: "new", count: 1 },
      { name: "current" },
    );
    expect(merged).toEqual({ name: "new", count: 5 });
  });

  it("merges per-field — current for A and pending for B", () => {
    const pending = { fieldA: "draft-a", fieldB: "draft-b" };
    const current = { fieldA: "server-a", fieldB: "server-b" };
    const merged = applyPicks(pending, current, {
      fieldA: "current",
      fieldB: "pending",
    });
    expect(merged).toEqual({ fieldA: "server-a", fieldB: "draft-b" });
  });

  it("supports nested paths via dot+bracket syntax", () => {
    const pending = { meta: { tags: ["draft-tag"] }, name: "draft" };
    const current = { meta: { tags: ["server-tag"] }, name: "server" };
    const merged = applyPicks(pending, current, {
      "meta.tags[0]": "current",
    });
    expect(merged).toEqual({ meta: { tags: ["server-tag"] }, name: "draft" });
  });
});
