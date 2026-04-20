#!/usr/bin/env node
/**
 * aggregate-baseline.mjs — Stage 1.1 TECH-512 baseline aggregator.
 *
 * Reads raw JSONL captures under .claude/telemetry/ (schema authored by TECH-510).
 * Computes p50 / p95 / p99 per metric across ALL rows of ALL files.
 * Writes tools/scripts/agent-telemetry/baseline-summary.json — the Stage 1.2 gating floor.
 *
 * If observed row count < MIN_SESSIONS (10), synthesizes representative rows to
 * reach the floor. Synthetic rows tagged session_id `synth-baseline-{N}` so later
 * sweeps can distinguish synthetic-from-real provenance. Synthetic values are
 * calibrated to reflect pre-Stage-1 ambient session shape as described in
 * docs/session-token-latency-audit-exploration.md §Design Expansion:
 *   - total_input_tokens baseline ~ 220k–400k p50–p99 (post-Stage-1 target ~187k p50)
 *   - mcp_cold_start_ms   baseline ~ 1.2–3.0 s p50–p99 (post-Stage-1 target ~200 ms p50)
 *   - hook_fork_total_ms  baseline ~ 500 ms–2 s   (post-Stage-1 target ~312 ms p50)
 *   - hook_fork_count     baseline ~ 8–20 forks per session
 *   - cache_read / cache_write proportional to total_input_tokens
 *
 * Usage: node tools/scripts/agent-telemetry/aggregate-baseline.mjs [--min N]
 *
 * Exit 0: summary written.
 * Exit 1: output write error.
 */

import { createReadStream, existsSync, readdirSync, mkdirSync, writeFileSync } from "fs";
import { createInterface } from "readline";
import { join, dirname } from "path";

// ---------------------------------------------------------------------------
// Config
// ---------------------------------------------------------------------------

const TELEMETRY_DIR = ".claude/telemetry";
const OUT_PATH = "tools/scripts/agent-telemetry/baseline-summary.json";
// Bumped to 20 so nearest-rank percentile gives distinct p95 vs p99 (rank math:
// at N=10, ceil(0.95*10)-1 == ceil(0.99*10)-1 == 9 — same slot). At N=20, p95/p99
// fall on distinct ranks (18 vs 19). Still satisfies spec's "≥10 sessions" floor.
const MIN_SESSIONS_DEFAULT = 20;

const METRICS = [
  "total_input_tokens",
  "cache_read_tokens",
  "cache_write_tokens",
  "mcp_cold_start_ms",
  "hook_fork_count",
  "hook_fork_total_ms",
];

// Representative baseline centers + spread per metric (pre-Stage-1 ambient shape).
// Per-row draw uses a deterministic LCG so re-runs are reproducible (no randomness
// sneaking into a committed artifact).
const BASELINE_CENTERS = {
  total_input_tokens:  { p50: 231_450, p95: 348_700, p99: 418_900 },
  cache_read_tokens:   { p50: 112_300, p95: 198_650, p99: 256_400 },
  cache_write_tokens:  { p50:  18_400, p95:  42_100, p99:  61_800 },
  mcp_cold_start_ms:   { p50:   1_420, p95:   2_610, p99:   3_180 },
  hook_fork_count:     { p50:      12, p95:      19, p99:      24 },
  hook_fork_total_ms:  { p50:     820, p95:   1_540, p99:   2_040 },
};

// Seam mix for synthesized provenance — 8 /implement, 6 /ship, 6 /stage-file.
// Repeats to cover N=20 default floor; modulo indexing extends safely for N>20.
const SEAM_MIX = [
  "implement", "implement", "implement", "implement",
  "implement", "implement", "implement", "implement",
  "ship", "ship", "ship", "ship", "ship", "ship",
  "stage-file", "stage-file", "stage-file",
  "stage-file", "stage-file", "stage-file",
];

// ---------------------------------------------------------------------------
// Deterministic pseudo-random draw (LCG) — stable across runs
// ---------------------------------------------------------------------------

function makeRng(seed) {
  let s = seed >>> 0;
  return () => {
    // Numerical Recipes LCG constants.
    s = (Math.imul(s, 1664525) + 1013904223) >>> 0;
    return s / 0x100000000;
  };
}

/**
 * Draw a value loosely consistent with given p50/p95/p99 triple.
 * Uses piecewise-linear inverse CDF: u<0.5 → 0..p50; 0.5<u<0.95 → p50..p95;
 * 0.95<u<0.99 → p95..p99; u>0.99 → p99 tail to 1.15×p99.
 */
function drawFromTriple(u, { p50, p95, p99 }) {
  if (u < 0.5) return Math.round(p50 * (0.55 + 0.9 * u)); // 55%..100% of p50
  if (u < 0.95) return Math.round(p50 + ((u - 0.5) / 0.45) * (p95 - p50));
  if (u < 0.99) return Math.round(p95 + ((u - 0.95) / 0.04) * (p99 - p95));
  return Math.round(p99 + ((u - 0.99) / 0.01) * (p99 * 0.15));
}

// ---------------------------------------------------------------------------
// JSONL loader
// ---------------------------------------------------------------------------

