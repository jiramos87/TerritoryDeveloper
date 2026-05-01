// Pool catalog API integration harness (TECH-8608 / Stage 19.1).
// Mirrors button/sprite harness pattern: direct-invoke route handlers;
// truncate spine tables between tests; one test user inserted to satisfy
// actor_user_id FK.

import { NextRequest } from "next/server";
import { getSql } from "@/lib/db/client";

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type RouteHandler = (req: NextRequest, ctx?: any) => Promise<Response>;

export const POOL_TEST_USER_ID = "77777777-7777-4777-8777-777777777777";

export async function resetPoolTables(): Promise<void> {
  const sql = getSql();
  // CASCADE handles pool_member + pool_detail + asset_detail children.
  await sql.unsafe(
    "truncate pool_member, pool_detail, economy_detail, asset_detail, entity_version, catalog_entity restart identity cascade",
  );
  await sql.unsafe("delete from audit_log where action like 'catalog.pool.%'");
}

export async function seedPoolTestUser(): Promise<string> {
  const sql = getSql();
  await sql`
    insert into users (id, email, display_name, role)
    values (${POOL_TEST_USER_ID}::uuid, 'pool-tests@example.com', 'pool tests', 'admin')
    on conflict (id) do nothing
  `;
  return POOL_TEST_USER_ID;
}

/** Insert an asset entity + minimal asset_detail + economy_detail (for pool_member FK). */
export async function seedAssetForPool(
  slug: string,
  displayName: string,
): Promise<string> {
  const sql = getSql();
  const inserted = (await sql`
    insert into catalog_entity (kind, slug, display_name)
    values ('asset', ${slug}, ${displayName})
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  const idStr = inserted[0]!.id;
  const idNum = Number.parseInt(idStr, 10);
  await sql`
    insert into asset_detail (entity_id, category, footprint_w, footprint_h)
    values (${idNum}, 'residential', 1, 1)
  `;
  await sql`
    insert into economy_detail (entity_id, base_cost_cents, monthly_upkeep_cents,
                                demolition_refund_pct, construction_ticks)
    values (${idNum}, 0, 0, 0, 0)
  `;
  return idStr;
}

export async function invokePoolRoute(
  handler: RouteHandler,
  method: "GET" | "POST" | "PATCH" | "DELETE",
  url: string,
  init?: { body?: unknown; headers?: Record<string, string>; params?: Record<string, string> },
): Promise<Response> {
  const reqInit: { method: string; body?: string; headers?: Record<string, string> } = { method };
  const headers: Record<string, string> = { ...(init?.headers ?? {}) };
  if (init?.body !== undefined) {
    reqInit.body = JSON.stringify(init.body);
    headers["content-type"] = "application/json";
  }
  if (Object.keys(headers).length > 0) reqInit.headers = headers;
  const req = new NextRequest(new URL(url, "http://localhost"), reqInit);
  const ctx = init?.params ? { params: Promise.resolve(init.params) } : undefined;
  return handler(req, ctx);
}
