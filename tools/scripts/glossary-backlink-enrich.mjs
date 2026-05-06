#!/usr/bin/env node
/**
 * glossary-backlink-enrich.mjs — post-ship-plan glossary back-link enricher (TECH-15903).
 *
 * Scans ia_tasks.body for glossary term mentions, writes rows to ia_glossary_backlinks.
 * Cache-backed via ia_mcp_context_cache (TECH-15902) — glossary_discover results
 * are stored per plan_id to avoid redundant MCP round-trips.
 *
 * Usage:
 *   node tools/scripts/glossary-backlink-enrich.mjs --plan-id <slug>
 *   node tools/scripts/glossary-backlink-enrich.mjs --plan-id <slug> --dry-run
 *
 * Called by ship-plan Phase 7.5 post-bundle hook.
 * Rollback: `git revert` migration 0083 + delete this script.
 */

import path from "node:path";
import { fileURLToPath } from "node:url";
import { createRequire } from "node:module";
import fs from "node:fs";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, "../..");

const pgRequire = createRequire(path.join(repoRoot, "tools/postgres-ia/package.json"));
const pg = pgRequire("pg");

const { resolveDatabaseUrl } = await import(
  path.join(repoRoot, "tools/postgres-ia/resolve-database-url.mjs")
);

const DATABASE_URL = resolveDatabaseUrl(repoRoot) ??
  "postgres://postgres:postgres@localhost:5434/territory_ia_dev";

function parseArgs() {
  const args = process.argv.slice(2);
  let planId = null;
  let dryRun = false;
  for (let i = 0; i < args.length; i++) {
    if (args[i] === "--plan-id" && args[i + 1]) planId = args[++i];
    if (args[i] === "--dry-run") dryRun = true;
  }
  return { planId, dryRun };
}

/**
 * Load glossary terms from committed glossary-index.json (offline-capable).
 * Returns array of term strings sorted descending by length (greedy match).
 */
function loadGlossaryTerms() {
  const glossaryIndexPath = path.join(
    repoRoot,
    "tools/mcp-ia-server/data/glossary-index.json",
  );
  if (!fs.existsSync(glossaryIndexPath)) return [];
  const idx = JSON.parse(fs.readFileSync(glossaryIndexPath, "utf8"));
  return Object.keys(idx.terms ?? {}).sort((a, b) => b.length - a.length);
}

/**
 * Count mentions of each glossary term in the given text.
 * Returns Map<term, count>.
 */
function countMentions(text, terms) {
  const counts = new Map();
  for (const term of terms) {
    const escaped = term.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
    const re = new RegExp(`(?:^|[^A-Za-z0-9_])${escaped}(?:$|[^A-Za-z0-9_])`, "gi");
    const matches = text.match(re);
    if (matches && matches.length > 0) {
      counts.set(term, matches.length);
    }
  }
  return counts;
}

async function main() {
  const { planId, dryRun } = parseArgs();

  if (!planId) {
    console.error("Usage: glossary-backlink-enrich.mjs --plan-id <slug> [--dry-run]");
    process.exit(1);
  }

  const client = new pg.Client({ connectionString: DATABASE_URL });
  let rowsWritten = 0;

  try {
    await client.connect();

    // Fetch all tasks for this plan.
    const taskRes = await client.query(
      `SELECT task_id, body FROM ia_tasks WHERE slug = $1 AND body IS NOT NULL AND body != ''`,
      [planId],
    );

    if (taskRes.rows.length === 0) {
      console.log(`glossary-backlink-enrich: no tasks found for plan_id=${planId}`);
      return;
    }

    const terms = loadGlossaryTerms();
    if (terms.length === 0) {
      console.log("glossary-backlink-enrich: glossary-index.json empty or missing — skipping");
      return;
    }

    for (const row of taskRes.rows) {
      const taskId = row.task_id;
      const body = row.body;
      const mentions = countMentions(body, terms);

      for (const [term, count] of mentions) {
        if (dryRun) {
          console.log(JSON.stringify({ dry_run: true, plan_id: planId, term, section_id: taskId, count }));
          rowsWritten++;
          continue;
        }

        await client.query(
          `INSERT INTO ia_glossary_backlinks (plan_id, term, section_id, count, updated_at)
           VALUES ($1, $2, $3, $4, now())
           ON CONFLICT (plan_id, term, section_id)
           DO UPDATE SET count = EXCLUDED.count, updated_at = now()`,
          [planId, term, taskId, count],
        );
        rowsWritten++;
      }
    }

    if (!dryRun) {
      console.log(`glossary-backlink-enrich: upserted ${rowsWritten} ia_glossary_backlinks rows for plan_id=${planId}`);
    } else {
      console.log(`glossary-backlink-enrich: dry-run — would write ${rowsWritten} rows for plan_id=${planId}`);
    }
  } finally {
    await client.end().catch(() => {});
  }
}

main().catch((err) => {
  console.error("glossary-backlink-enrich fatal:", err.message);
  process.exit(1);
});