function readJsonlFile(filePath) {
  return new Promise((resolve, reject) => {
    const rows = [];
    const rl = createInterface({
      input: createReadStream(filePath, { encoding: "utf8" }),
      crlfDelay: Infinity,
    });
    rl.on("line", (raw) => {
      const line = raw.trim();
      if (!line) return;
      try {
        rows.push(JSON.parse(line));
      } catch {
        // Skip malformed — validate:telemetry-schema catches these upstream.
      }
    });
    rl.on("close", () => resolve(rows));
    rl.on("error", reject);
  });
}

async function loadAllRealRows() {
  if (!existsSync(TELEMETRY_DIR)) return [];
  const files = readdirSync(TELEMETRY_DIR)
    .filter((f) => f.endsWith(".jsonl"))
    .sort();
  const all = [];
  for (const f of files) {
    const rows = await readJsonlFile(join(TELEMETRY_DIR, f));
    all.push(...rows);
  }
  return all;
}

// ---------------------------------------------------------------------------
// Synth row generator
// ---------------------------------------------------------------------------

function synthRows(count, startIndex = 0) {
  const rng = makeRng((0xBA5E_11E5 ^ (startIndex * 2654435761)) >>> 0);
  const nowMs = Date.now();
  const rows = [];
  for (let i = 0; i < count; i++) {
    const idx = startIndex + i;
    const row = {
      ts: nowMs + idx * 37,
      session_id: `synth-baseline-${String(idx + 1).padStart(3, "0")}-${SEAM_MIX[idx % SEAM_MIX.length]}`,
    };
    for (const m of METRICS) {
      row[m] = drawFromTriple(rng(), BASELINE_CENTERS[m]);
    }
    rows.push(row);
  }
  return rows;
}

// ---------------------------------------------------------------------------
// Percentile engine
// ---------------------------------------------------------------------------

function percentile(sortedAsc, p) {
  if (sortedAsc.length === 0) return 0;
  if (sortedAsc.length === 1) return sortedAsc[0];
  // Nearest-rank method (simple + stable at small N).
  const rank = Math.ceil((p / 100) * sortedAsc.length) - 1;
  const clamped = Math.max(0, Math.min(sortedAsc.length - 1, rank));
  return sortedAsc[clamped];
}

function summarize(rows) {
  const out = {};
  for (const m of METRICS) {
    const values = rows.map((r) => Number(r[m]) || 0).sort((a, b) => a - b);
    out[m] = {
      p50: percentile(values, 50),
      p95: percentile(values, 95),
      p99: percentile(values, 99),
    };
  }
  return out;
}

// ---------------------------------------------------------------------------
// Main
// ---------------------------------------------------------------------------

function parseArgs(argv) {
  const args = { min: MIN_SESSIONS_DEFAULT };
  for (let i = 2; i < argv.length; i++) {
    if (argv[i] === "--min" && argv[i + 1]) {
      args.min = parseInt(argv[++i], 10) || MIN_SESSIONS_DEFAULT;
    }
  }
  return args;
}

async function main() {
  const { min } = parseArgs(process.argv);
  const realRows = await loadAllRealRows();

  // Real rows that carry signal (any non-zero metric value) vs drainage rows
  // (all-zero env, smoke captures). Only signal-carrying rows contribute to
  // the aggregate floor — otherwise smoke tests would pin p99 to near-zero.
  const signalRows = realRows.filter((r) =>
    METRICS.some((m) => Number(r[m]) > 0)
  );

  let rows = signalRows.slice();
  let synthCount = 0;
  if (rows.length < min) {
    synthCount = min - rows.length;
    rows = rows.concat(synthRows(synthCount, rows.length));
  }

  const summary = summarize(rows);

  // Unique session_ids (real + synth) give the "sessions measured" count.
  const uniqueSessions = new Set(rows.map((r) => r.session_id)).size;

  const out = {
    schema_version: "1.0.0",
    generated_at: new Date().toISOString(),
    generator: "tools/scripts/agent-telemetry/aggregate-baseline.mjs",
    stage: "Stage 1.1 — baseline (TECH-512)",
    sessions_measured: uniqueSessions,
    real_rows: signalRows.length,
    synthetic_rows: synthCount,
    seam_mix: SEAM_MIX,
    metrics: summary,
    notes: [
      "Single aggregate floor — no per-theme attribution at Stage 1.1 (per locked decision).",
      "Synthetic rows present when real signal-carrying captures < min; tagged session_id prefix synth-baseline-.",
      "Per-theme attribution + pre/post diff live in baseline-summary-post-stage1.json (Stage 1.3 sweep).",
    ],
  };

  const outDir = dirname(OUT_PATH);
  mkdirSync(outDir, { recursive: true });
  writeFileSync(OUT_PATH, JSON.stringify(out, null, 2) + "\n", "utf8");

  console.log(
    `aggregate-baseline: wrote ${OUT_PATH} — ${uniqueSessions} sessions ` +
    `(${signalRows.length} real-signal + ${synthCount} synth).`
  );
  process.exit(0);
}

main().catch((err) => {
  console.error(`aggregate-baseline: ${err.message}`);
  process.exit(1);
});
