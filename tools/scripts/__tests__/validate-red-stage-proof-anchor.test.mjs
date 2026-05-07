// TECH-22668 unit tests for validate-red-stage-proof-anchor.mjs.
//
// Tests pure-function exports — no DB, no subprocess.
// Covers all 5 §Test Blueprint cases from the plan digest.

import assert from "node:assert";
import * as os from "node:os";
import * as fs from "node:fs";
import * as path from "node:path";
import { test } from "node:test";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "..", "..", "..");

import {
  extractRedStageProofSection,
  parseAnchors,
  extractSurfaceKeywords,
  extractTestMethodBody,
  validateAnchor,
  validateAllAnchors,
} from "../validate-red-stage-proof-anchor.mjs";

// ── Helpers ────────────────────────────────────────────────────────────────

/**
 * Write a temp file and return its path relative to REPO_ROOT.
 * Cleans up after each test via returned disposer.
 */
function writeTempFile(dir, filename, content) {
  const absPath = path.join(dir, filename);
  fs.writeFileSync(absPath, content, "utf8");
  return absPath;
}

function makeTaskBody(anchorLine, prose) {
  return `
## §Plan Digest

### §Goal
Some goal text.

### §Red-Stage Proof

**Anchor:** \`${anchorLine}\`

${prose}

### §Work Items
- step 1
`;
}

// ── Tests ──────────────────────────────────────────────────────────────────

test("DriftedAnchor_ExitsNonZero — spec keyword missing from test body → exit 1", () => {
  const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), "anchor-test-"));
  try {
    // Seed test file with a body that references panels.json, NOT CatalogPrefabRef.
    const testContent = `
using NUnit.Framework;
public class HudBarTest {
    [Test]
    public void HudBar_Root_IsCatalogPrefabInstance() {
        string raw = File.ReadAllText("panels.json");
        Assert.IsTrue(raw.Contains("slug: hud-bar"), "must have slug");
    }
}
`;
    const testFilePath = writeTempFile(tmpDir, "HudBarTest.cs", testContent);
    const relPath = path.relative(REPO_ROOT, testFilePath).replace(/\\/g, "/");

    const bodyMd = makeTaskBody(
      `${relPath}::HudBar_Root_IsCatalogPrefabInstance`,
      `Red: scene has no CatalogPrefabRef. Assert CatalogPrefabRef.slug=="hud-bar" AND zero illuminated-button descendants.`,
    );

    const { errors, driftCount } = validateAllAnchors(
      [{ taskId: "TECH-99999", bodyMd }],
      REPO_ROOT,
    );

    assert.ok(driftCount > 0, `expected drift but got 0 errors; errors=${JSON.stringify(errors)}`);
    const driftLine = errors.find((e) => e.includes("[anchor-drift]"));
    assert.ok(driftLine, `expected [anchor-drift] line in errors; got: ${JSON.stringify(errors)}`);
    assert.ok(
      driftLine.includes("CatalogPrefabRef"),
      `expected missing keyword CatalogPrefabRef in drift line: ${driftLine}`,
    );
  } finally {
    fs.rmSync(tmpDir, { recursive: true, force: true });
  }
});

test("AlignedAnchor_ExitsZero — test body references CatalogPrefabRef → no drift", () => {
  const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), "anchor-test-"));
  try {
    const testContent = `
using NUnit.Framework;
using Territory.UI;
public class HudBarTest {
    [Test]
    public void HudBar_Root_IsCatalogPrefabInstance() {
        var catalogRef = hudBar.GetComponent<CatalogPrefabRef>();
        Assert.IsNotNull(catalogRef, "missing CatalogPrefabRef");
        Assert.AreEqual("hud-bar", catalogRef.slug);
    }
}
`;
    const testFilePath = writeTempFile(tmpDir, "HudBarTest.cs", testContent);
    const relPath = path.relative(REPO_ROOT, testFilePath).replace(/\\/g, "/");

    const bodyMd = makeTaskBody(
      `${relPath}::HudBar_Root_IsCatalogPrefabInstance`,
      `Red: scene has no CatalogPrefabRef. Assert CatalogPrefabRef.slug=="hud-bar" AND zero illuminated-button descendants.`,
    );

    const { errors, driftCount } = validateAllAnchors(
      [{ taskId: "TECH-99999", bodyMd }],
      REPO_ROOT,
    );

    assert.strictEqual(driftCount, 0, `expected 0 drift; errors=${JSON.stringify(errors)}`);
    assert.strictEqual(errors.length, 0, `expected no errors; got: ${JSON.stringify(errors)}`);
  } finally {
    fs.rmSync(tmpDir, { recursive: true, force: true });
  }
});

