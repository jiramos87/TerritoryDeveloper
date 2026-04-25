/**
 * Read-only IA dashboard query helpers (Step 10 — DB-primary refactor).
 *
 * Direct pg via shared `getSql()` — mirrors the territory-ia MCP read tools
 * (`master_plan_state`, `stage_state`, `task_state`, `task_spec_body`,
 * `task_spec_search`) but skips the subprocess transport. Both surfaces
 * read the same `ia_*` tables; types intentionally line up.
 *
 * @see `docs/ia-dev-db-refactor-implementation.md` Step 10
 */

import { sql } from "@/lib/db/client";
import type {
  ChangeLogRow,
  MasterPlanRow,
  PlanDetail,
  SearchHit,
  StageRow,
  TaskBodyResponse,
  TaskDetail,
  TaskRow,
} from "@/types/api/ia-api";

export async function listMasterPlans(): Promise<MasterPlanRow[]> {
  const rows = await sql<
    {
      slug: string;
      title: string;
      created_at: Date;
      updated_at: Date;
      stage_count: string;
      task_count: string;
      task_done_count: string;
    }[]
  >`
    SELECT
      p.slug,
      p.title,
      p.created_at,
      p.updated_at,
      coalesce(s.stage_count, 0) AS stage_count,
      coalesce(t.task_count, 0) AS task_count,
      coalesce(t.task_done_count, 0) AS task_done_count
    FROM ia_master_plans p
    LEFT JOIN (
      SELECT slug, count(*)::bigint AS stage_count
      FROM ia_stages GROUP BY slug
    ) s USING (slug)
    LEFT JOIN (
      SELECT
        slug,
        count(*)::bigint AS task_count,
        count(*) FILTER (WHERE status IN ('done', 'archived'))::bigint AS task_done_count
      FROM ia_tasks WHERE slug IS NOT NULL GROUP BY slug
    ) t USING (slug)
    ORDER BY p.updated_at DESC
  `;
  return rows.map((r) => ({
    slug: r.slug,
    title: r.title,
    created_at: r.created_at.toISOString(),
    updated_at: r.updated_at.toISOString(),
    stage_count: Number(r.stage_count),
    task_count: Number(r.task_count),
    task_done_count: Number(r.task_done_count),
  }));
}

