/**
 * sql step — execute Postgres query/exec against the IA DB.
 *
 * Reuses the shared pool from tools/mcp-ia-server/src/ia-db/pool.ts.
 * `query` returns the rows array; `exec` returns affected-row count.
 *
 * Templated `query` and `params` resolve via resolveTree before dispatch.
 */

import type { SqlStep, RunContext, StepResult } from "../types.js";
import { resolveTree } from "../template.js";
import { getIaDatabasePool } from "../../../mcp-ia-server/src/ia-db/pool.js";

export async function runSqlStep(step: SqlStep, ctx: RunContext): Promise<StepResult> {
  const pool = getIaDatabasePool();
  if (!pool) {
    return {
      ok: false,
      error: { code: "no_database", message: "IA DB unavailable — DATABASE_URL not configured" },
    };
  }
  const queryStr = String(resolveTree(ctx.vars, step.query));
  const params = (resolveTree(ctx.vars, step.params ?? []) as unknown[]) ?? [];

  if (ctx.dry_run) {
    return { ok: true, value: { dry_run: true, query: queryStr, params } };
  }

  try {
    const res = await pool.query(queryStr, params);
    if (step.sql === "query") {
      return { ok: true, value: { rows: res.rows, rowCount: res.rowCount } };
    }
    return { ok: true, value: { rowCount: res.rowCount } };
  } catch (err) {
    const msg = err instanceof Error ? err.message : String(err);
    return { ok: false, error: { code: "sql_error", message: msg } };
  }
}
