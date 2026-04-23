/**
 * MCP tool: lifecycle_stage_context
 * Composite bundle for a Stage in a master-plan: stage header block, task list,
 * glossary hits derived from stage title, and invariants summary slice.
 * Used by plan-reviewer-mechanical to amortize MCP context load to O(1) per Stage.
 * Input: { master_plan_path: string, stage_id: string, keyword_override?: string }
 * Output: { stage_block, tasks, glossary_hits, invariants_hint }
 */

import { z } from "zod";
import fs from "node:fs";
import path from "node:path";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import type { SpecRegistryEntry, GlossaryEntry } from "../parser/types.js";
import { resolveRepoRoot, findEntryByKey } from "../config.js";
import { parseGlossary } from "../parser/glossary-parser.js";
import { normalizeGlossaryQuery } from "../parser/fuzzy.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";

const inputShape = {
  master_plan_path: z
    .string()
    .min(1)
    .describe("Path to the master-plan markdown file (relative to repo root or absolute)."),
  stage_id: z
    .string()
    .min(1)
    .describe("Stage identifier, e.g. 'Stage 1.2' or '1.2'."),
  keyword_override: z
    .string()
    .optional()
    .describe("Space-separated keywords for glossary search. Default: derived from stage block heading."),
};

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

function normalizeStageId(raw: string): string {
  return raw.trim().replace(/^stage\s+/i, "");
}

function extractStageBlock(content: string, stageId: string): { block: string; heading: string } | null {
  const normId = normalizeStageId(stageId);
  const lines = content.split("\n");
  const headingRx = new RegExp(`^#{2,4}\\s+Stage\\s+${normId.replace(".", "\\.")}\\b`, "i");
  let startIdx = -1;
  let heading = "";
  for (let i = 0; i < lines.length; i++) {
    if (headingRx.test(lines[i]!)) {
      startIdx = i;
      heading = lines[i]!;
      break;
    }
  }
  if (startIdx === -1) return null;

  // Read until next same-or-higher level heading
  const headingDepth = (lines[startIdx]!.match(/^#{2,4}/) ?? ["##"])[0]!.length;
  const nextHeadingRx = new RegExp(`^#{1,${headingDepth}}\\s`);
  let endIdx = lines.length;
  for (let i = startIdx + 1; i < lines.length; i++) {
    if (nextHeadingRx.test(lines[i]!)) {
      endIdx = i;
      break;
    }
  }
  return { block: lines.slice(startIdx, endIdx).join("\n"), heading };
}

interface TaskRow {
  task_key: string;
  title: string;
  status: string;
  issue_id: string;
}

function extractTaskRows(block: string): TaskRow[] {
  const rows: TaskRow[] = [];
  // Match table rows with | col | col | ... pattern
  const lines = block.split("\n");
  let inTable = false;
  let headerParsed = false;
  let colMap: Record<string, number> = {};

  for (const line of lines) {
    if (!line.trim().startsWith("|")) {
      if (inTable) break;
      continue;
    }
    const cols = line.split("|").slice(1, -1).map((c) => c.trim());
    if (!inTable) {
      inTable = true;
      // Build column map from header
      cols.forEach((c, i) => {
        colMap[c.toLowerCase().replace(/\s+/g, "_")] = i;
      });
      headerParsed = true;
      continue;
    }
    if (headerParsed && line.trim().replace(/[|\s-]/g, "") === "") {
      // separator row
      continue;
    }
    if (inTable && headerParsed) {
      const taskKey = cols[colMap["task"] ?? colMap["phase/task"] ?? 0] ?? "";
      const title = cols[colMap["title"] ?? colMap["description"] ?? 1] ?? "";
      const status = cols[colMap["status"] ?? 2] ?? "";
      const issueId = cols[colMap["issue"] ?? colMap["issue_id"] ?? 3] ?? "";
      if (taskKey || issueId) {
        rows.push({
          task_key: taskKey.replace(/\*\*/g, ""),
          title: title.replace(/\*\*/g, ""),
          status: status.replace(/\*\*/g, ""),
          issue_id: issueId.replace(/\*\*/g, ""),
        });
      }
    }
  }
  return rows;
}

function deriveKeywords(text: string): string[] {
  return text
    .toLowerCase()
    .replace(/[^a-z0-9 ]/g, " ")
    .split(/\s+/)
    .filter((w) => w.length > 3);
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

export function registerLifecycleStageContext(server: McpServer, registry: SpecRegistryEntry[]): void {
  server.registerTool(
    "lifecycle_stage_context",
    {
      description:
        "Composite Stage-level context bundle for plan-reviewer-mechanical: stage block text, task list, glossary hits. Amortizes MCP calls to O(1) per Stage instead of O(N) per task. Use at Stage-review start.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("lifecycle_stage_context", async () => {
        const envelope = await wrapTool(
          async (input: { master_plan_path: string; stage_id: string; keyword_override?: string }) => {
            const root = resolveRepoRoot();
            const absPath = path.isAbsolute(input.master_plan_path)
              ? input.master_plan_path
              : path.resolve(root, input.master_plan_path);

            if (!fs.existsSync(absPath)) {
              throw { code: "file_not_found", message: `Master plan not found: ${absPath}` };
            }

            const content = fs.readFileSync(absPath, "utf8");
            const extracted = extractStageBlock(content, input.stage_id);
            if (!extracted) {
              throw {
                code: "stage_not_found",
                message: `Stage '${input.stage_id}' not found in ${input.master_plan_path}`,
              };
            }

            const tasks = extractTaskRows(extracted.block);
            const rawKeywords = input.keyword_override ?? extracted.heading;
            const keywords = deriveKeywords(rawKeywords);
            const glossaryHits = glossarySearch(root, registry, keywords);

            return {
              stage_id: input.stage_id,
              master_plan: input.master_plan_path,
              stage_heading: extracted.heading,
              stage_block: extracted.block,
              tasks,
              glossary_hits: glossaryHits,
              hint: "Use spec_section to load individual task specs. Use issue_context_bundle per task if deeper context needed.",
            };
          },
        )(args as { master_plan_path: string; stage_id: string; keyword_override?: string });
        return jsonResult(envelope);
      }),
  );
}