export async function getMasterPlan(slug: string): Promise<PlanDetail | null> {
  const planRows = await sql<
    {
      slug: string;
      title: string;
      preamble: string | null;
      created_at: Date;
      updated_at: Date;
    }[]
  >`
    SELECT slug, title, preamble, created_at, updated_at
    FROM ia_master_plans WHERE slug = ${slug}
  `;
  if (planRows.length === 0) return null;
  const plan = planRows[0];

  const stageRows = await sql<
    {
      slug: string;
      stage_id: string;
      title: string | null;
      objective: string | null;
      exit_criteria: string | null;
      status: "pending" | "in_progress" | "done";
      created_at: Date;
      updated_at: Date;
    }[]
  >`
    SELECT slug, stage_id, title, objective, exit_criteria, status, created_at, updated_at
    FROM ia_stages WHERE slug = ${slug}
    ORDER BY stage_id
  `;

  const taskRows = await sql<
    {
      task_id: string;
      prefix: TaskRow["prefix"];
      slug: string | null;
      stage_id: string | null;
      title: string;
      status: TaskRow["status"];
      priority: string | null;
      type: string | null;
      notes: string | null;
      created_at: Date;
      updated_at: Date;
      completed_at: Date | null;
      archived_at: Date | null;
    }[]
  >`
    SELECT
      task_id, prefix, slug, stage_id, title, status, priority, type, notes,
      created_at, updated_at, completed_at, archived_at
    FROM ia_tasks WHERE slug = ${slug}
    ORDER BY stage_id, task_id
  `;

  const changeLogRows = await sql<
    {
      entry_id: string;
      slug: string;
      ts: Date;
      kind: string;
      body: string;
      actor: string | null;
      commit_sha: string | null;
    }[]
  >`
    SELECT entry_id, slug, ts, kind, body, actor, commit_sha
    FROM ia_master_plan_change_log WHERE slug = ${slug}
    ORDER BY ts DESC LIMIT 50
  `;

  const tasksByStage = new Map<string | null, TaskRow[]>();
  for (const t of taskRows) {
    const k = t.stage_id;
    if (!tasksByStage.has(k)) tasksByStage.set(k, []);
    tasksByStage.get(k)!.push({
      task_id: t.task_id,
      prefix: t.prefix,
      slug: t.slug,
      stage_id: t.stage_id,
      title: t.title,
      status: t.status,
      priority: t.priority,
      type: t.type,
      notes: t.notes,
      created_at: t.created_at.toISOString(),
      updated_at: t.updated_at.toISOString(),
      completed_at: t.completed_at?.toISOString() ?? null,
      archived_at: t.archived_at?.toISOString() ?? null,
    });
  }

  const stages = stageRows.map((s) => {
    const tasks = tasksByStage.get(s.stage_id) ?? [];
    return {
      slug: s.slug,
      stage_id: s.stage_id,
      title: s.title,
      objective: s.objective,
      exit_criteria: s.exit_criteria,
      status: s.status,
      created_at: s.created_at.toISOString(),
      updated_at: s.updated_at.toISOString(),
      task_count: tasks.length,
      task_done_count: tasks.filter((t) => t.status === "done" || t.status === "archived").length,
      tasks,
    };
  });

  const change_log: ChangeLogRow[] = changeLogRows.map((r) => ({
    entry_id: Number(r.entry_id),
    slug: r.slug,
    ts: r.ts.toISOString(),
    kind: r.kind,
    body: r.body,
    actor: r.actor,
    commit_sha: r.commit_sha,
  }));

  const totalTasks = taskRows.length;
  const doneTasks = taskRows.filter((t) => t.status === "done" || t.status === "archived").length;

  const planRow: MasterPlanRow = {
    slug: plan.slug,
    title: plan.title,
    created_at: plan.created_at.toISOString(),
    updated_at: plan.updated_at.toISOString(),
    stage_count: stageRows.length,
    task_count: totalTasks,
    task_done_count: doneTasks,
  };

  return { plan: planRow, preamble: plan.preamble, stages, change_log };
}

export async function getTask(taskId: string): Promise<TaskDetail | null> {
  const taskRows = await sql<
    {
      task_id: string;
      prefix: TaskRow["prefix"];
      slug: string | null;
      stage_id: string | null;
      title: string;
      status: TaskRow["status"];
      priority: string | null;
      type: string | null;
      notes: string | null;
      created_at: Date;
      updated_at: Date;
      completed_at: Date | null;
      archived_at: Date | null;
    }[]
  >`
    SELECT
      task_id, prefix, slug, stage_id, title, status, priority, type, notes,
      created_at, updated_at, completed_at, archived_at
    FROM ia_tasks WHERE task_id = ${taskId}
  `;
  if (taskRows.length === 0) return null;
  const t = taskRows[0];
  const task: TaskRow = {
    task_id: t.task_id,
    prefix: t.prefix,
    slug: t.slug,
    stage_id: t.stage_id,
    title: t.title,
    status: t.status,
    priority: t.priority,
    type: t.type,
    notes: t.notes,
    created_at: t.created_at.toISOString(),
    updated_at: t.updated_at.toISOString(),
    completed_at: t.completed_at?.toISOString() ?? null,
    archived_at: t.archived_at?.toISOString() ?? null,
  };

  const depRows = await sql<
    { task_id: string; depends_on_id: string; kind: "depends_on" | "related" }[]
  >`
    SELECT task_id, depends_on_id, kind
    FROM ia_task_deps WHERE task_id = ${taskId}
    ORDER BY kind, depends_on_id
  `;

  const commitRows = await sql<
    { id: string; commit_sha: string; commit_kind: string; message: string | null; recorded_at: Date }[]
  >`
    SELECT id, commit_sha, commit_kind, message, recorded_at
    FROM ia_task_commits WHERE task_id = ${taskId}
    ORDER BY recorded_at DESC LIMIT 50
  `;

  const historyRows = await sql<{ count: string }[]>`
    SELECT count(*)::bigint AS count
    FROM ia_task_spec_history WHERE task_id = ${taskId}
  `;

  return {
    task,
    deps: depRows.map((d) => ({ task_id: d.task_id, depends_on_id: d.depends_on_id, kind: d.kind })),
    commits: commitRows.map((c) => ({
      id: Number(c.id),
      commit_sha: c.commit_sha,
      commit_kind: c.commit_kind,
      message: c.message,
      recorded_at: c.recorded_at.toISOString(),
    })),
    history_count: Number(historyRows[0]?.count ?? 0),
  };
}

