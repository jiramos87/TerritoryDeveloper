#!/usr/bin/env npx tsx
/**
 * List repo text files that cite a backlog issue id or its project spec path — TECH-58.
 * Usage: npx tsx scripts/project-spec-dependents.ts --issue TECH-75
 *
 * Limitations: may miss plain mentions without word boundaries; BACKLOG-ARCHIVE.md is scanned
 * (historical rows often cite closed specs). Does not replace manual umbrella/sibling review.
 */

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { resolveRepoRoot } from "../src/config.js";
import {
  PROJECT_SPEC_ISSUE_ID_RE,
  normalizeIssueId,
} from "../src/parser/project-spec-closeout-parse.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
if (!process.env.REPO_ROOT) {
  process.env.REPO_ROOT = path.resolve(__dirname, "../../..");
}

const IGNORE_DIR_NAMES = new Set([
  "node_modules",
  ".git",
  "Library",
  "Temp",
  "obj",
  "Build",
  "Logs",
]);

const TEXT_EXTENSIONS = new Set([
  ".md",
  ".mdc",
  ".yml",
  ".yaml",
  ".json",
  ".ts",
  ".tsx",
  ".js",
  ".mjs",
  ".cjs",
]);

function collectTextFiles(dir: string, repoRoot: string, out: string[]): void {
  let entries: fs.Dirent[];
  try {
    entries = fs.readdirSync(dir, { withFileTypes: true });
  } catch {
    return;
  }
  for (const ent of entries) {
    const abs = path.join(dir, ent.name);
    if (ent.isDirectory()) {
      if (IGNORE_DIR_NAMES.has(ent.name)) continue;
      collectTextFiles(abs, repoRoot, out);
    } else if (ent.isFile()) {
      const ext = path.extname(ent.name).toLowerCase();
      if (!TEXT_EXTENSIONS.has(ext)) continue;
      out.push(abs);
    }
  }
}

function parseIssue(argv: string[]): string | null {
  for (let i = 0; i < argv.length; i++) {
    if (argv[i] === "--issue" && argv[i + 1]) return argv[i + 1].trim();
  }
  return null;
}

function main(): void {
  const raw = parseIssue(process.argv.slice(2));
  if (!raw) {
    console.error("Usage: project-spec-dependents.ts --issue TECH-75");
    process.exit(1);
  }
  const issue_id = normalizeIssueId(raw);
  if (!PROJECT_SPEC_ISSUE_ID_RE.test(issue_id)) {
    console.error("Invalid issue_id.");
    process.exit(1);
  }

  const repoRoot = resolveRepoRoot();
  const specPathLiteral = `.cursor/projects/${issue_id}.md`;
  const idRe = new RegExp(`\\b${issue_id.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}\\b`, "i");

  const files: string[] = [];
  for (const name of ["AGENTS.md", "ARCHITECTURE.md", "README.md", "BACKLOG.md"]) {
    const p = path.join(repoRoot, name);
    if (fs.existsSync(p)) files.push(p);
  }
  for (const sub of [".cursor", "docs", "projects", ".github"]) {
    const d = path.join(repoRoot, sub);
    if (fs.existsSync(d)) collectTextFiles(d, repoRoot, files);
  }

  const seen = new Set<string>();
  const unique = files.filter((f) => {
    const n = path.normalize(f);
    if (seen.has(n)) return false;
    seen.add(n);
    return true;
  });

  const hits: { file: string; line: number; text: string }[] = [];
  for (const abs of unique.sort()) {
    const text = fs.readFileSync(abs, "utf8");
    const lines = text.split(/\r?\n/);
    const rel = path.relative(repoRoot, abs);
    for (let i = 0; i < lines.length; i++) {
      const line = lines[i];
      if (line.includes(specPathLiteral) || idRe.test(line)) {
        hits.push({ file: rel.split(path.sep).join("/"), line: i + 1, text: line.trim() });
      }
      idRe.lastIndex = 0;
    }
  }

  if (hits.length === 0) {
    console.log(`project-spec-dependents: no hits for ${issue_id} / ${specPathLiteral}`);
    process.exit(0);
  }

  console.log(`project-spec-dependents: ${hits.length} line(s) citing ${issue_id} or path\n`);
  for (const h of hits) {
    console.log(`${h.file}:${h.line}: ${h.text}`);
  }
}

main();
