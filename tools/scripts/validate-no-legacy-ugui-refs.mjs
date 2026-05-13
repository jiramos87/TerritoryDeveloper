#!/usr/bin/env node
// validate-no-legacy-ugui-refs.mjs
// TECH-32930 Stage 6.0 (ui-toolkit-migration) — advisory baseline scanner.
// Greps Assets/Scripts/UI/** for legacy uGUI / quarantined refs:
//   Canvas | CanvasRenderer | RectTransform | UiBindRegistry | UiTheme (non-Obsolete callsite heuristic).
// Reports counts per category — does NOT fail (exit 1). Advisory only.
// Final purge plan owns the zero-gate; this script sets the baseline.
// Excludes: .archive/ subtrees, Obsolete attribute lines, this script itself.

import { readdirSync, readFileSync, statSync } from 'fs';
import { join, resolve } from 'path';
import { fileURLToPath } from 'url';

const REPO_ROOT = resolve(fileURLToPath(import.meta.url), '../../..');
const SCAN_DIR = join(REPO_ROOT, 'Assets/Scripts/UI');

/** Pattern table: { label, regex } — regex matches source lines (not file names). */
const PATTERNS = [
  { label: 'Canvas (uGUI)', regex: /\bCanvas\b(?!Renderer|Group|Scaler)/ },
  { label: 'CanvasRenderer', regex: /\bCanvasRenderer\b/ },
  { label: 'RectTransform', regex: /\bRectTransform\b/ },
  { label: 'UiBindRegistry', regex: /\bUiBindRegistry\b/ },
  { label: 'UiTheme', regex: /\bUiTheme\b/ },
];

/** Walk *.cs files; skip .archive/ subtrees. */
function* walkCs(dir) {
  for (const entry of readdirSync(dir)) {
    const full = join(dir, entry);
    const st = statSync(full);
    if (st.isDirectory()) {
      if (entry === '.archive') continue;
      yield* walkCs(full);
    } else if (entry.endsWith('.cs')) {
      yield full;
    }
  }
}

/** Counts per pattern across all scanned files. */
const counts = PATTERNS.map(() => 0);
const details = PATTERNS.map(() => /** @type {string[]} */ ([]));

for (const absPath of walkCs(SCAN_DIR)) {
  const repoRel = absPath.slice(REPO_ROOT.length + 1).replace(/\\/g, '/');
  const lines = readFileSync(absPath, 'utf-8').split('\n');

  for (let lineIdx = 0; lineIdx < lines.length; lineIdx++) {
    const line = lines[lineIdx];
    // Skip Obsolete attribute lines and XML doc lines (/// <see cref=...>).
    if (/^\s*\[Obsolete/.test(line)) continue;
    if (/^\s*\/\/\//.test(line)) continue;
    if (/^\s*\/\//.test(line)) continue;

    for (let pi = 0; pi < PATTERNS.length; pi++) {
      if (PATTERNS[pi].regex.test(line)) {
        counts[pi]++;
        details[pi].push(`  ${repoRel}:${lineIdx + 1}`);
      }
    }
  }
}

const total = counts.reduce((a, b) => a + b, 0);

console.log('validate-no-legacy-ugui-refs — advisory baseline (exit 0 always)');
console.log('-------------------------------------------------------------------');
for (let i = 0; i < PATTERNS.length; i++) {
  const label = PATTERNS[i].label.padEnd(24);
  console.log(`  ${label}: ${counts[i]} refs`);
  if (counts[i] > 0 && counts[i] <= 30) {
    for (const d of details[i]) console.log(d);
  } else if (counts[i] > 30) {
    console.log(`    ... (${counts[i]} total; run grep manually for full list)`);
  }
}
console.log('-------------------------------------------------------------------');
console.log(`  TOTAL LEGACY REFS: ${total}`);
console.log('  Advisory only — zero-gate owned by uGUI purge plan (TECH-32931).');

// Always exit 0 — advisory, not a hard gate.
process.exit(0);
