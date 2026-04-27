"use client";

import { useState } from "react";

import SaveAsSpriteForm from "./SaveAsSpriteForm";
import SaveBindPlaceholder from "./SaveBindPlaceholder";

/**
 * VariantGrid (TECH-1674) — outcome modal surfaced after a successful render.
 *
 * One tile per `output_uris` entry; each tile shows the thumbnail + variant
 * index + four actions:
 *   - Save as sprite — opens nested `<SaveAsSpriteForm>` then POST /api/catalog/sprites
 *   - Save + Bind — disabled stub (Stage 7.x)
 *   - Discard — PATCH /api/render/runs/[run_id] with `variant_disposition_json`
 *   - Re-render variant — re-POST /api/render/runs (variant_count: 1)
 *
 * Caller owns the network calls — this grid is presentational + emits handler
 * callbacks. Save-as-sprite is composed in-place as a nested view (per
 * Implementer Latitude — we keep state simpler by avoiding a route change).
 */

export type VariantTile = {
  variant_idx: number;
  thumbnail_uri: string;
};

export type VariantGridProps = {
  runId: string;
  variants: VariantTile[];
  /** Pre-discarded variants come back disabled (DEC-A41). */
  discardedIdx?: number[];
  onSave: (args: { runId: string; variantIdx: number; slug: string; displayName: string; tags: string[] }) => Promise<void> | void;
  onDiscard: (args: { runId: string; variantIdx: number }) => Promise<void> | void;
  onReRender: (args: { runId: string; variantIdx: number }) => Promise<void> | void;
  onClose: () => void;
};

type NestedView = { kind: "save"; variantIdx: number } | null;

export default function VariantGrid(props: VariantGridProps) {
  const { runId, variants, discardedIdx, onSave, onDiscard, onReRender, onClose } = props;
  const [nested, setNested] = useState<NestedView>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [bindNotice, setBindNotice] = useState<number | null>(null);

  const discardedSet = new Set(discardedIdx ?? []);

  function handleDiscardClick(variantIdx: number) {
    setActionError(null);
    Promise.resolve(onDiscard({ runId, variantIdx })).catch((err: unknown) => {
      setActionError(err instanceof Error ? err.message : "Discard failed");
    });
  }

  function handleReRenderClick(variantIdx: number) {
    setActionError(null);
    Promise.resolve(onReRender({ runId, variantIdx })).catch((err: unknown) => {
      setActionError(err instanceof Error ? err.message : "Re-render failed");
    });
  }

  function handleSaveSubmit(args: { slug: string; displayName: string; tags: string[] }) {
    if (nested?.kind !== "save") return;
    setActionError(null);
    Promise.resolve(
      onSave({
        runId,
        variantIdx: nested.variantIdx,
        slug: args.slug,
        displayName: args.displayName,
        tags: args.tags,
      }),
    )
      .then(() => setNested(null))
      .catch((err: unknown) => {
        setActionError(err instanceof Error ? err.message : "Save failed");
      });
  }

  function handleSaveBindClick(variantIdx: number) {
    setBindNotice(variantIdx);
  }

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-label="Render variants"
      data-testid="variant-grid"
      data-run-id={runId}
      style={{
        position: "fixed",
        inset: 0,
        background: "var(--ds-overlay-panel)",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        zIndex: 50,
      }}
    >
      <div
        style={{
          background: "var(--ds-bg-panel)",
          color: "var(--ds-text-primary)",
          padding: "var(--ds-spacing-lg)",
          maxWidth: "80vw",
          maxHeight: "85vh",
          overflow: "auto",
          border: "1px solid var(--ds-border-strong)",
          minWidth: "40vw",
        }}
      >
        <header style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
          <h2 style={{ margin: 0, fontSize: "var(--ds-font-size-h3)" }}>Render variants</h2>
          <button
            type="button"
            aria-label="Close"
            data-testid="variant-grid-close"
            onClick={onClose}
            style={{ background: "transparent", border: 0, color: "var(--ds-text-muted)", cursor: "pointer" }}
          >
            ×
          </button>
        </header>

        {nested?.kind === "save" ? (
          <div data-testid="variant-grid-save-form">
            <p style={{ marginTop: "var(--ds-spacing-sm)", color: "var(--ds-text-muted)" }}>
              Saving variant v{nested.variantIdx} as new sprite.
            </p>
            <SaveAsSpriteForm
              onSubmit={handleSaveSubmit}
              onCancel={() => setNested(null)}
            />
          </div>
        ) : (
          <ul
            data-testid="variant-grid-tiles"
            style={{
              listStyle: "none",
              padding: 0,
              margin: "var(--ds-spacing-md) 0",
              display: "grid",
              gridTemplateColumns: "repeat(auto-fill, minmax(180px, 1fr))",
              gap: "var(--ds-spacing-sm)",
            }}
          >
            {variants.map((tile) => {
              const isDiscarded = discardedSet.has(tile.variant_idx);
              return (
                <li
                  key={tile.variant_idx}
                  data-testid={`variant-tile-${tile.variant_idx}`}
                  data-discarded={isDiscarded ? "true" : "false"}
                  style={{
                    border: "1px solid var(--ds-border-subtle)",
                    padding: "var(--ds-spacing-xs)",
                    opacity: isDiscarded ? 0.4 : 1,
                  }}
                >
                  {/* eslint-disable-next-line @next/next/no-img-element -- transient render preview; next/image needs known width/height + remote-pattern config we don't have here */}
                  <img
                    src={tile.thumbnail_uri}
                    alt={`Variant ${tile.variant_idx}`}
                    data-testid={`variant-thumbnail-${tile.variant_idx}`}
                    style={{ width: "100%", display: "block", background: "var(--ds-bg-canvas)" }}
                  />
                  <p style={{ margin: "var(--ds-spacing-xs) 0", fontSize: "var(--ds-font-size-body-sm)" }}>
                    v{tile.variant_idx}
                  </p>
                  <div style={{ display: "flex", flexWrap: "wrap", gap: "var(--ds-spacing-xs)" }}>
                    <button
                      type="button"
                      data-testid={`variant-action-save-${tile.variant_idx}`}
                      disabled={isDiscarded}
                      onClick={() => setNested({ kind: "save", variantIdx: tile.variant_idx })}
                    >
                      Save as sprite
                    </button>
                    <button
                      type="button"
                      data-testid={`variant-action-save-bind-${tile.variant_idx}`}
                      disabled
                      title="Save + Bind to asset slot — coming in Stage 7.x"
                      onClick={() => handleSaveBindClick(tile.variant_idx)}
                    >
                      Save + Bind
                    </button>
                    <button
                      type="button"
                      data-testid={`variant-action-discard-${tile.variant_idx}`}
                      disabled={isDiscarded}
                      onClick={() => handleDiscardClick(tile.variant_idx)}
                    >
                      Discard
                    </button>
                    <button
                      type="button"
                      data-testid={`variant-action-rerender-${tile.variant_idx}`}
                      onClick={() => handleReRenderClick(tile.variant_idx)}
                    >
                      Re-render
                    </button>
                  </div>
                </li>
              );
            })}
          </ul>
        )}

        {bindNotice !== null ? (
          <SaveBindPlaceholder variantIdx={bindNotice} onDismiss={() => setBindNotice(null)} />
        ) : null}

        {actionError ? (
          <p
            data-testid="variant-grid-error"
            role="alert"
            style={{ color: "var(--ds-text-accent-critical)" }}
          >
            {actionError}
          </p>
        ) : null}
      </div>
    </div>
  );
}
