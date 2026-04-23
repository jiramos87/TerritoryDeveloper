/**
 * MCP tool: verify_classify
 * Input: { command: string, exit_code: number, stderr: string, stdout: string }
 * Output: { failure_enum: VerifyFailure, suggested_recovery: Recovery, stderr_excerpt: string }
 * Classifies verify-loop failures and returns structured recovery suggestions.
 */

import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";

type VerifyFailure =
  | "NONE"
  | "STALE_LOCK"
  | "BRIDGE_TIMEOUT"
  | "COMPILE_ERROR"
  | "SCHEMA_DRIFT"
  | "BACKLOG_YAML_INVALID"
  | "DEAD_PROJECT_SPEC"
  | "DB_UNCONFIGURED"
  | "UNKNOWN";

interface Recovery {
  action: string;
  retry: boolean;
  retry_modifier?: string;
}

const inputShape = {
  command: z.string().describe("The command that was run."),
  exit_code: z.number().int().describe("Exit code of the command."),
  stderr: z.string().describe("Stderr output from the command."),
  stdout: z.string().describe("Stdout output from the command."),
};

function classify(
  exit_code: number,
  stderr: string,
  stdout: string,
): { failure_enum: VerifyFailure; suggested_recovery: Recovery } {
  if (exit_code === 0) {
    return {
      failure_enum: "NONE",
      suggested_recovery: { action: "none", retry: false },
    };
  }

  if (/UnityLockfile|another Unity instance/i.test(stderr)) {
    return {
      failure_enum: "STALE_LOCK",
      suggested_recovery: {
        action: "rm -f Temp/UnityLockfile",
        retry: true,
        retry_modifier: "once",
      },
    };
  }

  if (/bridge.*timeout|ETIMEDOUT/i.test(stderr)) {
    return {
      failure_enum: "BRIDGE_TIMEOUT",
      suggested_recovery: {
        action: "retry with timeout_ms doubled, cap 60000",
        retry: true,
        retry_modifier: "double_timeout",
      },
    };
  }

  if (/error CS[0-9]+/.test(stderr) || /error CS[0-9]+/.test(stdout)) {
    return {
      failure_enum: "COMPILE_ERROR",
      suggested_recovery: {
        action: "surface CS error lines; fix before retry",
        retry: false,
      },
    };
  }

  if (/validate:master-plan-status/.test(stderr) || /validate:master-plan-status/.test(stdout)) {
    return {
      failure_enum: "SCHEMA_DRIFT",
      suggested_recovery: {
        action: "surface stderr; fix master-plan status before retry",
        retry: false,
      },
    };
  }

  if (/validate:backlog-yaml/.test(stderr) || /validate:backlog-yaml/.test(stdout)) {
    return {
      failure_enum: "BACKLOG_YAML_INVALID",
      suggested_recovery: {
        action: "surface stderr; fix backlog yaml before retry",
        retry: false,
      },
    };
  }

  if (/validate:dead-project-specs/.test(stderr) || /validate:dead-project-specs/.test(stdout)) {
    return {
      failure_enum: "DEAD_PROJECT_SPEC",
      suggested_recovery: {
        action: "surface stderr; fix dead project spec reference before retry",
        retry: false,
      },
    };
  }

  if (/DATABASE_URL|pg_connect/i.test(stderr) || /DATABASE_URL|pg_connect/i.test(stdout)) {
    return {
      failure_enum: "DB_UNCONFIGURED",
      suggested_recovery: {
        action: "fall back to Path A (skip Path B bridge)",
        retry: true,
        retry_modifier: "path_a_only",
      },
    };
  }

  return {
    failure_enum: "UNKNOWN",
    suggested_recovery: {
      action: "escalate verbatim",
      retry: false,
    },
  };
}

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

export function registerVerifyClassify(server: McpServer): void {
  server.registerTool(
    "verify_classify",
    {
      description:
        "Classify a verify-loop failure by exit code + stderr pattern. Returns {failure_enum, suggested_recovery, stderr_excerpt}.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("verify_classify", async () => {
        const envelope = await wrapTool(
          async (input: { command: string; exit_code: number; stderr: string; stdout: string }) => {
            const { failure_enum, suggested_recovery } = classify(
              input.exit_code,
              input.stderr,
              input.stdout,
            );
            const stderr_excerpt = input.stderr.slice(0, 500);
            return { failure_enum, suggested_recovery, stderr_excerpt };
          },
        )(args as { command: string; exit_code: number; stderr: string; stdout: string });
        return jsonResult(envelope);
      }),
  );
}
