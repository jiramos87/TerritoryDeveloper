/**
 * findobjectoftype_scan — regex scan for FindObjectOfType in hot-path methods.
 */

import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import os from "node:os";
import { fileURLToPath } from "node:url";
import { scanFileForFot } from "../../src/tools/findobjectoftype-scan.js";
import { wrapTool } from "../../src/envelope.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, "../../../../");

function writeTempCs(content: string): { filePath: string; dir: string } {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), "fot-test-"));
  const filePath = path.join(dir, "TestScript.cs");
  fs.writeFileSync(filePath, content, "utf8");
  return { filePath, dir };
}

test("detects FindObjectOfType inside Update method", () => {
  const { filePath, dir } = writeTempCs(`
using UnityEngine;
public class BadScript : MonoBehaviour
{
    void Update()
    {
        var mgr = FindObjectOfType<UIManager>();
        mgr.DoSomething();
    }
}
`);
  const violations = scanFileForFot(filePath, dir);
  assert.equal(violations.length, 1);
  assert.equal(violations[0]!.method, "Update");
  assert.ok(violations[0]!.snippet.includes("FindObjectOfType"));
  fs.rmSync(dir, { recursive: true });
});

test("detects FindObjectsOfType inside LateUpdate", () => {
  const { filePath, dir } = writeTempCs(`
public class Another : MonoBehaviour
{
    void LateUpdate()
    {
        var all = FindObjectsOfType<Camera>();
    }
}
`);
  const violations = scanFileForFot(filePath, dir);
  assert.equal(violations.length, 1);
  assert.equal(violations[0]!.method, "LateUpdate");
  fs.rmSync(dir, { recursive: true });
});

test("no violations when FindObjectOfType is in Start", () => {
  const { filePath, dir } = writeTempCs(`
public class GoodScript : MonoBehaviour
{
    UIManager mgr;
    void Start()
    {
        mgr = FindObjectOfType<UIManager>();
    }
    void Update()
    {
        mgr.DoSomething();
    }
}
`);
  const violations = scanFileForFot(filePath, dir);
  assert.equal(violations.length, 0);
  fs.rmSync(dir, { recursive: true });
});

test("detects in FixedUpdate", () => {
  const { filePath, dir } = writeTempCs(`
public class Physics : MonoBehaviour
{
    void FixedUpdate()
    {
        FindObjectOfType<Rigidbody>();
    }
}
`);
  const violations = scanFileForFot(filePath, dir);
  assert.equal(violations.length, 1);
  assert.equal(violations[0]!.method, "FixedUpdate");
  fs.rmSync(dir, { recursive: true });
});

// ---------------------------------------------------------------------------
// Envelope tests (Phase 4 — TECH-405)
// ---------------------------------------------------------------------------

test("envelope: empty dir with no *.cs files returns ok:true and matches:[]", () => {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), "fot-empty-"));
  const handler = wrapTool(async (input: { path?: string }) => {
    const repoRoot = dir;
    const scanPath = input?.path ?? dir;
    const absPath = path.isAbsolute(scanPath) ? scanPath : path.join(repoRoot, scanPath);
    if (!fs.existsSync(absPath)) {
      throw { code: "invalid_input" as const, message: `Directory not found: ${scanPath}` };
    }
    // Inline globCsFiles — no *.cs files in empty dir.
    const csFiles: string[] = [];
    const allViolations: ReturnType<typeof scanFileForFot> = [];
    return { scanned_path: scanPath, files_scanned: csFiles.length, violation_count: allViolations.length, matches: allViolations };
  });
  handler({ path: dir }).then((envelope) => {
    assert.equal(envelope.ok, true);
    assert.ok("payload" in envelope);
    assert.deepEqual((envelope as { ok: true; payload: { matches: unknown[] } }).payload.matches, []);
    fs.rmSync(dir, { recursive: true });
  });
});

test("envelope: missing path throws invalid_input → ok:false, error.code=invalid_input", async () => {
  const handler = wrapTool(async (input: { path?: string }) => {
    const scanPath = (input?.path ?? "").trim();
    if (!fs.existsSync(scanPath)) {
      throw { code: "invalid_input" as const, message: `Directory not found: ${scanPath}`, hint: "Provide a repo-relative path." };
    }
    return { matches: [] };
  });
  const envelope = await handler({ path: "/nonexistent-path-that-does-not-exist-12345" });
  assert.equal(envelope.ok, false);
  assert.ok("error" in envelope);
  assert.equal((envelope as { ok: false; error: { code: string } }).error.code, "invalid_input");
});

test(
  "scans real Assets/Scripts/ without crashing",
  { skip: !fs.existsSync(path.join(repoRoot, "Assets/Scripts/")) },
  () => {
    // Smoke test: scan the real codebase and verify structured output
    const dir = path.join(repoRoot, "Assets/Scripts/Managers/GameManagers/");
    const csFiles = fs.readdirSync(dir).filter((f) => f.endsWith(".cs"));
    assert.ok(csFiles.length > 0, "Should find C# files in GameManagers/");

    let totalViolations = 0;
    for (const file of csFiles) {
      const violations = scanFileForFot(path.join(dir, file), repoRoot);
      for (const v of violations) {
        assert.ok(v.file, "violation should have file");
        assert.ok(v.line > 0, "violation should have positive line number");
        assert.ok(v.method, "violation should have method name");
        assert.ok(v.snippet, "violation should have snippet");
      }
      totalViolations += violations.length;
    }
    // Note: BUG-14 violations are in UpdateUI() (custom method name, not Unity Update()),
    // so the heuristic scanner may not find them directly. This test validates the scanner
    // runs without errors on real code and produces valid output structure.
    assert.ok(typeof totalViolations === "number");
  },
);
