#!/usr/bin/env node
/**
 * Optional seed (TECH-44b §7 Phase 3): first N glossary table rows from
 * ia/specs/glossary.md (| Term | Definition | Spec | blocks only).
 * Idempotent: ON CONFLICT (term_key) DO UPDATE.
 */

import { readFile } from 'node:fs/promises';
import { join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { dirname } from 'node:path';
import pg from 'pg';
import { loadRepoDotenvIfNotCi } from './repo-dotenv.mjs';
import { resolveDatabaseUrl } from './resolve-database-url.mjs';

const __dirname = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = resolve(__dirname, '../..');
loadRepoDotenvIfNotCi(REPO_ROOT);
const GLOSSARY_PATH = join(REPO_ROOT, 'ia/specs/glossary.md');

const MAX_ROWS = Number.parseInt(process.env.SEED_GLOSSARY_MAX ?? '20', 10);

function termKeyFromCell(raw) {
  return raw
    .replace(/^\*\*|\*\*$/g, '')
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_+|_+$/g, '');
}

function parseTableRows(text) {
  const lines = text.split(/\r?\n/);
  const rows = [];
  let category = '';
  let inTermTable = false;

  for (const line of lines) {
    const h2 = line.match(/^## (.+)$/);
    if (h2) {
      category = h2[1].trim();
      inTermTable = false;
      continue;
    }
    if (line.includes('| Term |') && line.includes('| Definition |')) {
      inTermTable = true;
      continue;
    }
    if (!inTermTable) continue;
    if (!line.trim().startsWith('|')) {
      if (line.trim() === '') continue;
      inTermTable = false;
      continue;
    }
    if (/^\|\s*---/.test(line)) continue;

    const cells = line.split('|').map((c) => c.trim());
    if (cells.length < 4) continue;
    const termCell = cells[1];
    const defCell = cells[2];
    const specCell = cells[3];
    if (!termCell || termCell === 'Term') continue;

    const termKey = termKeyFromCell(termCell);
    if (!termKey) continue;

    rows.push({
      term_key: termKey,
      term: termCell.replace(/^\*\*|\*\*$/g, '').trim(),
      definition: defCell || null,
      spec_key: specCell || null,
      category,
    });
  }

  return rows;
}

const databaseUrl = resolveDatabaseUrl(REPO_ROOT);
if (!databaseUrl) {
  console.error(
    'Missing database URL: set DATABASE_URL or config/postgres-dev.json. See docs/postgres-ia-dev-setup.md.',
  );
  process.exit(1);
}

const md = await readFile(GLOSSARY_PATH, 'utf8');
const parsed = parseTableRows(md).slice(0, MAX_ROWS);

if (parsed.length === 0) {
  console.error('No glossary rows parsed from', GLOSSARY_PATH);
  process.exit(1);
}

const client = new pg.Client({ connectionString: databaseUrl });
await client.connect();

let inserted = 0;
for (const r of parsed) {
  await client.query(
    `INSERT INTO glossary (term_key, term, definition, spec_key, category)
     VALUES ($1, $2, $3, $4, $5)
     ON CONFLICT (term_key) DO UPDATE SET
       term = EXCLUDED.term,
       definition = EXCLUDED.definition,
       spec_key = EXCLUDED.spec_key,
       category = EXCLUDED.category,
       updated_at = now()`,
    [r.term_key, r.term, r.definition, r.spec_key, r.category]
  );
  inserted += 1;
}

await client.end();
console.log(`Seed complete: upserted ${inserted} glossary row(s) (cap ${MAX_ROWS}).`);
