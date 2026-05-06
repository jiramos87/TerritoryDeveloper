/**
 * task-commit-record-cron-handler — processes one cron_task_commit_record_jobs row.
 *
 * Calls INSERT INTO ia_task_commits mirroring the task_commit_record MCP tool shape.
 * UNIQUE(task_id, commit_sha) — re-record updates commit_kind + message.
 */

import { getCronDbPool } from "../lib/index.js";

export interface TaskCommitRecordJobRow {
  job_id: string;
  task_id: string;
  commit_sha: string;
  commit_kind: string;
  message?: string | null;
  slug?: string | null;
  stage_id?: string | null;
}

export async function run(row: TaskCommitRecordJobRow): Promise<void> {
  const pool = getCronDbPool();

  // Mirror the upsert logic from mutateTaskCommitRecord:
  // UNIQUE(task_id, commit_sha) → ON CONFLICT DO UPDATE.
  await pool.query(
    `INSERT INTO ia_task_commits (task_id, commit_sha, commit_kind, message)
     VALUES ($1, $2, $3, $4)
     ON CONFLICT (task_id, commit_sha) DO UPDATE
       SET commit_kind = EXCLUDED.commit_kind,
           message     = EXCLUDED.message`,
    [
      row.task_id,
      row.commit_sha,
      row.commit_kind,
      row.message ?? null,
    ],
  );
}
