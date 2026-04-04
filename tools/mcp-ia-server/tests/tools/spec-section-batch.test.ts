/**
 * Batch spec section extraction (TECH-58).
 */

import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { describe, it } from "node:test";
import { buildRegistry } from "../../src/config.js";
import { runSpecSectionExtract } from "../../src/tools/spec-section.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, "../../../../");

describe("runSpecSectionExtract batch helper", () => {
  it(
    "returns two slices for geo + glossary",
    { skip: !fs.existsSync(path.join(repoRoot, ".cursor/specs/glossary.md")) },
    () => {
      const prev = process.env.REPO_ROOT;
      process.env.REPO_ROOT = repoRoot;
      try {
        const registry = buildRegistry();
        const a = runSpecSectionExtract(registry, "geo", "1", 800);
        const b = runSpecSectionExtract(registry, "glossary", "Grid & Coordinates", 500);
        if ("error" in a && a.error) assert.fail(String(a.error));
        if ("error" in b && b.error) assert.fail(String(b.error));
        assert.ok(String((a as { content?: string }).content ?? "").length > 0);
        assert.ok(String((b as { content?: string }).content ?? "").length > 0);
      } finally {
        process.env.REPO_ROOT = prev;
      }
    },
  );
});
