#!/usr/bin/env node
/**
 * backfill-master-plan-preamble-and-stage-blocks.mjs
 *
 * One-shot backfill for Step 9.6.7 of the IA dev DB-primary refactor.
 *
 * Reads the folded master-plan repo state under `ia/projects/{slug}/`:
 *   - `index.md` → upserts `ia_master_plans.preamble` (verbatim file content).
 *   - `stage-{id}-{name}.md` (one per stage) → upserts
 *     `ia_stages.objective` + `ia_stages.exit_criteria` parsed from the
 *     `**Objectives:**` and `**Exit:**` blocks, plus stages get the FULL
 *     stage file body persisted via `ia_master_plans.preamble` siblings —
 *     no, stage blocks land in objective + exit_criteria columns only;
 *     verbatim render later derives from structured fields.
 *
 * Idempotent: re-run UPDATE wins; missing files leave columns as-is.
 *
 * Skips master plans absent from `ia_master_plans` (warns to stderr) and
 * stages absent from `ia_stages` (warns once per stage).
 *
 * Usage:
 *   node tools/scripts/backfill-master-plan-preamble-and-stage-blocks.mjs [--dry-run]
 */

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import pg from 'pg';
import { resolveDatabaseUrl } from './resolve-database-url.mjs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, '../..');
const PROJECTS_DIR = path.join(REPO_ROOT, 'ia', 'projects');

const DRY_RUN = process.argv.includes('--dry-run');

const STAGE_FILE_RE = /^stage-([0-9]+(?:\.[0-9]+)?)-.+\.md$/;

// ---------------------------------------------------------------------------
// Filesystem walk
// ---------------------------------------------------------------------------

function listSlugs() {
  return fs.readdirSync(PROJECTS_DIR, { withFileTypes: true })
    .filter((d) => d.isDirectory() && !d.name.startsWith('_'))
    .map((d) => d.name)
    .sort();
}

function readIndexMaybe(slug) {
  const p = path.join(PROJECTS_DIR, slug, 'index.md');
  if (!fs.existsSync(p)) return null;
  return fs.readFileSync(p, 'utf8');
}

function listStageFiles(slug) {
  const dir = path.join(PROJECTS_DIR, slug);
  if (!fs.existsSync(dir)) return [];
  return fs.readdirSync(dir)
    .filter((f) => STAGE_FILE_RE.test(f))
    .sort();
}

function stageIdFromFilename(filename) {
  const m = filename.match(STAGE_FILE_RE);
  return m ? m[1] : null;
}

// ---------------------------------------------------------------------------
// Stage-body parser
// ---------------------------------------------------------------------------

/**
 * Pull the inline `**Objectives:**` value (single paragraph) from a stage body.
 * Returns null when the marker is absent.
 *
 * Pattern: `**Objectives:** {prose}` — value runs until the next blank line OR
 * the next bold-key marker (`**Exit:**`, `**Tasks:**`, etc.).
 */
function extractObjective(body) {
  const lines = body.split('\n');
  for (let i = 0; i < lines.length; i++) {
    const m = lines[i].match(/^\*\*Objectives?:\*\*\s*(.*)$/);
    if (!m) continue;
    const collected = [m[1]];
    for (let j = i + 1; j < lines.length; j++) {
      const next = lines[j];
      if (next.trim() === '') break;
      if (/^\*\*[^*]+:\*\*/.test(next)) break;
      collected.push(next);
    }
    return collected.join('\n').trim() || null;
  }
  return null;
}

/**
 * Pull the `**Exit:**` block. The exit value is typically a list (one or more
 * `- ...` lines + interleaved phase notes). Capture from the line AFTER the
 * `**Exit:**` marker until the next bold-key marker (`**Tasks:**` / `**Decision Log:**` / etc.) OR end of file.
 */
