/**
 * Kind-schema dispatch tables for typed version diffs (TECH-3300 / Stage 14.3).
 *
 * Per-kind field-hint tables flag how each catalog kind's `params_json` field
 * should be rendered when its value changes between two `entity_version` rows.
 * `hintFor(kind, field)` resolves a `FieldHint` per field, falling back to
 * `"scalar"` for unknown fields. Renderers (T14.3.3 / T14.3.4 / T14.3.5)
 * dispatch on the hint to choose `ScalarFieldDiff` / `ListFieldDiff` /
 * `BlobFieldDiff` / kind-specific renderers (e.g. token-swatch chip).
 *
 * Hint extension `"asset" | "sprite" | "audio" | "token"` flags archetype
 * sub-payload references — `archetype.tsx` (T14.3.5) recursively dispatches
 * to the matching kind renderer when these hints fire.
 *
 * Module is pure: no DB / fetch / React imports.
 *
 * @see ia/projects/asset-pipeline/stage-14.3 — TECH-3300 §Plan Digest
 * @see web/lib/diff/diff-versions.ts — consumer
 */
import type { CatalogKind } from "@/lib/refs/types";

/**
 * Render-hint discriminator for a single field's diff.
 *
 * Base hints (T14.3.1):
 * - `scalar` — primitive value (string / number / boolean), before/after pair render.
 * - `list` — array value, line-level added/removed markers.
 * - `token` — color-bearing value, render as visual swatch chip when CSS-parseable.
 * - `blob` — binary reference (image / audio path), text-stub side-by-side.
 *
 * Nested-kind hints (T14.3.5 — archetype sub-payload dispatch):
 * - `asset` / `sprite` / `audio` / `token` (when used as nested-kind, not color):
 *   archetype renderer recursively dispatches to that kind's renderer.
 *
 * Note: `token` overloads — the value `"token"` covers BOTH the color-swatch
 * hint (token kind's `value` field) AND the nested-kind hint (archetype's
 * token sub-payload). Both renderers handle the `token` hint distinctly:
 * `token.tsx` checks `CSS.supports("color", value)` for the swatch path;
 * `archetype.tsx` treats it as a nested kind reference.
 */
export type FieldHint =
  | "scalar"
  | "list"
  | "token"
  | "blob"
  | "asset"
  | "sprite"
  | "audio";

/**
 * Single changed-field record produced by `diffVersions`.
 *
 * `before` / `after` are the raw `params_json` values; renderers narrow
 * `unknown` per hint. `hint` resolved via `hintFor(kind, field)`.
 */
export interface KindDiffChange {
  field: string;
  before: unknown;
  after: unknown;
  hint: FieldHint;
}

/**
 * Typed diff payload returned by `diffVersions`. Lists are alpha-sorted for
 * stable golden fixtures.
 */
export interface KindDiff {
  added: string[];
  removed: string[];
  changed: KindDiffChange[];
}

type HintTable = Readonly<Record<string, FieldHint>>;

/**
 * Per-kind field-hint tables. Whitelist known fields only — `hintFor`
 * falls back to `"scalar"` when a field is absent.
 *
 * Coverage targets representative `params_json` shapes per kind. Authoring
 * console may add fields beyond the whitelist; those render as `scalar` until
 * a future Stage extends the table (Pending Decision: token_hint_keys).
 */
const SPRITE_HINTS: HintTable = {
  image_path: "blob",
  thumbnail_path: "blob",
  tags: "list",
  variants: "list",
};

const ASSET_HINTS: HintTable = {
  sprite_id: "sprite",
  thumbnail_path: "blob",
  tags: "list",
  zone_subtype_ids: "list",
  variants: "list",
};

const BUTTON_HINTS: HintTable = {
  sprite_id: "sprite",
  icon_sprite_id: "sprite",
  tags: "list",
  states: "list",
  hover_token: "token",
  pressed_token: "token",
};

const PANEL_HINTS: HintTable = {
  background_token: "token",
  border_token: "token",
  child_button_ids: "list",
  child_token_ids: "list",
  slot_archetype_ids: "list",
  tags: "list",
};

const POOL_HINTS: HintTable = {
  members: "list",
  member_asset_ids: "list",
  predicate: "list",
  tags: "list",
};

const TOKEN_HINTS: HintTable = {
  // Color-bearing fields render as swatch chips via `token.tsx` (T14.3.4).
  value: "token",
  hex: "token",
  rgb: "token",
  hsl: "token",
  // Non-color fields fall through to scalar.
  tags: "list",
};

const ARCHETYPE_HINTS: HintTable = {
  // Nested-kind sub-payloads — `archetype.tsx` (T14.3.5) recursively dispatches.
  asset: "asset",
  sprite: "sprite",
  audio: "audio",
  token: "token",
  asset_ref: "asset",
  sprite_ref: "sprite",
  audio_ref: "audio",
  token_ref: "token",
  slots: "list",
  tags: "list",
};

const AUDIO_HINTS: HintTable = {
  audio_path: "blob",
  waveform_path: "blob",
  tags: "list",
  variants: "list",
};

const HINTS: Readonly<Record<CatalogKind, HintTable>> = {
  sprite: SPRITE_HINTS,
  asset: ASSET_HINTS,
  button: BUTTON_HINTS,
  panel: PANEL_HINTS,
  pool: POOL_HINTS,
  token: TOKEN_HINTS,
  archetype: ARCHETYPE_HINTS,
  audio: AUDIO_HINTS,
};

/**
 * Resolve the render hint for a `(kind, field)` pair. Unknown fields fall
 * back to `"scalar"` (per Stage 14.3 objective).
 */
export function hintFor(kind: CatalogKind, field: string): FieldHint {
  const table = HINTS[kind];
  if (table == null) return "scalar";
  return table[field] ?? "scalar";
}
