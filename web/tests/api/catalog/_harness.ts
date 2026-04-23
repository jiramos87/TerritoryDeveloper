// Catalog API integration test harness (TECH-755).
// Direct-invoke route handlers with constructed NextRequest; no dev server.
// DB isolation: TRUNCATE catalog_* tables RESTART IDENTITY CASCADE between tests.
// Migration 0013 seeds seven Zone S rows — re-applied via seedZoneS() after each reset.

import { fileURLToPath } from "node:url";
import { NextRequest } from "next/server";
import { getSql } from "@/lib/db/client";

const HARNESS_DIR = fileURLToPath(new URL(".", import.meta.url));

// Route handlers ship two shapes: list/post (no ctx) vs by-id/retire (ctx.params). Unify via
// loose typing — caller provides ctx iff the handler needs it. Use `any` for ctx to sidestep
// Next's `Promise<{ id: string }>` vs `Promise<Record<string, string>>` variance.
// eslint-disable-next-line @typescript-eslint/no-explicit-any
type RouteHandler = (req: NextRequest, ctx?: any) => Promise<Response>;

// Order matters: children before parents (CASCADE handles it, but explicit is clearer).
// Spawn-pool tables from migration 0012 (not the earlier `zone_s_*` naming in the plan digest).
const CATALOG_TABLES = [
  "catalog_pool_member",
  "catalog_spawn_pool",
  "catalog_asset_sprite",
  "catalog_economy",
  "catalog_sprite",
  "catalog_asset",
] as const;

export async function resetCatalogTables(): Promise<void> {
  const sql = getSql();
  // Hardcoded catalog_* table names — safe for unsafe() interpolation.
  const ident = CATALOG_TABLES.join(", ");
  await sql.unsafe(`truncate ${ident} restart identity cascade`);
}

export async function seedZoneS(): Promise<void> {
  // Re-apply 0013 Zone S seed + one placeholder sprite (for happy-path sprite_bind test).
  // Migration runs pre-test (db:migrate); this re-seeds after resetCatalogTables().
  // Idempotent: on conflict do nothing.
  const sql = getSql();
  // Placeholder sprite id=1 for POST create test sprite_bind.
  await sql`insert into catalog_sprite (id, path, ppu, pivot_x, pivot_y, provenance)
            values (1, 'zone-s/placeholder', 100, 0.5, 0.5, 'hand')
            on conflict (id) do nothing`;
  // Seven Zone S asset rows + economy rows from 0013 migration.
  const fs = await import("node:fs/promises");
  const path = await import("node:path");
  const seedPath = path.resolve(
    HARNESS_DIR,
    "../../../../db/migrations/0013_zone_s_seed.sql",
  );
  const rawSeedSql = await fs.readFile(seedPath, "utf8");
  // `postgres` driver rejects raw BEGIN/COMMIT outside `sql.begin()`. Strip both — the
  // upstream migration wraps its own txn; re-seed here runs ad-hoc.
  // Also truncate `now()` to millisecond precision so updated_at round-trips through
  // JS ISO strings without microsecond drift (shipped patch route compares as string).
  const seedSql = rawSeedSql
    .replace(/^\s*BEGIN\s*;\s*$/gim, "")
    .replace(/^\s*COMMIT\s*;\s*$/gim, "")
    .replace(/now\(\)/g, "date_trunc('milliseconds', now())");
  await sql.unsafe(seedSql);
}

export async function invokeRoute(
  handler: RouteHandler,
  method: "GET" | "POST" | "PATCH",
  url: string,
  body?: unknown,
  params?: Record<string, string>,
): Promise<Response> {
  const init: { method: string; body?: string; headers?: Record<string, string> } = { method };
  if (body !== undefined) {
    init.body = JSON.stringify(body);
    init.headers = { "content-type": "application/json" };
  }
  const req = new NextRequest(new URL(url, "http://localhost"), init);
  const ctx = params ? { params: Promise.resolve(params) } : undefined;
  return handler(req, ctx);
}
