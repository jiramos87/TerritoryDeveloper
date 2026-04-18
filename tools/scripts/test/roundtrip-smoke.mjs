#!/usr/bin/env node
/**
 * roundtrip-smoke.mjs
 *
 * Smoke test: verify materialize-backlog --check passes against current
 * yaml records + manifests. Does NOT re-migrate (assumes yaml already generated).
 *
 * Exit 0 on success, 1 on failure.
 *
 * Usage:
 *   node tools/scripts/test/roundtrip-smoke.mjs
 */

import { execSync } from "node:child_process";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "../../..");

try {
  execSync(
    `node ${JSON.stringify(path.join(REPO_ROOT, "tools/scripts/materialize-backlog.mjs"))} --check`,
    { cwd: REPO_ROOT, stdio: "inherit" },
  );
  console.log("[roundtrip-smoke] PASS");
  process.exit(0);
} catch {
  console.error("[roundtrip-smoke] FAIL — materialize --check returned non-zero");
  process.exit(1);
}
