import type { ReactNode } from 'react';

import type { ConsoleChromePadding, ConsoleChromeTone } from './Rack';

/**
 * Recessed bezel around console readouts.
 * Ported from CD `web/design-refs/step-8-console/src/console-primitives.jsx` (`Bezel`).
 */

const BEZEL_PADDING: Record<ConsoleChromePadding, string> = {
  sm: 'p-[var(--ds-spacing-sm)]',
  md: 'p-[var(--ds-spacing-md)]',
  lg: 'p-[var(--ds-spacing-lg)]',
};

const BEZEL_TONE: Record<ConsoleChromeTone, string> = {
  default: 'bg-[var(--ds-surface-inset)]',
  muted: 'bg-[#0a0a0a]',
};

export type BezelProps = {
  tone?: ConsoleChromeTone;
  padding?: ConsoleChromePadding;
  /** Tighter inset padding (CD `thin`). */
  thin?: boolean;
  className?: string;
  children?: ReactNode;
};

export function Bezel({
  tone = 'default',
  padding = 'md',
  thin = false,
  className = '',
  children,
}: BezelProps): ReactNode {
  const p = thin ? 'p-[var(--ds-spacing-xs)]' : BEZEL_PADDING[padding];
  const t = BEZEL_TONE[tone];
  return (
    <div
      className={`rounded-[var(--ds-cd-radius-sm)] border border-black shadow-[inset_0_2px_4px_rgba(0,0,0,0.9),inset_0_-1px_0_rgba(255,255,255,0.04)] ${t} ${p} ${className}`.trim()}
    >
      {children}
    </div>
  );
}
