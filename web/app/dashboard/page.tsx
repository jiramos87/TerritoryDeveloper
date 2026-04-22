// Stage 27 T27.3 — CD ScreenDashboard port — preserve plan loaders + charts.
import Link from 'next/link';
import { loadAllPlans } from '@/lib/plan-loader';
import { computePlanMetrics } from '@/lib/plan-parser';
import type { PlanData, TaskRow } from '@/lib/plan-loader-types';
import { BadgeChip } from '@/components/BadgeChip';
import { FilterChips } from '@/components/FilterChips';
import type { Chip } from '@/components/FilterChips';
import { CollapsibleFilterRow } from '@/components/CollapsibleFilterRow';
import { StatBar } from '@/components/StatBar';
import PlanChartClient from '@/components/PlanChartClient';
import { CollapsiblePlanStage } from '@/components/CollapsiblePlanStage';
import { toBadgeStatus } from './_status';
import { parseFilterValues, toggleFilterParam } from '@/lib/dashboard/filter-params';
import { Button } from '@/components/Button';
import { Heading } from '@/components/type/Heading';
import {
  Bezel,
  HeatCell,
  Rack,
  Screen,
  Sparkline,
} from '@/components/console';
import { CD_WEEK_DENSITY } from '@/lib/cd-week-density';

/** Derive a URL slug from a plan title (lowercase, hyphens). */
function toSlug(title: string): string {
  return title.toLowerCase().replace(/\s+/g, '-').replace(/[^a-z0-9-]/g, '');
}

type MultiParams = { plan: string[]; status: string[] };

/**
 * Hierarchical prune: apply plan / status filters to plan list.
 * OR within dimension, AND across dimensions.
 * Empty array per dimension = no filter for that dimension.
 * Pending-decompose stages (no tasks) always pass through.
 */
function filterPlans(plans: PlanData[], params: MultiParams): PlanData[] {
  let result = plans;

  if (params.plan.length > 0) {
    result = result.filter((p) => params.plan.includes(toSlug(p.title)));
  }

  if (params.status.length > 0) {
    const matchesTask = (t: TaskRow) => params.status.includes(t.status);
    result = result
      .map((plan) => {
        const stages = plan.stages.flatMap((stage) => {
          if (stage.tasks.length === 0) return [stage];
          const tasks = stage.tasks.filter(matchesTask);
          return tasks.length > 0 ? [{ ...stage, tasks }] : [];
        });
        return { ...plan, stages };
      })
      .filter((p) => p.stages.length > 0);
  }

  return result;
}

/** Map loader task status → CD console aggregate buckets. */
function aggregateTaskCounts(tasks: TaskRow[]): {
  done: number;
  progress: number;
  pending: number;
  blocked: number;
  total: number;
} {
  let done = 0;
  let progress = 0;
  let pending = 0;
  let blocked = 0;
  for (const t of tasks) {
    const s = t.status;
    if (s === 'Done' || s === 'Done (archived)') {
      done++;
    } else if (s === 'In Progress' || s === 'In Review') {
      progress++;
    } else if (String(s).toLowerCase().includes('block')) {
      blocked++;
    } else {
      pending++;
    }
  }
  return { done, progress, pending, blocked, total: tasks.length };
}

const SPARK_DATA = [2, 3, 5, 4, 6, 8, 7, 9, 11, 8, 10, 12];

/**
 * DashboardPage — RSC. No "use client".
 */
