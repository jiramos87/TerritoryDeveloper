import type { Metadata } from 'next';
import { notFound } from 'next/navigation';
import { loadMdxContent } from '@/lib/mdx/loader';
import { loadGlossaryTerms } from '@/lib/glossary/import';
import { listWikiSlugs } from '@/lib/wiki/slugs';
import { GlossaryShell } from '@/components/GlossaryShell';
import { tokens } from '@/lib/tokens';
import { Breadcrumb } from '@/components/Breadcrumb';
import type { PageFrontmatter } from '@/lib/mdx/types';
import type { GlossaryTerm } from '@/lib/glossary/types';

// ---------------------------------------------------------------------------
// Static params — union of MDX files + glossary slugs; MDX wins on collision
// ---------------------------------------------------------------------------

export async function generateStaticParams(): Promise<{ slug: string[] }[]> {
  const entries = await listWikiSlugs();
  return entries.map(({ slug }) => ({ slug: slug.split('/') }));
}

// ---------------------------------------------------------------------------
// Resolution helper — returns null when not found
// ---------------------------------------------------------------------------

interface MdxResult {
  kind: 'mdx';
  source: string;
  frontmatter: PageFrontmatter;
}

interface GlossaryResult {
  kind: 'glossary';
  term: GlossaryTerm;
}

type WikiContent = MdxResult | GlossaryResult | null;

async function resolveWikiContent(joined: string): Promise<WikiContent> {
  // 1. Try MDX
  let mdxResult: { source: string; frontmatter: PageFrontmatter } | null = null;
  try {
    mdxResult = await loadMdxContent('wiki', joined);
  } catch {
    // not found — fall through
  }

  if (mdxResult !== null) {
    return { kind: 'mdx', source: mdxResult.source, frontmatter: mdxResult.frontmatter };
  }

  // 2. Try glossary term
  const terms = await loadGlossaryTerms();
  const term = terms.find((t) => t.slug === joined);
  if (term) {
    return { kind: 'glossary', term };
  }

  return null;
}

// ---------------------------------------------------------------------------
// Metadata
// ---------------------------------------------------------------------------

export async function generateMetadata({
  params,
}: {
  params: Promise<{ slug: string[] }>;
}): Promise<Metadata> {
  const { slug } = await params;
  const joined = slug.join('/');
  const content = await resolveWikiContent(joined);

  if (content?.kind === 'mdx') {
    return { title: content.frontmatter.title, description: content.frontmatter.description };
  }
  if (content?.kind === 'glossary') {
    return { title: content.term.term, description: content.term.definition };
  }
  return {};
}

// ---------------------------------------------------------------------------
// Page
// ---------------------------------------------------------------------------

export default async function WikiPage({
  params,
}: {
  params: Promise<{ slug: string[] }>;
}) {
  const { slug } = await params;
  const joined = slug.join('/');
  const content = await resolveWikiContent(joined);

  if (content === null) {
    notFound();
  }

  const wikiCrumbs = [{ label: 'Home', href: '/' }, { label: 'Wiki', href: '/wiki' }]

  if (content.kind === 'glossary') {
    return <GlossaryShell term={content.term} crumbs={[...wikiCrumbs, { label: content.term.term }]} />;
  }

  // MDX path
  const { source, frontmatter } = content;
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
      <Breadcrumb crumbs={[...wikiCrumbs, { label: frontmatter.title }]} />
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
          {frontmatter.title}
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
          {frontmatter.description}
        </p>
      </header>
      <article
        style={{
          fontSize: tokens.fontSize.base[0],
          lineHeight: tokens.fontSize.base[1],
          color: tokens.colors['text-primary'],
        }}
      >
        {source}
      </article>
    </main>
  );
}
