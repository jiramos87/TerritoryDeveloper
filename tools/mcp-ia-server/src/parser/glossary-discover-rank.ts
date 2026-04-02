/**
 * Deterministic keyword → glossary row ranking for glossary_discover (Phase A).
 * Scores overlap in term / definition / specReference / category plus a light fuzzy_term signal on collapsed term text.
 *
 * Weights are fixed constants (documented in tests); tune via tests when changing behavior.
 */

import type { GlossaryEntry } from "./types.js";
import { normalizeGlossaryQuery, scorePair } from "./fuzzy.js";

/** Where a non-zero score came from (for debugging and agent hints). */
export type DiscoverMatchReason =
  | "term"
  | "definition"
  | "spec_reference"
  | "category"
  | "fuzzy_term";

export interface RankedDiscoverRow {
  entry: GlossaryEntry;
  /** Higher is better; deterministic tie-breaks applied when sorting. */
  score: number;
  matchReasons: DiscoverMatchReason[];
}

/** Minimum token length after tokenization (filters "a", "an", two-letter noise kept intentionally short). */
export const DISCOVER_MIN_TOKEN_LEN = 2;

/** Token overlap weights (Phase A). */
export const DISCOVER_WEIGHT_TERM = 5;
export const DISCOVER_WEIGHT_DEFINITION = 2.5;
export const DISCOVER_WEIGHT_SPEC = 1.8;
export const DISCOVER_WEIGHT_CATEGORY = 0.8;

/** Substring hit multiplier when token is not a whole token in the field. */
export const DISCOVER_SUBSTRING_FACTOR = 0.55;

/** Collapsed-term fuzzy bonus: scorePair threshold; at or below adds (1 - score) * this factor. */
export const DISCOVER_FUZZY_TERM_MAX_SCORE = 0.35;
export const DISCOVER_FUZZY_TERM_FACTOR = 3;

function collapseForGlossary(s: string): string {
  return normalizeGlossaryQuery(s).toLowerCase().replace(/\s+/g, "");
}

/**
 * Lowercase alphanumeric / hyphen tokens for overlap scoring.
 */
export function tokenizeDiscoverText(s: string): string[] {
  const m = s.toLowerCase().match(/[a-z0-9][a-z0-9-]*/g) ?? [];
  return [...new Set(m.filter((t) => t.length >= DISCOVER_MIN_TOKEN_LEN))];
}

function tokenSetFromField(s: string): Set<string> {
  return new Set(tokenizeDiscoverText(s));
}

function addTokenFieldScore(
  queryTokens: Iterable<string>,
  fieldText: string,
  weight: number,
  reason: DiscoverMatchReason,
  reasons: Set<DiscoverMatchReason>,
  scoreRef: { value: number },
): void {
  if (!fieldText.trim()) return;
  const lower = fieldText.toLowerCase();
  const set = tokenSetFromField(fieldText);
  for (const qt of queryTokens) {
    if (set.has(qt)) {
      scoreRef.value += weight;
      reasons.add(reason);
      continue;
    }
    if (qt.length >= 3 && lower.includes(qt)) {
      scoreRef.value += weight * DISCOVER_SUBSTRING_FACTOR;
      reasons.add(reason);
    }
  }
}

function fuzzyTermContribution(
  fullQuery: string,
  term: string,
  reasons: Set<DiscoverMatchReason>,
  scoreRef: { value: number },
): void {
  const q = collapseForGlossary(fullQuery);
  const t = collapseForGlossary(term);
  if (!q || !t) return;
  const p = scorePair(q, t);
  if (!p) return;
  if (p.score <= DISCOVER_FUZZY_TERM_MAX_SCORE) {
    scoreRef.value += (1 - p.score) * DISCOVER_FUZZY_TERM_FACTOR;
    reasons.add("fuzzy_term");
  }
}

function compareRanked(a: RankedDiscoverRow, b: RankedDiscoverRow): number {
  if (b.score !== a.score) return b.score - a.score;
  const la = a.entry.term.length;
  const lb = b.entry.term.length;
  if (la !== lb) return la - lb;
  return a.entry.term.localeCompare(b.entry.term);
}

/**
 * Rank glossary entries by keyword overlap + fuzzy term signal.
 *
 * @param entries - Parsed glossary rows (e.g. from {@link parseGlossary}).
 * @param queryText - Free text built from `query` and optional `keywords` joined by spaces.
 * @param options.maxResults - Cap after sorting (default: all non-zero scores).
 */
export function rankGlossaryDiscover(
  entries: GlossaryEntry[],
  queryText: string,
  options?: { maxResults?: number },
): RankedDiscoverRow[] {
  const trimmed = queryText.trim();
  if (!trimmed) return [];

  const queryTokens = tokenizeDiscoverText(trimmed);
  const tokenList = queryTokens.length > 0 ? queryTokens : [];

  const ranked: RankedDiscoverRow[] = [];

  for (const entry of entries) {
    const reasons = new Set<DiscoverMatchReason>();
    const scoreRef = { value: 0 };

    if (tokenList.length > 0) {
      addTokenFieldScore(
        tokenList,
        entry.term,
        DISCOVER_WEIGHT_TERM,
        "term",
        reasons,
        scoreRef,
      );
      addTokenFieldScore(
        tokenList,
        entry.definition,
        DISCOVER_WEIGHT_DEFINITION,
        "definition",
        reasons,
        scoreRef,
      );
      addTokenFieldScore(
        tokenList,
        entry.specReference,
        DISCOVER_WEIGHT_SPEC,
        "spec_reference",
        reasons,
        scoreRef,
      );
      addTokenFieldScore(
        tokenList,
        entry.category,
        DISCOVER_WEIGHT_CATEGORY,
        "category",
        reasons,
        scoreRef,
      );
    }

    fuzzyTermContribution(trimmed, entry.term, reasons, scoreRef);

    if (scoreRef.value <= 0) continue;

    ranked.push({
      entry,
      score: scoreRef.value,
      matchReasons: [...reasons].sort(),
    });
  }

  ranked.sort(compareRanked);

  const max = options?.maxResults;
  if (typeof max === "number" && max > 0) {
    return ranked.slice(0, max);
  }
  return ranked;
}
