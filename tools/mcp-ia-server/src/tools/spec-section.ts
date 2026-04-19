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
import { wrapTool, type EnvelopeMeta } from "../envelope.js";

const DEFAULT_MAX_CHARS = 3000;
const FUZZY_STRONG_THRESHOLD = 0.3;

const stringOrNumber = z.union([z.string(), z.number()]);

/** Raw MCP arguments (canonical params only since Stage 2.3). */
export type SpecSectionRawArgs = {
  spec?: string;
  /** Rejected alias — kept in type for runtime rejection in normalizeSpecSectionInput. */
  key?: string;
  document_key?: string;
  doc?: string;
  section?: string | number;
  /** Rejected aliases — kept in type for runtime rejection. */
  section_heading?: string | number;
  section_id?: string | number;
  heading?: string | number;
  max_chars?: number;
  /** Rejected alias — kept in type for runtime rejection. */
  maxChars?: number;
};

/**
 * Validates and returns canonical `spec` + `section` + `max_chars`.
 * Throws `{ code: "invalid_input" }` (caught by `wrapTool`) on missing or alias params.
 */
export function normalizeSpecSectionInput(
  args: SpecSectionRawArgs | undefined,
): { spec: string; section: string; max_chars: number } {
  const a = args ?? {};

  // Reject legacy aliases with canonical-name hints (Stage 2.3 TECH-426).
  if (a.key !== undefined)
    throw { code: "invalid_input" as const, message: "Unknown param 'key'. Canonical: 'spec'." };
  if (a.document_key !== undefined)
    throw { code: "invalid_input" as const, message: "Unknown param 'document_key'. Canonical: 'spec'." };
  if (a.doc !== undefined)
    throw { code: "invalid_input" as const, message: "Unknown param 'doc'. Canonical: 'spec'." };
  if (a.section_heading !== undefined)
    throw { code: "invalid_input" as const, message: "Unknown param 'section_heading'. Canonical: 'section'." };
  if (a.section_id !== undefined)
    throw { code: "invalid_input" as const, message: "Unknown param 'section_id'. Canonical: 'section'." };
  if (a.heading !== undefined)
    throw { code: "invalid_input" as const, message: "Unknown param 'heading'. Canonical: 'section'." };
  if (a.maxChars !== undefined)
    throw { code: "invalid_input" as const, message: "Unknown param 'maxChars'. Canonical: 'max_chars'." };

  const toStr = (v: unknown): string =>
    v === undefined || v === null ? "" : String(v).trim();

  const spec = toStr(a.spec);
  const section = toStr(a.section);
  const max_chars =
    typeof a.max_chars === "number" && Number.isFinite(a.max_chars)
      ? a.max_chars
      : DEFAULT_MAX_CHARS;

  if (!spec || !section) {
    throw {
      code: "invalid_input" as const,
      message: "Provide `spec` (document key, e.g. geo) and `section` (e.g. \"14\" or \"14.5\").",
      hint: "Both `spec` and `section` are required.",
    };
  }

  return { spec, section, max_chars };
}

