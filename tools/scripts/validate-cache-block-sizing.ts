#!/usr/bin/env tsx
/**
 * validate-cache-block-sizing.ts
 *
 * CI sizing-gate validator for Tier 1 stable cache blocks.
 * Parses agent bodies (or fixture files) for cache_control declarations;
 * resolves @-concat references transitively; estimates token count (bytes × 0.25);
 * fails if count < F2 floor (4,096 tok Opus 4.7 / 1,024 tok Sonnet 4.6).
 *
 * Per docs/prompt-caching-mechanics.md §5 (C1/R2):
 *   - Silent no-cache is already billed; catch at CI.
 *   - Non-emitting agents (no cache_control declared) are skipped — opt-in surface.
 *
 * Usage:
 *   npx tsx tools/scripts/validate-cache-block-sizing.ts
 *   npx tsx tools/scripts/validate-cache-block-sizing.ts --agents-dir <dir>
 *   npx tsx tools/scripts/validate-cache-block-sizing.ts --fixture <path-to-dir-or-file>
 *
 * Exit: 0 all-pass; 1 any below-floor.
 */

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

// ---------------------------------------------------------------------------
// Constants
// ---------------------------------------------------------------------------

const BYTES_PER_TOKEN = 4; // 1 token ≈ 4 bytes (bytes × 0.25 = tokens)
const F2_FLOOR_OPUS = 4096; // Opus 4.7 minimum cacheable block
const F2_FLOOR_SONNET = 1024; // Sonnet 4.6 minimum cacheable block

const F2_FLOOR: Record<string, number> = {
  opus: F2_FLOOR_OPUS,
  sonnet: F2_FLOOR_SONNET,
};

const DEFAULT_FLOOR = F2_FLOOR_OPUS; // Conservative default when model undetected

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const REPO_ROOT = path.resolve(__dirname, "..", "..");

function repoPath(rel: string): string {
  return path.resolve(REPO_ROOT, rel);
}

