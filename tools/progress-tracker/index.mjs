#!/usr/bin/env node
/**
 * index.mjs — CLI entrypoint for the master-plan HTML progress tracker.
 *
 * Usage: node tools/progress-tracker/index.mjs [--plans-dir <path>] [--out <path>]
 * Invoked via: npm run progress (root package.json)
 *
 * Reads all ia/projects/*master-plan*.md files, parses them,
 * renders docs/progress.html, and writes the output.
 * Exit 0 on success; non-zero on error.
 */

import { readFileSync, writeFileSync, readdirSync, existsSync } from 'node:fs';
import { join, resolve, dirname, basename } from 'node:path';
import { fileURLToPath } from 'node:url';
import { parseMasterPlan } from './parse.mjs';
import { renderHtml } from './render.mjs';

const __dirname = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = resolve(__dirname, '..', '..');

// ─── Argument parsing (minimal, no external deps) ──────────────────────────

const args = process.argv.slice(2);

function getArg(flag, defaultVal) {
  const idx = args.indexOf(flag);
  if (idx !== -1 && args[idx + 1]) return args[idx + 1];
  return defaultVal;
}

const plansDir = resolve(getArg('--plans-dir', join(REPO_ROOT, 'ia', 'projects')));
const outPath = resolve(getArg('--out', join(REPO_ROOT, 'docs', 'progress.html')));

// ─── Main ──────────────────────────────────────────────────────────────────

try {
  if (!existsSync(plansDir)) {
    console.log(`[progress-tracker] No filesystem plans dir at ${plansDir} — DB-backed master plans; skipping HTML render.`);
    process.exit(0);
  }
  const allFiles = readdirSync(plansDir);
  const planFiles = allFiles
    .filter(f => f.includes('master-plan') && f.endsWith('.md'))
    .sort(); // deterministic order

  if (planFiles.length === 0) {
    console.error(`[progress-tracker] No master-plan files found in ${plansDir}`);
    process.exit(1);
  }

  console.log(`[progress-tracker] Parsing ${planFiles.length} master plan(s):`);
  const plans = planFiles.map(f => {
    const fullPath = join(plansDir, f);
    const content = readFileSync(fullPath, 'utf8');
    const plan = parseMasterPlan(content, f);
    const doneTasks = plan.allTasks.filter(t => t.status === 'Done (archived)').length;
    const total = plan.allTasks.length;
    const pct = total > 0 ? Math.round((doneTasks / total) * 100) : 0;
    console.log(`  ${f}: ${doneTasks}/${total} tasks Done (${pct}%)`);
    return plan;
  });

  const html = renderHtml(plans);
  writeFileSync(outPath, html, 'utf8');
  console.log(`[progress-tracker] Written: ${outPath}`);
} catch (err) {
  console.error('[progress-tracker] Error:', err.message);
  process.exit(1);
}
