/**
 * glossary_discover: spec reference → registry key / alias (pure helper).
 */

import assert from "node:assert/strict";
import { describe, it } from "node:test";
import type { SpecRegistryEntry } from "../../src/parser/types.js";
import { resolveSpecKeyFromReference } from "../../src/tools/glossary-discover.js";

function fakeRegistry(): SpecRegistryEntry[] {
  return [
    {
      key: "roads-system",
      fileName: "roads-system.md",
      filePath: "/x/.cursor/specs/roads-system.md",
      description: "",
      category: "spec",
    },
    {
      key: "roads",
      fileName: "roads.mdc",
      filePath: "/x/.cursor/rules/roads.mdc",
      description: "",
      category: "rule",
    },
  ];
}

describe("resolveSpecKeyFromReference", () => {
  it("resolves first spec document token, not a rule with overlapping name", () => {
    const r = resolveSpecKeyFromReference(
      "roads-system §3; see also roads.mdc",
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
        filePath: "/x/.cursor/specs/isometric-geography-system.md",
        description: "",
        category: "spec",
      },
    ]);
    assert.equal(r, undefined);
  });
});
