import PlanChartClient from './PlanChartClient'
import PlanRingChartClient from './PlanRingChartClient'
import type { PlanChartDatum } from './PlanChart'

export interface ReleaseChartsProps {
  data: PlanChartDatum[]
}

/**
 * ReleaseCharts — release-scope progress visualizations.
 *
 * Left bar: per-master-plan stage status counts (stacked done / in-progress / pending).
 * Right ring: equal-slice per master plan, colored by overall plan status.
 *
 * Centered horizontally; wraps on narrow viewports.
 */
export function ReleaseCharts({ data }: ReleaseChartsProps) {
  return (
    <div className="flex flex-wrap items-center justify-center gap-2">
      <div className="min-w-0 shrink-0">
        <PlanChartClient data={data} />
      </div>
      <div className="shrink-0">
        <PlanRingChartClient data={data} unitLabel="PLANS" />
      </div>
    </div>
  )
}
