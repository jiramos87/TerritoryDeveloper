"use client";

import { useState } from "react";

import AssetEditForm from "@/components/catalog/AssetEditForm";
import EntityEditTabs, { type TabKey } from "@/components/catalog/EntityEditTabs";
import type { CatalogAssetSpineDto, CatalogAssetSpinePatchBody } from "@/types/api/catalog-api";

/**
 * Spine-aware asset detail surface (TECH-1786 + TECH-1789). Five-tab strip
 * mirroring `<SpriteDetail>`; Edit tab is live, others are Stage-owned
 * placeholders.
 */

export type AssetDetailProps = {
  asset: CatalogAssetSpineDto;
  initialTab?: TabKey;
  onSave: (patch: CatalogAssetSpinePatchBody) => void;
  saveError?: string | null;
};

export default function AssetDetail({ asset, initialTab, onSave, saveError }: AssetDetailProps) {
  const [activeTab, setActiveTab] = useState<TabKey>(initialTab ?? "edit");

  const tabs = {
    edit: (
      <div data-testid="asset-detail-edit-tab" className="flex flex-col gap-[var(--ds-spacing-sm)]">
        <AssetEditForm initial={asset} onSubmit={onSave} submitError={saveError ?? null} />
      </div>
    ),
    versions: (
      <p data-testid="asset-detail-placeholder-versions" className="text-[var(--ds-text-muted)]">
        Owned by Stage 6.4 — version history + diff lands here.
      </p>
    ),
    references: (
      <p data-testid="asset-detail-placeholder-references" className="text-[var(--ds-text-muted)]">
        Owned by Stage 11.1 — cross-entity reference graph lands here.
      </p>
    ),
    lints: (
      <p data-testid="asset-detail-placeholder-lints" className="text-[var(--ds-text-muted)]">
        Owned by Stage 13.1 — per-entity lint findings land here.
      </p>
    ),
    audit: (
      <p data-testid="asset-detail-placeholder-audit" className="text-[var(--ds-text-muted)]">
        Owned by Stage 14.1 — audit_log slice lands here.
      </p>
    ),
  };

  return (
    <div data-testid="asset-detail" className="flex flex-col gap-[var(--ds-spacing-md)]">
      <header className="flex items-baseline gap-[var(--ds-spacing-sm)]">
        <h1 className="text-[length:var(--ds-font-size-h2)] font-semibold">{asset.display_name}</h1>
        <span data-testid="asset-detail-slug" className="font-mono text-[var(--ds-text-muted)]">
          {asset.slug}
        </span>
        {asset.retired_at ? (
          <span data-testid="asset-detail-retired-badge" className="text-[var(--ds-text-accent-warn)]">
            retired
          </span>
        ) : null}
      </header>
      <EntityEditTabs tabs={tabs} activeTab={activeTab} onTabChange={setActiveTab} />
    </div>
  );
}
