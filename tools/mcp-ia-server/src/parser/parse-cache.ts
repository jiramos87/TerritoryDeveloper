/**
 * On-disk mtime-keyed parse cache (B4 — TECH-495).
 *
 * Stores parsed `ParsedDocument` entries keyed by resolved file path + source
 * mtime (ms). Hit returns the entry, miss returns null. Callers are expected
 * to reparse on miss and call {@link writeCached} to persist.
 *
 * Non-goals:
 * - Content-hash validation (mtime is sufficient for local dev-loop usage).
 * - Cross-process locking (single-process MCP server; concurrent writes are
 *   idempotent — last-writer-wins on identical content).
 */
import fs from "node:fs";
import path from "node:path";
import type { ParsedDocument } from "./types.js";

const CACHE_DIR = "tools/mcp-ia-server/.cache";
const CACHE_FILE = "parse-cache.json";

interface CacheEntry {
  mtimeMs: number;
  doc: ParsedDocument;
}

interface CacheFile {
  version: 1;
  entries: Record<string, CacheEntry>;
}

let loadedCache: CacheFile | null = null;
let cacheFilePath: string | null = null;
let cacheDirty = false;

function resolveCacheFile(repoRoot: string): string {
  if (cacheFilePath) return cacheFilePath;
  cacheFilePath = path.join(repoRoot, CACHE_DIR, CACHE_FILE);
  return cacheFilePath;
}

function loadCache(repoRoot: string): CacheFile {
  if (loadedCache) return loadedCache;
  const file = resolveCacheFile(repoRoot);
  try {
    if (fs.existsSync(file)) {
      const raw = fs.readFileSync(file, "utf8");
      const parsed = JSON.parse(raw) as CacheFile;
      if (parsed && parsed.version === 1 && parsed.entries) {
        loadedCache = parsed;
        return parsed;
      }
    }
  } catch {
    // Corrupt cache — discard + start fresh. Non-fatal.
  }
  loadedCache = { version: 1, entries: {} };
  return loadedCache;
}

/**
 * Reset in-memory cache handles (tests only).
 */
export function resetParseCacheState(): void {
  loadedCache = null;
  cacheFilePath = null;
  cacheDirty = false;
}

/**
 * Read cached {@link ParsedDocument} for an absolute path at the given mtime.
 * Returns null on miss, corrupt entry, or mtime mismatch.
 */
export function readCached(
  repoRoot: string,
  absPath: string,
  mtimeMs: number,
): ParsedDocument | null {
  const cache = loadCache(repoRoot);
  const entry = cache.entries[absPath];
  if (!entry) return null;
  if (entry.mtimeMs !== mtimeMs) return null;
  return entry.doc;
}

/**
 * Persist a parsed document into the cache. Write-through to disk is
 * deferred to {@link flushParseCache} so batched parses do not thrash I/O.
 */
export function writeCached(
  repoRoot: string,
  absPath: string,
  mtimeMs: number,
  doc: ParsedDocument,
): void {
  const cache = loadCache(repoRoot);
  cache.entries[absPath] = { mtimeMs, doc };
  cacheDirty = true;
}

/**
 * Flush dirty cache entries to disk. No-op when clean.
 */
export function flushParseCache(repoRoot: string): void {
  if (!cacheDirty || !loadedCache) return;
  const file = resolveCacheFile(repoRoot);
  try {
    fs.mkdirSync(path.dirname(file), { recursive: true });
    fs.writeFileSync(file, JSON.stringify(loadedCache), "utf8");
    cacheDirty = false;
  } catch {
    // Cache write failures are non-fatal — next session reparses.
  }
}

/**
 * Resolve the repo root for cache placement. Prefers REPO_ROOT env, falls
 * back to cwd.
 */
export function resolveRepoRoot(): string {
  const envRoot = process.env.REPO_ROOT;
  if (envRoot && envRoot.trim().length > 0) {
    return path.resolve(envRoot);
  }
  return process.cwd();
}
