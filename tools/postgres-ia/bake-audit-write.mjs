#!/usr/bin/env node
/**
 * Inserts one row into ia_ui_bake_history (+ child rows into ia_bake_diffs)
 * from a UTF-8 JSON payload file.
 *
 * Usage: node bake-audit-write.mjs --payload-file <absolute-path>
 *
 * Payload shape (JSON object):
 *   panel_slug           string   required
 *   bake_handler_version string   required
 *   diff_summary         object   required  (BakeDiffer.Diff serialized)
 *   commit_sha           string   optional  (defaults to "")
 *   diffs                array    optional  [{change_kind, child_kind, slug, before, after}]
 */

import { readFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import pg from 'pg';
import { resolveDatabaseUrl } from './resolve-database-url.mjs';

const __dirname = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = resolve(__dirname, '../..');

function parseArgs(argv) {
  let payloadFile = null;
  for (let i = 2; i < argv.length; i++) {
    const a = argv[i];
    if (a === '--payload-file' && argv[i + 1]) {
      payloadFile = argv[++i];
    }
  }
  return { payloadFile };
}

async function main() {
  const { payloadFile } = parseArgs(process.argv);
  if (!payloadFile) {
    console.error('Missing --payload-file argument');
    process.exit(1);
  }

  let payload;
  try {
    payload = JSON.parse(readFileSync(payloadFile, 'utf8'));
  } catch (e) {
    console.error(`Failed to read/parse payload file: ${e.message}`);
    process.exit(1);
  }

  const {
    panel_slug,
    bake_handler_version,
    diff_summary = {},
    commit_sha = '',
    diffs = [],
  } = payload;

  if (!panel_slug || !bake_handler_version) {
    console.error('payload must include panel_slug and bake_handler_version');
    process.exit(1);
  }

  const dbUrl = await resolveDatabaseUrl(REPO_ROOT);
  if (!dbUrl) {
    console.error('DATABASE_URL not resolved — skipping bake audit write');
    // Non-fatal: bake audit is best-effort. Exit 0 so bake succeeds.
    process.exit(0);
  }

  const client = new pg.Client({ connectionString: dbUrl });
  try {
    await client.connect();
    await client.query('BEGIN');

    const insertHistory = await client.query(
      `INSERT INTO ia_ui_bake_history
         (panel_slug, bake_handler_version, diff_summary, commit_sha)
       VALUES ($1, $2, $3::jsonb, $4)
       RETURNING id`,
      [
        panel_slug,
        bake_handler_version,
        JSON.stringify(diff_summary),
        commit_sha,
      ],
    );
    const historyId = insertHistory.rows[0].id;

    for (const diff of diffs) {
      await client.query(
        `INSERT INTO ia_bake_diffs
           (history_id, change_kind, child_kind, slug, before, after)
         VALUES ($1, $2, $3, $4, $5::jsonb, $6::jsonb)`,
        [
          historyId,
          diff.change_kind ?? '',
          diff.child_kind ?? '',
          diff.slug ?? '',
          diff.before != null ? JSON.stringify(diff.before) : null,
          diff.after != null ? JSON.stringify(diff.after) : null,
        ],
      );
    }

    await client.query('COMMIT');
    console.log(JSON.stringify({ ok: true, history_id: historyId }));
  } catch (e) {
    await client.query('ROLLBACK').catch(() => {});
    console.error(`bake-audit-write DB error: ${e.message}`);
    // Non-fatal: bake audit is best-effort.
    process.exit(0);
  } finally {
    await client.end().catch(() => {});
  }
}

main();
