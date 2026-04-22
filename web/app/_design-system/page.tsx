import { notFound } from 'next/navigation';
import { BadgeChip, type Status } from '@/components/BadgeChip';
import { Surface } from '@/components/surface/Surface';
import { Heading, type HeadingLevel } from '@/components/type/Heading';
import { Prose } from '@/components/type/Prose';

if (process.env.NODE_ENV === 'production') {
  notFound();
}

const HEADING_LEVELS: HeadingLevel[] = [
  'display',
  'h1',
  'h2',
  'h3',
  'body-lg',
  'body',
  'body-sm',
  'caption',
  'mono-code',
  'mono-meta',
];

const STATUSES: Status[] = ['done', 'in-progress', 'pending', 'blocked'];
const TONES = ['raised', 'sunken', 'inset'] as const;
const PADDINGS = ['sm', 'md', 'lg', 'section'] as const;

const DS_VAR_ROWS: { name: string; value: string }[] = [
  { name: '--ds-font-size-display', value: '3.815rem' },
  { name: '--ds-spacing-md', value: '1rem' },
  { name: '--ds-surface-raised', value: 'panel' },
  { name: '--ds-duration-subtle', value: '120ms' },
];

export const metadata = {
  title: 'Design system — dev',
  robots: { index: false, follow: false },
};

export default function DesignSystemShowcasePage() {
  return (
    <div className="min-h-screen bg-bg-canvas p-8 text-text-primary">
      <div className="mx-auto max-w-5xl space-y-16">
        <header className="space-y-2">
          <h1 className="text-2xl font-semibold">Design system (development)</h1>
          <p className="text-text-muted">Primitive review — not linked in app navigation (NB2).</p>
        </header>

        <section className="space-y-4">
          <h2 className="text-sm font-mono uppercase tracking-widest text-text-muted">Heading</h2>
          <div className="space-y-3 border border-text-muted/20 p-4">
            {HEADING_LEVELS.map((level) => (
              <Heading key={level} level={level}>
                Level {level} sample
              </Heading>
            ))}
          </div>
        </section>

        <section className="space-y-4">
          <h2 className="text-sm font-mono uppercase tracking-widest text-text-muted">Prose</h2>
          <Prose>
            <p>First block — spacing to next comes from the Prose stack rule.</p>
            <p>Second block should sit below with medium vertical gap.</p>
            <p>Third block continues the rhythm.</p>
          </Prose>
        </section>

        <section className="space-y-4">
          <h2 className="text-sm font-mono uppercase tracking-widest text-text-muted">Surface (tone × padding)</h2>
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {TONES.map((tone) =>
              PADDINGS.map((padding) => (
                <Surface key={`${tone}-${padding}`} tone={tone} padding={padding} motion="none">
                  <span className="text-xs text-text-muted">
                    {tone} / {padding}
                  </span>
                </Surface>
              ))
            )}
          </div>
        </section>

        <section className="space-y-4">
          <h2 className="text-sm font-mono uppercase tracking-widest text-text-muted">Surface motion</h2>
          <div className="grid gap-4 md:grid-cols-3">
            <Surface tone="raised" padding="md" motion="subtle">
              <span>subtle</span>
            </Surface>
            <Surface tone="raised" padding="md" motion="gentle">
              <span>gentle</span>
            </Surface>
            <Surface tone="raised" padding="md" motion="deliberate">
              <span>deliberate</span>
            </Surface>
          </div>
        </section>

        <section className="space-y-4">
          <h2 className="text-sm font-mono uppercase tracking-widest text-text-muted">BadgeChip</h2>
          <div className="flex flex-wrap gap-2">
            {STATUSES.map((s) => (
              <BadgeChip key={s} status={s} />
            ))}
          </div>
        </section>

        <section className="space-y-4">
          <h2 className="text-sm font-mono uppercase tracking-widest text-text-muted">Design tokens (sample)</h2>
          <div className="overflow-x-auto rounded border border-text-muted/20">
            <table className="w-full text-left text-sm">
              <thead>
                <tr className="border-b border-text-muted/30">
                  <th className="p-2 font-mono">CSS variable</th>
                  <th className="p-2 font-mono">Reference</th>
                </tr>
              </thead>
              <tbody>
                {DS_VAR_ROWS.map((row) => (
                  <tr key={row.name} className="border-b border-text-muted/15">
                    <td className="p-2 font-mono text-xs text-text-muted">{row.name}</td>
                    <td className="p-2 text-text-muted">{row.value}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      </div>
    </div>
  );
}
