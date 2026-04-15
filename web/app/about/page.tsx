import type { Metadata } from 'next';
import About from '@/content/pages/about.mdx';
import { loadMdxPage } from '@/lib/mdx/loader';
import { tokens } from '@/lib/tokens';
import { buildPageMetadata } from '@/lib/site/metadata';

export async function generateMetadata(): Promise<Metadata> {
  return buildPageMetadata('about');
}

export default async function AboutPage() {
  const { frontmatter } = await loadMdxPage('about');

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
        <About />
      </article>
    </main>
  );
}
