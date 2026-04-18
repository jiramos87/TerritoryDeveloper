/**
 * Tests for loadAllYamlIssues malformed-yaml handling (C4 fix).
 * Verifies:
 *   - malformed yaml (missing required 'id') is logged to stderr
 *   - parseErrorCount is incremented
 *   - good records in the same dir still load
 */

import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { loadAllYamlIssues } from "../../src/parser/backlog-yaml-loader.js";

function makeTmpRoot(): string {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), "yaml-malformed-test-"));
  fs.mkdirSync(path.join(root, "ia", "backlog"), { recursive: true });
  fs.mkdirSync(path.join(root, "ia", "backlog-archive"), { recursive: true });
  return root;
}

function writeTmpYaml(dir: string, filename: string, content: string): void {
  fs.writeFileSync(path.join(dir, filename), content, "utf8");
}

const GOOD_YAML = [
  "id: TECH-001",
  "type: tech",
  "title: Good record",
  "status: open",
  "section: High Priority",
  "priority: high",
  "related: []",
  "created: 2026-04-17",
  "raw_markdown: ''",
].join("\n") + "\n";

const BAD_YAML = [
  "# Missing required id field",
  "title: Bad record",
  "status: open",
  "section: High Priority",
].join("\n") + "\n";

test("loadAllYamlIssues: malformed yaml increments parseErrorCount + good records still load", () => {
  const root = makeTmpRoot();
  try {
    const openDir = path.join(root, "ia", "backlog");
    writeTmpYaml(openDir, "TECH-001.yaml", GOOD_YAML);
    writeTmpYaml(openDir, "BAD-001.yaml", BAD_YAML);

    // Capture stderr
    const stderrLines: string[] = [];
    const origStderr = process.stderr.write.bind(process.stderr);
    process.stderr.write = (chunk: string | Uint8Array, ...rest: unknown[]) => {
      if (typeof chunk === "string") stderrLines.push(chunk);
      return (origStderr as (...a: unknown[]) => boolean)(chunk, ...rest);
    };

    let result;
    try {
      result = loadAllYamlIssues(root, "open");
    } finally {
      process.stderr.write = origStderr;
    }

    // parseErrorCount === 1 (the BAD-001 file)
    assert.equal(result.parseErrorCount, 1, "parseErrorCount should be 1");

    // Good records still load
    assert.equal(result.records.length, 1, "only 1 good record loaded");
    assert.equal(result.records[0]!.issue_id, "TECH-001");

    // stderr received a [backlog-yaml] error line
    const errorLine = stderrLines.find((l) => l.includes("[backlog-yaml] parse error"));
    assert.ok(errorLine, "stderr should contain [backlog-yaml] parse error line");
    assert.ok(
      errorLine!.includes("BAD-001.yaml"),
      "stderr line should name the failing file",
    );
  } finally {
    fs.rmSync(root, { recursive: true, force: true });
  }
});

test("loadAllYamlIssues: no errors when all records are valid", () => {
  const root = makeTmpRoot();
  try {
    const openDir = path.join(root, "ia", "backlog");
    writeTmpYaml(openDir, "TECH-002.yaml", GOOD_YAML.replace("TECH-001", "TECH-002"));

    const result = loadAllYamlIssues(root, "open");
    assert.equal(result.parseErrorCount, 0, "parseErrorCount should be 0");
    assert.equal(result.records.length, 1);
  } finally {
    fs.rmSync(root, { recursive: true, force: true });
  }
});
