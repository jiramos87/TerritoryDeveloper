/**
 * MCP tool: runtime_state — read / merge-write shared repo runtime state
 * (ia/state/runtime-state.json) under flock via tools/scripts/runtime-state-write.sh.
 *
 * Harness-agnostic: Cursor / Claude Code / any MCP client. Active task / stage live in
 * per-harness active-session JSON — not this file.
 */

import { z } from "zod";
import { readFileSync, existsSync, writeFileSync, unlinkSync } from "node:fs";
import { join } from "node:path";
import { tmpdir } from "node:os";
import { randomBytes } from "node:crypto";
import { spawnSync } from "node:child_process";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { resolveRepoRoot } from "../config.js";
import { runWithToolTiming } from "../instrumentation.js";

const PATCH_KEYS = [
  "last_verify_exit_code",
  "last_bridge_preflight_exit_code",
  "queued_test_scenario_id",
] as const;

const inputShape = {
  action: z
    .enum(["read", "write"])
    .describe('Read full JSON, or merge-write a patch ("write").'),
  patch: z
    .record(z.string(), z.unknown())
    .optional()
    .describe(
      'For action "write": shallow merge keys (last_verify_exit_code, last_bridge_preflight_exit_code, queued_test_scenario_id). updated_at is set automatically.',
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

function buildScriptEnv(repoRoot: string): NodeJS.ProcessEnv {
  const homebrew =
    "/opt/homebrew/opt/util-linux/bin:/opt/homebrew/opt/util-linux/sbin";
  const existing = process.env.PATH ?? "";
  return {
    ...process.env,
    REPO_ROOT: repoRoot,
    PATH: existing.includes("util-linux") ? existing : `${homebrew}:${existing}`,
  };
}

function readStateFile(repoRoot: string): { ok: true; data: unknown } | { ok: false; code: string; message: string } {
  const p = join(repoRoot, "ia/state/runtime-state.json");
  if (!existsSync(p)) {
    return { ok: true, data: null };
  }
  try {
    const raw = readFileSync(p, "utf8");
    return { ok: true, data: JSON.parse(raw) as unknown };
  } catch (e) {
    return {
      ok: false,
      code: "RUNTIME_STATE_READ_FAILED",
      message: e instanceof Error ? e.message : String(e),
    };
  }
}

function validatePatch(patch: Record<string, unknown>): string | null {
  const bad = Object.keys(patch).filter((k) => !PATCH_KEYS.includes(k as (typeof PATCH_KEYS)[number]));
  if (bad.length) return `Invalid patch keys: ${bad.join(", ")}. Allowed: ${PATCH_KEYS.join(", ")}`;
  for (const k of PATCH_KEYS) {
    if (!(k in patch)) continue;
    const v = patch[k];
    if (k === "queued_test_scenario_id") {
      if (v !== null && typeof v !== "string") return `${k} must be string or null`;
      continue;
    }
    if (typeof v !== "number" || !Number.isInteger(v)) return `${k} must be integer`;
  }
  return null;
}

function writeState(repoRoot: string, patch: Record<string, unknown>): { ok: true } | { ok: false; code: string; stderr: string } {
  const script = join(repoRoot, "tools/scripts/runtime-state-write.sh");
  const patchFile = join(tmpdir(), `rs-patch-${randomBytes(8).toString("hex")}.json`);
  writeFileSync(patchFile, JSON.stringify(patch), "utf8");
  try {
    const r = spawnSync(script, [patchFile], {
      encoding: "utf8",
      env: buildScriptEnv(repoRoot),
      maxBuffer: 2 * 1024 * 1024,
    });
    if (r.status !== 0) {
      return {
        ok: false,
        code: "RUNTIME_STATE_WRITE_FAILED",
        stderr: (r.stderr || r.stdout || "").trim() || `exit ${r.status}`,
      };
    }
    return { ok: true };
  } finally {
    try {
      unlinkSync(patchFile);
    } catch {
      /* best-effort */
    }
  }
}

export function registerRuntimeState(server: McpServer): void {
  server.registerTool(
    "runtime_state",
    {
      title: "Runtime state (verify / bridge / queued scenario)",
      description:
        "Read or merge-write ia/state/runtime-state.json (gitignored per clone). Uses flock. " +
        "Patch keys: last_verify_exit_code, last_bridge_preflight_exit_code, queued_test_scenario_id.",
      inputSchema: inputShape,
    },
    async (args: unknown) => {
      return runWithToolTiming("runtime_state", async () => {
        const parsed = z.object(inputShape).safeParse(args);
        if (!parsed.success) {
          return jsonResult({ code: "invalid_input", issues: parsed.error.issues });
        }
        const repoRoot = resolveRepoRoot();
        const { action, patch } = parsed.data;

        if (action === "read") {
          const r = readStateFile(repoRoot);
          if (!r.ok) return jsonResult(r);
          return jsonResult({ ok: true, path: "ia/state/runtime-state.json", data: r.data });
        }

        if (!patch || Object.keys(patch).length === 0) {
          return jsonResult({ code: "invalid_input", message: 'action "write" requires non-empty patch' });
        }
        const err = validatePatch(patch as Record<string, unknown>);
        if (err) return jsonResult({ code: "invalid_input", message: err });

        const w = writeState(repoRoot, patch as Record<string, unknown>);
        if (!w.ok) return jsonResult({ code: w.code, stderr: w.stderr });

        const again = readStateFile(repoRoot);
        if (!again.ok) return jsonResult(again);
        return jsonResult({ ok: true, path: "ia/state/runtime-state.json", data: again.data });
      });
    },
  );
}
