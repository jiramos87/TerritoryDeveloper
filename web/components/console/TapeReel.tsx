'use client';

import { useEffect, useState, type ReactNode } from 'react';

/**
 * Animated tape reel glyph. Spin respects **`prefers-reduced-motion`** via `matchMedia` in `useEffect`
 * plus global CSS (`ds-console-tape-reel`) — **NB-CD3** reduced-motion audit.
 *
 * Source: `web/design-refs/step-8-console/src/console-primitives.jsx` (`TapeReel`).
 */
export type TapeReelSize = 'sm' | 'md' | 'lg';

const SIZE_PX: Record<TapeReelSize, number> = {
  sm: 16,
  md: 22,
  lg: 28,
};

export type TapeReelProps = {
  spinning?: boolean;
  size?: TapeReelSize;
  className?: string;
};

export function TapeReel({
  spinning = false,
  size = 'md',
  className = '',
}: TapeReelProps): ReactNode {
  const [reduceMotion, setReduceMotion] = useState(false);

  useEffect(() => {
    const mq = window.matchMedia('(prefers-reduced-motion: reduce)');
    setReduceMotion(mq.matches);
    const onChange = (): void => setReduceMotion(mq.matches);
    mq.addEventListener('change', onChange);
    return () => mq.removeEventListener('change', onChange);
  }, []);

  const px = SIZE_PX[size];
  const spin = Boolean(spinning && !reduceMotion);

  return (
    <svg
      className={`ds-console-tape-reel text-[var(--ds-raw-amber)] ${className}`.trim()}
      width={px}
      height={px}
      viewBox="0 0 22 22"
      aria-hidden
      data-spinning={spin ? 'true' : 'false'}
    >
      <circle cx="11" cy="11" r="10" fill="var(--ds-raw-black)" stroke="#000" />
      <g stroke="currentColor" strokeWidth="1" opacity="0.9">
        {[0, 60, 120, 180, 240, 300].map((a) => (
          <line
            key={a}
            x1="11"
            y1="11"
            x2={11 + Math.cos((a * Math.PI) / 180) * 8}
            y2={11 + Math.sin((a * Math.PI) / 180) * 8}
          />
        ))}
      </g>
      <circle cx="11" cy="11" r="2.5" fill="#2a2a2a" stroke="#000" />
      <circle cx="11" cy="11" r="1" fill="currentColor" />
    </svg>
  );
}
