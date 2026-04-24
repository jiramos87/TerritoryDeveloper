/**
 * DB read queries for the IA tables (Step 3 of ia-dev-db-refactor).
 *
 * All queries hit the shared pool from `./pool.ts`. Filesystem is never
 * touched here — body text is stored in `ia_tasks.body`, full-text search
 * via `ia_tasks.body_tsv` (GIN), trigram via `ia_tasks.body gin_trgm_ops`.
 *
 * Source of truth for decisions: docs/ia-dev-db-refactor-implementation.md
 * §Step 3. Related skill: N/A (tools consumed by read MCP surfaces).
 */

import type pg from "pg";
import { getIaDatabasePool } from "./pool.js";

// ---------------------------------------------------------------------------
// Typed shapes (subset of DB columns; extend as tools grow).
// ---------------------------------------------------------------------------

export interface TaskRowDB {
  task_id: string;
  prefix: string;
  slug: string | null;
  stage_id: string | null;
  title: string;
  status: "pending" | "implemented" | "verified" | "done" | "archived";
  priority: string | null;
  type: string | null;
  notes: string | null;
  body: string;
  created_at: string;
  updated_at: string;
  completed_at: string | null;
  archived_at: string | null;
}

export interface TaskDepsDB {
  depends_on: string[];
  related: string[];
}

export interface TaskCommitDB {
  commit_sha: string;
  commit_kind: string;
  message: string | null;
  recorded_at: string;
}

export interface TaskStateDB extends TaskRowDB {
  deps: TaskDepsDB;
  depends_on_status: Record<string, "pending" | "archived" | "unknown">;
  commits: TaskCommitDB[];
}

export interface StageRowDB {
  slug: string;
  stage_id: string;
  title: string | null;
  objective: string | null;
  exit_criteria: string | null;
  status: "pending" | "in_progress" | "done";
  source_file_path: string | null;
  created_at: string;
  updated_at: string;
}

export interface StageTaskSummary {
  task_id: string;
  title: string;
  status: TaskRowDB["status"];
  priority: string | null;
}

export interface StageStateDB extends StageRowDB {
  tasks: StageTaskSummary[];
  counts: Record<TaskRowDB["status"], number>;
  next_pending: string | null;
  latest_verdict: {
    verdict: "pass" | "fail" | "partial";
    verified_at: string;
    commit_sha: string | null;
  } | null;
}

export interface MasterPlanRowDB {
  slug: string;
  title: string;
  source_spec_path: string | null;
  created_at: string;
  updated_at: string;
}

export interface MasterPlanStateDB extends MasterPlanRowDB {
  stages: Array<{
    stage_id: string;
    title: string | null;
    status: StageRowDB["status"];
    counts: Record<TaskRowDB["status"], number>;
  }>;
}

// ---------------------------------------------------------------------------
// Error helpers.
// ---------------------------------------------------------------------------

export class IaDbUnavailableError extends Error {
  code = "ia_db_unavailable";
  constructor() {
    super(
      "IA database pool is not configured — set DATABASE_URL or config/postgres-dev.json.",
    );
  }
}

function poolOrThrow(): pg.Pool {
  const pool = getIaDatabasePool();
  if (!pool) throw new IaDbUnavailableError();
  return pool;
}

// ---------------------------------------------------------------------------
// Core queries.
// ---------------------------------------------------------------------------

const ZERO_COUNTS: Record<TaskRowDB["status"], number> = {
  pending: 0,
  implemented: 0,
  verified: 0,
  done: 0,
  archived: 0,
};

function blankCounts(): Record<TaskRowDB["status"], number> {
  return { ...ZERO_COUNTS };
}

