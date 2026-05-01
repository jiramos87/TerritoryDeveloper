// Archetype catalog API integration harness (TECH-8608 / Stage 19.1).
// Mirrors button/sprite harness pattern: direct-invoke route handlers;
// truncate spine tables between tests; one test user inserted to satisfy
// actor_user_id FK.

import { NextRequest } from "next/server";
import { getSql } from "@/lib/db/client";

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type RouteHandler = (req: NextRequest, ctx?: any) => Promise<Response>;

export const ARCHETYPE_TEST_USER_ID = "88888888-8888-4888-8888-888888888888";

export async function resetArchetypeTables(): Promise<void> {
  const sql = getSql();
  // CASCADE handles entity_version + archetype-versioned children.
  await sql.unsafe(
    "truncate entity_version, catalog_entity restart identity cascade",
  );
  await sql.unsafe(
    "delete from audit_log where action like 'catalog.archetype.%'",
  );
}

export async function seedArchetypeTestUser(): Promise<string> {
  const sql = getSql();
  await sql`
    insert into users (id, email, display_name, role)
    values (${ARCHETYPE_TEST_USER_ID}::uuid, 'archetype-tests@example.com', 'archetype tests', 'admin')
    on conflict (id) do nothing
  `;
  return ARCHETYPE_TEST_USER_ID;
}

export async function invokeArchetypeRoute(
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
