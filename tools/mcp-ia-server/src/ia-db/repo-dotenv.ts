/**
 * Load repo-root `.env` / `.env.local` into `process.env` for local tooling.
 * Skipped in CI so job-injected secrets and empty DATABASE_URL behavior stay predictable.
 */

import fs from "node:fs";
import path from "node:path";

export function isCiLikeEnvironment(): boolean {
  return process.env.CI === "true" || process.env.GITHUB_ACTIONS === "true";
}

/**
 * Parse `KEY=value` lines; assigns only when the key is unset or empty in `process.env`
 * (non-empty shell exports win).
 */
export function loadRepoDotenvIfNotCi(repoRoot: string): void {
  if (isCiLikeEnvironment()) return;
  for (const name of [".env", ".env.local"] as const) {
    const filePath = path.join(repoRoot, name);
    if (!fs.existsSync(filePath)) continue;
    const text = fs.readFileSync(filePath, "utf8");
    for (let line of text.split("\n")) {
      line = line.replace(/\r$/, "");
      const t = line.trim();
      if (!t || t.startsWith("#")) continue;
      const eq = t.indexOf("=");
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
      if (cur === undefined || cur === "") {
        process.env[key] = val;
      }
    }
  }
}
