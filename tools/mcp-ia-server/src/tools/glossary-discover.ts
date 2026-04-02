/**
 * MCP tool: glossary_discover — keyword-style discovery over glossary rows (term, definition, spec, category).
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import type { SpecRegistryEntry } from "../parser/types.js";
import { parseGlossary } from "../parser/glossary-parser.js";
import {
  findEntryByKey,
  findEntryForSpecDoc,
  SPEC_KEY_ALIASES,
} from "../config.js";
import { fuzzyFind, normalizeGlossaryQuery } from "../parser/fuzzy.js";
import { rankGlossaryDiscover } from "../parser/glossary-discover-rank.js";
import { runWithToolTiming } from "../instrumentation.js";

const DEFAULT_MAX_RESULTS = 10;
const HARD_CAP_MAX_RESULTS = 25;
const MAX_QUERY_CHARS = 2000;
const MAX_KEYWORD_STRING_CHARS = 200;
const MAX_KEYWORDS_COUNT = 50;

const HINT_NEXT_TOOLS =
  "For each match: call glossary_lookup with the exact `term`, then spec_section using `spec` (alias) " +
  "and a section id or heading from `specReference` when present. Use spec_outline on `spec` if the section is unclear.";

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

function preferredSpecAlias(canonicalKey: string): string {
  const aliasesForKey: string[] = [];
  for (const [alias, key] of Object.entries(SPEC_KEY_ALIASES)) {
    if (key === canonicalKey) aliasesForKey.push(alias);
  }
  if (aliasesForKey.length === 0) return canonicalKey;
  return [...aliasesForKey].sort((a, b) => a.length - b.length)[0] ?? canonicalKey;
}

/**
 * First registry key (and preferred short alias) resolvable from a glossary Spec cell.
 */
export function resolveSpecKeyFromReference(
  specReference: string,
  registry: SpecRegistryEntry[],
): { registryKey: string; spec: string } | undefined {
  const raw = specReference.match(/[a-z][a-z0-9.-]*/gi) ?? [];
  for (const r of raw) {
    const t = r.replace(/\.$/, "");
    if (/^\d+(\.\d+)*$/i.test(t)) continue;
    if (t.length < 2) continue;
    const entry = findEntryForSpecDoc(registry, t);
    if (entry?.category === "spec") {
      return {
        registryKey: entry.key,
        spec: preferredSpecAlias(entry.key),
      };
    }
  }
  return undefined;
}

function clampInt(n: unknown, fallback: number, min: number, max: number): number {
  if (typeof n !== "number" || !Number.isFinite(n)) return fallback;
  return Math.min(max, Math.max(min, Math.floor(n)));
}

function normalizeDiscoverArgs(args: {
  query?: string;
  keywords?: string[];
  q?: string;
  search?: string;
  terms?: string[];
  max_results?: number;
  maxResults?: number;
}): { queryText: string; maxResults: number } | { error: string } {
  const q =
    (args.query ?? args.q ?? args.search ?? "").trim();
  const kwFromArray = args.keywords ?? args.terms ?? [];
  const keywords = Array.isArray(kwFromArray)
    ? kwFromArray.map((k) => String(k).trim())
    : [];

  const parts: string[] = [];
  if (q) parts.push(q.slice(0, MAX_QUERY_CHARS));
  let kwCount = 0;
  for (const k of keywords) {
    if (kwCount >= MAX_KEYWORDS_COUNT) break;
    if (!k) continue;
    parts.push(k.slice(0, MAX_KEYWORD_STRING_CHARS));
    kwCount++;
  }

  const queryText = parts.join(" ").trim();
  if (!queryText) {
    return {
      error:
        "Provide non-empty `query` and/or `keywords` (string array). " +
        "Aliases: `q` / `search` for query; `terms` for keywords.",
    };
  }

  const maxRaw = args.max_results ?? args.maxResults;
  const maxResults = clampInt(
    maxRaw,
    DEFAULT_MAX_RESULTS,
    1,
    HARD_CAP_MAX_RESULTS,
  );

  return { queryText, maxResults };
}

const inputShape = {
  query: z
    .string()
    .optional()
    .describe(
      "Free-text keywords (multiple words allowed). Combined with `keywords`. Aliases: `q`, `search`.",
    ),
  keywords: z
    .array(z.string())
    .optional()
    .describe("Extra tokens or phrases. Alias: `terms`."),
  max_results: z
    .number()
    .optional()
    .describe(
      `Max matches to return (default ${DEFAULT_MAX_RESULTS}, hard cap ${HARD_CAP_MAX_RESULTS}). Alias: maxResults.`,
    ),
};

/**
 * Register the glossary_discover tool.
 */
export function registerGlossaryDiscover(
  server: McpServer,
  registry: SpecRegistryEntry[],
): void {
  server.registerTool(
    "glossary_discover",
    {
      description:
        "Discover canonical glossary terms from rough keywords: scores Term, Definition, Spec, and category text. " +
        "Use before glossary_lookup when you do not know the exact term name. Complements glossary_lookup (exact/fuzzy term).",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("glossary_discover", async () => {
        const a = args as Record<string, unknown>;
        const normalized = normalizeDiscoverArgs({
          query: typeof a.query === "string" ? a.query : undefined,
          keywords: Array.isArray(a.keywords) ? (a.keywords as string[]) : undefined,
          q: typeof a.q === "string" ? a.q : undefined,
          search: typeof a.search === "string" ? a.search : undefined,
          terms: Array.isArray(a.terms) ? (a.terms as string[]) : undefined,
          max_results: typeof a.max_results === "number" ? a.max_results : undefined,
          maxResults: typeof a.maxResults === "number" ? a.maxResults : undefined,
        });

        if ("error" in normalized) {
          return jsonResult({ error: "invalid_input", message: normalized.error });
        }

        const { queryText, maxResults } = normalized;

        const entry = findEntryByKey(registry, "glossary");
        if (!entry) {
          return jsonResult({
            error: "glossary_missing",
            message: "Glossary file is not registered.",
            matches: [],
          });
        }

        const entries = parseGlossary(entry.filePath);
        const ranked = rankGlossaryDiscover(entries, queryText, {
          maxResults,
        });

        if (ranked.length === 0) {
          const collapse = (s: string) =>
            normalizeGlossaryQuery(s).toLowerCase().replace(/\s+/g, "");
          const cq = collapse(queryText);
          const fuzzyRows =
            cq.length > 0
              ? fuzzyFind(cq, entries, (e) => collapse(e.term), {
                  threshold: 0.45,
                  maxResults: 5,
                })
              : [];
          const suggestions = fuzzyRows.map((r) => r.item.term);
          return jsonResult({
            matches: [] as unknown[],
            query_normalized: queryText,
            hint_next_tools: HINT_NEXT_TOOLS,
            message:
              "No glossary rows matched these keywords in term/definition/spec/category. " +
              "Try different words, router_for_task, or spec_outline.",
            suggestions,
          });
        }

        const matches = ranked.map((r) => {
          const specKeys = resolveSpecKeyFromReference(
            r.entry.specReference,
            registry,
          );
          return {
            term: r.entry.term,
            specReference: r.entry.specReference,
            category: r.entry.category,
            score: Math.round(r.score * 1000) / 1000,
            matchReasons: r.matchReasons,
            ...(specKeys
              ? { spec: specKeys.spec, registryKey: specKeys.registryKey }
              : {}),
          };
        });

        return jsonResult({
          matches,
          query_normalized: queryText,
          hint_next_tools: HINT_NEXT_TOOLS,
        });
      }),
  );
}
