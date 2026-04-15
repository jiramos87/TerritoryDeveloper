import type { MetadataRoute } from 'next';
import fs from 'fs/promises';
import path from 'path';
import matter from 'gray-matter';
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

/** Resolve absolute path to web/content/devlog/ regardless of cwd. */
async function resolveDevlogDir(): Promise<string> {
  const fromRoot = path.join(process.cwd(), 'web', 'content', 'devlog');
  try {
    await fs.access(fromRoot);
    return fromRoot;
  } catch {
    return path.join(process.cwd(), 'content', 'devlog');
  }
}

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const pagesDir = await resolvePagesDir();
  const entries = await fs.readdir(pagesDir);
  const stems = entries
    .filter((f) => f.endsWith('.mdx'))
    .map((f) => f.replace(/\.mdx$/, ''));

  const base = getBaseUrl();

  const pageItems = await Promise.all(
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

  // Devlog entries
  const devlogDir = await resolveDevlogDir();
  let devlogFiles: string[] = [];
  try {
    const all = await fs.readdir(devlogDir);
    devlogFiles = all.filter((f) => f.endsWith('.mdx'));
  } catch {
    devlogFiles = [];
  }

  const devlogDates: Date[] = [];
  const devlogItems: MetadataRoute.Sitemap = [];

  for (const file of devlogFiles) {
    const stem = file.replace(/\.mdx$/, '');
    let postDate: Date;
    try {
      const raw = await fs.readFile(path.join(devlogDir, file), 'utf8');
      const { data } = matter(raw);
      postDate = new Date(data.date as string);
    } catch {
      postDate = new Date();
    }
    devlogDates.push(postDate);
    devlogItems.push({
      url: `${base}/devlog/${stem}`,
      lastModified: postDate,
      changeFrequency: 'weekly' as const,
      priority: 0.6,
    });
  }

  // /devlog index entry — lastModified = max date across posts
  const indexDate =
    devlogDates.length > 0
      ? new Date(Math.max(...devlogDates.map((d) => d.getTime())))
      : new Date();

  const devlogIndex: MetadataRoute.Sitemap[number] = {
    url: `${base}/devlog`,
    lastModified: indexDate,
    changeFrequency: 'weekly' as const,
    priority: 0.7,
  };

  return [...pageItems, devlogIndex, ...devlogItems];
}
