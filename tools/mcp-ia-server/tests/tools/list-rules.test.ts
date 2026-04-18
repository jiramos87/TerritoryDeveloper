import test from "node:test";
import assert from "node:assert/strict";
import { formatRuleGlobs } from "../../src/tools/list-rules.js";
import { wrapTool } from "../../src/envelope.js";

test("formatRuleGlobs null and string", () => {
  assert.equal(formatRuleGlobs(null), null);
  assert.equal(formatRuleGlobs("**/*.cs"), "**/*.cs");
});

test("formatRuleGlobs array", () => {
  assert.equal(formatRuleGlobs(["a", "b"]), "a, b");
});

// Envelope shape — list_rules wraps handler in wrapTool (TECH-399).
test("list_rules envelope: ok true with payload.rules array", async () => {
  const envelope = await wrapTool(async () => ({
    rules: [{ key: "invariants", description: "d" }],
  }))(undefined);
  assert.equal(envelope.ok, true);
  if (envelope.ok) {
    assert.ok(Array.isArray(envelope.payload.rules));
  }
});
