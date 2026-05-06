/**
 * journal-append-cron-handler — processes one cron_journal_append_jobs row.
 *
 * Calls INSERT INTO ia_ship_stage_journal mirroring the journal_append MCP tool.
 * session_id stored as uuid in queue but ia_ship_stage_journal.session_id is text.
 */

import { getCronDbPool } from "../lib/index.js";

export interface JournalAppendJobRow {
  job_id: string;
  session_id?: string | null;
  slug?: string | null;
  stage_id?: string | null;
  phase?: string | null;
  payload_kind?: string | null;
  payload?: unknown;
  task_id?: string | null;
}

export async function run(row: JournalAppendJobRow): Promise<void> {
  const pool = getCronDbPool();

  const session_id = row.session_id ?? "unknown";
  const phase = row.phase ?? "unknown";
  const payload_kind = row.payload_kind ?? "unknown";
  const payload =
    row.payload && typeof row.payload === "object"
      ? row.payload
      : {};

  // task_id has FK constraint — only pass through if it looks like a valid TECH/FEAT/BUG id.
  // Set NULL when absent; the FK will reject unknown ids at write time (let markFailed handle it).
  await pool.query(
    `INSERT INTO ia_ship_stage_journal
       (session_id, task_id, slug, stage_id, phase, payload_kind, payload)
     VALUES ($1, $2, $3, $4, $5, $6, $7::jsonb)`,
    [
      session_id,
      row.task_id ?? null,
      row.slug ?? null,
      row.stage_id ?? null,
      phase,
      payload_kind,
      JSON.stringify(payload),
    ],
  );
}
