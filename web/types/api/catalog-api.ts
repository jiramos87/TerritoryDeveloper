import type { CatalogAssetRow } from "./catalog-asset";
import type { CatalogEconomyRow } from "./catalog-economy";
import type { CatalogAssetStatus, CatalogSpriteSlot } from "./catalog-enums";

/**
 * Shared query / body types for Stage 1.3 ` /api/catalog/*` handlers (this file is types-only).
 */

/** List filters for catalog asset collections (aligns with `catalog_asset` columns + visibility story). */
export interface CatalogAssetListFilters {
  /** When set, restrict to a single lifecycle status. */
  status?: CatalogAssetStatus;
  /** When set, `WHERE category = :category` */
  category?: string;
}

/**
 * Request bodies that carry an optimistic lock token. Mutations compare to row `updated_at`
 * and return 409 on mismatch (Stage 1.3).
 */
export interface CatalogOptimisticLockFields {
  /** ISO-8601 timestamptz string — must match current row `updated_at` for successful PATCH. */
  updated_at: string;
}

/**
 * JSON-serializable preview-diff result for “what would change” calls (no migration changes in Stage 1.2).
 * `patch` is a loose record so route code can diff concrete DTO fields in Stage 1.3.
 */
export interface CatalogPreviewDiffResult<TBefore = unknown, TAfter = unknown> {
  /** Whether any field would change. */
  changed: boolean;
  before: TBefore;
  after: TAfter;
  /** Optional list of top-level field keys that differ. */
  diff_keys?: string[];
}

/** `POST /api/catalog/assets` (Stage 1.3). */
export interface CatalogCreateAssetBody {
  category: string;
  slug: string;
  display_name: string;
  status: CatalogAssetStatus;
  replaced_by?: string | null;
  footprint_w?: number;
  footprint_h?: number;
  placement_mode?: string | null;
  unlocks_after?: string | null;
  has_button?: boolean;
  economy: {
    base_cost_cents: number;
    monthly_upkeep_cents: number;
    demolition_refund_pct?: number;
    construction_ticks?: number;
    budget_envelope_id?: number | null;
    cost_catalog_row_id?: string | null;
  };
  sprite_binds: Array<{
    slot: CatalogSpriteSlot;
    sprite_id: string;
  }>;
}

/** `PATCH /api/catalog/assets/:id` (Stage 1.3) — versioned partial update. */
export type CatalogPatchAssetBody = Partial<
  Pick<
    CatalogAssetRow,
    | "display_name"
    | "status"
    | "replaced_by"
    | "footprint_w"
    | "footprint_h"
    | "placement_mode"
    | "unlocks_after"
    | "has_button"
  >
> & {
  updated_at: string;
  economy?: Partial<
    Pick<
      CatalogEconomyRow,
      | "base_cost_cents"
      | "monthly_upkeep_cents"
      | "demolition_refund_pct"
      | "construction_ticks"
      | "budget_envelope_id"
      | "cost_catalog_row_id"
    >
  >;
};

/** `POST /api/catalog/assets/:id/retire` (Stage 1.3). */
export interface CatalogRetireBody {
  replaced_by?: string | null;
}

/** `POST /api/catalog/preview-diff` (Stage 1.3) — `patch` is shallow asset-field overrides only. */
export interface CatalogPreviewDiffRequest {
  asset_id: string;
  patch: Record<string, unknown>;
}

/**
 * Row shape returned by `GET /api/catalog/entities` for `<EntityRefPicker>` (TECH-1787).
 * Mirrors `catalog_entity` columns needed for the picker dropdown + badge resolution.
 */
export interface EntityRefSearchRow {
  entity_id: string;
  slug: string;
  display_name: string;
  kind: string;
  current_published_version_id: string | null;
  retired_at: string | null;
}

export interface EntityRefPickerProps {
  accepts_kind: string[];
  value: EntityRefSearchRow | null;
  valueId?: string | null;
  onChange: (entityId: string | null, row: EntityRefSearchRow | null) => void;
  label?: string;
  disabled?: boolean;
  testId?: string;
}

