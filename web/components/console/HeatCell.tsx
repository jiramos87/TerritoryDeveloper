import type { ReactNode } from 'react';

/** CD `densityBucket` — maps task count 0..8 to visual bucket. */
export function heatDensityBucket(
  n: number,
): 'h-null' | 'h-low' | 'h-mid' | 'h-high' | 'h-peak' {
  if (n === 0) return 'h-null';
  if (n <= 1) return 'h-low';
  if (n <= 3) return 'h-mid';
  if (n <= 5) return 'h-high';
  return 'h-peak';
}

const BUCKET: Record<ReturnType<typeof heatDensityBucket>, string> = {
  'h-null': 'bg-[rgba(232,232,232,0.05)]',
  'h-low': 'bg-[rgba(58,155,74,0.35)] shadow-[0_0_2px_rgba(58,155,74,0.25)]',
  'h-mid': 'bg-[var(--ds-raw-green)] shadow-[0_0_3px_rgba(58,155,74,0.5)]',
  'h-high': 'bg-[#8fb54a] shadow-[0_0_3px_rgba(232,163,61,0.45)]',
  'h-peak': 'bg-[var(--ds-raw-amber)] shadow-[0_0_4px_rgba(232,163,61,0.6)]',
};

export type HeatCellProps = {
  n: number;
  label?: string;
  className?: string;
};

/**
 * One density cell for 7×12 heatmaps — parity with CD `console-primitives.jsx` `HeatCell`.
 */
export function HeatCell({ n, label, className = '' }: HeatCellProps): ReactNode {
  const b = heatDensityBucket(n);
  return (
    <div
      className={`aspect-square min-w-[12px] cursor-pointer rounded-[1px] transition-transform duration-[var(--ds-duration-subtle)] hover:z-[1] hover:scale-125 hover:outline hover:outline-1 hover:outline-[var(--ds-raw-blue)] ${BUCKET[b]} ${className}`.trim()}
      title={label ?? `${n} tasks`}
      aria-label={label}
    />
  );
}
