/**
 * TECH-515 — Freshness metadata tests for glossary_discover path.
 *
 * Mirrors the lookup freshness suite; tests the same getGraphFreshness() +
 * spawnGraphRegen() helpers, which are also called by the discover handler.
 * Covers 5 discover-side Blueprint rows: fresh/stale default env, refresh_graph
 * spawn detached, env NaN fallback, ISO round-trip.
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

function makeFakeStat(mtime: Date) {
  return { mtime } as unknown as Awaited<ReturnType<typeof fsPromises.stat>>;
}

function makeFakeChild() {
  return { unref: mock.fn() };
}

describe("glossary_discover freshness metadata", () => {
  let envSnapshot: string | undefined;
  let statMock: ReturnType<typeof mock.method<typeof fsPromises, "stat">>;

  beforeEach(() => {
    clearFreshnessCache();
    envSnapshot = process.env.GLOSSARY_GRAPH_STALE_DAYS;
    delete process.env.GLOSSARY_GRAPH_STALE_DAYS;
    _setSpawnFn(null);
  });

  afterEach(() => {
    if (envSnapshot === undefined) {
      delete process.env.GLOSSARY_GRAPH_STALE_DAYS;
    } else {
      process.env.GLOSSARY_GRAPH_STALE_DAYS = envSnapshot;
    }
    mock.restoreAll();
    _setSpawnFn(null);
  });

  // -------------------------------------------------------------------------
  // discover_fresh_default_env: mtime = now - 1d -> graph_stale false
  // -------------------------------------------------------------------------
  it("discover_fresh_default_env: mtime 1d old -> graph_stale false", async () => {
    const mtime = new Date(Date.now() - DAY_MS);
    statMock = mock.method(fsPromises, "stat", async () => makeFakeStat(mtime));

    const result = await getGraphFreshness();

    assert.equal(result.graph_stale, false);
    assert.ok(result.graph_generated_at, "graph_generated_at present");
    assert.equal(statMock.mock.calls.length, 1);
  });

  // -------------------------------------------------------------------------
  // discover_stale_default_env: mtime = now - 15d -> graph_stale true
  // -------------------------------------------------------------------------
  it("discover_stale_default_env: mtime 15d old -> graph_stale true", async () => {
    const mtime = new Date(Date.now() - 15 * DAY_MS);
    statMock = mock.method(fsPromises, "stat", async () => makeFakeStat(mtime));

    const result = await getGraphFreshness();

    assert.equal(result.graph_stale, true);
  });

  // -------------------------------------------------------------------------
  // discover_refresh_graph_spawns: spawn detached + unref asserted
  // -------------------------------------------------------------------------
  it("discover_refresh_graph_spawns: spawnGraphRegen detached spawn + unref", () => {
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
    );
    const unrefFn = fakeChild.unref as ReturnType<typeof mock.fn>;
    assert.equal(unrefFn.mock.calls.length, 1, "unref called once");
  });

  // -------------------------------------------------------------------------
  // env_nan_falls_back_to_14 (discover path): shared helper -> same 14d default
  // -------------------------------------------------------------------------
  it("env_nan_falls_back_to_14 (discover): GLOSSARY_GRAPH_STALE_DAYS=abc, 10d old -> fresh", async () => {
    process.env.GLOSSARY_GRAPH_STALE_DAYS = "abc";
    const mtime = new Date(Date.now() - 10 * DAY_MS);
    statMock = mock.method(fsPromises, "stat", async () => makeFakeStat(mtime));

    const result = await getGraphFreshness();

    assert.equal(result.graph_stale, false, "10d < 14d fallback -> not stale");
  });

  // -------------------------------------------------------------------------
  // ISO round-trip (discover path)
  // -------------------------------------------------------------------------
  it("discover_iso_round_trip: graph_generated_at is canonical ISO 8601", async () => {
    const mtime = new Date(Date.now() - DAY_MS);
    statMock = mock.method(fsPromises, "stat", async () => makeFakeStat(mtime));

    const result = await getGraphFreshness();

    assert.ok(result.graph_generated_at);
    const iso = result.graph_generated_at!;
    assert.equal(new Date(iso).toISOString(), iso);
  });
});
