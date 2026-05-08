/**
 * invariant-glob-update.test.mjs
 *
 * invariant_5_glob_matches_new_domains_path:
 *   Asserts ia/rules/unity-invariants.md invariant #5 contains both legacy
 *   Managers/GameManagers/*Service.cs AND new Domains/*/Services/*Service.cs globs.
 */

import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { readFileSync, existsSync } from "node:fs";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);
const REPO_ROOT = resolve(__dirname, "../../..");

describe("invariant_5_glob_matches_new_domains_path", () => {
  it("unity-invariants.md contains legacy Managers/GameManagers/*Service.cs carve-out", () => {
    const invPath = resolve(REPO_ROOT, "ia/rules/unity-invariants.md");
    assert.ok(existsSync(invPath), `unity-invariants.md not found at ${invPath}`);
    const content = readFileSync(invPath, "utf8");
    assert.match(
      content,
      /Managers\/GameManagers\/\*Service\.cs/,
      "Must contain legacy Managers/GameManagers/*Service.cs glob in invariant #5"
    );
  });

  it("unity-invariants.md contains new Domains/*/Services/*Service.cs carve-out", () => {
    const invPath = resolve(REPO_ROOT, "ia/rules/unity-invariants.md");
    const content = readFileSync(invPath, "utf8");
    assert.match(
      content,
      /Domains\/\*\/Services\/\*Service\.cs/,
      "Must contain new Domains/*/Services/*Service.cs glob in invariant #5"
    );
  });

  it("both globs appear in the same invariant #5 line or clause", () => {
    const invPath = resolve(REPO_ROOT, "ia/rules/unity-invariants.md");
    const content = readFileSync(invPath, "utf8");
    // Find the invariant #5 line
    const lines = content.split("\n");
    const inv5Line = lines.find((l) => l.includes("5.") && l.includes("gridArray") && l.includes("cellArray"));
    assert.ok(inv5Line, "Could not find invariant #5 line (containing gridArray and cellArray)");
    assert.match(inv5Line, /Managers\/GameManagers\/\*Service\.cs/, "Legacy glob in inv #5 line");
    assert.match(inv5Line, /Domains\/\*\/Services\/\*Service\.cs/, "New Domains glob in inv #5 line");
  });
});
