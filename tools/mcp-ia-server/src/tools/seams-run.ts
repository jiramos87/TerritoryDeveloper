/**
 * MCP tool: seams_run — Phase E dispatch (DEC-A19 recipe-runner).
 *
 * Loads tools/seams/{name}/{input,output}.schema.json and validates input.
 * When dispatch_mode="subagent" (default) and CLAUDE_CODE_SUBAGENT_AVAILABLE
 * env var is set, invokes the plan-covered subagent for LLM dispatch and
 * validates the output envelope. When env var absent, returns success with
 * dispatch_unavailable:true so the recipe step can fall back gracefully.
 *
 * Modes (legacy validate-only path still supported):
 *   - mode="validate-input"   → check input only; payload returns the validated input.
 *   - mode="validate-output"  → check output only; payload returns the validated output.
 *   - mode="validate-pair"    → check both input + output; payload returns both.
 *
 * dispatch_mode:
 *   - "subagent"      → invoke SubagentDispatchHandler (injectable for tests); fallback
 *                        to dispatch_unavailable when handler not set or env gate absent.
 *   - "validate-only" → schema validation only, no LLM call (legacy + dry-run path).
 *
 * Inputs the tool will reject with `invalid_input`:
 *   - unknown seam name (no tools/seams/{name} dir)
 *   - schema file missing
 *   - payload fails AJV validation
 */

import { z } from "zod";
import fs from "node:fs";
import path from "node:path";
import { Ajv2020 } from "ajv/dist/2020.js";
import draft7MetaSchema from "ajv/dist/refs/json-schema-draft-07.json" with { type: "json" };

type AjvInstance = InstanceType<typeof Ajv2020>;
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { resolveRepoRoot } from "../config.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";

const SEAM_NAMES = [
  "author-spec-body",
  "author-plan-digest",
  "decompose-skeleton-stage",
  "align-glossary",
  "review-semantic-drift",
  "align-arch-decision",
] as const;

type SeamName = (typeof SEAM_NAMES)[number];

export interface TokenTotals {
  input_tokens: number;
  output_tokens: number;
  cache_read_tokens?: number;
  cache_creation_tokens?: number;
}

export type SubagentDispatchHandler = (
  seamName: SeamName,
  seamDir: string,
  input: unknown,
) => Promise<{ output: unknown; token_totals?: TokenTotals }>;

let injectedDispatchHandler: SubagentDispatchHandler | undefined;

export function setSubagentDispatchHandler(handler: SubagentDispatchHandler | undefined): void {
  injectedDispatchHandler = handler;
}

const inputShape = {
  name: z.enum(SEAM_NAMES).describe("Seam name (matches tools/seams/{name}/ dir)."),
  dispatch_mode: z
    .enum(["subagent", "validate-only"])
    .default("subagent")
    .describe("subagent=invoke LLM (default); validate-only=schema gate only (legacy/dry-run)."),
  mode: z
    .enum(["validate-input", "validate-output", "validate-pair"])
    .default("validate-pair")
    .describe("Validation mode (used when dispatch_mode=validate-only)."),
  input: z.unknown().optional().describe("Seam input payload (required for validate-input/pair and subagent dispatch)."),
  output: z.unknown().optional().describe("Seam output payload (required for validate-output/pair; ignored for subagent dispatch)."),
};

interface SeamRunArgs {
  name: SeamName;
  dispatch_mode?: "subagent" | "validate-only";
  mode?: "validate-input" | "validate-output" | "validate-pair";
  input?: unknown;
  output?: unknown;
}

interface SchemaLoad {
  inputSchema: unknown;
  outputSchema: unknown;
  seamDir: string;
}

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

function loadSeamSchemas(name: SeamName): SchemaLoad {
  const seamDir = path.join(resolveRepoRoot(), "tools", "seams", name);
  if (!fs.existsSync(seamDir)) {
    throw {
      code: "invalid_input" as const,
      message: `Unknown seam: ${name}`,
      hint: `Expected tools/seams/${name}/ to exist.`,
    };
  }
  const inputPath = path.join(seamDir, "input.schema.json");
  const outputPath = path.join(seamDir, "output.schema.json");
  for (const p of [inputPath, outputPath]) {
    if (!fs.existsSync(p)) {
      throw {
        code: "invalid_input" as const,
        message: `Seam ${name} missing schema file: ${path.basename(p)}`,
        hint: `Author tools/seams/${name}/${path.basename(p)} per tools/seams/README.md`,
      };
    }
  }
  return {
    inputSchema: JSON.parse(fs.readFileSync(inputPath, "utf8")),
    outputSchema: JSON.parse(fs.readFileSync(outputPath, "utf8")),
    seamDir,
  };
}

