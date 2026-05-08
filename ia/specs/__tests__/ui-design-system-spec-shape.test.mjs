/**
 * ui-design-system-spec-shape.test.mjs — Stage 5 spec-shape assertions.
 *
 * Red-Stage Proof: DesignSystem_TokensSection_HasMinimumLockedSet
 *   Parses ia/specs/ui-design-system.md; counts rows in §Tokens consumer table;
 *   asserts ≥20. Counts rows in §Components table; asserts ≥3 (IconButton + HudStrip + Label).
 *
 * Does NOT require DB. Pure spec file parse.
 */

import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const specPath = path.resolve(__dirname, "../ui-design-system.md");

function parseTableRows(content, sectionHeading) {
  // Find the section heading line
  const lines = content.split("\n");
  let inSection = false;
  let inTable = false;
  let headerPassed = false;
  const rows = [];

  for (const line of lines) {
    // Match heading (## or ###)
    if (/^#{2,3}\s/.test(line)) {
      if (inSection) break; // left our target section
      if (line.includes(sectionHeading)) {
        inSection = true;
        continue;
      }
    }

    if (!inSection) continue;

    const trimmed = line.trim();

    // Detect table rows (start with |)
    if (trimmed.startsWith("|")) {
      inTable = true;
      // Skip separator row (| --- | --- |)
      if (/^\|[\s\-:]+(\|[\s\-:]+)+\|$/.test(trimmed)) {
        headerPassed = true;
        continue;
      }
      if (!headerPassed) {
        // First row is header
        headerPassed = false; // will be set after separator
        continue;
      }
      rows.push(trimmed);
    } else if (inTable && trimmed === "") {
      // Blank line after table — stop collecting
      if (rows.length > 0) break;
    }
  }

  return rows;
}

const specContent = fs.readFileSync(specPath, "utf8");

// ---------------------------------------------------------------------------
// Test 1: DesignSystem_TokensSection_HasMinimumLockedSet
// ---------------------------------------------------------------------------
test("DesignSystem_TokensSection_HasMinimumLockedSet — §Tokens consumer table has ≥20 rows", () => {
  // Parse the Consumer token table under ## Tokens / ### Consumer token table
  const tokenRows = parseTableRows(specContent, "Consumer token table");

  assert.ok(
    tokenRows.length >= 20,
    `Expected ≥20 token rows in §Tokens consumer table, got ${tokenRows.length}. Check ia/specs/ui-design-system.md §Tokens.`,
  );
});

// ---------------------------------------------------------------------------
// Test 2: DesignSystem_ComponentsSection_HasMinimumLockedSet
// ---------------------------------------------------------------------------
test("DesignSystem_ComponentsSection_HasMinimumLockedSet — §Components table has ≥3 rows (IconButton + HudStrip + Label)", () => {
  // Parse Component table under ## Components / ### Component table
  const componentRows = parseTableRows(specContent, "Component table");

  assert.ok(
    componentRows.length >= 3,
    `Expected ≥3 component rows in §Components table, got ${componentRows.length}. Check ia/specs/ui-design-system.md §Components.`,
  );
});

// ---------------------------------------------------------------------------
// Test 3: LOCKED-COUNT annotations present in spec
// ---------------------------------------------------------------------------
test("DesignSystem_LockedCountAnnotations — spec headers carry LOCKED-COUNT annotations", () => {
  assert.ok(
    specContent.includes("LOCKED-COUNT: 20 tokens"),
    "§Tokens section must carry LOCKED-COUNT: 20 tokens annotation",
  );
  assert.ok(
    specContent.includes("LOCKED-COUNT: 6 components"),
    "§Components section must carry LOCKED-COUNT: 6 components annotation",
  );
});
