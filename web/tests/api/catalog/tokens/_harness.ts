// Token catalog API integration harness (TECH-2092 / Stage 10.1).
// Mirrors button harness pattern: direct-invoke route handlers; truncate
// catalog tables between tests; one test user inserted to satisfy actor_user_id FK.

import { NextRequest } from "next/server";
import { getSql } from "@/lib/db/client";

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type RouteHandler = (req: NextRequest, ctx?: any) => Promise<Response>;

export const TOKEN_TEST_USER_ID = "44444444-4444-4444-8444-444444444444";

export async function resetTokenTables(): Promise<void> {
  const sql = getSql();
  // CASCADE handles token_detail child of catalog_entity.
  await sql.unsafe(
    "truncate token_detail, button_detail, sprite_detail, entity_version, catalog_entity restart identity cascade",
  );
  await sql.unsafe("delete from audit_log where action like 'catalog.token.%'");
}

export async function seedTokenTestUser(): Promise<string> {
  const sql = getSql();
  await sql`
    insert into users (id, email, display_name, role)
    values (${TOKEN_TEST_USER_ID}::uuid, 'token-tests@example.com', 'token tests', 'admin')
    on conflict (id) do nothing
  `;
  return TOKEN_TEST_USER_ID;
}

/** Insert a token catalog_entity directly without detail row (for FK target). */
export async function seedBareTokenEntity(slug: string, displayName: string): Promise<string> {
  const sql = getSql();
  const rows = (await sql`
    insert into catalog_entity (kind, slug, display_name)
    values ('token', ${slug}, ${displayName})
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  return rows[0]!.id;
}

export async function invokeTokenRoute(
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
