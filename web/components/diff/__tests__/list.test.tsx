/**
 * ListFieldDiff render tests (TECH-3302 / Stage 14.3).
 *
 * @see web/components/diff/renderers/list.tsx
 */
import { describe, expect, it } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

import ListFieldDiff from "@/components/diff/renderers/list";

describe("ListFieldDiff (TECH-3302)", () => {
  it("renders + markers for added items and - markers for removed", () => {
    const html = renderToStaticMarkup(
      <ListFieldDiff field="tags" before={["a", "b"]} after={["a", "c"]} />,
    );
    expect(html).toContain('data-field="tags"');
    // 'c' added; 'b' removed.
    expect(html).toMatch(/\+\s*c/);
    expect(html).toMatch(/-\s*b/);
    // 'a' present in both → not duplicated as add or remove marker
    // (assert against marker text content, not className `space-y-*`).
    expect(html).not.toMatch(/>\+\s*a</);
    expect(html).not.toMatch(/>-\s*a</);
  });

  it("added-only: empty before, populated after → only + markers", () => {
    const html = renderToStaticMarkup(
      <ListFieldDiff field="tags" before={[]} after={["x", "y"]} />,
    );
    expect(html).toMatch(/\+\s*x/);
    expect(html).toMatch(/\+\s*y/);
    expect(html).not.toContain("list-diff-removed");
  });

  it("removed-only: populated before, empty after → only - markers", () => {
    const html = renderToStaticMarkup(
      <ListFieldDiff field="tags" before={["m"]} after={[]} />,
    );
    expect(html).toMatch(/-\s*m/);
    expect(html).not.toContain("list-diff-added");
  });

  it("falls back to ScalarFieldDiff when either side not array", () => {
    const html = renderToStaticMarkup(
      <ListFieldDiff field="tags" before="oops" after={["a"]} />,
    );
    expect(html).toContain('data-testid="scalar-diff"');
  });
});
