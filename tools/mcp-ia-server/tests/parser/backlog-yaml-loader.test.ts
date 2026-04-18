import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { loadYamlIssue } from "../../src/parser/backlog-yaml-loader.js";
import { isSoftDependencyMention } from "../../src/parser/backlog-parser.js";

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function writeTmpYaml(dir: string, id: string, content: string): string {
  const p = path.join(dir, `${id}.yaml`);
  fs.writeFileSync(p, content, "utf8");
  return p;
}

function makeTmpRoot(): string {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), "yaml-loader-test-"));
  fs.mkdirSync(path.join(root, "ia", "backlog"), { recursive: true });
  fs.mkdirSync(path.join(root, "ia", "backlog-archive"), { recursive: true });
  return root;
}

// ---------------------------------------------------------------------------
// Fixture A — all three fields populated
// ---------------------------------------------------------------------------

test("yamlToIssue: populated priority / related / created are mapped", () => {
  const root = makeTmpRoot();
  try {
    writeTmpYaml(
      path.join(root, "ia", "backlog"),
      "TECH-999",
      [
        "id: TECH-999",
        "type: tech",
        "title: Test issue",
        "status: open",
        "section: High Priority",
        "priority: high",
        "related:",
        "  - TECH-100",
        "  - FEAT-42",
        "created: 2026-04-17",
        "raw_markdown: ''",
      ].join("\n") + "\n",
    );

    const issue = loadYamlIssue(root, "TECH-999");
    assert.ok(issue, "loadYamlIssue returned null");
    assert.equal(issue!.priority, "high");
    assert.deepEqual(issue!.related, ["TECH-100", "FEAT-42"]);
    assert.equal(issue!.created, "2026-04-17");
  } finally {
    fs.rmSync(root, { recursive: true, force: true });
  }
});

// ---------------------------------------------------------------------------
// Fixture B — fields absent → safe defaults
// ---------------------------------------------------------------------------

test("yamlToIssue: absent priority / related / created default to null / undefined / null", () => {
  const root = makeTmpRoot();
  try {
    writeTmpYaml(
      path.join(root, "ia", "backlog"),
      "TECH-998",
      [
        "id: TECH-998",
        "type: tech",
        "title: Minimal issue",
        "status: open",
        "section: High Priority",
        "raw_markdown: ''",
      ].join("\n") + "\n",
    );

    const issue = loadYamlIssue(root, "TECH-998");
    assert.ok(issue, "loadYamlIssue returned null");
    assert.equal(issue!.priority, null);
    // absent 'related' field → undefined (optional, honor the type signature)
    assert.equal(issue!.related, undefined);
    assert.equal(issue!.created, null);
  } finally {
    fs.rmSync(root, { recursive: true, force: true });
  }
});

// ---------------------------------------------------------------------------
// Fixture D — depends_on_raw present: soft marker must survive round-trip
// ---------------------------------------------------------------------------

test("yamlToIssue: depends_on_raw with soft marker preserved verbatim", () => {
  const root = makeTmpRoot();
  try {
    writeTmpYaml(
      path.join(root, "ia", "backlog"),
      "TECH-996",
      [
        "id: TECH-996",
        "type: tech",
        "title: Soft dep test",
        "status: open",
        "section: High Priority",
        "depends_on:",
        "  - FEAT-12",
        'depends_on_raw: "FEAT-12 (soft)"',
        "raw_markdown: ''",
      ].join("\n") + "\n",
    );

    const issue = loadYamlIssue(root, "TECH-996");
    assert.ok(issue, "loadYamlIssue returned null");
    assert.equal(issue!.depends_on, "FEAT-12 (soft)");
  } finally {
    fs.rmSync(root, { recursive: true, force: true });
  }
});

// ---------------------------------------------------------------------------
// Fixture E — depends_on_raw absent: fallback synthesizes from array (lossy)
// ---------------------------------------------------------------------------

