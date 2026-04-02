/**
 * MCP tool: glossary_lookup — exact + fuzzy term match in glossary.md.
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import type { SpecRegistryEntry } from "../parser/types.js";
import { parseGlossary } from "../parser/glossary-parser.js";
import { findEntryByKey } from "../config.js";
import { fuzzyFind, normalizeGlossaryQuery } from "../parser/fuzzy.js";
import { runWithToolTiming } from "../instrumentation.js";

const FUZZY_STRONG_THRESHOLD = 0.3;

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
        "Pass the term in English (glossary language); translate from the conversation if the user did not use English.",
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

        if (hit) {
          return jsonResult({
            term: hit.term,
            definition: hit.definition,
            specReference: hit.specReference,
            category: hit.category,
            matchType: "exact" as const,
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
          return jsonResult({
            term: best.term,
            definition: best.definition,
            specReference: best.specReference,
            category: best.category,
            matchType: "fuzzy" as const,
            suggestion: `Did you mean '${best.term}'?`,
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
