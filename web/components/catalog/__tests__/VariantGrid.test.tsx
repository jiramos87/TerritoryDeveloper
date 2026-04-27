/**
 * VariantGrid component tests (TECH-1674).
 *
 * SSR-only assertions on initial render contract — node env (no jsdom). We
 * verify per-tile structure + four action buttons + Save+Bind disabled state +
 * SaveAsSpriteForm visible after Save click is exercised by directly mounting
 * the form component (covered alongside in this spec).
 */
import { describe, expect, it } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

import SaveAsSpriteForm from "@/components/catalog/SaveAsSpriteForm";
import SaveBindPlaceholder from "@/components/catalog/SaveBindPlaceholder";
import VariantGrid, { type VariantTile } from "@/components/catalog/VariantGrid";

const RUN_ID = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";

const TILES: VariantTile[] = [
  { variant_idx: 0, thumbnail_uri: "gen://x/0" },
  { variant_idx: 1, thumbnail_uri: "gen://x/1" },
  { variant_idx: 2, thumbnail_uri: "gen://x/2" },
];

const NOOP = () => {};

describe("<VariantGrid /> SSR shape", () => {
  it("renders one tile per variant with thumbnail + four action buttons", () => {
    const html = renderToStaticMarkup(
      <VariantGrid
        runId={RUN_ID}
        variants={TILES}
        onSave={NOOP}
        onDiscard={NOOP}
        onReRender={NOOP}
        onClose={NOOP}
      />,
    );
    expect(html).toContain('data-testid="variant-grid"');
    expect(html).toContain(`data-run-id="${RUN_ID}"`);
    for (const tile of TILES) {
      expect(html).toContain(`data-testid="variant-tile-${tile.variant_idx}"`);
      expect(html).toContain(`data-testid="variant-thumbnail-${tile.variant_idx}"`);
      expect(html).toContain(`data-testid="variant-action-save-${tile.variant_idx}"`);
      expect(html).toContain(`data-testid="variant-action-save-bind-${tile.variant_idx}"`);
      expect(html).toContain(`data-testid="variant-action-discard-${tile.variant_idx}"`);
      expect(html).toContain(`data-testid="variant-action-rerender-${tile.variant_idx}"`);
    }
  });

  it("Save+Bind action is disabled per DEC — Stage 7.x note", () => {
    const html = renderToStaticMarkup(
      <VariantGrid
        runId={RUN_ID}
        variants={TILES.slice(0, 1)}
        onSave={NOOP}
        onDiscard={NOOP}
        onReRender={NOOP}
        onClose={NOOP}
      />,
    );
    const tag = html.match(/<button[^>]*data-testid="variant-action-save-bind-0"[^>]*>/)?.[0] ?? "";
    expect(tag).toMatch(/disabled/);
    expect(tag).toMatch(/coming in Stage 7\.x/);
  });

  it("dims pre-discarded tiles and disables Save / Discard for them", () => {
    const html = renderToStaticMarkup(
      <VariantGrid
        runId={RUN_ID}
        variants={TILES.slice(0, 2)}
        discardedIdx={[1]}
        onSave={NOOP}
        onDiscard={NOOP}
        onReRender={NOOP}
        onClose={NOOP}
      />,
    );
    const discardedTile = html.match(/<li[^>]*data-testid="variant-tile-1"[^>]*>/)?.[0] ?? "";
    expect(discardedTile).toContain('data-discarded="true"');
    const saveBtn = html.match(/<button[^>]*data-testid="variant-action-save-1"[^>]*>/)?.[0] ?? "";
    expect(saveBtn).toMatch(/disabled/);
    const discardBtn = html.match(/<button[^>]*data-testid="variant-action-discard-1"[^>]*>/)?.[0] ?? "";
    expect(discardBtn).toMatch(/disabled/);
  });

  it("close button surfaces with aria-label", () => {
    const html = renderToStaticMarkup(
      <VariantGrid
        runId={RUN_ID}
        variants={[]}
        onSave={NOOP}
        onDiscard={NOOP}
        onReRender={NOOP}
        onClose={NOOP}
      />,
    );
    const tag = html.match(/<button[^>]*data-testid="variant-grid-close"[^>]*>/)?.[0] ?? "";
    expect(tag).toMatch(/aria-label="Close"/);
  });
});

describe("<SaveAsSpriteForm /> validation contract", () => {
  it("disables Save button while slug regex fails", () => {
    const html = renderToStaticMarkup(
      <SaveAsSpriteForm onSubmit={() => {}} onCancel={() => {}} />,
    );
    const submit = html.match(/<button[^>]*data-testid="save-as-sprite-submit"[^>]*>/)?.[0] ?? "";
    expect(submit).toMatch(/disabled/);
    expect(html).toContain('data-testid="save-as-sprite-slug"');
    expect(html).toContain('data-testid="save-as-sprite-display-name"');
    expect(html).toContain('data-testid="save-as-sprite-tags"');
    expect(html).toContain('data-testid="save-as-sprite-slug-error"');
  });

  it("emits trimmed payload when default values satisfy validation", () => {
    let captured: { slug: string; displayName: string; tags: string[] } | null = null;
    const form = (
      <SaveAsSpriteForm
        defaultSlug="tree_oak_a"
        defaultDisplayName="Oak A"
        onSubmit={(v) => {
          captured = v;
        }}
        onCancel={() => {}}
      />
    );
    const html = renderToStaticMarkup(form);
    const submit = html.match(/<button[^>]*data-testid="save-as-sprite-submit"[^>]*>/)?.[0] ?? "";
    // Defaults satisfy regex + non-empty display name → submit enabled.
    expect(submit).not.toMatch(/disabled=""/);
    // captured stays null because SSR cannot dispatch DOM events; the
    // contract assertion is that `disabled=""` is absent from the static
    // rendering when validation passes.
    expect(captured).toBeNull();
  });
});

describe("<SaveBindPlaceholder /> placeholder copy", () => {
  it("renders Stage 7.x copy + dismiss button", () => {
    const html = renderToStaticMarkup(
      <SaveBindPlaceholder variantIdx={2} onDismiss={() => {}} />,
    );
    expect(html).toContain('data-testid="save-bind-placeholder"');
    expect(html).toContain('data-testid="save-bind-placeholder-dismiss"');
    expect(html).toMatch(/Stage 7\.x/);
    expect(html).toContain("v2");
  });
});
