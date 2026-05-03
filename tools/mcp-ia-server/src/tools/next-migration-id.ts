/**
 * MCP tool: next_migration_id — read `db/migrations/` directory, parse the
 * leading 4-digit ordinal prefix from each `.sql` filename, and return the
 * next available id (`max + 1`) zero-padded to 4 digits.
 *
 * Resolves a recurring authoring friction: spec stubs / project specs / plan
 * digests that need to lock a migration id at author time without shelling
 * out (`ls db/migrations/`) and without guessing — guesses cause numbering
 * collisions when two streams race.
 *
 * Edit descriptor → restart Claude Code (or `tsx tools/mcp-ia-server/src/index.ts`
 * script) to refresh the in-memory schema cache (N4).
 */
import * as fs from "fs";
import * as path from "path";
import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { resolveRepoRoot } from "../config.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";

// ---------------------------------------------------------------------------
// Input schema
// ---------------------------------------------------------------------------

const inputShape = {
  migrations_dir: z
    .string()
    .optional()
    .describe(
      "Repo-relative migrations dir. Defaults to `db/migrations`. Override only for tests.",
    ),
};

// ---------------------------------------------------------------------------
// Output shape
// ---------------------------------------------------------------------------

export interface NextMigrationIdResult {
  next_id: string; // zero-padded 4-digit (e.g. "0055")
  next_id_int: number; // raw int (e.g. 55)
  current_head: string | null; // tail filename, e.g. "0054_parallel_carcass_runnable_prototype_signal.sql"
  current_head_int: number | null;
  migrations_dir: string; // repo-relative
  scanned_count: number;
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

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

const PREFIX_RE = /^(\d{4})_/;

// ---------------------------------------------------------------------------
// Pure core — exported for testability
// ---------------------------------------------------------------------------

/**
 * Scan a migrations directory, return next available 4-digit ordinal.
 *
 * @param repoRoot - Absolute repo root (injected; no env dependency).
 * @param migrationsDirRel - Repo-relative migrations dir (default `db/migrations`).
 * @throws `{ code, message, hint? }` on missing / unreadable dir.
 */
export function computeNextMigrationId(
  repoRoot: string,
  migrationsDirRel: string = "db/migrations",
): NextMigrationIdResult {
  const dirAbs = path.join(repoRoot, migrationsDirRel);
  if (!fs.existsSync(dirAbs)) {
    throw {
      code: "invalid_input",
      message: `Migrations dir '${migrationsDirRel}' not found at ${dirAbs}.`,
      hint: "Default is db/migrations. Pass migrations_dir to override.",
    };
  }
  const stat = fs.statSync(dirAbs);
  if (!stat.isDirectory()) {
    throw {
      code: "invalid_input",
      message: `Migrations path '${migrationsDirRel}' is not a directory.`,
    };
  }

  const entries = fs.readdirSync(dirAbs, { withFileTypes: true });
  let maxId = -1;
  let headName: string | null = null;
  let scannedCount = 0;
  for (const ent of entries) {
    if (!ent.isFile()) continue;
    if (!ent.name.endsWith(".sql")) continue;
    const m = PREFIX_RE.exec(ent.name);
    if (!m) continue;
    scannedCount += 1;
    const n = Number(m[1]);
    if (Number.isNaN(n)) continue;
    if (n > maxId) {
      maxId = n;
      headName = ent.name;
    }
  }

  const nextInt = maxId + 1; // -1 → 0 (empty dir → first migration "0000")
  const nextStr = String(nextInt).padStart(4, "0");

  return {
    next_id: nextStr,
    next_id_int: nextInt,
    current_head: headName,
    current_head_int: maxId === -1 ? null : maxId,
    migrations_dir: migrationsDirRel,
    scanned_count: scannedCount,
  };
}

// ---------------------------------------------------------------------------
// Registration
// ---------------------------------------------------------------------------

type NextMigArgs = { migrations_dir?: string };

export function registerNextMigrationId(server: McpServer): void {
  server.registerTool(
    "next_migration_id",
    {
      description:
        "Scan `db/migrations/` and return the next available 4-digit migration id " +
        "(`max(prefix) + 1`, zero-padded). Use at author time to lock the migration id " +
        "in spec stubs / plan digests without shelling out or guessing. " +
        "Returns { next_id, next_id_int, current_head, current_head_int, migrations_dir, scanned_count }. " +
        "Errors: invalid_input (dir missing or not a directory). " +
        "Schema-cache restart required after adding this tool (N4).",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("next_migration_id", async () => {
        const envelope = await wrapTool(
          async (
            input: NextMigArgs | undefined,
          ): Promise<NextMigrationIdResult> => {
            const dirArg = (input?.migrations_dir ?? "").trim();
            const repoRoot = resolveRepoRoot();
            return computeNextMigrationId(
              repoRoot,
              dirArg === "" ? undefined : dirArg,
            );
          },
        )(args as NextMigArgs | undefined);

        return jsonResult(envelope);
      }),
  );
}
