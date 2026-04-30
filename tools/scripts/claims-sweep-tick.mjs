#!/usr/bin/env node
/**
 * claims-sweep-tick.mjs
 *
 * Scheduled claim-sweep tick. Releases stale `ia_section_claims` +
 * `ia_stage_claims` rows whose `last_heartbeat` is older than
 * `carcass_config.claim_heartbeat_timeout_minutes` (default 10), then emits
 * one `master_plan_change_log` row per affected master-plan slug with
 * `kind='claim_swept'` + structured `metadata` (released_count,
 * section_released[], stage_released[], timeout_minutes).
 *
 * Scheduler: system `cron` at `* * * * *` (every minute) is the chosen
 * production cadence — zero dev-host daemon dep, survives shell exit. Ops doc
 * `docs/parallel-carcass-claims-sweep-ops.md` covers crontab + macOS launchd
 * snippets. `node-cron` named as fallback only (when host lacks system cron).
 *
 * Activation (linux): write `/etc/cron.d/claims-sweep` with
 *   `* * * * * <user> cd /repo && npm run claims:sweep:tick >> /var/log/claims-sweep.log 2>&1`
 * Activation (macOS): see ops doc for `~/Library/LaunchAgents/...plist` shape.
 *
 * Manual / agent-driven invocation: `npm run claims:sweep:tick` (or direct
 * `mcp__territory-ia__claims_sweep` MCP call — see ops doc §3).
 *
 * `.mjs` (not `.ts`): cross-tree TS imports under `node --import tsx` only
 * pass through `default` exports (sibling test cannot resolve named export).
 * Plain ESM `.mjs` matches `tools/scripts/audit-master-plan-change-log-dups.mjs`
 * pattern + works under both `node` direct + test runner.
 *
 * parallel-carcass §6.2 / D4. Stage 2.3 / TECH-5251.
 */

import path from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import { createRequire } from "node:module";
import { resolveDatabaseUrl } from "../postgres-ia/resolve-database-url.mjs";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = process.env.IA_REPO_ROOT ?? path.resolve(__dirname, "../..");

const pgRequire = createRequire(
  path.join(REPO_ROOT, "tools/postgres-ia/package.json"),
);
const pg = pgRequire("pg");

/**
 * @typedef {object} SweepTickResult
 * @property {number} timeout_minutes
 * @property {number} released_count_total
 * @property {string[]} affected_slugs
 * @property {number} change_log_rows_appended
 */

/**
 * Runs one sweep tick: capture stale-row candidates pre-sweep, release stale
 * rows in both claim tables, group released rows by slug, emit one
 * `master_plan_change_log` row per affected slug. Zero releases → silent
 * no-op (no change_log rows).
 *
 * Throws on rows lacking slug — invariant escalation per §Plan Digest STOP
 * rule. Next tick covers transient inconsistency.
 *
 * @param {{ pool?: import('pg').Pool }} [opts]
 * @returns {Promise<SweepTickResult>}
 */