/** Fetch a single task row + deps + commits + cited-id status map. */
export async function queryTaskState(
  task_id: string,
): Promise<TaskStateDB | null> {
  const pool = poolOrThrow();
  const res = await pool.query<TaskRowDB>(
    `SELECT task_id, prefix, slug, stage_id, title, status, priority, type,
            notes, body, created_at, updated_at, completed_at, archived_at
       FROM ia_tasks
      WHERE task_id = $1`,
    [task_id],
  );
  if (res.rowCount === 0) return null;
  const row = res.rows[0]!;

  const depsRes = await pool.query<{ depends_on_id: string; kind: string }>(
    `SELECT depends_on_id, kind
       FROM ia_task_deps
      WHERE task_id = $1
      ORDER BY kind, depends_on_id`,
    [task_id],
  );
  const deps: TaskDepsDB = { depends_on: [], related: [] };
  for (const d of depsRes.rows) {
    if (d.kind === "depends_on") deps.depends_on.push(d.depends_on_id);
    else if (d.kind === "related") deps.related.push(d.depends_on_id);
  }

  const citedIds = [...deps.depends_on, ...deps.related];
  const depStatus: Record<string, "pending" | "archived" | "unknown"> = {};
  if (citedIds.length > 0) {
    const cr = await pool.query<{ task_id: string; status: string }>(
      `SELECT task_id, status::text as status
         FROM ia_tasks
        WHERE task_id = ANY($1::text[])`,
      [citedIds],
    );
    const found = new Map(cr.rows.map((r) => [r.task_id, r.status]));
    for (const id of citedIds) {
      const s = found.get(id);
      if (!s) depStatus[id] = "unknown";
      else if (s === "archived") depStatus[id] = "archived";
      else depStatus[id] = "pending";
    }
  }

  const commitRes = await pool.query<TaskCommitDB>(
    `SELECT commit_sha, commit_kind, message, recorded_at
       FROM ia_task_commits
      WHERE task_id = $1
      ORDER BY recorded_at DESC`,
    [task_id],
  );

  return {
    ...row,
    deps,
    depends_on_status: depStatus,
    commits: commitRes.rows,
  };
}

/** Fetch a stage + its tasks + counts + latest verification verdict. */
export async function queryStageState(
  slug: string,
  stage_id: string,
): Promise<StageStateDB | null> {
  const pool = poolOrThrow();
  const stageRes = await pool.query<StageRowDB>(
    `SELECT slug, stage_id, title, objective, exit_criteria, status,
            source_file_path, created_at, updated_at
       FROM ia_stages
      WHERE slug = $1 AND stage_id = $2`,
    [slug, stage_id],
  );
  if (stageRes.rowCount === 0) return null;
  const stage = stageRes.rows[0]!;

  const tasksRes = await pool.query<StageTaskSummary>(
    `SELECT task_id, title, status, priority
       FROM ia_tasks
      WHERE slug = $1 AND stage_id = $2
      ORDER BY task_id`,
    [slug, stage_id],
  );

  const counts = blankCounts();
  let next_pending: string | null = null;
  for (const t of tasksRes.rows) {
    counts[t.status] = (counts[t.status] ?? 0) + 1;
    if (!next_pending && t.status === "pending") next_pending = t.task_id;
  }

  const verRes = await pool.query<{
    verdict: "pass" | "fail" | "partial";
    verified_at: string;
    commit_sha: string | null;
  }>(
    `SELECT verdict, verified_at, commit_sha
       FROM ia_stage_verifications
      WHERE slug = $1 AND stage_id = $2
      ORDER BY verified_at DESC
      LIMIT 1`,
    [slug, stage_id],
  );

  return {
    ...stage,
    tasks: tasksRes.rows,
    counts,
    next_pending,
    latest_verdict: verRes.rowCount! > 0 ? verRes.rows[0]! : null,
  };
}

/** Fetch a master plan + rollup across its stages. */
export async function queryMasterPlanState(
  slug: string,
): Promise<MasterPlanStateDB | null> {
  const pool = poolOrThrow();
  const planRes = await pool.query<MasterPlanRowDB>(
    `SELECT slug, title, source_spec_path, created_at, updated_at
       FROM ia_master_plans
      WHERE slug = $1`,
    [slug],
  );
  if (planRes.rowCount === 0) return null;

  const stagesRes = await pool.query<{
    stage_id: string;
    title: string | null;
    status: StageRowDB["status"];
  }>(
    `SELECT stage_id, title, status
       FROM ia_stages
      WHERE slug = $1
      ORDER BY stage_id`,
    [slug],
  );

  const countsRes = await pool.query<{
    stage_id: string;
    status: TaskRowDB["status"];
    n: string;
  }>(
    `SELECT stage_id, status::text as status, count(*)::text as n
       FROM ia_tasks
      WHERE slug = $1 AND stage_id IS NOT NULL
      GROUP BY stage_id, status`,
    [slug],
  );
  const countsByStage = new Map<string, Record<TaskRowDB["status"], number>>();
  for (const r of countsRes.rows) {
    let bucket = countsByStage.get(r.stage_id);
    if (!bucket) {
      bucket = blankCounts();
      countsByStage.set(r.stage_id, bucket);
    }
    bucket[r.status] = parseInt(r.n, 10);
  }

  return {
    ...planRes.rows[0]!,
    stages: stagesRes.rows.map((s) => ({
      stage_id: s.stage_id,
      title: s.title,
      status: s.status,
      counts: countsByStage.get(s.stage_id) ?? blankCounts(),
    })),
  };
}

