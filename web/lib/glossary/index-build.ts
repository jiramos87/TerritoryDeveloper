/**
 * index-build.ts — fast lookup index for glossary terms.
 *
 * Used by the dashboard markdown renderer to attach tooltip definitions
 * to backtick-wrapped glossary terms (e.g. `BlipPatch`, `HeightMap`).
 *
 * Exact-match (case-insensitive) on the term column. Caller passes the
 * raw inner-backtick string; renderer falls back to plain bold styling
 * when no match is found.
 */

import type { GlossaryTerm } from "./types";

export interface GlossaryIndex {
  /** Lookup definition + slug by lowercased term key. Returns null on miss. */
  lookup: (raw: string) => GlossaryTerm | null;
  /** Total entries indexed (debug / empty-state guards). */
  size: number;
}

/** Normalise a term key for case-insensitive lookup. Strips bold markers
 *  and trims whitespace; matches `slugify`-friendly raw input. */
function normalize(raw: string): string {
  return raw.replace(/\*\*/g, "").trim().toLowerCase();
}

export function buildGlossaryIndex(terms: GlossaryTerm[]): GlossaryIndex {
  const map = new Map<string, GlossaryTerm>();
  for (const t of terms) {
    const k = normalize(t.term);
    if (!map.has(k)) map.set(k, t);
  }
  return {
    lookup: (raw: string) => map.get(normalize(raw)) ?? null,
    size: map.size,
  };
}
