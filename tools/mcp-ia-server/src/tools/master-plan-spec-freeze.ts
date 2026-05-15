/**
 * MCP tool: master_plan_spec_freeze
 *
 * Input {slug, source_doc_path}. Reads Design Expansion section from
 * source_doc_path, parses open questions count (lines matching
 * /\bopen question/i), INSERTs ia_master_plan_specs row with frozen_at=NOW().
 * Emits arch_changelog row kind=spec_frozen.
 * Fails if open_questions_count > 0.
 *
 * TECH-36117 / vibe-coding-safety Stage 3.0
 */

import { z } from "zod";
import fs from "node:fs/promises";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import { getIaDatabasePool } from "../ia-db/pool.js";
import { IaDbUnavailableError } from "../ia-db/queries.js";

function jsonResult(payload: unknown) {
  return {
    content: [
      {
        type: "text" as const,
        text: JSON.stringify(payload, null, 2),
      },
    ],
  };
}

/**
 * Count unresolved open questions in a markdown body.
 * Matches lines containing "open question" (case-insensitive) that are
 * NOT struck-through (~~...~~) and NOT prefixed with a checked checkbox ([x]).
 */
function countOpenQuestions(body: string): number {
  const lines = body.split("\n");
  let count = 0;
  for (const line of lines) {
    const trimmed = line.trim();
    // Skip struck-through lines
    if (trimmed.startsWith("~~") && trimmed.endsWith("~~")) continue;
    // Skip checked checkbox items
    if (/^\[x\]/i.test(trimmed)) continue;
    if (/open question/i.test(trimmed)) {
      count++;
    }
  }
  return count;
}

export function registerMasterPlanSpecFreeze(server: McpServer): void {
  server.registerTool(
    "master_plan_spec_freeze",
    {
      description:
        "Freeze a master plan spec: reads source_doc_path, parses open questions count, INSERTs ia_master_plan_specs row with frozen_at=NOW(). Emits arch_changelog kind=spec_frozen. Fails if open_questions_count > 0. Returns {slug, version, frozen_at, open_questions_count, spec_id}.",
      inputSchema: {
        slug: z.string().describe("Master-plan slug to freeze."),
        source_doc_path: z
          .string()
          .describe(
            "Repo-relative path to the design exploration document (e.g. docs/explorations/{slug}.md). Body is stored as the spec snapshot.",
          ),
        version: z
          .number()
          .int()
          .positive()
          .default(1)
          .describe("Spec version (default 1). Increments when re-freezing a revised spec."),
        force: z
          .boolean()
          .default(false)
          .describe(
            "When true, freeze even if open_questions_count > 0. Logs arch_changelog kind=spec_freeze_bypass instead of spec_frozen.",
          ),
      },
    },
    async (args) =>
      runWithToolTiming("master_plan_spec_freeze", async () => {
        const envelope = await wrapTool(
          async (input: {
            slug: string;
            source_doc_path: string;
            version?: number;
            force?: boolean;
          }) => {
            const pool = getIaDatabasePool();
            if (!pool) throw new IaDbUnavailableError();

            const slug = input.slug;
            const version = input.version ?? 1;
            const force = input.force ?? false;
            const docPath = input.source_doc_path;

            // Read source document
            let body: string;
            try {
              body = await fs.readFile(docPath, "utf8");
            } catch (err: unknown) {
              const msg = err instanceof Error ? err.message : String(err);
              throw {
                code: "source_doc_missing",
                message: `Cannot read source_doc_path: ${docPath}. ${msg}`,
              };
            }

            const openQuestionsCount = countOpenQuestions(body);

            if (openQuestionsCount > 0 && !force) {
              throw {
                code: "open_questions_unresolved",
                message: `Cannot freeze spec for '${slug}': ${openQuestionsCount} open question(s) detected. Resolve all open questions or pass force=true (logs spec_freeze_bypass).`,
                open_questions_count: openQuestionsCount,
              };
            }

            const client = await pool.connect();
            try {
              await client.query("BEGIN");

              // Upsert ia_master_plan_specs row
              const upsertResult = await client.query(
                `INSERT INTO ia_master_plan_specs (slug, version, frozen_at, body, open_questions_count, updated_at)
                 VALUES ($1, $2, NOW(), $3, $4, NOW())
                 ON CONFLICT (slug, version) DO UPDATE
                   SET frozen_at = NOW(),
                       body = EXCLUDED.body,
                       open_questions_count = EXCLUDED.open_questions_count,
                       updated_at = NOW()
                 RETURNING id, frozen_at`,
                [slug, version, body, openQuestionsCount],
              );

              const specRow = upsertResult.rows[0];
              const specId: number = specRow.id;
              const frozenAt: string = specRow.frozen_at;

              // Emit arch_changelog row
              const changelogKind = force && openQuestionsCount > 0 ? "spec_freeze_bypass" : "spec_frozen";
              const changelogBody = force && openQuestionsCount > 0
                ? `spec_freeze_bypass: slug=${slug} version=${version} open_questions_count=${openQuestionsCount}`
                : `spec_frozen: slug=${slug} version=${version} open_questions_count=${openQuestionsCount}`;

              await client.query(
                `INSERT INTO cron_arch_changelog_append_jobs
                   (decision_slug, kind, body, plan_slug, status, created_at)
                 VALUES ($1, $2, $3, $4, 'pending', NOW())`,
                [`plan-${slug}-spec-freeze`, changelogKind, changelogBody, slug],
              );

              await client.query("COMMIT");

              return {
                slug,
                version,
                spec_id: specId,
                frozen_at: frozenAt,
                open_questions_count: openQuestionsCount,
                changelog_kind: changelogKind,
                bypassed: force && openQuestionsCount > 0,
              };
            } catch (err) {
              await client.query("ROLLBACK");
              throw err;
            } finally {
              client.release();
            }
          },
        )(args);
        return jsonResult(envelope);
      }),
  );
}
