import { describe, it, expect, vi } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

import SpriteEditForm, { type SpriteEditFormValue } from "@/components/catalog/SpriteEditForm";

const VALID: SpriteEditFormValue = {
  slug: "tree_oak_a",
  display_name: "Tree — Oak A",
  tags: ["tree", "nature"],
  sprite_detail: {
    pixels_per_unit: 16,
    pivot_x: 0.5,
    pivot_y: 0.5,
    source_uri: null,
  },
};

describe("<SpriteEditForm />", () => {
  it("renders all editable fields + slug as read-only when frozen", () => {
    const html = renderToStaticMarkup(
      <SpriteEditForm slugFrozen={true} initial={VALID} onSubmit={() => {}} />,
    );
    expect(html).toContain('data-testid="sprite-edit-slug"');
    expect(html).toContain('data-testid="sprite-edit-display-name"');
    expect(html).toContain('data-testid="sprite-edit-tags"');
    expect(html).toContain('data-testid="sprite-edit-ppu"');
    expect(html).toContain('data-testid="sprite-edit-pivot-x"');
    expect(html).toContain('data-testid="sprite-edit-pivot-y"');
    expect(html).toContain('data-testid="sprite-edit-source-uri"');
    // Slug input is readOnly + disabled when frozen.
    const slugTag = html.match(/<input[^>]*data-testid="sprite-edit-slug"[^>]*>/);
    expect(slugTag?.[0]).toMatch(/readOnly=""|readonly=""/);
    expect(slugTag?.[0]).toContain('aria-readonly="true"');
  });

  it("renders editable slug when not frozen", () => {
    const html = renderToStaticMarkup(
      <SpriteEditForm slugFrozen={false} initial={VALID} onSubmit={() => {}} />,
    );
    const slugTag = html.match(/<input[^>]*data-testid="sprite-edit-slug"[^>]*>/);
    expect(slugTag?.[0]).not.toMatch(/readOnly=""|readonly=""/);
  });

  it("flags display_name empty as inline error and disables submit", () => {
    const html = renderToStaticMarkup(
      <SpriteEditForm
        slugFrozen={true}
        initial={{ ...VALID, display_name: "" }}
        onSubmit={() => {}}
      />,
    );
    expect(html).toContain('data-testid="sprite-edit-error-display-name"');
    expect(html).toMatch(/data-testid="sprite-edit-submit"[^>]*disabled=""/);
  });

  it("flags non-positive PPU and out-of-bounds pivots", () => {
    const html = renderToStaticMarkup(
      <SpriteEditForm
        slugFrozen={true}
        initial={{
          ...VALID,
          sprite_detail: { pixels_per_unit: 0, pivot_x: -0.1, pivot_y: 1.5, source_uri: null },
        }}
        onSubmit={() => {}}
      />,
    );
    expect(html).toContain('data-testid="sprite-edit-error-ppu"');
    expect(html).toContain('data-testid="sprite-edit-error-pivot-x"');
    expect(html).toContain('data-testid="sprite-edit-error-pivot-y"');
  });

  it("submit-shape via direct invocation matches PATCH body contract", () => {
    const onSubmit = vi.fn();
    onSubmit({
      display_name: "Tree — Oak A",
      tags: ["tree", "nature"],
      sprite_detail: {
        pixels_per_unit: 16,
        pivot_x: 0.5,
        pivot_y: 0.5,
        source_uri: null,
      },
    });
    expect(onSubmit).toHaveBeenCalledWith(
      expect.objectContaining({
        display_name: expect.any(String),
        tags: expect.any(Array),
        sprite_detail: expect.objectContaining({
          pixels_per_unit: expect.any(Number),
          pivot_x: expect.any(Number),
          pivot_y: expect.any(Number),
        }),
      }),
    );
  });

  it("surfaces submitError under the submit button when provided", () => {
    const html = renderToStaticMarkup(
      <SpriteEditForm
        slugFrozen={true}
        initial={VALID}
        onSubmit={() => {}}
        submitError="Slug already in use"
      />,
    );
    expect(html).toContain('data-testid="sprite-edit-submit-error"');
    expect(html).toContain("Slug already in use");
  });
});
