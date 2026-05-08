/**
 * glossary-amendment.test.mjs
 *
 * new_componentization_terms_resolvable_via_mcp:
 *   Asserts all 11 new componentization terms are present in ia/specs/glossary.md.
 *   (MCP glossary_lookup is integration-only; this test validates the source file directly.)
 */

import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { readFileSync, existsSync } from "node:fs";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const REPO_ROOT = resolve(__dirname, "../../..");

const REQUIRED_TERMS = [
  "atomization",
  "componentization",
  "service facade",
  "concern boundary",
  "asmdef boundary",
  "domain folder",
  "facade interface",
  "sub-stage decomposition",
  "soft cap",
  "escape hatch",
  "trust boundary",
];

describe("new_componentization_terms_resolvable_via_mcp", () => {
  it("glossary.md exists", () => {
    const glossaryPath = resolve(REPO_ROOT, "ia/specs/glossary.md");
    assert.ok(existsSync(glossaryPath), `glossary.md not found at ${glossaryPath}`);
  });

  it("§Code architecture (atomization) section exists", () => {
    const content = readFileSync(resolve(REPO_ROOT, "ia/specs/glossary.md"), "utf8");
    assert.match(content, /## Code architecture \(atomization\)/, "Missing §Code architecture (atomization) section");
  });

  for (const term of REQUIRED_TERMS) {
    it(`glossary contains term: "${term}"`, () => {
      const content = readFileSync(resolve(REPO_ROOT, "ia/specs/glossary.md"), "utf8");
      // Check for the term as a table row entry (| term |)
      const termRe = new RegExp(`\\|\\s*${term.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}\\s*\\|`, "i");
      assert.match(content, termRe, `Glossary missing term: "${term}"`);
    });
  }

  it("glossary change-log entry exists for TECH-23773", () => {
    const logPath = resolve(REPO_ROOT, "ia/state/change-log/glossary-changes.md");
    assert.ok(existsSync(logPath), `glossary-changes.md not found at ${logPath}`);
    const content = readFileSync(logPath, "utf8");
    assert.match(content, /TECH-23773/, "glossary-changes.md missing TECH-23773 entry");
  });
});
