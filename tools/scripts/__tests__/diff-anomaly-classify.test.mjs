// TECH-15906 — unit tests for diff-anomaly-classify.mjs
// Red-stage proof: flags_debug_and_meta_delete

import assert from "node:assert";
import { test } from "node:test";
import { classifyDiffAnomalies } from "../diff-anomaly-classify.mjs";

// ---------------------------------------------------------------------------
// Red-stage proof: flags_debug_and_meta_delete
// ---------------------------------------------------------------------------

test("flags_debug_and_meta_delete — detects both anomaly kinds", () => {
  const diff = [
    "diff --git a/foo.cs b/foo.cs",
    "--- a/foo.cs",
    "+++ b/foo.cs",
    "@@ -1,3 +1,4 @@",
    " class Foo {",
    "+  Debug.Log(\"test\");",
    " }",
    "diff --git a/bar.meta b/bar.meta",
    "--- a/bar.meta",
    "+++ /dev/null",
  ].join("\n");

  const result = classifyDiffAnomalies(diff);
  assert.strictEqual(result.ok, false);
  assert.strictEqual(result.anomaly_count, 2);

  const kinds = result.anomalies.map((a) => a.kind);
  assert.ok(kinds.includes("debug_log"), `expected debug_log in ${JSON.stringify(kinds)}`);
  assert.ok(kinds.includes("meta_delete"), `expected meta_delete in ${JSON.stringify(kinds)}`);
});

// ---------------------------------------------------------------------------
// Additional cases
// ---------------------------------------------------------------------------

test("clean diff returns ok=true, no anomalies", () => {
  const diff = [
    "diff --git a/foo.cs b/foo.cs",
    "--- a/foo.cs",
    "+++ b/foo.cs",
    "@@ -1,3 +1,4 @@",
    " class Foo {",
    "+  int x = 1;",
    " }",
  ].join("\n");

  const result = classifyDiffAnomalies(diff);
  assert.strictEqual(result.ok, true);
  assert.strictEqual(result.anomaly_count, 0);
});

test("console.log on added line flagged as debug_log", () => {
  const diff = "+  console.log('debug');";
  const result = classifyDiffAnomalies(diff);
  assert.strictEqual(result.ok, false);
  assert.ok(result.anomalies.some((a) => a.kind === "debug_log"));
});

test("debug log on context line (no +) not flagged", () => {
  const diff = "   Debug.Log('context line — not added');";
  const result = classifyDiffAnomalies(diff);
  assert.strictEqual(result.ok, true);
});

test("retired symbol on added line flagged", () => {
  const diff = "+  var tp = new ThemedPanel();";
  const result = classifyDiffAnomalies(diff);
  assert.strictEqual(result.ok, false);
  assert.ok(result.anomalies.some((a) => a.kind === "retired_symbol"));
});

test("empty diff is clean", () => {
  const result = classifyDiffAnomalies("");
  assert.strictEqual(result.ok, true);
  assert.strictEqual(result.anomaly_count, 0);
});
