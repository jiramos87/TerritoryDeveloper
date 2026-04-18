import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { buildRegistry } from "../../src/config.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, "../../../../");

test(
  "buildRegistry finds IA entries when repo fixtures exist",
  { skip: !fs.existsSync(path.join(repoRoot, "ia/specs/glossary.md")) },
  () => {
    const prev = process.env.REPO_ROOT;
    process.env.REPO_ROOT = repoRoot;
    try {
      const r = buildRegistry();
      assert.ok(r.length >= 36, `expected ≥36 entries, got ${r.length}`);
      assert.ok(r.some((e) => e.key === "unity-development-context"));
      const rules = r.filter((e) => e.category === "rule");
      assert.ok(rules.length >= 21, `expected ≥21 rules, got ${rules.length}`);
      assert.ok(rules.some((e) => e.key === "agent-output-caveman"));
      assert.ok(rules.some((e) => e.key === "agent-output-caveman-authoring"));
      assert.ok(rules.some((e) => e.key === "terminology-consistency-authoring"));
      assert.ok(rules.some((e) => e.key === "xml-doc-caveman"));
    } finally {
      process.env.REPO_ROOT = prev;
    }
  },
);
