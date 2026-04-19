#!/usr/bin/env node
/**
 * Report references to project spec paths that do not exist.
 *
 * The canonical location is `ia/projects/{ISSUE_ID}[-{description}].md`.
 *
 * Usage:
 *   node tools/validate-dead-project-spec-paths.mjs [--advisory]
 *
 * Exit: 0 if no dead targets; 1 if any found (unless --advisory or CI_DEAD_SPEC_ADVISORY=1).
 *
 * BACKLOG.md: only `Spec:` lines in **open** (`- [ ]`) top-level issue rows are checked,
 * so "promote to `ia/projects/TECH-48.md`" in Notes does not false-positive.
 * BACKLOG-ARCHIVE.md: skipped (historical rows may cite removed specs).
 */

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { loadRepoDotenvIfNotCi } from "./postgres-ia/repo-dotenv.mjs";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "..");
loadRepoDotenvIfNotCi(REPO_ROOT);

/**
 * Project spec path scan: accepts `ia/projects/{ID}[-{description}].md`.
 */
const PROJECT_SPEC_PATH_RE =
  /ia\/projects\/((?:BUG|FEAT|TECH|ART|AUDIO)-\d+[a-z]?(?:-[A-Za-z0-9._-]+)?)\.md/gi;

/** Entire `Spec:` value is only a project-spec path (BACKLOG convention). */
const BACKLOG_SPEC_LINE_RE =
  /^(\s*)-\s*Spec:\s*`(ia\/projects\/(?:BUG|FEAT|TECH|ART|AUDIO)-\d+[a-z]?(?:-[A-Za-z0-9._-]+)?\.md)`\s*$/;

const IGNORE_DIR_NAMES = new Set([
  "node_modules",
  ".git",
  "Library",
  "Temp",
  "obj",
  "Build",
  "Logs",
  // Lifecycle refactor (TECH-443): frozen pre-refactor snapshot may cite specs
  // already deleted on closeout; skip to avoid false positives.
  "pre-refactor-snapshot",
]);

const TEXT_EXTENSIONS = new Set([
  ".md",
  ".yml",
  ".yaml",
  ".json",
  ".ts",
  ".tsx",
  ".js",
  ".mjs",
  ".cjs",
]);

/**
 * @param {string} content
 * @returns {{ isOpen: boolean, lines: string[] }[]}
 */
function splitBacklogTopLevelBlocks(content) {
  const lines = content.split(/\r?\n/);
  /** @type {{ isOpen: boolean, lines: string[] }[]} */
  const blocks = [];
  /** @type {{ isOpen: boolean, lines: string[] } | null} */
  let current = null;

  for (const line of lines) {
    const top = /^- \[([ x])\]/.exec(line);
    if (top) {
      if (current) blocks.push(current);
      current = { isOpen: top[1] === " ", lines: [line] };
    } else if (current) {
      current.lines.push(line);
    }
  }
  if (current) blocks.push(current);
  return blocks;
}

/**
 * @param {string} repoRoot
 * @param {string} relFromRoot posix-style `ia/projects/X.md`
 */
function specPathExists(repoRoot, relFromRoot) {
  const fsPath = path.join(repoRoot, ...relFromRoot.split("/"));
  return fs.existsSync(fsPath);
}

/**
 * @param {string} filePath absolute
 * @param {string} repoRoot
 * @param {{ file: string, line: number, target: string }[]} hits
 */
function scanNonBacklogFile(filePath, repoRoot, hits) {
  const relFile = path.relative(repoRoot, filePath);
  const text = fs.readFileSync(filePath, "utf8");
  const lines = text.split(/\r?\n/);

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    let m;
    PROJECT_SPEC_PATH_RE.lastIndex = 0;
    while ((m = PROJECT_SPEC_PATH_RE.exec(line)) !== null) {
      const id = m[1];
      const rel = `ia/projects/${id}.md`;
      if (!specPathExists(repoRoot, rel)) {
        hits.push({ file: relFile, line: i + 1, target: rel });
      }
    }
  }
}

/**
 * @param {string} backlogPath
 * @param {string} repoRoot
 * @param {{ file: string, line: number, target: string }[]} hits
 */
function scanBacklogOpenSpecLines(backlogPath, repoRoot, hits) {
  const text = fs.readFileSync(backlogPath, "utf8");
  const relFile = path.relative(repoRoot, backlogPath);
  const blocks = splitBacklogTopLevelBlocks(text);

  for (const block of blocks) {
    if (!block.isOpen) continue;
    const blockText = block.lines.join("\n");
    const startIndex = text.indexOf(blockText);
    const prefix = startIndex >= 0 ? text.slice(0, startIndex) : "";
    const baseLine = prefix.split(/\r?\n/).length;

    for (let j = 0; j < block.lines.length; j++) {
      const line = block.lines[j];
      const sm = BACKLOG_SPEC_LINE_RE.exec(line);
      if (!sm) continue;
      const rel = sm[2];
      if (!specPathExists(repoRoot, rel)) {
        hits.push({ file: relFile, line: baseLine + j, target: rel });
      }
    }
  }
}

