/**
 * /dashboard/plan/[slug] — RSC detail view for one master plan.
 *
 * Composes the 22-widget detail bundle (groups A-G) over `loadPlanDetail()`:
 *   A header strip · B progress core · C time-series · D quality/verification
 *   · E structure/deps · F audit/activity · G contextual cross-refs.
 */

import Link from 'next/link';
import { notFound } from 'next/navigation';
import {
  loadPlanDetail,
  loadPlanSlugs,
  aggregateVelocity,
  aggregateBurndown,
  aggregateCycleTimes,
  aggregateCommitCadence,
  aggregateSpecChurn,
  aggregateFixPlanRounds,
  type PlanDetailStageRow,
  type PlanDetailTaskRow,
  type PlanDetailVerificationRow,
  type PlanDetailChangeLogRow,
  type PlanDetailJournalRow,
  type PlanDetailCommitRow,
  type PlanDetailDepRow,
  type PlanDetailGlossaryHit,
} from '@/lib/ia/plan-detail-data';
import { loadGlossaryTerms } from '@/lib/glossary/import';
import { buildGlossaryIndex } from '@/lib/glossary/index-build';
import { Markdown } from '@/lib/markdown/render';
import { BadgeChip, type Status } from '@/components/BadgeChip';
import { StatBar } from '@/components/StatBar';
import { Heading } from '@/components/type/Heading';
import { Tooltip } from '@/components/Tooltip';
import { DataTable, type Column } from '@/components/DataTable';
import { Bezel, Rack, Screen } from '@/components/console';
import PlanRingChartClient from '@/components/PlanRingChartClient';
import PlanChartClient from '@/components/PlanChartClient';
import type { PlanChartDatum } from '@/components/PlanChart';
import {
  BurndownChartClient,
  CommitCadenceChartClient,
  CycleHistogramClient,
  DepGraphClient,
  VelocityAreaChartClient,
} from '@/components/charts/clients';
import type { DepNode, DepLink } from '@/components/charts/DepGraph';

// ---------------------------------------------------------------------------
// Status mapping helpers
// ---------------------------------------------------------------------------

function dbStageToBadge(s: PlanDetailStageRow['status']): Status {
  if (s === 'done') return 'done';
  if (s === 'in_progress') return 'in-progress';
  return 'pending';
}

function dbTaskToBadge(s: PlanDetailTaskRow['status']): Status {
  if (s === 'done' || s === 'archived' || s === 'verified') return 'done';
  if (s === 'implemented') return 'in-progress';
  return 'pending';
}

// Derive a stage's effective status from its task statuses.
// `ia_stages.status` lags reality on legacy plans — task aggregation matches
// what the dashboard `.md`-derived view shows.
function deriveStageStatus(stageTasks: PlanDetailTaskRow[]): PlanDetailStageRow['status'] {
  if (stageTasks.length === 0) return 'pending';
  let done = 0;
  let progress = 0;
  for (const t of stageTasks) {
    if (t.status === 'done' || t.status === 'archived' || t.status === 'verified') done++;
    else if (t.status === 'implemented') progress++;
  }
  if (done === stageTasks.length) return 'done';
  if (done > 0 || progress > 0) return 'in_progress';
  return 'pending';
}

function verdictBadge(v: PlanDetailVerificationRow['verdict']): Status {
  if (v === 'pass') return 'done';
  if (v === 'fail') return 'blocked';
  return 'in-progress';
}

function shortSha(sha: string | null): string {
  return sha ? sha.slice(0, 7) : '—';
}

function formatTs(ts: string | null): string {
  if (!ts) return '—';
  return new Date(ts).toISOString().replace('T', ' ').slice(0, 16);
}

