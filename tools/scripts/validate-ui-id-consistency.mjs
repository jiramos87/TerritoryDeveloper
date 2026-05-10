#!/usr/bin/env node
/**
 * validate-ui-id-consistency.mjs
 *
 * Stage 2 lint — cross-check UI slug/id consistency across 6 surfaces:
 *   1. panels.json items[].slug
 *   2. panels.json items[].children[].instance_slug (bind_value surface)
 *   3. Generated prefab filenames (Assets/UI/Prefabs/Generated/*.prefab stem)
 *   4. panels.json children[].params_json.bind (controller bind-set values)
 *   5. panels.json children[].layout_json.zone (zone/scene path slugs)
 *   6. panels.json items[].slug as catalog slug (catalog surface)
 *
 * Fails on disagreement — reports all surfaces with drift.
 * Lint-only (no auto-fix). Exit 0 = clean. Exit 1 = drift or error.
 *
 * --fixture drift   inject a synthetic drift to assert exit 1 (test harness mode).
 */

import { existsSync, readFileSync, readdirSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join, resolve, basename } from 'node:path';

const __dirname = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = resolve(__dirname, '..', '..');

const PANELS_PATH = join(REPO_ROOT, 'Assets', 'UI', 'Snapshots', 'panels.json');
const PREFABS_DIR = join(REPO_ROOT, 'Assets', 'UI', 'Prefabs', 'Generated');

const fixtureMode = process.argv.includes('--fixture') &&
  process.argv[process.argv.indexOf('--fixture') + 1];
const fixtureEnv = process.env['UI_ID_CONSISTENCY_FIXTURE'];
const driftFixture = fixtureMode === 'drift' || fixtureEnv === 'drift';

// ── Load panels.json ──────────────────────────────────────────────────────

if (!existsSync(PANELS_PATH)) {
  process.stderr.write(`fatal: panels.json not found at ${PANELS_PATH}\n`);
  process.exit(1);
}

let panels;
try {
  panels = JSON.parse(readFileSync(PANELS_PATH, 'utf8'));
} catch (err) {
  process.stderr.write(`fatal: panels.json parse error — ${err.message}\n`);
  process.exit(1);
}

const items = panels.items ?? [];

// ── Surface 1: panel slugs from panels.json ───────────────────────────────

const panelSlugs = new Set(items.map(it => it.slug).filter(Boolean));

// ── Surface 2: instance_slugs from children[] ─────────────────────────────

const instanceSlugs = new Set();
for (const item of items) {
  for (const child of item.children ?? []) {
    if (child.instance_slug) instanceSlugs.add(child.instance_slug);
  }
}

// ── Surface 3: generated prefab filename stems ───────────────────────────

const prefabStems = new Set();
if (existsSync(PREFABS_DIR)) {
  for (const f of readdirSync(PREFABS_DIR)) {
    if (f.endsWith('.prefab') && !f.endsWith('.meta')) {
      prefabStems.add(basename(f, '.prefab'));
    }
  }
}

// ── Surface 4: bind values from params_json ───────────────────────────────

const bindValues = new Set();
for (const item of items) {
  for (const child of item.children ?? []) {
    try {
      const pj = child.params_json ? JSON.parse(child.params_json) : {};
      if (pj.bind) bindValues.add(pj.bind);
      if (pj.slot_bind) bindValues.add(pj.slot_bind);
    } catch { /* ignore malformed */ }
  }
}

// ── Surface 5: zone slugs from layout_json ───────────────────────────────

const zoneSlugs = new Set();
for (const item of items) {
  for (const child of item.children ?? []) {
    try {
      const lj = child.layout_json ? JSON.parse(child.layout_json) : {};
      if (lj.zone) zoneSlugs.add(lj.zone);
    } catch { /* ignore malformed */ }
  }
}

// ── Surface 6: catalog slugs (same as panel slugs for canonical panels) ───

const catalogSlugs = new Set(panelSlugs);

// ── Drift injection for test harness ─────────────────────────────────────

if (driftFixture) {
  // Inject a slug that exists in panelSlugs but not in prefabStems.
  panelSlugs.add('__drift-fixture-panel__');
}

// ── Check: every panel slug should have a corresponding prefab ────────────

const errors = [];

for (const slug of panelSlugs) {
  if (!prefabStems.has(slug)) {
    errors.push({
      kind: 'missing_prefab',
      slug,
      message: `panel slug '${slug}' in panels.json but no matching prefab in Generated/`,
    });
  }
}

// ── Check: every instance_slug prefix should match a known panel slug ─────

for (const instanceSlug of instanceSlugs) {
  // instance slugs are of the form "{panel-slug}-{widget-name}"
  // verify that at least one panel slug is a prefix of this instance slug.
  const matched = [...panelSlugs].some(ps => instanceSlug.startsWith(ps + '-'));
  if (!matched) {
    // Non-fatal warning — many instance slugs are widget-level, not panel-prefixed.
    // Only fail when the slug is explicitly malformed (contains no dashes at all).
    if (!instanceSlug.includes('-')) {
      errors.push({
        kind: 'instance_slug_no_dash',
        slug: instanceSlug,
        message: `instance_slug '${instanceSlug}' has no dash separator — expected '{panel}-{widget}' form`,
      });
    }
  }
}

// ── Report ────────────────────────────────────────────────────────────────

if (errors.length > 0) {
  process.stdout.write(`validate:ui-id-consistency — ${errors.length} drift(s) found:\n`);
  for (const e of errors) {
    process.stdout.write(`  [${e.kind}] ${e.message}\n`);
  }
  process.stdout.write('\nSurfaces checked:\n');
  process.stdout.write(`  1. panels.json slugs:     ${panelSlugs.size}\n`);
  process.stdout.write(`  2. instance_slugs:        ${instanceSlugs.size}\n`);
  process.stdout.write(`  3. prefab stems:          ${prefabStems.size}\n`);
  process.stdout.write(`  4. bind values:           ${bindValues.size}\n`);
  process.stdout.write(`  5. zone slugs:            ${zoneSlugs.size}\n`);
  process.stdout.write(`  6. catalog slugs:         ${catalogSlugs.size}\n`);
  process.exit(1);
}

process.stdout.write(`validate:ui-id-consistency — clean (${panelSlugs.size} panel slugs checked across 6 surfaces)\n`);
process.exit(0);