/**
 * Asset detail composite envelope returned by `GET /api/catalog/assets/[slug]`
 * for the spine-aware Stage 7.1 surface (TECH-1786). Bridges `catalog_entity`
 * + `asset_detail` + `economy_detail` + memberships and primary subtype
 * (TECH-1789).
 */
export interface CatalogAssetSpineDto {
  entity_id: string;
  slug: string;
  display_name: string;
  tags: string[];
  retired_at: string | null;
  current_published_version_id: string | null;
  updated_at: string;
  asset_detail: {
    category: string;
    footprint_w: number;
    footprint_h: number;
    placement_mode: string | null;
    unlocks_after: string | null;
    has_button: boolean;
    world_sprite_entity_id: string | null;
    button_target_sprite_entity_id: string | null;
    button_pressed_sprite_entity_id: string | null;
    button_disabled_sprite_entity_id: string | null;
    button_hover_sprite_entity_id: string | null;
    primary_subtype_pool_id: string | null;
  } | null;
  economy_detail: {
    base_cost_cents: number;
    monthly_upkeep_cents: number;
    demolition_refund_pct: number;
    construction_ticks: number;
  } | null;
  /** Resolved sprite-slot rows for picker hydration (DEC-A22). */
  sprite_slot_resolutions: Record<string, EntityRefSearchRow | null>;
  /** Subtype membership pool rows (TECH-1789). */
  subtype_memberships: EntityRefSearchRow[];
}

export interface CatalogAssetSpinePatchBody {
  updated_at: string;
  display_name?: string;
  tags?: string[];
  asset_detail?: Partial<{
    category: string;
    footprint_w: number;
    footprint_h: number;
    placement_mode: string | null;
    unlocks_after: string | null;
    has_button: boolean;
    world_sprite_entity_id: string | null;
    button_target_sprite_entity_id: string | null;
    button_pressed_sprite_entity_id: string | null;
    button_disabled_sprite_entity_id: string | null;
    button_hover_sprite_entity_id: string | null;
    primary_subtype_pool_id: string | null;
  }>;
  economy_detail?: Partial<{
    base_cost_cents: number;
    monthly_upkeep_cents: number;
    demolition_refund_pct: number;
    construction_ticks: number;
  }>;
  /** TECH-1789 — diff-style membership update; server applies in single tx. */
  subtype_membership?: {
    added: string[];
    removed: string[];
  };
}

export interface CatalogPoolMemberSpineRow {
  asset_entity_id: string;
  slug: string;
  display_name: string;
  weight: number;
  conditions_json: Record<string, unknown>;
}

export interface CatalogPoolDto {
  entity_id: string;
  slug: string;
  display_name: string;
  tags: string[];
  retired_at: string | null;
  current_published_version_id: string | null;
  updated_at: string;
  pool_detail: {
    primary_subtype: string | null;
    owner_category: string | null;
  } | null;
  members: CatalogPoolMemberSpineRow[];
  /** TECH-1789 — count of asset_detail rows whose primary_subtype_pool_id = this pool. */
  primary_tagged_by_count: number;
}

export interface CatalogPoolPatchBody {
  updated_at: string;
  display_name?: string;
  tags?: string[];
  pool_detail?: Partial<{
    primary_subtype: string | null;
    owner_category: string | null;
  }>;
  /** Member upsert + delete (single-tx server diff). */
  members?: Array<{
    asset_entity_id: string;
    weight: number;
    conditions_json?: Record<string, unknown>;
  }>;
  removed_member_entity_ids?: string[];
}

export interface CatalogPoolCreateBody {
  slug: string;
  display_name: string;
  tags?: string[];
  pool_detail?: {
    primary_subtype?: string | null;
    owner_category?: string | null;
  };
}

/**
 * Button binding (TECH-1885 / Stage 8.1) — DEC-A7 hybrid binding model.
 * 6 sprite slots + 4 token slots + size_variant + action_id + enable_predicate_json.
 */