/**
 * @param {string} dir absolute
 * @param {string} repoRoot
 * @param {string[]} out
 */
function collectTextFiles(dir, repoRoot, out) {
  let entries;
  try {
    entries = fs.readdirSync(dir, { withFileTypes: true });
  } catch {
    return;
  }
  for (const ent of entries) {
    const abs = path.join(dir, ent.name);
    const rel = path.relative(repoRoot, abs);
    let isDir = ent.isDirectory();
    let isFile = ent.isFile();
    if (!isDir && !isFile && ent.isSymbolicLink()) {
      try {
        const st = fs.statSync(abs);
        isDir = st.isDirectory();
        isFile = st.isFile();
      } catch {
        continue;
      }
    }
    if (isDir) {
      if (IGNORE_DIR_NAMES.has(ent.name)) continue;
      collectTextFiles(abs, repoRoot, out);
    } else if (isFile) {
      const ext = path.extname(ent.name).toLowerCase();
      if (!TEXT_EXTENSIONS.has(ext)) continue;
      if (rel === "BACKLOG-ARCHIVE.md") continue;
      out.push(abs);
    }
  }
}

function main() {
  const advisory =
    process.argv.includes("--advisory") ||
    process.env.CI_DEAD_SPEC_ADVISORY === "1";

  /** @type {{ file: string, line: number, target: string }[]} */
  const hits = [];

  const backlogPath = path.join(REPO_ROOT, "BACKLOG.md");
  if (fs.existsSync(backlogPath)) {
    scanBacklogOpenSpecLines(backlogPath, REPO_ROOT, hits);
  }

  /** @type {string[]} */
  const files = [];

  for (const name of ["AGENTS.md", "ARCHITECTURE.md", "README.md"]) {
    const p = path.join(REPO_ROOT, name);
    if (fs.existsSync(p)) files.push(p);
  }

  for (const sub of ["ia", "docs", "projects", ".github"]) {
    const d = path.join(REPO_ROOT, sub);
    if (fs.existsSync(d)) collectTextFiles(d, REPO_ROOT, files);
  }
  // Exclude per-issue yaml records — they are mined from BACKLOG.md / BACKLOG-ARCHIVE.md
  // and carry "promote to" spec paths that are not yet created. Proper yaml validation
  // happens in validate:backlog-yaml.
  const YAML_DIRS = new Set([
    path.normalize(path.join(REPO_ROOT, "ia/backlog")),
    path.normalize(path.join(REPO_ROOT, "ia/backlog-archive")),
    path.normalize(path.join(REPO_ROOT, "ia/state")),
  ]);

  const seen = new Set();
  const uniqueFiles = [];
  for (const f of files) {
    const norm = path.normalize(f);
    if (seen.has(norm)) continue;
    seen.add(norm);
    uniqueFiles.push(norm);
  }

  for (const abs of uniqueFiles) {
    const rel = path.relative(REPO_ROOT, abs);
    if (rel === "BACKLOG.md") continue;
    if (rel === "BACKLOG-ARCHIVE.md") continue;
    // Skip per-issue yaml dirs (validated separately by validate:backlog-yaml)
    const absNorm = path.normalize(abs);
    const dirNorm = path.normalize(path.dirname(abs));
    if (YAML_DIRS.has(dirNorm)) continue;
    scanNonBacklogFile(abs, REPO_ROOT, hits);
  }

  if (hits.length === 0) {
    console.log("validate-dead-project-spec-paths: OK (no missing ia/projects/*.md targets).");
    process.exit(0);
  }

  console.error("validate-dead-project-spec-paths: missing project spec path(s):\n");
  const byTarget = new Map();
  for (const h of hits) {
    if (!byTarget.has(h.target)) byTarget.set(h.target, []);
    byTarget.get(h.target).push(h);
  }
  for (const [target, list] of [...byTarget.entries()].sort()) {
    console.error(`  ${target}`);
    for (const h of list) {
      console.error(`    ${h.file}:${h.line}`);
    }
  }
  console.error(
    "\nFix: point durable docs at BACKLOG.md / BACKLOG-ARCHIVE.md by issue id, or restore the spec file. See PROJECT-SPEC-STRUCTURE.md (lifecycle / closeout).",
  );

  if (advisory) {
    console.error("\n(advisory mode: exit 0)");
    process.exit(0);
  }
  process.exit(1);
}

main();
