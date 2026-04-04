#!/usr/bin/env node
/**
 * TECH-55b — insert one Editor Reports row with full document JSONB + metadata payload.
 *
 * Usage:
 *   node register-editor-export.mjs --kind agent_context|sorting_debug|terrain_cell_chunk|world_snapshot_dev \
 *     --document-file <repo-relative> [--issue BUG-37] [--sha <git>]
 *
 * Reads UTF-8 from --document-file: JSON kinds expect JSON; sorting_debug expects Markdown (wrapped in DB).
 * Requires: DATABASE_URL (env), git in PATH for rev-parse (unless --sha).
 * backlog_issue_id is optional (NULL when --issue omitted).
 */

import { readFileSync } from 'node:fs';
import { spawnSync } from 'node:child_process';
import { dirname, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import pg from 'pg';

const __dirname = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = resolve(__dirname, '../..');

const TABLE_BY_KIND = {
  agent_context: 'editor_export_agent_context',
  sorting_debug: 'editor_export_sorting_debug',
  terrain_cell_chunk: 'editor_export_terrain_cell_chunk',
  world_snapshot_dev: 'editor_export_world_snapshot_dev',
};

const EXPECTED_ARTIFACT = {
  terrain_cell_chunk: 'terrain_cell_chunk',
  world_snapshot_dev: 'world_snapshot_dev',
};

/** Same semantics as tools/mcp-ia-server/src/parser/backlog-parser.ts normalizeIssueId */
function normalizeIssueId(issueId) {
  const t = String(issueId).trim();
  const m = t.match(/^([A-Za-z]+)-(\d+)([a-zA-Z]*)$/);
  if (!m) return t;
  return `${m[1].toUpperCase()}-${m[2]}${m[3].toLowerCase()}`;
}

function parseArgs(argv) {
  const out = {
    kind: null,
    documentFile: null,
    issue: null,
    sha: null,
  };
  for (let i = 2; i < argv.length; i++) {
    const a = argv[i];
    if (a === '--kind' && argv[i + 1]) {
      out.kind = argv[++i];
    } else if (a === '--document-file' && argv[i + 1]) {
      out.documentFile = argv[++i];
    } else if (a === '--issue' && argv[i + 1]) {
      out.issue = argv[++i];
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

function normalizeRelPath(p) {
  return String(p).trim().replace(/\\/g, '/').replace(/^\/+/, '');
}

function assertUnderRepo(rel) {
  const abs = resolve(REPO_ROOT, rel);
  const relBack = relative(REPO_ROOT, abs);
  if (relBack.startsWith('..') || relBack === '..') {
    console.error('Refusing path outside repo:', rel);
    process.exit(1);
  }
}

function readDocumentAndPayload(kind, relDocPath) {
  const abs = resolve(REPO_ROOT, relDocPath);
  let raw;
  try {
    raw = readFileSync(abs, 'utf8');
  } catch (e) {
    console.error('Cannot read document file:', abs, e.message);
    process.exit(1);
  }

  if (kind === 'sorting_debug') {
    const document = { format: 'markdown', body: raw };
    const payload = {
      artifact: 'editor_export_sorting_debug',
      schema_version: 1,
      source_relative_path: relDocPath,
    };
    return { document, payload, interchangeRevision: 1 };
  }

  let doc;
  try {
    doc = JSON.parse(raw);
  } catch (e) {
    console.error('Invalid JSON document:', relDocPath, e.message);
    process.exit(1);
  }

  if (kind === 'agent_context') {
    const payload = {
      artifact: 'editor_export_agent_context',
      schema_version: 1,
      source_relative_path: relDocPath,
    };
    const sv =
      typeof doc.schema_version === 'string'
        ? parseInt(doc.schema_version, 10) || 1
        : typeof doc.schema_version === 'number'
          ? doc.schema_version
          : 1;
    return { document: doc, payload, interchangeRevision: Number.isFinite(sv) ? sv : 1 };
  }

  if (kind === 'terrain_cell_chunk' || kind === 'world_snapshot_dev') {
    const expected = EXPECTED_ARTIFACT[kind];
    if (doc.artifact !== expected) {
      console.warn(
        `Warning: expected artifact "${expected}", document has "${doc.artifact}" — proceeding.`,
      );
    }
    const schemaVersion =
      typeof doc.schema_version === 'number' ? doc.schema_version : 1;
    const payload = {
      artifact: doc.artifact ?? expected,
      schema_version: schemaVersion,
      source_relative_path: relDocPath,
    };
    return { document: doc, payload, interchangeRevision: schemaVersion };
  }

  throw new Error(`Unknown kind: ${kind}`);
}

const args = parseArgs(process.argv);
const kinds = Object.keys(TABLE_BY_KIND).join('|');
if (!args.kind || !args.documentFile) {
  console.error(
    `Usage: node register-editor-export.mjs --kind ${kinds} --document-file <repo-relative> [--issue <id>] [--sha <git>]`,
  );
  process.exit(1);
}

if (!TABLE_BY_KIND[args.kind]) {
  console.error('Invalid --kind:', args.kind);
  process.exit(1);
}

const relDoc = normalizeRelPath(args.documentFile);
assertUnderRepo(relDoc);

const backlogIssueId =
  args.issue != null && String(args.issue).trim() !== ''
    ? normalizeIssueId(args.issue)
    : null;

const gitSha = (args.sha || gitHeadSha()).trim();
if (!gitSha) {
  console.error('Empty git SHA');
  process.exit(1);
}

const { document, payload, interchangeRevision } = readDocumentAndPayload(
  args.kind,
  relDoc,
);

const databaseUrl = process.env.DATABASE_URL?.trim();
if (!databaseUrl) {
  console.error('Missing DATABASE_URL. See docs/postgres-ia-dev-setup.md.');
  process.exit(1);
}

const table = TABLE_BY_KIND[args.kind];
const client = new pg.Client({ connectionString: databaseUrl });

try {
  await client.connect();
  const { rows } = await client.query(
    `INSERT INTO ${table} (backlog_issue_id, git_sha, interchange_revision, payload, document)
     VALUES ($1, $2, $3, $4::jsonb, $5::jsonb)
     RETURNING id, backlog_issue_id, git_sha, exported_at_utc`,
    [
      backlogIssueId,
      gitSha,
      interchangeRevision,
      JSON.stringify(payload),
      JSON.stringify(document),
    ],
  );
  console.log(JSON.stringify({ ok: true, table, row: rows[0] }, null, 2));
} catch (e) {
  console.error(e.message || String(e));
  process.exit(1);
} finally {
  await client.end();
}
