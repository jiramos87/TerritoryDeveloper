import type { ReactNode } from 'react';

/**
 * Console rack chrome — structural wrapper with corner hardware and optional label.
 * Ported from CD `web/design-refs/step-8-console/src/console-primitives.jsx` (`Rack`).
 */
export type ConsoleChromeTone = 'default' | 'muted';
export type ConsoleChromePadding = 'sm' | 'md' | 'lg';

const RACK_PADDING: Record<ConsoleChromePadding, string> = {
  sm: 'pt-[22px] pb-[var(--ds-spacing-sm)] px-[var(--ds-spacing-sm)]',
  md: 'pt-[26px] pb-[var(--ds-spacing-md)] px-[var(--ds-spacing-md)]',
  lg: 'pt-8 pb-[var(--ds-spacing-lg)] px-[var(--ds-spacing-lg)]',
};

const RACK_TONE: Record<ConsoleChromeTone, string> = {
  default:
    'bg-[linear-gradient(180deg,#2a2a2a_0%,#1e1e1e_45%,#141414_100%)] [background-blend-mode:overlay] shadow-[inset_0_1px_0_rgba(255,255,255,0.06),inset_0_-1px_0_rgba(0,0,0,0.6),0_2px_6px_rgba(0,0,0,0.5)]',
  muted:
    'bg-[linear-gradient(180deg,#222222_0%,#181818_45%,#121212_100%)] shadow-[inset_0_1px_0_rgba(255,255,255,0.04),inset_0_-1px_0_rgba(0,0,0,0.55),0_1px_4px_rgba(0,0,0,0.45)]',
};

const SCREW =
  'pointer-events-none absolute size-2.5 shrink-0 rounded-full bg-[radial-gradient(circle_at_40%_35%,#555_0%,#2a2a2a_55%,#0a0a0a_100%)] shadow-[inset_0_0_0_1px_#000] before:absolute before:left-1/2 before:top-1/2 before:h-px before:w-1.5 before:-translate-x-1/2 before:-translate-y-1/2 before:bg-black before:content-[""]';

export type RackProps = {
  label?: string;
  tone?: ConsoleChromeTone;
  padding?: ConsoleChromePadding;
  className?: string;
  id?: string;
  children?: ReactNode;
};

export function Rack({
  label,
  tone = 'default',
  padding = 'md',
  className = '',
  id,
  children,
}: RackProps): ReactNode {
  const p = RACK_PADDING[padding];
  const t = RACK_TONE[tone];
  return (
    <div
      id={id}
      className={`relative rounded-[var(--ds-cd-radius-md)] border border-black ${t} ${p} ${className}`.trim()}
    >
      <span className={`${SCREW} left-1.5 top-1.5`} aria-hidden />
      <span className={`${SCREW} right-1.5 top-1.5`} aria-hidden />
      <span className={`${SCREW} bottom-1.5 left-1.5`} aria-hidden />
      <span className={`${SCREW} bottom-1.5 right-1.5`} aria-hidden />
      {label ? (
        <span className="pointer-events-none absolute left-1/2 top-1.5 -translate-x-1/2 whitespace-nowrap font-mono text-[9px] uppercase tracking-[0.2em] text-[var(--ds-text-muted)]">
          {label}
        </span>
      ) : null}
      {children}
    </div>
  );
}
