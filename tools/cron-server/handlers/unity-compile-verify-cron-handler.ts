/**
 * unity-compile-verify-cron-handler — processes one cron_unity_compile_verify_jobs row.
 *
 * Replaces the synchronous 60s compile-poll block in ship-cycle Phase 8 step 0a.
 * Enqueues an agent_bridge_job (kind=get_compilation_status), polls completion,
 * writes verdict back to the row + appends an ia_stage_verifications history row.
 *
 * Lifecycle skills refactor — Phase 3 / weak-spot #9.
 */

import { randomUUID } from "node:crypto";
import { getCronDbPool } from "../lib/index.js";

export interface UnityCompileVerifyJobRow {
  job_id: string;
  slug: string;
  stage_id: string;
  commit_sha?: string | null;
  bridge_lease_id?: string | null;
}

interface CompilationStatus {
  compiling?: boolean;
  compilation_failed?: boolean;
  last_error_excerpt?: string;
  recent_error_messages?: string[];
}

const POLL_INTERVAL_MS = 2_000;
const POLL_TIMEOUT_MS = 120_000;

function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

export async function run(row: UnityCompileVerifyJobRow): Promise<void> {
  const pool = getCronDbPool();
  const commandId = randomUUID();

  // Enqueue bridge job — Unity Editor's agent-bridge dequeue loop picks it up
  // and writes response on completion. Same path the unity_compile MCP uses.
  await pool.query(
    `INSERT INTO agent_bridge_job (command_id, kind, status, request)
     VALUES ($1::uuid, 'get_compilation_status', 'pending', $2::jsonb)`,
    [commandId, JSON.stringify({ params: {} })],
  );

  // Poll until completed/failed/timeout.
  const deadline = Date.now() + POLL_TIMEOUT_MS;
  let status: string = "pending";
  let response: CompilationStatus | null = null;
  let bridgeError: string | null = null;

  while (Date.now() < deadline) {
    const res = await pool.query<{
      status: string;
      response: unknown;
      error: string | null;
    }>(
      `SELECT status, response, error FROM agent_bridge_job WHERE command_id = $1::uuid`,
      [commandId],
    );
    if (res.rowCount === 0) break;
    status = res.rows[0]!.status;
    if (status === "completed") {
      const r = res.rows[0]!.response as { compilation_status?: CompilationStatus } | null;
      response = r?.compilation_status ?? null;
      break;
    }
    if (status === "failed") {
      bridgeError = res.rows[0]!.error;
      break;
    }
    await sleep(POLL_INTERVAL_MS);
  }

  // Determine verdict.
  let verdict: "pass" | "fail";
  let lastErrorExcerpt: string | null = null;

  if (status === "completed" && response) {
    if (response.compilation_failed) {
      verdict = "fail";
      lastErrorExcerpt = response.last_error_excerpt ?? null;
    } else if (response.compiling) {
      // Still compiling at deadline — treat as fail (timeout).
      verdict = "fail";
      lastErrorExcerpt = "compile still in progress at poll deadline";
    } else {
      verdict = "pass";
    }
  } else if (status === "failed") {
    verdict = "fail";
    lastErrorExcerpt = bridgeError ?? "bridge job failed";
  } else {
    // timeout — bridge never returned
    verdict = "fail";
    lastErrorExcerpt = `bridge timeout after ${POLL_TIMEOUT_MS}ms (status=${status})`;
    // Best-effort cleanup of orphaned pending row.
    await pool.query(
      `DELETE FROM agent_bridge_job WHERE command_id = $1::uuid AND status = 'pending'`,
      [commandId],
    );
  }

  // Write verdict + diagnostics back to the cron job row.
  await pool.query(
    `UPDATE cron_unity_compile_verify_jobs
        SET verdict_out = $1,
            last_error_excerpt = $2
      WHERE job_id = $3`,
    [verdict, lastErrorExcerpt, row.job_id],
  );

  // Append history row to ia_stage_verifications — same pattern as
  // stage-verification-flip-cron-handler.ts.
  let resolvedStageId = row.stage_id;
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

  await pool.query(
    `INSERT INTO ia_stage_verifications
       (slug, stage_id, verdict, commit_sha, notes, actor)
     VALUES ($1, $2, $3::stage_verdict, $4, $5, $6)`,
    [
      row.slug,
      resolvedStageId,
      verdict,
      row.commit_sha ?? null,
      lastErrorExcerpt,
      "cron:unity-compile-verify",
    ],
  );

  // If fail, surface error to claimBatch so the row goes to status=failed.
  if (verdict === "fail") {
    throw new Error(
      `unity-compile-verify failed (slug=${row.slug} stage=${row.stage_id}): ${lastErrorExcerpt ?? "unknown"}`,
    );
  }
}
