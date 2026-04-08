/**
 * Resolve Postgres URI: repo `.env` / `.env.local` (local only), then DATABASE_URL,
 * else `config/postgres-dev.json`, else the committed dev default (matches postgres-dev.json).
 */

import fs from "node:fs";
import path from "node:path";
import { resolveRepoRoot } from "../config.js";
import { loadRepoDotenvIfNotCi } from "./repo-dotenv.js";

const CONFIG_REL = "config/postgres-dev.json";

/** Keep in sync with `config/postgres-dev.json` `database_url` (last-resort fallback). */
export const DEFAULT_DEV_IA_DATABASE_URL =
  "postgresql://postgres:postgres@localhost:5434/territory_ia_dev";

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
 * Loads repo dotenv when not CI, then DATABASE_URL, then JSON config, then {@link DEFAULT_DEV_IA_DATABASE_URL}.
 */
export function resolveIaDatabaseUrl(): string | null {
  const repoRoot = resolveRepoRoot();
  loadRepoDotenvIfNotCi(repoRoot);

  const env = process.env.DATABASE_URL?.trim();
  if (env) return env;
  if (process.env.CI === "true" || process.env.GITHUB_ACTIONS === "true") {
    return null;
  }
  return readFallbackDatabaseUrl(repoRoot) ?? DEFAULT_DEV_IA_DATABASE_URL;
}
