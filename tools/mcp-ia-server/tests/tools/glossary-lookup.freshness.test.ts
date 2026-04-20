/**
 * TECH-515 — Freshness metadata tests for glossary_lookup path.
 *
 * Tests exercise getGraphFreshness() + spawnGraphRegen() from the freshness
 * helper directly, mocking fs.promises.stat and child_process.spawn via
 * node:test mock.method(). All 8 lookup-side Blueprint rows covered here.
 */

import { describe, it, beforeEach, afterEach, mock } from "node:test";
import assert from "node:assert/strict";
import { promises as fsPromises } from "node:fs";
import {
  getGraphFreshness,
  spawnGraphRegen,
  clearFreshnessCache,
  _setSpawnFn,
} from "../../src/tools/glossary-freshness.js";

const DAY_MS = 86_400_000;

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function makeFakeStat(mtime: Date) {
  return { mtime } as unknown as Awaited<ReturnType<typeof fsPromises.stat>>;
}

function makeFakeChild() {
  return { unref: mock.fn() };
}

// ---------------------------------------------------------------------------
// Suite
// ---------------------------------------------------------------------------

describe("glossary_lookup freshness metadata", () => {
  let envSnapshot: string | undefined;
  let statMock: ReturnType<typeof mock.method<typeof fsPromises, "stat">>;

  beforeEach(() => {
    // Reset process-lifetime cache so each test gets a fresh stat call.
    clearFreshnessCache();
    // Snapshot + clear env override.
    envSnapshot = process.env.GLOSSARY_GRAPH_STALE_DAYS;
    delete process.env.GLOSSARY_GRAPH_STALE_DAYS;
    // Restore spawn to real impl at start of each test.
    _setSpawnFn(null);
  });

  afterEach(() => {
    // Restore env.
    if (envSnapshot === undefined) {
      delete process.env.GLOSSARY_GRAPH_STALE_DAYS;
    } else {
      process.env.GLOSSARY_GRAPH_STALE_DAYS = envSnapshot;
    }
    mock.restoreAll();
    // Always restore spawn.
    _setSpawnFn(null);
  });

  // -------------------------------------------------------------------------
  // lookup_fresh_default_env: mtime = now - 1d → graph_stale false
  // -------------------------------------------------------------------------
  it("lookup_fresh_default_env: mtime 1d old → graph_stale false", async () => {
    const mtime = new Date(Date.now() - DAY_MS);
    statMock = mock.method(fsPromises, "stat", async () => makeFakeStat(mtime));

    const result = await getGraphFreshness();

    assert.equal(result.graph_stale, false);
    assert.ok(result.graph_generated_at, "graph_generated_at should be set");
    assert.equal(statMock.mock.calls.length, 1, "stat called once");
  });

  // -------------------------------------------------------------------------
  // lookup_stale_default_env: mtime = now - 15d → graph_stale true
  // -------------------------------------------------------------------------
  it("lookup_stale_default_env: mtime 15d old → graph_stale true", async () => {
    const mtime = new Date(Date.now() - 15 * DAY_MS);
    statMock = mock.method(fsPromises, "stat", async () => makeFakeStat(mtime));

    const result = await getGraphFreshness();

    assert.equal(result.graph_stale, true);
    assert.ok(result.graph_generated_at);
  });

  // -------------------------------------------------------------------------
  // lookup_env_override_stale: GLOSSARY_GRAPH_STALE_DAYS=1, mtime 2d old → stale
  // -------------------------------------------------------------------------
  it("lookup_env_override_stale: GLOSSARY_GRAPH_STALE_DAYS=1, 2d old → stale", async () => {
    process.env.GLOSSARY_GRAPH_STALE_DAYS = "1";
    const mtime = new Date(Date.now() - 2 * DAY_MS);
    statMock = mock.method(fsPromises, "stat", async () => makeFakeStat(mtime));

    const result = await getGraphFreshness();

    assert.equal(result.graph_stale, true);
  });

  // -------------------------------------------------------------------------
  // lookup_env_override_fresh: GLOSSARY_GRAPH_STALE_DAYS=60, mtime 30d old → fresh
  // -------------------------------------------------------------------------
  it("lookup_env_override_fresh: GLOSSARY_GRAPH_STALE_DAYS=60, 30d old → fresh", async () => {
    process.env.GLOSSARY_GRAPH_STALE_DAYS = "60";
    const mtime = new Date(Date.now() - 30 * DAY_MS);
    statMock = mock.method(fsPromises, "stat", async () => makeFakeStat(mtime));

    const result = await getGraphFreshness();

    assert.equal(result.graph_stale, false);
  });

  // -------------------------------------------------------------------------
  // env_nan_falls_back_to_14: GLOSSARY_GRAPH_STALE_DAYS=abc, 10d old → fresh (10 < 14)
  // -------------------------------------------------------------------------
  it("env_nan_falls_back_to_14: GLOSSARY_GRAPH_STALE_DAYS=abc, 10d old → fresh", async () => {
    process.env.GLOSSARY_GRAPH_STALE_DAYS = "abc";
    const mtime = new Date(Date.now() - 10 * DAY_MS);
    statMock = mock.method(fsPromises, "stat", async () => makeFakeStat(mtime));

    const result = await getGraphFreshness();

    // 10d < 14d default → should NOT be stale
    assert.equal(result.graph_stale, false);
  });

  // -------------------------------------------------------------------------
  // lookup_iso_round_trip: graph_generated_at is valid ISO 8601
  // -------------------------------------------------------------------------
  it("lookup_iso_round_trip: graph_generated_at round-trips as canonical ISO 8601", async () => {
    const mtime = new Date(Date.now() - DAY_MS);
    statMock = mock.method(fsPromises, "stat", async () => makeFakeStat(mtime));

    const result = await getGraphFreshness();

    assert.ok(result.graph_generated_at, "graph_generated_at should be defined");
    const iso = result.graph_generated_at!;
    assert.equal(
      new Date(iso).toISOString(),
      iso,
      "graph_generated_at must be canonical ISO 8601",
    );
  });

  // -------------------------------------------------------------------------
  // lookup_missing_graph_index: stat rejects ENOENT → stale, no generated_at, no throw
  // -------------------------------------------------------------------------
  it("lookup_missing_graph_index: ENOENT → graph_stale true, no generated_at, no throw", async () => {
    const enoent = Object.assign(new Error("ENOENT"), { code: "ENOENT" });
    statMock = mock.method(fsPromises, "stat", async () => { throw enoent; });

    const result = await getGraphFreshness();

    assert.equal(result.graph_stale, true);
    assert.equal(result.graph_generated_at, undefined);
  });

  // -------------------------------------------------------------------------
  // lookup_refresh_graph_spawns: spawnGraphRegen() → spawn called with detached args + unref
  // -------------------------------------------------------------------------
  it("lookup_refresh_graph_spawns: spawnGraphRegen calls spawn detached + unref", () => {
    const fakeChild = makeFakeChild();
    let spawnCallCount = 0;
    let lastArgs: { cmd: string; argv: string[]; opts: Record<string, unknown> } | undefined;
    _setSpawnFn((cmd, argv, opts) => {
      spawnCallCount++;
      lastArgs = { cmd, argv, opts: opts as Record<string, unknown> };
      return fakeChild;
    });

    spawnGraphRegen();

    assert.equal(spawnCallCount, 1, "spawn called once");
    assert.equal(lastArgs!.cmd, "npm");
    assert.deepEqual(lastArgs!.argv, ["run", "build:glossary-graph"]);
    assert.equal(lastArgs!.opts["detached"], true);
    assert.equal(lastArgs!.opts["stdio"], "ignore");
    assert.ok(
      typeof lastArgs!.opts["cwd"] === "string" && (lastArgs!.opts["cwd"] as string).length > 0,
      "cwd set",
    );
    // unref must be called
    const unrefFn = fakeChild.unref as ReturnType<typeof mock.fn>;
    assert.equal(unrefFn.mock.calls.length, 1, "unref called once");
  });

  // -------------------------------------------------------------------------
  // lookup_refresh_graph_non_blocking: spawnGraphRegen returns without awaiting child
  // -------------------------------------------------------------------------
  it("lookup_refresh_graph_non_blocking: spawnGraphRegen resolves without awaiting child exit", () => {
    const fakeChild = makeFakeChild();
    let spawnCallCount = 0;
    _setSpawnFn((_cmd, _argv, _opts) => {
      spawnCallCount++;
      return fakeChild;
    });

    const start = Date.now();
    spawnGraphRegen(); // synchronous — void, no await
    const elapsed = Date.now() - start;

    // Should return essentially instantly (synchronous path)
    assert.ok(elapsed < 100, `Expected < 100ms, got ${elapsed}ms`);
    assert.equal(spawnCallCount, 1);
  });
});
