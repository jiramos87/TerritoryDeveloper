/**
 * validate-seeded-task-stale.ts — TECH-14103
 *
 * Warns (exit 0) when backfilled=true rows older than 30 days exist.
 * These are seeded placeholder bodies that have sat unupgraded and
 * may be forgotten.
 *
 * Exit codes:
 *   0 — no stale seeded tasks (or DB unavailable — graceful skip)
 *   0 — stale tasks found but warning only (not a hard-fail gate)
 *
 * CLI: tsx tools/scripts/validators/validate-seeded-task-stale.ts
 */

// pg dep lives under tools/postgres-ia/node_modules/ (workspace, not hoisted).
// Import via relative path to that workspace's installed module.
// @ts-expect-error — relative path import resolves to workspace pg
import pg from "../../postgres-ia/node_modules/pg/lib/index.js";

const STALE_DAYS = 30;

function resolveDbUrl(): string {
  const env = process.env.DATABASE_URL?.trim();
  if (env) return env;
  if (process.env.CI === "true" || process.env.GITHUB_ACTIONS === "true") {
    return "";
  }
  return "postgresql://postgres:postgres@localhost:5434/territory_ia_dev";
}

interface StaleRow {
  task_id: string;
  slug: string;
  stage_id: string | null;
  title: string;
  created_at: string;
}

async function main() {
  const dbUrl = resolveDbUrl();
  if (!dbUrl) {
    console.log("validate:seeded-task-stale — SKIP (no DB in CI)");
    process.exit(0);
  }

  const pool = new pg.Pool({ connectionString: dbUrl, max: 2 });
  try {
    // Check if backfilled column exists (requires migration 0073)
    const colCheck = await pool.query(
      `SELECT 1 FROM information_schema.columns
        WHERE table_name = 'ia_tasks' AND column_name = 'backfilled'`,
    );
    if ((colCheck.rowCount ?? 0) === 0) {
      console.log(
        "validate:seeded-task-stale — SKIP (migration 0073 not yet applied; backfilled column absent)",
      );
      process.exit(0);
    }

    const res = await pool.query<StaleRow>(
      `SELECT task_id, slug, stage_id, title, created_at
         FROM ia_tasks
        WHERE backfilled = true
          AND status NOT IN ('done', 'archived')
          AND created_at < NOW() - INTERVAL '${STALE_DAYS} days'
        ORDER BY created_at ASC`,
    );

    if (res.rows.length === 0) {
      console.log("validate:seeded-task-stale — OK (0 stale seeded tasks)");
      process.exit(0);
    }

    console.log(
      `validate:seeded-task-stale — WARN: ${res.rows.length} seeded task(s) older than ${STALE_DAYS} days`,
    );
    for (const r of res.rows) {
      console.log(
        `  ${r.task_id}  ${r.slug}/${r.stage_id ?? "?"}  "${r.title}"  created=${r.created_at}`,
      );
    }

    // Warn-only: exit 0 regardless
    process.exit(0);
  } catch (err) {
    console.error("validate:seeded-task-stale DB error:", err);
    process.exit(0); // warn-only; never block CI on DB error
  } finally {
    await pool.end();
  }
}

main();