// Build per-stage ChartDatum: count of tasks per status bucket.
function buildStageChartData(
  stages: PlanDetailStageRow[],
  tasks: PlanDetailTaskRow[],
): PlanChartDatum[] {
  return stages.map((stage) => {
    const stageTasks = tasks.filter((t) => t.stage_id === stage.stage_id);
    let pending = 0;
    let inProgress = 0;
    let done = 0;
    for (const t of stageTasks) {
      if (t.status === 'done' || t.status === 'archived' || t.status === 'verified') done++;
      else if (t.status === 'implemented') inProgress++;
      else pending++;
    }
    return {
      label: `${stage.stage_id}`,
      pending,
      inProgress,
      done,
      status: stage.status,
    };
  });
}

// Per-stage status badge chart data (ring) — uses derived status from task aggregation.
function buildStageRingData(
  stages: PlanDetailStageRow[],
  tasks: PlanDetailTaskRow[],
): PlanChartDatum[] {
  return stages.map((stage) => {
    const stageTasks = tasks.filter((t) => t.stage_id === stage.stage_id);
    const status = deriveStageStatus(stageTasks);
    return {
      label: `Stage ${stage.stage_id}`,
      pending: status === 'pending' ? 1 : 0,
      inProgress: status === 'in_progress' ? 1 : 0,
      done: status === 'done' ? 1 : 0,
      status,
    };
  });
}

// Per-task status ring (one slice per task).
function buildTaskRingData(tasks: PlanDetailTaskRow[]): PlanChartDatum[] {
  return tasks.map((t) => {
    const buckets = (() => {
      if (t.status === 'done' || t.status === 'archived' || t.status === 'verified')
        return { pending: 0, inProgress: 0, done: 1 };
      if (t.status === 'implemented') return { pending: 0, inProgress: 1, done: 0 };
      return { pending: 1, inProgress: 0, done: 0 };
    })();
    return {
      label: t.task_id,
      ...buckets,
      status: t.status,
    };
  });
}

// Build dep-graph node + link arrays. External = depends_on_slug !== current slug.
function buildDepGraph(
  tasks: PlanDetailTaskRow[],
  deps: PlanDetailDepRow[],
  currentSlug: string,
): { nodes: DepNode[]; links: DepLink[] } {
  const nodeMap = new Map<string, DepNode>();
  for (const t of tasks) {
    nodeMap.set(t.task_id, {
      id: t.task_id,
      title: t.title,
      status: t.status,
      external: false,
      stage: t.stage_id ?? null,
    });
  }
  const links: DepLink[] = [];
  for (const d of deps) {
    if (!nodeMap.has(d.depends_on_id)) {
      nodeMap.set(d.depends_on_id, {
        id: d.depends_on_id,
        title: d.depends_on_title,
        status: 'pending',
        external: d.depends_on_slug !== null && d.depends_on_slug !== currentSlug,
        stage: null,
      });
    }
    links.push({ source: d.task_id, target: d.depends_on_id, kind: d.kind });
  }
  return { nodes: Array.from(nodeMap.values()), links };
}

// ---------------------------------------------------------------------------
// Page
// ---------------------------------------------------------------------------

