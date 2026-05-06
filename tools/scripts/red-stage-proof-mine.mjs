#!/usr/bin/env node
/**
 * red-stage-proof-mine.mjs
 *
 * Scan Assets/Scripts/Tests/** for C# test files matching BDD-name patterns.
 * Emit candidate red-stage proof Markdown for a given issue id.
 *
 * Usage:
 *   node tools/scripts/red-stage-proof-mine.mjs FEAT-123
 *   node tools/scripts/red-stage-proof-mine.mjs TECH-15909
 *
 * Output (stdout): Markdown candidate block, or empty string if no match.
 *
 * BDD-name pattern matched:
 *   - Class: {Word}{Suffix}Tests.cs  where Suffix ∈ {Parity,Smoke,PlayMode,''}
 *   - Method: Should_X_When_Y  OR  Test_X_Given_Y  OR  Validates_X  OR  Assert_X
 *
 * TECH-15910
 */

import { readFileSync, readdirSync, statSync } from "fs";
import { join, relative } from "path";
import { fileURLToPath } from "url";

const __dirname = fileURLToPath(new URL(".", import.meta.url));
const REPO_ROOT = join(__dirname, "../../");
const TESTS_DIR = join(REPO_ROOT, "Assets/Scripts/Tests");

/** Recursively list all .cs files under a directory */
function walkCs(dir) {
  const results = [];
  let entries;
  try {
    entries = readdirSync(dir, { withFileTypes: true });
  } catch {
    return results;
  }
  for (const e of entries) {
    const fullPath = join(dir, e.name);
    if (e.isDirectory()) {
      results.push(...walkCs(fullPath));
    } else if (e.isFile() && e.name.endsWith(".cs")) {
      results.push(fullPath);
    }
  }
  return results;
}

/**
 * Normalise an issue id to a fragment that might appear inside a test class.
 * FEAT-123 → feat123 / Feat123 / FeatOneHundredTwentyThree (heuristic: number to digits slug).
 * We look for file names containing a loose match OR class names mentioning the domain.
 */
function issueSlug(issueId) {
  // "FEAT-123" → "feat-123" slug forms used by file names
  return issueId.toLowerCase().replace(/[^a-z0-9]+/g, "");
}

/**
 * Extract test methods from a C# file.
 * Returns { className, methods: [{ name, body }] }
 */
function extractTestClass(filePath) {
  const src = readFileSync(filePath, "utf8");

  // Extract class name
  const classMatch = src.match(/class\s+(\w+)/);
  const className = classMatch ? classMatch[1] : null;
  if (!className) return null;

  // Extract [Test] / [UnityTest] methods
  const methodPattern =
    /\[(?:UnityTest|Test|TestCase)\][^\[]*?(?:public|private|protected)\s+(?:IEnumerator|void|Task)\s+(\w+)\s*\([^)]*\)\s*\{([\s\S]*?)(?=\n\s{4,8}\[(?:Test|UnityTest|Setup|TearDown|OneTimeSetUp)|^\s{4,8}(?:public|private|protected)\s+(?:IEnumerator|void|Task)\s|\n\s{0,4}\})/gm;

  const methods = [];
  let m;
  while ((m = methodPattern.exec(src)) !== null) {
    const name = m[1];
    const rawBody = m[2] || "";
    // Extract Assert lines as proof fragments
    const assertLines = rawBody
      .split("\n")
      .filter((l) => /Assert\.|UnityEngine\.Assertions\.Assert/.test(l))
      .map((l) => l.trim())
      .slice(0, 5);
    methods.push({ name, assertLines });
  }

  return { className, methods, filePath };
}

/**
 * Score a candidate class for relevance to the issueId.
 * Higher = more relevant.
 */
function score(candidate, issueId, slug) {
  const lowerClass = candidate.className.toLowerCase();
  const lowerFile = candidate.filePath.toLowerCase();
  let s = 0;
  if (lowerClass.includes(slug)) s += 10;
  if (lowerFile.includes(slug)) s += 5;
  // BDD-name methods add score
  for (const m of candidate.methods) {
    if (/Should_|Test_|Validates_|Assert_|Given_|When_/.test(m.name)) s += 2;
  }
  return s;
}

/**
 * Emit a Markdown red-stage proof candidate block.
 */
function emitCandidate(issueId, candidate) {
  const relPath = relative(REPO_ROOT, candidate.filePath);
  const lines = [
    `### Red-Stage Proof — ${issueId} (mined candidate)`,
    "",
    `**Source:** \`${relPath}\`  `,
    `**Class:** \`${candidate.className}\`  `,
    "",
    "```",
  ];

  if (candidate.methods.length === 0) {
    lines.push(`# No [Test]/[UnityTest] methods found in ${candidate.className}`);
  } else {
    for (const m of candidate.methods.slice(0, 8)) {
      lines.push(`# ${m.name}`);
      for (const a of m.assertLines) {
        lines.push(`assert ${a}`);
      }
      if (m.assertLines.length === 0) {
        lines.push(`# (no Assert lines found — inspect manually)`);
      }
      lines.push("");
    }
  }

  lines.push("```");
  lines.push("");
  lines.push(
    `> Mined by \`red-stage-proof-mine.mjs\`. ` +
      `Review + trim before committing to task spec.`,
  );

  return lines.join("\n");
}

// ---------------------------------------------------------------------------
// Main
// ---------------------------------------------------------------------------

const issueId = process.argv[2];
if (!issueId) {
  console.error("Usage: red-stage-proof-mine.mjs <ISSUE_ID>  (e.g. FEAT-123)");
  process.exit(1);
}

const slug = issueSlug(issueId);
const allFiles = walkCs(TESTS_DIR);

const candidates = allFiles
  .map(extractTestClass)
  .filter(Boolean)
  .map((c) => ({ ...c, _score: score(c, issueId, slug) }))
  .filter((c) => c._score > 0 || c.methods.length > 0)
  .sort((a, b) => b._score - a._score);

if (candidates.length === 0) {
  // No matches — emit empty (callers check stdout length)
  process.stdout.write("");
  process.exit(0);
}

// Best candidate
const best = candidates[0];
process.stdout.write(emitCandidate(issueId, best) + "\n");
process.exit(0);
