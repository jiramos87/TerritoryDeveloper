/**
 * Journal-domain mutations: journal_append, fix_plan_write,
 * fix_plan_consume.
 */

import { IaDbValidationError, poolOrThrow, withTx } from "./shared.js";

// ---------------------------------------------------------------------------
// journal_append
// ---------------------------------------------------------------------------

export interface JournalAppendInput {
  session_id: string;
  task_id?: string | null;
  slug?: string | null;
  stage_id?: string | null;
  phase: string;
  payload_kind: string;
  payload: Record<string, unknown>;
}

export async function mutateJournalAppend(
  input: JournalAppendInput,
): Promise<{ id: number; recorded_at: string }> {
  const session_id = (input.session_id ?? "").trim();
  if (!session_id) throw new IaDbValidationError("session_id is required");
  const phase = (input.phase ?? "").trim();
  if (!phase) throw new IaDbValidationError("phase is required");
  const payload_kind = (input.payload_kind ?? "").trim();
  if (!payload_kind) throw new IaDbValidationError("payload_kind is required");
  if (input.payload === null || typeof input.payload !== "object") {
    throw new IaDbValidationError("payload must be an object");
  }

  const pool = poolOrThrow();
  const res = await pool.query<{ id: string; recorded_at: string }>(
    `INSERT INTO ia_ship_stage_journal
       (session_id, task_id, slug, stage_id, phase, payload_kind, payload)
     VALUES ($1, $2, $3, $4, $5, $6, $7::jsonb)
     RETURNING id::text AS id, recorded_at`,
    [
      session_id,
      input.task_id ?? null,
      input.slug ?? null,
      input.stage_id ?? null,
      phase,
      payload_kind,
      JSON.stringify(input.payload),
    ],
  );
  return {
    id: parseInt(res.rows[0]!.id, 10),
    recorded_at: res.rows[0]!.recorded_at,
  };
}

// ---------------------------------------------------------------------------
// fix_plan_write / fix_plan_consume
// ---------------------------------------------------------------------------

export async function mutateFixPlanWrite(
  task_id: string,
  round: number,
  tuples: Array<Record<string, unknown>>,
): Promise<{ task_id: string; round: number; written: number }> {
  if (!Number.isInteger(round) || round < 0) {
    throw new IaDbValidationError("round must be a non-negative integer");
  }
  if (!Array.isArray(tuples) || tuples.length === 0) {
    throw new IaDbValidationError("tuples must be a non-empty array");
  }
  return withTx(async (c) => {
    const tr = await c.query(
      `SELECT 1 FROM ia_tasks WHERE task_id = $1`,
      [task_id],
    );
    if (tr.rowCount === 0) {
      throw new IaDbValidationError(`task not found: ${task_id}`);
    }

    await c.query(
      `DELETE FROM ia_fix_plan_tuples
        WHERE task_id = $1 AND round = $2 AND applied_at IS NULL`,
      [task_id, round],
    );

    for (let i = 0; i < tuples.length; i++) {
      await c.query(
        `INSERT INTO ia_fix_plan_tuples (task_id, round, tuple_index, tuple)
           VALUES ($1, $2, $3, $4::jsonb)`,
        [task_id, round, i, JSON.stringify(tuples[i])],
      );
    }
    return { task_id, round, written: tuples.length };
  });
}

export async function mutateFixPlanConsume(
  task_id: string,
  round: number,
): Promise<{ task_id: string; round: number; consumed: number }> {
  if (!Number.isInteger(round) || round < 0) {
    throw new IaDbValidationError("round must be a non-negative integer");
  }
  return withTx(async (c) => {
    const res = await c.query(
      `UPDATE ia_fix_plan_tuples
          SET applied_at = now()
        WHERE task_id = $1 AND round = $2 AND applied_at IS NULL`,
      [task_id, round],
    );
    return { task_id, round, consumed: res.rowCount ?? 0 };
  });
}
