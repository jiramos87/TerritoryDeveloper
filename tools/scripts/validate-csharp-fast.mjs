#!/usr/bin/env node
/**
 * validate-csharp-fast.mjs — pattern-based pre-Unity lint for recurring C# errors.
 *
 * Editor-friendly: no Unity / dotnet / csproj needed. Catches a narrow class of errors
 * that have slipped past `validate:fast` and only surfaced at Unity recompile time.
 *
 * Detectors:
 *   1. CS0111 — duplicate method signature within a `partial class X` (across files).
 *
 * Scope: Assets/Scripts/**\/*.cs + Assets/Tests/**\/*.cs.
 * Mode: --hard-fail (exit 1 on offenders) default; --lint warns only.
 *
 * NOT a full compiler. CS0136 (var shadow) was attempted but stripped — false-positive
 * rate too high without a real C# AST parser. Add detectors here only when their
 * pattern is unambiguous text-search.
 */

import { readFileSync } from "node:fs";
import { resolve, dirname, relative } from "node:path";
import { fileURLToPath } from "node:url";
import { execSync } from "node:child_process";
import { glob } from "node:fs/promises";

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const REPO_ROOT = resolve(__dirname, "../..");

const HARD_FAIL = !process.argv.includes("--lint");
const DIFF_ONLY = process.argv.includes("--diff");

const SCAN_GLOBS = ["Assets/Scripts/**/*.cs", "Assets/Tests/**/*.cs"];
const SKIP_DIRS = ["Plugins", "ThirdParty", "Library", "Temp", "obj", "bin"];

// ---------------------------------------------------------------------------
// File enumeration
// ---------------------------------------------------------------------------

async function listFiles() {
  if (DIFF_ONLY) {
    const out = execSync("git diff HEAD --name-only --diff-filter=ACMR", {
      encoding: "utf8",
      cwd: REPO_ROOT,
    });
    return out
      .split("\n")
      .filter((p) => p.endsWith(".cs"))
      .filter((p) => !SKIP_DIRS.some((d) => p.includes(`/${d}/`)));
  }
  const found = [];
  for (const pattern of SCAN_GLOBS) {
    for await (const entry of glob(pattern, { cwd: REPO_ROOT })) {
      if (SKIP_DIRS.some((d) => entry.includes(`/${d}/`))) continue;
      found.push(entry);
    }
  }
  return found;
}

// ---------------------------------------------------------------------------
// Lightweight C# tokenizer — strips strings + comments so brace counting is safe.
// ---------------------------------------------------------------------------

function stripStringsAndComments(src) {
  let out = "";
  let i = 0;
  while (i < src.length) {
    const c = src[i];
    const next = src[i + 1];
    // Block comment.
    if (c === "/" && next === "*") {
      const end = src.indexOf("*/", i + 2);
      i = end < 0 ? src.length : end + 2;
      continue;
    }
    // Line comment.
    if (c === "/" && next === "/") {
      const end = src.indexOf("\n", i + 2);
      out += src.slice(i, end < 0 ? src.length : end);
      i = end < 0 ? src.length : end;
      // keep newline
      continue;
    }
    // Verbatim string @"...".
    if (c === "@" && next === '"') {
      let j = i + 2;
      while (j < src.length) {
        if (src[j] === '"' && src[j + 1] === '"') { j += 2; continue; }
        if (src[j] === '"') { j++; break; }
        j++;
      }
      out += " ".repeat(j - i);
      i = j;
      continue;
    }
    // Interpolated verbatim $@"..." or @$"...".
    if ((c === "$" && next === "@" && src[i + 2] === '"') ||
        (c === "@" && next === "$" && src[i + 2] === '"')) {
      let j = i + 3;
      let depth = 0;
      while (j < src.length) {
        if (src[j] === "{" && src[j + 1] !== "{") depth++;
        else if (src[j] === "}" && src[j + 1] !== "}") depth--;
        else if (src[j] === '"' && depth === 0) { j++; break; }
        j++;
      }
      out += " ".repeat(j - i);
      i = j;
      continue;
    }
    // Regular string "...".
    if (c === '"') {
      let j = i + 1;
      while (j < src.length) {
        if (src[j] === "\\") { j += 2; continue; }
        if (src[j] === '"') { j++; break; }
        if (src[j] === "\n") break;
        j++;
      }
      out += " ".repeat(j - i);
      i = j;
      continue;
    }
    // Interpolated string $"...".
    if (c === "$" && next === '"') {
      let j = i + 2;
      let braceDepth = 0;
      while (j < src.length) {
        if (src[j] === "\\") { j += 2; continue; }
        if (src[j] === "{" && src[j + 1] !== "{") braceDepth++;
        else if (src[j] === "}" && src[j + 1] !== "}") braceDepth--;
        else if (src[j] === '"' && braceDepth === 0) { j++; break; }
        j++;
      }
      out += " ".repeat(j - i);
      i = j;
      continue;
    }
    // Char '.'.
    if (c === "'") {
      let j = i + 1;
      while (j < src.length) {
        if (src[j] === "\\") { j += 2; continue; }
        if (src[j] === "'") { j++; break; }
        j++;
      }
      out += " ".repeat(j - i);
      i = j;
      continue;
    }
    out += c;
    i++;
  }
  return out;
}

