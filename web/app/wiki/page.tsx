import type { Metadata } from 'next';
import Link from 'next/link';
import { loadGlossaryTerms } from '@/lib/glossary/import';
import { listWikiSlugs } from '@/lib/wiki/slugs';
import { DataTable, type Column } from '@/components/DataTable';
import { tokens } from '@/lib/tokens';

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
        style={{ color: tokens.colors['text-primary'], textDecoration: 'underline' }}
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
        style={{
          display: 'inline-block',
          fontSize: tokens.fontSize.xs[0],
          fontFamily: tokens.fontFamily.mono.join(', '),
          color: tokens.colors['text-muted'],
          backgroundColor: tokens.colors['bg-panel'],
          borderRadius: '9999px',
          padding: `2px ${tokens.spacing[2]}`,
        }}
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
          Wiki
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
          Index of canonical glossary terms and hand-authored pages.
        </p>
      </header>
      <section>
        <DataTable<IndexRow>
          columns={COLUMNS}
          rows={rows}
          getRowKey={(r) => r.slug}
        />
      </section>
    </main>
  );
}
