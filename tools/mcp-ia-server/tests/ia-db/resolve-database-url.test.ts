import assert from "node:assert/strict";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { describe, it } from "node:test";
import {
  readFallbackDatabaseUrl,
  resolveIaDatabaseUrl,
} from "../../src/ia-db/resolve-database-url.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "../../../..");

describe("readFallbackDatabaseUrl", () => {
  it("reads database_url from committed config when present", () => {
    const url = readFallbackDatabaseUrl(REPO_ROOT);
    assert.ok(url?.startsWith("postgresql://"));
    assert.ok(url?.includes("territory_ia_dev"));
  });
});

describe("resolveIaDatabaseUrl", () => {
  it("returns null when CI is set even without DATABASE_URL", async () => {
    const prevCi = process.env.CI;
    const prevDb = process.env.DATABASE_URL;
    process.env.CI = "true";
    delete process.env.DATABASE_URL;
    const { resolveIaDatabaseUrl: resolveFresh } = await import(
      "../../src/ia-db/resolve-database-url.js"
    );
    assert.equal(resolveFresh(), null);
    if (prevCi === undefined) delete process.env.CI;
    else process.env.CI = prevCi;
    if (prevDb === undefined) delete process.env.DATABASE_URL;
    else process.env.DATABASE_URL = prevDb;
  });
});
