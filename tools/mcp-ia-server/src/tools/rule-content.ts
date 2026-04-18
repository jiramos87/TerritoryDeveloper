/**
 * MCP tools: rule_content — Markdown body of a `.mdc` rule (frontmatter stripped).
 *             rule_section  — Slice a named heading section from a `.mdc` rule (symmetric to spec_section).
 */

import fs from "node:fs";
import matter from "gray-matter";
import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import type { SpecRegistryEntry } from "../parser/types.js";
import { findRuleEntry } from "../config.js";
import { formatRuleGlobs } from "./list-rules.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool, type EnvelopeMeta } from "../envelope.js";
import {
  buildExtractMatch,
  extractSection,
  flattenHeadingTree,
  parseDocument,
  type ExtractSectionMatch,
} from "../parser/markdown-parser.js";
import { fuzzyFindByHeadingTitle } from "../parser/fuzzy.js";

const DEFAULT_MAX_CHARS = 3000;
const FUZZY_STRONG_THRESHOLD = 0.3;

// ---------------------------------------------------------------------------
// Shared helpers
// ---------------------------------------------------------------------------

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

/** Build available_rules list from registry (for error details). */
function availableRulesList(registry: SpecRegistryEntry[]) {
  return registry
    .filter((e) => e.category === "rule")
    .map((e) => ({ key: e.key, description: e.description }))
    .sort((a, b) => a.key.localeCompare(b.key));
}

// ---------------------------------------------------------------------------
// rule_content
// ---------------------------------------------------------------------------

const ruleContentInputShape = {
  rule: z
    .string()
    .describe(
      "Key or filename (e.g. 'invariants', 'coding-conventions', 'roads').",
    ),
  max_chars: z
    .number()
    .optional()
    .describe("Maximum characters to return. Default: 3000."),
};

/**
 * Register the rule_content tool.
 */
export function registerRuleContent(
  server: McpServer,
  registry: SpecRegistryEntry[],
): void {
  server.registerTool(
    "rule_content",
    {
      description:
        "Retrieve the full Markdown body of a Cursor rule (.mdc), without YAML frontmatter.",
      inputSchema: ruleContentInputShape,
    },
    async (args) =>
      runWithToolTiming("rule_content", async () => {
        const envelope = await wrapTool(
          async (input: typeof args) => {
            const ruleKey = input?.rule ?? "";
            const maxChars = input?.max_chars ?? DEFAULT_MAX_CHARS;

            const entry = findRuleEntry(registry, ruleKey);
            if (!entry) {
              throw {
                code: "spec_not_found" as const,
                message: `No rule found for '${ruleKey}'. Use list_rules to see available rules.`,
                hint: "Call list_rules to retrieve available rule keys.",
                details: { available_rules: availableRulesList(registry) },
              };
            }

            const raw = fs.readFileSync(entry.filePath, "utf8");
            const { data, content } = matter(raw);
            const d = data as Record<string, unknown>;
            const description =
              typeof d.description === "string"
                ? d.description
                : entry.description;
            const alwaysApply =
              typeof d.alwaysApply === "boolean" ? d.alwaysApply : false;
            const globs = formatRuleGlobs(d.globs);
            const body = content.trimStart();
            const { text, truncated, totalChars } = truncateContent(
              body,
              maxChars,
            );

            return {
              key: entry.key,
              fileName: entry.fileName,
              description,
              alwaysApply,
              globs,
              content: text,
              truncated,
              totalChars,
            };
          },
        )(args);

        return jsonResult(envelope);
      }),
  );
}

// ---------------------------------------------------------------------------
// rule_section
// ---------------------------------------------------------------------------

const stringOrNumber = z.union([z.string(), z.number()]);

const ruleSectionInputShape = {
  rule: z
    .string()
    .optional()
    .describe(
      "Key or filename of the rule (e.g. 'invariants', 'roads'). Short keys preferred.",
    ),
  section: stringOrNumber
    .optional()
    .describe(
      "Section ID (e.g. '3'), title substring, or fuzzy heading text. Numbers are coerced to strings.",
    ),
  section_heading: stringOrNumber
    .optional()
    .describe("Alias for `section` (same meaning)."),
  heading: stringOrNumber.optional().describe("Alias for `section`."),
  max_chars: z
    .number()
    .optional()
    .describe(
      "Maximum characters to return. Default: 3000. Truncates at the end.",
    ),
  maxChars: z.number().optional().describe("Alias for `max_chars`."),
};

/** Raw MCP arguments for rule_section. */
export type RuleSectionRawArgs = {
  rule?: string;
  section?: string | number;
  section_heading?: string | number;
  heading?: string | number;
  max_chars?: number;
  maxChars?: number;
};

