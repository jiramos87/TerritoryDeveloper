/**
 * Lightweight fuzzy matching: exact, substring, and normalized Levenshtein (no npm deps).
 */

export type FuzzyMatchType = "exact" | "substring" | "fuzzy";

export interface FuzzyMatch<T> {
  item: T;
  /** 0 = perfect, 1 = no similarity; lower is better. */
  score: number;
  matchType: FuzzyMatchType;
}

/**
 * Classic Levenshtein edit distance between two strings.
 */
export function levenshteinDistance(a: string, b: string): number {
  if (a === b) return 0;
  if (!a.length) return b.length;
  if (!b.length) return a.length;
  const m = a.length;
  const n = b.length;
  const row = new Array<number>(n + 1);
  for (let j = 0; j <= n; j++) row[j] = j;
  for (let i = 1; i <= m; i++) {
    let prev = row[0]!;
    row[0] = i;
    for (let j = 1; j <= n; j++) {
      const tmp = row[j]!;
      const cost = a[i - 1] === b[j - 1] ? 0 : 1;
      row[j] = Math.min(row[j]! + 1, row[j - 1]! + 1, prev + cost);
      prev = tmp;
    }
  }
  return row[n]!;
}

/** Similarity between trimmed query and text: exact, substring, or normalized Levenshtein. */
export function scorePair(
  query: string,
  text: string,
): { score: number; matchType: FuzzyMatchType } | null {
  const ql = query.trim().toLowerCase();
  const tl = text.trim().toLowerCase();
  if (!ql || !tl) return null;
  if (ql === tl) return { score: 0, matchType: "exact" };
  if (tl.includes(ql) || ql.includes(tl)) {
    const shorter = Math.min(ql.length, tl.length);
    const longer = Math.max(ql.length, tl.length);
    const score = 1 - shorter / longer;
    return { score, matchType: "substring" };
  }
  const dist = levenshteinDistance(ql, tl);
  const score = dist / Math.max(ql.length, tl.length);
  return { score, matchType: "fuzzy" };
}

/**
 * Best similarity between `query` and `text`, or any alphanumeric token in `text`
 * with length at least `minTokenLength` (default 4). Helps typo queries like "Briges" match "bridges".
 */
export function fuzzyScoreAgainstTextOrTokens(
  query: string,
  text: string,
  options?: { minTokenLength?: number },
): { score: number; matchType: FuzzyMatchType } | null {
  const minLen = options?.minTokenLength ?? 4;
  const q = query.trim();
  if (!q) return null;
  let best: { score: number; matchType: FuzzyMatchType } | null = null;

  const consider = (candidate: string) => {
    const p = scorePair(q, candidate);
    if (p && (!best || p.score < best.score)) best = p;
  };

  consider(text);
  const words = text.match(/[a-z0-9]+/gi) ?? [];
  for (const w of words) {
    if (w.length < minLen) continue;
    consider(w);
  }
  return best;
}

/**
 * Rank items by fuzzy match on heading titles (full string + per-token scores).
 */
export function fuzzyFindByHeadingTitle<T>(
  query: string,
  items: T[],
  getTitle: (item: T) => string,
  options?: FuzzyFindOptions & { minTokenLength?: number },
): FuzzyMatch<T>[] {
  const threshold = options?.threshold ?? 0.4;
  const maxResults = options?.maxResults ?? 5;
  const minTokenLength = options?.minTokenLength ?? 4;
  const q = query.trim();
  if (!q) return [];

  const scored: FuzzyMatch<T>[] = [];
  for (const item of items) {
    const title = getTitle(item);
    const pair = fuzzyScoreAgainstTextOrTokens(q, title, { minTokenLength });
    if (pair === null || pair.score > threshold) continue;
    scored.push({ item, score: pair.score, matchType: pair.matchType });
  }

  scored.sort((a, b) => {
    if (a.score !== b.score) return a.score - b.score;
    return getTitle(a.item).length - getTitle(b.item).length;
  });

  return scored.slice(0, maxResults);
}

export interface FuzzyFindOptions {
  /** Reject matches with score strictly greater than this (default 0.4). */
  threshold?: number;
  maxResults?: number;
}

/**
 * Rank items by similarity to `query` using exact → substring → Levenshtein.
 */
export function fuzzyFind<T>(
  query: string,
  items: T[],
  getText: (item: T) => string,
  options?: FuzzyFindOptions,
): FuzzyMatch<T>[] {
  const threshold = options?.threshold ?? 0.4;
  const maxResults = options?.maxResults ?? 5;
  const q = query.trim();
  if (!q) return [];

  const scored: FuzzyMatch<T>[] = [];
  for (const item of items) {
    const text = getText(item);
    const pair = scorePair(q, text);
    if (pair === null) continue;
    if (pair.score > threshold) continue;
    scored.push({ item, score: pair.score, matchType: pair.matchType });
  }

  scored.sort((a, b) => {
    if (a.score !== b.score) return a.score - b.score;
    return getText(a.item).length - getText(b.item).length;
  });

  return scored.slice(0, maxResults);
}

/**
 * Strip bracket subscripts (e.g. `HeightMap[x,y]`) for glossary-style matching.
 */
export function normalizeGlossaryQuery(term: string): string {
  return term
    .replace(/\[[^\]]*\]/g, "")
    .replace(/\s+/g, " ")
    .trim();
}
