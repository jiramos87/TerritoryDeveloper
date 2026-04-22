import type { Metadata } from 'next';
import fs from 'fs/promises';
import path from 'path';
import { notFound } from 'next/navigation';
import { loadDevlogPost } from '@/lib/mdx/loader';
import { computeReadingTime } from '@/lib/mdx/reading-time';
import { Breadcrumb } from '@/components/Breadcrumb';
import { Prose } from '@/components/type/Prose';

// ---------------------------------------------------------------------------
// Static params
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

export async function generateStaticParams(): Promise<{ slug: string }[]> {
  const dir = await resolveDevlogDir();
  let files: string[];
  try {
    const all = await fs.readdir(dir);
    files = all.filter((f) => f.endsWith('.mdx'));
  } catch {
    return [];
  }
  return files.map((f) => ({ slug: f.replace(/\.mdx$/, '') }));
}

export const dynamicParams = false;

// ---------------------------------------------------------------------------
// Metadata
// ---------------------------------------------------------------------------

export async function generateMetadata({
  params,
}: {
  params: Promise<{ slug: string }>;
}): Promise<Metadata> {
  const { slug } = await params;
  try {
    const { frontmatter } = await loadDevlogPost(slug);
    return {
      title: `${frontmatter.title} — Territory Devlog`,
      description: frontmatter.excerpt,
      openGraph: {
        images: [frontmatter.cover ?? '/og-default.png'],
      },
    };
  } catch {
    return {};
  }
}

// ---------------------------------------------------------------------------
// Page
// ---------------------------------------------------------------------------

export default async function DevlogPostPage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const { slug } = await params;

  let result: Awaited<ReturnType<typeof loadDevlogPost>>;
  try {
    result = await loadDevlogPost(slug);
  } catch {
    notFound();
  }

  const { Component, frontmatter, source } = result;
  const readingTime = computeReadingTime(source);

  return (
    <main
      className="min-h-screen max-w-[800px] mx-auto bg-[var(--ds-bg-canvas)] text-[var(--ds-text-primary)] font-sans py-[var(--ds-spacing-xl)] px-[var(--ds-spacing-md)]"
    >
      <Breadcrumb crumbs={[{ label: 'Home', href: '/' }, { label: 'Devlog', href: '/devlog' }, { label: frontmatter.title }]} />
      <header
        className="mb-[var(--ds-spacing-xl)] border-b border-[var(--ds-bg-panel)] pb-[var(--ds-spacing-md)]"
      >
        <h1
          className="m-0 mb-[var(--ds-spacing-sm)] font-mono text-[1.5rem] leading-8 text-[var(--ds-text-primary)]"
        >
          {frontmatter.title}
        </h1>

        <div
          className="flex gap-[var(--ds-spacing-sm)] items-center mb-[var(--ds-spacing-sm)]"
        >
          <time
            dateTime={frontmatter.date}
            className="font-mono text-sm leading-5 text-[var(--ds-text-muted)]"
          >
            {frontmatter.date}
          </time>
          <span className="text-sm leading-5 text-[var(--ds-text-muted)]">
            {readingTime} min read
          </span>
        </div>

        {frontmatter.tags.length > 0 && (
          <div
            className="flex flex-wrap gap-[var(--ds-spacing-xs)] mb-[var(--ds-spacing-sm)]"
          >
            {frontmatter.tags.map((tag) => (
              <span
                key={tag}
                className="inline-block font-mono text-xs leading-4 text-[var(--ds-text-muted)] bg-[var(--ds-bg-panel)] rounded-full [padding:2px_var(--ds-spacing-xs)]"
              >
                {tag}
              </span>
            ))}
          </div>
        )}

        {frontmatter.cover && (
          // eslint-disable-next-line @next/next/no-img-element
          <img
            src={frontmatter.cover}
            alt={frontmatter.title}
            className="w-full max-h-[400px] object-cover rounded mt-[var(--ds-spacing-md)]"
          />
        )}
      </header>

      <article>
        <Prose>
          <Component />
        </Prose>
      </article>
    </main>
  );
}
