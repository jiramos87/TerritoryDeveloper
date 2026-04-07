#!/usr/bin/env node
/**
 * Claim one pending agent_bridge_job row (FOR UPDATE SKIP LOCKED) and set status to processing.
 * Prints JSON to stdout for Unity: { ok, empty?, command_id, kind, request }.
 *
 * Requires: DATABASE_URL or config/postgres-dev.json; migration 0008 applied.
 */

import pg from 'pg';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { resolveDatabaseUrl } from './resolve-database-url.mjs';

const __dirname = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = resolve(__dirname, '../..');

const databaseUrl = resolveDatabaseUrl(REPO_ROOT);
if (!databaseUrl) {
  console.error(
    JSON.stringify({
      ok: false,
      error: 'no_database_url',
      message:
        'Missing DATABASE_URL or config/postgres-dev.json. See docs/postgres-ia-dev-setup.md.',
    }),
  );
  process.exit(1);
}

const client = new pg.Client({ connectionString: databaseUrl });

try {
  await client.connect();
  let rows;
  try {
    await client.query('BEGIN');
    const r = await client.query(
      `WITH c AS (
         SELECT id FROM agent_bridge_job
         WHERE status = 'pending'
         ORDER BY id ASC
         FOR UPDATE SKIP LOCKED
         LIMIT 1
       )
       UPDATE agent_bridge_job j
       SET status = 'processing', updated_at = now()
       FROM c
       WHERE j.id = c.id
       RETURNING j.command_id, j.kind, j.request`,
    );
    rows = r.rows;
    await client.query('COMMIT');
  } catch (e) {
    try {
      await client.query('ROLLBACK');
    } catch {
      // ignored
    }
    throw e;
  }

  if (rows.length === 0) {
    console.log(JSON.stringify({ ok: true, empty: true }));
  } else {
    const row = rows[0];
    console.log(
      JSON.stringify({
        ok: true,
        empty: false,
        command_id: String(row.command_id),
        kind: row.kind,
        request_json: JSON.stringify(row.request),
      }),
    );
  }
} catch (e) {
  console.error(e.message || String(e));
  process.exit(1);
} finally {
  await client.end();
}
