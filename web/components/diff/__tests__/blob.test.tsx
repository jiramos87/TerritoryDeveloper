/**
 * BlobFieldDiff render tests (TECH-3302 / Stage 14.3).
 *
 * @see web/components/diff/renderers/blob.tsx
 */
import { describe, expect, it } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

import BlobFieldDiff from "@/components/diff/renderers/blob";

describe("BlobFieldDiff (TECH-3302)", () => {
  it("renders both blob refs side-by-side", () => {
    const html = renderToStaticMarkup(
      <BlobFieldDiff field="image_path" before="a.png" after="b.png" />,
    );
    expect(html).toContain('data-field="image_path"');
    expect(html).toContain("before: a.png");
    expect(html).toContain("after: b.png");
  });

  it("renders (none) when before is null (added shape)", () => {
    const html = renderToStaticMarkup(
      <BlobFieldDiff field="audio_path" before={null} after="new.mp3" />,
    );
    expect(html).toContain("before: (none)");
    expect(html).toContain("after: new.mp3");
  });

  it("renders (none) when after is null (removed shape)", () => {
    const html = renderToStaticMarkup(
      <BlobFieldDiff field="audio_path" before="old.mp3" after={null} />,
    );
    expect(html).toContain("before: old.mp3");
    expect(html).toContain("after: (none)");
  });
});
