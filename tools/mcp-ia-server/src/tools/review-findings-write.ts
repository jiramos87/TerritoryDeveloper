/**
 * MCP tool: review_findings_write
 *
 * Inserts one ia_review_findings row — emitted by critic subagents
 * (/critic-style, /critic-logic, /critic-security) during /ship-final Pass B.
 *
 * TECH-36145 / vibe-coding-safety stage-6-0
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import { getIaDatabasePool } from "../ia-db/pool.js";
import { IaDbUnavailableError } from "../ia-db/queries.js";

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

interface ReviewFindingsWriteInput {
  plan_slug?: string;
  stage_id?: number | null;
  critic_kind?: string;
  severity?: string;
  body?: string;
  file_path?: string | null;
  line_range?: string | null;
}

export function registerReviewFindingsWrite(server: McpServer): void {
  server.registerTool(
    "review_findings_write",
    {
      description:
        "Insert one ia_review_findings row. Called by critic subagents (/critic-style, /critic-logic, /critic-security) during /ship-final Pass B. Returns {ok, id, plan_slug, critic_kind, severity}.",
      inputSchema: {
        plan_slug: z.string().describe("Master-plan slug being reviewed."),
        stage_id: z
          .number()
          .int()
          .nullable()
          .optional()
          .describe("Optional stage integer id (NULL for plan-level findings)."),
        critic_kind: z
          .enum(["style", "logic", "security"])
          .describe("Emitting critic subagent kind."),
        severity: z
          .enum(["low", "medium", "high"])
          .describe("Finding severity. severity=high blocks plan close in ship-final."),
        body: z.string().min(1).describe("Finding description. Include concrete quote + fix suggestion."),
        file_path: z
          .string()
          .nullable()
          .optional()
          .describe("Repo-relative path of the file the finding applies to (NULL for plan-level)."),
        line_range: z
          .string()
          .nullable()
          .optional()
          .describe("Line range e.g. 'L12-L15' (NULL when not applicable)."),
      },
    },
    async (args) =>
      runWithToolTiming("review_findings_write", async () => {
        const envelope = await wrapTool(async (input: ReviewFindingsWriteInput | undefined) => {
          const plan_slug = (input?.plan_slug ?? "").trim();
          if (!plan_slug) {
            throw { code: "invalid_input", message: "plan_slug is required." };
          }
          const critic_kind = (input?.critic_kind ?? "").trim();
          if (!["style", "logic", "security"].includes(critic_kind)) {
            throw { code: "invalid_input", message: "critic_kind must be style|logic|security." };
          }
          const severity = (input?.severity ?? "").trim();
          if (!["low", "medium", "high"].includes(severity)) {
            throw { code: "invalid_input", message: "severity must be low|medium|high." };
          }
          const body = (input?.body ?? "").trim();
          if (!body) {
            throw { code: "invalid_input", message: "body is required." };
          }

          const pool = getIaDatabasePool();
          if (!pool) throw new IaDbUnavailableError();

          const res = await pool.query<{ id: number }>(
            `INSERT INTO ia_review_findings
               (plan_slug, stage_id, critic_kind, severity, body, file_path, line_range)
             VALUES ($1, $2, $3, $4, $5, $6, $7)
             RETURNING id`,
            [
              plan_slug,
              input?.stage_id ?? null,
              critic_kind,
              severity,
              body,
              input?.file_path ?? null,
              input?.line_range ?? null,
            ],
          );

          const id = res.rows[0]?.id;
          return { ok: true, id, plan_slug, critic_kind, severity };
        })(args as ReviewFindingsWriteInput | undefined);
        return jsonResult(envelope);
      }),
  );
}
