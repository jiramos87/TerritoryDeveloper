/**
 * Load repo-root `.env` / `.env.local` into `process.env` (Node postgres-ia scripts).
 * Skipped when CI is set. Mirrors tools/mcp-ia-server/src/ia-db/repo-dotenv.ts.
 */

import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';

export function isCiLikeEnvironment() {
  return process.env.CI === 'true' || process.env.GITHUB_ACTIONS === 'true';
}

/**
 * @param {string} repoRoot
 */
export function loadRepoDotenvIfNotCi(repoRoot) {
  if (isCiLikeEnvironment()) return;
  for (const name of ['.env', '.env.local']) {
    const filePath = join(repoRoot, name);
    if (!existsSync(filePath)) continue;
    const text = readFileSync(filePath, 'utf8');
    for (let line of text.split('\n')) {
      line = line.replace(/\r$/, '');
      const t = line.trim();
      if (!t || t.startsWith('#')) continue;
      const eq = t.indexOf('=');
      if (eq <= 0) continue;
      const key = t.slice(0, eq).trim();
      if (!key) continue;
      let val = t.slice(eq + 1).trim();
      if (
        (val.startsWith('"') && val.endsWith('"')) ||
        (val.startsWith("'") && val.endsWith("'"))
      ) {
        val = val.slice(1, -1);
      }
      const cur = process.env[key];
      if (cur === undefined || cur === '') {
        process.env[key] = val;
      }
    }
  }
}
