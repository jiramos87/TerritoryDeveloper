/**
 * validate-plan-digest-coverage.ts — TECH-14103
 *
 * Flags non-done tasks with empty §Plan Digest body.
 * Tasks carrying the seeded marker `<!-- seeded: backfill_v1 -->` are classified
 * in a separate `seeded` band, NOT the `missing` band.
 *
 * Exit codes:
 *   0 — all non-done tasks have non-empty body (or are seeded)
 *   1 — ≥1 non-done task with truly missing body
 *   2 — DB error
 *
 * CLI: tsx tools/scripts/validators/validate-plan-digest-coverage.ts
 */

// pg dep lives under tools/postgres-ia/node_modules/ (workspace, not hoisted).
// Import via relative path to that workspace's installed module.
// @ts-expect-error — relative path import resolves to workspace pg
import pg from "../../postgres-ia/node_modules/pg/lib/index.js";

const SEEDED_MARKER = "<!-- seeded: backfill_v1 -->";

function resolveDbUrl(): string {
  const env = process.env.DATABASE_URL?.trim();
  if (env) return env;
  if (process.env.CI === "true" || process.env.GITHUB_ACTIONS === "true") {
    // In CI without DB — skip gracefully
    return "";
  }
  return "postgresql://postgres:postgres@localhost:5434/territory_ia_dev";
}

interface TaskCoverageRow {
  task_id: string;
  slug: string;
  stage_id: string | null;
  title: string;
  body: string | null;
}

async function main() {
  const dbUrl = resolveDbUrl();
  if (!dbUrl) {
    console.log("validate:plan-digest-coverage — SKIP (no DB in CI)");
    process.exit(0);
  }

  const pool = new pg.Pool({ connectionString: dbUrl, max: 2 });
  try {
    // Check if backfilled column exists (requires migration 0073).
    // Until migration lands, gate is advisory-only — preexisting legacy tasks
    // without §Plan Digest predate this validator.
    const colCheck = await pool.query(
      `SELECT 1 FROM information_schema.columns
        WHERE table_name = 'ia_tasks' AND column_name = 'backfilled'`,
    );
    if ((colCheck.rowCount ?? 0) === 0) {
      console.log(
        "validate:plan-digest-coverage — SKIP (migration 0073 not yet applied; backfilled column absent)",
      );
      process.exit(0);
    }

    // Scope: only tasks whose parent master plan is active (not grandfathered,
    // not closed). Drops null-slug orphans + grandfather-plan tasks that
    // predate the §Plan Digest requirement. Mirrors the partition pattern
    // in `validate-plan-red-stage.mjs` (TECH-10896).
    const res = await pool.query<TaskCoverageRow>(
      `SELECT t.task_id, t.slug, t.stage_id, t.title,
              COALESCE(t.body, '') AS body
         FROM ia_tasks t
         JOIN ia_master_plans p ON p.slug = t.slug
        WHERE t.status NOT IN ('done', 'archived')
          AND COALESCE(p.tdd_red_green_grandfathered, FALSE) = FALSE
          AND p.closed_at IS NULL
        ORDER BY t.slug, t.stage_id, t.task_id`,
    );

    const missing: TaskCoverageRow[] = [];
    const seeded: TaskCoverageRow[] = [];

    for (const row of res.rows) {
      const bodyTrimmed = (row.body ?? "").trim();
      if (!bodyTrimmed) {
        missing.push(row);
        continue;
      }
      if (bodyTrimmed.startsWith(SEEDED_MARKER)) {
        seeded.push(row);
        continue;
      }
      // has non-empty, non-seeded body — covered
    }

    console.log(
      `validate:plan-digest-coverage — missing=${missing.length} seeded=${seeded.length}`,
    );

    if (missing.length > 0) {
      console.log("\nMissing §Plan Digest (non-done tasks with empty body):");
      for (const t of missing) {
        console.log(`  ${t.task_id}  ${t.slug}/${t.stage_id ?? "?"}  "${t.title}"`);
      }
    }

    if (seeded.length > 0) {
      console.log("\nSeeded band (backfill placeholder — upgrade before close):");
      for (const t of seeded) {
        console.log(`  ${t.task_id}  ${t.slug}/${t.stage_id ?? "?"}  "${t.title}"`);
      }
    }

    const output = {
      missing: missing.map((t) => t.task_id),
      seeded: seeded.map((t) => t.task_id),
    };
    console.log("\n" + JSON.stringify(output, null, 2));

    process.exit(missing.length > 0 ? 1 : 0);
  } catch (err) {
    console.error("validate:plan-digest-coverage DB error:", err);
    process.exit(2);
  } finally {
    await pool.end();
  }
}

main();
