import test from "node:test";
import assert from "node:assert/strict";
import type { SpecRegistryEntry } from "../../src/parser/types.js";
import {
  resolveSpecKeyAlias,
  findEntryForSpecDoc,
  findRuleEntry,
} from "../../src/config.js";

const mockRegistry: SpecRegistryEntry[] = [
  {
    key: "isometric-geography-system",
    fileName: "isometric-geography-system.md",
    filePath: "/repo/ia/specs/isometric-geography-system.md",
    description: "",
    category: "spec",
  },
  {
    key: "roads-system",
    fileName: "roads-system.md",
    filePath: "/repo/ia/specs/roads-system.md",
    description: "",
    category: "spec",
  },
  {
    key: "roads",
    fileName: "roads.md",
    filePath: "/repo/ia/rules/roads.md",
    description: "Roads rule",
    category: "rule",
  },
];

test("resolveSpecKeyAlias maps geo and roads spec", () => {
  assert.equal(resolveSpecKeyAlias("geo"), "isometric-geography-system");
  assert.equal(resolveSpecKeyAlias("roads"), "roads-system");
});

test("resolveSpecKeyAlias maps refspec aliases to reference-spec-structure", () => {
  assert.equal(resolveSpecKeyAlias("refspec"), "reference-spec-structure");
  assert.equal(resolveSpecKeyAlias("specstructure"), "reference-spec-structure");
});

test("resolveSpecKeyAlias maps unity aliases to unity-development-context", () => {
  assert.equal(resolveSpecKeyAlias("unity"), "unity-development-context");
  assert.equal(resolveSpecKeyAlias("unityctx"), "unity-development-context");
});

test("findEntryForSpecDoc uses roads alias for spec not rule", () => {
  const e = findEntryForSpecDoc(mockRegistry, "roads");
  assert.equal(e?.key, "roads-system");
});

test("findRuleEntry resolves roads rule", () => {
  const e = findRuleEntry(mockRegistry, "roads");
  assert.equal(e?.key, "roads");
  assert.equal(e?.category, "rule");
});
