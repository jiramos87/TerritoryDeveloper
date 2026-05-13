#!/usr/bin/env node
/**
 * sweep-visual-baselines.mjs — horizontal sweep orchestrator.
 *
 * ui-visual-regression Stage 2.0 (TECH-31893).
 *
 * Enumerates every published panel via DB, dispatches baseline capture,
 * groups candidates by archetype (slug-prefix fallback), fires one
 * AskUserQuestion-style prompt per archetype group, calls
 * ui_visual_baseline_record for each approved slug, writes audit jsonl to
 * Library/UiBaselines/_sweep-{ts}.jsonl.
 *
 * Usage:
 *   node tools/scripts/sweep-visual-baselines.mjs [--dry-run] [--approve-all]
 *
 * Flags:
 *   --dry-run     Enumerate + group only; do not write baseline rows or jsonl.
 *   --approve-all Auto-approve all archetype groups (non-interactive batch).
 *
 * Env:
 *   DATABASE_URL   Postgres connection string (loaded from .env if absent).
 *   PANELS_JSON    Override panels.json path (default Assets/UI/Snapshots/panels.json).
 */

import { readFileSync, existsSync, mkdirSync, appendFileSync } from "node:fs";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";
import { createRequire } from "node:module";
import * as readline from "node:readline/promises";
import { stdin as input, stdout as output } from "node:process";

const __dirname = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = resolve(__dirname, "../../");

// ── Args ───────────────────────────────────────────────────────────────────

const args = process.argv.slice(2);
const DRY_RUN = args.includes("--dry-run");
const APPROVE_ALL = args.includes("--approve-all");

// ── Load .env ──────────────────────────────────────────────────────────────

const dotenvPath = resolve(REPO_ROOT, ".env");
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

const panelsPath = resolve(
  REPO_ROOT,
  process.env.PANELS_JSON ?? "Assets/UI/Snapshots/panels.json",
);
if (!existsSync(panelsPath)) {
  console.error(`[sweep] panels.json not found at ${panelsPath}`);
  process.exit(1);
}

/** @type {Array<{slug: string}>} */
let panels;
try {
  const raw = JSON.parse(readFileSync(panelsPath, "utf8"));
  panels = (raw.items ?? []).filter((item) => item.slug).map((item) => ({ slug: item.slug }));
} catch (e) {
  console.error(`[sweep] Failed to parse panels.json: ${e.message}`);
  process.exit(1);
}

if (panels.length === 0) {
  console.log("[sweep] No published panels found in panels.json — nothing to sweep.");
  process.exit(0);
}

console.log(`[sweep] Found ${panels.length} published panels.`);

// ── DB connection ──────────────────────────────────────────────────────────

const dbUrl = process.env.DATABASE_URL;
if (!dbUrl) {
  console.error("[sweep] DATABASE_URL not set. Provide via .env or environment.");
  process.exit(1);
}

const req = createRequire(import.meta.url);
let pg;
try {
  pg = req("pg");
} catch {
  console.error("[sweep] pg module not available — run npm install in tools/mcp-ia-server.");
  process.exit(1);
}

const pool = new pg.Pool({ connectionString: dbUrl, max: 3 });

// ── Fetch catalog_panel archetype + existing baseline status ───────────────

/**
 * Resolve archetype from DB catalog_entity.tags JSONB or slug-prefix fallback.
 * @param {string} slug
 * @param {Record<string, unknown>|null} tags
 * @returns {string}
 */
function resolveArchetype(slug, tags) {
  if (tags && typeof tags === "object" && !Array.isArray(tags)) {
    const t = tags;
    if (typeof t.archetype === "string" && t.archetype) return t.archetype;
  }
  // Slug-prefix fallback: take first hyphen-delimited segment.
  const prefix = slug.split("-")[0];
  return prefix ?? "other";
}

const panelMeta = [];
for (const { slug } of panels) {
  // Look up catalog_entity for archetype tag.
  let archetype = slug.split("-")[0] ?? "other";
  try {
    const res = await pool.query(
      `SELECT tags FROM catalog_entity WHERE slug = $1 AND kind = 'panel' LIMIT 1`,
      [slug],
    );
    if (res.rows.length > 0) {
      archetype = resolveArchetype(slug, res.rows[0].tags);
    }
  } catch {
    // DB miss — slug-prefix fallback already set above.
  }

  // Check existing active baseline.
  let hasBaseline = false;
  try {
    const bRes = await pool.query(
      `SELECT id FROM ia_visual_baseline
       WHERE panel_slug = $1 AND status = 'active'
       ORDER BY captured_at DESC LIMIT 1`,
      [slug],
    );
    hasBaseline = bRes.rows.length > 0;
  } catch {
    // Treat as no baseline.
  }

  panelMeta.push({ slug, archetype, hasBaseline });
}