// ---------------------------------------------------------------------------
// CS0111 detector — duplicate methods in same partial class across files.
// ---------------------------------------------------------------------------

const PARTIAL_CLASS_RE =
  /\b(?:public|internal|private|protected|sealed|static|abstract|partial|\s)+\bclass\s+(\w+)\s*(?::[^{]+)?\{/g;

const METHOD_DECL_RE =
  /\b(?:public|internal|private|protected|static|virtual|override|sealed|abstract|async|extern|unsafe|partial|new|\s)+(?:[\w<>?,\s\.\[\]]+?)\s+(\w+)\s*\(([^)]*)\)\s*(?:where\s+[^{;]+)?\s*[{;]/g;

function normalizeParams(paramSrc) {
  // Strip default values, attributes [in/out/ref], whitespace.
  return paramSrc
    .split(",")
    .map((p) => p.trim())
    .filter((p) => p.length > 0)
    .map((p) => {
      const eq = p.indexOf("=");
      if (eq >= 0) p = p.slice(0, eq).trim();
      const tokens = p.split(/\s+/);
      // Last token = name; rest = type. Use type only.
      if (tokens.length < 2) return p;
      return tokens.slice(0, -1).join(" ");
    })
    .join(",");
}

// Tight method-signature regex anchored at class-body depth=1 only.
// Requires: optional XML doc skipped, modifiers, return type, identifier, ( params ), then { or ;.
const STRICT_METHOD_RE =
  /^[\t ]*(?:\[[^\]]+\][\t ]*\n[\t ]*)*(?:(?:public|private|protected|internal|static|virtual|override|sealed|abstract|async|extern|unsafe|partial|new|readonly)[\t ]+)+([\w<>?,\s\.\[\]]+?)\s+(\w+)\s*\(([^)]*)\)\s*(?:where\s+[^{;]+)?\s*[{;]/gm;

const CSHARP_KEYWORDS = new Set([
  "if", "else", "for", "foreach", "while", "do", "switch", "case", "break",
  "continue", "return", "throw", "try", "catch", "finally", "using", "lock",
  "new", "this", "base", "var", "void", "ref", "out", "in", "is", "as",
  "true", "false", "null", "default", "typeof", "sizeof", "stackalloc",
  "checked", "unchecked", "fixed", "yield", "await", "from", "where",
  "select", "group", "into", "orderby", "join", "let", "by", "on", "equals",
]);

function findClassesAndMethods(filePath, srcStripped) {
  const results = [];
  const classMatches = [...srcStripped.matchAll(PARTIAL_CLASS_RE)];
  for (const cm of classMatches) {
    const className = cm[1];
    const bodyStart = cm.index + cm[0].length;
    // Find matching closing brace.
    let depth = 1;
    let i = bodyStart;
    while (i < srcStripped.length && depth > 0) {
      if (srcStripped[i] === "{") depth++;
      else if (srcStripped[i] === "}") depth--;
      i++;
    }
    const bodyEnd = i;
    const decl = srcStripped.slice(cm.index, bodyStart);
    if (!/\bpartial\b/.test(decl)) continue;

    // Walk class body; only inspect tokens at depth=0 relative to class body.
    // Skip nested braces (method bodies, nested types, property bodies).
    const body = srcStripped.slice(bodyStart, bodyEnd);
    const memberRanges = [];
    let memberStart = 0;
    let bd = 0;
    for (let p = 0; p < body.length; p++) {
      const ch = body[p];
      if (ch === "{") {
        if (bd === 0) {
          // Skip from here to matching close.
          let dd = 1;
          let q = p + 1;
          while (q < body.length && dd > 0) {
            if (body[q] === "{") dd++;
            else if (body[q] === "}") dd--;
            q++;
          }
          memberRanges.push([memberStart, q]);
          memberStart = q;
          p = q - 1;
        }
        // else nested already handled by skip
      } else if (ch === ";" && bd === 0) {
        memberRanges.push([memberStart, p + 1]);
        memberStart = p + 1;
      }
    }

    for (const [s, e] of memberRanges) {
      const slice = body.slice(s, e);
      // Reset regex state.
      STRICT_METHOD_RE.lastIndex = 0;
      const m = STRICT_METHOD_RE.exec(slice);
      if (!m) continue;
      const methodName = m[2];
      if (methodName === className) continue; // ctor
      if (CSHARP_KEYWORDS.has(methodName)) continue;
      if (["get", "set", "init", "add", "remove", "value"].includes(methodName)) continue;
      const sig = `${methodName}(${normalizeParams(m[3])})`;
      const absOffset = bodyStart + s + m.index;
      const upToMatch = srcStripped.slice(0, absOffset);
      const line = upToMatch.split("\n").length;
      results.push({ className, signature: sig, file: filePath, line });
    }
  }
  return results;
}

function detectCS0111(filesData) {
  const map = new Map();
  for (const entry of filesData) {
    const key = `${entry.className}::${entry.signature}`;
    if (!map.has(key)) map.set(key, []);
    map.get(key).push(entry);
  }
  const offenders = [];
  for (const [key, entries] of map) {
    if (entries.length > 1) {
      offenders.push({ key, entries });
    }
  }
  return offenders;
}

// ---------------------------------------------------------------------------
// Main
// ---------------------------------------------------------------------------

async function main() {
  const files = await listFiles();
  if (files.length === 0) {
    if (DIFF_ONLY) {
      console.log("validate-csharp-fast: no .cs files in diff — skip.");
      process.exit(0);
    }
    console.error("validate-csharp-fast: no .cs files matched scan globs.");
    process.exit(1);
  }

  // Read + strip all files once.
  const filesData = [];
  for (const file of files) {
    const abs = resolve(REPO_ROOT, file);
    let raw;
    try { raw = readFileSync(abs, "utf8"); } catch { continue; }
    const stripped = stripStringsAndComments(raw);
    filesData.push({ file, raw, srcStripped: stripped });
  }

  // CS0111 — partial class duplicates. Always scope to ALL files (cross-file).
  // For diff-mode, still need full corpus for partial classes touched in diff.
  let allFilesData = filesData;
  if (DIFF_ONLY) {
    // Re-glob full set for partial-class context.
    const fullList = [];
    for (const pattern of SCAN_GLOBS) {
      for await (const entry of glob(pattern, { cwd: REPO_ROOT })) {
        if (SKIP_DIRS.some((d) => entry.includes(`/${d}/`))) continue;
        fullList.push(entry);
      }
    }
    allFilesData = [];
    for (const file of fullList) {
      const abs = resolve(REPO_ROOT, file);
      let raw;
      try { raw = readFileSync(abs, "utf8"); } catch { continue; }
      const stripped = stripStringsAndComments(raw);
      allFilesData.push({ file, raw, srcStripped: stripped });
    }
  }

  const classMembers = [];
  for (const { file, srcStripped } of allFilesData) {
    classMembers.push(...findClassesAndMethods(file, srcStripped));
  }
  const cs0111 = detectCS0111(classMembers);

  let issues = 0;

  if (cs0111.length > 0) {
    console.error("\n=== CS0111 candidates — duplicate methods in partial class ===");
    for (const off of cs0111) {
      console.error(`  ${off.key}`);
      for (const e of off.entries) {
        console.error(`    ${e.file}:${e.line}`);
      }
    }
    issues += cs0111.length;
  }

  if (issues === 0) {
    console.log(`validate-csharp-fast: clean (${filesData.length} files scanned).`);
    process.exit(0);
  }

  console.error(`\nvalidate-csharp-fast: ${issues} candidate(s) found.`);
  if (HARD_FAIL) {
    console.error("Hard-fail mode (default). Run with --lint to suppress exit code.");
    process.exit(1);
  }
  process.exit(0);
}

main().catch((e) => {
  console.error("validate-csharp-fast: unexpected error:", e);
  process.exit(2);
});