test("MissingAnchor_Skipped — spec without §Red-Stage Proof → no errors", () => {
  const bodyMd = `
## §Plan Digest

### §Goal
Just a goal, no red-stage proof section.

### §Work Items
- do something
`;
  const { errors, driftCount } = validateAllAnchors(
    [{ taskId: "TECH-11111", bodyMd }],
    REPO_ROOT,
  );
  assert.strictEqual(driftCount, 0, "expected 0 drift for spec without §Red-Stage Proof");
  assert.strictEqual(errors.length, 0, "expected no errors for spec without §Red-Stage Proof");
});

test("MultiAnchor_AllChecked — spec with Anchor 1 + Anchor 2 → both validated independently", () => {
  const tmpDir = fs.mkdtempSync(path.join(os.tmpdir(), "anchor-test-"));
  try {
    // Anchor 1 — aligned.
    const testContent1 = `
using NUnit.Framework;
using Territory.UI;
public class TestA {
    [Test]
    public void TestMethodOne() {
        var catalogRef = go.GetComponent<CatalogPrefabRef>();
        Assert.IsNotNull(catalogRef);
    }
}
`;
    const file1 = writeTempFile(tmpDir, "TestA.cs", testContent1);
    const rel1 = path.relative(REPO_ROOT, file1).replace(/\\/g, "/");

    // Anchor 2 — drifted (missing GrowthBudgetPanel keyword).
    const testContent2 = `
using NUnit.Framework;
public class TestB {
    [Test]
    public void TestMethodTwo() {
        Assert.IsTrue(true, "placeholder");
    }
}
`;
    const file2 = writeTempFile(tmpDir, "TestB.cs", testContent2);
    const rel2 = path.relative(REPO_ROOT, file2).replace(/\\/g, "/");

    const bodyMd = `
## §Plan Digest

### §Red-Stage Proof

**Anchor 1:** \`${rel1}::TestMethodOne\`

Post-swap: scene root carries CatalogPrefabRef component attached by bridge.

**Anchor 2:** \`${rel2}::TestMethodTwo\`

Expects GrowthBudgetPanel visibility toggled correctly.
`;

    const { errors, driftCount } = validateAllAnchors(
      [{ taskId: "TECH-22222", bodyMd }],
      REPO_ROOT,
    );

    // Anchor 1 is aligned; Anchor 2 is drifted (GrowthBudgetPanel missing).
    assert.strictEqual(driftCount, 1, `expected 1 drift (Anchor 2); got ${driftCount}`);
    const driftLine = errors.find((e) => e.includes("TestMethodTwo"));
    assert.ok(driftLine, `expected drift line for TestMethodTwo; got: ${JSON.stringify(errors)}`);
  } finally {
    fs.rmSync(tmpDir, { recursive: true, force: true });
  }
});

test("BrokenFilePath_ExitsNonZero — anchor points to non-existent file → anchor-missing error", () => {
  const bodyMd = makeTaskBody(
    `path/to/nonexistent/file/DoesNotExist.cs::SomeTestMethod`,
    `Asserts SomeComponent present on root.`,
  );

  const { errors, driftCount } = validateAllAnchors(
    [{ taskId: "TECH-33333", bodyMd }],
    REPO_ROOT,
  );

  assert.ok(driftCount > 0, `expected drift for broken file path; got 0`);
  const missingLine = errors.find((e) => e.includes("[anchor-missing]"));
  assert.ok(missingLine, `expected [anchor-missing] line; got: ${JSON.stringify(errors)}`);
  assert.ok(
    missingLine.includes("file_not_found"),
    `expected reason=file_not_found; got: ${missingLine}`,
  );
});
