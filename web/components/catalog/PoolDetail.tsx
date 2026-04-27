"use client";

import { useState } from "react";

import AssetMultiSelectModal from "@/components/catalog/AssetMultiSelectModal";
import EntityEditTabs, { type TabKey } from "@/components/catalog/EntityEditTabs";
import PoolMemberTable, { type PoolMemberDraft } from "@/components/catalog/PoolMemberTable";
import type {
  CatalogPoolDto,
  CatalogPoolPatchBody,
  EntityRefSearchRow,
} from "@/types/api/catalog-api";

/**
 * Spine pool detail surface (TECH-1788 + TECH-1789 badge).
 *
 * Header (display_name, slug, retired badge, primary-tagged-by count) +
 * `pool_detail` editor (`primary_subtype`, `owner_category`) + member
 * table + asset multi-select modal. Five-tab strip mirrors AssetDetail.
 *
 * @see ia/projects/asset-pipeline/stage-7.1.md — TECH-1788 §Plan Digest
 */

export type PoolDetailProps = {
  pool: CatalogPoolDto;
  initialTab?: TabKey;
  onSave: (patch: CatalogPoolPatchBody) => void;
  saveError?: string | null;
};

export default function PoolDetail({ pool, initialTab, onSave, saveError }: PoolDetailProps) {
  const [activeTab, setActiveTab] = useState<TabKey>(initialTab ?? "edit");
  const [displayName, setDisplayName] = useState<string>(pool.display_name);
  const [tagsRaw, setTagsRaw] = useState<string>(pool.tags.join(", "));
  const [primarySubtype, setPrimarySubtype] = useState<string>(pool.pool_detail?.primary_subtype ?? "");
  const [ownerCategory, setOwnerCategory] = useState<string>(pool.pool_detail?.owner_category ?? "");
  const [members, setMembers] = useState<PoolMemberDraft[]>(pool.members);
  const [pickerOpen, setPickerOpen] = useState<boolean>(false);

  function diffMembers(): {
    upserts: PoolMemberDraft[];
    removed: string[];
  } {
    const before = new Map(pool.members.map((m) => [m.asset_entity_id, m] as const));
    const after = new Map(members.map((m) => [m.asset_entity_id, m] as const));
    const upserts: PoolMemberDraft[] = [];
    for (const [id, draft] of after) {
      const orig = before.get(id);
      const sameWeight = orig != null && orig.weight === draft.weight;
      const sameCond = orig != null && JSON.stringify(orig.conditions_json ?? {}) === JSON.stringify(draft.conditions_json ?? {});
      if (orig == null || !sameWeight || !sameCond) upserts.push(draft);
    }
    const removed: string[] = [];
    for (const id of before.keys()) if (!after.has(id)) removed.push(id);
    return { upserts, removed };
  }

  function handleSave() {
    const tags = tagsRaw
      .split(",")
      .map((t) => t.trim())
      .filter((t) => t !== "");
    const patch: CatalogPoolPatchBody = {
      updated_at: pool.updated_at,
      display_name: displayName.trim(),
      tags,
      pool_detail: {
        primary_subtype: primarySubtype.trim() === "" ? null : primarySubtype.trim(),
        owner_category: ownerCategory.trim() === "" ? null : ownerCategory.trim(),
      },
    };
    const diff = diffMembers();
    if (diff.upserts.length > 0) {
      patch.members = diff.upserts.map((m) => ({
        asset_entity_id: m.asset_entity_id,
        weight: m.weight,
        conditions_json: m.conditions_json ?? {},
      }));
    }
    if (diff.removed.length > 0) patch.removed_member_entity_ids = diff.removed;
    onSave(patch);
  }

  function handlePicker(rows: EntityRefSearchRow[]) {
    const existingIds = new Set(members.map((m) => m.asset_entity_id));
    const additions: PoolMemberDraft[] = rows
      .filter((r) => !existingIds.has(r.entity_id))
      .map((r) => ({
        asset_entity_id: r.entity_id,
        slug: r.slug,
        display_name: r.display_name,
        weight: 1,
        conditions_json: {},
      }));
    setMembers((cur) => [...cur, ...additions]);
    setPickerOpen(false);
  }

  const editTab = (
    <div data-testid="pool-detail-edit-tab" className="flex flex-col gap-[var(--ds-spacing-md)]">
      <fieldset className="flex flex-col gap-[var(--ds-spacing-xs)]">
        <legend className="text-[var(--ds-text-muted)]">Pool</legend>
        <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
          <span className="text-[var(--ds-text-muted)]">Display name</span>
          <input
            type="text"
            data-testid="pool-detail-display-name"
            value={displayName}
            onChange={(e) => setDisplayName(e.currentTarget.value)}
          />
        </label>
        <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
          <span className="text-[var(--ds-text-muted)]">Tags (comma-separated)</span>
          <input
            type="text"
            data-testid="pool-detail-tags"
            value={tagsRaw}
            onChange={(e) => setTagsRaw(e.currentTarget.value)}
          />
        </label>
        <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
          <span className="text-[var(--ds-text-muted)]">Primary subtype</span>
          <input
            type="text"
            data-testid="pool-detail-primary-subtype"
            value={primarySubtype}
            onChange={(e) => setPrimarySubtype(e.currentTarget.value)}
          />
        </label>
        <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
          <span className="text-[var(--ds-text-muted)]">Owner category</span>
          <input
            type="text"
            data-testid="pool-detail-owner-category"
            value={ownerCategory}
            onChange={(e) => setOwnerCategory(e.currentTarget.value)}
          />
        </label>
      </fieldset>

      <PoolMemberTable members={members} onChange={setMembers} onAddRequested={() => setPickerOpen(true)} />

      <div className="flex items-center gap-[var(--ds-spacing-sm)]">
        <button type="button" data-testid="pool-detail-save" onClick={handleSave}>
          Save changes
        </button>
        {saveError ? (
          <span data-testid="pool-detail-save-error" role="alert" className="text-[var(--ds-text-accent-critical)]">
            {saveError}
          </span>
        ) : null}
      </div>

      <AssetMultiSelectModal
        open={pickerOpen}
        alreadySelected={members.map((m) => m.asset_entity_id)}
        onSubmit={handlePicker}
        onCancel={() => setPickerOpen(false)}
      />
    </div>
  );

  const tabs = {
    edit: editTab,
    versions: (
      <p data-testid="pool-detail-placeholder-versions" className="text-[var(--ds-text-muted)]">
        Owned by Stage 6.4 — version history + diff lands here.
      </p>
    ),
    references: (
      <p data-testid="pool-detail-placeholder-references" className="text-[var(--ds-text-muted)]">
        Owned by Stage 11.1 — cross-entity reference graph lands here.
      </p>
    ),
    lints: (
      <p data-testid="pool-detail-placeholder-lints" className="text-[var(--ds-text-muted)]">
        Owned by Stage 13.1 — per-entity lint findings land here.
      </p>
    ),
    audit: (
      <p data-testid="pool-detail-placeholder-audit" className="text-[var(--ds-text-muted)]">
        Owned by Stage 14.1 — audit_log slice lands here.
      </p>
    ),
  };

  return (
    <div data-testid="pool-detail" className="flex flex-col gap-[var(--ds-spacing-md)]">
      <header className="flex items-baseline gap-[var(--ds-spacing-sm)]">
        <h1 className="text-[length:var(--ds-font-size-h2)] font-semibold">{pool.display_name}</h1>
        <span data-testid="pool-detail-slug" className="font-mono text-[var(--ds-text-muted)]">
          {pool.slug}
        </span>
        {pool.retired_at ? (
          <span data-testid="pool-detail-retired-badge" className="text-[var(--ds-text-accent-warn)]">
            retired
          </span>
        ) : null}
        <span data-testid="pool-detail-primary-tagged-count" className="text-[var(--ds-text-muted)]">
          primary of {pool.primary_tagged_by_count} asset{pool.primary_tagged_by_count === 1 ? "" : "s"}
        </span>
      </header>
      <EntityEditTabs tabs={tabs} activeTab={activeTab} onTabChange={setActiveTab} />
    </div>
  );
}