/** Normalize rule_section input; throws `{ code: "invalid_input" }` on missing args. */
export function normalizeRuleSectionInput(
  args: RuleSectionRawArgs | undefined,
): { rule: string; section: string; max_chars: number } {
  const a = args ?? {};
  const toStr = (v: unknown): string =>
    v === undefined || v === null ? "" : String(v).trim();

  const rule = toStr(a.rule);
  const section = toStr(a.section ?? a.section_heading ?? a.heading);
  const maxRaw = a.max_chars ?? a.maxChars;
  const max_chars =
    typeof maxRaw === "number" && Number.isFinite(maxRaw)
      ? maxRaw
      : DEFAULT_MAX_CHARS;

  if (!rule || !section) {
    throw {
      code: "invalid_input" as const,
      message:
        "Provide `rule` (rule key, e.g. 'invariants') and `section` (heading title or id). " +
        "Aliases accepted: `section_heading`/`heading` for section; `maxChars` for max_chars.",
      hint: "Both `rule` and `section` are required.",
    };
  }

  return { rule, section, max_chars };
}

/** Top-level section hints for error details (mirrors spec_section helper). */
function topLevelRuleSectionHints(
  entry: SpecRegistryEntry,
  limit: number,
): { sectionId: string; title: string }[] {
  const doc = parseDocument(entry.filePath);
  const roots = doc.headings.slice(0, limit);
  return roots.map((h) => ({ sectionId: h.sectionId, title: h.title }));
}

/**
 * Extract one rule section — mirrors runSpecSectionExtract but loads via parseDocument
 * (handles .mdc frontmatter stripping natively) vs gray-matter in rule_content.
 * Returns pre-shaped `{ ok: true, payload, meta }` envelope (caught by wrapTool passthrough).
 *
 * Phase 1 (TECH-399): sibling to runSpecSectionExtract; consolidation deferred to Stage 2.3 T2.3.2.
 */
export function runRuleSectionExtract(
  registry: SpecRegistryEntry[],
  ruleKey: string,
  sectionQ: string,
  maxChars: number,
): Record<string, unknown> {
  const entry = findRuleEntry(registry, ruleKey);
  if (!entry) {
    throw {
      code: "spec_not_found" as const,
      message: `No rule found for '${ruleKey}'. Use list_rules to see available rules.`,
      hint: "Call list_rules to retrieve available rule keys.",
      details: { available_rules: availableRulesList(registry) },
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
        message: `No section found for '${sectionQ}' in rule '${entry.key}'. Use list_rules to browse rules, or try a shorter title substring.`,
        hint: "Try a shorter title substring.",
        details: {
          available_sections: topLevelRuleSectionHints(entry, 20),
          suggestions: fuzzy.map((f) => f.item.title),
        },
      };
    }
  }

  if (match !== null && "kind" in match) {
    throw {
      code: "section_not_found" as const,
      message: `Multiple sections match '${sectionQ}'. Narrow the section id or title.`,
      details: { available_sections: (match as { candidates: unknown[] }).candidates.slice(0, 20) },
    };
  }

  const { heading, content, lineStart, lineEnd } = match as ExtractSectionMatch;
  const { text, truncated, totalChars } = truncateContent(content, maxChars);

  const meta: EnvelopeMeta = {
    section_id: heading.sectionId,
    line_range: [lineStart, lineEnd],
    truncated,
    total_chars: totalChars,
  };

  const payload = {
    key: entry.key,
    sectionId: heading.sectionId,
    title: heading.title,
    lineStart,
    lineEnd,
    content: text,
    ...(fuzzySuggestion
      ? { matchType: "fuzzy" as const, suggestion: fuzzySuggestion }
      : {}),
  };

  // Pre-shaped envelope — wrapTool passes it through unchanged (detects { ok: true }).
  return { ok: true, payload, meta };
}

/**
 * Register the rule_section tool.
 */
export function registerRuleSection(
  server: McpServer,
  registry: SpecRegistryEntry[],
): void {
  server.registerTool(
    "rule_section",
    {
      description:
        "Retrieve one section from a Cursor rule (.mdc), without YAML frontmatter. Required: `rule` (key) and `section` (heading title or id). Supports fuzzy heading matches. Symmetric to spec_section.",
      inputSchema: ruleSectionInputShape,
    },
    async (args) =>
      runWithToolTiming("rule_section", async () => {
        const envelope = await wrapTool(
          async (input: RuleSectionRawArgs | undefined) => {
            const normalized = normalizeRuleSectionInput(input);
            const { rule: ruleKey, section: sectionQ, max_chars: maxChars } =
              normalized;
            return runRuleSectionExtract(registry, ruleKey, sectionQ, maxChars);
          },
        )(args as RuleSectionRawArgs | undefined);

        return jsonResult(envelope);
      }),
  );
}
