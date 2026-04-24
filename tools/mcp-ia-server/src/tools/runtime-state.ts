/**
 * MCP tool: runtime_state — read / merge-write shared repo runtime state
 * via Postgres `ia_runtime_state` (singleton row, id=1).
 *
 * Harness-agnostic: Cursor / Claude Code / any MCP client. Active task / stage live in
 * per-harness active-session JSON — not this row.
 *
 * DB-primary as of Step 9.6.5 (2026-04-24). Replaces ia/state/runtime-state.json
 * + tools/scripts/runtime-state-write.sh flock script.
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { getIaDatabasePool } from "../ia-db/pool.js";

const PATCH_KEYS = [
  "last_verify_exit_code",
  "last_bridge_preflight_exit_code",
  "queued_test_scenario_id",
] as const;

type PatchKey = (typeof PATCH_KEYS)[number];

const inputShape = {
  action: z
    .enum(["read", "write"])
    .describe('Read full row, or merge-write a patch ("write").'),
  patch: z
    .record(z.string(), z.unknown())
    .optional()
    .describe(
      'For action "write": shallow merge keys (last_verify_exit_code, last_bridge_preflight_exit_code, queued_test_scenario_id). updated_at set automatically.',
    ),
};

interface RuntimeStateRow {
  last_verify_exit_code: number | null;
  last_bridge_preflight_exit_code: number | null;
  queued_test_scenario_id: string | null;
  updated_at: Date | string;
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

function validatePatch(patch: Record<string, unknown>): string | null {
  const bad = Object.keys(patch).filter((k) => !PATCH_KEYS.includes(k as PatchKey));
  if (bad.length) return `Invalid patch keys: ${bad.join(", ")}. Allowed: ${PATCH_KEYS.join(", ")}`;
  for (const k of PATCH_KEYS) {
    if (!(k in patch)) continue;
    const v = patch[k];
    if (k === "queued_test_scenario_id") {
      if (v !== null && typeof v !== "string") return `${k} must be string or null`;
      continue;
    }
    if (v !== null && (typeof v !== "number" || !Number.isInteger(v))) {
      return `${k} must be integer or null`;
    }
  }
  return null;
}

function rowToPayload(row: RuntimeStateRow): Record<string, unknown> {
  return {
    last_verify_exit_code: row.last_verify_exit_code,
    last_bridge_preflight_exit_code: row.last_bridge_preflight_exit_code,
    queued_test_scenario_id: row.queued_test_scenario_id,
    updated_at:
      typeof row.updated_at === "string"
        ? new Date(row.updated_at).toISOString()
        : row.updated_at.toISOString(),
  };
}

export function registerRuntimeState(server: McpServer): void {
  server.registerTool(
    "runtime_state",
    {
      title: "Runtime state (verify / bridge / queued scenario)",
      description:
        "Read or merge-write ia_runtime_state singleton row in Postgres. " +
        "Patch keys: last_verify_exit_code, last_bridge_preflight_exit_code, queued_test_scenario_id.",
      inputSchema: inputShape,
    },
    async (args: unknown) => {
      return runWithToolTiming("runtime_state", async () => {
        const parsed = z.object(inputShape).safeParse(args);
        if (!parsed.success) {
          return jsonResult({ code: "invalid_input", issues: parsed.error.issues });
        }
        const { action, patch } = parsed.data;
        const pool = getIaDatabasePool();
        if (!pool) {
          return jsonResult({
            code: "db_unavailable",
            message:
              "ia_runtime_state requires DATABASE_URL or config/postgres-dev.json (no JSON fallback after Step 9.6.5).",
          });
        }

        if (action === "read") {
          try {
            const { rows } = await pool.query<RuntimeStateRow>(
              `SELECT last_verify_exit_code, last_bridge_preflight_exit_code, queued_test_scenario_id, updated_at
                 FROM ia_runtime_state WHERE id = 1`,
            );
            if (rows.length === 0) {
              return jsonResult({
                ok: false,
                code: "RUNTIME_STATE_MISSING",
                message: "ia_runtime_state row id=1 not seeded — run db:migrate.",
              });
            }
            return jsonResult({ ok: true, source: "ia_runtime_state", data: rowToPayload(rows[0]) });
          } catch (e) {
            return jsonResult({
              ok: false,
              code: "RUNTIME_STATE_READ_FAILED",
              message: e instanceof Error ? e.message : String(e),
            });
          }
        }

        if (!patch || Object.keys(patch).length === 0) {
          return jsonResult({ code: "invalid_input", message: 'action "write" requires non-empty patch' });
        }
        const err = validatePatch(patch as Record<string, unknown>);
        if (err) return jsonResult({ code: "invalid_input", message: err });

        const sets: string[] = [];
        const values: unknown[] = [];
        let i = 1;
        for (const k of PATCH_KEYS) {
          if (k in patch) {
            sets.push(`${k} = $${i++}`);
            values.push((patch as Record<string, unknown>)[k]);
          }
        }
        sets.push("updated_at = now()");
        try {
          const { rows } = await pool.query<RuntimeStateRow>(
            `UPDATE ia_runtime_state SET ${sets.join(", ")} WHERE id = 1
              RETURNING last_verify_exit_code, last_bridge_preflight_exit_code, queued_test_scenario_id, updated_at`,
            values,
          );
          if (rows.length === 0) {
            return jsonResult({
              ok: false,
              code: "RUNTIME_STATE_MISSING",
              message: "ia_runtime_state row id=1 not seeded — run db:migrate.",
            });
          }
          return jsonResult({ ok: true, source: "ia_runtime_state", data: rowToPayload(rows[0]) });
        } catch (e) {
          return jsonResult({
            ok: false,
            code: "RUNTIME_STATE_WRITE_FAILED",
            message: e instanceof Error ? e.message : String(e),
          });
        }
      });
    },
  );
}