test("yamlToIssue: depends_on_raw absent — fallback synthesizes from array without markers", () => {
  const root = makeTmpRoot();
  try {
    writeTmpYaml(
      path.join(root, "ia", "backlog"),
      "TECH-995",
      [
        "id: TECH-995",
        "type: tech",
        "title: No raw dep test",
        "status: open",
        "section: High Priority",
        "depends_on:",
        "  - FEAT-12",
        "raw_markdown: ''",
      ].join("\n") + "\n",
    );

    const issue = loadYamlIssue(root, "TECH-995");
    assert.ok(issue, "loadYamlIssue returned null");
    assert.equal(issue!.depends_on, "FEAT-12");
  } finally {
    fs.rmSync(root, { recursive: true, force: true });
  }
});

// ---------------------------------------------------------------------------
// Fixture F — depends_on_raw empty string: fallback synthesizes from array
// ---------------------------------------------------------------------------

test("yamlToIssue: depends_on_raw empty string — fallback synthesizes from array", () => {
  const root = makeTmpRoot();
  try {
    writeTmpYaml(
      path.join(root, "ia", "backlog"),
      "TECH-994",
      [
        "id: TECH-994",
        "type: tech",
        "title: Empty raw dep test",
        "status: open",
        "section: High Priority",
        "depends_on:",
        "  - FEAT-12",
        'depends_on_raw: ""',
        "raw_markdown: ''",
      ].join("\n") + "\n",
    );

    const issue = loadYamlIssue(root, "TECH-994");
    assert.ok(issue, "loadYamlIssue returned null");
    assert.equal(issue!.depends_on, "FEAT-12");
  } finally {
    fs.rmSync(root, { recursive: true, force: true });
  }
});

// ---------------------------------------------------------------------------
// Fixture C — related as explicit empty list []
// ---------------------------------------------------------------------------

test("yamlToIssue: explicit empty related: [] returns []", () => {
  const root = makeTmpRoot();
  try {
    writeTmpYaml(
      path.join(root, "ia", "backlog"),
      "TECH-997",
      [
        "id: TECH-997",
        "type: tech",
        "title: Empty related",
        "status: open",
        "section: High Priority",
        "related: []",
        "raw_markdown: ''",
      ].join("\n") + "\n",
    );

    const issue = loadYamlIssue(root, "TECH-997");
    assert.ok(issue, "loadYamlIssue returned null");
    assert.deepEqual(issue!.related, []);
  } finally {
    fs.rmSync(root, { recursive: true, force: true });
  }
});

// ---------------------------------------------------------------------------
// TECH-364 Phase 3 — locator field mapping
// ---------------------------------------------------------------------------

test("yamlToIssue: all 9 locator fields populated from yaml", () => {
  const root = makeTmpRoot();
  try {
    writeTmpYaml(
      path.join(root, "ia", "backlog"),
      "TECH-990",
      [
        "id: TECH-990",
        "type: tech",
        "title: Locator full",
        "status: open",
        "section: High Priority",
        "parent_plan: ia/projects/foo-master-plan.md",
        "task_key: T3.1.2",
        "step: 3",
        "stage: 3.1",
        "phase: 2",
        "router_domain: backlog-yaml",
        "surfaces:",
        "  - backlog-yaml-loader.ts",
        "mcp_slices:",
        "  - backlog-yaml-mcp-alignment-master-plan::Stage 3.1",
        "skill_hints:",
        "  - project-spec-implement",
        "raw_markdown: ''",
      ].join("\n") + "\n",
    );

    const issue = loadYamlIssue(root, "TECH-990");
    assert.ok(issue, "loadYamlIssue returned null");
    assert.equal(issue!.parent_plan, "ia/projects/foo-master-plan.md");
    assert.equal(issue!.task_key, "T3.1.2");
    assert.equal(issue!.step, 3);
    assert.equal(issue!.stage, "3.1");
    assert.equal(issue!.phase, 2);
    assert.equal(issue!.router_domain, "backlog-yaml");
    assert.deepEqual(issue!.surfaces, ["backlog-yaml-loader.ts"]);
    assert.deepEqual(issue!.mcp_slices, ["backlog-yaml-mcp-alignment-master-plan::Stage 3.1"]);
    assert.deepEqual(issue!.skill_hints, ["project-spec-implement"]);
  } finally {
    fs.rmSync(root, { recursive: true, force: true });
  }
});

