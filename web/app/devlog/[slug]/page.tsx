import type { Metadata } from 'next';
import fs from 'fs/promises';
import path from 'path';
import { notFound } from 'next/navigation';
import { loadDevlogPost } from '@/lib/mdx/loader';
import { computeReadingTime } from '@/lib/mdx/reading-time';
import { tokens } from '@/lib/tokens';
import { Breadcrumb } from '@/components/Breadcrumb';

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
      <Breadcrumb crumbs={[{ label: 'Home', href: '/' }, { label: 'Devlog', href: '/devlog' }, { label: frontmatter.title }]} />
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
            marginBottom: tokens.spacing[3],
          }}
        >
          {frontmatter.title}
        </h1>

        {/* Date + reading time */}
        <div
          style={{
            display: 'flex',
            gap: tokens.spacing[3],
            alignItems: 'center',
            marginBottom: tokens.spacing[3],
          }}
        >
          <time
            dateTime={frontmatter.date}
            style={{
              fontSize: tokens.fontSize.sm[0],
              lineHeight: tokens.fontSize.sm[1],
              fontFamily: tokens.fontFamily.mono.join(', '),
              color: tokens.colors['text-muted'],
            }}
          >
            {frontmatter.date}
          </time>
          <span
            style={{
              fontSize: tokens.fontSize.sm[0],
              lineHeight: tokens.fontSize.sm[1],
              color: tokens.colors['text-muted'],
            }}
          >
            {readingTime} min read
          </span>
        </div>

        {/* Tag chips */}
        {frontmatter.tags.length > 0 && (
          <div
            style={{
              display: 'flex',
              gap: tokens.spacing[2],
              flexWrap: 'wrap',
              marginBottom: tokens.spacing[3],
            }}
          >
            {frontmatter.tags.map((tag) => (
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

        {/* Optional cover image */}
        {frontmatter.cover && (
          // eslint-disable-next-line @next/next/no-img-element
          <img
            src={frontmatter.cover}
            alt={frontmatter.title}
            style={{
              width: '100%',
              maxHeight: '400px',
              objectFit: 'cover',
              borderRadius: '4px',
              marginTop: tokens.spacing[4],
            }}
          />
        )}
      </header>

      {/* MDX body — compiled React component */}
      <article
        style={{
          fontSize: tokens.fontSize.base[0],
          lineHeight: tokens.fontSize.base[1],
          color: tokens.colors['text-primary'],
        }}
      >
        <Component />
      </article>
    </main>
  );
}
