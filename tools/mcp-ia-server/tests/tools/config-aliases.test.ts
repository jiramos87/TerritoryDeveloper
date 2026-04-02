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
    filePath: "/repo/.cursor/specs/isometric-geography-system.md",
    description: "",
    category: "spec",
  },
  {
    key: "roads-system",
    fileName: "roads-system.md",
    filePath: "/repo/.cursor/specs/roads-system.md",
    description: "",
    category: "spec",
  },
  {
    key: "roads",
    fileName: "roads.mdc",
    filePath: "/repo/.cursor/rules/roads.mdc",
    description: "Roads rule",
    category: "rule",
  },
];

test("resolveSpecKeyAlias maps geo and roads spec", () => {
  assert.equal(resolveSpecKeyAlias("geo"), "isometric-geography-system");
  assert.equal(resolveSpecKeyAlias("roads"), "roads-system");
});

test("findEntryForSpecDoc uses roads alias for spec not rule", () => {
  const e = findEntryForSpecDoc(mockRegistry, "roads");
  assert.equal(e?.key, "roads-system");
});

test("findRuleEntry resolves roads.mdc", () => {
  const e = findRuleEntry(mockRegistry, "roads");
  assert.equal(e?.key, "roads");
  assert.equal(e?.category, "rule");
});
