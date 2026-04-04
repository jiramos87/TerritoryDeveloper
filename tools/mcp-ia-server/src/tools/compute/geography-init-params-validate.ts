/**
 * MCP tool: geography_init_params_validate — Zod validation for interchange v1 (no Unity).
 */

import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { safeParseGeographyInitParamsV1 } from "territory-compute-lib";
import { z, type ZodIssue, ZodError } from "zod";
import { runWithToolTiming } from "../../instrumentation.js";
import { jsonToolResult } from "./jsonToolResult.js";

/** Accept any JSON object at top level (interchange fields are validated by geography Zod). */
const geographyInitParamsValidateInputSchema = z.record(z.string(), z.unknown());

export function registerGeographyInitParamsValidate(server: McpServer): void {
  server.registerTool(
    "geography_init_params_validate",
    {
      description:
        "Validate a Geography initialization interchange object (glossary: Geography initialization) against " +
        "docs/schemas/geography-init-params.v1.schema.json parity (artifact geography_init_params, schema_version 1). " +
        "Node-only — does not run Unity or mutate saves. Pass the document fields as the tool argument object.",
      inputSchema: geographyInitParamsValidateInputSchema,
    },
    async (args: unknown) =>
      runWithToolTiming("geography_init_params_validate", async () => {
        try {
          const parsed = geographyInitParamsValidateInputSchema.safeParse(args);
          if (!parsed.success) {
            return jsonToolResult({
              ok: false as const,
              error: {
                code: "VALIDATION_ERROR" as const,
                message: parsed.error.issues.map((i) => i.message).join("; "),
              },
            });
          }
          const result = safeParseGeographyInitParamsV1(parsed.data);
          if (!result.success) {
            return jsonToolResult({
              ok: false as const,
              error: {
                code: "VALIDATION_ERROR" as const,
                message: "Interchange validation failed",
                errors: result.error.issues.map((i: ZodIssue) => ({
                  path: i.path.join("."),
                  message: i.message,
                })),
              },
            });
          }
          return jsonToolResult({
            ok: true as const,
            data: { valid: true as const, artifact: result.data.artifact },
          });
        } catch (e) {
          if (e instanceof ZodError) {
            return jsonToolResult({
              ok: false as const,
              error: {
                code: "VALIDATION_ERROR" as const,
                message: e.issues.map((i) => i.message).join("; "),
              },
            });
          }
          const msg = e instanceof Error ? e.message : String(e);
          return jsonToolResult({
            ok: false as const,
            error: { code: "VALIDATION_ERROR" as const, message: msg },
          });
        }
      }),
  );
}
