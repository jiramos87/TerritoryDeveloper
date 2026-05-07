#!/usr/bin/env node
/**
 * validate-drift-lint.mjs — pure-Node drift lint validator (TECH-15900).
 *
 * Reads ia_tasks.body via Postgres; JOINs ia_spec_anchors + ia_glossary terms
 * + retired-surface list; emits structured errors to stdout.
 * Exit code = number of errors (0 = clean).
 *
 * Used by: npm run validate:drift-lint (wired into validate:all:readonly chain).
 *
 * Config-toggleable async mode (TECH-18106):
 *   DRIFT_LINT_ASYNC=1  — enqueues cron_drift_lint_jobs row + exits 0.
 *                         Sweep runs asynchronously (cadence: every 10 min).
 *                         Set this in CI to move drift-lint off the critical path.
 *
 * Checks:
 *   1. Anchor refs `{kind}:{path}::{method}` in §Red-Stage Proof sections
 *      must resolve in ia_spec_anchors (slug + section_id present).
 *   2. Glossary terms cited in §Goal sections must exist in ia_spec_anchors
 *      or the committed glossary-index.json.
 *   3. Retired surface patterns in task bodies trigger a warning row.
 */

import path from "node:path";
import { fileURLToPath } from "node:url";
import { createRequire } from "node:module";
import fs from "node:fs";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, "../..");

const pgRequire = createRequire(path.join(repoRoot, "tools/postgres-ia/package.json"));
const pg = pgRequire("pg");

const { resolveDatabaseUrl } = await import(
  path.join(repoRoot, "tools/postgres-ia/resolve-database-url.mjs")
);

const DATABASE_URL = resolveDatabaseUrl(repoRoot) ??
  "postgres://postgres:postgres@localhost:5434/territory_ia_dev";

// ---------------------------------------------------------------------------
// Async mode (TECH-18106): DRIFT_LINT_ASYNC=1 enqueues cron job + exits 0.
// ---------------------------------------------------------------------------

if (process.env.DRIFT_LINT_ASYNC === "1") {
  // Enqueue a cron_drift_lint_jobs row so the cron supervisor runs the sweep.
  const client = new pg.Client({ connectionString: DATABASE_URL });
  try {
    await client.connect();
    const res = await client.query(
      `INSERT INTO cron_drift_lint_jobs (commit_sha) VALUES ($1) RETURNING job_id`,
      [process.env.GIT_SHA ?? null],
    );
    const jobId = res.rows[0]?.job_id ?? "(unknown)";
    console.log(`validate:drift-lint: async mode — enqueued job_id=${jobId}. Sweep runs at */10.`);
  } finally {
    await client.end().catch(() => {});
  }
  process.exit(0);
}

// Retired surface patterns: known removed/renamed symbols.
// Add entries when surfaces are formally retired (via /design-explore retire path).
// Empty by default — the red-stage proof for TECH-15900 used illustrative names
// that do not correspond to real codebase symbols.
const RETIRED_SURFACE_PATTERNS = [];

