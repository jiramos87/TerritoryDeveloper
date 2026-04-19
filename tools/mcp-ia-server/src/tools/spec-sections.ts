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
  key: z.string().optional().describe("Rejected alias — use 'spec'. Returns invalid_input error."),
  document_key: z.string().optional().describe("Rejected alias — use 'spec'. Returns invalid_input error."),
  doc: z.string().optional().describe("Rejected alias — use 'spec'. Returns invalid_input error."),
  section: stringOrNumber.optional(),
  section_heading: stringOrNumber.optional().describe("Rejected alias — use 'section'. Returns invalid_input error."),
  section_id: stringOrNumber.optional().describe("Rejected alias — use 'section'. Returns invalid_input error."),
  heading: stringOrNumber.optional().describe("Rejected alias — use 'section'. Returns invalid_input error."),
  max_chars: z.number().optional(),
  maxChars: z.number().optional().describe("Rejected alias — use 'max_chars'. Returns invalid_input error."),
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
        "Batch variant of `spec_section`: fetch multiple spec / rule / root slices in one call. Returns partial-result shape `{ results, errors, meta.partial }` — one bad key does not fail the whole batch. Each element requires `spec` and `section` (canonical names only). Results keyed by `spec::section`.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("spec_sections", async () => {
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

            // Partial-result batch shape (Stage 2.3 TECH-428): separate results / errors maps.
            const results: Record<string, Record<string, unknown>> = {};
            const errors: Record<string, { code: string; message: string }> = {};

            const slice = list.slice(0, cap);

            for (let i = 0; i < slice.length; i++) {
              let spec: string;
              let section: string;
              let max_chars: number;
              try {
                const normalized = normalizeSpecSectionInput(
                  slice[i] as SpecSectionRawArgs | undefined,
                );
                ({ spec, section, max_chars } = normalized);
              } catch (e) {
                const errKey = `__invalid__::${i}`;
                const err = e as { code?: string; message?: string };
                errors[errKey] = { code: err.code ?? "invalid_input", message: err.message ?? String(e) };
                continue;
              }
              const rk = results[resultKey(spec, section)] !== undefined || errors[resultKey(spec, section)] !== undefined
                ? `${resultKey(spec, section)}::__${i}`
                : resultKey(spec, section);
              try {
                const inner = runSpecSectionExtract(registry, spec, section, max_chars) as unknown as {
                  ok: boolean;
                  payload: Record<string, unknown>;
                };
                results[rk] = inner.payload;
              } catch (e) {
                const err = e as { code?: string; message?: string };
                errors[rk] = { code: err.code ?? "internal_error", message: err.message ?? String(e) };
              }
            }

            const succeeded = Object.keys(results).length;
            const failed = Object.keys(errors).length;

            if (succeeded === 0 && failed > 0) {
              throw {
                code: "invalid_input" as const,
                message: `All ${failed} request(s) failed.`,
                hint: `Failed keys: ${Object.keys(errors).join(", ")}`,
                details: { errors },
              };
            }

            // Pre-shaped envelope — wrapTool passes it through unchanged.
            return {
              ok: true as const,
              payload: { results, errors },
              meta: { partial: { succeeded, failed } },
            };
          },
        )(args as { sections?: unknown[]; max_requests?: number });

        return jsonResult(envelope);
      }),
  );
}
