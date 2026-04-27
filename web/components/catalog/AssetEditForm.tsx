"use client";

import { useMemo, useState } from "react";

import EntityRefPicker, { type EntityRefRow } from "@/components/catalog/EntityRefPicker";
import SubtypeMembershipEditor from "@/components/catalog/SubtypeMembershipEditor";
import PrimarySubtypeSelect from "@/components/catalog/PrimarySubtypeSelect";
import type { CatalogAssetSpineDto, CatalogAssetSpinePatchBody } from "@/types/api/catalog-api";

/**
 * Asset edit form (TECH-1786 + TECH-1789).
 *
 * Composes display_name + tags + asset_detail (footprint, placement, slots) +
 * economy_detail + subtype memberships + primary subtype. Slot bindings use
 * `<EntityRefPicker accepts_kind={['sprite']}>` per DEC-A45 / DEC-A7 (typed
 * slot columns). Slug is read-only post-publish per DEC-A24.
 */

export type AssetEditFormProps = {
  /** Current asset DTO loaded from server. */
  initial: CatalogAssetSpineDto;
  /** Submit handler — emits a server-shape patch ready for `/api/catalog/assets/[slug]`. */
  onSubmit: (patch: CatalogAssetSpinePatchBody) => void;
  submitError?: string | null;
};

const SLOTS: ReadonlyArray<{ col: keyof NonNullable<CatalogAssetSpineDto["asset_detail"]>; label: string }> = [
  { col: "world_sprite_entity_id", label: "World sprite" },
  { col: "button_target_sprite_entity_id", label: "Button target" },
  { col: "button_pressed_sprite_entity_id", label: "Button pressed" },
  { col: "button_disabled_sprite_entity_id", label: "Button disabled" },
  { col: "button_hover_sprite_entity_id", label: "Button hover" },
];

