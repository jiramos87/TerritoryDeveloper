import { loadAllPlans } from '@/lib/plan-loader'
import { computePlanMetrics } from '@/lib/plan-parser'
import type { PlanData, TaskRow } from '@/lib/plan-loader-types'
import { BadgeChip } from '@/components/BadgeChip'
import { FilterChips } from '@/components/FilterChips'
import type { Chip } from '@/components/FilterChips'
import { CollapsibleFilterRow } from '@/components/CollapsibleFilterRow'
import { StatBar } from '@/components/StatBar'
import PlanChartClient from '@/components/PlanChartClient'
import { CollapsiblePlanStep } from '@/components/CollapsiblePlanStep'
import { toBadgeStatus } from './_status'
import { parseFilterValues, toggleFilterParam } from '@/lib/dashboard/filter-params'
import { Button } from '@/components/Button'
import { Breadcrumb } from '@/components/Breadcrumb'

/** Derive a URL slug from a plan title (lowercase, hyphens). */
function toSlug(title: string): string {
  return title.toLowerCase().replace(/\s+/g, '-').replace(/[^a-z0-9-]/g, '')
}

type MultiParams = { plan: string[]; status: string[]; phase: string[] }

/**
 * Hierarchical prune: apply plan / status / phase filters to plan list.
 * OR within dimension, AND across dimensions.
 * Empty array per dimension = no filter for that dimension.
 * Pending-decompose steps (no stages) and stages (no tasks) always pass through.
 */
function filterPlans(plans: PlanData[], params: MultiParams): PlanData[] {
  let result = plans

  // Plan filter (OR within dimension)
  if (params.plan.length > 0) {
    result = result.filter((p) => params.plan.includes(toSlug(p.title)))
  }

  // Task-level filters (AND across dimensions, OR within each).
  // flatMap: return [] to drop, [item] to keep — avoids ambiguity between
  // "originally no tasks" (pending-decompose) and "all tasks filtered out".
  if (params.status.length > 0 || params.phase.length > 0) {
    const matchesTask = (t: TaskRow) => {
      const statusOk = params.status.length === 0 || params.status.includes(t.status)
      const phaseOk  = params.phase.length  === 0 || params.phase.includes(t.phase)
      return statusOk && phaseOk
    }
    result = result
      .map((plan) => {
        const steps = plan.steps.flatMap((step) => {
          if (step.stages.length === 0) return [step]
          const stages = step.stages.flatMap((stage) => {
            if (stage.tasks.length === 0) return [stage]
            const tasks = stage.tasks.filter(matchesTask)
            return tasks.length > 0 ? [{ ...stage, tasks }] : []
          })
          return stages.length > 0 ? [{ ...step, stages }] : []
        })
        return { ...plan, steps }
      })
      .filter((p) => p.steps.length > 0)
  }

  return result
}

/**
 * DashboardPage — RSC. No "use client".
 *
 * Renders per-plan sections: title heading + overall-status badge +
 * step/stage hierarchy with per-stage DataTable.
 * Banner flags page as internal / non-public.
 */
