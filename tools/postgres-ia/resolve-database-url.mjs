/**
 * Resolve Postgres connection URI: DATABASE_URL env first, else config/postgres-dev.json at repo root.
 */

import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';

const CONFIG_REL = 'config/postgres-dev.json';

/**
 * @param {string} repoRoot - Absolute path to repository root
 * @returns {string | null}
 */
export function resolveDatabaseUrl(repoRoot) {
  const env = process.env.DATABASE_URL?.trim();
  if (env) return env;

  if (process.env.CI === 'true' || process.env.GITHUB_ACTIONS === 'true') {
    return null;
  }

  const configPath = join(repoRoot, CONFIG_REL);
  if (!existsSync(configPath)) return null;

  try {
    const raw = readFileSync(configPath, 'utf8');
    const j = JSON.parse(raw);
    const url =
      typeof j.database_url === 'string' ? j.database_url.trim() : '';
    return url || null;
  } catch {
    return null;
  }
}
