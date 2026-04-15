import fs from 'fs/promises';
import path from 'path';
import type { GlossaryTerm } from './types';

// ---------------------------------------------------------------------------
// Path resolution — mirrors web/lib/mdx/loader.ts resolveContentPath pattern
// ---------------------------------------------------------------------------

/** Resolve absolute path to ia/specs/glossary.md.
 *  cwd may be repo root (validate:web / npm scripts) or web/ (Next dev/build). */
async function resolveGlossaryPath(): Promise<string> {
  const fromRoot = path.join(process.cwd(), 'ia', 'specs', 'glossary.md');
  try {
    await fs.access(fromRoot);
    return fromRoot;
  } catch {
    // cwd is already web/ — go one level up
    return path.join(process.cwd(), '..', 'ia', 'specs', 'glossary.md');
  }
}

// ---------------------------------------------------------------------------
// Slug derivation
// ---------------------------------------------------------------------------

/** Produce a URL-safe kebab-case slug from a glossary term string.
 *
 *  Examples:
 *    "HeightMap"              → "heightmap"
 *    "World ↔ Grid conversion" → "world-grid-conversion"
 *    "A* search"              → "a-search"
 *    "HeightMap[x,y]"         → "heightmap"
 */
function slugify(raw: string): string {
  return raw
    .replace(/\*\*/g, '')          // strip bold markers
    .replace(/\[.*?\]/g, '')       // drop [bracketed] disambiguation
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')  // non-alphanumeric runs → hyphen
    .replace(/^-+|-+$/g, '');     // trim leading/trailing hyphens
}

/** Deduplicate slugs in source order: first occurrence keeps base slug,
 *  subsequent collisions get suffix -2, -3, … */
function deduplicateSlugs(terms: Omit<GlossaryTerm, 'slug'>[], slugs: string[]): GlossaryTerm[] {
  const seen = new Map<string, number>();
  return terms.map((t, i) => {
    const base = slugs[i];
    const count = seen.get(base) ?? 0;
    seen.set(base, count + 1);
    const slug = count === 0 ? base : `${base}-${count + 1}`;
    return { ...t, slug };
  });
}

// ---------------------------------------------------------------------------
// Markdown table parser
// ---------------------------------------------------------------------------

const SKIP_HEADINGS = new Set(['Index (quick skim)', 'Planned terminology', 'Planned domain terms']);

/** Parse raw glossary markdown. Returns unslugged term rows in source order. */
function parseGlossary(source: string): Omit<GlossaryTerm, 'slug'>[] {
  const lines = source.split('\n');
  const result: Omit<GlossaryTerm, 'slug'>[] = [];

  let currentCategory = '';
  let inSkippedSection = false;
  let headerSkipped = false; // skip per-section table header + separator rows

  for (const line of lines) {
    // Detect section heading
    if (line.startsWith('## ')) {
      const heading = line.slice(3).trim();
      inSkippedSection = SKIP_HEADINGS.has(heading);
      currentCategory = inSkippedSection ? '' : heading;
      headerSkipped = false;
      continue;
    }

    if (inSkippedSection || !currentCategory) continue;

    // Table rows start and end with |
    if (!line.startsWith('|') || !line.endsWith('|')) continue;

    // Skip separator rows (|---|---|---|)
    if (/^\|[-| :]+\|$/.test(line)) continue;

    const cells = line
      .slice(1, -1)       // drop leading/trailing |
      .split('|')
      .map(c => c.trim());

    if (cells.length < 2) continue;

    // First data row after heading is the header row (Term | Definition | Spec)
    if (!headerSkipped) {
      headerSkipped = true;
      continue;
    }

    const rawTerm = cells[0];
    const definition = cells[1];

    // Strip bold markers from term
    const term = rawTerm.replace(/^\*\*|\*\*$/g, '').trim();

    if (!term || !definition) continue;

    result.push({ term, definition, category: currentCategory });
  }

  return result;
}

// ---------------------------------------------------------------------------
// Validation smoke check
// ---------------------------------------------------------------------------

const SLUG_RE = /^[a-z0-9]+(-[a-z0-9]+)*$/;

function validateTerms(terms: GlossaryTerm[]): void {
  const categorySeen = new Set<string>();
  for (const t of terms) {
    categorySeen.add(t.category);
  }

  if (categorySeen.size === 0) {
    throw new Error('Glossary import: no terms found — check glossary.md path and format');
  }

  const invalid = terms.filter(t => !SLUG_RE.test(t.slug));
  if (invalid.length > 0) {
    const examples = invalid.slice(0, 3).map(t => `"${t.term}" → "${t.slug}"`).join(', ');
    throw new Error(`Glossary import: invalid slugs: ${examples}`);
  }

  const emptyDef = terms.filter(t => !t.definition.trim());
  if (emptyDef.length > 0) {
    const examples = emptyDef.slice(0, 3).map(t => `"${t.term}"`).join(', ');
    throw new Error(`Glossary import: empty definitions for: ${examples}`);
  }
}

// ---------------------------------------------------------------------------
// Public API
// ---------------------------------------------------------------------------

/** Load and parse all glossary terms from ia/specs/glossary.md.
 *  Runs at build time (Next.js RSC / generateStaticParams / build scripts).
 *  Returns GlossaryTerm[] in source order with stable kebab-case slugs. */
export async function loadGlossaryTerms(): Promise<GlossaryTerm[]> {
  const filePath = await resolveGlossaryPath();

  let raw: string;
  try {
    raw = await fs.readFile(filePath, 'utf8');
  } catch (err) {
    const e = err as NodeJS.ErrnoException;
    if (e.code === 'ENOENT') {
      return [];
    }
    throw err;
  }

  const unslugged = parseGlossary(raw);
  const slugs = unslugged.map(t => slugify(t.term));
  const terms = deduplicateSlugs(unslugged, slugs);

  validateTerms(terms);

  return terms;
}
