#!/usr/bin/env tsx
/**
 * validate-backlog-yaml.ts
 *
 * Schema lint, id uniqueness, counter consistency, and open-yaml ↔ spec
 * cross-check for the per-issue yaml backlog.
 *
 * Checks:
 *   1. Required fields present in every yaml (id, type, title, status, section).
 *   2. Enum validity: status in {open, closed}, priority in {high, medium, low, closed}.
 *   3. Id format: matches /^(TECH|FEAT|BUG|ART|AUDIO)-\d+[a-z]?$/.
 *   4. Id uniqueness across ia/backlog/ + ia/backlog-archive/ combined.
 *   5. Counter consistency: ia/state/id-counter.json per-prefix max >= highest observed id.
 *   6. Open yaml w/ non-empty spec field → ia/projects/{spec} must exist on disk.
 *   7. ia/projects/{ISSUE_ID}*.md with open yaml → yaml must exist in ia/backlog/.
 *
 * Per-record schema checks delegated to shared lint core:
 *   tools/mcp-ia-server/src/parser/backlog-record-schema.ts
 *
 * Usage:
 *   npx tsx tools/validate-backlog-yaml.ts [--advisory]
 *
 * Exit: 0 OK; 1 failure (unless --advisory or VALIDATE_BACKLOG_YAML_ADVISORY=1).
 */

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import {
  validateBacklogRecord,
  parseYamlScalars,
} from "./mcp-ia-server/src/parser/backlog-record-schema.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "..");

const BACKLOG_DIR = path.join(REPO_ROOT, "ia/backlog");
const ARCHIVE_DIR = path.join(REPO_ROOT, "ia/backlog-archive");
const COUNTER_FILE = path.join(REPO_ROOT, "ia/state/id-counter.json");
const PROJECTS_DIR = path.join(REPO_ROOT, "ia/projects");

const PREFIXES = ["TECH", "FEAT", "BUG", "ART", "AUDIO"];
const VALID_PRIORITY = new Set(["high", "medium", "low", "closed"]);
const ID_RE = /^(TECH|FEAT|BUG|ART|AUDIO)-(\d+)[a-z]?$/;

// ---------------------------------------------------------------------------
// Load yaml files from a directory
// ---------------------------------------------------------------------------

function loadDir(dir: string): Array<{ file: string; stem: string; scalars: ReturnType<typeof parseYamlScalars>; content: string }> {
  if (!fs.existsSync(dir)) return [];
  return fs
    .readdirSync(dir)
    .filter((f) => f.endsWith(".yaml"))
    .map((f) => {
      const filePath = path.join(dir, f);
      const content = fs.readFileSync(filePath, "utf8");
      const scalars = parseYamlScalars(content);
      return { file: path.relative(REPO_ROOT, filePath), stem: f.replace(/\.yaml$/, ""), scalars, content };
    });
}

// ---------------------------------------------------------------------------
// Main
// ---------------------------------------------------------------------------

