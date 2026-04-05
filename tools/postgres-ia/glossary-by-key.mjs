#!/usr/bin/env node
/**
 * Proof read path (TECH-44b §7 Phase 2a): fetch one glossary row by term_key.
 * Usage: node glossary-by-key.mjs <term_key>
 * Example: node glossary-by-key.mjs heightmap
 */

import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import pg from 'pg';
import { resolveDatabaseUrl } from './resolve-database-url.mjs';

const __dirname = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = resolve(__dirname, '../..');

const termKey = process.argv[2]?.trim();
if (!termKey) {
  console.error('Usage: node glossary-by-key.mjs <term_key>');
  process.exit(1);
}

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
  'SELECT * FROM ia_glossary_row_by_key($1)',
  [termKey]
);

await client.end();

if (rows.length === 0) {
  console.log(JSON.stringify({ found: false, term_key: termKey }, null, 2));
  process.exit(2);
}

console.log(JSON.stringify({ found: true, row: rows[0] }, null, 2));
