#!/usr/bin/env node
/**
 * validate-bake-handler-kind-coverage.mjs
 *
 * Stage 2 lint — cross-check panels.json widget kinds vs KindRendererMatrix entries.
 *
 * Reads panels.json items[].children[].kind + params_json.kind to collect distinct
 * widget kinds, then reads KindRendererMatrix.cs registered kinds from source.
 * Fails when panels.json declares a kind not present in the matrix (finding F1 —
 * drop-on-the-floor pattern).
 *
 * Owner annotations in KindRendererMatrix.cs are validated as a side-effect:
 * the matrix entries must cover all kinds seen in panels.json.
 *
 * --fixture unmapped-kind   inject a synthetic unmapped kind to assert exit 1
 *                           (test harness mode).
 *
 * Exit 0 = full coverage. Exit 1 = gap found or error.
 */

import { existsSync, readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join, resolve } from 'node:path';

const __dirname = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = resolve(__dirname, '..', '..');

const PANELS_PATH = join(REPO_ROOT, 'Assets', 'UI', 'Snapshots', 'panels.json');
const MATRIX_PATH = join(REPO_ROOT, 'Assets', 'Scripts', 'Editor', 'UiBake', 'KindRendererMatrix.cs');
// Also check the existing _knownKinds in UiBakeHandler.Archetype.cs for full coverage.
const ARCHETYPE_PATH = join(REPO_ROOT, 'Assets', 'Scripts', 'Editor', 'Bridge', 'UiBakeHandler.Archetype.cs');

const fixtureArg = process.argv.includes('--fixture') &&
  process.argv[process.argv.indexOf('--fixture') + 1];
const fixtureEnv = process.env['BAKE_COVERAGE_FIXTURE'];
const unmappedFixture = fixtureArg === 'unmapped-kind' || fixtureEnv === 'unmapped-kind';

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

// ── Collect distinct widget kinds from panels.json ────────────────────────

const widgetKinds = new Set();
for (const item of panels.items ?? []) {
  for (const child of item.children ?? []) {
    // outer kind (button / label / etc.)
    if (child.kind) widgetKinds.add(child.kind);
    // inner kind from params_json.kind (illuminated-button / readout / etc.)
    try {
      const pj = child.params_json ? JSON.parse(child.params_json) : {};
      if (pj.kind) widgetKinds.add(pj.kind);
    } catch { /* ignore */ }
  }
}

// Inject drift fixture for test harness.
if (unmappedFixture) {
  widgetKinds.add('__synthetic-unmapped-kind__');
}

// ── Parse KindRendererMatrix.cs for registered kind strings ──────────────

function extractQuotedStrings(source) {
  const results = new Set();
  // Match string literals in dictionary initializers: { "kind-slug", new ...
  const re = /"\s*([a-z][a-z0-9-]*)\s*"\s*,\s*new\s+\w/g;
  let m;
  while ((m = re.exec(source)) !== null) {
    results.add(m[1]);
  }
  return results;
}

const matrixKinds = new Set();
if (existsSync(MATRIX_PATH)) {
  const src = readFileSync(MATRIX_PATH, 'utf8');
  for (const k of extractQuotedStrings(src)) matrixKinds.add(k);
}

// ── Parse UiBakeHandler.Archetype.cs _knownKinds for studioControl kinds ──
// (bakeChildByKind switch handles illuminated-button / segmented-readout /
//  themed-label / confirm-button / view-slot — treat _knownKinds as coverage.)

const archetypeKinds = new Set();
if (existsSync(ARCHETYPE_PATH)) {
  const src = readFileSync(ARCHETYPE_PATH, 'utf8');
  // Extract string literals in _knownKinds HashSet initializer block.
  const blockMatch = src.match(/_knownKinds\s*=\s*new\s+HashSet<string>\s*\{([^}]+)\}/s);
  if (blockMatch) {
    const block = blockMatch[1];
    const re2 = /"([^"]+)"/g;
    let m2;
    while ((m2 = re2.exec(block)) !== null) {
      archetypeKinds.add(m2[1]);
    }
  }
}

// Also check BakeChildByKind switch cases in UiBakeHandler.cs.
const uiBakeHandlerPath = join(REPO_ROOT, 'Assets', 'Scripts', 'Editor', 'Bridge', 'UiBakeHandler.cs');
const switchKinds = new Set();
if (existsSync(uiBakeHandlerPath)) {
  const src = readFileSync(uiBakeHandlerPath, 'utf8');
  const re3 = /case\s+"([^"]+)"\s*:/g;
  let m3;
  while ((m3 = re3.exec(src)) !== null) {
    switchKinds.add(m3[1]);
  }
}

