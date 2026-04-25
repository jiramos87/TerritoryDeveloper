#!/usr/bin/env npx tsx
/**
 * CLI: persist Decision Log + Lessons learned from a task spec body into Postgres.
 * Mirrors MCP `project_spec_journal_persist` for agents without MCP tool access.
 *
 * Usage (repo root):
 *   npx tsx tools/mcp-ia-server/scripts/persist-project-spec-journal.ts --issue TECH-58
 *   --git-sha optional
 *
 * Requires: DATABASE_URL or config/postgres-dev.json; migration `0007_ia_project_spec_journal` applied.
 */

import { persistProjectSpecJournal } from "../src/ia-db/journal-repo.js";
import { getIaDatabasePool } from "../src/ia-db/pool.js";
import { queryTaskBody } from "../src/ia-db/queries.js";
import { normalizeIssueId } from "../src/parser/project-spec-closeout-parse.js";

function parseArgs(argv: string[]) {
  const out: {
    issue?: string;
    gitSha?: string;
  } = {};
  for (let i = 2; i < argv.length; i++) {
    const a = argv[i];
    if (a === "--issue" && argv[i + 1]) out.issue = argv[++i];
    else if (a === "--git-sha" && argv[i + 1]) out.gitSha = argv[++i];
  }
  return out;
}

async function main() {
  const args = parseArgs(process.argv);
  if (!args.issue) {
    console.error(
      "Usage: persist-project-spec-journal.ts --issue ID [--git-sha SHA]",
    );
    process.exit(1);
  }

  const pool = getIaDatabasePool();
  if (!pool) {
    console.error(
      "No database URL: set DATABASE_URL or config/postgres-dev.json (see docs/postgres-ia-dev-setup.md).",
    );
    process.exit(1);
  }

  const issueId = normalizeIssueId(args.issue);
  const markdown = await queryTaskBody(issueId);
  if (markdown == null) {
    console.error(`No task spec body for '${issueId}' in ia_task_specs.`);
    process.exit(1);
  }

  const result = await persistProjectSpecJournal(pool, {
    markdown,
    issueId,
    gitSha: args.gitSha ?? null,
  });

  console.log(JSON.stringify(result, null, 2));
  if (!result.ok) process.exit(1);
  await pool.end();
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