export default async function PlanDetailPage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const { slug } = await params;

  const [glossaryTerms, allSlugs] = await Promise.all([
    loadGlossaryTerms(),
    loadPlanSlugs(),
  ]);
  const bundle = await loadPlanDetail(slug, glossaryTerms);

  if (!bundle) notFound();

  const glossary = buildGlossaryIndex(glossaryTerms);
  const {
    master,
    stages,
    tasks,
    commits,
    verifications,
    changeLog,
    journal,
    specHistory,
    deps,
    fixPlans,
    glossaryHits,
  } = bundle;

  // ----- aggregates -----
  const tasksDone = tasks.filter(
    (t) => t.status === 'done' || t.status === 'archived' || t.status === 'verified',
  ).length;
  const tasksInProg = tasks.filter((t) => t.status === 'implemented').length;
  const tasksPending = tasks.length - tasksDone - tasksInProg;
  // Derive stage status per row — DB ia_stages.status lags reality.
  const stageDerived = stages.map((s) => ({
    stage: s,
    derived: deriveStageStatus(tasks.filter((t) => t.stage_id === s.stage_id)),
  }));
  const stagesDone = stageDerived.filter((s) => s.derived === 'done').length;
  const overallPct =
    tasks.length === 0 ? 0 : Math.round((tasksDone / tasks.length) * 100);

  const lastTouched = [
    ...stages.map((s) => s.updated_at),
    ...tasks.map((t) => t.updated_at),
    master.updated_at,
  ]
    .filter(Boolean)
    .sort()
    .at(-1);

  const overallStatus: Status =
    tasksDone === tasks.length && tasks.length > 0
      ? 'done'
      : tasksInProg > 0 || tasksDone > 0
        ? 'in-progress'
        : 'pending';

  const stageChartData = buildStageChartData(stages, tasks);
  const stageRingData = buildStageRingData(stages, tasks);
  const taskRingData = buildTaskRingData(tasks);

  const velocity = aggregateVelocity(tasks);
  const burndown = aggregateBurndown(tasks);
  const cycleTimes = aggregateCycleTimes(tasks);
  const commitCadence = aggregateCommitCadence(tasks, commits);
  const specChurn = aggregateSpecChurn(tasks, specHistory);
  const fixPlanRounds = aggregateFixPlanRounds(fixPlans);

  const stageIds = stages.map((s) => s.stage_id);
  const { nodes: depNodes, links: depLinks } = buildDepGraph(tasks, deps, slug);
  const externalDeps = deps.filter(
    (d) => d.depends_on_slug !== null && d.depends_on_slug !== slug,
  );

  // glossary hits — group by term, count occurrences
  const glossaryByTerm = new Map<string, number>();
  for (const h of glossaryHits) {
    glossaryByTerm.set(h.term, (glossaryByTerm.get(h.term) ?? 0) + 1);
  }
  const glossarySorted = Array.from(glossaryByTerm.entries()).sort(
    (a, b) => b[1] - a[1],
  );

  // ---------------------------------------------------------------------------
  // Table column sets
  // ---------------------------------------------------------------------------

  const verificationCols: Column<PlanDetailVerificationRow>[] = [
    { key: 'stage_id', header: 'Stage', render: (r) => `Stage ${r.stage_id}` },
    {
      key: 'verdict',
      header: 'Verdict',
      render: (r) => <BadgeChip status={verdictBadge(r.verdict)} />,
    },
    { key: 'commit_sha', header: 'Commit', render: (r) => <code className="font-mono text-xs">{shortSha(r.commit_sha)}</code> },
    { key: 'verified_at', header: 'When', render: (r) => formatTs(r.verified_at) },
    { key: 'actor', header: 'Actor', render: (r) => r.actor ?? '—' },
  ];

  const changeLogCols: Column<PlanDetailChangeLogRow>[] = [
    { key: 'ts', header: 'When', render: (r) => formatTs(r.ts) },
    { key: 'kind', header: 'Kind' },
    { key: 'body', header: 'Body', render: (r) => <span className="line-clamp-2 text-sm">{r.body}</span> },
    { key: 'commit_sha', header: 'Commit', render: (r) => <code className="font-mono text-xs">{shortSha(r.commit_sha)}</code> },
  ];

  const commitCols: Column<PlanDetailCommitRow>[] = [
    { key: 'recorded_at', header: 'When', render: (r) => formatTs(r.recorded_at) },
    { key: 'task_id', header: 'Task' },
    { key: 'commit_kind', header: 'Kind' },
    { key: 'commit_sha', header: 'Sha', render: (r) => <code className="font-mono text-xs">{shortSha(r.commit_sha)}</code> },
    { key: 'message', header: 'Message', render: (r) => <span className="line-clamp-1 text-sm">{r.message ?? '—'}</span> },
  ];

  const journalCols: Column<PlanDetailJournalRow>[] = [
    { key: 'recorded_at', header: 'When', render: (r) => formatTs(r.recorded_at) },
    { key: 'phase', header: 'Phase' },
    { key: 'payload_kind', header: 'Kind' },
    { key: 'task_id', header: 'Task', render: (r) => r.task_id ?? '—' },
    { key: 'stage_id', header: 'Stage', render: (r) => r.stage_id ?? '—' },
  ];

  // ---------------------------------------------------------------------------
  // Render
  // ---------------------------------------------------------------------------

  return (
    <main className="mx-auto max-w-6xl space-y-6 px-4 py-8">
      {/* Breadcrumb */}
      <div className="mb-2 flex flex-wrap gap-2 font-mono text-[11px] uppercase tracking-wide text-[var(--ds-text-meta)]">
        <Link href="/" className="text-[var(--ds-text-meta)]">
          Territory
        </Link>
        <span className="opacity-50">{'//'}</span>
        <Link href="/dashboard" className="text-[var(--ds-text-meta)]">
          Dashboard
        </Link>
        <span className="opacity-50">{'//'}</span>
        <span className="text-[var(--ds-raw-blue)]">{slug}</span>
      </div>

      {/* ===== Group A — Header strip ===== */}
      <Rack label="Master plan // detail" className="space-y-4">
        <div className="space-y-2">
          <div className="flex flex-wrap items-center gap-3">
            <Heading level="h1" className="min-w-0 text-[26px] font-semibold">
              {master.title}
            </Heading>
            <BadgeChip status={overallStatus} />
            <span className="font-mono text-[10px] uppercase tracking-[0.2em] text-[var(--ds-text-meta)]">
              {slug}
            </span>
          </div>
          <div className="min-w-[12rem] max-w-[36rem]">
            <StatBar label={`Overall ${overallPct}%`} value={tasksDone} max={Math.max(1, tasks.length)} />
          </div>
          {master.description && (
            <div className="text-[var(--ds-text-meta)]">
              <Markdown source={master.description} glossary={glossary} />
            </div>
          )}
          {master.preamble && (
            <details className="text-sm text-[var(--ds-text-muted)]">
              <summary className="cursor-pointer font-mono text-xs uppercase tracking-wide text-[var(--ds-text-meta)]">
                Preamble
              </summary>
              <div className="mt-2">
                <Markdown source={master.preamble} glossary={glossary} />
              </div>
            </details>
          )}
        </div>

        <div className="grid gap-3" style={{ gridTemplateColumns: 'repeat(auto-fit, minmax(120px, 1fr))' }}>
          <Bezel>
            <Screen tone="dark" sweep={false} className="lcd p-2 text-[var(--ds-raw-green)]">
              <div className="lcd-label font-mono text-[9px] uppercase tracking-[0.25em] opacity-60">
                Tasks done
              </div>
              <div className="font-mono text-[26px] font-bold leading-none">
                {String(tasksDone).padStart(3, '0')}
              </div>
            </Screen>
          </Bezel>
          <Bezel>
            <Screen tone="readout" sweep={false} className="lcd p-2">
              <div className="lcd-label font-mono text-[9px] uppercase tracking-[0.25em] opacity-60">
                In progress
              </div>
              <div className="font-mono text-[26px] font-bold leading-none text-[var(--ds-raw-amber)]">
                {String(tasksInProg).padStart(3, '0')}
              </div>
            </Screen>
          </Bezel>
          <Bezel>
            <Screen tone="dark" sweep={false} className="lcd p-2 text-[var(--ds-text-meta)]">
              <div className="lcd-label font-mono text-[9px] uppercase tracking-[0.25em] opacity-60">
                Pending
              </div>
              <div className="font-mono text-[26px] font-bold leading-none">
                {String(tasksPending).padStart(3, '0')}
              </div>
            </Screen>
          </Bezel>
          <Bezel>
            <Screen tone="dark" sweep={false} className="lcd p-2 text-[var(--ds-raw-blue)]">
              <div className="lcd-label font-mono text-[9px] uppercase tracking-[0.25em] opacity-60">
                Stages
              </div>
              <div className="font-mono text-[26px] font-bold leading-none">
                {stagesDone}/{stages.length}
              </div>
            </Screen>
          </Bezel>
          <Bezel>
            <Screen tone="dark" sweep={false} className="lcd p-2 text-[var(--ds-text-meta)]">
              <div className="lcd-label font-mono text-[9px] uppercase tracking-[0.25em] opacity-60">
                Last touched
              </div>
              <div className="font-mono text-[12px] leading-tight">
                {formatTs(lastTouched ?? null)}
              </div>
            </Screen>
          </Bezel>
        </div>
      </Rack>

      {/* ===== Group B — Progress core ===== */}
      <Rack label="Progress // rings + stacks">
        <div className="flex flex-wrap items-start justify-around gap-4">
          <div className="text-center">
            <div className="mb-1 font-mono text-[10px] uppercase tracking-[0.2em] text-[var(--ds-text-meta)]">
              Stages
            </div>
            <PlanRingChartClient data={stageRingData} unitLabel="STAGES" />
          </div>
          <div className="text-center">
            <div className="mb-1 font-mono text-[10px] uppercase tracking-[0.2em] text-[var(--ds-text-meta)]">
              Tasks
            </div>
            <PlanRingChartClient data={taskRingData} unitLabel="TASKS" />
          </div>
          <div>
            <div className="mb-1 font-mono text-[10px] uppercase tracking-[0.2em] text-[var(--ds-text-meta)]">
              Per-stage stack
            </div>
            <PlanChartClient data={stageChartData} />
          </div>
        </div>
      </Rack>

      {/* ===== Group C — Time-series ===== */}
      <Rack label="Time-series // velocity / burndown / cycle / cadence">
        <div className="grid gap-4 md:grid-cols-2">
          <div>
            <div className="mb-1 font-mono text-[10px] uppercase tracking-[0.2em] text-[var(--ds-text-meta)]">
              Velocity (tasks per week)
            </div>
            <VelocityAreaChartClient data={velocity} />
          </div>
          <div>
            <div className="mb-1 font-mono text-[10px] uppercase tracking-[0.2em] text-[var(--ds-text-meta)]">
              Burndown (open vs closed)
            </div>
            <BurndownChartClient data={burndown} />
          </div>
          <div>
            <div className="mb-1 font-mono text-[10px] uppercase tracking-[0.2em] text-[var(--ds-text-meta)]">
              Cycle-time histogram (days)
            </div>
            <CycleHistogramClient data={cycleTimes} />
          </div>
          <div className="md:col-span-2">
            <div className="mb-1 font-mono text-[10px] uppercase tracking-[0.2em] text-[var(--ds-text-meta)]">
              Commit cadence (per stage)
            </div>
            <CommitCadenceChartClient data={commitCadence} stageIds={stageIds} />
          </div>
        </div>
      </Rack>

      {/* ===== Group D — Quality / verification ===== */}
      <Rack label="Quality // verifications + churn + fix-plan rounds">
        <div className="space-y-4">
          {verifications.length === 0 ? (
            <p className="text-sm text-[var(--ds-text-muted)]">No stage verifications recorded.</p>
          ) : (
            <DataTable
              columns={verificationCols}
              rows={verifications}
              getRowKey={(r) => `${r.slug}-${r.stage_id}-${r.verified_at}`}
            />
          )}

          <div>
            <div className="mb-2 font-mono text-[10px] uppercase tracking-[0.2em] text-[var(--ds-text-meta)]">
              Spec churn (revisions per task)
            </div>
            <div className="flex flex-wrap gap-2">
              {specChurn.length === 0 ? (
                <span className="text-xs text-[var(--ds-text-muted)]">No spec history yet.</span>
              ) : (
                specChurn.map((c) => (
                  <Tooltip
                    key={c.taskId}
                    label={c.taskId}
                    content={`${c.revisions} revisions in stage ${c.stageId}`}
                  >
                    <span
                      className="inline-flex h-7 min-w-[3rem] items-center justify-center rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] px-2 font-mono text-xs"
                      style={{
                        opacity: 0.45 + Math.min(0.55, c.revisions * 0.15),
                      }}
                    >
                      {c.taskId}·{c.revisions}
                    </span>
                  </Tooltip>
                ))
              )}
            </div>
          </div>

          <div>
            <div className="mb-2 font-mono text-[10px] uppercase tracking-[0.2em] text-[var(--ds-text-meta)]">
              Fix-plan rounds (per task)
            </div>
            <div className="flex flex-wrap gap-2">
              {fixPlanRounds.length === 0 ? (
                <span className="text-xs text-[var(--ds-text-muted)]">No fix-plan rounds.</span>
              ) : (
                fixPlanRounds.map((r) => (
                  <span
                    key={r.taskId}
                    className="inline-flex items-center gap-1 rounded-full border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] px-2 py-0.5 font-mono text-xs"
                  >
                    <span className="text-[var(--ds-text-muted)]">{r.taskId}</span>
                    <span className="text-[var(--ds-raw-amber)]">×{r.rounds}</span>
                  </span>
                ))
              )}
            </div>
          </div>
        </div>
      </Rack>

      {/* ===== Group E — Structure / deps ===== */}
      <Rack label="Structure // stages + deps">
        <div className="space-y-4">
          <div>
            <div className="mb-2 font-mono text-[10px] uppercase tracking-[0.2em] text-[var(--ds-text-meta)]">
              Stages ({stages.length})
            </div>
            <ul className="space-y-2">
              {stageDerived.map(({ stage, derived }) => {
                const stageTasks = tasks.filter((t) => t.stage_id === stage.stage_id);
                const stageDone = stageTasks.filter(
                  (t) => t.status === 'done' || t.status === 'archived' || t.status === 'verified',
                ).length;
                return (
                  <li
                    key={stage.stage_id}
                    className="rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] px-3 py-2"
                  >
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="font-mono text-sm text-[var(--ds-text-primary)]">
                        Stage {stage.stage_id}
                      </span>
                      <BadgeChip status={dbStageToBadge(derived)} />
                      <span className="font-mono text-xs text-[var(--ds-text-muted)]">
                        {stageDone}/{stageTasks.length} tasks
                      </span>
                      {stage.title && (
                        <span className="text-sm text-[var(--ds-text-primary)]">
                          {stage.title}
                        </span>
                      )}
                    </div>
                    {stage.objective && (
                      <div className="mt-1 text-xs text-[var(--ds-text-muted)]">
                        <Markdown source={stage.objective} glossary={glossary} />
                      </div>
                    )}
                  </li>
                );
              })}
            </ul>
          </div>

          <div>
            <div className="mb-2 font-mono text-[10px] uppercase tracking-[0.2em] text-[var(--ds-text-meta)]">
              Dependency graph ({depNodes.length} nodes / {depLinks.length} edges)
            </div>
            <DepGraphClient nodes={depNodes} links={depLinks} />
          </div>

          {externalDeps.length > 0 && (
            <div>
              <div className="mb-2 font-mono text-[10px] uppercase tracking-[0.2em] text-[var(--ds-text-meta)]">
                Cross-plan dependencies ({externalDeps.length})
              </div>
              <div className="flex flex-wrap gap-2">
                {externalDeps.map((d) => (
                  <Link
                    key={`${d.task_id}-${d.depends_on_id}`}
                    href={`/dashboard/plan/${d.depends_on_slug}`}
                    className="inline-flex items-center gap-1 rounded border border-[var(--ds-raw-amber)] bg-[var(--ds-bg-panel)] px-2 py-0.5 font-mono text-xs text-[var(--ds-raw-amber)] hover:underline"
                  >
                    {d.task_id} → {d.depends_on_id}
                    <span className="text-[var(--ds-text-meta)]">@{d.depends_on_slug}</span>
                  </Link>
                ))}
              </div>
            </div>
          )}
        </div>
      </Rack>

      {/* ===== Group F — Audit / activity ===== */}
      <Rack label="Activity // change log + journal + commits">
        <div className="space-y-4">
          <div>
            <div className="mb-2 font-mono text-[10px] uppercase tracking-[0.2em] text-[var(--ds-text-meta)]">
              Change log ({changeLog.length})
            </div>
            {changeLog.length === 0 ? (
              <p className="text-sm text-[var(--ds-text-muted)]">No change-log entries.</p>
            ) : (
              <DataTable
                columns={changeLogCols}
                rows={changeLog}
                getRowKey={(r) => r.entry_id}
              />
            )}
          </div>

          <div>
            <div className="mb-2 font-mono text-[10px] uppercase tracking-[0.2em] text-[var(--ds-text-meta)]">
              Ship-stage journal ({journal.length})
            </div>
            {journal.length === 0 ? (
              <p className="text-sm text-[var(--ds-text-muted)]">No journal events.</p>
            ) : (
              <DataTable
                columns={journalCols}
                rows={journal.slice(0, 50)}
                getRowKey={(r, i) => `${r.recorded_at}-${i}`}
              />
            )}
          </div>

          <div>
            <div className="mb-2 font-mono text-[10px] uppercase tracking-[0.2em] text-[var(--ds-text-meta)]">
              Recent commits ({commits.length})
            </div>
            {commits.length === 0 ? (
              <p className="text-sm text-[var(--ds-text-muted)]">No commits recorded.</p>
            ) : (
              <DataTable
                columns={commitCols}
                rows={commits.slice(0, 50)}
                getRowKey={(r, i) => `${r.commit_sha}-${i}`}
              />
            )}
          </div>
        </div>
      </Rack>

      {/* ===== Group G — Contextual cross-refs ===== */}
      <Rack label="Cross-refs // glossary + plan index">
        <div className="space-y-4">
          <div>
            <div className="mb-2 font-mono text-[10px] uppercase tracking-[0.2em] text-[var(--ds-text-meta)]">
              Glossary references ({glossarySorted.length} unique terms)
            </div>
            <div className="flex flex-wrap gap-2">
              {glossarySorted.length === 0 ? (
                <span className="text-xs text-[var(--ds-text-muted)]">
                  No glossary terms found in task bodies.
                </span>
              ) : (
                glossarySorted.map(([term, count]) => {
                  const def = glossary.lookup(term);
                  return (
                    <Tooltip
                      key={term}
                      label={term}
                      content={def?.definition ?? `Used in ${count} tasks.`}
                    >
                      <span className="inline-flex items-center gap-1 rounded-full border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] px-2 py-0.5 font-mono text-xs">
                        <span className="text-[var(--ds-raw-blue)]">{term}</span>
                        <span className="text-[var(--ds-text-meta)]">×{count}</span>
                      </span>
                    </Tooltip>
                  );
                })
              )}
            </div>
          </div>

          <div>
            <div className="mb-2 font-mono text-[10px] uppercase tracking-[0.2em] text-[var(--ds-text-meta)]">
              Other master plans
            </div>
            <div className="flex flex-wrap gap-2">
              {allSlugs
                .filter((p) => p.slug !== slug)
                .map((p) => (
                  <Link
                    key={p.slug}
                    href={`/dashboard/plan/${p.slug}`}
                    className="inline-flex items-center rounded border border-[var(--ds-border-subtle)] bg-[var(--ds-bg-panel)] px-2 py-0.5 font-mono text-xs text-[var(--ds-text-muted)] hover:text-[var(--ds-raw-blue)] hover:underline"
                  >
                    {p.title}
                  </Link>
                ))}
            </div>
          </div>
        </div>
      </Rack>
    </main>
  );
}
