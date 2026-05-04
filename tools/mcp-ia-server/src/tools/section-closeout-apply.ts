/**
 * MCP tool: section_closeout_apply — pure-DB closeout for a section.
 *
 * Verifies all section stages have status='done'. Appends a row to
 * `ia_master_plan_change_log` (kind='section_done'). Releases the active
 * section claim and cascades release of stage claims for the section.
 *
 * V2 row-only — no holder identity. Section claim addressed by
 * (slug, section_id) only. Any caller may apply closeout once stages are
 * all done. parallel-carcass §6.2 (D4 V2 / D9).
 *
 * **Does NOT run git** — same-branch same-worktree model. No worktree
 * teardown, no merge step. Caller skill (`/section-closeout`) is mechanical
 * DB-only.
 *
 * **Does NOT re-run drift scan** — caller MUST first verify zero open
 * `arch_drift_scan(scope='intra-plan')` events. This tool is the
 * post-verify atomic apply.
 *
 * Schema-cache restart required after add (N4).
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import { getIaDatabasePool } from "../ia-db/pool.js";

const inputShape = {
  slug: z.string().describe("Master-plan slug."),
  section_id: z.string().describe("Section identifier."),
  actor: z.string().optional().describe("Optional actor for change_log row."),
  commit_sha: z
    .string()
    .optional()
    .describe("Optional commit_sha for change_log row."),
};

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

type Args = {
  slug: string;
  section_id: string;
  actor?: string;
  commit_sha?: string;
};

export interface SectionCloseoutResult {
  slug: string;
  section_id: string;
  applied: boolean;
  stages_total: number;
  stages_done: number;
  change_log_entry_id: number | null;
  section_claim_released: boolean;
  cascaded_stage_releases: number;
  red_stage_coverage_pct: number | null;
  pending_count: number;
  unexpected_pass_count: number;
  error?: string;
}

export async function applySectionCloseout(
  args: Args,
): Promise<SectionCloseoutResult> {
  const pool = getIaDatabasePool();
  if (!pool) {
    throw { code: "db_unavailable", message: "ia_db pool not initialized" };
  }
  const { slug, section_id, actor, commit_sha } = args;

  const stages = await pool.query<{ stage_id: string; status: string }>(
    `SELECT stage_id, status FROM ia_stages
      WHERE slug = $1 AND section_id = $2
      ORDER BY stage_id`,
    [slug, section_id],
  );
  const total = stages.rows.length;
  const done = stages.rows.filter((r) => r.status === "done").length;

  if (total === 0) {
    return {
      slug,
      section_id,
      applied: false,
      stages_total: 0,
      stages_done: 0,
      change_log_entry_id: null,
      section_claim_released: false,
      cascaded_stage_releases: 0,
      red_stage_coverage_pct: null,
      pending_count: 0,
      unexpected_pass_count: 0,
      error: "no_stages_in_section",
    };
  }
  if (done < total) {
    return {
      slug,
      section_id,
      applied: false,
      stages_total: total,
      stages_done: done,
      change_log_entry_id: null,
      section_claim_released: false,
      cascaded_stage_releases: 0,
      red_stage_coverage_pct: null,
      pending_count: 0,
      unexpected_pass_count: 0,
      error: "stages_not_done",
    };
  }

  // --- Red-stage coverage aggregation (pre-transaction read) ---
  const memberStageIds = stages.rows.map((r) => r.stage_id);

  interface PerStageProofRow {
    stage_id: string;
    proof_status: string;
  }
  const perStageProofs = await pool.query<PerStageProofRow>(
    `SELECT DISTINCT ON (stage_id) stage_id, proof_status
       FROM ia_red_stage_proofs
      WHERE slug = $1
        AND stage_id = ANY($2::text[])
      ORDER BY stage_id, captured_at DESC`,
    [slug, memberStageIds],
  );

  const latestByStage = new Map<string, string>();
  for (const row of perStageProofs.rows) {
    latestByStage.set(row.stage_id, row.proof_status);
  }

  let red_stage_coverage_pct: number | null = null;
  let pending_count = 0;
  let unexpected_pass_count = 0;

  if (latestByStage.size > 0) {
    let failed_as_expected = 0;
    let not_applicable = 0;

    for (const stageId of memberStageIds) {
      const status = latestByStage.get(stageId);
      if (!status) {
        // No proof row → pending
        pending_count += 1;
      } else if (status === "failed_as_expected") {
        failed_as_expected += 1;
      } else if (status === "not_applicable") {
        not_applicable += 1;
      } else if (status === "unexpected_pass") {
        unexpected_pass_count += 1;
      } else {
        // pending status on the row itself
        pending_count += 1;
      }
    }

    red_stage_coverage_pct =
      Math.round(((failed_as_expected + not_applicable) / total) * 100 * 100) / 100;
  } else {
    // Zero proof rows → grandfathered, back-compat
    red_stage_coverage_pct = null;
  }

  // Gate: unexpected_pass_count blocks closeout
  if (unexpected_pass_count > 0) {
    return {
      slug,
      section_id,
      applied: false,
      stages_total: total,
      stages_done: done,
      change_log_entry_id: null,
      section_claim_released: false,
      cascaded_stage_releases: 0,
      red_stage_coverage_pct,
      pending_count,
      unexpected_pass_count,
      error: "red_stage_unexpected_pass_blocks_closeout",
    };
  }

  const client = await pool.connect();
  try {
    await client.query("BEGIN");

    const body = JSON.stringify({
      section_id,
      stages: stages.rows.map((r) => r.stage_id),
    });
    const ins = await client.query<{ entry_id: number }>(
      `INSERT INTO ia_master_plan_change_log
         (slug, kind, body, actor, commit_sha)
       VALUES ($1, 'section_done', $2, $3, $4)
       RETURNING entry_id`,
      [slug, body, actor ?? null, commit_sha ?? null],
    );
    const entry_id = ins.rows[0]!.entry_id;

    // V2 row-only: release section claim by row key alone, cascade to stage claims.
    const upd = await client.query(
      `UPDATE ia_section_claims
          SET released_at = now()
        WHERE slug = $1 AND section_id = $2
          AND released_at IS NULL`,
      [slug, section_id],
    );
    const releasedSection = (upd.rowCount ?? 0) > 0;

    const cas = await client.query(
      `UPDATE ia_stage_claims sc
          SET released_at = now()
         FROM ia_stages s
        WHERE sc.slug = s.slug
          AND sc.stage_id = s.stage_id
          AND sc.slug = $1
          AND s.section_id = $2
          AND sc.released_at IS NULL`,
      [slug, section_id],
    );
    const cascaded = cas.rowCount ?? 0;

    await client.query("COMMIT");

    return {
      slug,
      section_id,
      applied: true,
      stages_total: total,
      stages_done: done,
      change_log_entry_id: entry_id,
      section_claim_released: releasedSection,
      cascaded_stage_releases: cascaded,
      red_stage_coverage_pct,
      pending_count,
      unexpected_pass_count,
    };
  } catch (err) {
    await client.query("ROLLBACK").catch(() => {});
    throw err;
  } finally {
    client.release();
  }
}

export function registerSectionCloseoutApply(server: McpServer): void {
  server.registerTool(
    "section_closeout_apply",
    {
      description:
        "DB-backed mutate: pure-DB closeout for a section (V2 row-only). " +
        "Asserts all stages with `section_id` have status='done'; aggregates " +
        "§Red-Stage Proof rows across member stages — returns " +
        "`red_stage_coverage_pct`, `pending_count`, `unexpected_pass_count`; " +
        "rejects with `red_stage_unexpected_pass_blocks_closeout` when any stage " +
        "recorded an unexpected_pass proof. Sections with zero proof rows get " +
        "`red_stage_coverage_pct: null` (grandfathered back-compat). " +
        "Appends `ia_master_plan_change_log` row (kind='section_done'); " +
        "releases active section claim + cascade-releases open stage claims. " +
        "Does NOT run git. Caller (`/section-closeout`) verifies drift first. " +
        "parallel-carcass §6.2 (D4 V2 / D9). " +
        "Schema-cache restart required after add (N4).",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("section_closeout_apply", async () => {
        const envelope = await wrapTool(
          async (input: Args): Promise<SectionCloseoutResult> =>
            applySectionCloseout(input),
        )(args as Args);
        return jsonResult(envelope);
      }),
  );
}
