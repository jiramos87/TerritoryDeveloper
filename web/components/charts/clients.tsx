'use client';

/**
 * clients.tsx — dynamic SSR-disabled wrappers for D3 client islands.
 *
 * Detail-page chart components rely on `useEffect` + DOM measurement; rendering
 * them under SSR causes hydration mismatches. Each wrapper imports its chart
 * with `ssr: false` and a sized loading skeleton.
 */

import dynamic from 'next/dynamic';
import type { BurndownChartProps } from './BurndownChart';
import type { CycleHistogramProps } from './CycleHistogram';
import type { CommitCadenceChartProps } from './CommitCadenceChart';
import type { DepGraphProps } from './DepGraph';
import type { VelocityAreaChartProps } from './VelocityAreaChart';

const skeleton = (w: number, h: number) => {
  const Skeleton = () => (
    <div className="animate-pulse bg-bg-panel" style={{ width: w, height: h }} />
  );
  Skeleton.displayName = 'ChartLoadingSkeleton';
  return Skeleton;
};

const Burndown = dynamic(() => import('./BurndownChart'), {
  ssr: false,
  loading: skeleton(480, 220),
});
const Cycle = dynamic(() => import('./CycleHistogram'), {
  ssr: false,
  loading: skeleton(480, 200),
});
const Cadence = dynamic(() => import('./CommitCadenceChart'), {
  ssr: false,
  loading: skeleton(720, 220),
});
const Dep = dynamic(() => import('./DepGraph'), {
  ssr: false,
  loading: skeleton(720, 360),
});
const Velocity = dynamic(() => import('./VelocityAreaChart'), {
  ssr: false,
  loading: skeleton(480, 160),
});

export function BurndownChartClient(props: BurndownChartProps) {
  return <Burndown {...props} />;
}
export function CycleHistogramClient(props: CycleHistogramProps) {
  return <Cycle {...props} />;
}
export function CommitCadenceChartClient(props: CommitCadenceChartProps) {
  return <Cadence {...props} />;
}
export function DepGraphClient(props: DepGraphProps) {
  return <Dep {...props} />;
}
export function VelocityAreaChartClient(props: VelocityAreaChartProps) {
  return <Velocity {...props} />;
}
