import type { Metadata } from 'next';
import Link from 'next/link';
import { loadGlossaryTerms } from '@/lib/glossary/import';
import { listWikiSlugs } from '@/lib/wiki/slugs';
import { DataTable, type Column } from '@/components/DataTable';
import { WikiSearch } from '@/components/WikiSearch';
import { Breadcrumb } from '@/components/Breadcrumb';
import { Prose } from '@/components/type/Prose';

export const metadata: Metadata = {
  title: 'Wiki',
  description: 'Index of canonical glossary terms and hand-authored pages.',
};

// ---------------------------------------------------------------------------
// Row type
// ---------------------------------------------------------------------------

interface IndexRow {
  slug: string;
  title: string;
  category: string;
  source: 'mdx' | 'glossary';
}

// ---------------------------------------------------------------------------
// Columns
// ---------------------------------------------------------------------------

const COLUMNS: Column<IndexRow>[] = [
  { key: 'category', header: 'Category' },
  {
    key: 'title',
    header: 'Term',
    render: (r) => (
      <Link
        href={`/wiki/${r.slug}`}
        className="text-[var(--ds-text-primary)] underline"
      >
        {r.title}
      </Link>
    ),
  },
  {
    key: 'source',
    header: 'Source',
    render: (r) => (
      <span
        className="inline-block rounded-full font-mono text-[0.75rem] leading-4 text-[var(--ds-text-muted)] bg-[var(--ds-bg-panel)] [padding:2px_var(--ds-spacing-xs)]"
      >
        {r.source}
      </span>
    ),
  },
];

// ---------------------------------------------------------------------------
// Data loader
// ---------------------------------------------------------------------------

async function loadIndexRows(): Promise<IndexRow[]> {
  const [slugEntries, glossaryTerms] = await Promise.all([
    listWikiSlugs(),
    loadGlossaryTerms(),
  ]);

  const glossaryBySlug = new Map(glossaryTerms.map((t) => [t.slug, t]));

  return slugEntries.map(({ slug, source }) => {
    if (source === 'glossary') {
      const term = glossaryBySlug.get(slug);
      return {
        slug,
        title: term?.term ?? slug,
        category: term?.category ?? '',
        source: 'glossary' as const,
      };
    }
    // MDX: title derived from slug (capitalized); category filled at build if frontmatter loaded
    return {
      slug,
      title: slug.charAt(0).toUpperCase() + slug.slice(1),
      category: 'Wiki',
      source: 'mdx' as const,
    };
  });
}

// ---------------------------------------------------------------------------
// Page
// ---------------------------------------------------------------------------

export default async function WikiIndexPage() {
  const rows = await loadIndexRows();

  return (
    <main
      className="min-h-screen max-w-[800px] mx-auto bg-[var(--ds-bg-canvas)] text-[var(--ds-text-primary)] font-sans py-[var(--ds-spacing-xl)] px-[var(--ds-spacing-md)]"
    >
      <Breadcrumb crumbs={[{ label: 'Home', href: '/' }, { label: 'Wiki' }]} />
      <header
        className="mb-[var(--ds-spacing-xl)] border-b border-[var(--ds-bg-panel)] pb-[var(--ds-spacing-md)]"
      >
        <h1
          className="m-0 font-mono text-[1.5rem] leading-8 text-[var(--ds-text-primary)]"
        >
          Wiki
        </h1>
        <p
          className="m-0 mt-[var(--ds-spacing-xs)] text-[1rem] leading-6 text-[var(--ds-text-muted)]"
        >
          Index of canonical glossary terms and hand-authored pages.
        </p>
        <WikiSearch />
      </header>
      <Prose>
        <DataTable<IndexRow>
          columns={COLUMNS}
          rows={rows}
          getRowKey={(r) => r.slug}
        />
      </Prose>
    </main>
  );
}
