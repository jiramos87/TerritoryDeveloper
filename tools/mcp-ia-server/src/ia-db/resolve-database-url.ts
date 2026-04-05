/**
 * Resolve Postgres URI: DATABASE_URL env first, else repo config/postgres-dev.json (versioned local default).
 */

import fs from "node:fs";
import path from "node:path";
import { resolveRepoRoot } from "../config.js";

const CONFIG_REL = "config/postgres-dev.json";

/**
 * Read `database_url` from committed dev config when present and valid.
 */
export function readFallbackDatabaseUrl(repoRoot: string): string | null {
  const configPath = path.join(repoRoot, CONFIG_REL);
  try {
    const raw = fs.readFileSync(configPath, "utf8");
    const j = JSON.parse(raw) as { database_url?: string };
    const u = j.database_url?.trim();
    return u || null;
  } catch {
    return null;
  }
}

/**
 * Env wins; otherwise `config/postgres-dev.json` under {@link resolveRepoRoot}.
 */
export function resolveIaDatabaseUrl(): string | null {
  const env = process.env.DATABASE_URL?.trim();
  if (env) return env;
  if (process.env.CI === "true" || process.env.GITHUB_ACTIONS === "true") {
    return null;
  }
  return readFallbackDatabaseUrl(resolveRepoRoot());
}