// ── Group by archetype ─────────────────────────────────────────────────────

/** @type {Map<string, typeof panelMeta>} */
const groups = new Map();
for (const m of panelMeta) {
  const bucket = groups.get(m.archetype) ?? [];
  bucket.push(m);
  groups.set(m.archetype, bucket);
}

console.log(`[sweep] ${groups.size} archetype group(s):`);
for (const [arch, members] of groups) {
  const missing = members.filter((m) => !m.hasBaseline).length;
  console.log(`  ${arch}: ${members.length} panel(s), ${missing} missing baseline`);
}

// ── Per-archetype approval flow ────────────────────────────────────────────

/**
 * Prompt operator for archetype group.
 * Returns approved slug list.
 * @param {string} archetype
 * @param {typeof panelMeta} members
 * @returns {Promise<string[]>}
 */
async function promptArchetypeGroup(archetype, members) {
  const slugList = members.map((m) => m.slug).join(", ");
  const missingCount = members.filter((m) => !m.hasBaseline).length;
  console.log(`\n--- Archetype: ${archetype} ---`);
  console.log(`Panels (${members.length}): ${slugList}`);
  console.log(`Missing baselines: ${missingCount}`);
  console.log("Options:");
  console.log("  [1] approve_all   — record baseline for all panels in this group");
  console.log("  [2] approve_subset {slugs} — comma-separated subset to approve");
  console.log("  [3] skip          — skip this group (no baseline recorded)");
  console.log("  [4] refresh       — re-record even panels that already have a baseline");

  if (APPROVE_ALL) {
    console.log("[sweep] --approve-all flag set: auto-selecting approve_all");
    return members.map((m) => m.slug);
  }

  const rl = readline.createInterface({ input, output });
  let choice;
  try {
    choice = (await rl.question("Choice: ")).trim().toLowerCase();
  } finally {
    rl.close();
  }

  if (choice.startsWith("1") || choice === "approve_all") {
    return members.map((m) => m.slug);
  }
  if (choice.startsWith("2") || choice.startsWith("approve_subset")) {
    const raw = choice.replace(/^(2|approve_subset)\s*/i, "");
    return raw.split(",").map((s) => s.trim()).filter((s) => s.length > 0);
  }
  if (choice.startsWith("3") || choice === "skip") {
    console.log(`[sweep] Skipping archetype: ${archetype}`);
    return [];
  }
  if (choice.startsWith("4") || choice === "refresh") {
    // refresh = approve all including already-baselined.
    return members.map((m) => m.slug);
  }
  console.log("[sweep] Unrecognized choice — skipping group.");
  return [];
}

// ── Audit JSONL setup ──────────────────────────────────────────────────────

const ts = new Date().toISOString().replace(/[:.]/g, "-");
const auditDir = resolve(REPO_ROOT, "Library/UiBaselines");
const auditPath = resolve(auditDir, `_sweep-${ts}.jsonl`);

if (!DRY_RUN) {
  mkdirSync(auditDir, { recursive: true });
}

/**
 * Append one row to audit JSONL.
 * @param {object} row
 */
function appendAudit(row) {
  if (DRY_RUN) {
    console.log("[dry-run] audit row:", JSON.stringify(row));
    return;
  }
  appendFileSync(auditPath, JSON.stringify(row) + "\n", "utf8");
}

// ── Sweep loop ─────────────────────────────────────────────────────────────

let totalApproved = 0;
let totalRecorded = 0;
let totalSkipped = 0;

