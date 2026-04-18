/**
 * MCP tool: backlog_search — keyword search across open/archived backlog issues.
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { resolveRepoRoot } from "../config.js";
import {
  parseAllBacklogIssuesWithMeta,
  type ParsedBacklogIssue,
} from "../parser/backlog-parser.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";

const MAX_RESULTS_CAP = 50;
const NOTES_TRUNCATE = 200;

const inputShape = {
  query: z
    .string()
    .describe(
      "Search keywords (space-separated, case-insensitive). Matches against issue title, Notes, Files, and Type fields.",
    ),
  scope: z
    .enum(["open", "archive", "all"])
    .default("open")
    .describe(
      "Which backlog file(s) to search: open (BACKLOG.md), archive (BACKLOG-ARCHIVE.md), or all (both). Default: open.",
    ),
  max_results: z
    .number()
    .int()
    .min(1)
    .max(MAX_RESULTS_CAP)
    .default(10)
    .describe(`Max results to return (1–${MAX_RESULTS_CAP}, default 10).`),
};

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
 * Tokenize a string into lowercase words (≥2 chars).
 */
function tokenize(text: string): string[] {
  return text
    .toLowerCase()
    .split(/[^a-z0-9]+/)
    .filter((t) => t.length >= 2);
}

/**
 * Score an issue against query tokens. Higher = better match.
 * Title matches are weighted 3x, type 2x, files/notes 1x.
 */
export function scoreIssue(
  issue: ParsedBacklogIssue,
  queryTokens: string[],
): number {
  if (queryTokens.length === 0) return 0;
  let score = 0;

  const titleLower = issue.title.toLowerCase();
  const notesLower = (issue.notes ?? "").toLowerCase();
  const filesLower = (issue.files ?? "").toLowerCase();
  const typeLower = (issue.type ?? "").toLowerCase();
  const idLower = issue.issue_id.toLowerCase();

  for (const qt of queryTokens) {
    if (idLower.includes(qt)) score += 5;
    if (titleLower.includes(qt)) score += 3;
    if (typeLower.includes(qt)) score += 2;
    if (filesLower.includes(qt)) score += 1;
    if (notesLower.includes(qt)) score += 1;
  }

  return score;
}

function truncate(text: string, maxLen: number): string {
  if (text.length <= maxLen) return text;
  return text.slice(0, maxLen) + "…";
}

type BacklogSearchArgs = {
  query?: string;
  scope?: "open" | "archive" | "all";
  max_results?: number;
};

/**
 * Register the backlog_search tool.
 */
export function registerBacklogSearch(server: McpServer): void {
  server.registerTool(
    "backlog_search",
    {
      description:
        "Search backlog issues by keywords (title, Notes, Files, Type). Returns ranked results. Use when you need to find related issues without knowing the exact id.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("backlog_search", async () => {
        const envelope = await wrapTool(
          async (input: BacklogSearchArgs | undefined) => {
            const query = (input?.query ?? "").trim();
            if (!query) {
              throw { code: "invalid_input", message: "query is required." };
            }

            const scope = (input?.scope as "open" | "archive" | "all") ?? "open";
            const maxResults = input?.max_results ?? 10;

            const repoRoot = resolveRepoRoot();
            const { records: allIssues, parseErrorCount } = parseAllBacklogIssuesWithMeta(repoRoot, scope);
            const queryTokens = tokenize(query);

            if (queryTokens.length === 0) {
              throw {
                code: "invalid_input",
                message:
                  "No searchable tokens in query (tokens must be ≥2 alphanumeric chars).",
              };
            }

            const scored = allIssues
              .map((issue) => ({
                issue,
                score: scoreIssue(issue, queryTokens),
              }))
              .filter((s) => s.score > 0)
              .sort((a, b) => b.score - a.score)
              .slice(0, maxResults);

            const results = scored.map((s) => ({
              issue_id: s.issue.issue_id,
              title: s.issue.title,
              type: s.issue.type ?? null,
              status: s.issue.status,
              section: s.issue.backlog_section,
              score: s.score,
              notes: s.issue.notes ? truncate(s.issue.notes, NOTES_TRUNCATE) : null,
              priority: s.issue.priority ?? null,
              related: s.issue.related ?? [],
              created: s.issue.created ?? null,
            }));

            return {
              query,
              scope,
              total_searched: allIssues.length,
              result_count: results.length,
              results,
              ...(parseErrorCount > 0 ? { parseErrorCount } : {}),
            };
          },
        )(args as BacklogSearchArgs | undefined);

        return jsonResult(envelope);
      }),
  );
}
