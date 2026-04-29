import { describe, it, expect } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";
import { PreviewCompare } from "@/components/catalog/PreviewCompare";

describe("<PreviewCompare />", () => {
  it("renders two img tags in default state", () => {
    const html = renderToStaticMarkup(
      <PreviewCompare draftUrl="/draft.png" publishedUrl="/published.png" title="Test Entity" />,
    );
    const imgCount = (html.match(/<img /g) ?? []).length;
    expect(imgCount).toBe(2);
  });

  it("shows animate-pulse skeleton when loading prop is true", () => {
    const html = renderToStaticMarkup(
      <PreviewCompare draftUrl="" publishedUrl="" title="Test" loading />,
    );
    expect(html).toContain("animate-pulse");
  });

  it("renders role=alert when error prop is set", () => {
    const html = renderToStaticMarkup(
      <PreviewCompare draftUrl="" publishedUrl="" title="Test" error="Something went wrong" />,
    );
    expect(html).toContain('role="alert"');
  });

  it("renders diff-toggle button in default render", () => {
    const html = renderToStaticMarkup(
      <PreviewCompare draftUrl="/draft.png" publishedUrl="/published.png" title="Test Entity" />,
    );
    expect(html).toContain("diff");
    expect(html).toContain("<button");
  });
});
