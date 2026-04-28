/**
 * sweepRetiredSnapshots — disk-hygiene tail of asset-pipeline Stage 13.1
 * (TECH-2676 §Acceptance #1–#3).
 *
 * Deletes per-kind files + `manifest.json` from `Assets/StreamingAssets/catalog/`
 * for every `catalog_snapshot` row where:
 *   - `status = 'retired'`, AND
 *   - `retired_at < now - maxAgeDays`, AND
 *   - no other row with `status = 'active'` references the same `manifest_path`
 *     (active-twin guard — single shared path MVP per spec §2.1 #3).
 *
 * Order of operations per row:
 *   1. Delete per-kind files (`{kind}.json`) — `ENOENT` swallowed for
 *      idempotency (re-runs over already-cleaned disks must not throw).
 *   2. Delete `manifest.json` last so Unity's `FileSystemWatcher` does not
 *      observe a half-populated directory and trigger a parity-fail reload.
 *   3. DELETE the row from `catalog_snapshot`. Only fires after disk delete
 *      succeeds — failure on disk leaves the row in DB so the next sweep
 *      retries cleanly (no orphan files).
 *
 * Active-twin guard: when another active row references the same
 * `manifest_path`, the disk delete is skipped entirely and only the DB row
 * is removed. The active twin keeps the loader-visible files alive.
 *
 * @see ia/projects/asset-pipeline/stage-13.1 — TECH-2676 §Plan Digest
 */

import { promises as fsp } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

import { getSql } from "@/lib/db/client";
import { SNAPSHOT_KINDS } from "@/lib/snapshot/manifest";

/** Default retention window: 7 days (spec §1 + §2.1 #1). */
export const DEFAULT_MAX_AGE_DAYS = 7;

/**
 * Repo root resolved relative to this file (`web/lib/snapshot/gc-sweep.ts` →
 * three `..` segments). Mirrors `export.ts` so tests can override via
 * `repoRootOverride`.
 */
const HERE_DIR = path.dirname(fileURLToPath(import.meta.url));
const DEFAULT_REPO_ROOT = path.resolve(HERE_DIR, "..", "..", "..");

export type SweepResult = {
  removedCount: number;
  removedIds: string[];
};

export type SweepOptions = {
  /**
   * Override the resolved repo root used to locate
   * `Assets/StreamingAssets/catalog/{kind}.json`. Tests pass a temp dir.
   */
  repoRootOverride?: string;
};

type RetiredRow = {
  id: string;
  manifest_path: string;
  active_twin_count: string; // postgres bigint → string
};

/**
 * Run the sweep over all eligible retired rows. Idempotent — re-runs over
 * a clean DB return `{ removedCount: 0, removedIds: [] }` without touching
 * disk.
 */
export async function sweepRetiredSnapshots(
  now: Date,
  maxAgeDays: number = DEFAULT_MAX_AGE_DAYS,
  options: SweepOptions = {},
): Promise<SweepResult> {
  const sql = getSql();
  const repoRoot = options.repoRootOverride ?? DEFAULT_REPO_ROOT;
  const cutoff = new Date(now.getTime() - maxAgeDays * 24 * 60 * 60 * 1000);

  // Single read: candidate rows + active-twin presence flag in one query.
  // `manifest_path` directory derived in JS so we don't ship a stored proc.
  const candidates = (await sql`
    select
      s.id::text          as id,
      s.manifest_path     as manifest_path,
      (
        select count(*)::text
        from catalog_snapshot t
        where t.status = 'active'
          and t.manifest_path = s.manifest_path
      ) as active_twin_count
    from catalog_snapshot s
    where s.status = 'retired'
      and s.retired_at is not null
      and s.retired_at < ${cutoff.toISOString()}::timestamptz
    order by s.retired_at asc
  `) as unknown as RetiredRow[];

  if (candidates.length === 0) {
    return { removedCount: 0, removedIds: [] };
  }

  const removedIds: string[] = [];
  for (const row of candidates) {
    const hasActiveTwin = Number(row.active_twin_count) > 0;
    if (!hasActiveTwin) {
      // Resolve catalog dir from manifest_path (`.../catalog/manifest.json`).
      const catalogDir = path.dirname(path.join(repoRoot, row.manifest_path));
      // Per-kind files first; manifest last (avoids torn-state reload trigger).
      for (const kind of SNAPSHOT_KINDS) {
        await unlinkIgnoringMissing(path.join(catalogDir, `${kind}.json`));
      }
      await unlinkIgnoringMissing(path.join(catalogDir, "manifest.json"));
    }

    // Disk delete succeeded (or was skipped under active-twin guard) → remove
    // the row. Failure here would leave orphan disk in the no-twin path; the
    // try/catch upstream of `sweepRetiredSnapshots` is the operator's tool.
    await sql`delete from catalog_snapshot where id = ${row.id}::uuid`;
    removedIds.push(row.id);
  }

  return { removedCount: removedIds.length, removedIds };
}

/**
 * `fs.unlink` wrapper that swallows `ENOENT` so the sweep is idempotent over
 * disks where the file was already removed by a prior run or manual cleanup.
 * Other errors propagate so the operator can investigate.
 */
async function unlinkIgnoringMissing(target: string): Promise<void> {
  try {
    await fsp.unlink(target);
  } catch (e) {
    const err = e as NodeJS.ErrnoException;
    if (err && err.code === "ENOENT") return;
    throw e;
  }
}
