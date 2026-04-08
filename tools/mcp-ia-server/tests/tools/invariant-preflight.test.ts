/**
 * invariant_preflight — composite tool bundling invariants + router + spec sections.
 */

import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { parseBacklogIssue } from "../../src/parser/backlog-parser.js";
import { parseInvariantsBody } from "../../src/tools/invariants-summary.js";
import { inferDomainHintsFromPath } from "../../src/tools/router-for-task.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, "../../../../");

test("parseInvariantsBody extracts invariants and guardrails", () => {
  const body = `
# System Invariants
1. HeightMap and Cell.height must be in sync.
2. Road cache must be invalidated after changes.

# Guardrails
- Never use FindObjectOfType in Update.
- Always use road preparation family.
`;
  const result = parseInvariantsBody(body);
  assert.equal(result.invariants.length, 2);
  assert.equal(result.guardrails.length, 2);
  assert.ok(result.invariants[0]!.includes("HeightMap"));
  assert.ok(result.guardrails[0]!.includes("FindObjectOfType"));
});

test("inferDomainHintsFromPath returns domain hints for economy files", () => {
  const hints = inferDomainHintsFromPath("Assets/Scripts/Managers/GameManagers/DemandManager.cs");
  assert.ok(hints.length > 0);
  assert.ok(hints.some((h) => h.toLowerCase().includes("simulation")));
});

test("inferDomainHintsFromPath returns hints for road files", () => {
  const hints = inferDomainHintsFromPath("RoadManager.cs");
  assert.ok(hints.some((h) => h.toLowerCase().includes("road")));
});

test(
  "parseBacklogIssue returns files for FEAT-23",
  { skip: !fs.existsSync(path.join(repoRoot, "BACKLOG.md")) },
  () => {
    const issue = parseBacklogIssue(repoRoot, "FEAT-23");
    assert.ok(issue);
    assert.equal(issue!.issue_id, "FEAT-23");
    assert.ok(issue!.files, "FEAT-23 should have a Files field");
  },
);
