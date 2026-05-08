/**
 * run-csharp-lint.test.mjs
 *
 * Asserts lint caps: warn at threshold, err at hard cap.
 * Uses --file flag to lint synthetic fixtures.
 */

import { describe, it, before, after } from "node:test";
import assert from "node:assert/strict";
import { execSync } from "node:child_process";
import { writeFileSync, mkdirSync, rmSync } from "node:fs";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const REPO_ROOT = resolve(__dirname, "../../..");
const LINT_SCRIPT = resolve(__dirname, "../run-csharp-lint.mjs");
const TMP_DIR = resolve(__dirname, "../../../tmp-lint-test-fixtures");

function runLint(filePath, warnOnly = false) {
  const flag = warnOnly ? "--warn-only" : "";
  try {
    const out = execSync(`node "${LINT_SCRIPT}" --file "${filePath}" ${flag}`, {
      encoding: "utf8",
      stdio: ["pipe", "pipe", "pipe"],
    });
    return { exitCode: 0, stdout: out, stderr: "" };
  } catch (e) {
    return { exitCode: e.status ?? 1, stdout: e.stdout ?? "", stderr: e.stderr ?? "" };
  }
}

function makeMethod(name, locCount, escapeHatch = false) {
  const lines = [`    public void ${name}()`];
  if (escapeHatch) lines.unshift(`    // long-method-allowed: test synthetic fixture`);
  lines.push("    {");
  for (let i = 0; i < locCount; i++) lines.push(`        int x${i} = ${i};`);
  lines.push("    }");
  return lines.join("\n");
}

function makeSyntheticFile(methods, fileLinePad = 0) {
  const header = `using System;\nnamespace LintTest {\n    public class SyntheticClass {\n`;
  const footer = Array(fileLinePad).fill("    // padding line").join("\n") + `\n    }\n}\n`;
  return header + methods.join("\n") + "\n" + footer;
}

before(() => { mkdirSync(TMP_DIR, { recursive: true }); });
after(() => { try { rmSync(TMP_DIR, { recursive: true, force: true }); } catch {} });

describe("lint_caps_warn_at_threshold_and_err_at_hard_cap", () => {
  it("method at 90 LOC emits error (over hard cap 80)", () => {
    const src = makeSyntheticFile([makeMethod("LongMethod", 90)]);
    const fp = resolve(TMP_DIR, "method-90.cs");
    writeFileSync(fp, src);
    const { exitCode, stdout, stderr } = runLint(fp);
    assert.equal(exitCode, 1, "Expected exit 1 for 90-LOC method");
    const combined = stdout + stderr;
    assert.match(combined, /ERR.*LongMethod.*LOC=9[0-9]/, "Expected ERR line for LongMethod");
  });

  it("method at 200 LOC emits error", () => {
    const src = makeSyntheticFile([makeMethod("VeryLongMethod", 200)]);
    const fp = resolve(TMP_DIR, "method-200.cs");
    writeFileSync(fp, src);
    const { exitCode, stderr, stdout } = runLint(fp);
    assert.equal(exitCode, 1, "Expected exit 1 for 200-LOC method");
    const combined = stdout + stderr;
    assert.match(combined, /ERR.*VeryLongMethod.*LOC=2[0-9][0-9]/, "Expected ERR line for VeryLongMethod");
  });

  it("method at 50 LOC emits warning only (between warn=40 and err=80)", () => {
    const src = makeSyntheticFile([makeMethod("WarnMethod", 50)]);
    const fp = resolve(TMP_DIR, "method-50.cs");
    writeFileSync(fp, src);
    const { exitCode, stdout, stderr } = runLint(fp);
    assert.equal(exitCode, 0, "Expected exit 0 for 50-LOC method (warn only)");
    const combined = stdout + stderr;
    assert.match(combined, /WARN.*WarnMethod.*LOC=5[0-9]/, "Expected WARN line for WarnMethod");
  });

  it("method at 30 LOC is clean (under warn cap)", () => {
    const src = makeSyntheticFile([makeMethod("CleanMethod", 30)]);
    const fp = resolve(TMP_DIR, "method-30.cs");
    writeFileSync(fp, src);
    const { exitCode, stdout, stderr } = runLint(fp);
    assert.equal(exitCode, 0, "Expected exit 0 for clean 30-LOC method");
  });

  it("90-LOC method with escape-hatch emits warn not err", () => {
    const src = makeSyntheticFile([makeMethod("HatchMethod", 90, true)]);
    const fp = resolve(TMP_DIR, "method-hatch.cs");
    writeFileSync(fp, src);
    const { exitCode, stdout, stderr } = runLint(fp);
    assert.equal(exitCode, 0, "Expected exit 0 when escape-hatch present");
    const combined = stdout + stderr;
    assert.match(combined, /WARN.*HatchMethod/, "Expected WARN (not ERR) for escape-hatch method");
    assert.doesNotMatch(combined, /ERR.*HatchMethod/, "Should not emit ERR for escape-hatch method");
  });
});
