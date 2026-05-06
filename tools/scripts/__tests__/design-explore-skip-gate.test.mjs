/**
 * design-explore-skip-gate.test.mjs
 *
 * unit-test:tools/scripts/__tests__/design-explore-skip-gate.test.mjs::clean_skips_dirty_fires
 *
 * TECH-15912 — validate-design-explore-yaml skip-gate:
 *   clean YAML + zero warnings → exit 0 (skip gate passes)
 *   malformed YAML → exit 1 (gate fails → review fires)
 */

import { execFile } from "child_process";
import { writeFileSync, unlinkSync } from "fs";
import { promisify } from "util";
import { fileURLToPath } from "url";
import { join, resolve } from "path";
import { tmpdir } from "os";
import assert from "node:assert/strict";

// Promisify execFile — we need to handle both success (exit 0) and failure (exit 1)
const __dirname = fileURLToPath(new URL(".", import.meta.url));
const REPO_ROOT = resolve(__dirname, "../../..");
const SCRIPT = join(REPO_ROOT, "tools/scripts/validate-design-explore-yaml.mjs");

function runValidator(filePath) {
  return new Promise((resolve) => {
    execFile(
      process.execPath,
      [SCRIPT, filePath],
      { cwd: REPO_ROOT, timeout: 10000 },
      (err, stdout, stderr) => {
        resolve({ code: err ? (err.code ?? 1) : 0, stdout, stderr });
      },
    );
  });
}

// ---------------------------------------------------------------------------
// Fixtures
// ---------------------------------------------------------------------------

const CLEAN_YAML = `---
slug: test-exploration
parent_plan_slug: null
target_version: 1
stages:
  - stage_id: "1"
    title: "Tracer slice"
    status: pending
tasks:
  - prefix: TECH
    depends_on: []
    digest_outline: "Implement the feature"
    touched_paths: []
    kind: implementation
---

# Test exploration doc

Some content here.
`;

const DIRTY_YAML_MISSING_SLUG = `---
parent_plan_slug: null
target_version: 1
stages:
  - stage_id: "1"
    title: "Tracer slice"
    status: pending
tasks:
  - prefix: TECH
    depends_on: []
    digest_outline: "Implement"
    touched_paths: []
    kind: implementation
---

# Missing slug
`;

const DIRTY_YAML_BAD_VERSION = `---
slug: test-exploration
parent_plan_slug: null
target_version: 0
stages:
  - stage_id: "1"
    title: "Tracer"
    status: pending
tasks:
  - prefix: TECH
    depends_on: []
    digest_outline: "Do it"
    touched_paths: []
    kind: implementation
---
`;

const DIRTY_NO_FRONTMATTER = `# Just a doc with no YAML frontmatter

Some content.
`;

// ---------------------------------------------------------------------------
// clean_skips_dirty_fires
// ---------------------------------------------------------------------------

const tmpDir = tmpdir();

// Test 1 — clean YAML exits 0
const cleanFile = join(tmpDir, "clean-exploration.md");
writeFileSync(cleanFile, CLEAN_YAML, "utf8");
const cleanResult = await runValidator(cleanFile);
assert.equal(cleanResult.code, 0, `clean YAML should exit 0, got ${cleanResult.code}. stderr: ${cleanResult.stderr}`);
console.log("PASS clean_skips_dirty_fires[clean_exit_0]");

// Test 2 — missing slug exits 1
const dirtySluFile = join(tmpDir, "dirty-missing-slug.md");
writeFileSync(dirtySluFile, DIRTY_YAML_MISSING_SLUG, "utf8");
const dirtySlugResult = await runValidator(dirtySluFile);
assert(dirtySlugResult.code !== 0, `missing slug should exit non-zero, got ${dirtySlugResult.code}`);
assert(dirtySlugResult.stderr.includes("slug"), `stderr should mention 'slug': ${dirtySlugResult.stderr}`);
console.log("PASS clean_skips_dirty_fires[dirty_missing_slug_exit_1]");

// Test 3 — bad version exits 1
const dirtyVersionFile = join(tmpDir, "dirty-bad-version.md");
writeFileSync(dirtyVersionFile, DIRTY_YAML_BAD_VERSION, "utf8");
const dirtyVersionResult = await runValidator(dirtyVersionFile);
assert(dirtyVersionResult.code !== 0, `bad version should exit non-zero, got ${dirtyVersionResult.code}`);
assert(
  dirtyVersionResult.stderr.includes("target_version"),
  `stderr should mention 'target_version': ${dirtyVersionResult.stderr}`,
);
console.log("PASS clean_skips_dirty_fires[dirty_bad_version_exit_1]");

// Test 4 — no frontmatter exits 1
const noFrontFile = join(tmpDir, "no-frontmatter.md");
writeFileSync(noFrontFile, DIRTY_NO_FRONTMATTER, "utf8");
const noFrontResult = await runValidator(noFrontFile);
assert(noFrontResult.code !== 0, `no frontmatter should exit non-zero, got ${noFrontResult.code}`);
console.log("PASS clean_skips_dirty_fires[no_frontmatter_exit_1]");

// Cleanup
for (const f of [cleanFile, dirtySluFile, dirtyVersionFile, noFrontFile]) {
  try { unlinkSync(f); } catch {}
}

console.log("All design-explore-skip-gate tests passed.");
