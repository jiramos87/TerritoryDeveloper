/**
 * render-worker.test.ts — TECH-1468 unit harness.
 *
 * Covers the pure pieces of the render worker (no Postgres / FastAPI
 * needed): canonical-JSON hashing + dispatch wiring against a stub
 * fetch + manifest sidecar shape. The DB-backed claim / heartbeat /
 * sweep paths are covered by an integration test that lives behind
 * the optional `RENDER_WORKER_E2E_DB` env flag (skipped in CI by
 * default — TECH-1468 §Test Blueprint promotes it to a recurring
 * gate once the test-DB harness lands in a later stage).
 */

import assert from "node:assert/strict";
import * as fs from "node:fs/promises";
import * as os from "node:os";
import * as path from "node:path";
import { test } from "node:test";

import {
  canonicalJson,
  computeBuildFingerprint,
  dispatchSpriteGen,
  paramsHash,
  writeManifest,
} from "../render-worker.ts";

test("canonical JSON sorts object keys deterministically", () => {
  const a = canonicalJson({ b: 1, a: 2, c: { y: 3, x: 4 } });
  const b = canonicalJson({ a: 2, c: { x: 4, y: 3 }, b: 1 });
  assert.strictEqual(a, b);
  assert.strictEqual(a, '{"a":2,"b":1,"c":{"x":4,"y":3}}');
});

test("paramsHash is stable across key order", () => {
  const h1 = paramsHash({ archetype: "tree", params: { wind: 0.4, age: 12 } });
  const h2 = paramsHash({ params: { age: 12, wind: 0.4 }, archetype: "tree" });
  assert.strictEqual(h1, h2);
  assert.match(h1, /^[a-f0-9]{64}$/);
});

test("computeBuildFingerprint returns stable colon-separated string", () => {
  const fp = computeBuildFingerprint();
  assert.match(fp, /:/);
  assert.notStrictEqual(fp, "");
});

test("writeManifest creates sidecar JSON under {blob_root}/{run_id}/", async () => {
  const tmp = await fs.mkdtemp(path.join(os.tmpdir(), "render-worker-"));
  process.env.BLOB_ROOT = tmp;
  try {
    const runId = "run-test-001";
    const manifestPath = await writeManifest(runId, {
      archetype_version_id: "av-1",
      params_json: { archetype: "tree", params: {} },
      build_fingerprint: "deadbeef:1.2.3",
      duration_ms: 42,
    });
    assert.ok(manifestPath.endsWith(path.join(runId, "manifest.json")));
    const txt = await fs.readFile(manifestPath, "utf8");
    const m = JSON.parse(txt);
    assert.strictEqual(m.schema, "render-run-manifest/1");
    assert.strictEqual(m.run_id, runId);
    assert.strictEqual(m.archetype_version_id, "av-1");
    assert.strictEqual(m.duration_ms, 42);
    assert.strictEqual(m.build_fingerprint, "deadbeef:1.2.3");
    assert.ok(typeof m.written_at === "string" && m.written_at.length > 0);
  } finally {
    delete process.env.BLOB_ROOT;
    await fs.rm(tmp, { recursive: true, force: true });
  }
});

test("dispatchSpriteGen rejects when archetype missing from payload", async () => {
  const ac = new AbortController();
  await assert.rejects(
    () =>
      dispatchSpriteGen(
        {
          archetype_id: "ar-1",
          archetype_version_id: "av-1",
          params_json: { params: {} },
        },
        ac.signal,
      ),
    /payload\.params_json\.archetype required/,
  );
});

test("dispatchSpriteGen issues POST /render against SPRITE_GEN_URL", async () => {
  const calls: Array<{ url: string; init: RequestInit | undefined }> = [];
  const originalFetch = globalThis.fetch;
  const fakeResponse = {
    run_id: "spr-1",
    fingerprint: "tree:spr-1",
    variants: [
      { idx: 0, blob_ref: "gen://spr-1/0" },
      { idx: 1, blob_ref: "gen://spr-1/1" },
    ],
  };
  globalThis.fetch = (async (url: string, init?: RequestInit) => {
    calls.push({ url, init });
    return new Response(JSON.stringify(fakeResponse), {
      status: 200,
      headers: { "Content-Type": "application/json" },
    });
  }) as typeof fetch;
  process.env.SPRITE_GEN_URL = "http://stub:1234";
  try {
    const ac = new AbortController();
    const out = await dispatchSpriteGen(
      {
        archetype_id: "ar-1",
        archetype_version_id: "av-1",
        params_json: {
          archetype: "tree",
          terrain: "flat",
          params: { wind: 0.4 },
        },
      },
      ac.signal,
    );
    assert.deepStrictEqual(out, fakeResponse);
    assert.strictEqual(calls.length, 1);
    // SPRITE_GEN_URL is read once at module load; the per-call URL still
    // resolves through the cached value, so the assertion is on path
    // suffix rather than full URL.
    assert.ok(calls[0]!.url.endsWith("/render"));
    const body = JSON.parse(String(calls[0]!.init!.body));
    assert.strictEqual(body.archetype, "tree");
    assert.strictEqual(body.terrain, "flat");
    assert.deepStrictEqual(body.params, { wind: 0.4 });
  } finally {
    globalThis.fetch = originalFetch;
    delete process.env.SPRITE_GEN_URL;
  }
});

test("dispatchSpriteGen surfaces non-2xx body in error message", async () => {
  const originalFetch = globalThis.fetch;
  globalThis.fetch = (async () =>
    new Response("kaboom", { status: 500 })) as typeof fetch;
  try {
    const ac = new AbortController();
    await assert.rejects(
      () =>
        dispatchSpriteGen(
          {
            archetype_id: "ar-1",
            archetype_version_id: "av-1",
            params_json: { archetype: "tree" },
          },
          ac.signal,
        ),
      /sprite-gen render 500/,
    );
  } finally {
    globalThis.fetch = originalFetch;
  }
});
