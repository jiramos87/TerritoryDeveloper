import test from "node:test";
import assert from "node:assert/strict";
import { formatRuleGlobs } from "../../src/tools/list-rules.js";

test("formatRuleGlobs null and string", () => {
  assert.equal(formatRuleGlobs(null), null);
  assert.equal(formatRuleGlobs("**/*.cs"), "**/*.cs");
});

test("formatRuleGlobs array", () => {
  assert.equal(formatRuleGlobs(["a", "b"]), "a, b");
});
