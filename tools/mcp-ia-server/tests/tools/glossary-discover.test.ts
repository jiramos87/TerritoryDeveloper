/**
 * glossary_discover: spec reference → registry key / alias (pure helper) +
 * envelope wrap assertions (TECH-400).
 */

import assert from "node:assert/strict";
import { describe, it, test } from "node:test";
import type { SpecRegistryEntry } from "../../src/parser/types.js";
import { resolveSpecKeyFromReference } from "../../src/tools/glossary-discover.js";
import { wrapTool } from "../../src/envelope.js";

function fakeRegistry(): SpecRegistryEntry[] {
  return [
    {
      key: "roads-system",
      fileName: "roads-system.md",
      filePath: "/x/ia/specs/roads-system.md",
      description: "",
      category: "spec",
    },
    {
      key: "roads",
      fileName: "roads.md",
      filePath: "/x/ia/rules/roads.md",
      description: "",
      category: "rule",
    },
  ];
}

describe("resolveSpecKeyFromReference", () => {
  it("resolves first spec document token, not a rule with overlapping name", () => {
    const r = resolveSpecKeyFromReference(
      "roads-system §3; see also roads.md",
      fakeRegistry(),
    );
    assert.ok(r);
    assert.equal(r!.registryKey, "roads-system");
    assert.equal(r!.spec, "roads");
  });

  it("skips pure numeric tokens", () => {
    const r = resolveSpecKeyFromReference("§14.5 only numbers 3.2", [
      {
        key: "isometric-geography-system",
        fileName: "isometric-geography-system.md",
        filePath: "/x/ia/specs/isometric-geography-system.md",
        description: "",
        category: "spec",
      },
    ]);
    assert.equal(r, undefined);
  });
});

// ---------------------------------------------------------------------------
// TECH-400: envelope wrap shape assertions for glossary_discover handler path.
// Tests exercise wrapTool directly with inline handler closures that mirror
// what the handler does — avoids MCP server spin-up while covering envelope contract.
// ---------------------------------------------------------------------------

test("envelope — discover hit → { ok:true, payload } with matches array", async () => {
  const envelope = await wrapTool(async () => ({
    matches: [{ term: "HeightMap", score: 0.9 }],
    query_normalized: "heightmap",
    hint_next_tools: "...",
  }))({});
  assert.equal(envelope.ok, true);
  if (envelope.ok) {
    const p = envelope.payload as Record<string, unknown>;
    assert.ok(Array.isArray(p.matches), "payload.matches is array");
    assert.equal((p.matches as unknown[])[0] != null && typeof (p.matches as unknown[])[0] === "object"
      ? ((p.matches as unknown[])[0] as Record<string, unknown>).term
      : undefined, "HeightMap");
  }
});

test("envelope — discover empty-match → { ok:true, payload } with empty matches", async () => {
  const envelope = await wrapTool(async () => ({
    matches: [] as unknown[],
    query_normalized: "xyznonexistent",
    hint_next_tools: "...",
    message: "No glossary rows matched.",
    suggestions: [] as string[],
  }))({});
  assert.equal(envelope.ok, true);
  if (envelope.ok) {
    const p = envelope.payload as Record<string, unknown>;
    assert.deepEqual(p.matches, []);
  }
});

test("envelope — discover invalid_input → { ok:false, error:{ code:'invalid_input' } }", async () => {
  const envelope = await wrapTool(async () => {
    throw {
      code: "invalid_input" as const,
      message: "Provide non-empty `query` and/or `keywords`.",
    };
  })({});
  assert.equal(envelope.ok, false);
  if (!envelope.ok) {
    assert.equal(envelope.error.code, "invalid_input");
  }
});

test("envelope — discover glossary_missing → { ok:false, error:{ code:'internal_error' } }", async () => {
  const envelope = await wrapTool(async () => {
    throw {
      code: "internal_error" as const,
      message: "Glossary file is not registered.",
    };
  })({});
  assert.equal(envelope.ok, false);
  if (!envelope.ok) {
    assert.equal(envelope.error.code, "internal_error");
  }
});
