/**
 * audit-log-cron-handler — processes one cron_audit_log_jobs row.
 *
 * Calls INSERT INTO ia_master_plan_change_log mirroring
 * the master_plan_change_log_append MCP tool shape.
 */

import { getCronDbPool } from "../lib/index.js";

export interface AuditLogJobRow {
  job_id: string;
  slug: string;
  version: number;
  audit_kind: string;
  body: unknown;
  actor?: string | null;
  commit_sha?: string | null;
  stage_id?: string | null;
}

export async function run(row: AuditLogJobRow): Promise<void> {
  const pool = getCronDbPool();

  // Mirror the dedup logic from mutateChangLogAppend:
  // UNIQUE (slug, stage_id, kind, commit_sha) → ON CONFLICT DO NOTHING.
  await pool.query(
    `INSERT INTO ia_master_plan_change_log
       (slug, kind, body, actor, commit_sha, stage_id)
     VALUES ($1, $2, $3, $4, $5, $6)
     ON CONFLICT (slug, stage_id, kind, commit_sha) DO NOTHING`,
    [
      row.slug,
      row.audit_kind,
      typeof row.body === "string" ? row.body : JSON.stringify(row.body, null, 2),
      row.actor ?? null,
      row.commit_sha ?? null,
      row.stage_id ?? null,
    ],
  );
}
