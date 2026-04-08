/**
 * Resolve Postgres connection URI: `.env` / `.env.local` (local), DATABASE_URL, config/postgres-dev.json,
 * else default URI (same as committed postgres-dev.json).
 */

import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { loadRepoDotenvIfNotCi } from './repo-dotenv.mjs';

const CONFIG_REL = 'config/postgres-dev.json';

/** Keep in sync with `config/postgres-dev.json` `database_url`. */
export const DEFAULT_DEV_IA_DATABASE_URL =
  'postgresql://postgres:postgres@localhost:5434/territory_ia_dev';

/**
 * @param {string} repoRoot - Absolute path to repository root
 * @returns {string | null}
 */
export function resolveDatabaseUrl(repoRoot) {
  loadRepoDotenvIfNotCi(repoRoot);

  const env = process.env.DATABASE_URL?.trim();
  if (env) return env;

  if (process.env.CI === 'true' || process.env.GITHUB_ACTIONS === 'true') {
    return null;
  }

  const configPath = join(repoRoot, CONFIG_REL);
  if (existsSync(configPath)) {
    try {
      const raw = readFileSync(configPath, 'utf8');
      const j = JSON.parse(raw);
      const url =
        typeof j.database_url === 'string' ? j.database_url.trim() : '';
      if (url) return url;
    } catch {
      /* fall through to default */
    }
  }

  return DEFAULT_DEV_IA_DATABASE_URL;
}
