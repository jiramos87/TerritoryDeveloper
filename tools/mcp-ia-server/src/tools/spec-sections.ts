/**
 * MCP tool: spec_sections — batch retrieve multiple spec/rule/root slices (TECH-58).
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import type { SpecRegistryEntry } from "../parser/types.js";
import { runWithToolTiming } from "../instrumentation.js";
import {
  normalizeSpecSectionInput,
  type SpecSectionRawArgs,
  runSpecSectionExtract,
} from "./spec-section.js";

const MAX_BATCH_DEFAULT = 20;

const stringOrNumber = z.union([z.string(), z.number()]);

const requestItemSchema = z.object({
  spec: z.string().optional(),
  key: z.string().optional(),
  document_key: z.string().optional(),
  doc: z.string().optional(),
  section: stringOrNumber.optional(),
  section_heading: stringOrNumber.optional(),
  section_id: stringOrNumber.optional(),
  heading: stringOrNumber.optional(),
  max_chars: z.number().optional(),
  maxChars: z.number().optional(),
});

const inputShape = {
  sections: z
    .array(requestItemSchema)
    .describe(
      "Array of slice requests; each item uses the same fields as `spec_section` (spec + section required per item).",
    ),
  max_requests: z
    .number()
    .optional()
    .describe(
      `Hard cap on batch size (default ${MAX_BATCH_DEFAULT}). Server truncates with an error entry if exceeded.`,
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

function resultKey(spec: string, section: string): string {
  return `${spec}::${section}`;
}

/**
 * Register the spec_sections batch tool.
 */
export function registerSpecSections(
  server: McpServer,
  registry: SpecRegistryEntry[],
): void {
  server.registerTool(
    "spec_sections",
    {
      description:
        "Batch variant of `spec_section`: fetch multiple reference spec / rule / root slices in one call. Each element uses the same `spec`/`section` aliases as `spec_section`. Results are keyed by `spec::section`.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("spec_sections", async () => {
        const raw = args as {
          sections?: unknown[];
          max_requests?: number;
        };
        const list = Array.isArray(raw.sections) ? raw.sections : [];
        const cap =
          typeof raw.max_requests === "number" &&
          Number.isFinite(raw.max_requests) &&
          raw.max_requests > 0
            ? Math.min(Math.floor(raw.max_requests), 50)
            : MAX_BATCH_DEFAULT;

        if (list.length === 0) {
          return jsonResult({
            error: "invalid_arguments",
            message:
              "Provide non-empty `sections` array of { spec, section, max_chars? } objects.",
          });
        }

        const results: Record<string, Record<string, unknown>> = {};
        const errors: Record<string, unknown>[] = [];

        const slice = list.slice(0, cap);
        if (list.length > cap) {
          errors.push({
            error: "batch_truncated",
            message: `Only first ${cap} requests processed; pass max_requests up to 50 or split the batch.`,
            omitted_count: list.length - cap,
          });
        }

        for (let i = 0; i < slice.length; i++) {
          const normalized = normalizeSpecSectionInput(
            slice[i] as SpecSectionRawArgs | undefined,
          );
          if ("error" in normalized) {
            const key = `__invalid__::${i}`;
            results[key] = {
              error: "invalid_arguments",
              message: normalized.error,
            };
            continue;
          }
          const { spec, section, max_chars } = normalized;
          const rk = resultKey(spec, section);
          const payload = runSpecSectionExtract(
            registry,
            spec,
            section,
            max_chars,
          );
          if (results[rk] !== undefined) {
            results[`${rk}::__${i}`] = payload as Record<string, unknown>;
          } else {
            results[rk] = payload as Record<string, unknown>;
          }
        }

        return jsonResult({
          count: slice.length,
          results,
          ...(errors.length ? { batch_warnings: errors } : {}),
        });
      }),
  );
}
