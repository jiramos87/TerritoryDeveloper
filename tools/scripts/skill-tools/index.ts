#!/usr/bin/env tsx
// CLI entry for skill-tools. Subcommands: sync, lint.
// Usage:
//   tsx tools/scripts/skill-tools/index.ts sync [<slug>] [--apply] [--diff]
//   tsx tools/scripts/skill-tools/index.ts sync:all [--apply]
//   tsx tools/scripts/skill-tools/index.ts lint [<slug>] [--json] [--strict]

import { listSkillSlugs } from "./frontmatter.js";
import { lint } from "./lint.js";
import { syncAll, syncSlugs, shortDiffSummary } from "./sync.js";

interface ParsedFlags {
  apply: boolean;
  diff: boolean;
  json: boolean;
  strict: boolean;
}

function parseFlags(argv: string[]): { positional: string[]; flags: ParsedFlags } {
  const positional: string[] = [];
  const flags: ParsedFlags = { apply: false, diff: false, json: false, strict: false };
  for (const a of argv) {
    if (a === "--apply") flags.apply = true;
    else if (a === "--diff") flags.diff = true;
    else if (a === "--json") flags.json = true;
    else if (a === "--strict") flags.strict = true;
    else positional.push(a);
  }
  return { positional, flags };
}

function printUsage(): void {
  // eslint-disable-next-line no-console
  console.error([
    "skill-tools — sync + lint over canonical SKILL.md frontmatter",
    "",
    "Usage:",
    "  tsx tools/scripts/skill-tools/index.ts sync [<slug>] [--apply] [--diff]",
    "  tsx tools/scripts/skill-tools/index.ts sync:all [--apply]",
    "  tsx tools/scripts/skill-tools/index.ts lint [<slug>] [--json] [--strict]",
    "",
    "Default sync = diff-only; exits 1 on drift. --apply writes files.",
    "Default lint = warnings non-fatal; --strict treats warnings as errors.",
  ].join("\n"));
}

async function runSync(slugArg: string | undefined, flags: ParsedFlags): Promise<number> {
  const slugs = slugArg ? [slugArg] : listSkillSlugs();
  const result = syncSlugs(slugs, { apply: flags.apply, diff: flags.diff });
  for (const r of result.results) {
    if (r.driftCount > 0 || flags.diff) {
      // eslint-disable-next-line no-console
      console.log(`[${r.slug}] ${r.driftCount === 0 ? "clean" : `${r.driftCount} drift`}`);
      // eslint-disable-next-line no-console
      console.log(shortDiffSummary(r));
    }
  }
  // eslint-disable-next-line no-console
  console.log(
    `\nsync: ${slugs.length} skill(s) checked — drift=${result.totalDrift} written=${result.filesWritten}`
  );
  if (flags.apply) return 0;
  return result.totalDrift > 0 ? 1 : 0;
}

async function runSyncAll(flags: ParsedFlags): Promise<number> {
  const result = syncAll({ apply: flags.apply, diff: false });
  // eslint-disable-next-line no-console
  console.log(
    `sync:all — ${result.results.length} skill(s); drift=${result.totalDrift} written=${result.filesWritten}`
  );
  for (const r of result.results) {
    if (r.driftCount === 0) continue;
    // eslint-disable-next-line no-console
    console.log(`[${r.slug}]`);
    // eslint-disable-next-line no-console
    console.log(shortDiffSummary(r));
  }
  if (flags.apply) return 0;
  return result.totalDrift > 0 ? 1 : 0;
}

async function runLint(slugArg: string | undefined, flags: ParsedFlags): Promise<number> {
  const slugs = slugArg ? [slugArg] : undefined;
  const report = lint({ slugs, promoteWarnings: flags.strict });
  if (flags.json) {
    // eslint-disable-next-line no-console
    console.log(JSON.stringify(report, null, 2));
  } else {
    // eslint-disable-next-line no-console
    console.log(
      `lint — ${report.slugs_checked} skill(s); errors=${report.errors} warnings=${report.warnings}`
    );
    for (const f of report.findings) {
      const tag = f.severity === "error" ? "ERROR" : "WARN ";
      // eslint-disable-next-line no-console
      console.log(`  ${tag} [${f.slug}] ${f.check}: ${f.message}`);
    }
  }
  return report.errors > 0 ? 1 : 0;
}

const argv = process.argv.slice(2);
const subcommand = argv[0];
const { positional, flags } = parseFlags(argv.slice(1));

let exitPromise: Promise<number>;
if (subcommand === "sync") {
  exitPromise = runSync(positional[0], flags);
} else if (subcommand === "sync:all") {
  exitPromise = runSyncAll(flags);
} else if (subcommand === "lint") {
  exitPromise = runLint(positional[0], flags);
} else {
  printUsage();
  process.exit(2);
}

exitPromise
  .then((code) => process.exit(code))
  .catch((err) => {
    // eslint-disable-next-line no-console
    console.error(err);
    process.exit(2);
  });
