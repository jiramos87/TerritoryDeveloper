/**
 * orphan-blobs.ts — Stage 18.1 / TECH-8604.
 *
 * Reads the latest GC orphan-blob sweep JSON from
 * `data/state/orphan-blobs/{YYYY-MM-DD}.json` for the dashboard widget.
 * Sweep is produced by `tools/postgres-ia/gc-catalog.ts --mode orphan`.
 */

import { readFileSync, readdirSync } from 'node:fs';
import { join, resolve } from 'node:path';

export interface OrphanBlobReport {
  date: string;
  count: number;
  paths: string[];
}

const REPO_ROOT = resolve(process.cwd());
const ORPHAN_DIR = join(REPO_ROOT, 'data/state/orphan-blobs');

/**
 * Returns the most recent orphan-blob sweep report, or `null` when no sweep
 * has ever run (or `data/state/orphan-blobs/` is missing).
 */
export function loadLatestOrphanBlobReport(): OrphanBlobReport | null {
  let entries: string[];
  try {
    entries = readdirSync(ORPHAN_DIR);
  } catch {
    return null;
  }

  const dated = entries
    .filter((f) => /^\d{4}-\d{2}-\d{2}\.json$/.test(f))
    .sort();
  if (dated.length === 0) return null;

  const latest = dated[dated.length - 1];
  const fullPath = join(ORPHAN_DIR, latest);
  try {
    const raw = readFileSync(fullPath, 'utf8');
    const parsed = JSON.parse(raw) as OrphanBlobReport;
    if (
      typeof parsed.date !== 'string' ||
      typeof parsed.count !== 'number' ||
      !Array.isArray(parsed.paths)
    ) {
      return null;
    }
    return parsed;
  } catch {
    return null;
  }
}
