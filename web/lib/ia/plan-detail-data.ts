/**
 * plan-detail-data.ts — DB-backed loader for /dashboard/plan/[slug] RSC.
 *
 * Batched queries cover the 22-widget detail-page layout (groups A-G):
 *   plan + stages + tasks + commits + verifications + change log + journal
 *   + spec history + deps + fix-plan tuples + glossary refs.
 *
 * One round-trip per table; JS stitches by `slug` / `task_id` / `stage_id`.
 */

import { getSql } from '@/lib/db/client';
import type { GlossaryTerm } from '@/lib/glossary/types';

export type DbTaskStatus = 'pending' | 'implemented' | 'verified' | 'done' | 'archived';
export type DbStageStatus = 'pending' | 'in_progress' | 'done';
export type DbStageVerdict = 'pass' | 'fail' | 'partial';

export interface PlanDetailMasterRow {
  slug: string;
  title: string;
  preamble: string | null;
  description: string | null;
  created_at: string;
  updated_at: string;
}

export interface PlanDetailStageRow {
  slug: string;
  stage_id: string;
  title: string | null;
  objective: string | null;
  exit_criteria: string | null;
  status: DbStageStatus;
  body: string | null;
  created_at: string;
  updated_at: string;
}

export interface PlanDetailTaskRow {
  task_id: string;
  prefix: string;
  slug: string | null;
  stage_id: string | null;
  title: string;
  status: DbTaskStatus;
  priority: string | null;
  type: string | null;
  body: string | null;
  created_at: string;
  updated_at: string;
  completed_at: string | null;
  archived_at: string | null;
}

export interface PlanDetailCommitRow {
  task_id: string;
  commit_sha: string;
  commit_kind: string;
  message: string | null;
  recorded_at: string;
}

export interface PlanDetailVerificationRow {
  slug: string;
  stage_id: string;
  verdict: DbStageVerdict;
  commit_sha: string | null;
  notes: string | null;
  verified_at: string;
  actor: string | null;
}

export interface PlanDetailChangeLogRow {
  entry_id: number;
  slug: string;
  ts: string;
  kind: string;
  body: string;
  actor: string | null;
  commit_sha: string | null;
}

export interface PlanDetailJournalRow {
  task_id: string | null;
  stage_id: string | null;
  phase: string;
  payload_kind: string;
  payload: Record<string, unknown> | null;
  recorded_at: string;
}

export interface PlanDetailSpecHistoryRow {
  task_id: string;
  recorded_at: string;
  actor: string | null;
  git_sha: string | null;
  change_reason: string | null;
}

export interface PlanDetailDepRow {
  task_id: string;
  depends_on_id: string;
  kind: 'depends_on' | 'related';
  /** Slug of the depends-on task — used to flag cross-plan deps. */
  depends_on_slug: string | null;
  depends_on_title: string;
}

export interface PlanDetailFixPlanRow {
  task_id: string;
  round: number;
  applied_at: string | null;
}

export interface PlanDetailGlossaryHit {
  task_id: string;
  term: string;
}

export interface PlanDetailBundle {
  master: PlanDetailMasterRow;
  stages: PlanDetailStageRow[];
  tasks: PlanDetailTaskRow[];
  commits: PlanDetailCommitRow[];
  verifications: PlanDetailVerificationRow[];
  changeLog: PlanDetailChangeLogRow[];
  journal: PlanDetailJournalRow[];
  specHistory: PlanDetailSpecHistoryRow[];
  deps: PlanDetailDepRow[];
  fixPlans: PlanDetailFixPlanRow[];
  glossaryHits: PlanDetailGlossaryHit[];
}

/**
 * Load full detail bundle for one master plan.
 * Returns null if no plan with given slug.
 */