export default async function DashboardPage({
  searchParams,
}: {
  searchParams: Promise<Record<string, string | string[] | undefined>>
}) {
  const rawParams = await searchParams

  // Build URLSearchParams from rawParams (handles string | string[] union)
  const search = new URLSearchParams()
  for (const [k, v] of Object.entries(rawParams)) {
    if (v == null) continue
    for (const val of Array.isArray(v) ? v : [v]) search.append(k, val)
  }

  const multi: MultiParams = {
    plan:   parseFilterValues(search, 'plan'),
    status: parseFilterValues(search, 'status'),
    phase:  parseFilterValues(search, 'phase'),
  }

  const currentSearch = search.toString()

  /** Build /dashboard?... href via toggleFilterParam; bare /dashboard when empty. */
  function chipHref(key: string, value: string): string {
    const qs = toggleFilterParam(currentSearch, key, value)
    return qs ? `/dashboard?${qs}` : '/dashboard'
  }

  const allPlans = await loadAllPlans()

  // Filter plans using multi-value params
  const plans = filterPlans(allPlans, multi)

  // Chip value sets from UNFILTERED allPlans
  const planSlugs   = Array.from(new Set(allPlans.map((p) => toSlug(p.title))))
  const statusValues = Array.from(
    new Set(allPlans.flatMap((p) => p.allTasks.map((t) => t.status)))
  )
  const phaseValues = Array.from(
    new Set(allPlans.flatMap((p) => p.allTasks.map((t) => t.phase)))
  ).sort((a, b) => {
    const na = parseInt(a, 10)
    const nb = parseInt(b, 10)
    if (!isNaN(na) && !isNaN(nb)) return na - nb
    return a.localeCompare(b)
  })

  const planChips: Chip[] = planSlugs.map((slug) => ({
    label:  slug,
    active: multi.plan.includes(slug),
    href:   chipHref('plan', slug),
  }))
  const statusChips: Chip[] = statusValues.map((s) => ({
    label:  s,
    active: multi.status.includes(s),
    href:   chipHref('status', s),
  }))
  const phaseChips: Chip[] = phaseValues.map((ph) => ({
    label:  `Phase ${ph}`,
    active: multi.phase.includes(ph),
    href:   chipHref('phase', ph),
  }))

  const anyFilter = multi.plan.length + multi.status.length + multi.phase.length > 0

  return (
    <main className="mx-auto max-w-5xl px-4 py-8 space-y-10">
      <Breadcrumb crumbs={[{ label: 'Home', href: '/' }, { label: 'Dashboard' }]} />
      {/* Filter chip groups */}
      <div className="space-y-2">
        {planChips.length > 0 && (
          <CollapsibleFilterRow label="Plan" activeCount={multi.plan.length}>
            <FilterChips chips={planChips} />
          </CollapsibleFilterRow>
        )}
        {statusChips.length > 0 && (
          <div className="flex items-center gap-3 flex-wrap">
            <span className="text-xs text-text-muted font-mono w-14 shrink-0">Status</span>
            <FilterChips chips={statusChips} />
          </div>
        )}
        {phaseChips.length > 0 && (
          <div className="flex items-center gap-3 flex-wrap">
            <span className="text-xs text-text-muted font-mono w-14 shrink-0">Phase</span>
            <FilterChips chips={phaseChips} />
          </div>
        )}
        {anyFilter && (
          <div className="pt-1">
            <Button variant="ghost" size="sm" href="/dashboard">Clear filters</Button>
          </div>
        )}
      </div>

      {plans.length === 0 ? (
        <p className="text-text-muted text-sm">No plans match the current filters.</p>
      ) : (
        plans.map((plan) => {
          const { completedCount, totalCount, statBarLabel, chartData, stepCounts } = computePlanMetrics(plan)
          return (
          <section key={plan.title} className="space-y-6">
            {/* Plan heading */}
            <div className="flex items-center gap-3">
              <h2 className="text-xl font-semibold text-text-primary">{plan.title}</h2>
              <BadgeChip status={toBadgeStatus(plan.overallStatus)} />
              <div className="flex-1 min-w-[12rem] max-w-[24rem]">
                <StatBar label={statBarLabel} value={completedCount} max={totalCount} />
              </div>
            </div>

            {/* Step / Stage hierarchy */}
            {plan.steps.map((step) => {
              const { done: stepDone, total: stepTotal } = stepCounts[step.id] ?? { done: 0, total: 0 }
              return (
                <CollapsiblePlanStep key={step.id} step={step} stepDone={stepDone} stepTotal={stepTotal} />
              )
            })}
            {/* Per-plan grouped-bar chart — step breakdown by status */}
            <PlanChartClient data={chartData} />
          </section>
          )
        })
      )}
    </main>
  )
}
