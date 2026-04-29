/**
 * ScalarFieldDiff render tests (TECH-3302 / Stage 14.3).
 *
 * @see web/components/diff/renderers/scalar.tsx
 */
import { describe, expect, it } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

import ScalarFieldDiff from "@/components/diff/renderers/scalar";

describe("ScalarFieldDiff (TECH-3302)", () => {
  it("renders both before/after with diff palette + strikethrough on before", () => {
    const html = renderToStaticMarkup(
      <ScalarFieldDiff field="name" before="alpha" after="beta" />,
    );
    expect(html).toContain('data-field="name"');
    expect(html).toContain("alpha");
    expect(html).toContain("beta");
    expect(html).toContain("line-through");
    expect(html).toContain("bg-red-50");
    expect(html).toContain("bg-green-50");
  });

  it("renders (none) placeholder for null/undefined values (added/removed shapes)", () => {
    const addedHtml = renderToStaticMarkup(
      <ScalarFieldDiff field="x" before={null} after="new_val" />,
    );
    expect(addedHtml).toContain("(none)");
    expect(addedHtml).toContain("new_val");

    const removedHtml = renderToStaticMarkup(
      <ScalarFieldDiff field="x" before="old_val" after={undefined} />,
    );
    expect(removedHtml).toContain("(none)");
    expect(removedHtml).toContain("old_val");
  });

  it("stringifies non-primitive values via JSON.stringify", () => {
    const html = renderToStaticMarkup(
      <ScalarFieldDiff field="meta" before={{ a: 1 }} after={{ a: 2 }} />,
    );
    expect(html).toContain("&quot;a&quot;:1");
    expect(html).toContain("&quot;a&quot;:2");
  });
});
