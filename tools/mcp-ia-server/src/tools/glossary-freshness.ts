/**
 * Freshness helper for glossary-graph-index.json.
 *
 * Exports:
 *   getGraphFreshness() — async, returns { graph_generated_at?, graph_stale }
 *   spawnGraphRegen()   — detached child spawn of `npm run build:glossary-graph`; busts cache
 *   clearFreshnessCache() — for unit tests
 *
 * Cache: process-lifetime `{ mtime, checked_at }` tuple; re-stat only when
 * entry is older than CACHE_TTL_MS (60 s). Avoids hot-loop syscall churn.
 *
 * Stale threshold: GLOSSARY_GRAPH_STALE_DAYS env (integer, default 14).
 * Non-numeric value → parseInt NaN → fallback to 14.
 *
 * Missing graph index → graph_stale: true, graph_generated_at: undefined (non-fatal).
 *
 * Spawn pattern: spawn(..., { detached: true, stdio: "ignore" }).unref()
 * — exact three-piece form required; deviation leaks fds or blocks event loop.
 */

import { promises as fs } from "node:fs";
import { spawn } from "node:child_process";
import type { SpawnOptions } from "node:child_process";
import path from "node:path";
import { resolveRepoRoot } from "../config.js";

const CACHE_TTL_MS = 60_000;

let cache: { mtime: Date; checked_at: number } | null = null;

// Indirection layer: tests override via _setSpawnFn() to avoid
// "Cannot redefine property: spawn" on the named ESM export.
type SpawnFn = (
  command: string,
  args: string[],
  options: SpawnOptions & { detached: boolean; stdio: "ignore" },
) => { unref(): void };

let _spawnFn: SpawnFn = spawn as unknown as SpawnFn;

function graphIndexPath(): string {
  return path.join(
    resolveRepoRoot(),
    "tools",
    "mcp-ia-server",
    "data",
    "glossary-graph-index.json",
  );
}

function staleDaysThresholdMs(): number {
  const raw = parseInt(process.env.GLOSSARY_GRAPH_STALE_DAYS ?? "14", 10);
  const days = Number.isFinite(raw) && !Number.isNaN(raw) ? raw : 14;
  return days * 86_400_000;
}

export async function getGraphFreshness(): Promise<{
  graph_generated_at?: string;
  graph_stale: boolean;
}> {
  const now = Date.now();
  const thresholdMs = staleDaysThresholdMs();

  if (!cache || now - cache.checked_at > CACHE_TTL_MS) {
    try {
      const st = await fs.stat(graphIndexPath());
      cache = { mtime: st.mtime, checked_at: now };
    } catch {
      // Missing file or permission error — non-fatal; report stale.
      return { graph_stale: true };
    }
  }

  return {
    graph_generated_at: cache.mtime.toISOString(),
    graph_stale: cache.mtime.getTime() < now - thresholdMs,
  };
}

export function spawnGraphRegen(): void {
  // Bust cache synchronously so the NEXT getGraphFreshness() call re-stats disk.
  cache = null;
  const child = _spawnFn("npm", ["run", "build:glossary-graph"], {
    detached: true,
    stdio: "ignore",
    cwd: path.join(resolveRepoRoot(), "tools", "mcp-ia-server"),
  });
  child.unref();
}

/** Reset process-lifetime cache — for unit tests only. */
export function clearFreshnessCache(): void {
  cache = null;
}

/**
 * Override the spawn function used by spawnGraphRegen — for unit tests only.
 * Pass undefined / null to restore the real spawn.
 */
export function _setSpawnFn(fn: SpawnFn | null | undefined): void {
  _spawnFn = fn ?? (spawn as unknown as SpawnFn);
}
