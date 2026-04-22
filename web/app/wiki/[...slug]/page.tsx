import type { Metadata } from 'next';
import { notFound } from 'next/navigation';
import { loadMdxContent } from '@/lib/mdx/loader';
import { loadGlossaryTerms } from '@/lib/glossary/import';
import { listWikiSlugs } from '@/lib/wiki/slugs';
import { GlossaryShell } from '@/components/GlossaryShell';
import { Breadcrumb } from '@/components/Breadcrumb';
import { Prose } from '@/components/type/Prose';
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
      className="min-h-screen max-w-[800px] mx-auto bg-[var(--ds-bg-canvas)] text-[var(--ds-text-primary)] font-sans py-[var(--ds-spacing-xl)] px-[var(--ds-spacing-md)]"
    >
      <Breadcrumb crumbs={[...wikiCrumbs, { label: frontmatter.title }]} />
      <header
        className="mb-[var(--ds-spacing-xl)] border-b border-[var(--ds-bg-panel)] pb-[var(--ds-spacing-md)]"
      >
        <h1
          className="m-0 font-mono text-[1.5rem] leading-8 text-[var(--ds-text-primary)]"
        >
          {frontmatter.title}
        </h1>
        <p
          className="m-0 mt-[var(--ds-spacing-xs)] text-[1rem] leading-6 text-[var(--ds-text-muted)]"
        >
          {frontmatter.description}
        </p>
      </header>
      <article>
        <Prose>{source}</Prose>
      </article>
    </main>
  );
}
