/**
 * validate-claude-imports.ts
 *
 * Asserts every `@`-import in CLAUDE.md (Claude Code force-load chain) resolves
 * to an existing file and stays inside a small line-budget. Prevents drift
 * between CLAUDE.md, the rule files it force-loads, and the cache-block
 * sizing contract validated by `validate:cache-block-sizing`.
 *
 * Usage:
 *   npx tsx tools/scripts/validate-claude-imports.ts
 *   npx tsx tools/scripts/validate-claude-imports.ts --claude-md <path>
 *
 * Exit codes:
 *   0  all checks passed
 *   1  one or more `@`-imports missing / over-budget / malformed
 */

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "../..");
const DEFAULT_CLAUDE_MD = path.join(REPO_ROOT, "CLAUDE.md");

// Per-file line budgets. Keep the always-loaded set small. Tune when deliberate.
const LINE_BUDGETS: Record<string, number> = {
  "ia/rules/invariants.md": 60,
  "ia/rules/terminology-consistency.md": 30,
  "ia/rules/agent-output-caveman.md": 40,
};

// Hard ceiling on total @-imports in CLAUDE.md — force-load stays minimal.
const MAX_IMPORTS = 5;

// Hard ceiling on total bytes across all @-imported files.
const MAX_TOTAL_BYTES = 10_000;

interface Failure {
  path: string;
  reason: string;
}

function parseAtImports(body: string): string[] {
  const out: string[] = [];
  for (const line of body.split(/\r?\n/)) {
    const trimmed = line.trim();
    if (!trimmed.startsWith("@")) continue;
    // `@ia/rules/foo.md` — capture literal, strip leading `@`.
    const m = trimmed.match(/^@([\w\-./]+\.md)\b/);
    if (m?.[1]) out.push(m[1]);
  }
  return out;
}

function main() {
  const args = process.argv.slice(2);
  const claudeMdIdx = args.indexOf("--claude-md");
  const claudeMdPath =
    claudeMdIdx !== -1 && args[claudeMdIdx + 1]
      ? path.resolve(args[claudeMdIdx + 1]!)
      : DEFAULT_CLAUDE_MD;

  if (!fs.existsSync(claudeMdPath)) {
    process.stderr.write(
      `validate-claude-imports: CLAUDE.md not found at ${claudeMdPath}\n`,
    );
    process.exit(1);
  }

  const body = fs.readFileSync(claudeMdPath, "utf8");
  const imports = parseAtImports(body);

  const failures: Failure[] = [];

  if (imports.length > MAX_IMPORTS) {
    failures.push({
      path: claudeMdPath,
      reason: `${imports.length} @-imports exceeds MAX_IMPORTS=${MAX_IMPORTS}. Force-loaded set should stay minimal; move task-specific rules to on-demand + reference via router.`,
    });
  }

  let totalBytes = 0;

  for (const rel of imports) {
    const abs = path.join(REPO_ROOT, rel);
    if (!fs.existsSync(abs)) {
      failures.push({
        path: rel,
        reason: `@-imported by CLAUDE.md but file does not exist`,
      });
      continue;
    }
    const content = fs.readFileSync(abs, "utf8");
    const lines = content.split(/\r?\n/).length;
    const bytes = Buffer.byteLength(content, "utf8");
    totalBytes += bytes;

    const budget = LINE_BUDGETS[rel];
    if (budget !== undefined && lines > budget) {
      failures.push({
        path: rel,
        reason: `${lines} lines exceeds budget ${budget}. Trim or split task-specific content into an on-demand rule.`,
      });
    }
  }

  if (totalBytes > MAX_TOTAL_BYTES) {
    failures.push({
      path: claudeMdPath,
      reason: `@-imported files total ${totalBytes} bytes, exceeds MAX_TOTAL_BYTES=${MAX_TOTAL_BYTES}. Keep initial overhead small.`,
    });
  }

  const summary = `validate-claude-imports: ${imports.length} @-import(s), ${totalBytes} bytes, ${failures.length} failure(s)`;

  if (failures.length === 0) {
    process.stdout.write(`${summary}  OK\n`);
    for (const rel of imports) {
      process.stdout.write(`  ok  @${rel}\n`);
    }
    process.exit(0);
  }

  process.stderr.write(`${summary}\n`);
  for (const f of failures) {
    process.stderr.write(`  FAIL  ${f.path}\n        ${f.reason}\n`);
  }
  process.stderr.write(
    `\nFix: adjust @-imports in CLAUDE.md, trim content, or update this validator's LINE_BUDGETS / MAX_IMPORTS / MAX_TOTAL_BYTES constants if the change is deliberate.\n`,
  );
  process.exit(1);
}

main();
