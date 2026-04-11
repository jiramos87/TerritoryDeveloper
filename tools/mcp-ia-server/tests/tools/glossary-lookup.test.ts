/**
 * glossary_lookup — graph extensions (appears_in_code cache + scanner).
 */

import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import os from "node:os";
import {
  clearGlossaryGraphCaches,
  scanCodeAppearances,
} from "../../src/tools/glossary-lookup.js";

/**
 * Scaffold a fake repo with a single `Assets/Scripts/Thing.cs` file. Returns
 * the temp dir path so the test can use it as `repoRoot`.
 */
function withFakeRepo(
  csContent: string,
  body: (repoRoot: string) => void,
): void {
  const repoRoot = fs.mkdtempSync(path.join(os.tmpdir(), "glossary-scan-"));
  const scriptsDir = path.join(repoRoot, "Assets", "Scripts");
  fs.mkdirSync(scriptsDir, { recursive: true });
  fs.writeFileSync(
    path.join(scriptsDir, "Thing.cs"),
    csContent,
    "utf8",
  );
  try {
    clearGlossaryGraphCaches();
    body(repoRoot);
  } finally {
    fs.rmSync(repoRoot, { recursive: true, force: true });
  }
}

test("scanCodeAppearances finds term mentions in .cs files", () => {
  withFakeRepo(
    `
public class Thing
{
    // references the HeightMap field
    int[,] HeightMap;
    void Use() { var h = HeightMap[0, 0]; }
}
`,
    (repoRoot) => {
      const hits = scanCodeAppearances("HeightMap", repoRoot);
      assert.ok(hits.length >= 2, "expected at least 2 hits");
      for (const hit of hits) {
        assert.ok(hit.file.endsWith("Thing.cs"));
        assert.ok(hit.line > 0);
      }
    },
  );
});

test("scanCodeAppearances returns empty when term absent", () => {
  withFakeRepo(
    `
public class Thing
{
    void Noop() { }
}
`,
    (repoRoot) => {
      const hits = scanCodeAppearances("UrbanCentroid", repoRoot);
      assert.equal(hits.length, 0);
    },
  );
});

test("scanCodeAppearances caches per-term results across calls", () => {
  withFakeRepo(
    `
public class Thing
{
    int HeightMap;
}
`,
    (repoRoot) => {
      const first = scanCodeAppearances("HeightMap", repoRoot);
      // Delete the file; a fresh scan would now return empty, but the cache
      // should still give us the original answer.
      const scriptPath = path.join(repoRoot, "Assets/Scripts/Thing.cs");
      fs.rmSync(scriptPath);
      const second = scanCodeAppearances("HeightMap", repoRoot);
      assert.deepEqual(second, first);
    },
  );
});

test("scanCodeAppearances handles missing Assets/Scripts gracefully", () => {
  const repoRoot = fs.mkdtempSync(path.join(os.tmpdir(), "glossary-empty-"));
  try {
    clearGlossaryGraphCaches();
    const hits = scanCodeAppearances("Anything", repoRoot);
    assert.equal(hits.length, 0);
  } finally {
    fs.rmSync(repoRoot, { recursive: true, force: true });
  }
});