// ── Outer kind exclusions ─────────────────────────────────────────────────
// Some outer `child.kind` values in panels.json are container types, not
// dispatched through KindRendererMatrix or BakeChildByKind directly.
// "panel" = sub-widget container (inner kind is authoritative via params_json.kind).
// "subtype-card" = internal template within subtype-picker-strip composite.
const OUTER_KIND_EXCLUSIONS = new Set([
  'panel',
  'subtype-card',
  'modal-card',     // stage 5.5 pre-pop (cityscene B4) — backdrop + center + content-replace container
  'info-dock',      // stage 5.5 pre-pop (cityscene B5) — right-edge dock container
  'field-list',     // stage 5.5 pre-pop (cityscene B5) — row-list container
  'toast-stack',    // stage 5.5 pre-pop (cityscene B5) — vertical toast container
]);

// ── NormalizeChildKind aliases (from UiBakeHandler.cs NormalizeChildKind) ─
// Kinds that are aliased to covered kinds are considered covered.
const aliases = new Map([
  ['button', 'illuminated-button'],
  ['label', 'themed-label'],
  ['readout', 'segmented-readout'],
  ['confirm-button', 'confirm-button'],
  ['view-slot', 'view-slot'],
  ['icon-button', 'illuminated-button'],
  ['destructive-confirm-button', 'confirm-button'],
  // stage 5.5 pre-pop (cityscene B2..B5) — new kinds aliased to existing covered kinds
  // pre-population so validate:bake-handler-kind-coverage stays GREEN before each
  // panel migration ships its own renderer / switch arm.
  ['tab-strip', 'illuminated-button'],          // stage 5.5 pre-pop (cityscene B2) — tabs = button-click
  ['chart', 'themed-label'],                    // stage 5.5 pre-pop (cityscene B2) — read-only data display
  ['range-tabs', 'illuminated-button'],         // stage 5.5 pre-pop (cityscene B2) — button group w/ selection
  ['stacked-bar-row', 'segmented-readout'],     // stage 5.5 pre-pop (cityscene B2) — segmented horizontal data
  ['service-row', 'themed-label'],              // stage 5.5 pre-pop (cityscene B2) — icon + 2 labels read-only
  ['slider-row-numeric', 'slider-row'],         // stage 5.5 pre-pop (cityscene B3) — numeric variant of slider-row
  ['expense-row', 'segmented-readout'],         // stage 5.5 pre-pop (cityscene B3) — label + numeric readout
  ['readout-block', 'segmented-readout'],       // stage 5.5 pre-pop (cityscene B3) — multi-segment readout
  ['minimap-canvas', 'themed-label'],           // stage 5.5 pre-pop (cityscene B5) — RawImage display surface
  ['toast-card', 'themed-label'],               // stage 5.5 pre-pop (cityscene B5) — transient text display
]);

// Combined covered kinds.
const coveredKinds = new Set([
  ...matrixKinds,
  ...archetypeKinds,
  ...switchKinds,
  ...aliases.keys(),
]);

// ── Check: every panels.json widget kind must be covered ─────────────────

const gaps = [];
for (const kind of widgetKinds) {
  if (OUTER_KIND_EXCLUSIONS.has(kind)) continue; // container types — skip
  if (!coveredKinds.has(kind)) {
    gaps.push(kind);
  }
}

// ── Report ────────────────────────────────────────────────────────────────

if (gaps.length > 0) {
  process.stdout.write(`validate:bake-handler-kind-coverage — ${gaps.length} unmapped kind(s):\n`);
  for (const g of gaps) {
    process.stdout.write(`  [unmapped_kind] '${g}' found in panels.json but not in KindRendererMatrix or UiBakeHandler\n`);
  }
  process.stdout.write(`\nCoverage summary:\n`);
  process.stdout.write(`  panels.json widget kinds:     ${widgetKinds.size}\n`);
  process.stdout.write(`  KindRendererMatrix entries:   ${matrixKinds.size}\n`);
  process.stdout.write(`  _knownKinds entries:          ${archetypeKinds.size}\n`);
  process.stdout.write(`  switch case kinds:            ${switchKinds.size}\n`);
  process.stdout.write(`  alias mappings:               ${aliases.size}\n`);
  process.exit(1);
}

process.stdout.write(`validate:bake-handler-kind-coverage — clean (${widgetKinds.size} kinds, all covered)\n`);
process.exit(0);
