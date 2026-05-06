#!/usr/bin/env node
// Canonical asset-tree validator — warn-only (always exit 0).
// Regex per family from asset-families.json (single source of truth).
// Generated/ + Placeholders/ paths exempt (zero output for those).
// Phase 8 fold-in: family taxonomy auto-sourced from same JSON.

import { readFileSync } from 'node:fs';
import { execSync } from 'node:child_process';
import { join, dirname, basename } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dir = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = execSync('git rev-parse --show-toplevel').toString().trim();

// Load family taxonomy (single source of truth)
const familiesPath = join(__dir, 'asset-families.json');
const { families, exempt_path_substrings: EXEMPT } = JSON.parse(
  readFileSync(familiesPath, 'utf8')
);

// Compile regexes once
const FAMILY_REGEXES = Object.entries(families).map(([name, def]) => ({
  name,
  regex: new RegExp(def.regex, 'i'),
}));

// Scan roots
const SCAN_ROOTS = ['Assets/Sprites', 'Assets/Prefabs'];

// Gather all asset files (non-.meta, non-.DS_Store)
const rawLines = execSync(
  `git ls-files ${SCAN_ROOTS.join(' ')}`,
  { cwd: REPO_ROOT }
)
  .toString()
  .split('\n')
  .filter((f) => f && !f.endsWith('.meta') && !f.endsWith('.DS_Store'));

const stats = {
  total: 0,
  exempt: 0,
  matched: {},
  unknown: [],
};

for (const name of Object.keys(families)) {
  stats.matched[name] = 0;
}

for (const rel of rawLines) {
  stats.total++;

  // Check exempt substrings
  if (EXEMPT.some((sub) => rel.includes(sub))) {
    stats.exempt++;
    continue;
  }

  const fname = basename(rel);

  // Attempt family match
  let matched = false;
  for (const { name, regex } of FAMILY_REGEXES) {
    if (regex.test(fname)) {
      stats.matched[name]++;
      matched = true;
      break;
    }
  }

  if (!matched) {
    stats.unknown.push(rel);
  }
}

// Emit results
console.log('[validate:asset-tree-canonical] Scan complete.');
console.log(`  Total files scanned : ${stats.total}`);
console.log(`  Exempt (skip)       : ${stats.exempt}`);
console.log(`  Matched by family   :`);
for (const [name, count] of Object.entries(stats.matched)) {
  console.log(`    ${name.padEnd(14)}: ${count}`);
}

if (stats.unknown.length === 0) {
  console.log('[validate:asset-tree-canonical] PASS — all non-exempt assets match a known family.');
} else {
  console.warn(
    `[validate:asset-tree-canonical] WARN — ${stats.unknown.length} asset(s) did not match any family regex:`
  );
  for (const u of stats.unknown) {
    console.warn(`  WARN  ${u}`);
  }
  console.warn(
    '  Action: update asset-families.json regex or rename asset to match canonical pattern.'
  );
  console.warn('  Lock: warn-only (exit 0) — no CI block until lock promoted.');
}

// Always exit 0 — warn-only by lock
process.exit(0);
