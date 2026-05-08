#!/usr/bin/env node
/**
 * run-csharp-lint.mjs — C# soft-cap lint enforcer.
 *
 * Soft caps:
 *   method: warn=40 LOC, err=80 LOC
 *   line:   warn=120 chars, err=160 chars
 *   file:   warn=400 LOC, err=700 LOC
 *
 * Escape hatches:
 *   // long-method-allowed: {reason}  → suppresses method err, emits warn with reason
 *   // long-file-allowed: {reason}    → suppresses file err, emits warn with reason
 *
 * Scope: Assets/Scripts/**\/*.cs + Assets/Tests/**\/*.cs
 *
 * Exit codes:
 *   0 = clean (no errors; warnings may exist)
 *   1 = lint errors found
 *
 * Flags:
 *   --warn-only   don't exit 1 on errors (CI soft mode)
 *   --diff        only lint files changed in git diff HEAD
 *   --file <path> lint a single file
 */

import { readFileSync, existsSync } from "node:fs";
import { resolve, dirname, relative } from "node:path";
import { fileURLToPath } from "node:url";
import { execSync } from "node:child_process";
import { glob } from "node:fs/promises";

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const REPO_ROOT = resolve(__dirname, "../..");

const WARN_ONLY = process.argv.includes("--warn-only");
const DIFF_ONLY = process.argv.includes("--diff");
const FILE_ARG_IDX = process.argv.indexOf("--file");
const SINGLE_FILE = FILE_ARG_IDX !== -1 ? resolve(process.argv[FILE_ARG_IDX + 1]) : null;

const CAPS = {
  method: { warn: 40, err: 80 },
  line: { warn: 120, err: 160 },
  file: { warn: 400, err: 700 },
};

const SCAN_GLOBS = ["Assets/Scripts/**/*.cs", "Assets/Tests/**/*.cs"];
const SKIP_DIRS = ["Plugins", "ThirdParty", "Library", "Temp", "obj", "bin", "Generated"];

// ---------------------------------------------------------------------------
// File enumeration
// ---------------------------------------------------------------------------

async function listFiles() {
  if (SINGLE_FILE) return [SINGLE_FILE];

  if (DIFF_ONLY) {
    try {
      const out = execSync("git diff --name-only HEAD", { cwd: REPO_ROOT, encoding: "utf8" });
      return out
        .split("\n")
        .filter((f) => f.endsWith(".cs"))
        .map((f) => resolve(REPO_ROOT, f))
        .filter((f) => existsSync(f));
    } catch {
      return [];
    }
  }

  const files = [];
  for (const pattern of SCAN_GLOBS) {
    for await (const f of glob(pattern, { cwd: REPO_ROOT })) {
      const full = resolve(REPO_ROOT, f);
      if (SKIP_DIRS.some((d) => full.includes(`/${d}/`))) continue;
      files.push(full);
    }
  }
  return files;
}

// ---------------------------------------------------------------------------
// Method LOC counter (very simple: brace-balanced, ignores nesting nuance)
// ---------------------------------------------------------------------------

/**
 * Extract method bodies and their LOC counts from a C# source string.
 * Returns: Array of { name, startLine, locCount, hasEscapeHatch, escapeReason }
 */
function extractMethods(src) {
  const lines = src.split("\n");
  const methods = [];

  // Simplified regex: matches method/constructor signatures (not property bodies)
  const methodSignatureRe =
    /^\s*(public|private|protected|internal|static|override|virtual|abstract|async|sealed)[\w\s<>\[\],?*]*\s+(\w+)\s*\(([^)]*)\)\s*(\{|$)/;

  let i = 0;
  while (i < lines.length) {
    const line = lines[i];

    // Check for escape hatch comment on the line before the method
    let hasEscapeHatch = false;
    let escapeReason = "";
    if (i > 0) {
      const prevLine = lines[i - 1].trim();
      const hatchMatch = prevLine.match(/^\/\/\s*long-method-allowed:\s*(.+)$/);
      if (hatchMatch) {
        hasEscapeHatch = true;
        escapeReason = hatchMatch[1].trim();
      }
    }

    const sigMatch = line.match(methodSignatureRe);
    if (sigMatch) {
      const methodName = sigMatch[2];
      const startLine = i + 1;

      // Find opening brace
      let braceStart = i;
      while (braceStart < lines.length && !lines[braceStart].includes("{")) braceStart++;
      if (braceStart >= lines.length) { i++; continue; }

      // Count balanced braces
      let depth = 0;
      let endLine = braceStart;
      for (let j = braceStart; j < lines.length; j++) {
        for (const ch of lines[j]) {
          if (ch === "{") depth++;
          else if (ch === "}") { depth--; if (depth === 0) { endLine = j; break; } }
        }
        if (depth === 0 && j >= braceStart) break;
      }

      const locCount = endLine - braceStart; // body lines (excluding braces themselves)
      methods.push({ name: methodName, startLine, locCount, hasEscapeHatch, escapeReason });
      i = endLine + 1;
      continue;
    }
    i++;
  }
  return methods;
}

