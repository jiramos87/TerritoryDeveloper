import fs from 'fs/promises';
import path from 'path';
import { loadGlossaryTerms } from '@/lib/glossary/import';

// ---------------------------------------------------------------------------
// Path resolution — mirrors resolveContentPath pattern from lib/mdx/loader.ts
// ---------------------------------------------------------------------------

async function resolveWikiContentDir(): Promise<string> {
  const fromRoot = path.join(process.cwd(), 'web', 'content', 'wiki');
  try {
    await fs.access(fromRoot);
    return fromRoot;
  } catch {
    // cwd is already web/
    return path.join(process.cwd(), 'content', 'wiki');
  }
}

// ---------------------------------------------------------------------------
// MDX glob — strip .mdx suffix, return slugs
// ---------------------------------------------------------------------------

async function globMdxSlugs(): Promise<Array<{ slug: string; source: 'mdx' }>> {
  const dir = await resolveWikiContentDir();
  let entries: string[];
  try {
    entries = await fs.readdir(dir);
  } catch {
    // wiki content dir may not exist yet
    return [];
  }
  return entries
    .filter((f) => f.endsWith('.mdx'))
    .map((f) => ({ slug: f.slice(0, -4).toLowerCase(), source: 'mdx' as const }));
}

// ---------------------------------------------------------------------------
// Public API
// ---------------------------------------------------------------------------

export interface WikiSlugEntry {
  slug: string;
  source: 'mdx' | 'glossary';
}

/**
 * Union of MDX wiki files and glossary term slugs.
 * MDX wins on collision — hand-authored content overrides glossary shell.
 * Used by wiki/page.tsx index and wiki/[...slug]/page.tsx generateStaticParams.
 */
export async function listWikiSlugs(): Promise<WikiSlugEntry[]> {
  const mdx = await globMdxSlugs();
  const glossaryTerms = await loadGlossaryTerms();
  const glossary = glossaryTerms.map((t) => ({ slug: t.slug, source: 'glossary' as const }));

  const mdxSet = new Set(mdx.map((s) => s.slug));
  return [...mdx, ...glossary.filter((g) => !mdxSet.has(g.slug))];
}