export default async function DashboardPage({
  searchParams,
}: {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}) {
  const rawParams = await searchParams;

  const search = new URLSearchParams();
  for (const [k, v] of Object.entries(rawParams)) {
    if (v == null) continue;
    for (const val of Array.isArray(v) ? v : [v]) search.append(k, val);
  }

  const multi: MultiParams = {
    plan: parseFilterValues(search, 'plan'),
    status: parseFilterValues(search, 'status'),
  };

  const currentSearch = search.toString();

  function chipHref(key: string, value: string): string {
    const qs = toggleFilterParam(currentSearch, key, value);
    return qs ? `/dashboard?${qs}` : '/dashboard';
  }

  const allPlans = await loadAllPlans();
  const plans = filterPlans(allPlans, multi);

  const planOptions = Array.from(
    new Map(allPlans.map((p) => [toSlug(p.title), p.title])).entries(),
  ).map(([slug, title]) => ({ slug, title }));
  const statusValues = Array.from(
    new Set(allPlans.flatMap((p) => p.allTasks.map((t) => t.status))),
  );

  const planChips: Chip[] = planOptions.map(({ slug, title }) => ({
    label: title,
    active: multi.plan.includes(slug),
    href: chipHref('plan', slug),
  }));
  const statusChips: Chip[] = statusValues.map((s) => ({
    label: s,
    active: multi.status.includes(s),
    href: chipHref('status', s),
  }));

  const anyFilter = multi.plan.length + multi.status.length > 0;

  const allTasks = allPlans.flatMap((p) => p.allTasks);
  const counts = aggregateTaskCounts(allTasks);

  return (
    <main className="mx-auto max-w-5xl space-y-6 px-4 py-8">
      <div className="mb-3 flex flex-wrap gap-2 font-mono text-[11px] uppercase tracking-wide text-[var(--ds-text-meta)]">
        <Link href="/" className="text-[var(--ds-text-meta)]">
          Territory
        </Link>
        <span className="opacity-50">{'//'}</span>
        <span className="text-[var(--ds-raw-blue)]">Dashboard</span>
      </div>

      <div
        className="mb-4 grid items-stretch gap-4"
        style={{ gridTemplateColumns: 'repeat(auto-fit, minmax(120px, 1fr))' }}
      >
        <Rack className="min-w-0" label="Done">
          <Bezel>
            <Screen tone="dark" sweep={false} className="lcd p-2 text-[var(--ds-raw-green)]">
              <div className="lcd-label font-mono text-[9px] uppercase tracking-[0.25em] opacity-60">
                Completed
              </div>
              <div className="font-mono text-[26px] font-bold leading-none">
                {String(counts.done).padStart(3, '0')}
              </div>
            </Screen>
          </Bezel>
        </Rack>
        <Rack className="min-w-0" label="Prog">
          <Bezel>
            <Screen tone="readout" sweep={false} className="lcd p-2">
              <div className="lcd-label font-mono text-[9px] uppercase tracking-[0.25em] opacity-60">
                In progress
              </div>
              <div className="font-mono text-[26px] font-bold leading-none text-[var(--ds-raw-amber)]">
                {String(counts.progress).padStart(3, '0')}
              </div>
            </Screen>
          </Bezel>
        </Rack>
        <Rack className="min-w-0" label="Pend">
          <Bezel>
            <Screen tone="dark" sweep={false} className="lcd p-2 text-[var(--ds-text-meta)]">
              <div className="lcd-label font-mono text-[9px] uppercase tracking-[0.25em] opacity-60">
                Pending
              </div>
              <div className="font-mono text-[26px] font-bold leading-none">
                {String(counts.pending).padStart(3, '0')}
              </div>
            </Screen>
          </Bezel>
        </Rack>
        <Rack className="min-w-0" label="Blkd">
          <Bezel>
            <Screen tone="dark" sweep={false} className="lcd p-2 text-[var(--ds-raw-red)]">
              <div className="lcd-label font-mono text-[9px] uppercase tracking-[0.25em] opacity-60">
                Blocked
              </div>
              <div className="font-mono text-[26px] font-bold leading-none">
                {String(counts.blocked).padStart(3, '0')}
              </div>
            </Screen>
          </Bezel>
        </Rack>
        <Rack className="min-w-[200px] md:col-span-2" label="Velocity">
          <div className="px-2.5 py-1.5">
            <div className="mb-0.5 flex justify-between">
              <span className="lcd-label font-mono text-[9px] uppercase tracking-[0.25em] text-[var(--ds-text-meta)]">
                Tasks/week
              </span>
              <span className="font-mono text-sm text-[var(--ds-raw-amber)]">+6.2</span>
            </div>
            <Sparkline data={SPARK_DATA} width={220} height={40} />
          </div>
        </Rack>
      </div>

      <Rack className="mb-2" label="Density // 7 stages × 12 weeks">
        <Bezel>
          <div className="px-1 py-1.5">
            {CD_WEEK_DENSITY.map((row) => (
              <div
                key={row.stage}
                className="mb-0.5 grid items-center gap-1 [grid-template-columns:minmax(100px,220px)_repeat(12,minmax(0,1fr))] min-[600px]:[grid-template-columns:220px_repeat(12,minmax(0,1fr))]"
              >
                <span
                  className="overflow-hidden text-ellipsis whitespace-nowrap font-mono text-[10px] tracking-wide text-[var(--ds-text-meta)]"
                  style={{ letterSpacing: '0.05em' }}
                >
                  {row.stage}
                </span>
                {row.cells.map((n, i) => (
                  <HeatCell
                    key={i}
                    n={n}
                    label={`${row.stage} · wk ${i + 1} · ${n} tasks`}
                  />
                ))}
              </div>
            ))}
            <div
              className="mt-1.5 grid items-center gap-1 [grid-template-columns:minmax(100px,220px)_repeat(12,minmax(0,1fr))] min-[600px]:[grid-template-columns:220px_repeat(12,minmax(0,1fr))]"
            >
              <span />
              {Array.from({ length: 12 }, (_, i) => (
                <span
                  key={i}
                  className="text-center font-mono text-[9px] tracking-wide text-[var(--ds-text-meta)]"
                  style={{ letterSpacing: '0.05em' }}
                >
                  W{String(i + 1).padStart(2, '0')}
                </span>
              ))}
            </div>
          </div>
        </Bezel>
      </Rack>

      <div className="mb-2 space-y-2">
        {planChips.length > 0 && (
          <CollapsibleFilterRow label="Plan" activeCount={multi.plan.length}>
            <FilterChips chips={planChips} />
          </CollapsibleFilterRow>
        )}
        {statusChips.length > 0 && (
          <div className="flex flex-wrap items-center gap-3">
            <span className="w-14 shrink-0 font-mono text-xs text-text-muted">Status</span>
            <FilterChips chips={statusChips} />
          </div>
        )}
        {anyFilter && (
          <div className="pt-1">
            <Button variant="ghost" size="sm" href="/dashboard">
              Clear filters
            </Button>
          </div>
        )}
      </div>

      {plans.length === 0 ? (
        <p className="text-sm text-text-muted">No plans match the current filters.</p>
      ) : (
        plans.map((plan) => {
          const unfilteredPlan = allPlans.find((p) => p.title === plan.title) ?? plan;
          const { completedCount, totalCount, statBarLabel, chartData, stageCounts } =
            computePlanMetrics(unfilteredPlan);
          return (
            <Rack key={plan.title} className="space-y-4" label="Master plan">
              <div className="flex flex-wrap items-center gap-3">
                <Heading level="h2" className="min-w-0">
                  {plan.title}
                </Heading>
                <BadgeChip status={toBadgeStatus(plan.overallStatus)} />
                <div className="min-w-[12rem] max-w-[24rem] flex-1">
                  <StatBar label={statBarLabel} value={completedCount} max={totalCount} />
                </div>
              </div>
              {plan.stages.map((stage) => {
                const { done: stageDone, total: stageTotal } = stageCounts[stage.id] ?? {
                  done: 0,
                  total: 0,
                };
                return (
                  <CollapsiblePlanStage
                    key={stage.id}
                    stage={stage}
                    stageDone={stageDone}
                    stageTotal={stageTotal}
                  />
                );
              })}
              <PlanChartClient data={chartData} />
            </Rack>
          );
        })
      )}
    </main>
  );
}
