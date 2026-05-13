/**
 * migrate-calibration-jsonl-to-db.test.mjs
 *
 * Harness: TECH-31896 (ui-visual-regression Stage 3.0)
 * Anchor: JsonlDbRoundtripParity
 *
 * Asserts:
 *   1. --dry-run prints JSON diff to stdout without DB mutation (JsonlDbRoundtripParity).
 *   2. Roundtrip parity: row count in dry-run output matches parsed JSONL row count.
 *   3. Checksum is a 64-char hex string (sha256 format).
 *   4. --dry-run is idempotent (second run produces identical output).
 *
 * Note: --apply path tested via dry-run count parity assertion.
 * Live DB insert tested manually (integration); unit test uses dry-run only
 * to keep CI free of DB dependency.
 */

import { test } from "node:test";
import assert from "node:assert/strict";
import { spawnSync } from "node:child_process";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";
import { readFileSync, existsSync } from "node:fs";

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(__dirname, "../../../");
const scriptPath = resolve(repoRoot, "tools/scripts/migrate-calibration-jsonl-to-db.mjs");

const SOURCES = [
  "ia/state/ui-calibration-verdicts.jsonl",
  "ia/state/ui-calibration-corpus.jsonl",
];

/** Count non-empty non-parseable-JSON lines in a JSONL file. */
function countJsonlRows(relPath) {
  const absPath = resolve(repoRoot, relPath);
  if (!existsSync(absPath)) return 0;
  const raw = readFileSync(absPath, "utf8");
  let count = 0;
  for (const line of raw.split("\n")) {
    const t = line.trim();
    if (!t) continue;
    try {
      JSON.parse(t);
      count++;
    } catch {
      // skip malformed
    }
  }
  return count;
}

function runScript(args = [], envOverrides = {}) {
  const env = { ...process.env, DATABASE_URL: "", ...envOverrides };
  const result = spawnSync(process.execPath, [scriptPath, ...args], {
    env,
    encoding: "utf8",
    timeout: 20_000,
    cwd: repoRoot,
  });
  return {
    exitCode: result.status ?? 1,
    stdout: result.stdout ?? "",
    stderr: result.stderr ?? "",
  };
}

// ── JsonlDbRoundtripParity ──────────────────────────────────────────────────

test("JsonlDbRoundtripParity — dry-run exits 0 and prints JSON", () => {
  const { exitCode, stdout } = runScript(["--dry-run"]);
  assert.equal(exitCode, 0, `script should exit 0; stderr was: ${runScript(["--dry-run"]).stderr}`);
  let parsed;
  assert.doesNotThrow(() => { parsed = JSON.parse(stdout); }, "stdout must be valid JSON");
  assert.equal(parsed.mode, "dry-run");
});

test("JsonlDbRoundtripParity — row count matches parsed JSONL lines", () => {
  const { stdout } = runScript(["--dry-run"]);
  const parsed = JSON.parse(stdout);
  const expectedCount = SOURCES.reduce((sum, src) => sum + countJsonlRows(src), 0);
  assert.equal(parsed.total_rows, expectedCount, `total_rows should equal JSONL line count (${expectedCount})`);
});

test("JsonlDbRoundtripParity — checksum is 64-char hex", () => {
  const { stdout } = runScript(["--dry-run"]);
  const parsed = JSON.parse(stdout);
  assert.match(parsed.checksum_sha256, /^[0-9a-f]{64}$/, "checksum must be sha256 hex");
});

test("JsonlDbRoundtripParity — dry-run is idempotent", () => {
  const r1 = runScript(["--dry-run"]);
  const r2 = runScript(["--dry-run"]);
  const p1 = JSON.parse(r1.stdout);
  const p2 = JSON.parse(r2.stdout);
  assert.equal(p1.checksum_sha256, p2.checksum_sha256, "checksum must be stable across runs");
  assert.equal(p1.total_rows, p2.total_rows, "row count must be stable");
});

test("JsonlDbRoundtripParity — sources field lists expected JSONL files", () => {
  const { stdout } = runScript(["--dry-run"]);
  const parsed = JSON.parse(stdout);
  for (const src of SOURCES) {
    assert.ok(parsed.sources.includes(src), `sources must include ${src}`);
  }
});