export interface CatalogButtonDetail {
  sprite_idle_entity_id: string | null;
  sprite_hover_entity_id: string | null;
  sprite_pressed_entity_id: string | null;
  sprite_disabled_entity_id: string | null;
  sprite_icon_entity_id: string | null;
  sprite_badge_entity_id: string | null;
  token_palette_entity_id: string | null;
  token_frame_style_entity_id: string | null;
  token_font_entity_id: string | null;
  token_illumination_entity_id: string | null;
  size_variant: string;
  action_id: string;
  enable_predicate_json: Record<string, unknown>;
}

export interface CatalogButtonDto {
  entity_id: string;
  slug: string;
  display_name: string;
  tags: string[];
  retired_at: string | null;
  current_published_version_id: string | null;
  updated_at: string;
  button_detail: CatalogButtonDetail | null;
  /** Resolved entity rows for the 10 ref slots, keyed by column name. */
  slot_resolutions: Record<string, EntityRefSearchRow | null>;
}

export interface CatalogButtonCreateBody {
  slug: string;
  display_name: string;
  tags?: string[];
  button_detail?: Partial<CatalogButtonDetail>;
}

export interface CatalogButtonPatchBody {
  updated_at: string;
  display_name?: string;
  tags?: string[];
  button_detail?: Partial<CatalogButtonDetail>;
}

/**
 * Panel detail (TECH-1887 / Stage 8.1) — DEC-A27 slot-composition model.
 * Archetype declares `slots_schema` on `entity_version.params_json`; panel
 * binds children into named slots via `panel_child` rows.
 */
export type CatalogPanelChildKind =
  | "button"
  | "panel"
  | "label"
  | "spacer"
  | "audio"
  | "sprite"
  | "label_inline";

export interface CatalogPanelChildDto {
  /** Optional — NULL allowed per DEC-A27 for spacer / label_inline. */
  child_entity_id: string | null;
  child_kind: CatalogPanelChildKind;
  slot_name: string;
  order_idx: number;
  params_json: Record<string, unknown>;
  /** Resolved child catalog row for picker hydration (null when inline-only or unresolvable). */
  resolved: EntityRefSearchRow | null;
}

export interface CatalogPanelSlotSchemaEntry {
  accepts_kind: string[];
  min?: number;
  max?: number;
}

export interface CatalogPanelDetail {
  archetype_entity_id: string | null;
  background_sprite_entity_id: string | null;
  palette_entity_id: string | null;
  frame_style_entity_id: string | null;
  layout_template: "vstack" | "hstack" | "grid" | "free";
  modal: boolean;
  /** Cached copy of archetype `slots_schema` for the bound version. Read-only. */
  slots_schema: Record<string, CatalogPanelSlotSchemaEntry> | null;
}

export interface CatalogPanelDto {
  entity_id: string;
  slug: string;
  display_name: string;
  tags: string[];
  retired_at: string | null;
  current_published_version_id: string | null;
  updated_at: string;
  panel_detail: CatalogPanelDetail | null;
  /** Slot-grouped children, one entry per declared slot in archetype order. */
  slots: Array<{
    name: string;
    schema: CatalogPanelSlotSchemaEntry | null;
    children: CatalogPanelChildDto[];
  }>;
  /** Optional archetype resolution row (when archetype_entity_id is set). */
  archetype_resolution: EntityRefSearchRow | null;
}

export interface CatalogPanelCreateBody {
  slug: string;
  display_name: string;
  tags?: string[];
  panel_detail?: Partial<{
    archetype_entity_id: string | null;
    background_sprite_entity_id: string | null;
    palette_entity_id: string | null;
    frame_style_entity_id: string | null;
    layout_template: "vstack" | "hstack" | "grid" | "free";
    modal: boolean;
  }>;
}

export interface CatalogPanelPatchBody {
  updated_at: string;
  display_name?: string;
  tags?: string[];
  panel_detail?: Partial<{
    archetype_entity_id: string | null;
    background_sprite_entity_id: string | null;
    palette_entity_id: string | null;
    frame_style_entity_id: string | null;
    layout_template: "vstack" | "hstack" | "grid" | "free";
    modal: boolean;
  }>;
}