/** Parse a single field from YAML-ish frontmatter (no full YAML parser needed). */
function parseFrontmatterField(content: string, field: string): string | null {
  const match = content.match(/^---\n([\s\S]*?)\n---/);
  if (!match) return null;
  const fm = match[1];
  const lineMatch = fm.match(new RegExp(`^${field}:\\s*(.+)$`, "m"));
  return lineMatch ? lineMatch[1].trim().replace(/^["']|["']$/g, "") : null;
}

/**
 * Detect cache_control declaration in the markdown body (outside frontmatter).
 * Looks for the canonical form used in agent bodies:
 *   > `cache_control: {"type":"ephemeral","ttl":"1h"}`
 */
function hasCacheControlDeclaration(body: string): boolean {
  return /^>\s*`?cache_control\s*:/m.test(body);
}

/** Resolve @-concat references recursively, returning total referenced bytes. */
function resolveAtConcatBytes(
  body: string,
  baseDir: string,
  visited = new Set<string>()
): number {
  let total = 0;
  const atRefs = body.match(/^@(.+)$/gm) ?? [];
  for (const ref of atRefs) {
    const refPath = ref.slice(1).trim();
    // Try repo-root-relative first, then base-dir-relative
    const candidates = [repoPath(refPath), path.resolve(baseDir, refPath)];
    let resolved: string | null = null;
    for (const candidate of candidates) {
      if (fs.existsSync(candidate)) {
        resolved = candidate;
        break;
      }
    }
    if (!resolved) {
      process.stderr.write(`  [WARN] @-ref not found: ${refPath}\n`);
      continue;
    }
    if (visited.has(resolved)) continue; // Cycle guard
    visited.add(resolved);
    const content = fs.readFileSync(resolved, "utf8");
    total += Buffer.byteLength(content, "utf8");
    total += resolveAtConcatBytes(content, path.dirname(resolved), visited);
  }
  return total;
}

/** Strip frontmatter block from markdown content. */
function stripFrontmatter(content: string): string {
  return content.replace(/^---\n[\s\S]*?\n---\n?/, "");
}

/** Collect *.md files from a directory, skipping _retired/ subdirs. */
function collectMdFiles(dir: string): string[] {
  if (!fs.existsSync(dir)) return [];
  const stat = fs.statSync(dir);
  if (!stat.isDirectory()) return [dir];
  return fs
    .readdirSync(dir)
    .filter((f) => f.endsWith(".md") && !f.startsWith("_"))
    .map((f) => path.join(dir, f));
}

// ---------------------------------------------------------------------------
// Core validation
// ---------------------------------------------------------------------------

interface ValidationResult {
  file: string;
  model: string;
  floor: number;
  estimatedTokens: number;
  pass: boolean;
  skipped: boolean;
  skipReason?: string;
}

function validateAgentFile(filePath: string): ValidationResult {
  const content = fs.readFileSync(filePath, "utf8");
  const body = stripFrontmatter(content);
  const modelRaw = parseFrontmatterField(content, "model") ?? "opus";
  const modelKey = modelRaw.toLowerCase().includes("sonnet") ? "sonnet" : "opus";
  const floor = F2_FLOOR[modelKey] ?? DEFAULT_FLOOR;

  if (!hasCacheControlDeclaration(body)) {
    return {
      file: filePath,
      model: modelRaw,
      floor,
      estimatedTokens: 0,
      pass: true,
      skipped: true,
      skipReason: "no cache_control declaration (opt-in surface)",
    };
  }

  // Estimate bytes: body text + all @-concat referenced files
  const bodyBytes = Buffer.byteLength(body, "utf8");
  const atBytes = resolveAtConcatBytes(body, path.dirname(filePath));
  const totalBytes = bodyBytes + atBytes;
  const estimatedTokens = Math.round(totalBytes / BYTES_PER_TOKEN);

  return {
    file: filePath,
    model: modelRaw,
    floor,
    estimatedTokens,
    pass: estimatedTokens >= floor,
    skipped: false,
  };
}

// ---------------------------------------------------------------------------
// Entry point
// ---------------------------------------------------------------------------

function main(): void {
  const args = process.argv.slice(2);
  let files: string[] = [];

  const fixtureIdx = args.indexOf("--fixture");
  const agentsDirIdx = args.indexOf("--agents-dir");

  if (fixtureIdx !== -1) {
    const fixturePath = args[fixtureIdx + 1];
    if (!fixturePath) {
      process.stderr.write("Error: --fixture requires a path argument\n");
      process.exit(1);
    }
    const absFixture = path.resolve(fixturePath);
    const stat = fs.statSync(absFixture);
    if (stat.isDirectory()) {
      // Collect all .md files in fixture dir, exclude helper files starting with "tiny"
      files = fs
        .readdirSync(absFixture)
        .filter((f) => f.endsWith(".md") && !f.startsWith("tiny"))
        .map((f) => path.join(absFixture, f));
    } else {
      files = [absFixture];
    }
  } else if (agentsDirIdx !== -1) {
    const agentsDir = args[agentsDirIdx + 1];
    if (!agentsDir) {
      process.stderr.write("Error: --agents-dir requires a path argument\n");
      process.exit(1);
    }
    files = collectMdFiles(path.resolve(agentsDir));
  } else {
    // Default: .claude/agents/*.md (exclude _retired/ subdir)
    const agentsDir = path.join(REPO_ROOT, ".claude", "agents");
    files = collectMdFiles(agentsDir);
  }

  if (files.length === 0) {
    process.stderr.write("validate-cache-block-sizing: no agent files found\n");
    process.exit(0);
  }

  const results: ValidationResult[] = files.map(validateAgentFile);

  const checked = results.filter((r) => !r.skipped);
  const skipped = results.filter((r) => r.skipped);
  const failures = results.filter((r) => !r.skipped && !r.pass);

  // Report failures
  if (failures.length > 0) {
    process.stderr.write(
      `\nvalidate-cache-block-sizing: ${failures.length} below-floor block(s) found:\n`
    );
    for (const f of failures) {
      const delta = f.floor - f.estimatedTokens;
      process.stderr.write(
        `  FAIL  ${path.relative(REPO_ROOT, f.file)}\n` +
          `        model=${f.model}  estimated=${f.estimatedTokens} tok  ` +
          `floor=${f.floor} tok  delta=-${delta} tok\n`
      );
    }
    process.stderr.write(
      `\nSilent no-cache: blocks below F2 floor billed as fresh input every call.\n` +
        `Fix: inflate @-concat content to clear F2 floor.\n` +
        `Ref: docs/prompt-caching-mechanics.md §5 (R2).\n\n`
    );
  }

  // Summary line (stdout — captured by CI)
  process.stdout.write(
    `validate-cache-block-sizing: ${checked.length} checked, ` +
      `${skipped.length} skipped (no cache_control), ` +
      `${failures.length} failed\n`
  );

  if (failures.length > 0) {
    process.exit(1);
  }

  // Verbose pass details (suppressed in CI quiet mode)
  if (process.env.CI !== "true" && checked.length > 0) {
    for (const r of checked) {
      const rel = path.relative(REPO_ROOT, r.file);
      const clearance = (r.estimatedTokens / r.floor).toFixed(2);
      process.stdout.write(
        `  PASS  ${rel}  (${r.estimatedTokens} tok, ×${clearance} floor clearance)\n`
      );
    }
  }
}

main();
