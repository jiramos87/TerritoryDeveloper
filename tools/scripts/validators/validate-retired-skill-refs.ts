/**
 * validate-retired-skill-refs.ts — TECH-14103
 *
 * Greps scan_paths for references to retired skill slugs.
 * Soft-fail (warn + exit 0) until hard_fail_after date.
 * Hard-fail (exit 1) after the calendar flip date.
 *
 * Exclusions: git log output, CHANGELOG.md, archive tarball, design-decisions doc,
 * and this config file itself (F13 false-positive guard).
 *
 * CLI: tsx tools/scripts/validators/validate-retired-skill-refs.ts
 */

import fs from "node:fs";
import path from "node:path";
import { execSync } from "node:child_process";

const REPO_ROOT = path.resolve(process.cwd());
const CONFIG_PATH = path.join(
  REPO_ROOT,
  "tools/scripts/validators/_retired-skill-refs.config.json",
);

interface Config {
  retired_slugs: string[];
  hard_fail_after: string;
  scope_excludes: string[];
  scan_paths: string[];
}

function loadConfig(): Config {
  const raw = fs.readFileSync(CONFIG_PATH, "utf8");
  return JSON.parse(raw) as Config;
}

function isHardFail(hard_fail_after: string): boolean {
  const today = new Date();
  const flipDate = new Date(hard_fail_after);
  return today >= flipDate;
}

function buildGrepPattern(slugs: string[]): string {
  // Match slug references in prose: /slug, skill-name, command name refs
  return slugs.map((s) => `\\b${s}\\b`).join("|");
}

interface Hit {
  file: string;
  line: number;
  text: string;
  slug: string;
}

function scanForRefs(config: Config): Hit[] {
  const hits: Hit[] = [];
  const pattern = buildGrepPattern(config.retired_slugs);

  for (const scanPath of config.scan_paths) {
    const absPath = path.join(REPO_ROOT, scanPath);
    if (!fs.existsSync(absPath)) continue;

    let result: string;
    try {
      result = execSync(
        `grep -rn --include="*.md" --include="*.ts" --include="*.mdc" -E "${pattern}" "${absPath}" 2>/dev/null || true`,
        { encoding: "utf8", cwd: REPO_ROOT },
      );
    } catch {
      continue;
    }

    for (const rawLine of result.split("\n")) {
      if (!rawLine.trim()) continue;

      // Parse grep output: file:line:text
      const colonIdx = rawLine.indexOf(":");
      if (colonIdx < 0) continue;
      const afterFirst = rawLine.indexOf(":", colonIdx + 1);
      if (afterFirst < 0) continue;

      const filePath = rawLine.slice(0, colonIdx);
      const lineNum = parseInt(rawLine.slice(colonIdx + 1, afterFirst), 10);
      const text = rawLine.slice(afterFirst + 1);

      // Check exclusions
      const excluded = config.scope_excludes.some((excl) =>
        filePath.includes(excl) || text.includes(excl),
      );
      if (excluded) continue;

      // Also exclude _retired/ paths (archived, not live references)
      if (filePath.includes("_retired/")) continue;
      // Also exclude _preamble (shared cache block)
      if (filePath.includes("_preamble/")) continue;

      // Identify which slug matched
      const matchedSlug = config.retired_slugs.find((s) => {
        const re = new RegExp(`\\b${s}\\b`);
        return re.test(text);
      });
      if (!matchedSlug) continue;

      hits.push({ file: filePath, line: lineNum, text: text.trim(), slug: matchedSlug });
    }
  }

  return hits;
}

function main() {
  const config = loadConfig();
  const hits = scanForRefs(config);
  const hardFail = isHardFail(config.hard_fail_after);

  if (hits.length === 0) {
    console.log("validate:retired-skill-refs — OK (0 hits)");
    process.exit(0);
  }

  const label = hardFail ? "ERROR" : "WARN";
  console.log(
    `validate:retired-skill-refs — ${label}: ${hits.length} retired-slug reference(s) found`,
  );
  if (!hardFail) {
    console.log(
      `  Soft-fail until ${config.hard_fail_after}. Hard-fail flips after that date.`,
    );
  }

  for (const h of hits) {
    console.log(`  [${h.slug}] ${h.file}:${h.line}  ${h.text}`);
  }

  // JSON output for CI parsers
  const output = {
    hits,
    hard_fail_after: config.hard_fail_after,
    hard_fail_mode: hardFail,
  };
  console.log("\n" + JSON.stringify(output, null, 2));

  process.exit(hardFail ? 1 : 0);
}

main();
