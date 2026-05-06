/**
 * stage-verification-flip-cron-handler — processes one cron_stage_verification_flip_jobs row.
 *
 * Calls INSERT INTO ia_stage_verifications mirroring the stage_verification_flip MCP tool.
 * History-preserving append: no upsert — each row is an independent verification event.
 */

import { getCronDbPool } from "../lib/index.js";

export interface StageVerificationFlipJobRow {
  job_id: string;
  slug?: string | null;
  stage_id?: string | null;
  verdict?: string | null;
  commit_sha?: string | null;
  actor?: string | null;
  notes?: string | null;
}

export async function run(row: StageVerificationFlipJobRow): Promise<void> {
  const pool = getCronDbPool();

  // Mirror the INSERT shape from mutateStageVerificationFlip.
  // History-preserving — every cron row becomes a distinct ia_stage_verifications row.
  await pool.query(
    `INSERT INTO ia_stage_verifications
       (slug, stage_id, verdict, commit_sha, notes, actor)
     VALUES ($1, $2, $3::stage_verdict, $4, $5, $6)`,
    [
      row.slug ?? null,
      row.stage_id ?? null,
      row.verdict ?? "pass",
      row.commit_sha ?? null,
      row.notes ?? null,
      row.actor ?? null,
    ],
  );
}
