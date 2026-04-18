import type { Metadata } from 'next';
import fs from 'fs/promises';
import path from 'path';
import matter from 'gray-matter';
import Link from 'next/link';
import type { DevlogFrontmatter } from '@/lib/mdx/types';
import { computeReadingTime } from '@/lib/mdx/reading-time';
import { tokens } from '@/lib/tokens';
import { Breadcrumb } from '@/components/Breadcrumb';

export const metadata: Metadata = {
  title: 'Devlog — Territory',
  description: 'Development log for Territory, an isometric city builder.',
};

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

interface DevlogEntry {
  slug: string;
  frontmatter: DevlogFrontmatter;
  readingTime: number;
}

// ---------------------------------------------------------------------------
// Data loader
// ---------------------------------------------------------------------------

/** Resolve absolute path to web/content/devlog/.
 *  cwd may be repo root (validate:web) or web/ (Next dev/build). */
async function resolveDevlogDir(): Promise<string> {
  const fromRoot = path.join(process.cwd(), 'web', 'content', 'devlog');
  try {
    await fs.access(fromRoot);
    return fromRoot;
  } catch {
    return path.join(process.cwd(), 'content', 'devlog');
  }
}

async function loadDevlogEntries(): Promise<DevlogEntry[]> {
  const dir = await resolveDevlogDir();

  let files: string[];
  try {
    const all = await fs.readdir(dir);
    files = all.filter((f) => f.endsWith('.mdx'));
  } catch {
    return [];
  }

  const entries: DevlogEntry[] = [];

  for (const file of files) {
    const slug = file.replace(/\.mdx$/, '');
    try {
      const raw = await fs.readFile(path.join(dir, file), 'utf8');
      const parsed = matter(raw);
      const frontmatter = parsed.data as DevlogFrontmatter;
      const readingTime = computeReadingTime(parsed.content);
      entries.push({ slug, frontmatter, readingTime });
    } catch {
      // skip malformed files
    }
  }

  // sort descending by date
  entries.sort((a, b) => b.frontmatter.date.localeCompare(a.frontmatter.date));

  return entries;
}

// ---------------------------------------------------------------------------
// Page
// ---------------------------------------------------------------------------

export default async function DevlogListPage() {
  const entries = await loadDevlogEntries();

  return (
    <main
      style={{
        minHeight: '100vh',
        backgroundColor: tokens.colors['bg-canvas'],
        color: tokens.colors['text-primary'],
        fontFamily: tokens.fontFamily.sans.join(', '),
        padding: `${tokens.spacing[8]} ${tokens.spacing[4]}`,
        maxWidth: '800px',
        margin: '0 auto',
      }}
    >
      <Breadcrumb crumbs={[{ label: 'Home', href: '/' }, { label: 'Devlog' }]} />
      <header
        style={{
          marginBottom: tokens.spacing[8],
          borderBottom: `1px solid ${tokens.colors['bg-panel']}`,
          paddingBottom: tokens.spacing[4],
        }}
      >
        <h1
          style={{
            fontSize: tokens.fontSize['2xl'][0],
            lineHeight: tokens.fontSize['2xl'][1],
            fontFamily: tokens.fontFamily.mono.join(', '),
            color: tokens.colors['text-primary'],
            margin: 0,
          }}
        >
          Devlog
        </h1>
        <p
          style={{
            fontSize: tokens.fontSize.base[0],
            lineHeight: tokens.fontSize.base[1],
            color: tokens.colors['text-muted'],
            marginTop: tokens.spacing[2],
            marginBottom: 0,
          }}
        >
          Development log for Territory, an isometric city builder.
        </p>
      </header>

      <section>
        {entries.length === 0 ? (
          <p
            style={{
              color: tokens.colors['text-muted'],
              fontSize: tokens.fontSize.base[0],
              lineHeight: tokens.fontSize.base[1],
            }}
          >
            No posts yet — check back soon.
          </p>
        ) : (
          <ul style={{ listStyle: 'none', padding: 0, margin: 0 }}>
            {entries.map((entry) => (
              <li
                key={entry.slug}
                style={{
                  marginBottom: tokens.spacing[8],
                  paddingBottom: tokens.spacing[6],
                  borderBottom: `1px solid ${tokens.colors['bg-panel']}`,
                }}
              >
                {/* Date + reading time */}
                <div
                  style={{
                    display: 'flex',
                    gap: tokens.spacing[3],
                    alignItems: 'center',
                    marginBottom: tokens.spacing[2],
                  }}
                >
                  <time
                    dateTime={entry.frontmatter.date}
                    style={{
                      fontSize: tokens.fontSize.sm[0],
                      lineHeight: tokens.fontSize.sm[1],
                      fontFamily: tokens.fontFamily.mono.join(', '),
                      color: tokens.colors['text-muted'],
                    }}
                  >
                    {entry.frontmatter.date}
                  </time>
                  <span
                    style={{
                      fontSize: tokens.fontSize.sm[0],
                      lineHeight: tokens.fontSize.sm[1],
                      color: tokens.colors['text-muted'],
                    }}
                  >
                    {entry.readingTime} min read
                  </span>
                </div>

                {/* Title */}
                <h2
                  style={{
                    fontSize: tokens.fontSize.xl[0],
                    lineHeight: tokens.fontSize.xl[1],
                    fontFamily: tokens.fontFamily.mono.join(', '),
                    color: tokens.colors['text-primary'],
                    margin: 0,
                    marginBottom: tokens.spacing[2],
                  }}
                >
                  <Link
                    href={`/devlog/${entry.slug}`}
                    style={{
                      color: tokens.colors['text-primary'],
                      textDecoration: 'underline',
                    }}
                  >
                    {entry.frontmatter.title}
                  </Link>
                </h2>

                {/* Excerpt */}
                <p
                  style={{
                    fontSize: tokens.fontSize.base[0],
                    lineHeight: tokens.fontSize.base[1],
                    color: tokens.colors['text-muted'],
                    margin: 0,
                    marginBottom: tokens.spacing[3],
                  }}
                >
                  {entry.frontmatter.excerpt}
                </p>

                {/* Tag chips */}
                {entry.frontmatter.tags.length > 0 && (
                  <div style={{ display: 'flex', gap: tokens.spacing[2], flexWrap: 'wrap' }}>
                    {entry.frontmatter.tags.map((tag) => (
                      <span
                        key={tag}
                        style={{
                          display: 'inline-block',
                          fontSize: tokens.fontSize.xs[0],
                          lineHeight: tokens.fontSize.xs[1],
                          fontFamily: tokens.fontFamily.mono.join(', '),
                          color: tokens.colors['text-muted'],
                          backgroundColor: tokens.colors['bg-panel'],
                          borderRadius: '9999px',
                          padding: `2px ${tokens.spacing[2]}`,
                        }}
                      >
                        {tag}
                      </span>
                    ))}
                  </div>
                )}
              </li>
            ))}
          </ul>
        )}
      </section>
    </main>
  );
}
