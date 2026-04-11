/**
 * unity_subscribers_of — regex scan for `event += handler` subscription sites.
 */

import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import os from "node:os";
import { scanFileForSubscribers } from "../../src/tools/unity-subscribers-of.js";

function writeTempCs(content: string): { filePath: string; dir: string } {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), "subs-test-"));
  const filePath = path.join(dir, "TestScript.cs");
  fs.writeFileSync(filePath, content, "utf8");
  return { filePath, dir };
}

test("detects subscription via `event += handler`", () => {
  const { filePath, dir } = writeTempCs(`
public class MiniMapController
{
    GridManager gridManager;
    void Start()
    {
        gridManager.onGridRestored += OnGridRestored;
    }
    void OnGridRestored() {}
}
`);
  const hits = scanFileForSubscribers(filePath, dir, "onGridRestored");
  assert.equal(hits.length, 1);
  assert.equal(hits[0]!.className, "MiniMapController");
  assert.equal(hits[0]!.method, "Start");
  assert.equal(hits[0]!.handler, "OnGridRestored");
  fs.rmSync(dir, { recursive: true });
});

test("detects handler expression with dotted receiver", () => {
  const { filePath, dir } = writeTempCs(`
public class Editor
{
    void Init()
    {
        EditorApplication.update += this.OnEditorUpdate;
    }
}
`);
  const hits = scanFileForSubscribers(filePath, dir, "update");
  assert.equal(hits.length, 1);
  assert.equal(hits[0]!.className, "Editor");
  assert.equal(hits[0]!.method, "Init");
  // handler includes any dotted expression characters.
  assert.ok(hits[0]!.handler.length > 0);
  fs.rmSync(dir, { recursive: true });
});

test("skips commented-out subscriptions", () => {
  const { filePath, dir } = writeTempCs(`
public class Foo
{
    void Start()
    {
        // gridManager.onGridRestored += OnGridRestored;
        gridManager.onGridRestored += Real;
    }
    void Real() {}
}
`);
  const hits = scanFileForSubscribers(filePath, dir, "onGridRestored");
  assert.equal(hits.length, 1);
  assert.equal(hits[0]!.handler, "Real");
  fs.rmSync(dir, { recursive: true });
});

test("returns empty when event name absent", () => {
  const { filePath, dir } = writeTempCs(`
public class None
{
    void Y() {}
}
`);
  const hits = scanFileForSubscribers(filePath, dir, "onGridRestored");
  assert.equal(hits.length, 0);
  fs.rmSync(dir, { recursive: true });
});

test("detects multiple subscribers across methods", () => {
  const { filePath, dir } = writeTempCs(`
public class MultiSub
{
    void A()
    {
        mgr.tick += HandlerA;
    }
    void B()
    {
        mgr.tick += HandlerB;
    }
}
`);
  const hits = scanFileForSubscribers(filePath, dir, "tick");
  assert.equal(hits.length, 2);
  assert.equal(hits[0]!.method, "A");
  assert.equal(hits[0]!.handler, "HandlerA");
  assert.equal(hits[1]!.method, "B");
  assert.equal(hits[1]!.handler, "HandlerB");
  fs.rmSync(dir, { recursive: true });
});
