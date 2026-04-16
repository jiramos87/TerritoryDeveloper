'use client'

/**
 * PlanChartClient — thin 'use client' wrapper for PlanChart.
 *
 * `next/dynamic` with `ssr: false` must live inside a Client Component in
 * Next.js App Router. This wrapper exists solely to satisfy that constraint
 * so that `page.tsx` (RSC) can render PlanChart without SSR.
 *
 * Data shape matches PlanChartDatum from PlanChart.tsx.
 */

import dynamic from 'next/dynamic'
import type { PlanChartProps } from './PlanChart'

const PlanChart = dynamic(() => import('./PlanChart'), {
  ssr: false,
  loading: () => (
    <div className="h-[220px] bg-bg-panel animate-pulse rounded" />
  ),
})

export default function PlanChartClient(props: PlanChartProps) {
  return <PlanChart {...props} />
}
