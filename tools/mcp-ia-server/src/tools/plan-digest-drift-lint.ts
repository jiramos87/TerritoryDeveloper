/**
 * MCP tool: plan_digest_drift_lint
 *
 * Pure SQL+regex pre-bundle drift lint. No LLM round-trip. Inputs a list of
 * (task_key, body, anchors, glossary_terms); returns findings array matching
 * ship-stage-journal-schema drift_lint_summary payload.
 *
 * Replaces the in-prompt drift-lint logic in ship-plan Phase 6.
 *
 * Lifecycle skills refactor — Phase 2 / weak-spot #4.
 */

import { z } from "zod";
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

interface TaskInput {
  task_key?: string;
  body?: string;
  anchors?: string[];
  glossary_terms?: string[];
}

interface DriftLintInput {
  slug?: string;
  version?: number;
  tasks?: TaskInput[];
}

interface Finding {
  task_key: string;
  rule: string;
  severity: "warn" | "error";
  detail: string;
}

const ANCHOR_RX = /^[A-Za-z0-9_./\-]+::[A-Za-z0-9_]+$/;

async function checkGlossaryTerm(
  pool: ReturnType<typeof getIaDatabasePool>,
  term: string,
): Promise<boolean> {
  if (!pool) return true;
  // glossary_entries — canonical term column
  const res = await pool.query<{ exists: boolean }>(
    `SELECT EXISTS (
       SELECT 1 FROM glossary_entries WHERE lower(term) = lower($1)
     ) AS exists`,
    [term],
  );
  return res.rows[0]?.exists ?? false;
}

export function registerPlanDigestDriftLint(server: McpServer): void {
  server.registerTool(
    "plan_digest_drift_lint",
    {
      description:
        "Pure SQL+regex pre-bundle drift lint. Inputs {slug, version, tasks:[{task_key,body,anchors,glossary_terms}]}. Returns findings array (matching ship-stage-journal drift_lint_summary). No LLM. Replaces ship-plan Phase 6 in-prompt logic. Pair with cron_drift_lint_findings_enqueue(status='staged') for crash-safe stash.",
      inputSchema: {
        slug: z.string().describe("Master-plan slug (for finding tagging)."),
        version: z.number().int().describe("Plan version (for finding tagging)."),
        tasks: z
          .array(
            z.object({
              task_key: z.string(),
              body: z.string().optional(),
              anchors: z.array(z.string()).optional(),
              glossary_terms: z.array(z.string()).optional(),
            }),
          )
          .describe("Per-task lint payloads."),
      },
    },
    async (args) =>
      runWithToolTiming("plan_digest_drift_lint", async () => {
        const envelope = await wrapTool(async (input: DriftLintInput | undefined) => {
          const slug = (input?.slug ?? "").trim();
          if (!slug) {
            throw { code: "invalid_input", message: "slug is required." };
          }
          if (input?.version === undefined || input?.version === null) {
            throw { code: "invalid_input", message: "version is required." };
          }
          const tasks = Array.isArray(input?.tasks) ? input.tasks : [];
          const pool = getIaDatabasePool();
          if (!pool) {
            throw new IaDbUnavailableError();
          }

          const findings: Finding[] = [];

          for (const t of tasks) {
            const task_key = (t.task_key ?? "").trim();
            if (!task_key) continue;
            const body = t.body ?? "";

            // Rule 1: anchor format `{path}::{symbol}`.
            const anchors = Array.isArray(t.anchors) ? t.anchors : [];
            for (const a of anchors) {
              if (!ANCHOR_RX.test(a)) {
                findings.push({
                  task_key,
                  rule: "anchor_format",
                  severity: "error",
                  detail: `anchor '${a}' does not match {path}::{symbol}`,
                });
              }
            }

            // Rule 2: §Plan Digest body must include **Gate:** section.
            if (body && !/\n\*\*Gate:\*\*/.test(body)) {
              findings.push({
                task_key,
                rule: "missing_gate",
                severity: "warn",
                detail: "§Plan Digest body has no **Gate:** marker",
              });
            }

            // Rule 3: glossary terms must exist in glossary_entries.
            const terms = Array.isArray(t.glossary_terms) ? t.glossary_terms : [];
            for (const term of terms) {
              const ok = await checkGlossaryTerm(pool, term);
              if (!ok) {
                findings.push({
                  task_key,
                  rule: "glossary_unknown",
                  severity: "warn",
                  detail: `glossary term '${term}' not in glossary_entries`,
                });
              }
            }

            // Rule 4: caveman drift — common pleasantry / hedging tokens.
            const HEDGE_RX = /\b(likely|probably|maybe|perhaps|we could|might|consider)\b/i;
            if (HEDGE_RX.test(body)) {
              findings.push({
                task_key,
                rule: "hedging_language",
                severity: "warn",
                detail: "body contains hedging tokens (caveman drift)",
              });
            }
          }

          const n_unresolved = findings.filter((f) => f.severity === "error").length;
          const n_resolved = findings.length - n_unresolved;

          return {
            slug,
            version: input.version,
            findings,
            n_resolved,
            n_unresolved,
          };
        })(args as DriftLintInput | undefined);
        return jsonResult(envelope);
      }),
  );
}
