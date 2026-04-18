import type { Metadata } from 'next';
import Install from '@/content/pages/install.mdx';
import { loadMdxPage } from '@/lib/mdx/loader';
import { tokens } from '@/lib/tokens';
import { BadgeChip } from '@/components/BadgeChip';
import type { Status } from '@/components/BadgeChip';
import { buildPageMetadata } from '@/lib/site/metadata';
import { Breadcrumb } from '@/components/Breadcrumb';

export async function generateMetadata(): Promise<Metadata> {
  return buildPageMetadata('install');
}

type PlatformRow = {
  platform: string;
  status: Status;
};

const PLATFORMS: PlatformRow[] = [
  { platform: 'Windows', status: 'pending' },
  { platform: 'macOS', status: 'pending' },
  { platform: 'Linux', status: 'pending' },
  { platform: 'Web (browser)', status: 'pending' },
];

export default async function InstallPage() {
  const { frontmatter } = await loadMdxPage('install');

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
      <Breadcrumb crumbs={[{ label: 'Home', href: '/' }, { label: 'Install' }]} />
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
        <Install />
      </article>
      <section
        style={{
          marginTop: tokens.spacing[8],
        }}
      >
        <table
          style={{
            width: '100%',
            borderCollapse: 'collapse',
            fontSize: tokens.fontSize.sm[0],
            fontFamily: tokens.fontFamily.mono.join(', '),
          }}
        >
          <thead>
            <tr
              style={{
                borderBottom: `1px solid ${tokens.colors['bg-panel']}`,
              }}
            >
              <th
                style={{
                  textAlign: 'left',
                  padding: `${tokens.spacing[2]} ${tokens.spacing[3]}`,
                  color: tokens.colors['text-muted'],
                  fontWeight: 600,
                  textTransform: 'uppercase',
                  letterSpacing: '0.05em',
                  fontSize: tokens.fontSize.xs[0],
                }}
              >
                Platform
              </th>
              <th
                style={{
                  textAlign: 'left',
                  padding: `${tokens.spacing[2]} ${tokens.spacing[3]}`,
                  color: tokens.colors['text-muted'],
                  fontWeight: 600,
                  textTransform: 'uppercase',
                  letterSpacing: '0.05em',
                  fontSize: tokens.fontSize.xs[0],
                }}
              >
                Status
              </th>
            </tr>
          </thead>
          <tbody>
            {PLATFORMS.map((row) => (
              <tr
                key={row.platform}
                style={{
                  borderBottom: `1px solid ${tokens.colors['bg-panel']}`,
                }}
              >
                <td
                  style={{
                    padding: `${tokens.spacing[2]} ${tokens.spacing[3]}`,
                    color: tokens.colors['text-primary'],
                  }}
                >
                  {row.platform}
                </td>
                <td
                  style={{
                    padding: `${tokens.spacing[2]} ${tokens.spacing[3]}`,
                  }}
                >
                  <BadgeChip status={row.status} />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>
    </main>
  );
}
