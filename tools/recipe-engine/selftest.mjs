#!/usr/bin/env node
/**
 * recipe-engine selftest harness — DEC-A19 Phase D close-out.
 *
 * Runs the three smoke recipes via `npm run recipe:run` and asserts:
 *   1. CLI exits 0.
 *   2. Output JSON parses + carries `ok:true`.
 *   3. Per-recipe shape probe — covers each step kind exercised:
 *        - noop-smoke    → bash step + scalar input bind
 *        - foreach-smoke → flow.foreach iteration + count bind
 *        - mcp-smoke     → mcp step + injector + dotted-path bind on array
 *
 * Wired into `validate:all` after `validate:recipe-drift` so the same chain CI
 * runs locally also exercises the runtime, not just the schema gate.
 *
 * Exit codes: 0 = all pass, 1 = any failure.
 */

import { spawnSync } from "node:child_process";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "..", "..");

const cases = [
  {
    name: "noop-smoke",
    inputs: ["--input", "message=hello-recipe-engine"],
    assert: (out) => {
      if (out.outputs?.echoed?.trim() !== "hello-recipe-engine") {
        return `expected outputs.echoed='hello-recipe-engine', got ${JSON.stringify(out.outputs)}`;
      }
      return null;
    },
  },
  {
    name: "foreach-smoke",
    inputs: ["--input", 'items=["a","b","c"]'],
    assert: (out) => {
      if (out.outputs?.count !== 3) {
        return `expected outputs.count=3, got ${JSON.stringify(out.outputs)}`;
      }
      return null;
    },
  },
  {
    name: "mcp-smoke",
    inputs: [],
    assert: (out) => {
      const c = out.outputs?.count;
      if (typeof c !== "number" || c < 1) {
        return `expected outputs.count >= 1, got ${JSON.stringify(out.outputs)}`;
      }
      return null;
    },
  },
];

let failures = 0;
for (const c of cases) {
  process.stdout.write(`[recipe-engine selftest] ${c.name} … `);
  const res = spawnSync(
    "npm",
    ["run", "-s", "recipe:run", "--", c.name, ...c.inputs],
    { cwd: REPO_ROOT, encoding: "utf8" },
  );
  if (res.status !== 0) {
    failures += 1;
    process.stdout.write("FAIL\n");
    console.error(`  exit=${res.status}`);
    if (res.stdout) console.error(`  stdout:\n${res.stdout}`);
    if (res.stderr) console.error(`  stderr:\n${res.stderr}`);
    continue;
  }
  let parsed;
  try {
    parsed = JSON.parse(res.stdout);
  } catch (err) {
    failures += 1;
    process.stdout.write("FAIL\n");
    console.error(`  stdout did not parse as JSON: ${err.message}`);
    console.error(`  raw:\n${res.stdout}`);
    continue;
  }
  if (parsed?.ok !== true) {
    failures += 1;
    process.stdout.write("FAIL\n");
    console.error(`  ok!=true: ${JSON.stringify(parsed)}`);
    continue;
  }
  const msg = c.assert(parsed);
  if (msg) {
    failures += 1;
    process.stdout.write("FAIL\n");
    console.error(`  ${msg}`);
    continue;
  }
  process.stdout.write(`OK (run_id=${parsed.run_id})\n`);
}

if (failures > 0) {
  console.error(`[recipe-engine selftest] ${failures}/${cases.length} failure(s)`);
  process.exit(1);
}
console.log(`[recipe-engine selftest] OK — ${cases.length}/${cases.length} pass`);
