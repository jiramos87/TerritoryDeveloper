/**
 * dashboard-data.ts — DB-backed loader for the /dashboard RSC.
 *
 * Replaces the post Step-9.6 broken filesystem path (`ia/projects/*master-plan*.md`
 * was deleted; DB is now sole source of truth).
 *
 * Three batched queries (master plans, stages, tasks) + JS stitch keep the wire
 * shape identical to the legacy parser output (`PlanData[]`) so existing
 * dashboard chrome (Bezel / HeatCell / Sparkline / StatBar / CollapsiblePlanStage)
 * renders unchanged.
 *
 * @see `docs/web-dashboard-db-read-rewire.md`
 */

import { getSql } from "@/lib/db/client";
import { cleanPlanTitle, deriveHierarchyStatus } from "@/lib/plan-parser";
import type {
  HierarchyStatus,
  PlanData,
  Stage,
  TaskRow,
  TaskStatus,
} from "@/lib/plan-loader-types";

/**
 * Natural-sort comparator for stage_id values like "1", "10", "3.1", "7 addendum".
 * Splits on dot, compares numeric chunks numerically, falls back to string compare
 * for trailing tags (e.g. " addendum"). Pure-numeric ids sort before mixed.
 */
function compareStageIds(a: string, b: string): number {
  const partsA = a.split(".");
  const partsB = b.split(".");
  const n = Math.max(partsA.length, partsB.length);
  for (let i = 0; i < n; i++) {
    const pa = partsA[i] ?? "";
    const pb = partsB[i] ?? "";
    const numA = parseInt(pa, 10);
    const numB = parseInt(pb, 10);
    const aIsNum = !isNaN(numA);
    const bIsNum = !isNaN(numB);
    if (aIsNum && bIsNum && numA !== numB) return numA - numB;
    if (aIsNum && bIsNum && numA === numB) {
      const tailA = pa.replace(/^\d+/, "");
      const tailB = pb.replace(/^\d+/, "");
      if (tailA !== tailB) return tailA.localeCompare(tailB);
      continue;
    }
    if (pa !== pb) return pa.localeCompare(pb);
  }
  return 0;
}

type DbTaskStatus = "pending" | "implemented" | "verified" | "done" | "archived";
type DbStageStatus = "pending" | "in_progress" | "done";

interface MasterPlanDbRow {
  slug: string;
  title: string;
  preamble: string | null;
  description: string | null;
}

interface StageDbRow {
  slug: string;
  stage_id: string;
  title: string | null;
  objective: string | null;
  status: DbStageStatus;
}

interface TaskDbRow {
  task_id: string;
  slug: string;
  stage_id: string | null;
  title: string;
  status: DbTaskStatus;
  body: string | null;
}

function mapTaskStatus(db: DbTaskStatus): TaskStatus {
  switch (db) {
    case "pending":
      return "_pending_";
    case "implemented":
    case "verified":
      return "In Progress";
    case "done":
      return "Done";
    case "archived":
      return "Done (archived)";
  }
}

function mapStageStatus(db: DbStageStatus): HierarchyStatus {
  switch (db) {
    case "pending":
      return "Draft";
    case "in_progress":
      return "In Progress";
    case "done":
      return "Final";
  }
}

function synthOverallStatus(
  tasks: TaskRow[],
  pendingDecomposeCount: number,
): string {
  if (tasks.length === 0 && pendingDecomposeCount === 0) return "Draft";
  const isDone = (s: string) => s === "Done" || s === "Done (archived)";
  const isActive = (s: string) => s === "In Progress" || s === "In Review";
  const allDone = tasks.length > 0 && tasks.every((t) => isDone(t.status));
  if (allDone) {
    return pendingDecomposeCount > 0 ? "In Progress" : "Final";
  }
  const anyActive = tasks.some((t) => isActive(t.status));
  const anyDone = tasks.some((t) => isDone(t.status));
  if (anyActive || anyDone || pendingDecomposeCount > 0) return "In Progress";
  return "Draft";
}

export async function loadDashboardData(): Promise<PlanData[]> {
  const sql = getSql();

  const planRows = await sql<MasterPlanDbRow[]>`
    SELECT slug, title, preamble, description
    FROM ia_master_plans
    ORDER BY slug
  `;

  if (planRows.length === 0) return [];

  const stageRows = await sql<StageDbRow[]>`
    SELECT slug, stage_id, title, objective, status
    FROM ia_stages
    ORDER BY slug, stage_id
  `;

  const taskRows = await sql<TaskDbRow[]>`
    SELECT task_id, slug, stage_id, title, status, body
    FROM ia_tasks
    WHERE slug IS NOT NULL
    ORDER BY slug, stage_id, task_id
  `;

  const stagesBySlug = new Map<string, StageDbRow[]>();
  for (const s of stageRows) {
    const arr = stagesBySlug.get(s.slug);
    if (arr) arr.push(s);
    else stagesBySlug.set(s.slug, [s]);
  }
  for (const arr of stagesBySlug.values()) {
    arr.sort((a, b) => compareStageIds(a.stage_id, b.stage_id));
  }

  const tasksByKey = new Map<string, TaskDbRow[]>();
  const key = (slug: string, stageId: string | null) => `${slug}::${stageId ?? ""}`;
  for (const t of taskRows) {
    const k = key(t.slug, t.stage_id);
    const arr = tasksByKey.get(k);
    if (arr) arr.push(t);
    else tasksByKey.set(k, [t]);
  }

  return planRows.map((p) => {
    const dbStages = stagesBySlug.get(p.slug) ?? [];

    const stages: Stage[] = dbStages.map((s) => {
      const dbTasks = tasksByKey.get(key(s.slug, s.stage_id)) ?? [];
      const tasks: TaskRow[] = dbTasks.map((t) => ({
        id: t.task_id,
        issue: t.task_id,
        status: mapTaskStatus(t.status),
        intent: t.title,
        body: t.body ?? "",
      }));
      return {
        id: s.stage_id,
        title: s.title ?? `Stage ${s.stage_id}`,
        status: mapStageStatus(s.status),
        statusDetail: "",
        objective: s.objective ?? undefined,
        tasks,
      };
    });

    deriveHierarchyStatus(stages);

    const allTasks = stages.flatMap((s) => s.tasks);
    const pendingDecomposeCount = stages.filter(
      (s) => s.tasks.length === 0 && s.status !== "Final",
    ).length;

    return {
      title: cleanPlanTitle(p.title),
      filename: `${p.slug}-master-plan.md`,
      overallStatus: synthOverallStatus(allTasks, pendingDecomposeCount),
      overallStatusDetail: "",
      siblingWarnings: [],
      stages,
      allTasks,
      preamble: p.preamble ?? "",
      description: p.description ?? "",
      pendingDecomposeCount,
    };
  });
}
