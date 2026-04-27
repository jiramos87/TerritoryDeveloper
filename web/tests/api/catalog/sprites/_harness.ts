// Sprite catalog API integration harness (TECH-1675).
// Mirrors render harness pattern: direct-invoke route handlers; truncate spine
// tables between tests; one test user inserted to satisfy actor_user_id FK.

import { NextRequest } from "next/server";
import { getSql } from "@/lib/db/client";

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type RouteHandler = (req: NextRequest, ctx?: any) => Promise<Response>;

export const SPRITE_TEST_USER_ID = "22222222-2222-4222-8222-222222222222";

export async function resetSpriteTables(): Promise<void> {
  const sql = getSql();
  // CASCADE handles entity_version + sprite_detail children of catalog_entity.
  await sql.unsafe("truncate sprite_detail, entity_version, catalog_entity restart identity cascade");
  await sql.unsafe("delete from audit_log where action like 'catalog.sprite.%'");
}

export async function seedSpriteTestUser(): Promise<string> {
  const sql = getSql();
  await sql`
    insert into users (id, email, display_name, role)
    values (${SPRITE_TEST_USER_ID}::uuid, 'sprite-tests@example.com', 'sprite tests', 'admin')
    on conflict (id) do nothing
  `;
  return SPRITE_TEST_USER_ID;
}

export async function invokeSpriteRoute(
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
