import palette from "./palette.json";
import typeScale from "./type-scale.json";
import spacing from "./spacing.json";

type RawPalette = typeof palette.raw;
type SemanticPalette = typeof palette.semantic;

/**
 * Resolves a "{raw.<key>}" alias string against the raw palette.
 * Returns the raw hex if the alias pattern matches; otherwise returns the value as-is.
 */
function resolveAlias(value: string, raw: RawPalette): string {
  const match = value.match(/^\{raw\.(.+)\}$/);
  if (!match) return value;
  const key = match[1] as keyof RawPalette;
  return raw[key] ?? value;
}

/**
 * Resolves all semantic palette aliases to their raw hex values.
 */
function resolveSemantic(
  sem: SemanticPalette,
  raw: RawPalette
): Record<string, string> {
  return Object.fromEntries(
    Object.entries(sem).map(([name, val]) => [name, resolveAlias(val, raw)])
  );
}

/** Resolved token map for Tailwind / TS consumers. */
export const tokens = {
  colors: resolveSemantic(palette.semantic, palette.raw),
  fontFamily: typeScale.fontFamily,
  fontSize: typeScale.fontSize,
  spacing,
} as const;

export type TokenColors = typeof tokens.colors;
