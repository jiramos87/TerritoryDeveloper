/**
 * schema-v2-roundtrip — fixture coverage for TECH-366 (Stage 3.1 Phase 2).
 *
 * Proves byte-identical round-trip of buildYaml (writer, TECH-365) across three
 * fixture shapes:
 *   schema-v2-full.yaml    — all 9 locator fields populated
 *   schema-v2-minimal.yaml — only 2 required locator fields (parent_plan + task_key)
 *   schema-v1-legacy.yaml  — zero locator fields (back-compat gate)
 *
 * Round-trip path: fixture bytes → loadYamlIssue (loader) → adapter → buildYaml
 * (writer, imported from tools/scripts/migrate-backlog-to-yaml.mjs) → compare
 * bytes. The writer stamps `created:` to today's date; test normalizes that
 * single line before byte compare (per TECH-365 spec Decision Log).
 */

import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
// @ts-expect-error — .mjs import, no type defs
import { buildYaml } from "../../../scripts/backlog-yaml-writer.mjs";
import { loadYamlIssue } from "../../src/parser/backlog-yaml-loader.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "..", "..", "..", "..");
const FIXTURE_DIR = path.join(REPO_ROOT, "tools", "scripts", "test-fixtures");

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/**
 * Normalize the single `created: YYYY-MM-DD` line in both sides of the
 * compare. buildYaml stamps today's date; fixtures carry a fixed date.
 */
function normalizeCreatedLine(yaml: string): string {
  return yaml.replace(/^created: \d{4}-\d{2}-\d{2}$/m, "created: <DATE>");
}

/**
 * Adapt a ParsedBacklogIssue (loader shape) to the buildYaml input shape
 * (markdown-parser shape). Flattens files + depends_on to prose strings.
 */
function issueToBuildInput(issue: ReturnType<typeof loadYamlIssue>): Record<string, unknown> {
  if (!issue) throw new Error("loadYamlIssue returned null");
  return {
    issue_id: issue.issue_id,
    type: issue.type,
    title: issue.title,
    backlog_section: issue.backlog_section,
    status: issue.status,
    spec: issue.spec ?? "",
    files: issue.files ?? "",
    notes: issue.notes ?? "",
    acceptance: issue.acceptance ?? "",
    depends_on: issue.depends_on ?? "",
    raw_markdown: issue.raw_markdown ?? "",
    // Locator fields (schema v2)
    parent_plan: issue.parent_plan,
    task_key: issue.task_key,
    step: issue.step,
    stage: issue.stage,
    phase: issue.phase,
    router_domain: issue.router_domain,
    surfaces: issue.surfaces ?? [],
    mcp_slices: issue.mcp_slices ?? [],
    skill_hints: issue.skill_hints ?? [],
  };
}

/**
 * Stage a fixture into a temp repo-root subtree so loadYamlIssue resolves it
 * via its standard ia/backlog/{id}.yaml lookup path. Returns the tempRoot so
 * the caller can clean up.
 */
function stageFixture(fixtureName: string, issueId: string): { tempRoot: string; content: string } {
  const fixturePath = path.join(FIXTURE_DIR, fixtureName);
  const content = fs.readFileSync(fixturePath, "utf8");
  const tempRoot = fs.mkdtempSync(path.join(REPO_ROOT, ".tmp-roundtrip-"));
  fs.mkdirSync(path.join(tempRoot, "ia", "backlog"), { recursive: true });
  fs.writeFileSync(path.join(tempRoot, "ia", "backlog", `${issueId}.yaml`), content, "utf8");
  return { tempRoot, content };
}

function runRoundTrip(fixtureName: string, issueId: string): void {
  const { tempRoot, content } = stageFixture(fixtureName, issueId);
  try {
    const issue = loadYamlIssue(tempRoot, issueId);
    assert.ok(issue, `loadYamlIssue returned null for ${issueId}`);
    const input = issueToBuildInput(issue);
    const emitted = buildYaml(input) as string;

    const expectedNorm = normalizeCreatedLine(content);
    const actualNorm = normalizeCreatedLine(emitted);

    assert.strictEqual(
      actualNorm,
      expectedNorm,
      `round-trip byte diff in ${fixtureName}`,
    );
  } finally {
    fs.rmSync(tempRoot, { recursive: true, force: true });
  }
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

test("schema-v2-full fixture — all 9 locator fields round-trip byte-identical", () => {
  runRoundTrip("schema-v2-full.yaml", "TECH-999");
});

test("schema-v2-minimal fixture — 2 required locator fields round-trip byte-identical", () => {
  runRoundTrip("schema-v2-minimal.yaml", "TECH-998");
});

test("schema-v1-legacy fixture — zero locator fields round-trip byte-identical", () => {
  runRoundTrip("schema-v1-legacy.yaml", "TECH-997");
});

test("partial-v2 policy — parent_plan set, task_key null → drops to v1 (emits neither)", () => {
  // Build issue input directly (no fixture) — proves the partial-v2 = drop-to-v1
  // policy documented in TECH-365 spec §5.2.
  const partial = {
    issue_id: "TECH-996",
    type: "infrastructure",
    title: "Partial v2 — only parent_plan set",
    backlog_section: "Backlog YAML and MCP alignment program",
    status: "open",
    spec: "",
    files: "",
    notes: "",
    acceptance: "",
    depends_on: "",
    raw_markdown: "",
    parent_plan: "ia/projects/foo.md",
    task_key: null,
    step: null,
    stage: null,
    phase: null,
    router_domain: null,
    surfaces: [],
    mcp_slices: [],
    skill_hints: [],
  };
  const emitted = buildYaml(partial) as string;
  assert.ok(
    !emitted.includes("parent_plan:"),
    "partial-v2 must drop parent_plan emit (policy: both or neither)",
  );
  assert.ok(
    !emitted.includes("task_key:"),
    "partial-v2 must drop task_key emit",
  );
});
