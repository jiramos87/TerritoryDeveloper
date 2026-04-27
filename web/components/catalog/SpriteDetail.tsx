"use client";

import { useState } from "react";

import EntityEditTabs, { type TabKey } from "@/components/catalog/EntityEditTabs";
import RenderForm from "@/components/catalog/RenderForm";
import SpriteEditForm, { type SpriteEditFormValue } from "@/components/catalog/SpriteEditForm";
import VariantGrid, { type VariantTile } from "@/components/catalog/VariantGrid";
import type { JsonSchemaNode, UiHints } from "@/lib/json-schema-form/types";

export type SpriteDetailView = SpriteEditFormValue & {
  entity_id: string;
  retired_at: string | null;
  current_published_version_id: string | null;
};

export type SpriteArchetypeBinding = {
  archetype_id: string;
  archetype_version_id: string;
  params_schema: JsonSchemaNode;
  ui_hints?: UiHints;
  default_params?: unknown;
};

export type SpriteRenderHandlers = {
  /** Caller fetches `render_run.output_uris` after `RenderForm.onComplete`. */
  loadVariants: (runId: string) => Promise<VariantTile[]>;
  onSaveAsSprite: (args: { runId: string; variantIdx: number; slug: string; displayName: string; tags: string[] }) => Promise<void> | void;
  onDiscardVariant: (args: { runId: string; variantIdx: number }) => Promise<void> | void;
  onReRenderVariant: (args: { runId: string; variantIdx: number }) => Promise<void> | void;
};

export type SpriteDetailProps = {
  sprite: SpriteDetailView;
  initialTab?: TabKey;
  onSave: (patch: {
    display_name: string;
    tags: string[];
    sprite_detail: SpriteEditFormValue["sprite_detail"];
  }) => void;
  saveError?: string | null;
  /** When supplied, the Edit tab surfaces a "New render" button. */
  archetypeBinding?: SpriteArchetypeBinding;
  renderHandlers?: SpriteRenderHandlers;
};

/**
 * Sprite detail surface (TECH-1672) — five-tab strip; Edit tab is live in this
 * Stage; Versions / References / Lints / Audit show explicit Stage-ownership
 * placeholders.
 */
export default function SpriteDetail({
  sprite,
  initialTab,
  onSave,
  saveError,
  archetypeBinding,
  renderHandlers,
}: SpriteDetailProps) {
  const [activeTab, setActiveTab] = useState<TabKey>(initialTab ?? "edit");
  const [renderOpen, setRenderOpen] = useState<boolean>(false);
  const [variantRunId, setVariantRunId] = useState<string | null>(null);
  const [variants, setVariants] = useState<VariantTile[]>([]);
  const [variantLoadError, setVariantLoadError] = useState<string | null>(null);
  const slugFrozen = sprite.current_published_version_id !== null;

  function handleRenderComplete(runId: string) {
    setRenderOpen(false);
    setVariantLoadError(null);
    if (!renderHandlers) return;
    Promise.resolve(renderHandlers.loadVariants(runId))
      .then((tiles) => {
        setVariants(tiles);
        setVariantRunId(runId);
      })
      .catch((err: unknown) => {
        setVariantLoadError(err instanceof Error ? err.message : "Failed to load variants");
      });
  }

  const editTab = (
    <div data-testid="sprite-detail-edit-tab" style={{ display: "flex", flexDirection: "column", gap: "var(--ds-spacing-sm)" }}>
      {archetypeBinding && renderHandlers ? (
        <div data-testid="sprite-detail-render-controls" style={{ display: "flex", gap: "var(--ds-spacing-xs)" }}>
          <button
            type="button"
            data-testid="sprite-detail-new-render"
            onClick={() => setRenderOpen(true)}
            disabled={renderOpen}
          >
            New render
          </button>
          {variantLoadError ? (
            <span data-testid="sprite-detail-variant-error" role="alert" style={{ color: "var(--ds-text-accent-critical)" }}>
              {variantLoadError}
            </span>
          ) : null}
        </div>
      ) : null}

      {renderOpen && archetypeBinding ? (
        <RenderForm
          archetypeId={archetypeBinding.archetype_id}
          archetypeVersionId={archetypeBinding.archetype_version_id}
          paramsSchema={archetypeBinding.params_schema}
          uiHints={archetypeBinding.ui_hints}
          defaultParams={archetypeBinding.default_params}
          onComplete={handleRenderComplete}
          onCancel={() => setRenderOpen(false)}
        />
      ) : null}

      <SpriteEditForm
        slugFrozen={slugFrozen}
        initial={sprite}
        onSubmit={onSave}
        submitError={saveError ?? null}
      />

      {variantRunId && renderHandlers ? (
        <VariantGrid
          runId={variantRunId}
          variants={variants}
          onSave={renderHandlers.onSaveAsSprite}
          onDiscard={renderHandlers.onDiscardVariant}
          onReRender={renderHandlers.onReRenderVariant}
          onClose={() => {
            setVariantRunId(null);
            setVariants([]);
          }}
        />
      ) : null}
    </div>
  );

  const tabs = {
    edit: editTab,
    versions: (
      <p data-testid="sprite-detail-placeholder-versions" className="text-[var(--ds-text-muted)]">
        Owned by Stage 6.4 — version history + diff lands here.
      </p>
    ),
    references: (
      <p data-testid="sprite-detail-placeholder-references" className="text-[var(--ds-text-muted)]">
        Owned by Stage 11.1 — cross-entity reference graph lands here.
      </p>
    ),
    lints: (
      <p data-testid="sprite-detail-placeholder-lints" className="text-[var(--ds-text-muted)]">
        Owned by Stage 13.1 — per-entity lint findings land here.
      </p>
    ),
    audit: (
      <p data-testid="sprite-detail-placeholder-audit" className="text-[var(--ds-text-muted)]">
        Owned by Stage 14.1 — audit_log slice lands here.
      </p>
    ),
  };

  return (
    <div data-testid="sprite-detail" className="flex flex-col gap-[var(--ds-spacing-md)]">
      <header className="flex items-baseline gap-[var(--ds-spacing-sm)]">
        <h1 className="text-[length:var(--ds-font-size-h2)] font-semibold">{sprite.display_name}</h1>
        <span data-testid="sprite-detail-slug" className="font-mono text-[var(--ds-text-muted)]">
          {sprite.slug}
        </span>
        {sprite.retired_at ? (
          <span data-testid="sprite-detail-retired-badge" className="text-[var(--ds-text-accent-warn)]">
            retired
          </span>
        ) : null}
      </header>
      <EntityEditTabs tabs={tabs} activeTab={activeTab} onTabChange={setActiveTab} />
    </div>
  );
}
