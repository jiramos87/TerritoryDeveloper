/**
 * MCP tool: db_read_batch — read-only batch SQL executor + cache write-through.
 *
 * Collapses N sequential `psql -c` calls into one PG round-trip.
 * Two-layer read-only defense:
 *   1. SQL token-allowlist scan rejects DML/DDL before execution.
 *   2. PG `SET TRANSACTION READ ONLY` catches CTE-DML bypasses.
 *
 * Cache write-through via ia_mcp_context_cache (chain-token-cut Stage 1).
 * Cache key = (plan_id, sha256(normalize(sql))).
 *
 * Error codes: db_read_batch_disallowed_sql | db_read_batch_too_many_queries |
 *              db_read_batch_pg_error | db_read_batch_db_unavailable
 */

import { createHash } from "crypto";
import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import { getIaDatabasePool } from "../ia-db/pool.js";

// ---------------------------------------------------------------------------
// Constants
// ---------------------------------------------------------------------------

const MAX_QUERIES = 20;

/** DML/DDL tokens that must never appear outside string/comment context. */
const DISALLOWED_TOKEN_RE =
  /\b(INSERT|UPDATE|DELETE|DROP|ALTER|TRUNCATE|GRANT|REVOKE)\b/i;

// ---------------------------------------------------------------------------
// SQL helpers
// ---------------------------------------------------------------------------

/**
 * Strip SQL comments and string literals before scanning for disallowed tokens.
 * Covers: -- line comments, /* block comments, single-quoted literals, dollar-quoted literals.
 */
function stripSqlNoise(sql: string): string {
  // Remove /* … */ block comments (non-greedy)
  let s = sql.replace(/\/\*[\s\S]*?\*\//g, " ");
  // Remove -- … newline comments
  s = s.replace(/--[^\n]*/g, " ");
  // Remove single-quoted string literals (handles escaped quotes via '')
  s = s.replace(/'(?:[^']|'')*'/g, " ");
  // Remove dollar-quoted literals: $tag$…$tag$ — simplify by removing $…$ blocks
  s = s.replace(/\$([^$]*)\$[\s\S]*?\$\1\$/g, " ");
  return s;
}

/**
 * Normalize SQL for cache keying:
 * collapse whitespace to single space + lowercase SQL keywords.
 */
function normalizeSql(sql: string): string {
  return sql
    .replace(/\s+/g, " ")
    .trim()
    .replace(
      /\b(SELECT|FROM|WHERE|JOIN|LEFT|RIGHT|INNER|OUTER|CROSS|ON|AND|OR|NOT|IN|AS|GROUP|BY|ORDER|HAVING|LIMIT|OFFSET|DISTINCT|UNION|ALL|EXISTS|CASE|WHEN|THEN|ELSE|END|WITH|INSERT|UPDATE|DELETE|DROP|ALTER|TRUNCATE|GRANT|REVOKE|SET|INTO|VALUES|RETURNING)\b/gi,
      (m) => m.toLowerCase(),
    );
}

function sha256hex(s: string): string {
  return createHash("sha256").update(s, "utf8").digest("hex");
}

// ---------------------------------------------------------------------------
// Core executor
// ---------------------------------------------------------------------------

interface QuerySpec {
  name: string;
  sql: string;
}

interface QueryResult {
  rows: unknown[];
  cache_hit: boolean;
}

