import type { ReactNode } from 'react';

/**
 * LCD-style readout surface with optional scanline sweep.
 * Ported from CD `web/design-refs/step-8-console/src/console-primitives.jsx` (`Screen`).
 */
export type ScreenTone = 'dark' | 'readout';

const SCREEN_TONE: Record<ScreenTone, string> = {
  dark: 'text-[var(--ds-text-primary)]',
  readout: 'text-[var(--ds-accent-warm)]',
};

export type ScreenProps = {
  tone?: ScreenTone;
  /** Deeper inset shadow (CD inset bezel around glass). */
  inset?: boolean;
  /** Show animated sweep bar; respects `prefers-reduced-motion` via global CSS. */
  sweep?: boolean;
  className?: string;
  children?: ReactNode;
};

export function Screen({
  tone = 'dark',
  inset = false,
  sweep = true,
  className = '',
  children,
}: ScreenProps): ReactNode {
  const tc = SCREEN_TONE[tone];
  const insetCls = inset
    ? 'shadow-[inset_0_2px_8px_rgba(0,0,0,0.85)]'
    : '';
  return (
    <div
      className={`relative overflow-hidden rounded-[2px] bg-[var(--ds-surface-canvas)] p-[var(--ds-spacing-sm)] font-mono ${tc} ${insetCls} before:pointer-events-none before:absolute before:inset-0 before:bg-[repeating-linear-gradient(0deg,rgba(255,255,255,0.035)_0_1px,transparent_1px_3px)] before:mix-blend-screen after:pointer-events-none after:absolute after:inset-0 after:bg-[linear-gradient(155deg,rgba(255,255,255,0.05)_0%,transparent_40%)] ${className}`.trim()}
    >
      {sweep ? (
        <div className="ds-console-screen-sweep pointer-events-none absolute inset-0" aria-hidden />
      ) : null}
      <div className="relative z-[1]">{children}</div>
    </div>
  );
}
