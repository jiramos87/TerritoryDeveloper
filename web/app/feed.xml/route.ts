/**
 * RSS 2.0 feed route — /feed.xml
 * Returns latest 20 devlog posts sorted desc by date.
 * Content-Type: application/rss+xml; charset=utf-8
 */

import fs from 'fs/promises';
import path from 'path';
import matter from 'gray-matter';
import { getBaseUrl } from '@/lib/site/base-url';
import type { DevlogFrontmatter } from '@/lib/mdx/types';

export const dynamic = 'force-static';

// ---------------------------------------------------------------------------
// Phase 1 — Devlog scan helper
// ---------------------------------------------------------------------------

interface DevlogEntry {
  slug: string;
  frontmatter: DevlogFrontmatter;
}

/** Resolve absolute path to web/content/devlog/ regardless of cwd. */
async function resolveDevlogDir(): Promise<string> {
  const fromRoot = path.join(process.cwd(), 'web', 'content', 'devlog');
  try {
    await fs.access(fromRoot);
    return fromRoot;
  } catch {
    // cwd is already web/
    return path.join(process.cwd(), 'content', 'devlog');
  }
}

/** Scan devlog directory → sorted-desc entries (latest first). */
async function scanDevlogPosts(): Promise<DevlogEntry[]> {
  const dir = await resolveDevlogDir();
  let entries: string[];
  try {
    entries = await fs.readdir(dir);
  } catch {
    return [];
  }

  const stems = entries.filter((f) => f.endsWith('.mdx')).map((f) => f.replace(/\.mdx$/, ''));

  const posts = await Promise.all(
    stems.map(async (stem): Promise<DevlogEntry | null> => {
      try {
        const raw = await fs.readFile(path.join(dir, `${stem}.mdx`), 'utf8');
        const { data } = matter(raw);
        const fm = data as DevlogFrontmatter;
        // Validate required fields; skip malformed posts silently.
        if (
          typeof fm.title !== 'string' ||
          typeof fm.date !== 'string' ||
          typeof fm.excerpt !== 'string'
        ) {
          return null;
        }
        return { slug: stem, frontmatter: fm };
      } catch {
        return null;
      }
    })
  );

  return (posts.filter(Boolean) as DevlogEntry[]).sort(
    (a, b) => new Date(b.frontmatter.date).getTime() - new Date(a.frontmatter.date).getTime()
  );
}

// ---------------------------------------------------------------------------
// Phase 2 — RSS XML builder
// ---------------------------------------------------------------------------

/** Escape XML special characters in text content. */
function xmlEscape(text: string): string {
  return text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&apos;');
}

interface RssChannel {
  title: string;
  link: string;
  description: string;
  language: string;
  lastBuildDate: string;
}

interface RssItem {
  title: string;
  link: string;
  description: string;
  pubDate: string;
  guid: string;
}

function buildRssXml(items: RssItem[], channel: RssChannel): string {
  const itemsXml = items
    .map(
      (item) => `
    <item>
      <title>${xmlEscape(item.title)}</title>
      <link>${item.link}</link>
      <description>${xmlEscape(item.description)}</description>
      <pubDate>${item.pubDate}</pubDate>
      <guid isPermaLink="true">${item.guid}</guid>
    </item>`
    )
    .join('');

  return `<?xml version="1.0" encoding="UTF-8"?>
<rss version="2.0">
  <channel>
    <title>${xmlEscape(channel.title)}</title>
    <link>${channel.link}</link>
    <description>${xmlEscape(channel.description)}</description>
    <language>${channel.language}</language>
    <lastBuildDate>${channel.lastBuildDate}</lastBuildDate>${itemsXml}
  </channel>
</rss>`;
}

// ---------------------------------------------------------------------------
// Phase 3 — Route handler
// ---------------------------------------------------------------------------

export async function GET(): Promise<Response> {
  const base = getBaseUrl();
  const allPosts = await scanDevlogPosts();
  const topPosts = allPosts.slice(0, 20);

  const lastBuildDate =
    topPosts.length > 0
      ? new Date(topPosts[0].frontmatter.date).toUTCString()
      : new Date().toUTCString();

  const channel: RssChannel = {
    title: 'Territory Developer Devlog',
    link: base,
    description: 'Development updates from Territory — an isometric city builder by Bacayo Studio.',
    language: 'en',
    lastBuildDate,
  };

  const items: RssItem[] = topPosts.map((post) => {
    const link = `${base}/devlog/${post.slug}`;
    return {
      title: post.frontmatter.title,
      link,
      description: post.frontmatter.excerpt,
      pubDate: new Date(post.frontmatter.date).toUTCString(),
      guid: link,
    };
  });

  const xml = buildRssXml(items, channel);

  return new Response(xml, {
    headers: {
      'Content-Type': 'application/rss+xml; charset=utf-8',
    },
  });
}