async function runBatch(
  plan_id: string,
  queries: QuerySpec[],
): Promise<Record<string, QueryResult>> {
  if (queries.length > MAX_QUERIES) {
    throw {
      code: "db_read_batch_too_many_queries",
      message: `Batch contains ${queries.length} queries; max is ${MAX_QUERIES}.`,
      max: MAX_QUERIES,
      received: queries.length,
    };
  }

  // Token-scan all queries before opening any connection.
  for (const q of queries) {
    const stripped = stripSqlNoise(q.sql);
    const match = DISALLOWED_TOKEN_RE.exec(stripped);
    if (match) {
      throw {
        code: "db_read_batch_disallowed_sql",
        message: `Query '${q.name}' contains disallowed SQL token '${match[1]}'.`,
        offending_query: q.name,
        token: match[1],
      };
    }
  }

  const pool = getIaDatabasePool();
  if (!pool) {
    throw {
      code: "db_read_batch_db_unavailable",
      message: "IA database pool is not configured. Set DATABASE_URL.",
    };
  }

  const result: Record<string, QueryResult> = {};
  const client = await pool.connect();

  try {
    await client.query("BEGIN");
    await client.query("SET TRANSACTION READ ONLY");

    for (const q of queries) {
      const normalized = normalizeSql(q.sql);
      const hash = sha256hex(normalized);
      const cacheKey = `db_read_batch:${hash}`;

      // Cache read: ia_mcp_context_cache keyed by (plan_id, cacheKey)
      const cacheRes = await client.query<{ payload: unknown }>(
        `SELECT payload FROM ia_mcp_context_cache WHERE plan_id = $1 AND key = $2 LIMIT 1`,
        [plan_id, cacheKey],
      );

      if (cacheRes.rowCount && cacheRes.rowCount > 0) {
        const cached = cacheRes.rows[0]!.payload;
        const rows = Array.isArray(cached) ? cached : [cached];
        result[q.name] = { rows, cache_hit: true };
        continue;
      }

      // Cache miss — execute and write through.
      let rows: unknown[];
      try {
        const execRes = await client.query(q.sql);
        rows = execRes.rows;
      } catch (pgErr: unknown) {
        const msg =
          pgErr && typeof pgErr === "object" && "message" in pgErr
            ? String((pgErr as { message: unknown }).message)
            : String(pgErr);
        throw {
          code: "db_read_batch_pg_error",
          message: `Query '${q.name}' failed: ${msg}`,
          query_name: q.name,
          pg_message: msg,
        };
      }

      // Write through — best-effort (ignore errors so batch still returns data)
      try {
        await client.query(
          `INSERT INTO ia_mcp_context_cache (plan_id, key, payload, content_hash, updated_at)
           VALUES ($1, $2, $3, $4, now())
           ON CONFLICT (plan_id, key)
           DO UPDATE SET payload = EXCLUDED.payload, content_hash = EXCLUDED.content_hash, updated_at = now()`,
          [plan_id, cacheKey, JSON.stringify(rows), hash],
        );
      } catch {
        // cache write failure is non-fatal
      }

      result[q.name] = { rows, cache_hit: false };
    }

    await client.query("COMMIT");
  } catch (err) {
    try {
      await client.query("ROLLBACK");
    } catch {
      // ignore rollback errors
    }
    throw err;
  } finally {
    client.release();
  }

  return result;
}

// ---------------------------------------------------------------------------
// Tool registration
// ---------------------------------------------------------------------------

export function registerDbReadBatch(server: McpServer): void {
  server.registerTool(
    "db_read_batch",
    {
      description:
        "Read-only batch SQL executor + cache write-through. Accepts up to 20 named SQL queries, runs them inside a single PG connection with SET TRANSACTION READ ONLY, writes each result through ia_mcp_context_cache (key = plan_id + sha256(normalize(sql))). Returns {[name]: {rows, cache_hit}}. Rejects DML/DDL tokens before execution. Error codes: db_read_batch_disallowed_sql | db_read_batch_too_many_queries | db_read_batch_pg_error | db_read_batch_db_unavailable.",
      inputSchema: {
        plan_id: z
          .string()
          .min(1)
          .describe("Master-plan slug or session id used as cache namespace."),
        queries: z
          .array(
            z.object({
              name: z.string().min(1).describe("Logical name for this query (used as result key)."),
              sql: z.string().min(1).describe("Read-only SQL statement (SELECT / WITH … SELECT)."),
            }),
          )
          .min(1)
          .max(MAX_QUERIES)
          .describe(`Array of named SQL queries (max ${MAX_QUERIES}).`),
      },
    },
    async (args) =>
      runWithToolTiming("db_read_batch", async () => {
        const envelope = await wrapTool(
          async (input: { plan_id?: string; queries?: QuerySpec[] } | undefined) => {
            const plan_id = (input?.plan_id ?? "").trim();
            const queries = input?.queries ?? [];
            if (!plan_id) throw { code: "invalid_input", message: "plan_id is required." };
            if (!queries.length) throw { code: "invalid_input", message: "queries must be non-empty." };
            return await runBatch(plan_id, queries);
          },
        )(args as { plan_id?: string; queries?: QuerySpec[] } | undefined);

        return {
          content: [
            {
              type: "text" as const,
              text: JSON.stringify(envelope, null, 2),
            },
          ],
        };
      }),
  );
}
