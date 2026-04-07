/**
 * Repository root discovery when REPO_ROOT is unset (CLI from nested cwd).
 */

import assert from "node:assert/strict";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { describe, it } from "node:test";
import {
  findRepositoryRootWalkingUp,
  resolveRepoRoot,
} from "../../src/config.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, "../../../../");

describe("findRepositoryRootWalkingUp / resolveRepoRoot", () => {
  it("finds repo root from tools/mcp-ia-server path", () => {
    const nested = path.join(repoRoot, "tools", "mcp-ia-server");
    assert.equal(findRepositoryRootWalkingUp(nested), repoRoot);
  });

  it("resolveRepoRoot walks up when REPO_ROOT is unset (npm test cwd)", () => {
    const prev = process.env.REPO_ROOT;
    delete process.env.REPO_ROOT;
    try {
      assert.equal(resolveRepoRoot(), repoRoot);
    } finally {
      if (prev !== undefined) process.env.REPO_ROOT = prev;
    }
  });
});
