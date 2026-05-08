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

  // Resolve canonical stage_id form against ia_stages — producers (e.g. ship-cycle)
  // sometimes drop the trailing ".0" on dotted ids; the FK to ia_stages would then
  // fail. Try exact match, then ${stage_id}.0 fallback. If neither resolves, fall
  // through to INSERT which will surface the original FK error.
  let resolvedStageId = row.stage_id ?? null;
  if (row.slug && row.stage_id) {
    const resolved = await pool.query<{ stage_id: string }>(
      `SELECT stage_id FROM ia_stages
        WHERE slug = $1
          AND stage_id IN ($2, $2 || '.0')
        ORDER BY (stage_id = $2) DESC
        LIMIT 1`,
      [row.slug, row.stage_id],
    );
    if (resolved.rowCount && resolved.rows[0]) {
      resolvedStageId = resolved.rows[0].stage_id;
    }
  }

  // Mirror the INSERT shape from mutateStageVerificationFlip.
  // History-preserving — every cron row becomes a distinct ia_stage_verifications row.
  await pool.query(
    `INSERT INTO ia_stage_verifications
       (slug, stage_id, verdict, commit_sha, notes, actor)
     VALUES ($1, $2, $3::stage_verdict, $4, $5, $6)`,
    [
      row.slug ?? null,
      resolvedStageId,
      row.verdict ?? "pass",
      row.commit_sha ?? null,
      row.notes ?? null,
      row.actor ?? null,
    ],
  );
}
