/**
 * Token detail value_json validators (TECH-2092 / Stage 10.1).
 *
 * Per-kind shape gates for the 5 token kinds (color / type-scale / motion /
 * spacing / semantic) per DEC-A44. Hand-rolled validators (no zod dep in
 * web/) — mirror panel-child-validators.ts shape: discriminated-union result
 * with `code` + `details` fields the API maps to DEC-A48 envelope.
 *
 * Color value union: `{hex: string}` OR `{h, s, l}` numbers.
 * Type-scale: `{font_family, size_px, line_height}` (numbers > 0).
 * Motion: `{curve, cubic_bezier?, duration_ms}` curve enum + optional 4-tuple.
 * Spacing: `{px}` non-negative number.
 * Semantic: `{token_role}` non-empty string + caller-side FK column.
 *
 * @see ia/projects/asset-pipeline/stage-10.1 — TECH-2092 §Plan Digest
 */
import type { CatalogTokenKind } from "@/types/api/catalog-api";

export type TokenValueValidationOk = { ok: true };
export type TokenValueValidationErr = {
  ok: false;
  code: "validation";
  reason: string;
};
export type TokenValueValidationResult =
  | TokenValueValidationOk
  | TokenValueValidationErr;

export const TOKEN_KINDS: readonly CatalogTokenKind[] = [
  "color",
  "type-scale",
  "motion",
  "spacing",
  "semantic",
] as const;

const MOTION_CURVES = new Set<string>([
  "linear",
  "ease-in",
  "ease-out",
  "ease-in-out",
  "cubic-bezier",
]);

const HEX_RE = /^#?[0-9a-fA-F]{6}$/;

function err(reason: string): TokenValueValidationErr {
  return { ok: false, code: "validation", reason };
}

function isPlainObject(v: unknown): v is Record<string, unknown> {
  return typeof v === "object" && v !== null && !Array.isArray(v);
}

function validateColor(value: Record<string, unknown>): TokenValueValidationResult {
  // Discriminated union: `{hex}` OR `{h, s, l}`.
  if ("hex" in value) {
    if (typeof value.hex !== "string" || !HEX_RE.test(value.hex)) {
      return err("color.hex must match /^#?[0-9a-f]{6}$/i");
    }
    return { ok: true };
  }
  if ("h" in value && "s" in value && "l" in value) {
    const h = value.h;
    const s = value.s;
    const l = value.l;
    if (typeof h !== "number" || h < 0 || h > 360) return err("color.h must be 0..360");
    if (typeof s !== "number" || s < 0 || s > 100) return err("color.s must be 0..100");
    if (typeof l !== "number" || l < 0 || l > 100) return err("color.l must be 0..100");
    return { ok: true };
  }
  return err("color value_json must be {hex} or {h,s,l}");
}

function validateTypeScale(value: Record<string, unknown>): TokenValueValidationResult {
  if (typeof value.font_family !== "string" || value.font_family.trim() === "") {
    return err("type-scale.font_family required (non-empty string)");
  }
  if (typeof value.size_px !== "number" || !(value.size_px > 0)) {
    return err("type-scale.size_px must be a positive number");
  }
  if (typeof value.line_height !== "number" || !(value.line_height > 0)) {
    return err("type-scale.line_height must be a positive number");
  }
  return { ok: true };
}

function validateMotion(value: Record<string, unknown>): TokenValueValidationResult {
  if (typeof value.curve !== "string" || !MOTION_CURVES.has(value.curve)) {
    return err(`motion.curve must be one of ${[...MOTION_CURVES].join("|")}`);
  }
  if (typeof value.duration_ms !== "number" || !(value.duration_ms >= 0)) {
    return err("motion.duration_ms must be a non-negative number");
  }
  if ("cubic_bezier" in value && value.cubic_bezier != null) {
    const cb = value.cubic_bezier;
    if (
      !Array.isArray(cb) ||
      cb.length !== 4 ||
      !cb.every((n) => typeof n === "number" && Number.isFinite(n))
    ) {
      return err("motion.cubic_bezier must be a 4-tuple of finite numbers");
    }
  }
  return { ok: true };
}

function validateSpacing(value: Record<string, unknown>): TokenValueValidationResult {
  if (typeof value.px !== "number" || !(value.px >= 0)) {
    return err("spacing.px must be a non-negative number");
  }
  return { ok: true };
}

function validateSemantic(value: Record<string, unknown>): TokenValueValidationResult {
  if (typeof value.token_role !== "string" || value.token_role.trim() === "") {
    return err("semantic.token_role required (non-empty string)");
  }
  return { ok: true };
}

/**
 * Validate `value_json` against the shape required by `token_kind`.
 * Returns discriminated-union; caller maps `validation` to DEC-A48 envelope.
 */
export function validateTokenValueJson(
  token_kind: CatalogTokenKind,
  value_json: unknown,
): TokenValueValidationResult {
  if (!isPlainObject(value_json)) {
    return err("value_json must be an object");
  }
  switch (token_kind) {
    case "color":
      return validateColor(value_json);
    case "type-scale":
      return validateTypeScale(value_json);
    case "motion":
      return validateMotion(value_json);
    case "spacing":
      return validateSpacing(value_json);
    case "semantic":
      return validateSemantic(value_json);
    default: {
      const _exhaustive: never = token_kind;
      return err(`unknown token_kind: ${_exhaustive as string}`);
    }
  }
}

export function isTokenKind(v: unknown): v is CatalogTokenKind {
  return typeof v === "string" && TOKEN_KINDS.includes(v as CatalogTokenKind);
}
