#!/usr/bin/env node
/**
 * Complete or fail an agent_bridge_job row (must be status processing).
 *
 * Usage:
 *   node agent-bridge-complete.mjs --command-id <uuid> --response-file <path>
 *   node agent-bridge-complete.mjs --command-id <uuid> --response-json '<json>'
 *   node agent-bridge-complete.mjs --command-id <uuid> --failed --error "English message"
 *
 * --response-file: UTF-8 JSON file; absolute path or repo-relative (must stay under repo if relative).
 */

import { readFileSync } from 'node:fs';
import { dirname, isAbsolute, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import pg from 'pg';
import { resolveDatabaseUrl } from './resolve-database-url.mjs';

const __dirname = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = resolve(__dirname, '../..');

function parseArgs(argv) {
  const out = {
    commandId: null,
    responseFile: null,
    responseJson: null,
    failed: false,
    error: null,
  };
  for (let i = 2; i < argv.length; i++) {
    const a = argv[i];
    if (a === '--command-id' && argv[i + 1]) out.commandId = argv[++i];
    else if (a === '--response-file' && argv[i + 1]) out.responseFile = argv[++i];
    else if (a === '--response-json' && argv[i + 1]) out.responseJson = argv[++i];
    else if (a === '--failed') out.failed = true;
    else if (a === '--error' && argv[i + 1]) out.error = argv[++i];
  }
  return out;
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

function readResponseBody(args) {
  if (args.failed) {
    return null;
  }
  if (args.responseJson != null) {
    try {
      JSON.parse(args.responseJson);
    } catch (e) {
      console.error('Invalid --response-json:', e.message);
      process.exit(1);
    }
    return args.responseJson;
  }
  if (args.responseFile) {
    let abs;
    if (isAbsolute(args.responseFile)) {
      abs = args.responseFile;
    } else {
      const rel = normalizeRelPath(args.responseFile);
      assertUnderRepo(rel);
      abs = resolve(REPO_ROOT, rel);
    }
    try {
      return readFileSync(abs, 'utf8');
    } catch (e) {
      console.error('Cannot read response file:', e.message);
      process.exit(1);
    }
  }
  console.error(
    'Provide --response-file, --response-json, or --failed --error "..."',
  );
  process.exit(1);
}

const args = parseArgs(process.argv);
if (!args.commandId) {
  console.error('Usage: agent-bridge-complete.mjs --command-id <uuid> ...');
  process.exit(1);
}

const databaseUrl = resolveDatabaseUrl(REPO_ROOT);
if (!databaseUrl) {
  console.error('Missing database URL.');
  process.exit(1);
}

let responseText = null;
let status = 'completed';
let errorText = null;

if (args.failed) {
  status = 'failed';
  errorText = args.error?.trim() || 'Unknown error.';
} else {
  responseText = readResponseBody(args);
  try {
    JSON.parse(responseText);
  } catch (e) {
    console.error('Response body must be valid JSON:', e.message);
    process.exit(1);
  }
}

const client = new pg.Client({ connectionString: databaseUrl });

try {
  await client.connect();
  const { rowCount } = await client.query(
    `UPDATE agent_bridge_job
     SET status = $1,
         response = $2::jsonb,
         error = $3,
         updated_at = now()
     WHERE command_id = $4::uuid AND status = 'processing'`,
    [
      status,
      args.failed ? null : responseText,
      errorText,
      args.commandId.trim(),
    ],
  );
  if (rowCount === 0) {
    console.error(
      JSON.stringify({
        ok: false,
        error: 'no_matching_job',
        message:
          'No processing row for command_id (wrong id, or job not in processing state).',
      }),
    );
    process.exit(1);
  }
  console.log(JSON.stringify({ ok: true }, null, 2));
} catch (e) {
  console.error(e.message || String(e));
  process.exit(1);
} finally {
  await client.end();
}