test("yamlToIssue: minimal locator (only parent_plan + task_key) — other fields default", () => {
  const root = makeTmpRoot();
  try {
    writeTmpYaml(
      path.join(root, "ia", "backlog"),
      "TECH-989",
      [
        "id: TECH-989",
        "type: tech",
        "title: Locator minimal",
        "status: open",
        "section: High Priority",
        "parent_plan: ia/projects/bar-master-plan.md",
        "task_key: T1.2",
        "raw_markdown: ''",
      ].join("\n") + "\n",
    );

    const issue = loadYamlIssue(root, "TECH-989");
    assert.ok(issue, "loadYamlIssue returned null");
    assert.equal(issue!.parent_plan, "ia/projects/bar-master-plan.md");
    assert.equal(issue!.task_key, "T1.2");
    assert.equal(issue!.step, null);
    assert.equal(issue!.stage, null);
    assert.equal(issue!.phase, null);
    assert.equal(issue!.router_domain, null);
    assert.deepEqual(issue!.surfaces, []);
    assert.deepEqual(issue!.mcp_slices, []);
    assert.deepEqual(issue!.skill_hints, []);
  } finally {
    fs.rmSync(root, { recursive: true, force: true });
  }
});

test("yamlToIssue: v1 yaml (zero locator fields) — all locator members default to null / []", () => {
  const root = makeTmpRoot();
  try {
    writeTmpYaml(
      path.join(root, "ia", "backlog"),
      "TECH-988",
      [
        "id: TECH-988",
        "type: tech",
        "title: V1 legacy issue",
        "status: open",
        "section: High Priority",
        "priority: low",
        "raw_markdown: ''",
      ].join("\n") + "\n",
    );

    const issue = loadYamlIssue(root, "TECH-988");
    assert.ok(issue, "loadYamlIssue returned null");
    // Pre-existing fields still work
    assert.equal(issue!.priority, "low");
    // Locator fields absent → defaults
    assert.equal(issue!.parent_plan, null);
    assert.equal(issue!.task_key, null);
    assert.equal(issue!.step, null);
    assert.equal(issue!.stage, null);
    assert.equal(issue!.phase, null);
    assert.equal(issue!.router_domain, null);
    assert.deepEqual(issue!.surfaces, []);
    assert.deepEqual(issue!.mcp_slices, []);
    assert.deepEqual(issue!.skill_hints, []);
  } finally {
    fs.rmSync(root, { recursive: true, force: true });
  }
});

// ---------------------------------------------------------------------------
// Phase 3 — chain-through: fixture D depends_on feeds isSoftDependencyMention
// ---------------------------------------------------------------------------

test("resolveDependsOnStatus chain: fixture D soft marker classifies as soft via isSoftDependencyMention", () => {
  const root = makeTmpRoot();
  try {
    // Use "soft: FEAT-12" format — the pattern isSoftDependencyMention recognises
    // (soft token precedes id in the prose string).
    writeTmpYaml(
      path.join(root, "ia", "backlog"),
      "TECH-993",
      [
        "id: TECH-993",
        "type: tech",
        "title: Chain soft dep",
        "status: open",
        "section: High Priority",
        "depends_on:",
        "  - FEAT-12",
        'depends_on_raw: "soft: FEAT-12"',
        "raw_markdown: ''",
      ].join("\n") + "\n",
    );

    const issue = loadYamlIssue(root, "TECH-993");
    assert.ok(issue, "loadYamlIssue returned null");
    assert.equal(issue!.depends_on, "soft: FEAT-12");
    // Chain: raw string fed downstream classifies the dep as soft
    assert.equal(
      isSoftDependencyMention(issue!.depends_on!, "FEAT-12"),
      true,
      "isSoftDependencyMention should flag FEAT-12 as soft",
    );
  } finally {
    fs.rmSync(root, { recursive: true, force: true });
  }
});