function buildAjv(): AjvInstance {
  const ajv = new Ajv2020({ allErrors: true, strict: false });
  ajv.addMetaSchema(draft7MetaSchema);
  return ajv;
}

function validateOrThrow(
  ajv: AjvInstance,
  schema: unknown,
  payload: unknown,
  side: "input" | "output",
  seamName: SeamName,
): void {
  const validate = ajv.compile(schema as object);
  const ok = validate(payload);
  if (!ok) {
    throw {
      code: "invalid_input" as const,
      message: `Seam ${seamName} ${side} failed schema validation`,
      hint: `See tools/seams/${seamName}/${side}.schema.json`,
      details: validate.errors,
    };
  }
}

async function dispatchSubagent(
  seamName: SeamName,
  seamDir: string,
  input: unknown,
): Promise<{ output: unknown; token_totals?: TokenTotals; dispatch_unavailable?: boolean }> {
  if (!process.env["CLAUDE_CODE_SUBAGENT_AVAILABLE"]) {
    return { output: undefined, dispatch_unavailable: true };
  }
  if (!injectedDispatchHandler) {
    return { output: undefined, dispatch_unavailable: true };
  }
  const result = await injectedDispatchHandler(seamName, seamDir, input);
  return result;
}

export function registerSeamsRun(server: McpServer): void {
  server.registerTool(
    "seams_run",
    {
      description:
        "Phase E: validate seam input/output payloads and optionally dispatch LLM via plan-covered subagent. " +
        "dispatch_mode=subagent (default) invokes LLM; dispatch_mode=validate-only is schema-gate only. " +
        "Seams: " + SEAM_NAMES.join(", ") + ". " +
        "Returns dispatch_unavailable:true when subagent env not available (not an error — recipe step falls back).",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("seams_run", async () => {
        const envelope = await wrapTool(async (input: SeamRunArgs) => {
          const dispatchMode = input.dispatch_mode ?? "subagent";
          const mode = input.mode ?? "validate-pair";
          const { inputSchema, outputSchema, seamDir } = loadSeamSchemas(input.name);
          const ajv = buildAjv();

          // Always validate input when provided
          if (input.input !== undefined) {
            validateOrThrow(ajv, inputSchema, input.input, "input", input.name);
          }

          if (dispatchMode === "subagent") {
            if (input.input === undefined) {
              throw {
                code: "invalid_input" as const,
                message: `Seam ${input.name}: 'input' payload required for dispatch_mode='subagent'`,
              };
            }
            const dispatched = await dispatchSubagent(input.name, seamDir, input.input);
            if (dispatched.dispatch_unavailable) {
              return {
                seam: input.name,
                dispatch_mode: "subagent",
                dispatch_unavailable: true,
                input: input.input,
              };
            }
            // Validate the subagent output
            validateOrThrow(ajv, outputSchema, dispatched.output, "output", input.name);
            const result: Record<string, unknown> = {
              seam: input.name,
              dispatch_mode: "subagent",
              input: input.input,
              output: dispatched.output,
              validated: true,
            };
            if (dispatched.token_totals) result["token_totals"] = dispatched.token_totals;
            return result;
          }

          // validate-only path (legacy)
          const result: {
            seam: SeamName;
            dispatch_mode: "validate-only";
            mode: typeof mode;
            input?: unknown;
            output?: unknown;
          } = { seam: input.name, dispatch_mode: "validate-only", mode };

          if (mode === "validate-input" || mode === "validate-pair") {
            if (input.input === undefined) {
              throw {
                code: "invalid_input" as const,
                message: `Seam ${input.name}: 'input' payload required for mode '${mode}'`,
              };
            }
            validateOrThrow(ajv, inputSchema, input.input, "input", input.name);
            result.input = input.input;
          }
          if (mode === "validate-output" || mode === "validate-pair") {
            if (input.output === undefined) {
              throw {
                code: "invalid_input" as const,
                message: `Seam ${input.name}: 'output' payload required for mode '${mode}'`,
              };
            }
            validateOrThrow(ajv, outputSchema, input.output, "output", input.name);
            result.output = input.output;
          }
          return result;
        })(args as SeamRunArgs);
        return jsonResult(envelope);
      }),
  );
}
