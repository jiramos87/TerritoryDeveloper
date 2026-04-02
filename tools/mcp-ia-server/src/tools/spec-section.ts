/**
 * MCP tool: spec_section — body slice for one heading section; aliases + fuzzy title fallback.
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import type { SpecRegistryEntry } from "../parser/types.js";
import {
  buildExtractMatch,
  extractSection,
  flattenHeadingTree,
  parseDocument,
  type ExtractSectionMatch,
} from "../parser/markdown-parser.js";
import { fuzzyFindByHeadingTitle } from "../parser/fuzzy.js";
import { findEntryForSpecDoc } from "../config.js";
import { runWithToolTiming } from "../instrumentation.js";

const DEFAULT_MAX_CHARS = 3000;
const FUZZY_STRONG_THRESHOLD = 0.3;

const inputShape = {
  spec: z
    .string()
    .describe(
      "Key, alias, or filename (e.g. 'isometric-geography-system', 'geo', 'roads-system').",
    ),
  section: z
    .string()
    .describe(
      "Section ID (e.g. '13.4'), heading slug, title substring, or fuzzy heading text (e.g. 'bridge').",
    ),
  max_chars: z
    .number()
    .optional()
    .describe("Maximum characters to return. Default: 3000. Truncates at the end."),
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

function truncateContent(
  content: string,
  maxChars: number,
): { text: string; truncated: boolean; totalChars: number } {
  const totalChars = content.length;
  if (content.length <= maxChars) {
    return { text: content, truncated: false, totalChars };
  }
  const slice = content.slice(0, maxChars);
  const lastNl = slice.lastIndexOf("\n");
  const cut = lastNl > 0 ? slice.slice(0, lastNl) : slice;
  return { text: cut, truncated: true, totalChars };
}

function topLevelSectionHints(
  registryEntry: SpecRegistryEntry,
  limit: number,
): { sectionId: string; title: string }[] {
  const doc = parseDocument(registryEntry.filePath);
  const roots = doc.headings.slice(0, limit);
  return roots.map((h) => ({ sectionId: h.sectionId, title: h.title }));
}

/**
 * Register the spec_section tool.
 */
export function registerSpecSection(
  server: McpServer,
  registry: SpecRegistryEntry[],
): void {
  server.registerTool(
    "spec_section",
    {
      description:
        "Retrieve one section from a spec, rule, or root doc. Use spec_outline first; supports aliases (geo, roads) and fuzzy heading matches on typos.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("spec_section", async () => {
        const specKey = args?.spec ?? "";
        const sectionQ = args?.section ?? "";
        const maxChars = args?.max_chars ?? DEFAULT_MAX_CHARS;

        const entry = findEntryForSpecDoc(registry, specKey);
        if (!entry) {
          const available_keys = registry.map((e) => e.key).sort();
          return jsonResult({
            error: "unknown_spec",
            message: `No document found for key '${specKey}'. Use list_specs to see available documents.`,
            available_keys,
          });
        }

        const doc = parseDocument(entry.filePath);
        let match = extractSection(doc, sectionQ);
        let fuzzySuggestion: string | undefined;

        if (match == null) {
          const headingItems = flattenHeadingTree(doc.headings).map((h) => ({
            node: h,
            title: h.title,
          }));
          const fuzzy = fuzzyFindByHeadingTitle(
            sectionQ,
            headingItems,
            (x) => x.title,
            { threshold: 0.4, maxResults: 5, minTokenLength: 4 },
          );
          if (fuzzy[0] && fuzzy[0].score < FUZZY_STRONG_THRESHOLD) {
            const heading = fuzzy[0].item.node;
            match = buildExtractMatch(doc.filePath, heading);
            fuzzySuggestion = `Matched to '${heading.title}' (fuzzy).`;
          } else {
            return jsonResult({
              error: "unknown_section",
              message: `No section found for '${sectionQ}' in '${entry.key}'. Use spec_outline to list sections.`,
              available_sections: topLevelSectionHints(entry, 20),
              suggestions: fuzzy.map((f) => f.item.title),
            });
          }
        }

        if (match !== null && "kind" in match) {
          return jsonResult({
            error: "unknown_section",
            message: `Multiple sections match '${sectionQ}'. Narrow the section id or title.`,
            available_sections: match.candidates.slice(0, 20),
          });
        }

        const { heading, content, lineStart, lineEnd } = match as ExtractSectionMatch;
        const { text, truncated, totalChars } = truncateContent(content, maxChars);

        return jsonResult({
          key: entry.key,
          sectionId: heading.sectionId,
          title: heading.title,
          lineStart,
          lineEnd,
          content: text,
          truncated,
          totalChars,
          ...(fuzzySuggestion
            ? {
                matchType: "fuzzy" as const,
                suggestion: fuzzySuggestion,
              }
            : {}),
        });
      }),
  );
}
