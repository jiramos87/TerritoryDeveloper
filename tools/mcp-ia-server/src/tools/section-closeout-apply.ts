/**
 * MCP tool: section_closeout_apply — pure-DB closeout for a section.
 *
 * Verifies all section stages have status='done'. Appends a row to
 * `ia_master_plan_change_log` (kind='section_done'). Releases the active
 * section claim and cascades release of stage claims for the section held
 * by the same session.
 *
 * **Does NOT run git** — caller skill (`/section-closeout`) handles merge
 * + worktree teardown.
 *
 * **Does NOT re-run drift scan** — caller MUST first verify zero open
 * `arch_drift_scan(scope='intra-plan')` events. This tool is the
 * post-verify atomic apply.
 *
 * parallel-carcass §6.2 / D9. Schema-cache restart required after add (N4).
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import { getIaDatabasePool } from "../ia-db/pool.js";

const inputShape = {
  slug: z.string().describe("Master-plan slug."),
  section_id: z.string().describe("Section identifier."),
  session_id: z
    .string()
    .optional()
    .describe(
      "Caller session id. Required to release the matching section claim.",
    ),
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
  session_id?: string;
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
  error?: string;
}

export async function applySectionCloseout(
  args: Args,
): Promise<SectionCloseoutResult> {
  const pool = getIaDatabasePool();
  if (!pool) {
    throw { code: "db_unavailable", message: "ia_db pool not initialized" };
  }
  const { slug, section_id, session_id, actor, commit_sha } = args;

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
      error: "stages_not_done",
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

    let releasedSection = false;
    let cascaded = 0;
    if (session_id) {
      const upd = await client.query(
        `UPDATE ia_section_claims
            SET released_at = now()
          WHERE slug = $1 AND section_id = $2 AND session_id = $3
            AND released_at IS NULL`,
        [slug, section_id, session_id],
      );
      releasedSection = (upd.rowCount ?? 0) > 0;

      const cas = await client.query(
        `UPDATE ia_stage_claims sc
            SET released_at = now()
           FROM ia_stages s
          WHERE sc.slug = s.slug
            AND sc.stage_id = s.stage_id
            AND sc.slug = $1
            AND s.section_id = $2
            AND sc.session_id = $3
            AND sc.released_at IS NULL`,
        [slug, section_id, session_id],
      );
      cascaded = cas.rowCount ?? 0;
    }

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
        "DB-backed mutate: pure-DB closeout for a section. Asserts all " +
        "stages with `section_id` have status='done'; appends " +
        "`ia_master_plan_change_log` row (kind='section_done', body= " +
        "`{section_id, stages[]}` JSON); releases active section claim + " +
        "cascade-releases stage claims when `session_id` provided. " +
        "Does NOT run git or re-run drift scan — caller (`/section-closeout`) " +
        "verifies drift first and handles merge after. " +
        "parallel-carcass §6.2 (D9). " +
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
