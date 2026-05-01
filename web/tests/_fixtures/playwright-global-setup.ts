// TECH-8609 / Stage 19.1 — Playwright globalSetup. Populates DATABASE_URL +
// NEXT_PUBLIC_AUTH_DEV_FALLBACK from repo-root `.env` / `config/postgres-dev.json`
// so the critical-path spec + the spawned `next dev` server share the same env.
// Mirrors the vitest setup at `tests/api/_vitest-setup.ts` but uses CJS-friendly
// `__dirname` since Playwright transpiles .ts to CommonJS (web/ has no
// `"type": "module"` and `import.meta.url` errors at runtime).

import { existsSync, readFileSync } from "node:fs";
import { join, resolve } from "node:path";

const REPO_ROOT = resolve(__dirname, "../../..");

function isCi(): boolean {
  return process.env.CI === "true" || process.env.GITHUB_ACTIONS === "true";
}

function parseDotenv(filePath: string): Record<string, string> {
  if (!existsSync(filePath)) return {};
  const raw = readFileSync(filePath, "utf8");
  const out: Record<string, string> = {};
  for (let line of raw.split("\n")) {
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
    out[key] = val;
  }
  return out;
}

function readPostgresDevJson(): string | null {
  const configPath = join(REPO_ROOT, "config/postgres-dev.json");
  if (!existsSync(configPath)) return null;
  try {
    const raw = readFileSync(configPath, "utf8");
    const j = JSON.parse(raw) as { database_url?: string };
    const url = typeof j.database_url === "string" ? j.database_url.trim() : "";
    return url || null;
  } catch {
    return null;
  }
}

export default async function globalSetup(): Promise<void> {
  if (!process.env.DATABASE_URL || process.env.DATABASE_URL.trim() === "") {
    if (!isCi()) {
      for (const name of [".env", ".env.local"]) {
        const kv = parseDotenv(join(REPO_ROOT, name));
        if (kv.DATABASE_URL && !process.env.DATABASE_URL) {
          process.env.DATABASE_URL = kv.DATABASE_URL;
        }
      }
      if (!process.env.DATABASE_URL || process.env.DATABASE_URL.trim() === "") {
        const fromJson = readPostgresDevJson();
        if (fromJson) process.env.DATABASE_URL = fromJson;
      }
    }
  }
  // Always set the dev-cookie fallback flag so the proxy honors the cookie
  // injected by `loginAsPlaywrightAdmin`. Required for both the test process
  // and the dev server boot env.
  process.env.NEXT_PUBLIC_AUTH_DEV_FALLBACK = "1";
}