for (const [archetype, members] of groups) {
  const approvedSlugs = await promptArchetypeGroup(archetype, members);

  if (approvedSlugs.length === 0) {
    for (const m of members) {
      appendAudit({
        slug: m.slug,
        archetype,
        verdict: "skipped",
        captured_by: "sweep-orchestrator",
        sha256: null,
        lfs_size_bytes: null,
        skipped_at: new Date().toISOString(),
      });
      totalSkipped++;
    }
    continue;
  }

  for (const slug of approvedSlugs) {
    totalApproved++;

    // For the sweep, we record a synthetic baseline entry (no real PNG capture
    // in this orchestrator — real capture requires Editor batchmode bake).
    // The orchestrator marks the panel as sweep-approved; actual PNG paths
    // are captured via Editor bake pipeline (unity:bake-ui --capture-baselines).
    // We record the approval intent in JSONL for operator audit.
    //
    // If a candidate PNG exists at Library/UiBaselines/_candidate/{slug}.png,
    // we promote it. Otherwise we log pending_capture.
    const candidatePng = resolve(REPO_ROOT, `Library/UiBaselines/_candidate/${slug}.png`);
    const hasCandidatePng = existsSync(candidatePng);

    if (hasCandidatePng && !DRY_RUN) {
      // Promote candidate to baseline via DB.
      try {
        const { visualBaselineRepo } = await importUiCatalog();
        const client = await pool.connect();
        try {
          const repo = visualBaselineRepo(client);
          const { createHash } = await import("node:crypto");
          const { readFileSync: rf } = await import("node:fs");
          const bytes = rf(candidatePng);
          const sha256 = createHash("sha256").update(bytes).digest("hex");
          const lfsSize = bytes.length;

          const baselineDir = resolve(REPO_ROOT, "Assets/UI/VisualBaselines");
          mkdirSync(baselineDir, { recursive: true });
          const { copyFileSync } = await import("node:fs");
          const destName = `${slug}@v${Date.now()}.png`;
          const destPath = resolve(baselineDir, destName);
          copyFileSync(candidatePng, destPath);

          await repo.record({
            panel_slug: slug,
            image_ref: `Assets/UI/VisualBaselines/${destName}`,
            image_sha256: sha256,
            captured_by: "sweep-orchestrator",
          });

          appendAudit({
            slug,
            archetype,
            verdict: "captured",
            captured_by: "sweep-orchestrator",
            sha256,
            lfs_size_bytes: lfsSize,
            captured_at: new Date().toISOString(),
          });
          totalRecorded++;
          console.log(`[sweep] Recorded baseline for ${slug} (sha256: ${sha256.slice(0, 8)}...)`);
        } finally {
          client.release();
        }
      } catch (e) {
        console.error(`[sweep] Failed to record baseline for ${slug}: ${e.message}`);
        appendAudit({
          slug,
          archetype,
          verdict: "error",
          captured_by: "sweep-orchestrator",
          sha256: null,
          lfs_size_bytes: null,
          error: e.message,
          failed_at: new Date().toISOString(),
        });
      }
    } else {
      // No candidate PNG — log pending_capture so operator knows to run bake first.
      appendAudit({
        slug,
        archetype,
        verdict: "pending_capture",
        captured_by: "sweep-orchestrator",
        sha256: null,
        lfs_size_bytes: null,
        note: hasCandidatePng ? "dry_run" : "candidate_png_missing — run unity:bake-ui --capture-baselines first",
        queued_at: new Date().toISOString(),
      });
      if (!DRY_RUN) {
        console.log(`[sweep] ${slug}: no candidate PNG — queued as pending_capture. Run unity:bake-ui --capture-baselines --panels=${slug} first.`);
      }
    }
  }
}

await pool.end();

console.log(`\n[sweep] Done.`);
console.log(`  Approved: ${totalApproved} | Recorded: ${totalRecorded} | Skipped: ${totalSkipped}`);
if (!DRY_RUN) {
  console.log(`  Audit JSONL: ${auditPath}`);
}

// ── Lazy import ui-catalog (optional — skip if MCP server not built) ───────

async function importUiCatalog() {
  try {
    return await import("../mcp-ia-server/src/ia-db/ui-catalog.js");
  } catch {
    // Fallback: inline minimal record impl using raw SQL.
    return {
      visualBaselineRepo: (client) => ({
        async record(input) {
          const resolution = input.resolution ?? "1920x1080";
          const theme = input.theme ?? "dark";
          const tolerance_pct = input.tolerance_pct ?? 0.005;
          await client.query(
            `UPDATE ia_visual_baseline
             SET status = 'retired'
             WHERE panel_slug = $1 AND resolution = $2 AND theme = $3 AND status = 'active'`,
            [input.panel_slug, resolution, theme],
          );
          const ins = await client.query(
            `INSERT INTO ia_visual_baseline
               (panel_slug, image_ref, image_sha256, resolution, theme,
                tolerance_pct, captured_by, status)
             VALUES ($1,$2,$3,$4,$5,$6,$7,'active')
             RETURNING *`,
            [
              input.panel_slug,
              input.image_ref,
              input.image_sha256,
              resolution,
              theme,
              tolerance_pct,
              input.captured_by ?? null,
            ],
          );
          return ins.rows[0];
        },
      }),
    };
  }
}