export async function loadPlanDetail(
  slug: string,
  glossary: GlossaryTerm[] = [],
): Promise<PlanDetailBundle | null> {
  const sql = getSql();

  const [master] = await sql<PlanDetailMasterRow[]>`
    SELECT slug, title, preamble, description, created_at, updated_at
    FROM ia_master_plans
    WHERE slug = ${slug}
  `;
  if (!master) return null;

  // Stage / task ids look like "1", "1.2", "10.3" — sort each segment numerically
  // so "10" comes after "2" instead of after "1" (default lexicographic order).
  const stages = await sql<PlanDetailStageRow[]>`
    SELECT slug, stage_id, title, objective, exit_criteria, status, body, created_at, updated_at
    FROM ia_stages
    WHERE slug = ${slug}
    ORDER BY string_to_array(stage_id, '.')::int[]
  `;

  const tasks = await sql<PlanDetailTaskRow[]>`
    SELECT task_id, prefix, slug, stage_id, title, status, priority, type, body,
           created_at, updated_at, completed_at, archived_at
    FROM ia_tasks
    WHERE slug = ${slug}
    ORDER BY
      CASE WHEN stage_id IS NULL THEN 1 ELSE 0 END,
      string_to_array(coalesce(stage_id, '0'), '.')::int[],
      task_id
  `;

  const taskIds = tasks.map((t) => t.task_id);

  const [commits, verifications, changeLog, journal, specHistory, deps, fixPlans] =
    await Promise.all([
      taskIds.length === 0
        ? Promise.resolve([] as PlanDetailCommitRow[])
        : sql<PlanDetailCommitRow[]>`
            SELECT task_id, commit_sha, commit_kind, message, recorded_at
            FROM ia_task_commits
            WHERE task_id IN ${sql(taskIds)}
            ORDER BY recorded_at DESC
          `,
      sql<PlanDetailVerificationRow[]>`
        SELECT slug, stage_id, verdict, commit_sha, notes, verified_at, actor
        FROM ia_stage_verifications
        WHERE slug = ${slug}
        ORDER BY verified_at DESC
      `,
      sql<PlanDetailChangeLogRow[]>`
        SELECT entry_id, slug, ts, kind, body, actor, commit_sha
        FROM ia_master_plan_change_log
        WHERE slug = ${slug}
        ORDER BY ts DESC
        LIMIT 200
      `,
      sql<PlanDetailJournalRow[]>`
        SELECT task_id, stage_id, phase, payload_kind, payload, recorded_at
        FROM ia_ship_stage_journal
        WHERE slug = ${slug}
        ORDER BY recorded_at DESC
        LIMIT 500
      `,
      taskIds.length === 0
        ? Promise.resolve([] as PlanDetailSpecHistoryRow[])
        : sql<PlanDetailSpecHistoryRow[]>`
            SELECT task_id, recorded_at, actor, git_sha, change_reason
            FROM ia_task_spec_history
            WHERE task_id IN ${sql(taskIds)}
            ORDER BY recorded_at DESC
          `,
      taskIds.length === 0
        ? Promise.resolve([] as PlanDetailDepRow[])
        : sql<PlanDetailDepRow[]>`
            SELECT d.task_id,
                   d.depends_on_id,
                   d.kind,
                   t.slug  AS depends_on_slug,
                   t.title AS depends_on_title
            FROM ia_task_deps d
            JOIN ia_tasks t ON t.task_id = d.depends_on_id
            WHERE d.task_id IN ${sql(taskIds)}
          `,
      taskIds.length === 0
        ? Promise.resolve([] as PlanDetailFixPlanRow[])
        : sql<PlanDetailFixPlanRow[]>`
            SELECT task_id, round, applied_at
            FROM ia_fix_plan_tuples
            WHERE task_id IN ${sql(taskIds)}
          `,
    ]);

  const glossaryHits = computeGlossaryHits(tasks, glossary);

  return {
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
  };
}

/** Load the slug list of all plans — used to populate the breadcrumb cross-links. */
export async function loadPlanSlugs(): Promise<{ slug: string; title: string }[]> {
  const sql = getSql();
  return sql<{ slug: string; title: string }[]>`
    SELECT slug, title FROM ia_master_plans ORDER BY slug
  `;
}

// ---------------------------------------------------------------------------
// Glossary scan — light text-match (case-insensitive substring) over task body.
// ---------------------------------------------------------------------------

function computeGlossaryHits(
  tasks: PlanDetailTaskRow[],
  glossary: GlossaryTerm[],
): PlanDetailGlossaryHit[] {
  if (glossary.length === 0) return [];
  const hits: PlanDetailGlossaryHit[] = [];
  const lowerTerms = glossary.map((g) => ({ term: g.term, lc: g.term.toLowerCase() }));
  for (const t of tasks) {
    if (!t.body) continue;
    const lcBody = t.body.toLowerCase();
    for (const { term, lc } of lowerTerms) {
      if (lcBody.includes(lc)) hits.push({ task_id: t.task_id, term });
    }
  }
  return hits;
}

// ---------------------------------------------------------------------------
// Aggregations consumed by detail-page widgets.
// ---------------------------------------------------------------------------

export interface VelocityPoint {
  /** ISO week start (YYYY-MM-DD). */
  week: string;
  count: number;
}