/** Fetch full body text of a task. Returns null if task not found. */
export async function queryTaskBody(task_id: string): Promise<string | null> {
  const pool = poolOrThrow();
  const res = await pool.query<{ body: string }>(
    `SELECT body FROM ia_tasks WHERE task_id = $1`,
    [task_id],
  );
  if (res.rowCount === 0) return null;
  return res.rows[0]!.body;
}

/**
 * Extract one section of the task body by heading name.
 *
 * Strategy: body is markdown; header levels `##` / `###` count. `section`
 * param matches against the heading text (case-insensitive, trimmed). Returns
 * the lines from the heading through the line before the next heading of the
 * same or shallower level. Null when task or section missing.
 */
export async function queryTaskSection(
  task_id: string,
  section: string,
): Promise<{ heading: string; level: number; content: string } | null> {
  const body = await queryTaskBody(task_id);
  if (body === null) return null;
  return sliceSection(body, section);
}

/**
 * Pure markdown section slicer — exported for unit testing.
 * Returns the heading line through the line before the next heading of
 * the same-or-shallower level. Case-insensitive heading match.
 */
export function sliceSection(
  body: string,
  section: string,
): { heading: string; level: number; content: string } | null {
  const needle = section.trim().toLowerCase();
  if (!needle) return null;
  const lines = body.split(/\r?\n/);
  let start = -1;
  let startLevel = 0;
  let startHeading = "";
  for (let i = 0; i < lines.length; i++) {
    const m = lines[i]!.match(/^(#{1,6})\s+(.+?)\s*$/);
    if (!m) continue;
    if (m[2]!.trim().toLowerCase() === needle) {
      start = i;
      startLevel = m[1]!.length;
      startHeading = m[2]!.trim();
      break;
    }
  }
  if (start < 0) return null;
  let end = lines.length;
  for (let i = start + 1; i < lines.length; i++) {
    const m = lines[i]!.match(/^(#{1,6})\s+.+$/);
    if (!m) continue;
    if (m[1]!.length <= startLevel) {
      end = i;
      break;
    }
  }
  return {
    heading: startHeading,
    level: startLevel,
    content: lines.slice(start, end).join("\n"),
  };
}

export interface TaskSearchHit {
  task_id: string;
  title: string;
  status: TaskRowDB["status"];
  rank: number;
  snippet: string;
}

/** Full-text + trigram search over ia_tasks.body. */
export async function queryTaskSpecSearch(
  query: string,
  opts: { kind?: "fts" | "trgm"; limit?: number; status?: string } = {},
): Promise<TaskSearchHit[]> {
  const pool = poolOrThrow();
  const limit = Math.max(1, Math.min(200, opts.limit ?? 20));
  const kind = opts.kind ?? "fts";
  const q = query.trim();
  if (!q) return [];

  if (kind === "fts") {
    // Use plainto_tsquery for forgiving input (no tsquery syntax).
    const res = await pool.query<TaskSearchHit>(
      `SELECT task_id,
              title,
              status,
              ts_rank(body_tsv, plainto_tsquery('english', $1))::float as rank,
              ts_headline('english', body,
                          plainto_tsquery('english', $1),
                          'MaxWords=30, MinWords=10, ShortWord=3, MaxFragments=1'
              ) as snippet
         FROM ia_tasks
        WHERE body_tsv @@ plainto_tsquery('english', $1)
          ${opts.status ? "AND status = $3::task_status" : ""}
        ORDER BY rank DESC
        LIMIT $2`,
      opts.status ? [q, limit, opts.status] : [q, limit],
    );
    return res.rows;
  }

  // Trigram similarity — fuzzy over title (body trgm is near-zero at
  // default threshold; title is the useful fuzzy surface for typos). Lower
  // the similarity threshold via SET LOCAL scoped to this transaction.
  const client = await pool.connect();
  try {
    await client.query("BEGIN");
    await client.query("SET LOCAL pg_trgm.similarity_threshold = 0.1");
    const res = await client.query<TaskSearchHit>(
      `SELECT task_id,
              title,
              status,
              similarity(title, $1)::float as rank,
              substring(body from 1 for 200) as snippet
         FROM ia_tasks
        WHERE title % $1
          ${opts.status ? "AND status = $3::task_status" : ""}
        ORDER BY rank DESC
        LIMIT $2`,
      opts.status ? [q, limit, opts.status] : [q, limit],
    );
    await client.query("COMMIT");
    return res.rows;
  } catch (e) {
    await client.query("ROLLBACK").catch(() => {});
    throw e;
  } finally {
    client.release();
  }
}

// ---------------------------------------------------------------------------
// Bundles — convenience aggregates for agents.
// ---------------------------------------------------------------------------

export interface StageBundleDB extends StageStateDB {
  master_plan_title: string | null;
}

export async function queryStageBundle(
  slug: string,
  stage_id: string,
): Promise<StageBundleDB | null> {
  const stage = await queryStageState(slug, stage_id);
  if (!stage) return null;
  const pool = poolOrThrow();
  const planRes = await pool.query<{ title: string }>(
    `SELECT title FROM ia_master_plans WHERE slug = $1`,
    [slug],
  );
  return {
    ...stage,
    master_plan_title: planRes.rowCount! > 0 ? planRes.rows[0]!.title : null,
  };
}

// ---------------------------------------------------------------------------
// Master-plan + stage RENDER surfaces (Step 9.6.8 — Option A DB pivot).
//
// Reconstruct the verbatim markdown that used to live under
// `ia/projects/{slug}/{index.md, stage-X.Y-*.md}` from structured columns
// (preamble + objective + exit_criteria + tasks) and the change log table.
// ---------------------------------------------------------------------------

export interface StageRenderRowDB {
  slug: string;
  stage_id: string;
  title: string | null;
  status: StageRowDB["status"];
  objective: string | null;
  exit_criteria: string | null;
  tasks: StageTaskSummary[];
  rendered: string;
}

export interface MasterPlanChangeLogRow {
  entry_id: number;
  ts: string;
  kind: string;
  body: string;
  actor: string | null;
  commit_sha: string | null;
}

export interface MasterPlanRenderDB {
  slug: string;
  title: string;
  preamble: string | null;
  stages: StageRenderRowDB[];
  change_log?: MasterPlanChangeLogRow[];
}

/** Render a single stage block as markdown — heading + status + objective + exit + tasks table. */
export function renderStageBlock(stage: Omit<StageRenderRowDB, "rendered">): string {
  const lines: string[] = [];
  const titlePart = stage.title ? ` — ${stage.title}` : "";
  lines.push(`### Stage ${stage.stage_id}${titlePart}`);
  lines.push("");
  lines.push(`**Status:** ${stage.status}`);
  if (stage.objective && stage.objective.trim()) {
    lines.push("");
    lines.push(`**Objectives:** ${stage.objective.trim()}`);
  }
  if (stage.exit_criteria && stage.exit_criteria.trim()) {
    lines.push("");
    lines.push(`**Exit:**`);
    lines.push(stage.exit_criteria.replace(/\n+$/, ""));
  }
  if (stage.tasks.length > 0) {
    lines.push("");
    lines.push(`**Tasks:**`);
    lines.push("");
    lines.push(`| ID | Title | Status |`);
    lines.push(`| --- | --- | --- |`);
    for (const t of stage.tasks) {
      lines.push(`| ${t.task_id} | ${escapeTableCell(t.title)} | ${t.status} |`);
    }
  }
  return lines.join("\n");
}

function escapeTableCell(s: string): string {
  return s.replace(/\|/g, "\\|").replace(/\n/g, " ");
}

/** Fetch one stage rendered as markdown + structured fields. */
export async function queryStageRender(
  slug: string,
  stage_id: string,
): Promise<StageRenderRowDB | null> {
  const pool = poolOrThrow();
  const sr = await pool.query<{
    title: string | null;
    objective: string | null;
    exit_criteria: string | null;
    status: StageRowDB["status"];
  }>(
    `SELECT title, objective, exit_criteria, status
       FROM ia_stages
      WHERE slug = $1 AND stage_id = $2`,
    [slug, stage_id],
  );
  if (sr.rowCount === 0) return null;
  const stage = sr.rows[0]!;
  const tr = await pool.query<StageTaskSummary>(
    `SELECT task_id, title, status, priority
       FROM ia_tasks
      WHERE slug = $1 AND stage_id = $2
      ORDER BY task_id`,
    [slug, stage_id],
  );
  const base = {
    slug,
    stage_id,
    title: stage.title,
    status: stage.status,
    objective: stage.objective,
    exit_criteria: stage.exit_criteria,
    tasks: tr.rows,
  };
  return { ...base, rendered: renderStageBlock(base) };
}

/** Fetch master plan preamble + ALL stages rendered. Optional last-N change log. */
export async function queryMasterPlanRender(
  slug: string,
  opts: { include_change_log?: boolean; change_log_limit?: number } = {},
): Promise<MasterPlanRenderDB | null> {
  const pool = poolOrThrow();
  const pr = await pool.query<{ title: string; preamble: string | null }>(
    `SELECT title, preamble FROM ia_master_plans WHERE slug = $1`,
    [slug],
  );
  if (pr.rowCount === 0) return null;
  const plan = pr.rows[0]!;

  const sr = await pool.query<{
    stage_id: string;
    title: string | null;
    status: StageRowDB["status"];
    objective: string | null;
    exit_criteria: string | null;
  }>(
    `SELECT stage_id, title, status, objective, exit_criteria
       FROM ia_stages
      WHERE slug = $1
      ORDER BY stage_id`,
    [slug],
  );
  const tr = await pool.query<{
    stage_id: string;
    task_id: string;
    title: string;
    status: TaskRowDB["status"];
    priority: string | null;
  }>(
    `SELECT stage_id, task_id, title, status, priority
       FROM ia_tasks
      WHERE slug = $1 AND stage_id IS NOT NULL
      ORDER BY stage_id, task_id`,
    [slug],
  );
  const tasksByStage = new Map<string, StageTaskSummary[]>();
  for (const t of tr.rows) {
    let bucket = tasksByStage.get(t.stage_id);
    if (!bucket) {
      bucket = [];
      tasksByStage.set(t.stage_id, bucket);
    }
    bucket.push({ task_id: t.task_id, title: t.title, status: t.status, priority: t.priority });
  }
  const stages: StageRenderRowDB[] = sr.rows.map((s) => {
    const base = {
      slug,
      stage_id: s.stage_id,
      title: s.title,
      status: s.status,
      objective: s.objective,
      exit_criteria: s.exit_criteria,
      tasks: tasksByStage.get(s.stage_id) ?? [],
    };
    return { ...base, rendered: renderStageBlock(base) };
  });

  let change_log: MasterPlanChangeLogRow[] | undefined;
  if (opts.include_change_log) {
    change_log = await queryMasterPlanChangeLog(slug, opts.change_log_limit ?? 50);
  }

  return {
    slug,
    title: plan.title,
    preamble: plan.preamble,
    stages,
    ...(change_log ? { change_log } : {}),
  };
}

/** Fetch latest N change log entries for a master plan (DESC by ts). */
export async function queryMasterPlanChangeLog(
  slug: string,
  limit: number,
): Promise<MasterPlanChangeLogRow[]> {
  const pool = poolOrThrow();
  const lim = Math.max(1, Math.min(500, Math.floor(limit)));
  const res = await pool.query<{
    entry_id: string;
    ts: string;
    kind: string;
    body: string;
    actor: string | null;
    commit_sha: string | null;
  }>(
    `SELECT entry_id::text AS entry_id, ts, kind, body, actor, commit_sha
       FROM ia_master_plan_change_log
      WHERE slug = $1
      ORDER BY ts DESC
      LIMIT $2`,
    [slug, lim],
  );
  return res.rows.map((r) => ({
    entry_id: parseInt(r.entry_id, 10),
    ts: r.ts,
    kind: r.kind,
    body: r.body,
    actor: r.actor,
    commit_sha: r.commit_sha,
  }));
}

export interface TaskBundleDB extends TaskStateDB {
  stage: StageRowDB | null;
  master_plan_title: string | null;
}

export async function queryTaskBundle(
  task_id: string,
): Promise<TaskBundleDB | null> {
  const task = await queryTaskState(task_id);
  if (!task) return null;

  let stage: StageRowDB | null = null;
  let master_plan_title: string | null = null;
  if (task.slug && task.stage_id) {
    const pool = poolOrThrow();
    const sr = await pool.query<StageRowDB>(
      `SELECT slug, stage_id, title, objective, exit_criteria, status,
              source_file_path, created_at, updated_at
         FROM ia_stages
        WHERE slug = $1 AND stage_id = $2`,
      [task.slug, task.stage_id],
    );
    if (sr.rowCount! > 0) stage = sr.rows[0]!;

    const pr = await pool.query<{ title: string }>(
      `SELECT title FROM ia_master_plans WHERE slug = $1`,
      [task.slug],
    );
    if (pr.rowCount! > 0) master_plan_title = pr.rows[0]!.title;
  }

  return { ...task, stage, master_plan_title };
}