/**
 * Replace-tree request body for `POST /api/catalog/panels/[slug]/children`
 * (TECH-1887). Server validates slot.accepts + slot count + no panel cycle
 * inside SERIALIZABLE tx, then deletes-and-reinserts atomically (DEC-A43).
 */
export interface CatalogPanelChildSetBody {
  /** Optimistic-lock fingerprint per DEC-A38; compared against panel `catalog_entity.updated_at`. */
  updated_at: string;
  /** When true, snapshot child_version_id from each child entity's published version (DEC-A22). */
  publish?: boolean;
  slots: Array<{
    name: string;
    children: Array<{
      child_entity_id?: string | null;
      child_kind: CatalogPanelChildKind;
      order_idx: number;
      params_json?: Record<string, unknown>;
    }>;
  }>;
}

/**
 * Token catalog (TECH-2092 / Stage 10.1) — DEC-A44 5-kind token model.
 * Bridges `catalog_entity` (kind='token') + `token_detail` row carrying
 * `token_kind` + `value_json` + optional `semantic_target_entity_id` (FK,
 * required iff kind='semantic'). Per-kind value shapes validated by
 * `web/lib/catalog/token-detail-schema.ts`.
 */
export type CatalogTokenKind =
  | "color"
  | "type-scale"
  | "motion"
  | "spacing"
  | "semantic";

/** Color value: discriminated union of hex string OR HSL triple. */
export type CatalogTokenColorValue =
  | { hex: string }
  | { h: number; s: number; l: number };

export interface CatalogTokenTypeScaleValue {
  font_family: string;
  size_px: number;
  line_height: number;
}

export type CatalogTokenMotionCurve =
  | "linear"
  | "ease-in"
  | "ease-out"
  | "ease-in-out"
  | "cubic-bezier";

export interface CatalogTokenMotionValue {
  curve: CatalogTokenMotionCurve;
  duration_ms: number;
  /** Optional 4-tuple of finite numbers; required when curve='cubic-bezier'. */
  cubic_bezier?: [number, number, number, number] | null;
}

export interface CatalogTokenSpacingValue {
  px: number;
}

export interface CatalogTokenSemanticValue {
  token_role: string;
}

/** Discriminated union of value_json bodies; caller narrows by `token_kind`. */
export type CatalogTokenValueJson =
  | CatalogTokenColorValue
  | CatalogTokenTypeScaleValue
  | CatalogTokenMotionValue
  | CatalogTokenSpacingValue
  | CatalogTokenSemanticValue;

export interface CatalogTokenDetail {
  token_kind: CatalogTokenKind;
  value_json: Record<string, unknown>;
  /** FK to another `catalog_entity` (kind='token'); required iff kind='semantic'. */
  semantic_target_entity_id: string | null;
}

export interface CatalogTokenDto {
  entity_id: string;
  slug: string;
  display_name: string;
  tags: string[];
  retired_at: string | null;
  current_published_version_id: string | null;
  updated_at: string;
  token_detail: CatalogTokenDetail | null;
  /** Resolved row for `semantic_target_entity_id` (null when kind!=semantic). */
  semantic_target_resolution: EntityRefSearchRow | null;
}

export interface CatalogTokenCreateBody {
  slug: string;
  display_name: string;
  tags?: string[];
  token_detail: {
    token_kind: CatalogTokenKind;
    value_json: Record<string, unknown>;
    semantic_target_entity_id?: string | null;
  };
}

export interface CatalogTokenPatchBody {
  /** Optimistic-lock fingerprint per DEC-A38; compared against `catalog_entity.updated_at`. */
  updated_at: string;
  display_name?: string;
  tags?: string[];
  token_detail?: Partial<{
    token_kind: CatalogTokenKind;
    value_json: Record<string, unknown>;
    semantic_target_entity_id: string | null;
  }>;
}
