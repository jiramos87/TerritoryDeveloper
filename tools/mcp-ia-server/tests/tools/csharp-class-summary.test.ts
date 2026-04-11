/**
 * csharp_class_summary — regex-based class structural summary.
 */

import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import os from "node:os";
import { summarizeClassInFile } from "../../src/tools/csharp-class-summary.js";

function writeTempCs(content: string): { filePath: string; dir: string } {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), "class-summary-test-"));
  const filePath = path.join(dir, "Thing.cs");
  fs.writeFileSync(filePath, content, "utf8");
  return { filePath, dir };
}

test("returns null for class not declared in the file", () => {
  const { filePath, dir } = writeTempCs(`
public class Other { }
`);
  const summary = summarizeClassInFile(filePath, dir, "RoadResolver");
  assert.equal(summary, null);
  fs.rmSync(dir, { recursive: true });
});

test("extracts public methods, fields, base types, and xml doc", () => {
  const { filePath, dir } = writeTempCs(`
using UnityEngine;
using System.Collections.Generic;

namespace Territory.Roads
{
    /// <summary>
    /// Resolves the road prefab for a given grid cell based on neighbour connectivity.
    /// </summary>
    public class RoadResolver : MonoBehaviour, IRoadResolver
    {
        public int fallbackIndex = 0;
        private GridManager gridManager;

        public bool ResolveAt(int x, int y)
        {
            return true;
        }

        public void Clear()
        {
        }

        private void Helper() { }
    }
}
`);
  const summary = summarizeClassInFile(filePath, dir, "RoadResolver");
  assert.ok(summary, "summary should be returned");
  assert.equal(summary!.class_name, "RoadResolver");
  assert.deepEqual(summary!.base_types, ["MonoBehaviour", "IRoadResolver"]);
  assert.ok(
    summary!.public_methods.some((m) => m.name === "ResolveAt"),
    "should expose ResolveAt",
  );
  assert.ok(
    summary!.public_methods.some((m) => m.name === "Clear"),
    "should expose Clear",
  );
  assert.ok(
    !summary!.public_methods.some((m) => m.name === "Helper"),
    "should not expose private Helper",
  );
  assert.ok(
    summary!.fields.some((f) => f.name === "fallbackIndex"),
    "should expose fallbackIndex",
  );
  assert.ok(
    summary!.fields.some((f) => f.name === "gridManager"),
    "should expose gridManager",
  );
  assert.deepEqual(summary!.dependencies, [
    "UnityEngine",
    "System.Collections.Generic",
  ]);
  assert.ok(
    summary!.brief_xml_doc.includes("Resolves the road prefab"),
    "should pull the <summary> block",
  );
  fs.rmSync(dir, { recursive: true });
});

test("handles class without base types", () => {
  const { filePath, dir } = writeTempCs(`
public class Plain
{
    public int Count() { return 0; }
}
`);
  const summary = summarizeClassInFile(filePath, dir, "Plain");
  assert.ok(summary);
  assert.deepEqual(summary!.base_types, []);
  assert.equal(summary!.public_methods.length, 1);
  assert.equal(summary!.public_methods[0]!.name, "Count");
  fs.rmSync(dir, { recursive: true });
});

test("handles missing xml summary gracefully", () => {
  const { filePath, dir } = writeTempCs(`
public class NoDoc
{
    public void Run() { }
}
`);
  const summary = summarizeClassInFile(filePath, dir, "NoDoc");
  assert.ok(summary);
  assert.equal(summary!.brief_xml_doc, "");
  fs.rmSync(dir, { recursive: true });
});
