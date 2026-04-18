/**
 * MCP tool: spec_sections — batch retrieve multiple spec/rule/root slices (TECH-58).
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import type { SpecRegistryEntry } from "../parser/types.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
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
        // Outer wrapTool catches top-level throws (invalid_input when sections empty).
        const envelope = await wrapTool(
          async (input: { sections?: unknown[]; max_requests?: number }) => {
            const list = Array.isArray(input.sections) ? input.sections : [];
            const cap =
              typeof input.max_requests === "number" &&
              Number.isFinite(input.max_requests) &&
              input.max_requests > 0
                ? Math.min(Math.floor(input.max_requests), 50)
                : MAX_BATCH_DEFAULT;

            if (list.length === 0) {
              throw {
                code: "invalid_input" as const,
                message:
                  "Provide non-empty `sections` array of { spec, section, max_chars? } objects.",
              };
            }

            const results: Record<string, Record<string, unknown>> = {};
            const batch_warnings: Record<string, unknown>[] = [];

            const slice = list.slice(0, cap);
            if (list.length > cap) {
              batch_warnings.push({
                error: "batch_truncated",
                message: `Only first ${cap} requests processed; pass max_requests up to 50 or split the batch.`,
                omitted_count: list.length - cap,
              });
            }

            for (let i = 0; i < slice.length; i++) {
              // normalizeSpecSectionInput throws { code: "invalid_input" } on bad args —
              // catch per-item so a single bad entry doesn't abort the batch.
              let spec: string;
              let section: string;
              let max_chars: number;
              try {
                const normalized = normalizeSpecSectionInput(
                  slice[i] as SpecSectionRawArgs | undefined,
                );
                ({ spec, section, max_chars } = normalized);
              } catch (e) {
                const key = `__invalid__::${i}`;
                const err = e as { code?: string; message?: string };
                results[key] = {
                  ok: false,
                  error: { code: err.code ?? "invalid_input", message: err.message ?? String(e) },
                };
                continue;
              }
              const rk = resultKey(spec, section);
              // runSpecSectionExtract throws { code: "spec_not_found" | "section_not_found" } —
              // catch per-item so one missing section doesn't abort the batch (Stage 2.3 territory
              // for per-result envelope; for Stage 2.2 wrap outer call only per spec §7 Phase 2).
              try {
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
              } catch (e) {
                const err = e as { code?: string; message?: string; details?: unknown };
                const errEntry: Record<string, unknown> = {
                  ok: false,
                  error: {
                    code: err.code ?? "internal_error",
                    message: err.message ?? String(e),
                    ...(err.details !== undefined ? { details: err.details } : {}),
                  },
                };
                if (results[rk] !== undefined) {
                  results[`${rk}::__${i}`] = errEntry;
                } else {
                  results[rk] = errEntry;
                }
              }
            }

            return {
              count: slice.length,
              results,
              ...(batch_warnings.length ? { batch_warnings } : {}),
            };
          },
        )(args as { sections?: unknown[]; max_requests?: number });

        return jsonResult(envelope);
      }),
  );
}
