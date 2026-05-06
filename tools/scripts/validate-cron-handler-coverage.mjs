#!/usr/bin/env node
/**
 * validate-cron-handler-coverage.mjs
 *
 * Gates against silently adding a cron queue table without its handler + MCP tool.
 *
 * For each table matching `cron_%_jobs` in information_schema:
 *   1. Derives kind = table_name stripped of 'cron_' prefix and '_jobs' suffix.
 *   2. Checks handler file: tools/cron-server/handlers/{kind}-cron-handler.ts
 *   3. Checks MCP tool registration: string `cron_{kind}_enqueue` in
 *      tools/mcp-ia-server/src/server-registrations.ts
 *
 * Exit 0 = full coverage. Exit 1 = missing handler or MCP tool.
 *
 * TECH-18096 / async-cron-jobs Stage 2.0.4
 */

import path from "node:path";
import { fileURLToPath } from "node:url";
import { createRequire } from "node:module";
import fs from "node:fs";
import { resolveDatabaseUrl } from "../postgres-ia/resolve-database-url.mjs";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = process.env.IA_REPO_ROOT ?? path.resolve(__dirname, "../..");

const HANDLERS_DIR = path.join(REPO_ROOT, "tools/cron-server/handlers");
const MCP_TOOLS_DIR = path.join(
  REPO_ROOT,
  "tools/mcp-ia-server/src/tools",
);

const pgRequire = createRequire(
  path.join(REPO_ROOT, "tools/postgres-ia/package.json"),
);
const pg = pgRequire("pg");

async function main() {
  const dbUrl = await resolveDatabaseUrl(REPO_ROOT);
  const client = new pg.Client({ connectionString: dbUrl });
  await client.connect();

  let tableNames;
  try {
    const res = await client.query(
      `SELECT table_name
       FROM information_schema.tables
       WHERE table_schema = 'public'
         AND table_name LIKE 'cron_%\_jobs' ESCAPE '\\'
       ORDER BY table_name`,
    );
    tableNames = res.rows.map((r) => r.table_name);
  } finally {
    await client.end();
  }

  if (tableNames.length === 0) {
    console.log("validate:cron-handler-coverage: no cron_*_jobs tables found. ok.");
    process.exit(0);
  }

  // Scan all cron-*.ts MCP tool files once for registered tool names.
  // Each file calls server.registerTool("cron_{kind}_enqueue", ...).
  const cronToolFiles = fs
    .readdirSync(MCP_TOOLS_DIR)
    .filter((f) => f.startsWith("cron-") && f.endsWith(".ts"));

  const registeredToolNames = new Set();
  for (const file of cronToolFiles) {
    const content = fs.readFileSync(path.join(MCP_TOOLS_DIR, file), "utf8");
    // Extract all registerTool("cron_..._enqueue", ...) calls.
    const matches = content.matchAll(/registerTool\(\s*["']([^"']+)["']/g);
    for (const m of matches) {
      registeredToolNames.add(m[1]);
    }
  }

  const missingHandlers = [];
  const missingMcpTools = [];

  for (const tableName of tableNames) {
    // Derive kind: strip 'cron_' prefix and '_jobs' suffix.
    const kind = tableName.replace(/^cron_/, "").replace(/_jobs$/, "");

    // Handler files use kebab-case: underscores → hyphens.
    const handlerKind = kind.replace(/_/g, "-");
    const handlerFile = path.join(HANDLERS_DIR, `${handlerKind}-cron-handler.ts`);
    if (!fs.existsSync(handlerFile)) {
      missingHandlers.push(kind);
    }

    // Check MCP tool registration: look for tool name `cron_{kind}_enqueue`.
    const toolName = `cron_${kind}_enqueue`;
    if (!registeredToolNames.has(toolName)) {
      missingMcpTools.push(toolName);
    }
  }

  const ok = missingHandlers.length === 0 && missingMcpTools.length === 0;

  if (ok) {
    console.log(
      `validate:cron-handler-coverage: ${tableNames.length} table(s) — full coverage. ok.`,
    );
    process.exit(0);
  } else {
    console.error("validate:cron-handler-coverage: MISSING coverage detected.");
    if (missingHandlers.length > 0) {
      console.error(
        `  missing_handlers: ${JSON.stringify(missingHandlers)}`,
      );
      console.error(
        `  Expected files in tools/cron-server/handlers/:`,
      );
      for (const h of missingHandlers) {
        const kebab = h.replace(/_/g, "-");
        console.error(`    ${kebab}-cron-handler.ts`);
      }
    }
    if (missingMcpTools.length > 0) {
      console.error(
        `  missing_mcp_tools: ${JSON.stringify(missingMcpTools)}`,
      );
      console.error(
        `  Expected registerTool call in tools/mcp-ia-server/src/tools/cron-*.ts:`,
      );
      for (const t of missingMcpTools) {
        console.error(`    "${t}"`);
      }
    }
    process.exit(1);
  }
}

main().catch((err) => {
  console.error("validate:cron-handler-coverage: unexpected error:", err);
  process.exit(1);
});
