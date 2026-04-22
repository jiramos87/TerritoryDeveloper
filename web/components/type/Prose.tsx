import type { ReactNode } from 'react';

/**
 * Vertical rhythm for stacked block children.
 * @see `web/lib/design-system.md` section 5 (component map).
 */
export interface ProseProps {
  children: ReactNode;
  className?: string;
}

/**
 * RSC content stack — direct children get `mt` from adjacent sibling rule (design-system §5).
 */
export function Prose({ children, className }: ProseProps) {
  const base = '[&>*+*]:mt-[var(--ds-spacing-md)] text-[var(--ds-text-primary)]';
  const merged = className ? `${base} ${className}` : base;
  return <div className={merged}>{children}</div>;
}
