/**
 * MCP tool: issue_context_bundle
 * Composite bundle: backlog_issue + router_for_task keywords + glossary_discover.
 * Replaces sequential MCP chain for spec-implementer and plan-reviewer-mechanical.
 * Input: { issue_id: string, keyword_override?: string }
 * Output: { issue, router_hits, glossary_hits }
 */

import { z } from "zod";
import fs from "node:fs";
import path from "node:path";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import type { SpecRegistryEntry, GlossaryEntry } from "../parser/types.js";
import { resolveRepoRoot, findEntryByKey } from "../config.js";
import { parseBacklogIssue, resolveDependsOnStatus } from "../parser/backlog-parser.js";
import { parseGlossary } from "../parser/glossary-parser.js";
import { normalizeGlossaryQuery } from "../parser/fuzzy.js";
import { splitLines } from "../parser/markdown-parser.js";
import { parseMarkdownTables } from "../parser/table-parser.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";

const inputShape = {
  issue_id: z.string().min(1).describe("Issue id, e.g. TECH-471, BUG-37."),
  keyword_override: z
    .string()
    .optional()
    .describe("Space-separated keywords for router + glossary search. Default: derived from issue title."),
};

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

function deriveKeywords(title: string): string[] {
  return title
    .toLowerCase()
    .replace(/[^a-z0-9 ]/g, " ")
    .split(/\s+/)
    .filter((w) => w.length > 3);
}

function routerSearch(root: string, registry: SpecRegistryEntry[], keywords: string[]) {
  const routerEntry = findEntryByKey(registry, "agent-router");
  if (!routerEntry) return [];
  const routerPath = path.resolve(root, routerEntry.filePath);
  if (!fs.existsSync(routerPath)) return [];
  const content = fs.readFileSync(routerPath, "utf8");
  const lines = splitLines(content);
  const tables = parseMarkdownTables(lines);
  const hits: Array<{ keyword: string; row: Record<string, string> }> = [];
  for (const table of tables) {
    for (const row of table.rows) {
      const rowStr = Object.values(row).join(" ").toLowerCase();
      for (const kw of keywords) {
        if (rowStr.includes(kw)) {
          hits.push({ keyword: kw, row });
          break;
        }
      }
    }
  }
  return hits.slice(0, 10);
}

function glossarySearch(
  root: string,
  registry: SpecRegistryEntry[],
  keywords: string[],
): Array<{ term: string; definition: string; specReference?: string }> {
  const glossaryEntry = findEntryByKey(registry, "glossary");
  if (!glossaryEntry) return [];
  const glossaryPath = path.resolve(root, glossaryEntry.filePath);
  if (!fs.existsSync(glossaryPath)) return [];
  const content = fs.readFileSync(glossaryPath, "utf8");
  const rows: GlossaryEntry[] = parseGlossary(content);
  const hits: Array<{ term: string; definition: string; specReference?: string }> = [];
  const seen = new Set<string>();
  for (const kw of keywords) {
    const norm = normalizeGlossaryQuery(kw);
    for (const row of rows) {
      const termNorm = normalizeGlossaryQuery(row.term);
      if (!seen.has(row.term) && (termNorm.includes(norm) || norm.includes(termNorm))) {
        seen.add(row.term);
        hits.push({ term: row.term, definition: row.definition, specReference: row.specReference });
      }
    }
  }
  return hits.slice(0, 15);
}

export function registerIssueContextBundle(server: McpServer, registry: SpecRegistryEntry[]): void {
  server.registerTool(
    "issue_context_bundle",
    {
      description:
        "Composite context bundle for an issue: backlog_issue record + router_for_task keyword hits + glossary_discover matches. Replaces sequential chain: backlog_issue → router_for_task → glossary_discover → spec_section. Use at task-start for spec-implementer or plan-reviewer-mechanical.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("issue_context_bundle", async () => {
        const envelope = await wrapTool(
          async (input: { issue_id: string; keyword_override?: string }) => {
            const root = resolveRepoRoot();
            const issueId = input.issue_id.trim();

            const issue = parseBacklogIssue(root, issueId);
            if (!issue) {
              throw { code: "issue_not_found", message: `Issue not found: ${issueId}` };
            }
            const dependsOnStatus = resolveDependsOnStatus(root, issue.depends_on);

            const rawKeywords = input.keyword_override ?? issue.title ?? "";
            const keywords = deriveKeywords(rawKeywords);

            const routerHits = routerSearch(root, registry, keywords);
            const glossaryHits = glossarySearch(root, registry, keywords);

            return {
              issue: {
                id: issue.issue_id,
                title: issue.title,
                status: issue.status,
                type: issue.type,
                priority: issue.priority,
                notes: issue.notes,
                acceptance: issue.acceptance,
                files: issue.files,
                spec: issue.spec,
                depends_on: issue.depends_on,
                depends_on_status: dependsOnStatus,
              },
              router_hits: routerHits,
              glossary_hits: glossaryHits,
              hint: "Use spec_section(spec, section_id) for design details. Use spec_sections for multi-section load.",
            };
          },
        )(args as { issue_id: string; keyword_override?: string });
        return jsonResult(envelope);
      }),
  );
}
