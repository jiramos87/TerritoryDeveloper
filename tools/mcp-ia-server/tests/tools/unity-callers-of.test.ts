/**
 * unity_callers_of — regex scan for method call sites.
 */

import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import os from "node:os";
import { scanFileForCallers } from "../../src/tools/unity-callers-of.js";

function writeTempCs(content: string): { filePath: string; dir: string } {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), "callers-test-"));
  const filePath = path.join(dir, "TestScript.cs");
  fs.writeFileSync(filePath, content, "utf8");
  return { filePath, dir };
}

test("detects unqualified call site", () => {
  const { filePath, dir } = writeTempCs(`
public class Caller
{
    void DoWork()
    {
        ResolveAt(5, 10);
    }
}
`);
  const hits = scanFileForCallers(filePath, dir, "ResolveAt");
  assert.equal(hits.length, 1);
  assert.ok(hits[0]!.snippet.includes("ResolveAt"));
  assert.equal(hits[0]!.line, 6);
  fs.rmSync(dir, { recursive: true });
});

test("excludes declaration line", () => {
  const { filePath, dir } = writeTempCs(`
public class RoadResolver
{
    public bool ResolveAt(int x, int y)
    {
        return true;
    }
    void Caller()
    {
        ResolveAt(1, 2);
    }
}
`);
  const hits = scanFileForCallers(filePath, dir, "ResolveAt");
  assert.equal(hits.length, 1);
  assert.equal(hits[0]!.line, 10);
  fs.rmSync(dir, { recursive: true });
});

test("class filter narrows matches to ClassName.Method only", () => {
  const { filePath, dir } = writeTempCs(`
public class Caller
{
    RoadResolver r;
    OtherThing o;
    void X()
    {
        RoadResolver.ResolveAt(1, 2);
        o.ResolveAt(3, 4);
    }
}
`);
  const hits = scanFileForCallers(filePath, dir, "ResolveAt", "RoadResolver");
  assert.equal(hits.length, 1);
  assert.ok(hits[0]!.snippet.includes("RoadResolver.ResolveAt"));
  fs.rmSync(dir, { recursive: true });
});

test("skips single-line comments", () => {
  const { filePath, dir } = writeTempCs(`
public class X
{
    void Y()
    {
        // ResolveAt(1, 2);
        ResolveAt(3, 4);
    }
}
`);
  const hits = scanFileForCallers(filePath, dir, "ResolveAt");
  assert.equal(hits.length, 1);
  assert.equal(hits[0]!.line, 7);
  fs.rmSync(dir, { recursive: true });
});

test("returns empty array when method name absent", () => {
  const { filePath, dir } = writeTempCs(`
public class Empty
{
    void Noop() {}
}
`);
  const hits = scanFileForCallers(filePath, dir, "ResolveAt");
  assert.equal(hits.length, 0);
  fs.rmSync(dir, { recursive: true });
});

test("detects multiple call sites in one file", () => {
  const { filePath, dir } = writeTempCs(`
public class Caller
{
    void A() { ResolveAt(1, 2); }
    void B() { ResolveAt(3, 4); ResolveAt(5, 6); }
}
`);
  const hits = scanFileForCallers(filePath, dir, "ResolveAt");
  assert.equal(hits.length, 2); // one per line
  fs.rmSync(dir, { recursive: true });
});