export async function runSweepTick(opts = {}) {
  const ownsPool = !opts.pool;
  const pool = opts.pool ?? buildPool();
  if (!pool) {
    throw new Error(
      "claims-sweep-tick: ia_db pool not initialized (DATABASE_URL missing)",
    );
  }

  try {
    // Read timeout once — same source the MCP `claims_sweep` tool uses.
    const cfg = await pool.query(
      `SELECT value FROM carcass_config
        WHERE key = 'claim_heartbeat_timeout_minutes'`,
    );
    const timeoutMinutes =
      cfg.rows.length > 0
        ? parseInt(cfg.rows[0].value, 10) || 10
        : 10;

    // Pre-sweep: capture which (slug, section_id) + (slug, stage_id) rows
    // are about to be released. Same predicate as the UPDATE below — keeps
    // grouping deterministic.
    const sectionCandidates = await pool.query(
      `SELECT slug, section_id AS key
         FROM ia_section_claims
        WHERE released_at IS NULL
          AND last_heartbeat < now() - ($1 || ' minutes')::interval`,
      [String(timeoutMinutes)],
    );
    const stageCandidates = await pool.query(
      `SELECT slug, stage_id AS key
         FROM ia_stage_claims
        WHERE released_at IS NULL
          AND last_heartbeat < now() - ($1 || ' minutes')::interval`,
      [String(timeoutMinutes)],
    );

    // Slug invariant — escalate per STOP rule if any row lacks slug.
    for (const row of [
      ...sectionCandidates.rows,
      ...stageCandidates.rows,
    ]) {
      if (!row.slug) {
        throw new Error(
          "claims-sweep-tick: candidate row lacks slug — refusing to emit change_log without slug attribution",
        );
      }
    }

    // Apply the sweep — UPDATE both tables. Pure SQL (mirrors `applySweep`
    // in tools/mcp-ia-server/src/tools/claim-heartbeat.ts so the MCP path +
    // tick path stay shape-equivalent).
    const sec = await pool.query(
      `UPDATE ia_section_claims
          SET released_at = now()
        WHERE released_at IS NULL
          AND last_heartbeat < now() - ($1 || ' minutes')::interval`,
      [String(timeoutMinutes)],
    );
    const stg = await pool.query(
      `UPDATE ia_stage_claims
          SET released_at = now()
        WHERE released_at IS NULL
          AND last_heartbeat < now() - ($1 || ' minutes')::interval`,
      [String(timeoutMinutes)],
    );

    const totalReleased = (sec.rowCount ?? 0) + (stg.rowCount ?? 0);

    // Zero releases → silent no-op. No change_log rows.
    if (totalReleased === 0) {
      return {
        timeout_minutes: timeoutMinutes,
        released_count_total: 0,
        affected_slugs: [],
        change_log_rows_appended: 0,
      };
    }

    // Group candidates by slug to emit one change_log per affected slug.
    const buckets = new Map();
    function bucketFor(slug) {
      let b = buckets.get(slug);
      if (!b) {
        b = { section_released: [], stage_released: [] };
        buckets.set(slug, b);
      }
      return b;
    }
    for (const row of sectionCandidates.rows) {
      bucketFor(row.slug).section_released.push(row.key);
    }
    for (const row of stageCandidates.rows) {
      bucketFor(row.slug).stage_released.push(row.key);
    }

    let appended = 0;
    for (const [slug, bucket] of buckets) {
      const releasedCount =
        bucket.section_released.length + bucket.stage_released.length;
      const metadata = {
        released_count: releasedCount,
        section_released: bucket.section_released,
        stage_released: bucket.stage_released,
        timeout_minutes: timeoutMinutes,
      };
      // INSERT into ia_master_plan_change_log — mirrors
      // mutateMasterPlanChangeLogAppend (UNIQUE on
      // (slug, stage_id, kind, commit_sha); NULL stage_id + NULL commit_sha
      // are distinct under PG default semantics so repeat ticks always
      // append).
      await pool.query(
        `INSERT INTO ia_master_plan_change_log
           (slug, kind, body, actor)
         VALUES ($1, 'claim_swept', $2, 'claims-sweep-tick')
         ON CONFLICT ON CONSTRAINT ia_master_plan_change_log_unique
         DO NOTHING`,
        [slug, JSON.stringify(metadata)],
      );
      appended += 1;
    }

    return {
      timeout_minutes: timeoutMinutes,
      released_count_total: totalReleased,
      affected_slugs: Array.from(buckets.keys()),
      change_log_rows_appended: appended,
    };
  } finally {
    if (ownsPool) {
      await pool.end();
    }
  }
}

/**
 * Build a fresh pg.Pool from the resolved DATABASE_URL — mirrors
 * `getIaDatabasePool()` shape (max 4, idleTimeoutMillis 10s) without needing
 * the singleton.
 *
 * @returns {import('pg').Pool | null}
 */
function buildPool() {
  const url = resolveDatabaseUrl(REPO_ROOT);
  if (!url) return null;
  return new pg.Pool({
    connectionString: url,
    max: 4,
    idleTimeoutMillis: 10_000,
  });
}

// Self-running guard — only fires when invoked directly via `npm run
// claims:sweep:tick`, not when imported by tests or other modules.
const isMain =
  typeof process.argv[1] === "string" &&
  import.meta.url === pathToFileURL(process.argv[1]).href;

if (isMain) {
  try {
    const result = await runSweepTick();
    // Single-line stderr summary — cron-friendly, no terminal cruft.
    process.stderr.write(
      `claims-sweep-tick: released=${result.released_count_total} ` +
        `slugs=[${result.affected_slugs.join(",")}] ` +
        `change_log_rows=${result.change_log_rows_appended} ` +
        `timeout_min=${result.timeout_minutes}\n`,
    );
    process.exit(0);
  } catch (err) {
    const msg = err instanceof Error ? err.message : String(err);
    process.stderr.write(`claims-sweep-tick: ERROR ${msg}\n`);
    process.exit(1);
  }
}
