/**
 * validate-plan-digest-coverage.ts — TECH-14103 + TECH-36121
 *
 * Flags non-done tasks with empty §Plan Digest body.
 * Tasks carrying the seeded marker `<!-- seeded: backfill_v1 -->` are classified
 * in a separate `seeded` band, NOT the `missing` band.
 *
 * TECH-36121 (Wave B): also enforces EARS prefix on each §Acceptance row
 * unless plan.ears_grandfathered=TRUE.
 *
 * EARS prefixes (case-insensitive):
 *   WHEN, THE, IF, WHILE, WHERE
 * Each §Acceptance row must begin with one of these words.
 *
 * Exit codes:
 *   0 — all non-done tasks have non-empty body (or are seeded) + EARS clean
 *   1 — ≥1 non-done task with truly missing body OR EARS violation
 *   2 — DB error
 *
 * CLI: tsx tools/scripts/validators/validate-plan-digest-coverage.ts
 */

// pg dep lives under tools/postgres-ia/node_modules/ (workspace, not hoisted).
// Import via relative path to that workspace's installed module.
// @ts-expect-error — relative path import resolves to workspace pg
import pg from "../../postgres-ia/node_modules/pg/lib/index.js";

const SEEDED_MARKER = "<!-- seeded: backfill_v1 -->";

// EARS pattern prefixes (rule 10 of plan-digest-contract.md).
// Match at start of trimmed line, case-insensitive.
const EARS_PREFIX_RE = /^(when|the|if|while|where)\b/i;

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
  ears_grandfathered: boolean;
}

/**
 * Extract §Acceptance rows from a §Plan Digest body.
 * Looks for a `### §Acceptance` heading and collects non-empty lines until
 * the next `###` heading.
 */
function extractAcceptanceRows(body: string): string[] {
  const lines = body.split("\n");
  const rows: string[] = [];
  let inAcceptance = false;
  for (const line of lines) {
    const trimmed = line.trim();
    if (/^###\s+§Acceptance\b/i.test(trimmed)) {
      inAcceptance = true;
      continue;
    }
    if (inAcceptance) {
      if (/^###/.test(trimmed)) {
        // Next heading — stop
        break;
      }
      // Skip empty lines and markdown list markers only (bare `-` or `*`)
      if (trimmed === "" || trimmed === "-" || trimmed === "*") continue;
      // Collect content rows; strip leading list markers
      const content = trimmed.replace(/^[-*]\s+/, "").trim();
      if (content) rows.push(content);
    }
  }
  return rows;
}

/**
 * Validate EARS prefixes on acceptance rows.
 * Returns array of violating row strings (empty = all good).
 */
function findEarsViolations(rows: string[]): string[] {
  return rows.filter((row) => !EARS_PREFIX_RE.test(row));
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
    // Until migration lands, gate is advisory-only.
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

    // Check if ears_grandfathered column exists (requires migration 0160).
    const earsColCheck = await pool.query(
      `SELECT 1 FROM information_schema.columns
        WHERE table_name = 'ia_master_plans' AND column_name = 'ears_grandfathered'`,
    );
    const earsColumnExists = (earsColCheck.rowCount ?? 0) > 0;

    // Scope: only tasks whose parent master plan is active (not grandfathered,
    // not closed). Drops null-slug orphans + grandfather-plan tasks that
    // predate the §Plan Digest requirement.
    const earsGrandfatheredCol = earsColumnExists
      ? "COALESCE(p.ears_grandfathered, FALSE)"
      : "FALSE";

    const res = await pool.query<TaskCoverageRow>(
      `SELECT t.task_id, t.slug, t.stage_id, t.title,
              COALESCE(t.body, '') AS body,
              ${earsGrandfatheredCol} AS ears_grandfathered
         FROM ia_tasks t
         JOIN ia_master_plans p ON p.slug = t.slug
        WHERE t.status NOT IN ('done', 'archived')
          AND COALESCE(p.tdd_red_green_grandfathered, FALSE) = FALSE
          AND p.closed_at IS NULL
        ORDER BY t.slug, t.stage_id, t.task_id`,
    );

    const missing: TaskCoverageRow[] = [];
    const seeded: TaskCoverageRow[] = [];
    const earsViolations: Array<{ task: TaskCoverageRow; rows: string[] }> = [];

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
      // has non-empty, non-seeded body — covered for body check

      // EARS check: skip if plan is grandfathered
      if (!row.ears_grandfathered) {
        const acceptanceRows = extractAcceptanceRows(bodyTrimmed);
        if (acceptanceRows.length > 0) {
          const violations = findEarsViolations(acceptanceRows);
          if (violations.length > 0) {
            earsViolations.push({ task: row, rows: violations });
          }
        }
      }
    }

    console.log(
      `validate:plan-digest-coverage — missing=${missing.length} seeded=${seeded.length} ears_violations=${earsViolations.length}`,
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

    if (earsViolations.length > 0) {
      console.log(
        "\nEARS prefix violations (§Acceptance rows not starting with WHEN/THE/IF/WHILE/WHERE):",
      );
      for (const { task, rows } of earsViolations) {
        console.log(`  ${task.task_id}  ${task.slug}/${task.stage_id ?? "?"}  "${task.title}"`);
        for (const r of rows) {
          console.log(`    VIOLATION: "${r.substring(0, 120)}"`);
        }
      }
    }

    const output = {
      missing: missing.map((t) => t.task_id),
      seeded: seeded.map((t) => t.task_id),
      ears_violations: earsViolations.map((v) => ({
        task_id: v.task.task_id,
        violating_rows: v.rows,
      })),
    };
    console.log("\n" + JSON.stringify(output, null, 2));

    const failed = missing.length > 0 || earsViolations.length > 0;
    process.exit(failed ? 1 : 0);
  } catch (err) {
    console.error("validate:plan-digest-coverage DB error:", err);
    process.exit(2);
  } finally {
    await pool.end();
  }
}

main();
