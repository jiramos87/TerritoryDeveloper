/**
 * MCP tool: glossary_lookup — exact + fuzzy term match in glossary.md,
 * augmented with graph data: `related`, `cited_in`, `appears_in_code`.
 */

import { z } from "zod";
import fs from "node:fs";
import path from "node:path";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import type { SpecRegistryEntry } from "../parser/types.js";
import { parseGlossary } from "../parser/glossary-parser.js";
import { findEntryByKey, resolveRepoRoot } from "../config.js";
import { fuzzyFind, normalizeGlossaryQuery } from "../parser/fuzzy.js";
import { runWithToolTiming } from "../instrumentation.js";

const FUZZY_STRONG_THRESHOLD = 0.3;

interface GlossaryCitation {
  spec: string;
  section_id: string;
  section_title: string;
}

interface GlossaryGraphEntry {
  related: string[];
  cited_in: GlossaryCitation[];
}

interface GlossaryGraphDocument {
  artifact: string;
  schema_version: number;
  last_generated: string;
  graph: Record<string, GlossaryGraphEntry>;
}

/**
 * Lazy-loaded on first lookup; parsed from
 * `tools/mcp-ia-server/data/glossary-graph-index.json` (produced by
 * `npm run generate:ia-indexes`). `null` when the file does not exist.
 */
let graphCache: GlossaryGraphDocument | null | undefined = undefined;

function loadGraph(): GlossaryGraphDocument | null {
  if (graphCache !== undefined) return graphCache;
  const repoRoot = resolveRepoRoot();
  const p = path.join(
    repoRoot,
    "tools",
    "mcp-ia-server",
    "data",
    "glossary-graph-index.json",
  );
  if (!fs.existsSync(p)) {
    graphCache = null;
    return null;
  }
  try {
    const raw = fs.readFileSync(p, "utf8");
    graphCache = JSON.parse(raw) as GlossaryGraphDocument;
    return graphCache;
  } catch {
    graphCache = null;
    return null;
  }
}

/**
 * Clear the in-memory graph + code-appearance caches (used by tests).
 */
export function clearGlossaryGraphCaches(): void {
  graphCache = undefined;
  codeAppearanceCache.clear();
}

interface CodeAppearance {
  file: string;
  line: number;
}

/** Per-process cache: term → list of `{file, line}` from `Assets/Scripts/`. */
const codeAppearanceCache = new Map<string, CodeAppearance[]>();

const CODE_APPEARANCE_MAX = 50;

function globCsFiles(dir: string, results: string[] = []): string[] {
  if (!fs.existsSync(dir)) return results;
  const entries = fs.readdirSync(dir, { withFileTypes: true });
  for (const entry of entries) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      globCsFiles(full, results);
    } else if (entry.name.endsWith(".cs")) {
      results.push(full);
    }
  }
  return results;
}

/**
 * Scan `Assets/Scripts/**\/*.cs` for literal mentions of {@link term}.
 * Result is cached per process to keep subsequent lookups O(1).
 * Exported for unit testing.
 */
export function scanCodeAppearances(
  term: string,
  repoRoot: string,
): CodeAppearance[] {
  if (codeAppearanceCache.has(term)) {
    return codeAppearanceCache.get(term)!;
  }
  const assetsDir = path.join(repoRoot, "Assets", "Scripts");
  if (!fs.existsSync(assetsDir)) {
    codeAppearanceCache.set(term, []);
    return [];
  }
  // Escape regex specials; match the term as a whole word (alnum/underscore boundary).
  const escaped = term.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
  const re = new RegExp(
    `(?:^|[^A-Za-z0-9_])${escaped}(?:$|[^A-Za-z0-9_])`,
    "i",
  );

  const hits: CodeAppearance[] = [];
  const files = globCsFiles(assetsDir);
  outer: for (const file of files) {
    const content = fs.readFileSync(file, "utf8");
    if (!re.test(content)) continue;
    const lines = content.split(/\r?\n/);
    const rel = path.relative(repoRoot, file).split(path.sep).join("/");
    for (let i = 0; i < lines.length; i++) {
      if (re.test(lines[i]!)) {
        hits.push({ file: rel, line: i + 1 });
        if (hits.length >= CODE_APPEARANCE_MAX) break outer;
      }
    }
  }
  codeAppearanceCache.set(term, hits);
  return hits;
}

