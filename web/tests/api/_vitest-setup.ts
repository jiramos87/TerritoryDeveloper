// Vitest setup for API integration tests (TECH-755).
// Populates process.env.DATABASE_URL from repo-root `.env` or `config/postgres-dev.json`
// so tests that import `@/lib/db/client` (getSql) work without a shell-level env export.
// CI sets DATABASE_URL via secrets; local dev relies on this fallback chain.

import { existsSync, readFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const HERE = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = resolve(HERE, "../../..");

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

if (!process.env.DATABASE_URL || process.env.DATABASE_URL.trim() === "") {
  if (!isCi()) {
    // .env first (committed pattern at repo root), then .env.local, then config fallback.
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
