import fs from 'fs/promises';
import path from 'path';
import matter from 'gray-matter';
import type { MdxLoadResult, PageFrontmatter } from './types';

/** Resolve absolute path to a content file.
 *  cwd may be repo root (validate:web) or web/ (Next dev/build). */
async function resolveContentPath(dir: string, slug: string): Promise<string> {
  const fromRoot = path.join(process.cwd(), 'web', 'content', dir, `${slug}.mdx`);
  try {
    await fs.access(fromRoot);
    return fromRoot;
  } catch {
    // cwd is already web/
    return path.join(process.cwd(), 'content', dir, `${slug}.mdx`);
  }
}

/**
 * Generic MDX loader. Reads {dir}/{slug}.mdx, parses frontmatter, validates
 * required fields, returns { source, frontmatter }.
 */
export async function loadMdxContent<T = PageFrontmatter>(
  dir: string,
  slug: string
): Promise<MdxLoadResult<T>> {
  const filePath = await resolveContentPath(dir, slug);

  let raw: string;
  try {
    raw = await fs.readFile(filePath, 'utf8');
  } catch (err) {
    const e = err as NodeJS.ErrnoException;
    if (e.code === 'ENOENT') {
      throw new Error(`MDX file not found: dir="${dir}" slug="${slug}" path="${filePath}"`);
    }
    throw err;
  }

  const parsed = matter(raw);
  const frontmatter = parsed.data as T;

  // Required-field + ISO-date validation (Phase 3 — runs for PageFrontmatter shape).
  validatePageFrontmatter(frontmatter, slug, dir);

  return { source: parsed.content, frontmatter };
}

/** Thin wrapper for top-level pages. */
export async function loadMdxPage(slug: string): Promise<MdxLoadResult<PageFrontmatter>> {
  return loadMdxContent<PageFrontmatter>('pages', slug);
}

// ---------------------------------------------------------------------------
// Internal validation (Phase 3)
// ---------------------------------------------------------------------------

const ISO_DATE_RE = /^\d{4}-\d{2}-\d{2}$/;
const REQUIRED_FIELDS = ['title', 'description', 'updated'] as const;

function validatePageFrontmatter(data: unknown, slug: string, dir: string): void {
  if (typeof data !== 'object' || data === null) {
    throw new Error(`MDX frontmatter missing or non-object: dir="${dir}" slug="${slug}"`);
  }

  const record = data as Record<string, unknown>;
  const missingFields: string[] = [];

  for (const field of REQUIRED_FIELDS) {
    const val = record[field];
    if (typeof val !== 'string' || val.trim() === '') {
      missingFields.push(field);
    }
  }

  if (missingFields.length > 0) {
    throw new Error(
      `MDX frontmatter missing required fields: ${missingFields.join(', ')} — dir="${dir}" slug="${slug}"`
    );
  }

  const updated = record['updated'] as string;
  if (!ISO_DATE_RE.test(updated)) {
    throw new Error(
      `MDX frontmatter "updated" must be YYYY-MM-DD, got "${updated}" — dir="${dir}" slug="${slug}"`
    );
  }
}
