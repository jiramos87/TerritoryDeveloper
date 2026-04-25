import PlanChartClient from './PlanChartClient'
import PlanRingChartClient from './PlanRingChartClient'
import type { PlanChartDatum } from './PlanChart'

export interface PlanChartsProps {
  data: PlanChartDatum[]
}

/**
 * PlanCharts — paired master-plan visualizations.
 *
 * Left: per-stage stacked bar (task counts).
 * Right: equal-slice ring (stage completion %).
 * Centered horizontally; wraps on narrow viewports.
 */
export function PlanCharts({ data }: PlanChartsProps) {
  return (
    <div className="flex flex-wrap items-center justify-center gap-2">
      <div className="min-w-0 shrink-0">
        <PlanChartClient data={data} />
      </div>
      <div className="shrink-0">
        <PlanRingChartClient data={data} />
      </div>
    </div>
  )
}