// ---------------------------------------------------------------------------
// File-level escape hatch
// ---------------------------------------------------------------------------

function getFileEscapeHatch(src) {
  for (const line of src.split("\n").slice(0, 5)) {
    const m = line.trim().match(/^\/\/\s*long-file-allowed:\s*(.+)$/);
    if (m) return { hasEscapeHatch: true, reason: m[1].trim() };
  }
  return { hasEscapeHatch: false, reason: "" };
}

// ---------------------------------------------------------------------------
// Main lint logic
// ---------------------------------------------------------------------------

async function lintFile(filePath) {
  const src = readFileSync(filePath, "utf8");
  const lines = src.split("\n");
  const rel = relative(REPO_ROOT, filePath);
  const results = { warnings: [], errors: [] };

  // File LOC
  const fileLoc = lines.length;
  const fileHatch = getFileEscapeHatch(src);
  if (fileLoc > CAPS.file.err) {
    if (fileHatch.hasEscapeHatch) {
      results.warnings.push(`${rel}: file LOC=${fileLoc} exceeds err cap ${CAPS.file.err} (escape-hatch: ${fileHatch.reason})`);
    } else {
      results.errors.push(`${rel}: file LOC=${fileLoc} exceeds err cap ${CAPS.file.err}`);
    }
  } else if (fileLoc > CAPS.file.warn) {
    results.warnings.push(`${rel}: file LOC=${fileLoc} exceeds warn cap ${CAPS.file.warn}`);
  }

  // Line length
  lines.forEach((line, idx) => {
    const len = line.length;
    if (len > CAPS.line.err) {
      results.errors.push(`${rel}:${idx + 1}: line length ${len} exceeds err cap ${CAPS.line.err}`);
    } else if (len > CAPS.line.warn) {
      results.warnings.push(`${rel}:${idx + 1}: line length ${len} exceeds warn cap ${CAPS.line.warn}`);
    }
  });

  // Method LOC
  const methods = extractMethods(src);
  for (const m of methods) {
    if (m.locCount > CAPS.method.err) {
      if (m.hasEscapeHatch) {
        results.warnings.push(`${rel}:${m.startLine}: method '${m.name}' LOC=${m.locCount} exceeds err cap ${CAPS.method.err} (escape-hatch: ${m.escapeReason})`);
      } else {
        results.errors.push(`${rel}:${m.startLine}: method '${m.name}' LOC=${m.locCount} exceeds err cap ${CAPS.method.err}`);
      }
    } else if (m.locCount > CAPS.method.warn) {
      results.warnings.push(`${rel}:${m.startLine}: method '${m.name}' LOC=${m.locCount} exceeds warn cap ${CAPS.method.warn}`);
    }
  }

  return results;
}

// ---------------------------------------------------------------------------
// Entry point
// ---------------------------------------------------------------------------

async function main() {
  const files = await listFiles();
  let totalErrors = 0;
  let totalWarnings = 0;

  for (const f of files) {
    const { warnings, errors } = await lintFile(f);
    for (const w of warnings) { console.warn(`[WARN] ${w}`); totalWarnings++; }
    for (const e of errors) { console.error(`[ERR ] ${e}`); totalErrors++; }
  }

  console.log(`\nrun-csharp-lint: ${files.length} files scanned. Errors=${totalErrors} Warnings=${totalWarnings}`);

  if (totalErrors > 0 && !WARN_ONLY) {
    process.exit(1);
  }
}

main().catch((err) => { console.error(err); process.exit(1); });
