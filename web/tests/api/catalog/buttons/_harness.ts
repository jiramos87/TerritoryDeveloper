// Button catalog API integration harness (TECH-1885 / Stage 8.1).
// Mirrors sprite harness pattern: direct-invoke route handlers; truncate spine
// tables between tests; one test user inserted to satisfy actor_user_id FK.

import { NextRequest } from "next/server";
import { getSql } from "@/lib/db/client";

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type RouteHandler = (req: NextRequest, ctx?: any) => Promise<Response>;

export const BUTTON_TEST_USER_ID = "33333333-3333-4333-8333-333333333333";

export async function resetButtonTables(): Promise<void> {
  const sql = getSql();
  // CASCADE handles button_detail + sprite_detail children of catalog_entity.
  await sql.unsafe(
    "truncate button_detail, sprite_detail, entity_version, catalog_entity restart identity cascade",
  );
  await sql.unsafe("delete from audit_log where action like 'catalog.button.%'");
}

export async function seedButtonTestUser(): Promise<string> {
  const sql = getSql();
  await sql`
    insert into users (id, email, display_name, role)
    values (${BUTTON_TEST_USER_ID}::uuid, 'button-tests@example.com', 'button tests', 'admin')
    on conflict (id) do nothing
  `;
  return BUTTON_TEST_USER_ID;
}

/** Insert a sprite catalog_entity directly (bypass sprite POST for fixture speed). */
export async function seedSpriteEntity(slug: string, displayName: string): Promise<string> {
  const sql = getSql();
  const rows = (await sql`
    insert into catalog_entity (kind, slug, display_name)
    values ('sprite', ${slug}, ${displayName})
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  return rows[0]!.id;
}

/** Insert a token catalog_entity directly. */
export async function seedTokenEntity(slug: string, displayName: string): Promise<string> {
  const sql = getSql();
  const rows = (await sql`
    insert into catalog_entity (kind, slug, display_name)
    values ('token', ${slug}, ${displayName})
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  return rows[0]!.id;
}

export async function invokeButtonRoute(
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
