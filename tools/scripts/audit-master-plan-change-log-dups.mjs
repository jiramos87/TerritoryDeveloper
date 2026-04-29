#!/usr/bin/env node
/**
 * audit-master-plan-change-log-dups.mjs
 *
 * Pre-migration gate for `0042_master_plan_change_log_unique.sql`. Reports
 * duplicate `(slug, stage_id, kind, commit_sha)` groups in
 * `ia_master_plan_change_log` that would violate the new UNIQUE constraint.
 *
 * `stage_id` may not exist yet (pre-`0042`); the audit handles both
 * pre-migration (column absent) and post-migration (column present)
 * schemas. NULL columns are treated as distinct (PG default UNIQUE
 * semantics) — only groups with non-null `commit_sha` + count > 1 are
 * flagged. NULL stage_id rows are folded into the `(slug, kind,
 * commit_sha)` key when the column does not exist yet.
 *
 * Exit codes:
 *   0  Clean — no dup groups; safe to apply migration.
 *   1  DB connection / query error.
 *   2  Dup groups present — operator must dedup before running migration.
 *
 * Operator escape hatch on exit 2:
 *   Pick the row(s) to keep (newest `entry_id` typically); DELETE older
 *   entries by `entry_id`. Re-run audit until clean. Migration `0042` is
 *   blocked until exit 0.
 */

import path from "node:path";
import { fileURLToPath } from "node:url";
import { createRequire } from "node:module";
import { resolveDatabaseUrl } from "../postgres-ia/resolve-database-url.mjs";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = process.env.IA_REPO_ROOT ?? path.resolve(__dirname, "../..");

const pgRequire = createRequire(
  path.join(REPO_ROOT, "tools/postgres-ia/package.json"),
);
const pg = pgRequire("pg");

const conn = resolveDatabaseUrl(REPO_ROOT);
if (!conn) {
  console.error("DATABASE_URL not resolvable — aborting");
  process.exit(1);
}

const client = new pg.Client({ connectionString: conn });

try {
  await client.connect();
} catch (err) {
  console.error(`DB connect failed: ${err.message}`);
  process.exit(1);
}

// Detect whether stage_id column already exists (post-migration shape).
const colRes = await client.query(
  `SELECT column_name FROM information_schema.columns
    WHERE table_name = 'ia_master_plan_change_log' AND column_name = 'stage_id'`,
);
const hasStageId = colRes.rowCount > 0;

const groupCols = hasStageId
  ? "slug, stage_id, kind, commit_sha"
  : "slug, kind, commit_sha";
const selectCols = hasStageId
  ? "slug, stage_id, kind, commit_sha"
  : "slug, NULL::text AS stage_id, kind, commit_sha";

// Only flag groups where every UNIQUE-constraint column is NOT NULL.
// PG UNIQUE treats NULL as distinct, so partial-NULL groups would not
// violate the constraint and are not actionable for the operator.
//
// Pre-migration shape (no stage_id column): every row is implicit
// stage_id=NULL → no group can fully satisfy the new constraint key, so
// the audit reports zero violations.
const notNullClause = hasStageId
  ? "commit_sha IS NOT NULL AND stage_id IS NOT NULL"
  : "commit_sha IS NOT NULL AND FALSE";

const dupRes = await client.query(
  `SELECT ${selectCols}, count(*)::int AS n,
          array_agg(entry_id ORDER BY ts ASC) AS entry_ids
     FROM ia_master_plan_change_log
    WHERE ${notNullClause}
    GROUP BY ${groupCols}
   HAVING count(*) > 1
    ORDER BY n DESC, slug ASC, kind ASC`,
);

await client.end();

if (dupRes.rowCount === 0) {
  console.log(
    "audit-master-plan-change-log-dups: OK — 0 dup groups; safe to apply 0042.",
  );
  process.exit(0);
}

console.error(
  `audit-master-plan-change-log-dups: FAIL — ${dupRes.rowCount} dup group(s); UNIQUE constraint would reject.`,
);
console.error("");
console.error(
  "| Plan slug | Stage id | Kind | commit_sha | Count | Entry ids (oldest → newest) |",
);
console.error("|---|---|---|---|---|---|");
for (const row of dupRes.rows) {
  console.error(
    `| ${row.slug} | ${row.stage_id ?? "_null_"} | ${row.kind} | ${row.commit_sha} | ${row.n} | ${row.entry_ids.join(", ")} |`,
  );
}
console.error("");
console.error(
  "Resolution: keep the newest entry per group; DELETE older entry_ids. Then re-run audit.",
);
console.error(
  "  e.g. DELETE FROM ia_master_plan_change_log WHERE entry_id IN (...);",
);
process.exit(2);
