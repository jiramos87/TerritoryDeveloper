import type { MetadataRoute } from 'next';
import fs from 'fs/promises';
import path from 'path';
import { getBaseUrl } from '@/lib/site/base-url';
import { loadMdxPage } from '@/lib/mdx/loader';

/** Resolve absolute path to web/content/pages/ regardless of cwd. */
async function resolvePagesDir(): Promise<string> {
  const fromRoot = path.join(process.cwd(), 'web', 'content', 'pages');
  try {
    await fs.access(fromRoot);
    return fromRoot;
  } catch {
    return path.join(process.cwd(), 'content', 'pages');
  }
}

/** Map MDX filename stem to a URL path segment. `landing` → `''`. */
function stemToSlug(stem: string): string {
  return stem === 'landing' ? '' : stem;
}

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const pagesDir = await resolvePagesDir();
  const entries = await fs.readdir(pagesDir);
  const stems = entries
    .filter((f) => f.endsWith('.mdx'))
    .map((f) => f.replace(/\.mdx$/, ''));

  const base = getBaseUrl();

  const items = await Promise.all(
    stems.map(async (stem) => {
      const slug = stemToSlug(stem);
      let lastModified: Date;
      try {
        const { frontmatter } = await loadMdxPage(stem);
        lastModified = new Date(frontmatter.updated);
      } catch {
        lastModified = new Date();
      }
      return {
        url: `${base}/${slug}`.replace(/\/$/, '') || base,
        lastModified,
        changeFrequency: 'weekly' as const,
        priority: slug === '' ? 1.0 : 0.8,
      };
    })
  );

  return items;
}