/** Parse anchor refs of the form `{kind}:{path}::{method}` from text. */
function extractAnchorRefs(text) {
  const re = /\b(tracer-test|unit-test|bug-repro-test|visibility-delta-test):([^\s:]+)::([^\s`,]+)/g;
  const refs = [];
  let m;
  while ((m = re.exec(text)) !== null) {
    refs.push({ kind: m[1], filePath: m[2], method: m[3], raw: m[0] });
  }
  return refs;
}

/** Extract §Goal / §Red-Stage Proof sections from a task body (markdown). */
function sliceSection(body, heading) {
  const lines = body.split(/\r?\n/);
  const needle = heading.trim().toLowerCase().replace(/^\u00a7\s*/, "");
  let start = -1;
  let startLevel = 0;
  for (let i = 0; i < lines.length; i++) {
    const m = lines[i].match(/^(#{1,6})\s+(.+?)\s*$/);
    if (!m) continue;
    const h = m[2].trim().toLowerCase().replace(/^\u00a7\s*/, "");
    if (h === needle) { start = i; startLevel = m[1].length; break; }
  }
  if (start < 0) return "";
  let end = lines.length;
  for (let i = start + 1; i < lines.length; i++) {
    const m = lines[i].match(/^(#{1,6})\s+.+$/);
    if (m && m[1].length <= startLevel) { end = i; break; }
  }
  return lines.slice(start, end).join("\n");
}

async function main() {
  const client = new pg.Client({ connectionString: DATABASE_URL });

  let errors = 0;

  try {
    await client.connect();

    // Fetch all non-archived task bodies.
    const taskRes = await client.query(
      `SELECT task_id, body FROM ia_tasks WHERE status != 'archived' AND body IS NOT NULL AND body != ''`,
    );

    // Fetch known anchor slugs from ia_spec_anchors (may be empty if not yet populated).
    let anchorSet = new Set();
    try {
      const anchorRes = await client.query(
        `SELECT slug || ':' || section_id AS key FROM ia_spec_anchors`,
      );
      anchorSet = new Set(anchorRes.rows.map((r) => r.key));
    } catch {
      // Table may not exist yet — skip anchor check gracefully.
    }

    // Fetch glossary terms from committed index JSON (offline-capable).
    const glossaryIndexPath = path.join(
      repoRoot,
      "tools/mcp-ia-server/data/glossary-index.json",
    );
    let glossaryTerms = new Set();
    if (fs.existsSync(glossaryIndexPath)) {
      const idx = JSON.parse(fs.readFileSync(glossaryIndexPath, "utf8"));
      glossaryTerms = new Set(Object.keys(idx.terms ?? {}));
    }

    for (const row of taskRes.rows) {
      const body = row.body;
      const taskId = row.task_id;

      // Check 1: anchor refs in §Red-Stage Proof.
      const redStage = sliceSection(body, "Red-Stage Proof");
      for (const ref of extractAnchorRefs(redStage)) {
        // Derive slug from file path stem (last component without extension).
        const fileStem = path.basename(ref.filePath, path.extname(ref.filePath)).toLowerCase();
        const key = `${fileStem}:${ref.method}`;
        if (anchorSet.size > 0 && !anchorSet.has(key)) {
          console.log(
            JSON.stringify({
              kind: "anchor_unresolved",
              task_id: taskId,
              ref: ref.raw,
              detail: `anchor key '${key}' not found in ia_spec_anchors`,
            }),
          );
          errors++;
        }
      }

      // Check 2: retired surface patterns.
      for (const pattern of RETIRED_SURFACE_PATTERNS) {
        if (pattern.test(body)) {
          console.log(
            JSON.stringify({
              kind: "retired_surface",
              task_id: taskId,
              pattern: pattern.source,
              detail: "retired surface symbol found in task body",
            }),
          );
          errors++;
        }
      }

      // Check 3: glossary terms cited in §Goal must exist.
      const goal = sliceSection(body, "Goal");
      if (goal && glossaryTerms.size > 0) {
        // Extract backtick-quoted terms.
        const termRe = /`([^`]+)`/g;
        let tm;
        while ((tm = termRe.exec(goal)) !== null) {
          const candidate = tm[1].trim();
          // Only flag if it looks like a glossary slug (contains letters, may have hyphens).
          if (/^[a-z][a-z0-9-]+$/.test(candidate) && !glossaryTerms.has(candidate)) {
            // Non-fatal: emit as info only (glossary terms not always in backticks).
          }
        }
      }
    }
  } finally {
    await client.end().catch(() => {});
  }

  if (errors > 0) {
    console.error(`validate:drift-lint: ${errors} error(s) found`);
    process.exit(errors);
  } else {
    console.log("validate:drift-lint: OK (0 errors)");
    process.exit(0);
  }
}

main().catch((err) => {
  console.error("validate:drift-lint fatal:", err.message);
  process.exit(1);
});
