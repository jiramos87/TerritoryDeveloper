#!/usr/bin/env node
/**
 * TECH-44c — insert one dev_repro_bundle row (E1).
 * Normalizes backlog_issue_id to match MCP backlog-parser normalizeIssueId.
 *
 * Usage:
 *   node register-dev-repro.mjs --issue TECH-44c \
 *     [--agent-context tools/reports/agent-context-....json] \
 *     [--sorting-debug tools/reports/sorting-debug-....md] \
 *     [--notes "short note"] [--sha abc1234]
 *
 * Requires: DATABASE_URL or config/postgres-dev.json; git in PATH for rev-parse (unless --sha).
 */

import { spawnSync } from 'node:child_process';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import pg from 'pg';
import { resolveDatabaseUrl } from './resolve-database-url.mjs';

const __dirname = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = resolve(__dirname, '../..');

/** Same semantics as tools/mcp-ia-server/src/parser/backlog-parser.ts normalizeIssueId */
function normalizeIssueId(issueId) {
  const t = String(issueId).trim();
  const m = t.match(/^([A-Za-z]+)-(\d+)([a-zA-Z]*)$/);
  if (!m) return t;
  return `${m[1].toUpperCase()}-${m[2]}${m[3].toLowerCase()}`;
}

function parseArgs(argv) {
  const out = {
    issue: null,
    agentContext: null,
    sortingDebug: null,
    notes: null,
    sha: null,
  };
  for (let i = 2; i < argv.length; i++) {
    const a = argv[i];
    if (a === '--issue' && argv[i + 1]) {
      out.issue = argv[++i];
    } else if (a === '--agent-context' && argv[i + 1]) {
      out.agentContext = argv[++i];
    } else if (a === '--sorting-debug' && argv[i + 1]) {
      out.sortingDebug = argv[++i];
    } else if (a === '--notes' && argv[i + 1]) {
      out.notes = argv[++i];
    } else if (a === '--sha' && argv[i + 1]) {
      out.sha = argv[++i];
    }
  }
  return out;
}

function gitHeadSha() {
  const r = spawnSync('git', ['rev-parse', 'HEAD'], {
    cwd: REPO_ROOT,
    encoding: 'utf8',
  });
  if (r.status !== 0) {
    console.error(r.stderr || 'git rev-parse HEAD failed');
    process.exit(1);
  }
  return r.stdout.trim();
}

const args = parseArgs(process.argv);
if (!args.issue) {
  console.error(
    'Usage: node register-dev-repro.mjs --issue BUG-37|TECH-44c [...]',
  );
  process.exit(1);
}

const backlogIssueId = normalizeIssueId(args.issue);
const gitSha = (args.sha || gitHeadSha()).trim();
if (!gitSha) {
  console.error('Empty git SHA');
  process.exit(1);
}

const SCHEMA_VERSION = 1;
const payload = {
  artifact: 'dev_repro_bundle',
  schema_version: SCHEMA_VERSION,
};
if (args.agentContext) payload.agent_context_relative_path = args.agentContext;
if (args.sortingDebug) payload.sorting_debug_relative_path = args.sortingDebug;
if (args.notes) payload.notes = args.notes;

const databaseUrl = resolveDatabaseUrl(REPO_ROOT);
if (!databaseUrl) {
  console.error(
    'Missing database URL: set DATABASE_URL or config/postgres-dev.json. See docs/postgres-ia-dev-setup.md.',
  );
  process.exit(1);
}

const client = new pg.Client({ connectionString: databaseUrl });
await client.connect();

const { rows } = await client.query(
  `INSERT INTO dev_repro_bundle (backlog_issue_id, git_sha, interchange_revision, payload)
   VALUES ($1, $2, $3, $4::jsonb)
   RETURNING id, backlog_issue_id, git_sha, exported_at_utc`,
  [backlogIssueId, gitSha, SCHEMA_VERSION, JSON.stringify(payload)],
);

await client.end();

console.log(JSON.stringify({ ok: true, row: rows[0] }, null, 2));
