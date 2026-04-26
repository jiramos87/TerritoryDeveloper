import Link from 'next/link'
import PlanChartClient from './PlanChartClient'
import PlanRingChartClient from './PlanRingChartClient'
import type { PlanChartDatum } from './PlanChart'

export interface PlanChartsProps {
  data: PlanChartDatum[]
  /** Optional href — when set, the whole chart pair becomes a link to the plan detail view. */
  detailHref?: string
}

/**
 * PlanCharts — paired master-plan visualizations.
 *
 * Left: per-stage stacked bar (task counts).
 * Right: equal-slice ring (stage completion %).
 * Centered horizontally; wraps on narrow viewports.
 * When `detailHref` is set, wraps the pair in a link to the plan detail page.
 */
export function PlanCharts({ data, detailHref }: PlanChartsProps) {
  const inner = (
    <div className="flex flex-wrap items-center justify-center gap-2">
      <div className="min-w-0 shrink-0">
        <PlanChartClient data={data} />
      </div>
      <div className="shrink-0">
        <PlanRingChartClient data={data} />
      </div>
    </div>
  )
  if (!detailHref) return inner
  return (
    <Link
      href={detailHref}
      className="block rounded transition hover:bg-[var(--ds-bg-panel)]/40"
      aria-label="Open plan detail"
    >
      {inner}
    </Link>
  )
}
