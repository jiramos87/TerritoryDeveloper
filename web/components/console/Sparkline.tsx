import type { ReactNode } from 'react';

export type SparklineProps = {
  data: number[];
  color?: string;
  width?: number;
  height?: number;
  className?: string;
};

/**
 * Inline trend sparkline — from CD `console-assets.jsx` `Sparkline`.
 */
export function Sparkline({
  data,
  color = 'var(--ds-raw-amber)',
  width = 120,
  height = 32,
  className = '',
}: SparklineProps): ReactNode {
  if (data.length < 2) {
    return <span className="font-mono text-[var(--ds-text-meta)]" aria-hidden />;
  }
  const max = Math.max(...data, 1);
  const pts = data
    .map(
      (v, i) =>
        `${(i / (data.length - 1)) * width},${height - (v / max) * height}`,
    )
    .join(' ');
  return (
    <svg
      className={className}
      width={width}
      height={height}
      viewBox={`0 0 ${width} ${height}`}
      aria-hidden
    >
      <polyline points={pts} fill="none" stroke={color} strokeWidth="1.5" />
      {data.map((v, i) => (
        <circle
          key={i}
          cx={(i / (data.length - 1)) * width}
          cy={height - (v / max) * height}
          r="1"
          fill={color}
        />
      ))}
    </svg>
  );
}
