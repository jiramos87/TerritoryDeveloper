/**
 * BulkActionBar — render / dialog / clear tests (TECH-4182 §Test Blueprint).
 * Uses renderToStaticMarkup for pure output checks (no fetch mocking needed).
 */
import { describe, expect, it, vi } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

import { BulkActionBar } from "@/components/catalog/BulkActionBar";

// BulkConfirmDialog references fetch; mock it globally
vi.stubGlobal("fetch", vi.fn().mockResolvedValue({ ok: true, json: () => Promise.resolve({ ok: true, data: { updated: 1 } }) }));

describe("<BulkActionBar />", () => {
  it("renders null when selectedIds is empty", () => {
    const html = renderToStaticMarkup(
      <BulkActionBar selectedIds={[]} onClear={vi.fn()} />,
    );
    expect(html).toBe("");
  });

  it("renders toolbar when selectedIds has items", () => {
    const html = renderToStaticMarkup(
      <BulkActionBar selectedIds={["1", "2"]} onClear={vi.fn()} />,
    );
    expect(html).toContain("2 selected");
    expect(html).toContain("Apply");
  });

  it("renders action select with retire/restore/publish options", () => {
    const html = renderToStaticMarkup(
      <BulkActionBar selectedIds={["1"]} onClear={vi.fn()} />,
    );
    expect(html).toContain("Retire");
    expect(html).toContain("Restore");
    expect(html).toContain("Publish");
  });

  it("renders Clear button", () => {
    const html = renderToStaticMarkup(
      <BulkActionBar selectedIds={["42"]} onClear={vi.fn()} />,
    );
    expect(html).toContain("Clear");
  });
});
