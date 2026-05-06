#!/usr/bin/env node
// Single-owner Esc routing lint — enforces `Input.GetKeyDown(KeyCode.Escape)` lives only in
// `UIManager.cs` (canonical `HandleEscapePress()` routing). Any other matching line in
// `Assets/Scripts/**/*.cs` is a regression: dual-handler races collapsed the Stage 8 D9
// PopupStack design (TECH-14102) — pause-menu opened over selected tools because GridManager
// raced UIManager to consume the keydown.

import { readFileSync } from 'node:fs';
import { globSync } from 'node:fs';
import { execSync } from 'node:child_process';

const REPO_ROOT = execSync('git rev-parse --show-toplevel').toString().trim();
const ALLOWED = new Set([
  'Assets/Scripts/Managers/GameManagers/UIManager.cs',
]);

const files = execSync(
  "git ls-files 'Assets/Scripts/**/*.cs'",
  { cwd: REPO_ROOT }
)
  .toString()
  .split('\n')
  .filter((f) => f && !f.startsWith('Assets/Scripts/Tests/'));

const violations = [];
const pattern = /Input\.GetKeyDown\(KeyCode\.Escape\)/;

for (const rel of files) {
  const abs = `${REPO_ROOT}/${rel}`;
  let body;
  try { body = readFileSync(abs, 'utf8'); } catch { continue; }
  const lines = body.split('\n');
  for (let i = 0; i < lines.length; i++) {
    if (pattern.test(lines[i]) && !ALLOWED.has(rel)) {
      violations.push({ file: rel, line: i + 1, text: lines[i].trim() });
    }
  }
}

if (violations.length === 0) {
  console.log('[validate:single-esc-handler] OK — single-owner Esc routing intact (UIManager only).');
  process.exit(0);
}

console.error(`[validate:single-esc-handler] FAIL — ${violations.length} stray Esc handler(s) outside UIManager:`);
for (const v of violations) {
  console.error(`  ${v.file}:${v.line}  ${v.text}`);
}
console.error(
  '\nFix: route through UIManager.HandleEscapePress + popupStack frames (Stage 8 D9 / TECH-14102).\n' +
  'Allowlist: tools/scripts/validate-single-esc-handler.mjs (ALLOWED set).'
);
process.exit(1);