function graphDataFor(term: string): {
  related: string[];
  cited_in: GlossaryCitation[];
} {
  const graph = loadGraph();
  if (!graph) return { related: [], cited_in: [] };
  const entry = graph.graph[term];
  if (!entry) return { related: [], cited_in: [] };
  return {
    related: entry.related,
    cited_in: entry.cited_in,
  };
}

const inputShape = {
  term: z
    .string()
    .describe(
      "English glossary term to look up (e.g. 'wet run', 'HeightMap', 'hight map'). Translate from the user’s language when needed. Case-insensitive; bracket text like [x,y] is ignored for matching.",
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

/**
 * Register the glossary_lookup tool.
 */
export function registerGlossaryLookup(
  server: McpServer,
  registry: SpecRegistryEntry[],
): void {
  server.registerTool(
    "glossary_lookup",
    {
      description:
        "Look up a domain term in the glossary (exact match, then fuzzy suggestions for typos). " +
        "Pass the term in English (glossary language); translate from the conversation if the user did not use English. " +
        "Returns graph fields when available: `related` (top co-occurring glossary terms), `cited_in` (spec sections where the term appears), and `appears_in_code` (C# file/line mentions under Assets/Scripts/, cached per process).",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("glossary_lookup", async () => {
        const rawTerm = (args?.term ?? "").trim();
        const entry = findEntryByKey(registry, "glossary");
        if (!entry) {
          return jsonResult({
            error: "term_not_found",
            message: "Glossary file is not registered.",
            available_terms: [] as string[],
          });
        }

        const entries = parseGlossary(entry.filePath);
        const normQ = normalizeGlossaryQuery(rawTerm);
        const lowerRaw = rawTerm.toLowerCase();
        const lowerNorm = normQ.toLowerCase();

        const hit = entries.find(
          (e) =>
            e.term.toLowerCase() === lowerRaw ||
            e.term.toLowerCase() === lowerNorm,
        );

        const repoRoot = resolveRepoRoot();

        if (hit) {
          const { related, cited_in } = graphDataFor(hit.term);
          return jsonResult({
            term: hit.term,
            definition: hit.definition,
            specReference: hit.specReference,
            category: hit.category,
            matchType: "exact" as const,
            related,
            cited_in,
            appears_in_code: scanCodeAppearances(hit.term, repoRoot),
          });
        }

        const fuzzyQuery = normQ || rawTerm;
        const collapse = (s: string) => s.toLowerCase().replace(/\s+/g, "");
        const cq = collapse(fuzzyQuery);
        const ranked =
          cq.length > 0
            ? fuzzyFind(cq, entries, (e) => collapse(e.term), {
                threshold: 0.4,
                maxResults: 5,
              })
            : [];

        if (ranked[0] && ranked[0].score < FUZZY_STRONG_THRESHOLD) {
          const best = ranked[0].item;
          const { related, cited_in } = graphDataFor(best.term);
          return jsonResult({
            term: best.term,
            definition: best.definition,
            specReference: best.specReference,
            category: best.category,
            matchType: "fuzzy" as const,
            suggestion: `Did you mean '${best.term}'?`,
            related,
            cited_in,
            appears_in_code: scanCodeAppearances(best.term, repoRoot),
          });
        }

        const available_terms = [...new Set(entries.map((e) => e.term))].sort();
        return jsonResult({
          error: "term_not_found",
          message: `No glossary entry for '${rawTerm}'.`,
          available_terms,
          suggestions: ranked.slice(0, 3).map((r) => r.item.term),
        });
      }),
  );
}
