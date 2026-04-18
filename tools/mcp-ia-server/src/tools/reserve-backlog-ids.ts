/**
 * MCP tool: reserve_backlog_ids — reserve one or more monotonic backlog ids via reserve-id.sh.
 *
 * Delegates to tools/scripts/reserve-id.sh (owns flock + id-counter.json mutation per invariant #13).
 * Never reimplements flock logic in Node.
 */

import { z } from "zod";
import { spawn } from "node:child_process";
import { fileURLToPath } from "node:url";
import { resolve, join } from "node:path";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";

const VALID_PREFIXES = ["TECH", "FEAT", "BUG", "ART", "AUDIO"] as const;
type Prefix = (typeof VALID_PREFIXES)[number];

const inputShape = {
  prefix: z
    .enum(VALID_PREFIXES)
    .describe("Backlog id prefix to reserve: TECH | FEAT | BUG | ART | AUDIO"),
  count: z
    .number()
    .int()
    .min(1)
    .max(50)
    .default(1)
    .describe("Number of ids to reserve (1..50 inclusive). Default 1."),
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

/**
 * Resolve REPO_ROOT: env override → walk up from this file's directory.
 * Mirrors the pattern used by config.ts resolveRepoRoot().
 */
function resolveRepoRoot(): string {
  const raw = process.env.REPO_ROOT;
  if (raw) return resolve(process.cwd(), raw);
  // Walk up from src/tools/ → src/ → mcp-ia-server/ → tools/ → repo root
  const thisDir = fileURLToPath(new URL(".", import.meta.url));
  return resolve(thisDir, "../../../../");
}

/**
 * Build env that includes Homebrew util-linux bin path so flock is available
 * on macOS even when the MCP server starts with a minimal PATH.
 * Mirrors tools/scripts/test/reserve-id-concurrent.sh PATH patch.
 */
function buildScriptEnv(): NodeJS.ProcessEnv {
  const homebrew =
    "/opt/homebrew/opt/util-linux/bin:/opt/homebrew/opt/util-linux/sbin";
  const existing = process.env.PATH ?? "";
  return {
    ...process.env,
    PATH: existing.includes("util-linux")
      ? existing
      : `${homebrew}:${existing}`,
  };
}

/**
 * Spawn tools/scripts/reserve-id.sh and collect stdout/stderr/exit.
 */
function spawnReserveScript(
  prefix: Prefix,
  count: number,
  repoRoot: string,
): Promise<{ stdout: string; stderr: string; exitCode: number }> {
  return new Promise((resolve) => {
    const scriptPath = join(repoRoot, "tools/scripts/reserve-id.sh");
    const child = spawn("bash", [scriptPath, prefix, String(count)], {
      cwd: repoRoot,
      env: buildScriptEnv(),
    });

    let stdout = "";
    let stderr = "";

    child.stdout.on("data", (chunk: Buffer) => {
      stdout += chunk.toString();
    });
    child.stderr.on("data", (chunk: Buffer) => {
      stderr += chunk.toString();
    });

    child.on("close", (code) => {
      resolve({ stdout, stderr, exitCode: code ?? 1 });
    });

    child.on("error", (err) => {
      resolve({ stdout, stderr: stderr + err.message, exitCode: 1 });
    });
  });
}

/**
 * Parse stdout into an array of ids; validate format and count.
 */
function parseIds(
  stdout: string,
  prefix: Prefix,
  expectedCount: number,
): { ids: string[] } | { error: string; partial_ids: string[] } {
  const idPattern = new RegExp(`^${prefix}-\\d+$`);
  const lines = stdout
    .split("\n")
    .map((l) => l.trim())
    .filter((l) => l.length > 0);

  const valid: string[] = [];
  const invalid: string[] = [];
  for (const line of lines) {
    if (idPattern.test(line)) {
      valid.push(line);
    } else {
      invalid.push(line);
    }
  }

  if (invalid.length > 0) {
    return {
      error: `RESERVE_FAILED: unexpected output lines: ${JSON.stringify(invalid)}`,
      partial_ids: valid,
    };
  }

  if (valid.length !== expectedCount) {
    return {
      error: `RESERVE_FAILED: expected ${expectedCount} ids, got ${valid.length}`,
      partial_ids: valid,
    };
  }

  return { ids: valid };
}

/**
 * Register the reserve_backlog_ids tool.
 */
export function registerReserveBacklogIds(server: McpServer): void {
  server.registerTool(
    "reserve_backlog_ids",
    {
      description:
        "Reserve one or more monotonic backlog ids (TECH/FEAT/BUG/ART/AUDIO) via tools/scripts/reserve-id.sh. Preserves invariant #13 — all id-counter mutations go through the script under flock. Returns { ids: string[] } on success or { code: 'RESERVE_FAILED', stderr, partial_ids } on failure.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("reserve_backlog_ids", async () => {
        const prefix = args?.prefix as Prefix;
        const count = (args?.count as number) ?? 1;
        const repoRoot = resolveRepoRoot();

        const { stdout, stderr, exitCode } = await spawnReserveScript(
          prefix,
          count,
          repoRoot,
        );

        if (exitCode !== 0) {
          return jsonResult({
            code: "RESERVE_FAILED",
            stderr: stderr.trim(),
            partial_ids: [],
          });
        }

        const parsed = parseIds(stdout, prefix, count);

        if ("error" in parsed) {
          return jsonResult({
            code: "RESERVE_FAILED",
            stderr: parsed.error,
            partial_ids: parsed.partial_ids,
          });
        }

        return jsonResult({ ids: parsed.ids });
      }),
  );
}
