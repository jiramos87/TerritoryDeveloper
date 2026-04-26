'use client'

import dynamic from 'next/dynamic'
import type { PlanRingChartProps } from './PlanRingChart'

const PlanRingChart = dynamic(() => import('./PlanRingChart'), {
  ssr: false,
  loading: () => (
    <div className="h-[232px] w-[232px] animate-pulse rounded-full bg-bg-panel" />
  ),
})

export default function PlanRingChartClient(props: PlanRingChartProps) {
  return <PlanRingChart {...props} />
}
