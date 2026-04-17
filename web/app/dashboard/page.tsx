import { loadAllPlans } from '@/lib/plan-loader'
import type { PlanData, TaskRow, TaskStatus } from '@/lib/plan-loader-types'
import { BadgeChip } from '@/components/BadgeChip'
import { DataTable } from '@/components/DataTable'
import type { Column } from '@/components/DataTable'
import { FilterChips } from '@/components/FilterChips'
import type { Chip } from '@/components/FilterChips'
import { StatBar } from '@/components/StatBar'
import PlanChartClient from '@/components/PlanChartClient'
import { toBadgeStatus } from './_status'
import { parseFilterValues, toggleFilterParam } from '@/lib/dashboard/filter-params'
import { Button } from '@/components/Button'

/** Terminal task statuses counted as "done" for plan completion ratio. */
const DONE_STATUSES: ReadonlySet<TaskStatus> = new Set(['Done (archived)', 'Done'])
/** Task statuses counted as "pending" for chart aggregation. */
const PENDING_STATUSES: ReadonlySet<TaskStatus> = new Set(['_pending_', 'Draft'])
/** Task statuses counted as "in progress" for chart aggregation. */
const IN_PROGRESS_STATUSES: ReadonlySet<TaskStatus> = new Set(['In Progress', 'In Review'])

/** Derive a URL slug from a plan title (lowercase, hyphens). */
function toSlug(title: string): string {
  return title.toLowerCase().replace(/\s+/g, '-').replace(/[^a-z0-9-]/g, '')
}

type MultiParams = { plan: string[]; status: string[]; phase: string[] }

/**
 * Hierarchical prune: apply plan / status / phase filters to plan list.
 * OR within dimension, AND across dimensions.
 * Empty array per dimension = no filter for that dimension.
 * Drop stages with zero tasks post-filter; drop steps with zero stages;
 * drop plans with zero steps.
 */
function filterPlans(plans: PlanData[], params: MultiParams): PlanData[] {
  let result = plans

  // Plan filter (OR within dimension)
  if (params.plan.length > 0) {
    result = result.filter((p) => params.plan.includes(toSlug(p.title)))
  }

  // Task-level filters (AND across dimensions, OR within each)
  if (params.status.length > 0 || params.phase.length > 0) {
    result = result
      .map((plan) => {
        const steps = plan.steps
          .map((step) => {
            const stages = step.stages
              .map((stage) => {
                const tasks = stage.tasks.filter((t) => {
                  const statusOk = params.status.length === 0 || params.status.includes(t.status)
                  const phaseOk  = params.phase.length  === 0 || params.phase.includes(t.phase)
                  return statusOk && phaseOk
                })
                return { ...stage, tasks }
              })
              .filter((s) => s.tasks.length > 0)
            return { ...step, stages }
          })
          .filter((s) => s.stages.length > 0)
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

const TASK_COLUMNS: Column<TaskRow>[] = [
  { key: 'id',     header: 'ID' },
  { key: 'phase',  header: 'Phase' },
  { key: 'issue',  header: 'Issue' },
  {
    key: 'status',
    header: 'Status',
    render: (r) => <BadgeChip status={toBadgeStatus(r.status)} />,
  },
  { key: 'intent', header: 'Intent' },
]

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
      {/* Filter chip groups */}
      <div className="space-y-2">
        {planChips.length > 0 && (
          <div className="flex items-center gap-3 flex-wrap">
            <span className="text-xs text-text-muted font-mono w-14 shrink-0">Plan</span>
            <FilterChips chips={planChips} />
          </div>
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
          const totalCount = plan.allTasks.length
          const completedCount = plan.allTasks.filter((t) => DONE_STATUSES.has(t.status)).length
          const statBarLabel = `${completedCount} / ${totalCount} done`
          const chartData = plan.steps.map((step) => {
            const stepTasks = plan.allTasks.filter((t) =>
              t.id.startsWith('T' + step.id + '.')
            )
            return {
              label:      step.title,
              pending:    stepTasks.filter((t) => PENDING_STATUSES.has(t.status)).length,
              inProgress: stepTasks.filter((t) => IN_PROGRESS_STATUSES.has(t.status)).length,
              done:       stepTasks.filter((t) => DONE_STATUSES.has(t.status)).length,
            }
          })
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
              const stepTasks = plan.allTasks.filter((t) => t.id.startsWith('T' + step.id + '.'))
              const stepDone  = stepTasks.filter((t) => DONE_STATUSES.has(t.status)).length
              const stepTotal = stepTasks.length
              return (
              <div key={step.id} className="space-y-4 pl-2 border-l-2 border-border-subtle">
                {/* Step heading */}
                <div className="flex items-center gap-2 flex-wrap">
                  <h3 className="text-base font-semibold text-text-primary">
                    Step {step.id} — {step.title}
                  </h3>
                  <BadgeChip status={toBadgeStatus(step.status)} />
                  {step.statusDetail !== '' && (
                    <span className="text-text-muted text-sm">{step.statusDetail}</span>
                  )}
                  {stepTotal > 0 && (
                    <div className="flex-1 min-w-[10rem] max-w-[20rem]">
                      <StatBar label={`${stepDone} / ${stepTotal} done`} value={stepDone} max={stepTotal} />
                    </div>
                  )}
                </div>

                {step.stages.length === 0 ? (
                  <p className="text-text-muted text-sm pl-4">No stages.</p>
                ) : (
                  step.stages.map((stage) => (
                    <div key={stage.id} className="space-y-2 pl-4">
                      {/* Stage sub-heading */}
                      <div className="flex items-center gap-2 flex-wrap">
                        <h4 className="text-sm font-medium text-text-primary">
                          Stage {stage.id} — {stage.title}
                        </h4>
                        <BadgeChip status={toBadgeStatus(stage.status)} />
                        {stage.statusDetail !== '' && (
                          <span className="text-text-muted text-sm">{stage.statusDetail}</span>
                        )}
                      </div>

                      {stage.tasks.length === 0 ? (
                        <p className="text-text-muted text-sm pl-2">No tasks.</p>
                      ) : (
                        <DataTable<TaskRow>
                          columns={TASK_COLUMNS}
                          rows={stage.tasks}
                          getRowKey={(r) => r.id}
                        />
                      )}
                    </div>
                  ))
                )}
              </div>
            )})}
            {/* Per-plan grouped-bar chart — step breakdown by status */}
            <PlanChartClient data={chartData} />
          </section>
          )
        })
      )}
    </main>
  )
}
