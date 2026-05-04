/** Parameterised SQL for red_stage_proof_* tools. */

import type pg from "pg";

export type ProofRow = {
  slug: string;
  stage_id: string;
  target_kind: string;
  anchor: string;
  proof_artifact_id: string;
  proof_status: string;
  green_status: string | null;
  captured_at: string;
};

export async function insertProof(
  pool: pg.Pool,
  params: {
    slug: string;
    stage_id: string;
    target_kind: string;
    anchor: string;
    proof_artifact_id: string;
    proof_status: string;
    command_kind: string;
  },
): Promise<{ captured_at: string; proof_artifact_id: string }> {
  const result = await pool.query<{ captured_at: string; proof_artifact_id: string }>(
    `INSERT INTO ia_red_stage_proofs
       (slug, stage_id, target_kind, anchor, proof_artifact_id, proof_status)
     VALUES ($1, $2, $3, $4, $5, $6)
     RETURNING captured_at, proof_artifact_id`,
    [
      params.slug,
      params.stage_id,
      params.target_kind,
      params.anchor,
      params.proof_artifact_id,
      params.proof_status,
    ],
  );
  return result.rows[0];
}

export async function getProofsByStage(
  pool: pg.Pool,
  slug: string,
  stage_id: string,
): Promise<ProofRow[]> {
  const result = await pool.query<ProofRow>(
    `SELECT slug, stage_id, target_kind, anchor, proof_artifact_id,
            proof_status, green_status, captured_at
     FROM ia_red_stage_proofs
     WHERE slug = $1 AND stage_id = $2
     ORDER BY captured_at ASC, anchor ASC`,
    [slug, stage_id],
  );
  return result.rows;
}

export async function listProofCountsBySlug(
  pool: pg.Pool,
  slug: string,
): Promise<Array<{ stage_id: string; proof_status: string; count: number }>> {
  const result = await pool.query<{ stage_id: string; proof_status: string; count: string }>(
    `SELECT stage_id, proof_status, COUNT(*) AS count
     FROM ia_red_stage_proofs
     WHERE slug = $1
     GROUP BY stage_id, proof_status
     ORDER BY stage_id ASC, proof_status ASC`,
    [slug],
  );
  return result.rows.map((r) => ({ ...r, count: Number(r.count) }));
}

/**
 * Finalize a proof row: set green_status.
 * Guard: reject green_status='passed' if proof_status='unexpected_pass'.
 * Returns: { kind: 'updated', green_status, finalized_at } | { kind: 'blocked' } | { kind: 'not_found' }
 */
export async function finalizeProof(
  pool: pg.Pool,
  slug: string,
  stage_id: string,
  anchor: string,
  green_status: "passed" | "failed",
): Promise<
  | { kind: "updated"; green_status: string; finalized_at: string }
  | { kind: "blocked" }
  | { kind: "not_found" }
> {
  // Single UPDATE with inline guard; RETURNING to detect row existence
  const result = await pool.query<{ green_status: string; finalized_at: string }>(
    `UPDATE ia_red_stage_proofs
     SET green_status = $4
     WHERE slug = $1 AND stage_id = $2 AND anchor = $3
       AND NOT ($4 = 'passed' AND proof_status = 'unexpected_pass')
     RETURNING green_status, NOW() AS finalized_at`,
    [slug, stage_id, anchor, green_status],
  );

  if (result.rowCount && result.rowCount > 0) {
    const row = result.rows[0];
    return { kind: "updated", green_status: row.green_status, finalized_at: row.finalized_at };
  }

  // Distinguish not_found from blocked
  const check = await pool.query<{ proof_status: string }>(
    `SELECT proof_status FROM ia_red_stage_proofs
     WHERE slug = $1 AND stage_id = $2 AND anchor = $3`,
    [slug, stage_id, anchor],
  );

  if (check.rowCount === 0) return { kind: "not_found" };
  return { kind: "blocked" };
}
