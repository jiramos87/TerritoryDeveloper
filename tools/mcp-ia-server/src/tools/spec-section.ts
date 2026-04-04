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

const stringOrNumber = z.union([z.string(), z.number()]);

/** Raw MCP arguments (including mistaken names some models send). */
export type SpecSectionRawArgs = {
  spec?: string;
  /** Alias for `spec` (e.g. models send `key: "geo"`). */
  key?: string;
  document_key?: string;
  doc?: string;
  section?: string | number;
  /** Alias for `section`. */
  section_heading?: string | number;
  section_id?: string | number;
  heading?: string | number;
  max_chars?: number;
  maxChars?: number;
};

/**
 * Maps alternate parameter names and numeric section ids to canonical `spec` + `section` strings.
 * Returns an error message if both document key and section cannot be resolved.
 */
export function normalizeSpecSectionInput(
  args: SpecSectionRawArgs | undefined,
):
  | { spec: string; section: string; max_chars: number }
  | { error: string } {
  const a = args ?? {};
  const toStr = (v: unknown): string =>
    v === undefined || v === null ? "" : String(v).trim();

  const spec = toStr(
    a.spec ?? a.key ?? a.document_key ?? a.doc,
  );
  const section = toStr(
    a.section ?? a.section_heading ?? a.section_id ?? a.heading,
  );

  const maxRaw = a.max_chars ?? a.maxChars;
  const max_chars =
    typeof maxRaw === "number" && Number.isFinite(maxRaw)
      ? maxRaw
      : DEFAULT_MAX_CHARS;

  if (!spec || !section) {
    return {
      error:
        "Provide `spec` (document key or alias, e.g. geo) and `section` (e.g. \"14\" or \"14.5\"). " +
        "Aliases accepted: `key`/`document_key`/`doc` for spec; `section_heading`/`section_id`/`heading` for section. " +
        "Numeric `section` values are coerced to strings.",
    };
  }

  return { spec, section, max_chars };
}

const inputShape = {
  spec: z
    .string()
    .optional()
    .describe(
      "Document key, alias, or filename (e.g. 'geo', 'isometric-geography-system'). Prefer this over `key`.",
    ),
  key: z
    .string()
    .optional()
    .describe(
      "Alias for `spec` when the model sends `key` instead (same meaning).",
    ),
  document_key: z.string().optional().describe("Alias for `spec`."),
  doc: z.string().optional().describe("Alias for `spec`."),
  section: stringOrNumber
    .optional()
    .describe(
      "Section ID (e.g. '13.4'), title substring, or fuzzy heading text. Numbers are coerced to strings.",
    ),
  section_heading: stringOrNumber
    .optional()
    .describe("Alias for `section` (same meaning)."),
  section_id: stringOrNumber.optional().describe("Alias for `section`."),
  heading: stringOrNumber.optional().describe("Alias for `section`."),
  max_chars: z
    .number()
    .optional()
    .describe("Maximum characters to return. Default: 3000. Truncates at the end."),
  maxChars: z.number().optional().describe("Alias for `max_chars`."),
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

/** Payload shape returned as JSON from `spec_section` / `spec_sections` (success or error). */
export type SpecSectionPayload = Record<string, unknown>;

/**
 * Extract one spec section (same resolution rules as the `spec_section` MCP tool).
 * Shared by `spec_section` and `spec_sections` batch tool (TECH-58).
 */
export function runSpecSectionExtract(
  registry: SpecRegistryEntry[],
  specKey: string,
  sectionQ: string,
  maxChars: number,
): SpecSectionPayload {
  const entry = findEntryForSpecDoc(registry, specKey);
  if (!entry) {
    const available_keys = registry.map((e) => e.key).sort();
    return {
      error: "unknown_spec",
      message: `No document found for key '${specKey}'. Use list_specs to see available documents.`,
      available_keys,
    };
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
      return {
        error: "unknown_section",
        message: `No section found for '${sectionQ}' in '${entry.key}'. Use spec_outline to list sections.`,
        available_sections: topLevelSectionHints(entry, 20),
        suggestions: fuzzy.map((f) => f.item.title),
      };
    }
  }

  if (match !== null && "kind" in match) {
    return {
      error: "unknown_section",
      message: `Multiple sections match '${sectionQ}'. Narrow the section id or title.`,
      available_sections: match.candidates.slice(0, 20),
    };
  }

  const { heading, content, lineStart, lineEnd } = match as ExtractSectionMatch;
  const { text, truncated, totalChars } = truncateContent(content, maxChars);

  return {
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
  };
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
        "Retrieve one section from a spec, rule, or root doc. Required: document in `spec` (or alias `key`/`doc`) and target in `section` (or alias `section_heading`). Use spec_outline first; supports doc aliases (geo, roads) and fuzzy heading matches on typos.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("spec_section", async () => {
        const normalized = normalizeSpecSectionInput(
          args as SpecSectionRawArgs | undefined,
        );
        if ("error" in normalized) {
          return jsonResult({
            error: "invalid_arguments",
            message: normalized.error,
          });
        }
        const { spec: specKey, section: sectionQ, max_chars: maxChars } =
          normalized;
        return jsonResult(
          runSpecSectionExtract(registry, specKey, sectionQ, maxChars),
        );
      }),
  );
}