export default function AssetEditForm({ initial, onSubmit, submitError }: AssetEditFormProps) {
  const slugFrozen = initial.current_published_version_id !== null;

  const [displayName, setDisplayName] = useState<string>(initial.display_name);
  const [tagsRaw, setTagsRaw] = useState<string>(initial.tags.join(", "));

  const detail = initial.asset_detail;
  const [category, setCategory] = useState<string>(detail?.category ?? "");
  const [footprintW, setFootprintW] = useState<number>(detail?.footprint_w ?? 1);
  const [footprintH, setFootprintH] = useState<number>(detail?.footprint_h ?? 1);
  const [placementMode, setPlacementMode] = useState<string>(detail?.placement_mode ?? "");
  const [unlocksAfter, setUnlocksAfter] = useState<string>(detail?.unlocks_after ?? "");
  const [hasButton, setHasButton] = useState<boolean>(detail?.has_button ?? true);

  // Slot picker state — current values + resolved rows from initial.sprite_slot_resolutions.
  const [slotValues, setSlotValues] = useState<Record<string, EntityRefRow | null>>(() => {
    const out: Record<string, EntityRefRow | null> = {};
    for (const s of SLOTS) {
      out[s.col] = initial.sprite_slot_resolutions[s.col] ?? null;
    }
    return out;
  });

  const eco = initial.economy_detail;
  const [baseCost, setBaseCost] = useState<number>(eco?.base_cost_cents ?? 0);
  const [monthlyUpkeep, setMonthlyUpkeep] = useState<number>(eco?.monthly_upkeep_cents ?? 0);
  const [refundPct, setRefundPct] = useState<number>(eco?.demolition_refund_pct ?? 0);
  const [ticks, setTicks] = useState<number>(eco?.construction_ticks ?? 0);

  // Subtype memberships + primary (TECH-1789).
  const [memberships, setMemberships] = useState<EntityRefRow[]>(initial.subtype_memberships);
  const [primaryId, setPrimaryId] = useState<string | null>(detail?.primary_subtype_pool_id ?? null);

  const errors = useMemo(() => {
    const errs: Record<string, string> = {};
    if (displayName.trim() === "") errs.display_name = "Display name required";
    if (category.trim() === "") errs.category = "Category required";
    if (!(footprintW > 0)) errs.footprint_w = "Footprint W must be > 0";
    if (!(footprintH > 0)) errs.footprint_h = "Footprint H must be > 0";
    if (refundPct < 0 || refundPct > 100) errs.demolition_refund_pct = "0–100";
    if (primaryId != null && !memberships.some((m) => m.entity_id === primaryId)) {
      errs.primary_subtype_pool_id = "Primary must be a current membership";
    }
    return errs;
  }, [displayName, category, footprintW, footprintH, refundPct, primaryId, memberships]);

  const hasErrors = Object.keys(errors).length > 0;

  function diffMemberships(): { added: string[]; removed: string[] } {
    const before = new Set(initial.subtype_memberships.map((r) => r.entity_id));
    const after = new Set(memberships.map((r) => r.entity_id));
    const added: string[] = [];
    const removed: string[] = [];
    for (const id of after) if (!before.has(id)) added.push(id);
    for (const id of before) if (!after.has(id)) removed.push(id);
    return { added, removed };
  }

  function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (hasErrors) return;
    const tags = tagsRaw
      .split(",")
      .map((t) => t.trim())
      .filter((t) => t !== "");

    const patch: CatalogAssetSpinePatchBody = {
      updated_at: initial.updated_at,
      display_name: displayName.trim(),
      tags,
      asset_detail: {
        category: category.trim(),
        footprint_w: footprintW,
        footprint_h: footprintH,
        placement_mode: placementMode.trim() === "" ? null : placementMode.trim(),
        unlocks_after: unlocksAfter.trim() === "" ? null : unlocksAfter.trim(),
        has_button: hasButton,
        primary_subtype_pool_id: primaryId,
      },
      economy_detail: {
        base_cost_cents: Math.trunc(baseCost),
        monthly_upkeep_cents: Math.trunc(monthlyUpkeep),
        demolition_refund_pct: refundPct,
        construction_ticks: ticks,
      },
    };
    for (const s of SLOTS) {
      (patch.asset_detail as Record<string, unknown>)[s.col] = slotValues[s.col]?.entity_id ?? null;
    }
    const memDiff = diffMemberships();
    if (memDiff.added.length > 0 || memDiff.removed.length > 0) {
      patch.subtype_membership = memDiff;
    }
    onSubmit(patch);
  }

  return (
    <form data-testid="asset-edit-form" onSubmit={handleSubmit} className="flex flex-col gap-[var(--ds-spacing-sm)]">
      <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
        <span className="text-[var(--ds-text-muted)]">Slug</span>
        <input
          type="text"
          data-testid="asset-edit-slug"
          value={initial.slug}
          readOnly={slugFrozen}
          disabled={slugFrozen}
          aria-readonly={slugFrozen}
        />
      </label>

      <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
        <span className="text-[var(--ds-text-muted)]">Display name</span>
        <input
          type="text"
          data-testid="asset-edit-display-name"
          value={displayName}
          onChange={(e) => setDisplayName(e.currentTarget.value)}
        />
        {errors.display_name ? (
          <span data-testid="asset-edit-error-display-name" role="alert">
            {errors.display_name}
          </span>
        ) : null}
      </label>

      <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
        <span className="text-[var(--ds-text-muted)]">Tags (comma-separated)</span>
        <input
          type="text"
          data-testid="asset-edit-tags"
          value={tagsRaw}
          onChange={(e) => setTagsRaw(e.currentTarget.value)}
        />
      </label>

      <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
        <span className="text-[var(--ds-text-muted)]">Category</span>
        <input
          type="text"
          data-testid="asset-edit-category"
          value={category}
          onChange={(e) => setCategory(e.currentTarget.value)}
        />
        {errors.category ? (
          <span data-testid="asset-edit-error-category" role="alert">
            {errors.category}
          </span>
        ) : null}
      </label>

      <div className="flex gap-[var(--ds-spacing-md)]">
        <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
          <span className="text-[var(--ds-text-muted)]">Footprint W</span>
          <input
            type="number"
            data-testid="asset-edit-footprint-w"
            min="1"
            step="1"
            value={footprintW}
            onChange={(e) => setFootprintW(Number(e.currentTarget.value))}
          />
        </label>
        <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
          <span className="text-[var(--ds-text-muted)]">Footprint H</span>
          <input
            type="number"
            data-testid="asset-edit-footprint-h"
            min="1"
            step="1"
            value={footprintH}
            onChange={(e) => setFootprintH(Number(e.currentTarget.value))}
          />
        </label>
      </div>

      <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
        <span className="text-[var(--ds-text-muted)]">Placement mode</span>
        <input
          type="text"
          data-testid="asset-edit-placement-mode"
          value={placementMode}
          onChange={(e) => setPlacementMode(e.currentTarget.value)}
        />
      </label>

      <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
        <span className="text-[var(--ds-text-muted)]">Unlocks after</span>
        <input
          type="text"
          data-testid="asset-edit-unlocks-after"
          value={unlocksAfter}
          onChange={(e) => setUnlocksAfter(e.currentTarget.value)}
        />
      </label>

      <label className="flex items-center gap-[var(--ds-spacing-xs)]">
        <input
          type="checkbox"
          data-testid="asset-edit-has-button"
          checked={hasButton}
          onChange={(e) => setHasButton(e.currentTarget.checked)}
        />
        <span className="text-[var(--ds-text-muted)]">Has button</span>
      </label>

      <fieldset data-testid="asset-edit-slot-bindings" className="flex flex-col gap-[var(--ds-spacing-sm)]">
        <legend className="text-[var(--ds-text-muted)]">Sprite slot bindings</legend>
        {SLOTS.map((s) => (
          <EntityRefPicker
            key={s.col}
            accepts_kind={["sprite"]}
            value={slotValues[s.col] ?? null}
            onChange={(_id, row) => setSlotValues((cur) => ({ ...cur, [s.col]: row }))}
            label={s.label}
            testId={`asset-edit-slot-${s.col}`}
          />
        ))}
      </fieldset>

      <fieldset data-testid="asset-edit-economy" className="flex flex-col gap-[var(--ds-spacing-xs)]">
        <legend className="text-[var(--ds-text-muted)]">Economy</legend>
        <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
          <span className="text-[var(--ds-text-muted)]">Base cost (cents)</span>
          <input
            type="number"
            data-testid="asset-edit-base-cost"
            value={baseCost}
            onChange={(e) => setBaseCost(Number(e.currentTarget.value))}
          />
        </label>
        <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
          <span className="text-[var(--ds-text-muted)]">Monthly upkeep (cents)</span>
          <input
            type="number"
            data-testid="asset-edit-monthly-upkeep"
            value={monthlyUpkeep}
            onChange={(e) => setMonthlyUpkeep(Number(e.currentTarget.value))}
          />
        </label>
        <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
          <span className="text-[var(--ds-text-muted)]">Demolition refund (%)</span>
          <input
            type="number"
            data-testid="asset-edit-refund-pct"
            min="0"
            max="100"
            step="1"
            value={refundPct}
            onChange={(e) => setRefundPct(Number(e.currentTarget.value))}
          />
          {errors.demolition_refund_pct ? (
            <span data-testid="asset-edit-error-refund-pct" role="alert">
              {errors.demolition_refund_pct}
            </span>
          ) : null}
        </label>
        <label className="flex flex-col gap-[var(--ds-spacing-xs)]">
          <span className="text-[var(--ds-text-muted)]">Construction ticks</span>
          <input
            type="number"
            data-testid="asset-edit-construction-ticks"
            min="0"
            step="1"
            value={ticks}
            onChange={(e) => setTicks(Number(e.currentTarget.value))}
          />
        </label>
      </fieldset>

      <SubtypeMembershipEditor
        value={memberships}
        onChange={(next) => {
          setMemberships(next);
          // If primary was just removed from memberships, clear primary.
          if (primaryId != null && !next.some((r) => r.entity_id === primaryId)) {
            setPrimaryId(null);
          }
        }}
        primaryPoolId={primaryId}
      />

      <PrimarySubtypeSelect
        memberships={memberships}
        value={primaryId}
        onChange={setPrimaryId}
      />
      {errors.primary_subtype_pool_id ? (
        <span data-testid="asset-edit-error-primary" role="alert" className="text-[var(--ds-text-accent-critical)]">
          {errors.primary_subtype_pool_id}
        </span>
      ) : null}

      <div className="flex gap-[var(--ds-spacing-sm)] items-center">
        <button type="submit" data-testid="asset-edit-submit" disabled={hasErrors}>
          Save changes
        </button>
        {submitError ? (
          <span data-testid="asset-edit-submit-error" role="alert" className="text-[var(--ds-text-accent-critical)]">
            {submitError}
          </span>
        ) : null}
      </div>
    </form>
  );
}
