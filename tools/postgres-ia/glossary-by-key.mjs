#!/usr/bin/env node
/**
 * Proof read path (TECH-44b §7 Phase 2a): fetch one glossary row by term_key.
 * Usage: node glossary-by-key.mjs <term_key>
 * Example: node glossary-by-key.mjs heightmap
 */

import pg from 'pg';

const termKey = process.argv[2]?.trim();
if (!termKey) {
  console.error('Usage: node glossary-by-key.mjs <term_key>');
  process.exit(1);
}

const databaseUrl = process.env.DATABASE_URL?.trim();
if (!databaseUrl) {
  console.error('Missing DATABASE_URL. See docs/postgres-ia-dev-setup.md.');
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
