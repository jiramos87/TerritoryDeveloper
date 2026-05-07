/**
 * cron-server — async job supervisor.
 *
 * Boots node-cron, schedules one job per queue kind.
 * Each tick claims a FIFO batch, dispatches to the kind handler,
 * then marks rows done/failed.
 *
 * Boot: npx tsx tools/cron-server/index.ts
 * Boot via workspace: pnpm --filter cron-server dev
 */

import cron from "node-cron";
import { claimBatch, markDone, markFailed } from "./lib/index.js";
import { run as runAuditLog } from "./handlers/audit-log-cron-handler.js";
import { run as runJournalAppend } from "./handlers/journal-append-cron-handler.js";
import { run as runTaskCommitRecord } from "./handlers/task-commit-record-cron-handler.js";
import { run as runStageVerificationFlip } from "./handlers/stage-verification-flip-cron-handler.js";
import { run as runArchChangelogAppend } from "./handlers/arch-changelog-append-cron-handler.js";
import { run as runMaterializeBacklog } from "./handlers/materialize-backlog-cron-handler.js";
import { run as runRegenIndexes } from "./handlers/regen-indexes-cron-handler.js";
import type { AuditLogJobRow } from "./handlers/audit-log-cron-handler.js";
import type { JournalAppendJobRow } from "./handlers/journal-append-cron-handler.js";
import type { TaskCommitRecordJobRow } from "./handlers/task-commit-record-cron-handler.js";
import type { StageVerificationFlipJobRow } from "./handlers/stage-verification-flip-cron-handler.js";
import type { ArchChangelogAppendJobRow } from "./handlers/arch-changelog-append-cron-handler.js";
import type { MaterializeBacklogJobRow } from "./handlers/materialize-backlog-cron-handler.js";
import type { RegenIndexesJobRow } from "./handlers/regen-indexes-cron-handler.js";

// Load DATABASE_URL from .env if not already set.
const { config } = await import("dotenv");
config({ path: new URL("../../.env", import.meta.url).pathname });

// ---------------------------------------------------------------------------
// Per-kind configuration
// ---------------------------------------------------------------------------

interface KindConfig {
  table: string;
  cadence: string; // cron expression
  handler: (row: Record<string, unknown>) => Promise<void>;
  claimLimit: number;
}

const KINDS: KindConfig[] = [
  {
    table: "cron_audit_log_jobs",
    cadence: "* * * * *", // every minute
    handler: (row) => runAuditLog(row as unknown as AuditLogJobRow),
    claimLimit: 50,
  },
  {
    table: "cron_journal_append_jobs",
    cadence: "* * * * *", // every minute
    handler: (row) => runJournalAppend(row as unknown as JournalAppendJobRow),
    claimLimit: 50,
  },
  {
    table: "cron_task_commit_record_jobs",
    cadence: "* * * * *", // every minute (hot audit-trail)
    handler: (row) => runTaskCommitRecord(row as unknown as TaskCommitRecordJobRow),
    claimLimit: 50,
  },
  {
    table: "cron_stage_verification_flip_jobs",
    cadence: "* * * * *", // every minute (hot audit-trail)
    handler: (row) => runStageVerificationFlip(row as unknown as StageVerificationFlipJobRow),
    claimLimit: 50,
  },
  {
    table: "cron_arch_changelog_append_jobs",
    cadence: "* * * * *", // every minute (hot audit-trail)
    handler: (row) => runArchChangelogAppend(row as unknown as ArchChangelogAppendJobRow),
    claimLimit: 50,
  },
  {
    table: "cron_materialize_backlog_jobs",
    cadence: "*/2 * * * *", // every 2 min — heavy script, low concurrency
    handler: (row) => runMaterializeBacklog(row as unknown as MaterializeBacklogJobRow),
    claimLimit: 1,
  },
  {
    table: "cron_regen_indexes_jobs",
    cadence: "*/5 * * * *", // every 5 min — slower, index regen
    handler: (row) => runRegenIndexes(row as unknown as RegenIndexesJobRow),
    claimLimit: 1,
  },
];

// ---------------------------------------------------------------------------
// Supervisor tick
// ---------------------------------------------------------------------------

async function tick(kind: KindConfig): Promise<void> {
  let rows: Awaited<ReturnType<typeof claimBatch>>;
  try {
    rows = await claimBatch(kind.table, kind.claimLimit);
  } catch (e) {
    console.error(`[cron] tick: ${kind.table} claim error`, e);
    return;
  }

  console.log(`[cron] tick: ${kind.table} claimed=${rows.length}`);

  for (const row of rows) {
    const jobId = row["job_id"] as string;
    try {
      await kind.handler(row as Record<string, unknown>);
      await markDone(kind.table, jobId);
    } catch (e) {
      const errMsg = e instanceof Error ? e.message : String(e);
      console.error(`[cron] ${kind.table} job_id=${jobId} failed: ${errMsg}`);
      await markFailed(kind.table, jobId, errMsg);
    }
  }
}

// ---------------------------------------------------------------------------
// Boot
// ---------------------------------------------------------------------------

for (const kind of KINDS) {
  cron.schedule(kind.cadence, () => {
    void tick(kind);
  });
  console.log(`[cron] registered kind=${kind.table} cadence="${kind.cadence}"`);
}

console.log("[cron] supervisor running. Ctrl+C to stop.");