function main(): void {
  const advisory =
    process.argv.includes("--advisory") ||
    process.env["VALIDATE_BACKLOG_YAML_ADVISORY"] === "1";

  const errors: string[] = [];
  const warnings: string[] = [];

  // Check yaml dirs exist
  if (!fs.existsSync(BACKLOG_DIR) || !fs.existsSync(ARCHIVE_DIR)) {
    console.log(
      "validate-backlog-yaml: yaml dirs not present — migration not run yet. OK (skipping).",
    );
    process.exit(0);
  }

  const openRecords = loadDir(BACKLOG_DIR);
  const archiveRecords = loadDir(ARCHIVE_DIR);
  const allRecords = [...openRecords, ...archiveRecords];

  if (allRecords.length === 0) {
    console.log("validate-backlog-yaml: no yaml files found. OK (skipping).");
    process.exit(0);
  }

  // 1. Per-record schema checks (delegated to shared lint core)
  for (const rec of allRecords) {
    const result = validateBacklogRecord(rec.content);
    for (const e of result.errors) {
      errors.push(`${rec.file}: ${e}`);
    }
    for (const w of result.warnings) {
      warnings.push(`${rec.file}: ${w}`);
    }
    // priority check stays here (not in shared core — domain of the script, not schema)
    if (rec.scalars["priority"] && !VALID_PRIORITY.has(rec.scalars["priority"] as string)) {
      warnings.push(`${rec.file}: unexpected priority '${rec.scalars["priority"]}'`);
    }
  }

  // 2. Id uniqueness across both dirs
  const seenIds = new Map<string, string>(); // id → file
  for (const rec of allRecords) {
    const id = (rec.scalars["id"] as string | undefined) || rec.stem;
    if (seenIds.has(id)) {
      errors.push(
        `Duplicate id '${id}': ${seenIds.get(id)} AND ${rec.file}`,
      );
    } else {
      seenIds.set(id, rec.file);
    }
  }

  // Filename must match id field (cross-record check, stays in script)
  for (const rec of allRecords) {
    const id = rec.scalars["id"] as string | undefined;
    if (id && id !== rec.stem) {
      errors.push(`${rec.file}: filename stem '${rec.stem}' does not match id field '${id}'`);
    }
  }

  // 3. Counter consistency
  if (fs.existsSync(COUNTER_FILE)) {
    let counter: Record<string, number> | undefined;
    try {
      counter = JSON.parse(fs.readFileSync(COUNTER_FILE, "utf8")) as Record<string, number>;
    } catch {
      errors.push(`ia/state/id-counter.json: failed to parse JSON`);
    }
    if (counter) {
      const maxObserved: Record<string, number> = {};
      for (const prefix of PREFIXES) maxObserved[prefix] = 0;
      for (const rec of allRecords) {
        const id = (rec.scalars["id"] as string | undefined) || rec.stem;
        const m = ID_RE.exec(id);
        if (!m) continue;
        const prefix = m[1]!;
        const num = parseInt(m[2]!, 10);
        if (maxObserved[prefix] !== undefined && num > maxObserved[prefix]!) {
          maxObserved[prefix] = num;
        }
      }
      for (const prefix of PREFIXES) {
        const counterVal = counter[prefix] ?? 0;
        const observedVal = maxObserved[prefix]!;
        if (counterVal < observedVal) {
          errors.push(
            `Counter stale: id-counter.json[${prefix}]=${counterVal} but max observed ${prefix} id=${observedVal}`,
          );
        }
      }
    }
  } else {
    warnings.push("ia/state/id-counter.json not found — counter consistency check skipped.");
  }

  // 4. Open yaml with spec field → spec file must exist (only when spec looks like a file path)
  const SPEC_PATH_RE = /^ia\/projects\//;
  for (const rec of openRecords) {
    const s = rec.scalars;
    const spec = s["spec"] as string | undefined;
    const status = s["status"] as string | undefined;
    if (status === "open" && spec && spec.trim() && SPEC_PATH_RE.test(spec)) {
      const specPath = path.join(REPO_ROOT, spec);
      if (!fs.existsSync(specPath)) {
        errors.push(
          `${rec.file}: spec '${spec}' referenced but file not found on disk`,
        );
      }
    }
  }

  // 5. ia/projects/{ISSUE_ID}*.md with no open yaml = orphan spec
  if (fs.existsSync(PROJECTS_DIR)) {
    const specFiles = fs.readdirSync(PROJECTS_DIR).filter((f) => f.endsWith(".md"));
    const openIdSet = new Set(openRecords.map((r) => r.stem));
    const SPEC_ID_RE = /^((?:BUG|FEAT|TECH|ART|AUDIO)-\d+[a-z]?)(?:-|$)/;
    for (const specFile of specFiles) {
      const m = SPEC_ID_RE.exec(specFile);
      if (!m) continue;
      const issueId = m[1]!;
      if (!openIdSet.has(issueId)) {
        const archiveIdSet = new Set(archiveRecords.map((r) => r.stem));
        if (!archiveIdSet.has(issueId)) {
          warnings.push(
            `ia/projects/${specFile}: no matching yaml in ia/backlog/ or ia/backlog-archive/ for id '${issueId}'`,
          );
        }
        if (archiveIdSet.has(issueId)) {
          warnings.push(
            `ia/projects/${specFile}: id '${issueId}' is in archive but spec file still exists (closeout may be incomplete)`,
          );
        }
      }
    }
  }

  // Report
  if (errors.length === 0 && warnings.length === 0) {
    console.log(
      `validate-backlog-yaml: OK (${allRecords.length} records, ${openRecords.length} open, ${archiveRecords.length} archived).`,
    );
    process.exit(0);
  }

  for (const w of warnings) {
    console.warn(`[warn] ${w}`);
  }
  for (const e of errors) {
    console.error(`[error] ${e}`);
  }

  if (errors.length > 0) {
    console.error(
      `\nvalidate-backlog-yaml: ${errors.length} error(s), ${warnings.length} warning(s).`,
    );
    if (advisory) {
      console.error("(advisory mode: exit 0)");
      process.exit(0);
    }
    process.exit(1);
  }

  // Warnings only
  console.log(
    `validate-backlog-yaml: OK with ${warnings.length} warning(s) — see above.`,
  );
  process.exit(0);
}

main();
