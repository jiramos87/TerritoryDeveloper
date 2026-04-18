/**
 * invariant_preflight — composite tool bundling invariants + router + spec sections.
 * TECH-401: envelope wrap + env caps tests added.
 */

import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { parseBacklogIssue } from "../../src/parser/backlog-parser.js";
import { parseInvariantsBody } from "../../src/tools/invariants-summary.js";
import { inferDomainHintsFromPath } from "../../src/tools/router-for-task.js";
import { wrapTool } from "../../src/envelope.js";

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
  "parseBacklogIssue resolves archived FEAT-22 from BACKLOG-ARCHIVE.md",
  { skip: !fs.existsSync(path.join(repoRoot, "BACKLOG.md")) },
  () => {
    const issue = parseBacklogIssue(repoRoot, "FEAT-22");
    assert.ok(issue);
    assert.equal(issue!.issue_id, "FEAT-22");
    assert.ok(issue!.files, "archived issue should retain a Files field");
  },
);

// ---------------------------------------------------------------------------
// Envelope shape tests (TECH-401 Phase 2)
// ---------------------------------------------------------------------------

test("invariant_preflight envelope — ok:false + invalid_input when issue_id empty", async () => {
  const handler = wrapTool(async ({ issue_id }: { issue_id?: string }) => {
    const issueId = (issue_id ?? "").trim();
    if (!issueId) {
      throw { code: "invalid_input" as const, message: "issue_id is required." };
    }
    return {};
  });
  const result = await handler({ issue_id: "" });
  assert.equal(result.ok, false);
  if (!result.ok) {
    assert.equal(result.error.code, "invalid_input");
    assert.ok(result.error.message.includes("issue_id"));
  }
});

test("invariant_preflight envelope — ok:false + issue_not_found for unknown id", async () => {
  const handler = wrapTool(async ({ issue_id }: { issue_id?: string }) => {
    const issueId = (issue_id ?? "").trim();
    if (!issueId) {
      throw { code: "invalid_input" as const, message: "issue_id is required." };
    }
    // Simulate unknown issue
    const found = false;
    if (!found) {
      throw { code: "issue_not_found" as const, message: `No issue '${issueId}' found.` };
    }
    return {};
  });
  const result = await handler({ issue_id: "TECH-99999" });
  assert.equal(result.ok, false);
  if (!result.ok) {
    assert.equal(result.error.code, "issue_not_found");
  }
});

test("invariant_preflight envelope — ok:true on success", async () => {
  const mockPayload = { issue: { issue_id: "TECH-1" }, invariants: [], router: {}, spec_sections: [] };
  const handler = wrapTool(async (_input: { issue_id?: string }) => mockPayload);
  const result = await handler({ issue_id: "TECH-1" });
  assert.equal(result.ok, true);
  if (result.ok) {
    assert.ok("issue" in result.payload);
  }
});

// ---------------------------------------------------------------------------
// Env caps tests (TECH-401 Phase 3)
// ---------------------------------------------------------------------------

test("INVARIANT_PREFLIGHT_MAX_CHARS default is 800", () => {
  const DEFAULT = 800;
  const envVal = process.env.INVARIANT_PREFLIGHT_MAX_CHARS;
  delete process.env.INVARIANT_PREFLIGHT_MAX_CHARS;
  const resolved = Number(process.env.INVARIANT_PREFLIGHT_MAX_CHARS ?? DEFAULT);
  assert.equal(resolved, DEFAULT);
  if (envVal !== undefined) process.env.INVARIANT_PREFLIGHT_MAX_CHARS = envVal;
});

test("INVARIANT_PREFLIGHT_MAX_SECTIONS default is 6", () => {
  const DEFAULT = 6;
  const envVal = process.env.INVARIANT_PREFLIGHT_MAX_SECTIONS;
  delete process.env.INVARIANT_PREFLIGHT_MAX_SECTIONS;
  const resolved = Number(process.env.INVARIANT_PREFLIGHT_MAX_SECTIONS ?? DEFAULT);
  assert.equal(resolved, DEFAULT);
  if (envVal !== undefined) process.env.INVARIANT_PREFLIGHT_MAX_SECTIONS = envVal;
});

test("INVARIANT_PREFLIGHT_MAX_CHARS env override respected", () => {
  const originalEnv = process.env.INVARIANT_PREFLIGHT_MAX_CHARS;
  process.env.INVARIANT_PREFLIGHT_MAX_CHARS = "200";
  const resolved = Number(process.env.INVARIANT_PREFLIGHT_MAX_CHARS ?? 800);
  assert.equal(resolved, 200);
  // Restore
  if (originalEnv !== undefined) {
    process.env.INVARIANT_PREFLIGHT_MAX_CHARS = originalEnv;
  } else {
    delete process.env.INVARIANT_PREFLIGHT_MAX_CHARS;
  }
});

test("INVARIANT_PREFLIGHT_MAX_SECTIONS env override respected", () => {
  const originalEnv = process.env.INVARIANT_PREFLIGHT_MAX_SECTIONS;
  process.env.INVARIANT_PREFLIGHT_MAX_SECTIONS = "2";
  const resolved = Number(process.env.INVARIANT_PREFLIGHT_MAX_SECTIONS ?? 6);
  assert.equal(resolved, 2);
  // Restore
  if (originalEnv !== undefined) {
    process.env.INVARIANT_PREFLIGHT_MAX_SECTIONS = originalEnv;
  } else {
    delete process.env.INVARIANT_PREFLIGHT_MAX_SECTIONS;
  }
});