function extractExitCriteria(body) {
  const lines = body.split('\n');
  for (let i = 0; i < lines.length; i++) {
    if (!/^\*\*Exit:?\*\*/.test(lines[i])) continue;
    // Same-line content (rare) + following block.
    const sameLine = lines[i].replace(/^\*\*Exit:?\*\*\s*/, '').trim();
    const collected = [];
    if (sameLine) collected.push(sameLine);
    for (let j = i + 1; j < lines.length; j++) {
      const next = lines[j];
      if (/^\*\*[A-Za-z][^*]*:\*\*/.test(next)) break;
      collected.push(next);
    }
    const joined = collected.join('\n').replace(/\s+$/, '');
    return joined.trim() ? joined.replace(/^\n+/, '') : null;
  }
  return null;
}

// ---------------------------------------------------------------------------
// DB upserts
// ---------------------------------------------------------------------------

async function getKnownSlugs(client) {
  const { rows } = await client.query('SELECT slug FROM ia_master_plans');
  return new Set(rows.map((r) => r.slug));
}

async function getKnownStageKeys(client) {
  const { rows } = await client.query('SELECT slug, stage_id FROM ia_stages');
  const set = new Set();
  for (const r of rows) set.add(`${r.slug}::${r.stage_id}`);
  return set;
}

async function updatePreamble(client, slug, preamble) {
  if (DRY_RUN) return;
  await client.query(
    'UPDATE ia_master_plans SET preamble = $1, updated_at = now() WHERE slug = $2',
    [preamble, slug],
  );
}

async function updateStage(client, slug, stageId, objective, exitCriteria) {
  if (DRY_RUN) return;
  await client.query(
    `UPDATE ia_stages
        SET objective = COALESCE($1, objective),
            exit_criteria = COALESCE($2, exit_criteria),
            updated_at = now()
      WHERE slug = $3 AND stage_id = $4`,
    [objective, exitCriteria, slug, stageId],
  );
}

// ---------------------------------------------------------------------------
// Main
// ---------------------------------------------------------------------------

async function main() {
  const url = resolveDatabaseUrl(REPO_ROOT);
  if (!url) {
    process.stderr.write('No DATABASE_URL — abort.\n');
    process.exit(1);
  }
  const client = new pg.Client({ connectionString: url });
  await client.connect();
  try {
    const knownSlugs = await getKnownSlugs(client);
    const knownStageKeys = await getKnownStageKeys(client);

    const counts = {
      preambleUpdated: 0,
      preambleSkippedNoSlug: 0,
      preambleSkippedNoFile: 0,
      stagesUpdated: 0,
      stagesSkippedNoRow: 0,
      stagesNoMarkers: 0,
    };

    for (const slug of listSlugs()) {
      if (!knownSlugs.has(slug)) {
        counts.preambleSkippedNoSlug++;
        process.stderr.write(`skip preamble — slug not in ia_master_plans: ${slug}\n`);
        continue;
      }

      const preamble = readIndexMaybe(slug);
      if (preamble == null) {
        counts.preambleSkippedNoFile++;
        process.stderr.write(`skip preamble — no index.md for ${slug}\n`);
      } else {
        await updatePreamble(client, slug, preamble);
        counts.preambleUpdated++;
      }

      for (const file of listStageFiles(slug)) {
        const stageId = stageIdFromFilename(file);
        if (!stageId) continue;
        const key = `${slug}::${stageId}`;
        if (!knownStageKeys.has(key)) {
          counts.stagesSkippedNoRow++;
          process.stderr.write(`skip stage — not in ia_stages: ${slug}/${stageId} (file ${file})\n`);
          continue;
        }
        const body = fs.readFileSync(path.join(PROJECTS_DIR, slug, file), 'utf8');
        const objective = extractObjective(body);
        const exitCriteria = extractExitCriteria(body);
        if (objective == null && exitCriteria == null) {
          counts.stagesNoMarkers++;
          process.stderr.write(`stage has no Objectives/Exit markers: ${slug}/${stageId}\n`);
          continue;
        }
        await updateStage(client, slug, stageId, objective, exitCriteria);
        counts.stagesUpdated++;
      }
    }

    const summary = {
      mode: DRY_RUN ? 'dry-run' : 'apply',
      ...counts,
    };
    process.stdout.write(JSON.stringify(summary, null, 2) + '\n');
  } finally {
    await client.end();
  }
}

main().catch((err) => {
  process.stderr.write(`backfill failed: ${err && err.stack ? err.stack : err}\n`);
  process.exit(1);
});
