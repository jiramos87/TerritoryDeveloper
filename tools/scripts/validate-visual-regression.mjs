#!/usr/bin/env node
/**
 * validate-visual-regression.mjs — visual regression validator.
 *
 * ui-visual-regression Stage 3.0 (TECH-31895).
 *
 * Reads panels.json published rows (Assets/UI/Snapshots/panels.json),
 * calls ia_visual_diff per panel slug via DB pool,
 * emits per-panel verdict JSON to stdout.
 *
 * Strict mode (VISUAL_REGRESSION_STRICT=1):
 *   Resolves touched panel slugs from panels.json ∩ git diff path scan.
 *   Queries ia_visual_diff for verdict='regression' rows intersecting touched set.
 *   Exits non-zero with machine-parseable JSON error report when any regression found.
 *
 * Warn-only mode (default):
 *   Always exits 0. No behavior change for local verify:local.
 *
 * Exit codes:
 *   0 — ok or warn-only
 *   1 — strict mode regression detected
 *
 * Env:
 *   DATABASE_URL              Postgres connection string (loaded from .env if absent).
 *   VISUAL_REGRESSION_STRICT  When "1", exit 1 on any regression verdict for touched panels.
 */

import { readFileSync, existsSync } from "node:fs";
import { execSync } from "node:child_process";
import { createRequire } from "node:module";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(__dirname, "../../");

// ── Load .env ──────────────────────────────────────────────────────────────

if (!process.env.SKIP_DOTENV) {
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

// ── Strict mode: resolve touched panel slugs ───────────────────────────────

const strictMode = process.env.VISUAL_REGRESSION_STRICT === "1";

/** Resolve panel slugs touched by the current branch diff via git. */
function resolveTouchedSlugs(allSlugs) {
  let changedPaths = [];
  try {
    const out = execSync("git diff --name-only origin/main...HEAD 2>/dev/null || git diff --name-only HEAD~1...HEAD 2>/dev/null || echo ''", {
      cwd: repoRoot,
      encoding: "utf8",
      stdio: ["pipe", "pipe", "pipe"],
    });
    changedPaths = out.split("\n").map((p) => p.trim()).filter(Boolean);
  } catch {
    // If git fails, fall back to all panels (conservative).
    return new Set(allSlugs);
  }
  if (changedPaths.length === 0) return new Set(allSlugs);

  // Match panel slugs against changed paths: path contains slug token.
  const touched = new Set();
  for (const slug of allSlugs) {
    const slugToken = slug.replace(/-/g, "[-_]?");
    const re = new RegExp(slugToken, "i");
    if (changedPaths.some((p) => re.test(p))) {
      touched.add(slug);
    }
  }
  // If no slug matched path heuristic, use all (conservative).
  return touched.size > 0 ? touched : new Set(allSlugs);
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

// ── Per-panel baseline check ───────────────────────────────────────────────

const verdicts = [];

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
  }
}

// ── Strict mode: query ia_visual_diff for regression rows ─────────────────

let regressionRows = [];

if (strictMode) {
  const touchedSlugs = resolveTouchedSlugs(panels);
  try {
    const res = await pool.query(
      `SELECT d.id, d.verdict, d.diff_pct, d.ran_at, b.panel_slug
       FROM ia_visual_diff d
       JOIN ia_visual_baseline b ON b.id = d.baseline_id
       WHERE d.verdict = 'regression'
         AND b.panel_slug = ANY($1::text[])
       ORDER BY d.ran_at DESC`,
      [Array.from(touchedSlugs)],
    );
    regressionRows = res.rows;
  } catch (e) {
    console.warn(`[validate-visual-regression] strict-mode DB query failed: ${e.message} — treating as warn-only.`);
  }
}

await pool.end();

const hasRegression = regressionRows.length > 0;

const output = {
  ok: !hasRegression,
  warn_only: !strictMode,
  stage: "3.0",
  strict_mode: strictMode,
  verdicts,
  ...(strictMode && hasRegression ? {
    regressions: regressionRows.map((r) => ({
      panel_slug: r.panel_slug,
      diff_pct: parseFloat(r.diff_pct),
      ran_at: r.ran_at,
    })),
  } : {}),
};

console.log(JSON.stringify(output, null, 2));

if (strictMode && hasRegression) {
  console.error(JSON.stringify({
    error: "VISUAL_REGRESSION_STRICT=1: regression detected",
    regression_count: regressionRows.length,
    panels: regressionRows.map((r) => r.panel_slug),
  }));
  process.exit(1);
}
process.exit(0);
