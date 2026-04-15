import { loadAllPlans } from '@/lib/plan-loader'
import type { PlanData, TaskRow } from '@/lib/plan-loader-types'
import { BadgeChip } from '@/components/BadgeChip'
import { DataTable } from '@/components/DataTable'
import type { Column } from '@/components/DataTable'
import { FilterChips } from '@/components/FilterChips'
import type { Chip } from '@/components/FilterChips'
import { toBadgeStatus } from './_status'

/** Coerce Next multi-value param to a single string or undefined. */
function firstParam(v: string | string[] | undefined): string | undefined {
  if (v == null) return undefined
  const s = Array.isArray(v) ? v[0] : v
  return s === '' ? undefined : s
}

/** Derive a URL slug from a plan title (lowercase, hyphens). */
function toSlug(title: string): string {
  return title.toLowerCase().replace(/\s+/g, '-').replace(/[^a-z0-9-]/g, '')
}

/** Build a /dashboard?... href preserving sibling params; toggle-off when value matches current. */
function buildHref(
  current: Record<string, string | undefined>,
  key: string,
  value: string
): string {
  const next: Record<string, string> = {}
  for (const [k, v] of Object.entries(current)) {
    if (v != null && k !== key) next[k] = v
  }
  // toggle-off: clicking the active chip clears that filter
  if (current[key] !== value) next[key] = value
  const qs = new URLSearchParams(next).toString()
  return qs ? `/dashboard?${qs}` : '/dashboard'
}

/**
 * Hierarchical prune: apply status + phase filters to task rows.
 * Drop stages with zero tasks post-filter; drop steps with zero stages;
 * drop plans with zero steps.
 */
function filterPlans(
  plans: PlanData[],
  params: Record<string, string | undefined>
): PlanData[] {
  let result = plans

  // Plan filter
  if (params.plan) {
    const slug = params.plan
    result = result.filter((p) => toSlug(p.title) === slug)
  }

  // Task-level filters (status + phase) — prune empty hierarchy
  if (params.status || params.phase) {
    result = result
      .map((plan) => {
        const steps = plan.steps
          .map((step) => {
            const stages = step.stages
              .map((stage) => {
                const tasks = stage.tasks.filter((t) => {
                  const statusOk = params.status ? t.status === params.status : true
                  const phaseOk  = params.phase  ? t.phase  === params.phase  : true
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
  const params: Record<string, string | undefined> = {
    plan:   firstParam(rawParams['plan']),
    status: firstParam(rawParams['status']),
    phase:  firstParam(rawParams['phase']),
  }

  const allPlans = await loadAllPlans()

  // ---- Phase 3: filter plans ----
  const plans = filterPlans(allPlans, params)

  // ---- Phase 4: chip value sets from UNFILTERED allPlans ----
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
    active: params.plan === slug,
    href:   buildHref(params, 'plan', slug),
  }))
  const statusChips: Chip[] = statusValues.map((s) => ({
    label:  s,
    active: params.status === s,
    href:   buildHref(params, 'status', s),
  }))
  const phaseChips: Chip[] = phaseValues.map((ph) => ({
    label:  `Phase ${ph}`,
    active: params.phase === ph,
    href:   buildHref(params, 'phase', ph),
  }))

  const anyFilter = params.plan != null || params.status != null || params.phase != null

  return (
    <main className="mx-auto max-w-5xl px-4 py-8 space-y-10">
      <p className="rounded border border-amber-400 bg-amber-50 px-4 py-3 text-sm text-amber-900">
        This page is internal and non-public. It is not linked from the site navigation or sitemap.
        It tracks development plan progress for the Territory Developer project.
      </p>

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
            <a href="/dashboard" className="text-xs text-text-muted underline">
              Clear filters
            </a>
          </div>
        )}
      </div>

      {plans.length === 0 ? (
        <p className="text-text-muted text-sm">No plans match the current filters.</p>
      ) : (
        plans.map((plan) => (
          <section key={plan.title} className="space-y-6">
            {/* Plan heading */}
            <div className="flex items-center gap-3">
              <h2 className="text-xl font-semibold text-text-primary">{plan.title}</h2>
              <BadgeChip status={toBadgeStatus(plan.overallStatus)} />
            </div>

            {/* Step / Stage hierarchy */}
            {plan.steps.map((step) => (
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
            ))}
          </section>
        ))
      )}
    </main>
  )
}
