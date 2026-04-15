import type { Metadata } from 'next';
import History from '@/content/pages/history.mdx';
import { loadMdxPage } from '@/lib/mdx/loader';
import { tokens } from '@/lib/tokens';
import { DataTable } from '@/components/DataTable';
import { TIMELINE_ROWS } from '@/content/pages/history-timeline';
import type { TimelineRow } from '@/content/pages/history-timeline';
import type { Column } from '@/components/DataTable';
import { buildPageMetadata } from '@/lib/site/metadata';

export async function generateMetadata(): Promise<Metadata> {
  return buildPageMetadata('history');
}

const COLUMNS: Column<TimelineRow>[] = [
  { key: 'date', header: 'Date' },
  { key: 'milestone', header: 'Milestone' },
  { key: 'notes', header: 'Notes' },
];

export default async function HistoryPage() {
  const { frontmatter } = await loadMdxPage('history');

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
      <article>
        <History />
      </article>
      <section
        style={{
          marginTop: tokens.spacing[8],
        }}
      >
        <DataTable<TimelineRow>
          columns={COLUMNS}
          rows={TIMELINE_ROWS}
          getRowKey={(r) => r.date}
        />
      </section>
    </main>
  );
}
