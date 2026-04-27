/**
 * Button enums (TECH-1885 / Stage 8.1).
 *
 * Single source of truth for `button_detail.size_variant` CHECK + token
 * sub-kind discriminator on `entity_version.params_json.token_kind` per
 * DEC-A7. Hardcoded constant — DEC-A7 lists 3 size values verbatim; no
 * archetype-driven enum.
 */

export const SIZE_VARIANTS = ["sm", "md", "lg"] as const;
export type SizeVariant = (typeof SIZE_VARIANTS)[number];

export const TOKEN_KINDS = ["palette", "frame_style", "font", "illumination"] as const;
export type TokenKind = (typeof TOKEN_KINDS)[number];
