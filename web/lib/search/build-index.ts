import fs from 'fs/promises';
import path from 'path';
import matter from 'gray-matter';
import { loadGlossaryTerms } from '../glossary/import';
import type { SearchRecord } from './types';

// ---------------------------------------------------------------------------
// Path resolution — mirrors loader.ts cwd-dual pattern
// ---------------------------------------------------------------------------

/** Resolve absolute path to web/content/wiki/.
 *  cwd may be repo root OR web/ (Next build). */
async function resolveWikiDir(): Promise<string> {
  const fromRoot = path.join(process.cwd(), 'web', 'content', 'wiki');
  try {
    await fs.access(fromRoot);
    return fromRoot;
  } catch {
    return path.join(process.cwd(), 'content', 'wiki');
  }
}

/** Resolve absolute path to web/public/ for output.
 *  cwd may be repo root OR web/. */
async function resolvePublicDir(): Promise<string> {
  const fromRoot = path.join(process.cwd(), 'web', 'public');
  try {
    await fs.access(fromRoot);
    return fromRoot;
  } catch {
    return path.join(process.cwd(), 'public');
  }
}

// ---------------------------------------------------------------------------
// Wiki MDX glob (recursive)
// ---------------------------------------------------------------------------

/** Recursively collect all .mdx file paths under dir. */
async function collectMdxFiles(dir: string): Promise<string[]> {
  const entries = await fs.readdir(dir, { withFileTypes: true });
  const results: string[] = [];
  for (const entry of entries) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      results.push(...(await collectMdxFiles(full)));
    } else if (entry.isFile() && entry.name.endsWith('.mdx')) {
      results.push(full);
    }
  }
  return results;
}

// ---------------------------------------------------------------------------
// Core builder
// ---------------------------------------------------------------------------

/** Build search index records from glossary terms + wiki MDX files. */
export async function buildSearchIndex(): Promise<SearchRecord[]> {
  const records: SearchRecord[] = [];

  // Glossary records
  const glossaryTerms = await loadGlossaryTerms();
  for (const t of glossaryTerms) {
    records.push({
      slug: t.slug,
      title: t.term,
      body: t.definition,
      category: t.category,
      type: 'glossary',
    });
  }

  // Wiki records
  const wikiDir = await resolveWikiDir();
  const mdxFiles = await collectMdxFiles(wikiDir);

  for (const filePath of mdxFiles) {
    const raw = await fs.readFile(filePath, 'utf8');
    const parsed = matter(raw);
    const title = (parsed.data['title'] as string | undefined) ?? path.basename(filePath, '.mdx');
    // slug = path relative to wikiDir, without .mdx extension, forward-slash separated
    const rel = path.relative(wikiDir, filePath).replace(/\\/g, '/').replace(/\.mdx$/, '');
    records.push({
      slug: rel,
      title,
      body: parsed.content,
      category: 'wiki',
      type: 'wiki',
    });
  }

  // Deterministic sort by slug ascending
  records.sort((a, b) => (a.slug < b.slug ? -1 : a.slug > b.slug ? 1 : 0));

  return records;
}

// ---------------------------------------------------------------------------
// CLI entrypoint
// ---------------------------------------------------------------------------

async function main(): Promise<void> {
  const records = await buildSearchIndex();

  const publicDir = await resolvePublicDir();
  await fs.mkdir(publicDir, { recursive: true });

  const outPath = path.join(publicDir, 'search-index.json');
  await fs.writeFile(outPath, JSON.stringify(records, null, 2) + '\n', 'utf8');

  console.log(`search-index.json written — ${records.length} records → ${outPath}`);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
