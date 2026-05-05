/**
 * backfill-seeded-digests.ts — TECH-14103
 *
 * Pre-scan classifier + seeded-marker writer for in-flight master plans.
 * Bands every stage of every non-done plan as:
 *   present_complete — all tasks have non-empty §Plan Digest bodies
 *   partial          — ≥1 task has body but ≥1 is empty
 *   missing          — all tasks have empty or null bodies
 *
 * Writer touches ONLY `missing` stages — partial and present_complete skipped (M#5).
 * Seeded body format: line 1 = `<!-- seeded: backfill_v1 -->` (D2 byte-exact).
 *
 * CLI: tsx tools/scripts/backfill-seeded-digests.ts [--dry-run] [--slug SLUG]
 *
 * Idempotent: tasks already carrying backfilled=true are skipped (WHERE backfilled=false guard).
 */

// pg dep lives under tools/postgres-ia/node_modules/ (workspace, not hoisted).
// Import via relative path to that workspace's installed module.
// @ts-expect-error — relative path import resolves to workspace pg
import pg from "../postgres-ia/node_modules/pg/lib/index.js";

const SEEDED_MARKER = "<!-- seeded: backfill_v1 -->";
const SEEDED_BODY = `${SEEDED_MARKER}
## §Plan Digest

### §Goal
<!-- TODO: fill in goal for this task -->

### §Acceptance
- [ ] Acceptance criteria TBD

### §Invariants & Gate
validator_gate: npm run validate:all
`;

// ---------------------------------------------------------------------------
// Band classification
// ---------------------------------------------------------------------------

export type Band = "present_complete" | "partial" | "missing";

export interface StageClassification {
  slug: string;
  stage_id: string;
  band: Band;
  task_count: number;
  empty_count: number;
}

export interface TaskRow {
  task_id: string;
  slug: string;
  stage_id: string;
  body: string | null;
  backfilled: boolean;
}

export function classifyBand(tasks: TaskRow[]): Band {
  if (tasks.length === 0) return "missing";
  const emptyCount = tasks.filter(
    (t) => !t.body || t.body.trim() === "" || t.body.trim() === "<!-- task_key: T0.0 -->",
  ).length;
  if (emptyCount === 0) return "present_complete";
  if (emptyCount === tasks.length) return "missing";
  return "partial";
}

// ---------------------------------------------------------------------------
// DB helpers
// ---------------------------------------------------------------------------

function resolveDbUrl(): string {
  const env = process.env.DATABASE_URL?.trim();
  if (env) return env;
  return "postgresql://postgres:postgres@localhost:5434/territory_ia_dev";
}

// ---------------------------------------------------------------------------
// Main classifier + writer
// ---------------------------------------------------------------------------

async function run(opts: { dryRun: boolean; slugFilter?: string }): Promise<void> {
  const pool = new pg.Pool({ connectionString: resolveDbUrl(), max: 2 });
  try {
    // Fetch all non-done plans
    const planQ = await pool.query<{ slug: string }>(
      `SELECT DISTINCT slug FROM ia_stages WHERE status != 'done'
       ${opts.slugFilter ? "AND slug = $1" : ""}
       ORDER BY slug`,
      opts.slugFilter ? [opts.slugFilter] : [],
    );

    const classifications: StageClassification[] = [];
    let seededCount = 0;

    for (const planRow of planQ.rows) {
      const stagesQ = await pool.query<{ stage_id: string }>(
        `SELECT stage_id FROM ia_stages WHERE slug = $1 AND status != 'done' ORDER BY stage_id`,
        [planRow.slug],
      );

      for (const stageRow of stagesQ.rows) {
        const tasksQ = await pool.query<TaskRow>(
          `SELECT task_id, slug, stage_id, body, backfilled
             FROM ia_tasks
            WHERE slug = $1 AND stage_id = $2
            ORDER BY task_id`,
          [planRow.slug, stageRow.stage_id],
        );

        const tasks = tasksQ.rows;
        const band = classifyBand(tasks);
        const emptyCount = tasks.filter(
          (t) => !t.body || t.body.trim() === "" || t.body.trim() === "<!-- task_key: T0.0 -->",
        ).length;

        classifications.push({
          slug: planRow.slug,
          stage_id: stageRow.stage_id,
          band,
          task_count: tasks.length,
          empty_count: emptyCount,
        });

        // Only write on `missing` stages, only to non-backfilled tasks
        if (band === "missing") {
          const unbackfilledTasks = tasks.filter((t) => !t.backfilled);
          if (unbackfilledTasks.length > 0) {
            if (opts.dryRun) {
              console.log(
                `[DRY-RUN] Would seed ${unbackfilledTasks.length} tasks in ${planRow.slug}/${stageRow.stage_id}`,
              );
              seededCount += unbackfilledTasks.length;
            } else {
              for (const task of unbackfilledTasks) {
                await pool.query(
                  `UPDATE ia_tasks SET body = $1, backfilled = true WHERE task_id = $2`,
                  [SEEDED_BODY, task.task_id],
                );
                seededCount++;
              }
              // Mark plan-level backfill_version
              await pool.query(
                `UPDATE ia_master_plans SET backfill_version = 'backfill_v1' WHERE slug = $1`,
                [planRow.slug],
              );
            }
          }
        }
      }
    }

    // Print classification report
    console.log("\nBackfill classifier report:");
    console.log("─".repeat(60));
    const byBand: Record<Band, number> = {
      present_complete: 0,
      partial: 0,
      missing: 0,
    };
    for (const c of classifications) {
      byBand[c.band]++;
      if (c.band !== "present_complete") {
        console.log(
          `  [${c.band.padEnd(16)}] ${c.slug}/${c.stage_id}  tasks=${c.task_count} empty=${c.empty_count}`,
        );
      }
    }
    console.log("\nSummary:");
    console.log(`  present_complete: ${byBand.present_complete}`);
    console.log(`  partial:          ${byBand.partial}`);
    console.log(`  missing:          ${byBand.missing}`);
    console.log(`  seeded:           ${seededCount}${opts.dryRun ? " (dry-run)" : ""}`);
  } finally {
    await pool.end();
  }
}

// ---------------------------------------------------------------------------
// CLI entry
// ---------------------------------------------------------------------------

const args = process.argv.slice(2);
const dryRun = args.includes("--dry-run");
const slugIdx = args.indexOf("--slug");
const slugFilter = slugIdx >= 0 ? args[slugIdx + 1] : undefined;

run({ dryRun, slugFilter }).catch((err) => {
  console.error("backfill-seeded-digests error:", err);
  process.exit(1);
});
