#!/usr/bin/env node
/**
 * Applies ordered SQL files under db/migrations/ using psql (full-file DDL).
 * Records versions in schema_migrations after each successful file.
 *
 * Requires: psql on PATH, and DATABASE_URL or config/postgres-dev.json (see docs/postgres-ia-dev-setup.md).
 */

import { readdir } from 'node:fs/promises';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';
import pg from 'pg';
import { resolveDatabaseUrl } from './resolve-database-url.mjs';

const __dirname = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = resolve(__dirname, '../..');
const MIGRATIONS_DIR = join(REPO_ROOT, 'db/migrations');

function requireDatabaseUrl() {
  const url = resolveDatabaseUrl(REPO_ROOT);
  if (!url) {
    console.error(
      'Missing database URL: set DATABASE_URL or add config/postgres-dev.json. See docs/postgres-ia-dev-setup.md.',
    );
    process.exit(1);
  }
  return url;
}

async function listMigrationFiles() {
  const names = await readdir(MIGRATIONS_DIR);
  return names
    .filter((n) => n.endsWith('.sql'))
    .sort();
}

function applySqlWithPsql(databaseUrl, filePath) {
  const r = spawnSync(
    'psql',
    [databaseUrl, '-v', 'ON_ERROR_STOP=1', '-f', filePath],
    { stdio: 'inherit', env: process.env }
  );
  if (r.error) {
    console.error(r.error.message);
    console.error('Install PostgreSQL client tools (psql) or apply SQL files manually; see docs/postgres-ia-dev-setup.md.');
    process.exit(1);
  }
  if (r.status !== 0) process.exit(r.status ?? 1);
}

async function main() {
  const databaseUrl = requireDatabaseUrl();
  const files = await listMigrationFiles();
  if (files.length === 0) {
    console.error('No .sql files in', MIGRATIONS_DIR);
    process.exit(1);
  }

  const client = new pg.Client({ connectionString: databaseUrl });
  await client.connect();

  async function isVersionApplied(version) {
    try {
      const { rows } = await client.query(
        'SELECT 1 FROM schema_migrations WHERE version = $1',
        [version]
      );
      return rows.length > 0;
    } catch (e) {
      if (e.code === '42P01') return false;
      throw e;
    }
  }

  for (const name of files) {
    const version = name.replace(/\.sql$/i, '');
    if (await isVersionApplied(version)) {
      console.log('skip', version);
      continue;
    }

    const fullPath = join(MIGRATIONS_DIR, name);
    console.log('apply', version);
    applySqlWithPsql(databaseUrl, fullPath);

    await client.query('INSERT INTO schema_migrations (version) VALUES ($1)', [version]);
  }

  await client.end();
  console.log('Migrations complete.');
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
