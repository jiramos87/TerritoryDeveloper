/**
 * MCP tool: backlog_list — structured filter queries across backlog records.
 * Replaces ad-hoc Grep patterns for enumerating issues by field.
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { resolveRepoRoot } from "../config.js";
import {
  parseAllBacklogIssuesWithMeta,
  type ParsedBacklogIssue,
} from "../parser/backlog-parser.js";
import { runWithToolTiming } from "../instrumentation.js";

const NOTES_TRUNCATE = 200;

const inputShape = {
  section: z
    .string()
    .optional()
    .describe(
      "Substring match on backlog section label (case-insensitive). E.g. \"Blip audio program\".",
    ),
  priority: z
    .string()
    .optional()
    .describe(
      "Exact match on priority field (case-insensitive). E.g. \"high\", \"medium\", \"low\".",
    ),
  type: z
    .string()
    .optional()
    .describe(
      "Exact match on type field (case-insensitive). E.g. \"infrastructure / MCP tooling\".",
    ),
  status: z
    .string()
    .optional()
    .describe(
      "Exact match on status field (case-insensitive). E.g. \"open\", \"completed\".",
    ),
  scope: z
    .enum(["open", "archive", "all"])
    .default("open")
    .describe(
      "Which backlog file(s) to query: open (BACKLOG.md), archive (BACKLOG-ARCHIVE.md), or all (both). Default: open.",
    ),
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

function truncate(text: string, maxLen: number): string {
  if (text.length <= maxLen) return text;
  return text.slice(0, maxLen) + "…";
}

/**
 * Extract numeric id from an issue_id string like "TECH-328" → 328.
 * Returns 0 for unparseable strings.
 */
function numericId(issueId: string): number {
  const match = issueId.match(/(\d+)$/);
  return match ? parseInt(match[1]!, 10) : 0;
}

/**
 * Prefix-alphabetic then numeric id descending comparator.
 * E.g. AUDIO < BUG < FEAT < TECH; within TECH: 340 > 328.
 */
function compareIssues(a: ParsedBacklogIssue, b: ParsedBacklogIssue): number {
  const prefixA = a.issue_id.replace(/-\d+$/, "");
  const prefixB = b.issue_id.replace(/-\d+$/, "");
  if (prefixA < prefixB) return -1;
  if (prefixA > prefixB) return 1;
  // Same prefix — numeric id descending
  return numericId(b.issue_id) - numericId(a.issue_id);
}

/**
 * Apply AND filters to a list of issues.
 */
function applyFilters(
  issues: ParsedBacklogIssue[],
  filters: {
    section?: string;
    priority?: string;
    type?: string;
    status?: string;
  },
): ParsedBacklogIssue[] {
  return issues.filter((issue) => {
    if (filters.section !== undefined) {
      const haystack = (issue.backlog_section ?? "").toLowerCase();
      if (!haystack.includes(filters.section.toLowerCase())) return false;
    }
    if (filters.priority !== undefined) {
      const val = (issue.priority ?? "").toLowerCase();
      if (val !== filters.priority.toLowerCase()) return false;
    }
    if (filters.type !== undefined) {
      const val = (issue.type ?? "").toLowerCase();
      if (val !== filters.type.toLowerCase()) return false;
    }
    if (filters.status !== undefined) {
      const val = (issue.status ?? "").toLowerCase();
      if (val !== filters.status.toLowerCase()) return false;
    }
    return true;
  });
}

/**
 * Register the backlog_list tool.
 */
export function registerBacklogList(server: McpServer): void {
  server.registerTool(
    "backlog_list",
    {
      description:
        "List backlog issues using structured filters (section, priority, type, status, scope). All filters are AND-combined. Use for enumeration by field; use backlog_search for keyword scoring.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("backlog_list", async () => {
        const scope = (args?.scope as "open" | "archive" | "all") ?? "open";
        const filters = {
          section: args?.section as string | undefined,
          priority: args?.priority as string | undefined,
          type: args?.type as string | undefined,
          status: args?.status as string | undefined,
        };

        const repoRoot = resolveRepoRoot();
        const { records: allIssues, parseErrorCount } =
          parseAllBacklogIssuesWithMeta(repoRoot, scope);

        const matched = applyFilters(allIssues, filters).sort(compareIssues);

        const issues = matched.map((issue) => ({
          issue_id: issue.issue_id,
          title: issue.title,
          type: issue.type ?? null,
          status: issue.status,
          section: issue.backlog_section,
          priority: issue.priority ?? null,
          related: issue.related ?? [],
          created: issue.created ?? null,
          notes: issue.notes ? truncate(issue.notes, NOTES_TRUNCATE) : null,
        }));

        return jsonResult({
          scope,
          total_searched: allIssues.length,
          result_count: issues.length,
          issues,
          ...(parseErrorCount > 0 ? { parseErrorCount } : {}),
        });
      }),
  );
}
