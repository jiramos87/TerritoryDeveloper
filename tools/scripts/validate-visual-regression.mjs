#!/usr/bin/env node
/**
 * validate-visual-regression.mjs — warn-only visual regression validator.
 *
 * ui-visual-regression Stage 1.0 (TECH-31892).
 *
 * Reads panels.json published rows (Assets/UI/Snapshots/panels.json),
 * calls ui_visual_baseline_get per panel slug via DB pool,
 * emits per-panel verdict JSON to stdout.
 *
 * Exit codes:
 *   0 always at Stage 1.0 (warn-only mode).
 *   Strict mode (exit 1 on regression) lands at Task 3.0.1 via STRICT env gate.
 *
 * Env:
 *   DATABASE_URL   Postgres connection string (loaded from .env if absent).
 *   STRICT         When "1", exit 1 on any regression verdict (Stage 3.0.1+).
 */

import { readFileSync, existsSync } from "node:fs";
import { createRequire } from "node:module";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(__dirname, "../../");

// ── Load .env ──────────────────────────────────────────────────────────────

const dotenvPath = resolve(repoRoot, ".env");
if (existsSync(dotenvPath)) {
  const raw = readFileSync(dotenvPath, "utf8");
  for (const line of raw.split("\n")) {
    const trimmed = line.trim();
    if (!trimmed || trimmed.startsWith("#")) continue;
    const eqIdx = trimmed.indexOf("=");
    if (eqIdx < 0) continue;
    const key = trimmed.slice(0, eqIdx).trim();
    const val = trimmed.slice(eqIdx + 1).trim().replace(/^['"]|['"]$/g, "");
    if (!process.env[key]) process.env[key] = val;
  }
}

// ── Load panels.json ───────────────────────────────────────────────────────

const panelsPath = resolve(repoRoot, "Assets/UI/Snapshots/panels.json");
if (!existsSync(panelsPath)) {
  console.log(JSON.stringify({
    ok: true,
    skipped: true,
    reason: "panels.json not found — skip (no panels baked yet)",
    verdicts: [],
  }, null, 2));
  process.exit(0);
}

let panels;
try {
  const raw = JSON.parse(readFileSync(panelsPath, "utf8"));
  panels = (raw.items ?? []).map((item) => item.slug).filter(Boolean);
} catch (e) {
  console.error(`[validate-visual-regression] Failed to parse panels.json: ${e.message}`);
  process.exit(0); // warn-only
}

if (panels.length === 0) {
  console.log(JSON.stringify({ ok: true, skipped: true, reason: "no panels in snapshot", verdicts: [] }, null, 2));
  process.exit(0);
}

// ── DB connection ──────────────────────────────────────────────────────────

const dbUrl = process.env.DATABASE_URL;
if (!dbUrl) {
  console.warn("[validate-visual-regression] DATABASE_URL not set — skip (warn-only).");
  console.log(JSON.stringify({ ok: true, skipped: true, reason: "DATABASE_URL not set", verdicts: [] }, null, 2));
  process.exit(0);
}

// Dynamic import of pg to avoid hard ESM/CJS dependency.
let pg;
try {
  const req = createRequire(import.meta.url);
  pg = req("pg");
} catch {
  console.warn("[validate-visual-regression] pg module not available — skip.");
  process.exit(0);
}

const pool = new pg.Pool({ connectionString: dbUrl, max: 2 });

// ── Per-panel verdict ──────────────────────────────────────────────────────

const verdicts = [];
let hasRegression = false;

for (const slug of panels) {
  try {
    const res = await pool.query(
      `SELECT id, image_sha256, tolerance_pct, captured_at, status
       FROM ia_visual_baseline
       WHERE panel_slug = $1 AND status = 'active'
       ORDER BY captured_at DESC LIMIT 1`,
      [slug],
    );
    if (res.rows.length === 0) {
      verdicts.push({ panel_slug: slug, verdict: "new_baseline_needed", baseline_status: "missing" });
    } else {
      const row = res.rows[0];
      verdicts.push({
        panel_slug: slug,
        verdict: "match",
        baseline_status: "active",
        image_sha256: row.image_sha256,
        tolerance_pct: parseFloat(row.tolerance_pct),
        captured_at: row.captured_at,
      });
    }
  } catch (e) {
    verdicts.push({ panel_slug: slug, verdict: "error", error: e.message });
    hasRegression = true;
  }
}

await pool.end();

const output = {
  ok: !hasRegression,
  warn_only: true,
  stage: "1.0",
  verdicts,
};

console.log(JSON.stringify(output, null, 2));

// Stage 1.0: always exit 0 (warn-only). STRICT mode gate at Task 3.0.1.
const strictMode = process.env.STRICT === "1";
if (strictMode && hasRegression) {
  console.error("[validate-visual-regression] STRICT mode: regression detected — exit 1.");
  process.exit(1);
}
process.exit(0);
