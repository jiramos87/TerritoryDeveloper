import type { Metadata } from 'next';
import fs from 'fs/promises';
import path from 'path';
import matter from 'gray-matter';
import Link from 'next/link';
import type { DevlogFrontmatter } from '@/lib/mdx/types';
import { computeReadingTime } from '@/lib/mdx/reading-time';
import { Breadcrumb } from '@/components/Breadcrumb';
import { Prose } from '@/components/type/Prose';

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
      className="min-h-screen max-w-[800px] mx-auto bg-[var(--ds-bg-canvas)] text-[var(--ds-text-primary)] font-sans py-[var(--ds-spacing-xl)] px-[var(--ds-spacing-md)]"
    >
      <Breadcrumb crumbs={[{ label: 'Home', href: '/' }, { label: 'Devlog' }]} />
      <header
        className="mb-[var(--ds-spacing-xl)] border-b border-[var(--ds-bg-panel)] pb-[var(--ds-spacing-md)]"
      >
        <h1
          className="m-0 font-mono text-[1.5rem] leading-8 text-[var(--ds-text-primary)]"
        >
          Devlog
        </h1>
        <p
          className="m-0 mt-[var(--ds-spacing-xs)] text-[1rem] leading-6 text-[var(--ds-text-muted)]"
        >
          Development log for Territory, an isometric city builder.
        </p>
      </header>

      <Prose>
        {entries.length === 0 ? (
          <p className="text-[1rem] leading-6 text-[var(--ds-text-muted)] m-0">
            No posts yet — check back soon.
          </p>
        ) : (
          <ul className="list-none p-0 m-0">
            {entries.map((entry) => (
              <li
                key={entry.slug}
                className="mb-[var(--ds-spacing-xl)] pb-[var(--ds-spacing-lg)] border-b border-[var(--ds-bg-panel)]"
              >
                <div
                  className="flex gap-[var(--ds-spacing-sm)] items-center mb-[var(--ds-spacing-xs)]"
                >
                  <time
                    dateTime={entry.frontmatter.date}
                    className="font-mono text-sm leading-5 text-[var(--ds-text-muted)]"
                  >
                    {entry.frontmatter.date}
                  </time>
                  <span className="text-sm leading-5 text-[var(--ds-text-muted)]">
                    {entry.readingTime} min read
                  </span>
                </div>

                <h2
                  className="font-mono text-xl leading-7 m-0 mb-[var(--ds-spacing-xs)]"
                >
                  <Link
                    href={`/devlog/${entry.slug}`}
                    className="text-[var(--ds-text-primary)] underline"
                  >
                    {entry.frontmatter.title}
                  </Link>
                </h2>

                <p
                  className="m-0 mb-[var(--ds-spacing-sm)] text-[1rem] leading-6 text-[var(--ds-text-muted)]"
                >
                  {entry.frontmatter.excerpt}
                </p>

                {entry.frontmatter.tags.length > 0 && (
                  <div className="flex flex-wrap gap-[var(--ds-spacing-xs)]">
                    {entry.frontmatter.tags.map((tag) => (
                      <span
                        key={tag}
                        className="inline-block font-mono text-xs leading-4 text-[var(--ds-text-muted)] bg-[var(--ds-bg-panel)] rounded-full [padding:2px_var(--ds-spacing-xs)]"
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
      </Prose>
    </main>
  );
}
