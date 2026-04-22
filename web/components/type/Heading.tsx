import type { ElementType, ReactNode } from 'react';

/**
 * Type primitive — see `web/lib/design-system.md` §5 (component map).
 */
export type HeadingLevel =
  | 'display'
  | 'h1'
  | 'h2'
  | 'h3'
  | 'body-lg'
  | 'body'
  | 'body-sm'
  | 'caption'
  | 'mono-code'
  | 'mono-meta';

const levelToTag: Record<HeadingLevel, ElementType> = {
  display: 'h1',
  h1: 'h1',
  h2: 'h2',
  h3: 'h3',
  'body-lg': 'p',
  body: 'p',
  'body-sm': 'p',
  caption: 'span',
  'mono-code': 'span',
  'mono-meta': 'span',
};

const levelToTextClass: Record<HeadingLevel, string> = {
  display: 'text-[var(--ds-font-size-display)]',
  h1: 'text-[var(--ds-font-size-h1)]',
  h2: 'text-[var(--ds-font-size-h2)]',
  h3: 'text-[var(--ds-font-size-h3)]',
  'body-lg': 'text-[var(--ds-font-size-body-lg)]',
  body: 'text-[var(--ds-font-size-body)]',
  'body-sm': 'text-[var(--ds-font-size-body-sm)]',
  caption: 'text-[var(--ds-font-size-caption)]',
  'mono-code': 'font-mono text-[var(--ds-font-size-mono-code)]',
  'mono-meta': 'font-mono text-[var(--ds-font-size-mono-meta)]',
};

export interface HeadingProps {
  level: HeadingLevel;
  children: ReactNode;
  /** Optional Tailwind / utility class override (e.g. font weight). */
  weight?: string;
  className?: string;
}

/**
 * RSC heading / type line — `level` maps to a semantic HTML tag and the matching
 * `--ds-font-size-…` token from `levelToTextClass` (do not put glob/wildcard syntax in
 * JSDoc backticks: Tailwind content scan treats them as class candidates).
 * @see `web/lib/design-system.md` section 5 (component map).
 * Spec: `ia/projects/web-platform-master-plan.md` Stage 23 T23.1.
 */
export function Heading({ level, children, weight, className }: HeadingProps) {
  const Tag = levelToTag[level];
  const textClass = levelToTextClass[level];
  const merged = [textClass, weight, 'text-text-primary', className].filter(Boolean).join(' ');
  return <Tag className={merged}>{children}</Tag>;
}
