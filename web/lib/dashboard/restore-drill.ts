/**
 * restore-drill.ts — Stage 18.1 / TECH-8605.
 *
 * Reads the latest DR drill outcome JSON from
 * `data/state/restore-drill/{YYYY-MM-DD}.json` for the dashboard banner.
 * Drill is produced by `tools/scripts/verify-db-restore.sh`.
 */

import { readFileSync, readdirSync } from 'node:fs';
import { join, resolve } from 'node:path';

export interface RestoreDrillReport {
  date: string;
  ok: boolean;
  reason: string;
  dump_path: string | null;
  ephemeral_db?: string;
  latency_ms: number;
  restore_exit?: number;
  validate_exit: number | null;
}

export interface RestoreDrillStatus {
  state: 'ok' | 'failed' | 'stale' | 'never_run';
  report: RestoreDrillReport | null;
  ageDays: number | null;
}

const REPO_ROOT = resolve(process.cwd());
const DRILL_DIR = join(REPO_ROOT, 'data/state/restore-drill');
const STALE_THRESHOLD_DAYS = 35;

/**
 * Returns drill status for the dashboard banner.
 *  - `ok`         — latest report ok=true AND within 35 days
 *  - `failed`     — latest report ok=false (any age)
 *  - `stale`      — latest report ok=true but > 35 days old
 *  - `never_run`  — no reports ever
 */
export function loadLatestRestoreDrillStatus(): RestoreDrillStatus {
  let entries: string[];
  try {
    entries = readdirSync(DRILL_DIR);
  } catch {
    return { state: 'never_run', report: null, ageDays: null };
  }

  const dated = entries
    .filter((f) => /^\d{4}-\d{2}-\d{2}\.json$/.test(f))
    .sort();
  if (dated.length === 0) {
    return { state: 'never_run', report: null, ageDays: null };
  }

  const latest = dated[dated.length - 1];
  const fullPath = join(DRILL_DIR, latest);
  let report: RestoreDrillReport;
  try {
    const raw = readFileSync(fullPath, 'utf8');
    report = JSON.parse(raw) as RestoreDrillReport;
  } catch {
    return { state: 'never_run', report: null, ageDays: null };
  }

  const reportDate = new Date(`${report.date}T00:00:00Z`);
  const nowMs = Date.now();
  const ageDays = Math.floor((nowMs - reportDate.getTime()) / (1000 * 60 * 60 * 24));

  if (!report.ok) {
    return { state: 'failed', report, ageDays };
  }
  if (ageDays > STALE_THRESHOLD_DAYS) {
    return { state: 'stale', report, ageDays };
  }
  return { state: 'ok', report, ageDays };
}
