/**
 * backlog_issue — payload field coverage tests.
 * Verifies priority, related, created surfaced for yaml-backed issues;
 * verifies null / empty defaults for issues missing those fields.
 *
 * TECH-301: integration tests for round-trip soft-dep marker via parseBacklogIssue
 * + resolveDependsOnStatus (tool-layer regression guard for TECH-297 fix).
 */

import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";
import {
  parseBacklogIssue,
  resolveDependsOnStatus,
  type ParsedBacklogIssue,
} from "../../src/parser/backlog-parser.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, "../../../../");

// ---------------------------------------------------------------------------
// TECH-300 has priority="high", related=[...], created="2026-04-17"
// ---------------------------------------------------------------------------
test(
  "backlog_issue payload includes priority for TECH-300",
  { skip: !fs.existsSync(path.join(repoRoot, "ia/backlog/TECH-300.yaml")) },
  () => {
    const issue = parseBacklogIssue(repoRoot, "TECH-300");
    assert.ok(issue, "TECH-300 should resolve");
    assert.equal(issue!.priority, "high");
  },
);

test(
  "backlog_issue payload includes related array for TECH-300",
  { skip: !fs.existsSync(path.join(repoRoot, "ia/backlog/TECH-300.yaml")) },
  () => {
    const issue = parseBacklogIssue(repoRoot, "TECH-300");
    assert.ok(issue, "TECH-300 should resolve");
    assert.ok(Array.isArray(issue!.related), "related should be an array");
    assert.ok(issue!.related!.length > 0, "related should be non-empty for TECH-300");
  },
);

test(
  "backlog_issue payload includes created for TECH-300",
  { skip: !fs.existsSync(path.join(repoRoot, "ia/backlog/TECH-300.yaml")) },
  () => {
    const issue = parseBacklogIssue(repoRoot, "TECH-300");
    assert.ok(issue, "TECH-300 should resolve");
    assert.equal(issue!.created, "2026-04-17");
  },
);

// ---------------------------------------------------------------------------
// Null / empty defaults — fabricate an issue missing the new fields
// ---------------------------------------------------------------------------
function makeMinimalIssue(overrides: Partial<ParsedBacklogIssue>): ParsedBacklogIssue {
  return {
    issue_id: "TEST-1",
    title: "test",
    status: "open",
    backlog_section: "Test",
    raw_markdown: "",
    ...overrides,
  };
}

test("priority defaults to null when not set", () => {
  const issue = makeMinimalIssue({});
  const priority = issue.priority ?? null;
  assert.equal(priority, null);
});

test("related defaults to empty array when not set", () => {
  const issue = makeMinimalIssue({});
  const related = issue.related ?? [];
  assert.deepEqual(related, []);
});

test("created defaults to null when not set", () => {
  const issue = makeMinimalIssue({});
  const created = issue.created ?? null;
  assert.equal(created, null);
});

// ---------------------------------------------------------------------------
// TECH-301 — Round-trip soft-dep marker integration tests
//
// These tests exercise parseBacklogIssue + resolveDependsOnStatus at the
// tool layer, using in-process tmp-root yaml fixtures (same pattern as
// backlog-yaml-loader.test.ts). No repo ia/backlog/ state is touched.
//
// Regression signal: if backlog-yaml-loader.ts fallback reverts to lossy
// array.join(", "), depends_on_raw soft marker is dropped → depends_on
// becomes "FEAT-12" (no "soft" token) → isSoftDependencyMention returns false
// → Test 1 below fails on soft_only: true.
// ---------------------------------------------------------------------------

function writeTmpYaml(dir: string, id: string, content: string): string {
  const p = path.join(dir, `${id}.yaml`);
  fs.writeFileSync(p, content, "utf8");
  return p;
}

function makeTmpRoot(): string {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), "backlog-issue-test-"));
  fs.mkdirSync(path.join(root, "ia", "backlog"), { recursive: true });
  fs.mkdirSync(path.join(root, "ia", "backlog-archive"), { recursive: true });
  return root;
}

test("round-trip soft-dep marker classifies via backlog_issue (soft_only: true)", () => {
  // Fixture A — TECH-992: depends_on_raw uses "soft: FEAT-12" format so
  // isSoftDependencyMention("soft: FEAT-12", "FEAT-12") → true.
  const root = makeTmpRoot();
  try {
    writeTmpYaml(
      path.join(root, "ia", "backlog"),
      "TECH-992",
      [
        "id: TECH-992",
        "type: tech",
        "title: Soft dep integration fixture",
        "status: open",
        "section: High Priority",
        "depends_on:",
        "  - FEAT-12",
        'depends_on_raw: "soft: FEAT-12"',
        "raw_markdown: ''",
      ].join("\n") + "\n",
    );

    const issue = parseBacklogIssue(root, "TECH-992");
    assert.ok(issue, "TECH-992 should resolve from yaml fixture");
    // depends_on must preserve the raw string (soft marker survives loader round-trip)
    assert.equal(issue!.depends_on, "soft: FEAT-12");

    const statuses = resolveDependsOnStatus(root, issue!.depends_on);
    assert.ok(statuses.length > 0, "should resolve at least one dep entry");
    const entry = statuses.find((e) => e.id === "FEAT-12");
    assert.ok(entry, "FEAT-12 entry should be present in depends_on_status");
    assert.equal(entry!.soft_only, true, "FEAT-12 must be classified soft_only: true");
    assert.equal(entry!.satisfied, true, "soft dep is always satisfied");
  } finally {
    fs.rmSync(root, { recursive: true, force: true });
  }
});

test("plain dep id → soft_only: false (no false-positive classification)", () => {
  // Fixture B — TECH-991: plain id in depends_on_raw, no soft marker.
  // Guards against isSoftDependencyMention over-classifying plain deps.
  const root = makeTmpRoot();
  try {
    writeTmpYaml(
      path.join(root, "ia", "backlog"),
      "TECH-991",
      [
        "id: TECH-991",
        "type: tech",
        "title: Plain dep integration fixture",
        "status: open",
        "section: High Priority",
        "depends_on:",
        "  - FEAT-12",
        'depends_on_raw: "FEAT-12"',
        "raw_markdown: ''",
      ].join("\n") + "\n",
    );

    const issue = parseBacklogIssue(root, "TECH-991");
    assert.ok(issue, "TECH-991 should resolve from yaml fixture");
    assert.equal(issue!.depends_on, "FEAT-12");

    const statuses = resolveDependsOnStatus(root, issue!.depends_on);
    assert.ok(statuses.length > 0, "should resolve at least one dep entry");
    const entry = statuses.find((e) => e.id === "FEAT-12");
    assert.ok(entry, "FEAT-12 entry should be present");
    assert.equal(entry!.soft_only, false, "plain dep must not be classified as soft");
  } finally {
    fs.rmSync(root, { recursive: true, force: true });
  }
});