const inputShape = {
  spec: z
    .string()
    .optional()
    .describe(
      "Document key or filename (e.g. 'geo', 'isometric-geography-system'). Required.",
    ),
  key: z
    .string()
    .optional()
    .describe("Rejected alias — use 'spec' instead. Returns invalid_input error."),
  document_key: z.string().optional().describe("Rejected alias — use 'spec' instead. Returns invalid_input error."),
  doc: z.string().optional().describe("Rejected alias — use 'spec' instead. Returns invalid_input error."),
  section: stringOrNumber
    .optional()
    .describe(
      "Section ID (e.g. '13.4'), title substring, or fuzzy heading text. Required. Numbers coerced to strings.",
    ),
  section_heading: stringOrNumber
    .optional()
    .describe("Rejected alias — use 'section' instead. Returns invalid_input error."),
  section_id: stringOrNumber.optional().describe("Rejected alias — use 'section' instead. Returns invalid_input error."),
  heading: stringOrNumber.optional().describe("Rejected alias — use 'section' instead. Returns invalid_input error."),
  max_chars: z
    .number()
    .optional()
    .describe("Maximum characters to return. Default: 3000. Truncates at the end."),
  maxChars: z.number().optional().describe("Rejected alias — use 'max_chars' instead. Returns invalid_input error."),
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

/** Success payload shape for `spec_section` (inner content fields). */
export type SpecSectionSuccessPayload = {
  key: string;
  sectionId: string;
  title: string;
  lineStart: number;
  lineEnd: number;
  content: string;
  matchType?: "fuzzy";
  suggestion?: string;
};

/** Legacy catch-all for `spec_sections` batch inner results (still Record-shaped). */
export type SpecSectionPayload = Record<string, unknown>;

/**
 * Extract one spec section (same resolution rules as the `spec_section` MCP tool).
 * Shared by `spec_section` and `spec_sections` batch tool (TECH-58).
 *
 * Phase 1 (TECH-398): throws typed `{ code: ErrorCode, ... }` on not-found paths so
 * callers using `wrapTool` receive structured `{ ok: false, error: { code } }` envelopes.
 * Phase 2 (TECH-398): returns `{ ok: true, payload, meta }` envelope on success so
 * `spec_section` meta fields flow through `EnvelopeMeta`.
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
    throw {
      code: "spec_not_found" as const,
      message: `No document found for key '${specKey}'. Use list_specs to see available documents.`,
      hint: "Call list_specs to retrieve available keys.",
      details: { available_keys },
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
      throw {
        code: "section_not_found" as const,
        message: `No section found for '${sectionQ}' in '${entry.key}'. Use spec_outline to list sections.`,
        hint: "Call spec_outline to see available sections, or try a shorter title substring.",
        details: {
          available_sections: topLevelSectionHints(entry, 20),
          suggestions: fuzzy.map((f) => f.item.title),
        },
      };
    }
  }

  if (match !== null && "kind" in match) {
    throw {
      code: "section_not_found" as const,
      message: `Multiple sections match '${sectionQ}'. Narrow the section id or title.`,
      details: { available_sections: match.candidates.slice(0, 20) },
    };
  }

  const { heading, content, lineStart, lineEnd } = match as ExtractSectionMatch;
  const { text, truncated, totalChars } = truncateContent(content, maxChars);

  // Phase 2 (TECH-398): return envelope shape so wrapTool passes meta through EnvelopeMeta.
  // EnvelopeMeta extended with section_id / line_range / truncated / total_chars (Decision Log 2026-04-18).
  const meta: EnvelopeMeta = {
    section_id: heading.sectionId,
    line_range: [lineStart, lineEnd],
    truncated,
    total_chars: totalChars,
  };

  const payload: SpecSectionSuccessPayload = {
    key: entry.key,
    sectionId: heading.sectionId,
    title: heading.title,
    lineStart,
    lineEnd,
    content: text,
    ...(fuzzySuggestion
      ? {
          matchType: "fuzzy" as const,
          suggestion: fuzzySuggestion,
        }
      : {}),
  };

  // Return pre-shaped envelope so wrapTool passes it through unchanged.
  return { ok: true, payload, meta } as unknown as SpecSectionPayload;
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
        "Retrieve one section from a spec, rule, or root doc. Required: `spec` (document key, e.g. 'geo') and `section` (heading id or title). Use spec_outline first; supports doc keys (geo, roads) and fuzzy heading matches on typos.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("spec_section", async () => {
        // wrapTool catches throws from normalizeSpecSectionInput (invalid_input) and
        // runSpecSectionExtract (spec_not_found / section_not_found) → { ok: false, error }.
        const envelope = await wrapTool(
          async (input: SpecSectionRawArgs | undefined) => {
            const normalized = normalizeSpecSectionInput(input);
            const { spec: specKey, section: sectionQ, max_chars: maxChars } =
              normalized;
            return runSpecSectionExtract(registry, specKey, sectionQ, maxChars);
          },
        )(args as SpecSectionRawArgs | undefined);

        return jsonResult(envelope);
      }),
  );
}
