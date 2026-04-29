/**
 * MCP tool: seams_run — Phase A scaffold (DEC-A19 recipe-runner).
 *
 * Validation-only entrypoint. Loads tools/seams/{name}/{input,output}.schema.json
 * and validates the supplied input + output payloads against them. No subagent
 * dispatch yet — Phase B introduces the recipe engine + plan-covered subagent
 * regen layer that fills in the LLM call. Today this tool is the contract gate
 * that future seam invocations will pass through.
 *
 * Modes:
 *   - mode="validate-input"   → check input only; payload returns the validated input.
 *   - mode="validate-output"  → check output only; payload returns the validated output.
 *   - mode="validate-pair"    → check both input + output; payload returns both.
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
] as const;

type SeamName = (typeof SEAM_NAMES)[number];

const inputShape = {
  name: z.enum(SEAM_NAMES).describe("Seam name (matches tools/seams/{name}/ dir)."),
  mode: z
    .enum(["validate-input", "validate-output", "validate-pair"])
    .default("validate-pair")
    .describe("Validation mode."),
  input: z.unknown().optional().describe("Seam input payload (required for validate-input/pair)."),
  output: z.unknown().optional().describe("Seam output payload (required for validate-output/pair)."),
};

interface SeamRunArgs {
  name: SeamName;
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
  // Draft-07 schemas authored under tools/seams/ — Ajv2020 accepts them via
  // `strict: false`. No `format` keywords used in Phase A schemas.
  return new Ajv2020({ allErrors: true, strict: false });
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

export function registerSeamsRun(server: McpServer): void {
  server.registerTool(
    "seams_run",
    {
      description:
        "Phase A: validate seam input/output payloads against tools/seams/{name}/*.schema.json. " +
        "No LLM dispatch yet — pure schema-gate. Modes: validate-input | validate-output | validate-pair (default).",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("seams_run", async () => {
        const envelope = await wrapTool(async (input: SeamRunArgs) => {
          const mode = input.mode ?? "validate-pair";
          const { inputSchema, outputSchema } = loadSeamSchemas(input.name);
          const ajv = buildAjv();
          const result: {
            seam: SeamName;
            mode: typeof mode;
            input?: unknown;
            output?: unknown;
          } = { seam: input.name, mode };

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
