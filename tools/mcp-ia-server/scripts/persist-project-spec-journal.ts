#!/usr/bin/env npx tsx
/**
 * CLI: persist Decision Log + Lessons learned from a project spec into Postgres.
 * Mirrors MCP `project_spec_journal_persist` for agents without MCP tool access.
 *
 * Usage (repo root):
 *   npx tsx tools/mcp-ia-server/scripts/persist-project-spec-journal.ts --issue TECH-58
 *   npx tsx tools/mcp-ia-server/scripts/persist-project-spec-journal.ts --spec-path .cursor/projects/TECH-58.md
 *   --git-sha optional
 *
 * Requires: DATABASE_URL or config/postgres-dev.json; migration `0007_ia_project_spec_journal` applied.
 */

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { persistProjectSpecJournal } from "../src/ia-db/journal-repo.js";
import { getIaDatabasePool } from "../src/ia-db/pool.js";
import { resolveProjectSpecFile } from "../src/parser/project-spec-closeout-parse.js";

function parseArgs(argv: string[]) {
  const out: {
    issue?: string;
    specPath?: string;
    gitSha?: string;
  } = {};
  for (let i = 2; i < argv.length; i++) {
    const a = argv[i];
    if (a === "--issue" && argv[i + 1]) out.issue = argv[++i];
    else if (a === "--spec-path" && argv[i + 1]) out.specPath = argv[++i];
    else if (a === "--git-sha" && argv[i + 1]) out.gitSha = argv[++i];
  }
  return out;
}

/** Repo root when `npm run db:persist-project-journal` runs with `cwd` under `tools/mcp-ia-server`. */
function resolveCliRepoRoot(): string {
  const raw = process.env.REPO_ROOT?.trim();
  if (raw) {
    return path.isAbsolute(raw) ? raw : path.resolve(process.cwd(), raw);
  }
  const here = path.dirname(fileURLToPath(import.meta.url));
  return path.resolve(here, "../../..");
}

async function main() {
  const args = parseArgs(process.argv);
  if (!args.issue && !args.specPath) {
    console.error(
      "Usage: persist-project-spec-journal.ts --issue ID | --spec-path .cursor/projects/ID.md [--git-sha SHA]",
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

  const repoRoot = resolveCliRepoRoot();
  const resolved = resolveProjectSpecFile(repoRoot, {
    issue_id: args.issue,
    spec_path: args.specPath,
  });
  if (!resolved.ok) {
    console.error(resolved.message);
    process.exit(1);
  }

  const markdown = fs.readFileSync(resolved.absPath, "utf8");
  const result = await persistProjectSpecJournal(pool, {
    markdown,
    specPathPosix: resolved.relPosix,
    issueId: resolved.issue_id,
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
