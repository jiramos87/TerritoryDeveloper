/**
 * Batch spec section extraction (TECH-58).
 * Phase 1 (TECH-398): runSpecSectionExtract now returns a ToolEnvelope shape on success
 * and throws typed { code } errors on not-found paths.
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
const hasSpecs = fs.existsSync(path.join(repoRoot, "ia/specs/glossary.md"));

describe("runSpecSectionExtract batch helper", () => {
  it(
    "returns envelope with ok:true + payload.content for two slices",
    { skip: !hasSpecs },
    () => {
      const prev = process.env.REPO_ROOT;
      process.env.REPO_ROOT = repoRoot;
      try {
        const registry = buildRegistry();
        const a = runSpecSectionExtract(registry, "geo", "1", 800) as unknown as {
          ok: boolean; payload: { content: string };
        };
        const b = runSpecSectionExtract(registry, "glossary", "Grid & Coordinates", 500) as unknown as {
          ok: boolean; payload: { content: string };
        };
        assert.equal(a.ok, true);
        assert.equal(b.ok, true);
        assert.ok(a.payload.content.length > 0);
        assert.ok(b.payload.content.length > 0);
      } finally {
        process.env.REPO_ROOT = prev;
      }
    },
  );

  it(
    "throws spec_not_found for unknown spec key",
    { skip: !hasSpecs },
    () => {
      const prev = process.env.REPO_ROOT;
      process.env.REPO_ROOT = repoRoot;
      try {
        const registry = buildRegistry();
        assert.throws(
          () => runSpecSectionExtract(registry, "__missing__", "intro", 500),
          (e: { code?: string }) => {
            assert.equal(e.code, "spec_not_found");
            return true;
          },
        );
      } finally {
        process.env.REPO_ROOT = prev;
      }
    },
  );
});