/** Tasks completed per ISO-week (Mon-anchored) — for sparkline + line chart. */
export function aggregateVelocity(tasks: PlanDetailTaskRow[]): VelocityPoint[] {
  const buckets = new Map<string, number>();
  for (const t of tasks) {
    if (!t.completed_at) continue;
    const w = isoWeekStart(new Date(t.completed_at));
    buckets.set(w, (buckets.get(w) ?? 0) + 1);
  }
  return Array.from(buckets.entries())
    .map(([week, count]) => ({ week, count }))
    .sort((a, b) => (a.week < b.week ? -1 : 1));
}

export interface BurndownPoint {
  date: string;
  open: number;
  closed: number;
}

/** Daily open vs closed counts derived from task created_at / completed_at. */
export function aggregateBurndown(tasks: PlanDetailTaskRow[]): BurndownPoint[] {
  if (tasks.length === 0) return [];
  const events: { date: string; delta: number }[] = [];
  for (const t of tasks) {
    events.push({ date: dayKey(new Date(t.created_at)), delta: +1 });
    if (t.completed_at) events.push({ date: dayKey(new Date(t.completed_at)), delta: -1 });
  }
  const days = Array.from(new Set(events.map((e) => e.date))).sort();
  if (days.length === 0) return [];
  let open = 0;
  let closed = 0;
  const out: BurndownPoint[] = [];
  for (const day of days) {
    const opensToday = events.filter((e) => e.date === day && e.delta === 1).length;
    const closesToday = events.filter((e) => e.date === day && e.delta === -1).length;
    open += opensToday - closesToday;
    closed += closesToday;
    out.push({ date: day, open, closed });
  }
  return out;
}

/** Cycle-time histogram bins (days) from created_at → completed_at. */
export function aggregateCycleTimes(tasks: PlanDetailTaskRow[]): number[] {
  const out: number[] = [];
  for (const t of tasks) {
    if (!t.completed_at) continue;
    const ms = new Date(t.completed_at).getTime() - new Date(t.created_at).getTime();
    if (ms < 0) continue;
    out.push(Math.max(0, Math.round(ms / (24 * 60 * 60 * 1000))));
  }
  return out;
}

export interface CommitSwimlanePoint {
  stageId: string;
  date: string;
  count: number;
}

/** Per-stage commit cadence — daily commit counts keyed by stage_id. */
export function aggregateCommitCadence(
  tasks: PlanDetailTaskRow[],
  commits: PlanDetailCommitRow[],
): CommitSwimlanePoint[] {
  const stageByTask = new Map<string, string>();
  for (const t of tasks) if (t.stage_id) stageByTask.set(t.task_id, t.stage_id);
  const buckets = new Map<string, number>();
  for (const c of commits) {
    const stage = stageByTask.get(c.task_id);
    if (!stage) continue;
    const day = dayKey(new Date(c.recorded_at));
    const k = `${stage}::${day}`;
    buckets.set(k, (buckets.get(k) ?? 0) + 1);
  }
  return Array.from(buckets.entries()).map(([k, count]) => {
    const [stageId, date] = k.split('::');
    return { stageId, date, count };
  });
}

export interface SpecChurnCell {
  stageId: string;
  taskId: string;
  revisions: number;
}

/** Per-task spec-history revision count. */
export function aggregateSpecChurn(
  tasks: PlanDetailTaskRow[],
  history: PlanDetailSpecHistoryRow[],
): SpecChurnCell[] {
  const counts = new Map<string, number>();
  for (const h of history) counts.set(h.task_id, (counts.get(h.task_id) ?? 0) + 1);
  return tasks
    .filter((t) => t.stage_id)
    .map((t) => ({
      stageId: t.stage_id as string,
      taskId: t.task_id,
      revisions: counts.get(t.task_id) ?? 0,
    }));
}

export interface FixPlanRoundsCell {
  taskId: string;
  rounds: number;
}

export function aggregateFixPlanRounds(rows: PlanDetailFixPlanRow[]): FixPlanRoundsCell[] {
  const max = new Map<string, number>();
  for (const r of rows) {
    const cur = max.get(r.task_id) ?? 0;
    if (r.round > cur) max.set(r.task_id, r.round);
  }
  return Array.from(max.entries()).map(([taskId, rounds]) => ({ taskId, rounds }));
}

// ---------------------------------------------------------------------------
// Date helpers
// ---------------------------------------------------------------------------

function dayKey(d: Date): string {
  return d.toISOString().slice(0, 10);
}

function isoWeekStart(d: Date): string {
  const date = new Date(Date.UTC(d.getUTCFullYear(), d.getUTCMonth(), d.getUTCDate()));
  const day = date.getUTCDay() || 7; // Sun → 7
  if (day !== 1) date.setUTCDate(date.getUTCDate() - (day - 1));
  return date.toISOString().slice(0, 10);
}
