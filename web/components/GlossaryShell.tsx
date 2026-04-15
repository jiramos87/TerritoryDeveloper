import type { ReactNode } from 'react';
import type { GlossaryTerm } from '@/lib/glossary/types';
import { tokens } from '@/lib/tokens';

interface Props {
  term: GlossaryTerm;
}

/**
 * Renders a minimal wiki shell for a glossary-derived term.
 * Used by /wiki/[...slug] when no hand-authored MDX exists for the slug.
 */
export function GlossaryShell({ term }: Props): ReactNode {
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
          {term.term}
        </h1>
        <span
          style={{
            display: 'inline-block',
            marginTop: tokens.spacing[2],
            fontSize: tokens.fontSize.xs[0],
            fontFamily: tokens.fontFamily.mono.join(', '),
            color: tokens.colors['text-muted'],
            backgroundColor: tokens.colors['bg-panel'],
            borderRadius: '9999px',
            padding: `2px ${tokens.spacing[2]}`,
          }}
        >
          {term.category}
        </span>
      </header>
      <article
        style={{
          fontSize: tokens.fontSize.base[0],
          lineHeight: tokens.fontSize.base[1],
          color: tokens.colors['text-primary'],
        }}
      >
        <p style={{ margin: 0 }}>{term.definition}</p>
      </article>
    </main>
  );
}