export async function getTaskBody(taskId: string): Promise<TaskBodyResponse | null> {
  const rows = await sql<{ task_id: string; body: string }[]>`
    SELECT task_id, body FROM ia_tasks WHERE task_id = ${taskId}
  `;
  if (rows.length === 0) return null;
  return { task_id: rows[0].task_id, body: rows[0].body };
}

export async function searchTaskSpecs(q: string, limit = 25): Promise<SearchHit[]> {
  if (!q.trim()) return [];
  const cap = Math.max(1, Math.min(limit, 100));
  const rows = await sql<
    {
      task_id: string;
      prefix: string;
      slug: string | null;
      stage_id: string | null;
      title: string;
      status: string;
      rank: number;
      snippet: string;
    }[]
  >`
    SELECT
      task_id, prefix, slug, stage_id, title, status::text AS status,
      ts_rank(body_tsv, websearch_to_tsquery('english', ${q}))::float AS rank,
      ts_headline('english', body, websearch_to_tsquery('english', ${q}),
        'StartSel=<<,StopSel=>>,MaxFragments=2,FragmentDelimiter= ... ,MaxWords=20,MinWords=5')
        AS snippet
    FROM ia_tasks
    WHERE body_tsv @@ websearch_to_tsquery('english', ${q})
    ORDER BY rank DESC, updated_at DESC
    LIMIT ${cap}
  `;
  return rows;
}

export async function getStageRow(slug: string, stageId: string): Promise<StageRow | null> {
  const rows = await sql<
    {
      slug: string;
      stage_id: string;
      title: string | null;
      objective: string | null;
      exit_criteria: string | null;
      status: "pending" | "in_progress" | "done";
      created_at: Date;
      updated_at: Date;
      task_count: string;
      task_done_count: string;
    }[]
  >`
    SELECT
      s.slug, s.stage_id, s.title, s.objective, s.exit_criteria, s.status,
      s.created_at, s.updated_at,
      coalesce(t.task_count, 0) AS task_count,
      coalesce(t.task_done_count, 0) AS task_done_count
    FROM ia_stages s
    LEFT JOIN (
      SELECT slug, stage_id,
        count(*)::bigint AS task_count,
        count(*) FILTER (WHERE status IN ('done', 'archived'))::bigint AS task_done_count
      FROM ia_tasks GROUP BY slug, stage_id
    ) t USING (slug, stage_id)
    WHERE s.slug = ${slug} AND s.stage_id = ${stageId}
  `;
  if (rows.length === 0) return null;
  const r = rows[0];
  return {
    slug: r.slug,
    stage_id: r.stage_id,
    title: r.title,
    objective: r.objective,
    exit_criteria: r.exit_criteria,
    status: r.status,
    created_at: r.created_at.toISOString(),
    updated_at: r.updated_at.toISOString(),
    task_count: Number(r.task_count),
    task_done_count: Number(r.task_done_count),
  };
}
