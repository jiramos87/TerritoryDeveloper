// Stage 27 T27.6 — CD ScreenDesign cross-check (augments T23.4 + T25.6; NB-CD4 de-dupe)
import { notFound } from 'next/navigation';
import { BadgeChip, type Status } from '@/components/BadgeChip';
import { ConsoleMediaShowcase } from '@/components/dev/ConsoleMediaShowcase';
import { ConsoleChromeShowcase } from '@/components/console/ConsoleChromeShowcase';
import { HeatCell, Rack } from '@/components/console';
import { Surface } from '@/components/surface/Surface';
import { Heading, type HeadingLevel } from '@/components/type/Heading';
import { Prose } from '@/components/type/Prose';

/**
 * Dev-only design-system showcase. Must live under a routable segment — Next.js App Router
 * treats folders prefixed with `_` as private (not part of the URL), so the former
 * `app/_design-system` path was unreachable.
 * Production: 404. Local: `/design-system`
 *
 * Guard is inside the component: top-level `notFound()` throws during `next build` (production).
 */
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

const RAW_SWATCH: { v: string; label: string }[] = [
  { v: 'var(--ds-raw-green)', label: 'raw green' },
  { v: 'var(--ds-raw-amber)', label: 'raw amber' },
  { v: 'var(--ds-raw-red)', label: 'raw red' },
  { v: 'var(--ds-raw-blue)', label: 'raw blue' },
  { v: 'var(--ds-raw-panel)', label: 'raw panel' },
  { v: 'var(--ds-raw-text)', label: 'raw text' },
];

export const metadata = {
  title: 'Design system — dev',
  robots: { index: false, follow: false },
};

export default function DesignSystemShowcasePage() {
  if (process.env.NODE_ENV === 'production') {
    notFound();
  }

  return (
    <div className="min-h-screen bg-bg-canvas p-8 text-text-primary">
      <div className="mx-auto max-w-5xl space-y-16">
        <header className="space-y-2">
          <h1 className="text-2xl font-semibold">Design system (development)</h1>
          <p className="text-text-muted">
            Local route: <code className="font-mono">/design-system</code> — 404 in production; not
            in sidebar (NB2).
          </p>
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
          <h2 className="text-sm font-mono uppercase tracking-widest text-text-muted">Console chrome</h2>
          <p className="text-sm text-text-muted">
            Stage 25 primitives — rack, bezel, screen, LEDs, tape reel, VU strip, transport (client
            island for interaction).
          </p>
          <ConsoleChromeShowcase />
        </section>

        <section className="space-y-4">
          <h2 className="text-sm font-mono uppercase tracking-widest text-text-muted">Media transport + TIcon</h2>
          <p className="text-sm text-text-muted">
            Stage 26 — 13-glyph <code className="font-mono">TIcon</code> matrix and four{' '}
            <code className="font-mono">MediaTransport</code> state demos (client).
          </p>
          <ConsoleMediaShowcase />
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

        <section className="space-y-4" id="cd-screen-design-t27">
          <h2 className="text-sm font-mono uppercase tracking-widest text-text-muted">
            CD design kit (Stage 27 — delta)
          </h2>
          <p className="text-sm text-text-muted">
            NB-CD4: Headings, Prose, Surface motion, and Console chrome above stay canonical; this
            block adds CD `ScreenDesign` matrix rows (palette + density) without forking the pilot
            bundle.
          </p>
          <Rack label="Palette swatches (raw)">
            <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
              {RAW_SWATCH.map((s) => (
                <div
                  key={s.label}
                  className="flex min-h-16 items-end rounded-sm border border-black p-2"
                  style={{ background: s.v }}
                >
                  <span className="font-mono text-[10px] uppercase tracking-wide text-white mix-blend-difference">
                    {s.label}
                  </span>
                </div>
              ))}
            </div>
          </Rack>
          <Rack label="HeatmapCell scale (null → peak)">
            <div className="flex flex-wrap items-center gap-1">
              {[0, 1, 2, 4, 6, 8].map((n) => (
                <HeatCell key={n} n={n} />
              ))}
              <span className="ml-3 font-mono text-[10px] text-text-muted">CD density buckets</span>
            </div>
          </Rack>
          <Rack label="Motion stops (CD parity)">
            <p className="mb-3 text-sm text-text-muted">
              Durations align with <code className="font-mono">--ds-duration-*</code> — see Surface
              motion section for live demos; reduced-motion: coerces to 0 in globals.
            </p>
            <ul className="list-inside list-disc space-y-1 font-mono text-sm text-text-muted">
              <li>instant / subtle / gentle / deliberate</li>
            </ul>
          </Rack>
        </section>
      </div>
    </div>
  );
}
