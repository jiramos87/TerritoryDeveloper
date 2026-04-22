'use client';

import type { ReactNode } from 'react';

/**
 * Segmented level meter (VU-style). Segment fill transitions use `--ds-duration-subtle`;
 * **`prefers-reduced-motion: reduce`** removes transitions (NB-CD3).
 *
 * Source: `web/design-refs/step-8-console/src/console-primitives.jsx` (`VuStrip`).
 */
export type VuStripProps = {
  level?: number;
  segments?: number;
  peak?: boolean;
  className?: string;
};

export function VuStrip({
  level = 0,
  segments = 24,
  peak = false,
  className = '',
}: VuStripProps): ReactNode {
  const clamped = Math.max(0, Math.min(1, level));
  let lit = Math.floor(clamped * segments);
  if (peak && lit < segments) {
    lit = Math.min(segments, lit + 1);
  }

  return (
    <div
      className={`flex h-4 max-w-[280px] flex-1 gap-0.5 rounded-sm bg-[#050505] p-0.5 shadow-[inset_0_1px_2px_rgba(0,0,0,0.9)] ${className}`.trim()}
      role="meter"
      aria-valuemin={0}
      aria-valuemax={100}
      aria-valuenow={Math.round(clamped * 100)}
      aria-label={`Signal level ${Math.round(clamped * 100)} percent`}
    >
      {Array.from({ length: segments }, (_, i) => {
        const tone = i < segments * 0.7 ? 'g' : i < segments * 0.9 ? 'a' : 'r';
        const on = i < lit;
        const segTone = on
          ? tone === 'g'
            ? 'bg-[var(--ds-raw-green)] shadow-[0_0_4px_var(--ds-raw-green)]'
            : tone === 'a'
              ? 'bg-[var(--ds-raw-amber)] shadow-[0_0_4px_var(--ds-raw-amber)]'
              : 'bg-[var(--ds-raw-red)] shadow-[0_0_4px_var(--ds-raw-red)]'
          : 'bg-[rgba(255,255,255,0.05)]';
        return (
          <div
            key={i}
            className={`min-w-0 flex-1 rounded-[1px] transition-[background-color,box-shadow] duration-[var(--ds-duration-subtle)] ease-out motion-reduce:transition-none ${segTone}`}
          />
        );
      })}
    </div>
  );
}
