/**
 * MCP tool: task_bundle_batch (TECH-12635 — ship-protocol Stage 2)
 *
 * Pre-loads per-task context for N tasks of one master-plan slug in ONE call.
 * Used by `ship-plan` skill to amortize shared MCP fetches across all tasks
 * of a plan run.
 *
 * Input:  { slug: string, task_ids: string[] }
 * Output: { plan: {slug}, tasks: {[task_id]: BundleEntry | {error: string}} }
 *
 * Where BundleEntry =
 *   {
 *     title, status, stage_id,
 *     glossary_anchors: [{term, definition, specReference?}],
 *     invariants: [{id, summary}],
 *     spec_sections: [{spec, section_id}],
 *     router_domains: [{keyword, row}]
 *   }
 *
 * Dedup: glossary / invariants / router source files are read ONCE per call;
 * per-task searches are run against the cached parse trees.
 *
 * Unknown task_id → `{[task_id]: {error: "task_not_found"}}` (partial-success
 * batch). Empty `task_ids[]` returns `{tasks: {}}` without error.
 */

import { z } from "zod";
import fs from "node:fs";
import path from "node:path";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import type { SpecRegistryEntry, GlossaryEntry } from "../parser/types.js";
import { resolveRepoRoot, findEntryByKey } from "../config.js";
import { parseGlossary } from "../parser/glossary-parser.js";
import { normalizeGlossaryQuery } from "../parser/fuzzy.js";
import { splitLines } from "../parser/markdown-parser.js";
import { parseMarkdownTables } from "../parser/table-parser.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import { queryTaskState } from "../ia-db/queries.js";

const inputShape = {
  slug: z
    .string()
    .min(1)
    .describe("Master-plan slug (DB). All task_ids should belong to this plan."),
  task_ids: z
    .array(z.string().min(1))
    .describe("Task ids e.g. ['TECH-100','TECH-101']. Empty array → empty result."),
};

interface Input {
  slug?: string;
  task_ids?: string[];
}

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

function deriveKeywords(text: string): string[] {
  return text
    .toLowerCase()
    .replace(/[^a-z0-9 ]/g, " ")
    .split(/\s+/)
    .filter((w) => w.length > 3);
}

interface CachedSources {
  glossaryRows: GlossaryEntry[];
  invariantsLines: string[];
  routerTables: ReturnType<typeof parseMarkdownTables>;
}

function loadSources(root: string, registry: SpecRegistryEntry[]): CachedSources {
  const glossaryEntry = findEntryByKey(registry, "glossary");
  let glossaryRows: GlossaryEntry[] = [];
  if (glossaryEntry) {
    const glossaryPath = path.resolve(root, glossaryEntry.filePath);
    if (fs.existsSync(glossaryPath)) {
      glossaryRows = parseGlossary(glossaryPath);
    }
  }

  const invariantsPath = path.resolve(root, "ia/rules/invariants.md");
  const invariantsLines = fs.existsSync(invariantsPath)
    ? splitLines(fs.readFileSync(invariantsPath, "utf8"))
    : [];

  const routerEntry = findEntryByKey(registry, "agent-router");
  let routerTables: ReturnType<typeof parseMarkdownTables> = [];
  if (routerEntry) {
    const routerPath = path.resolve(root, routerEntry.filePath);
    if (fs.existsSync(routerPath)) {
      const lines = splitLines(fs.readFileSync(routerPath, "utf8"));
      routerTables = parseMarkdownTables(lines);
    }
  }

  return { glossaryRows, invariantsLines, routerTables };
}

function searchGlossary(
  rows: GlossaryEntry[],
  keywords: string[],
): Array<{ term: string; definition: string; specReference?: string }> {
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
  return hits.slice(0, 10);
}

function searchInvariants(lines: string[], keywords: string[]): Array<{ line: string; idx: number }> {
  const hits: Array<{ line: string; idx: number }> = [];
  if (keywords.length === 0) return hits;
  const lowerKws = keywords.map((k) => k.toLowerCase());
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i] ?? "";
    const lower = line.toLowerCase();
    if (lowerKws.some((kw) => lower.includes(kw))) {
      hits.push({ line, idx: i });
      if (hits.length >= 6) break;
    }
  }
  return hits;
}

function searchRouter(
  tables: ReturnType<typeof parseMarkdownTables>,
  keywords: string[],
): Array<{ keyword: string; row: Record<string, string> }> {
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
  return hits.slice(0, 8);
}

function deriveSpecSections(
  glossaryHits: Array<{ specReference?: string }>,
): Array<{ spec: string; section_id: string }> {
  const out: Array<{ spec: string; section_id: string }> = [];
  const seen = new Set<string>();
  for (const hit of glossaryHits) {
    if (!hit.specReference) continue;
    const ref = hit.specReference;
    const [spec, section] = ref.includes("#") ? ref.split("#", 2) : [ref, ""];
    const key = `${spec}#${section}`;
    if (seen.has(key)) continue;
    seen.add(key);
    out.push({ spec: spec ?? "", section_id: section ?? "" });
  }
  return out;
}

export function registerTaskBundleBatch(
  server: McpServer,
  registry: SpecRegistryEntry[],
): void {
  server.registerTool(
    "task_bundle_batch",
    {
      description:
        "Pre-load per-task context for N tasks of one master-plan slug in one call. Returns per-task `{glossary_anchors, invariants, spec_sections, router_domains}` map keyed by task_id. Source files (glossary, invariants, router) read once and shared across tasks. Unknown task_id → `{error: 'task_not_found'}`. Used by ship-plan to amortize shared MCP fetches across a plan run.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("task_bundle_batch", async () => {
        const envelope = await wrapTool(async (input: Input) => {
          const slug = (input.slug ?? "").trim();
          const taskIds = Array.isArray(input.task_ids) ? input.task_ids : [];
          if (!slug) {
            throw { code: "invalid_input", message: "slug is required." };
          }
          if (taskIds.length === 0) {
            return { plan: { slug }, tasks: {} as Record<string, unknown> };
          }

          const root = resolveRepoRoot();
          const sources = loadSources(root, registry);

          const out: Record<string, unknown> = {};
          for (const rawId of taskIds) {
            const id = rawId.trim().toUpperCase();
            if (!id) continue;
            let task;
            try {
              task = await queryTaskState(id);
            } catch (e) {
              out[id] = {
                error: "db_error",
                message: e instanceof Error ? e.message : String(e),
              };
              continue;
            }
            if (!task) {
              out[id] = { error: "task_not_found" };
              continue;
            }
            const keywords = deriveKeywords(task.title ?? "");
            const glossary_anchors = searchGlossary(sources.glossaryRows, keywords);
            const invariants = searchInvariants(sources.invariantsLines, keywords);
            const router_domains = searchRouter(sources.routerTables, keywords);
            const spec_sections = deriveSpecSections(glossary_anchors);
            out[id] = {
              title: task.title,
              status: task.status,
              stage_id: task.stage_id,
              glossary_anchors,
              invariants,
              spec_sections,
              router_domains,
            };
          }

          return { plan: { slug }, tasks: out };
        })(args as Input);
        return jsonResult(envelope);
      }),
  );
}
