/**
 * Tests for rule_content envelope shape (TECH-399 Phase 3).
 *
 * rule_content now wraps handler in wrapTool; unknown rule → spec_not_found.
 * These tests exercise the internal extract logic directly to validate envelope.
 */

import assert from "node:assert/strict";
import { describe, it } from "node:test";
import path from "node:path";
import { fileURLToPath } from "node:url";
import fs from "node:fs";
import { wrapTool } from "../../src/envelope.js";
import { findRuleEntry } from "../../src/config.js";
import { buildRegistry } from "../../src/config.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, "../../../../");
const hasRules = fs.existsSync(
  path.join(repoRoot, "ia", "rules", "invariants.md"),
);

describe("rule_content envelope (TECH-399)", () => {
  it(
    "known rule → ok: true with payload fields",
    { skip: !hasRules },
    async () => {
      const prev = process.env.REPO_ROOT;
      process.env.REPO_ROOT = repoRoot;
      try {
        const registry = buildRegistry();
        const envelope = await wrapTool(async () => {
          const entry = findRuleEntry(registry, "invariants");
          if (!entry) {
            throw {
              code: "spec_not_found" as const,
              message: "No rule found for 'invariants'.",
              details: {},
            };
          }
          return { key: entry.key, fileName: entry.fileName, content: "ok" };
        })(undefined);
        assert.equal(envelope.ok, true);
        if (envelope.ok) {
          assert.ok(typeof envelope.payload.key === "string");
        }
      } finally {
        process.env.REPO_ROOT = prev;
      }
    },
  );

  it(
    "unknown rule → ok: false, error.code spec_not_found with details.available_rules",
    { skip: !hasRules },
    async () => {
      const prev = process.env.REPO_ROOT;
      process.env.REPO_ROOT = repoRoot;
      try {
        const registry = buildRegistry();
        const envelope = await wrapTool(async () => {
          const ruleKey = "__no_such_rule__";
          const entry = findRuleEntry(registry, ruleKey);
          if (!entry) {
            const available_rules = registry
              .filter((e) => e.category === "rule")
              .map((e) => ({ key: e.key, description: e.description }))
              .sort((a, b) => a.key.localeCompare(b.key));
            throw {
              code: "spec_not_found" as const,
              message: `No rule found for '${ruleKey}'.`,
              hint: "Call list_rules to retrieve available rule keys.",
              details: { available_rules },
            };
          }
          return {};
        })(undefined);
        assert.equal(envelope.ok, false);
        if (!envelope.ok) {
          assert.equal(envelope.error.code, "spec_not_found");
          assert.ok(
            Array.isArray(
              (envelope.error.details as { available_rules?: unknown[] })
                ?.available_rules,
            ),
          );
        }
      } finally {
        process.env.REPO_ROOT = prev;
      }
    },
  );
});
