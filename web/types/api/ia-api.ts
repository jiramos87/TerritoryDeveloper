/**
 * Read-only IA dashboard API surface (Step 10 — DB-primary refactor).
 *
 * @see `docs/ia-dev-db-refactor-implementation.md` Step 10
 * @see `db/migrations/0015_ia_tasks_core.sql` + `0019_ia_master_plan_preamble_change_log.sql`
 */

export type IaErrorCode = "bad_request" | "not_found" | "internal";

export type IaErrorBody = {
  error: string;
  code: IaErrorCode;
  details?: unknown;
};

export type MasterPlanRow = {
  slug: string;
  title: string;
  source_spec_path: string | null;
  created_at: string;
  updated_at: string;
  stage_count: number;
  task_count: number;
  task_done_count: number;
};

export type StageRow = {
  slug: string;
  stage_id: string;
  title: string | null;
  objective: string | null;
  exit_criteria: string | null;
  status: "pending" | "in_progress" | "done";
  source_file_path: string | null;
  created_at: string;
  updated_at: string;
  task_count: number;
  task_done_count: number;
};

export type TaskRow = {
  task_id: string;
  prefix: "TECH" | "FEAT" | "BUG" | "ART" | "AUDIO";
  slug: string | null;
  stage_id: string | null;
  title: string;
  status: "pending" | "implemented" | "verified" | "done" | "archived";
  priority: string | null;
  type: string | null;
  notes: string | null;
  created_at: string;
  updated_at: string;
  completed_at: string | null;
  archived_at: string | null;
};

export type PlanDetail = {
  plan: MasterPlanRow;
  preamble: string | null;
  stages: (StageRow & { tasks: TaskRow[] })[];
  change_log: ChangeLogRow[];
};

export type ChangeLogRow = {
  entry_id: number;
  slug: string;
  ts: string;
  kind: string;
  body: string;
  actor: string | null;
  commit_sha: string | null;
};

export type TaskDetail = {
  task: TaskRow;
  deps: { task_id: string; depends_on_id: string; kind: "depends_on" | "related" }[];
  commits: { id: number; commit_sha: string; commit_kind: string; message: string | null; recorded_at: string }[];
  history_count: number;
};

export type TaskBodyResponse = {
  task_id: string;
  body: string;
};

export type SearchHit = {
  task_id: string;
  prefix: string;
  slug: string | null;
  stage_id: string | null;
  title: string;
  status: string;
  rank: number;
  snippet: string;
};
