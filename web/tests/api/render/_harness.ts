// Render API integration harness (TECH-1469 / TECH-1470).
// Mirrors the catalog harness pattern: direct-invoke route handlers with a
// constructed NextRequest; no dev server. DB isolation via TRUNCATE between
// tests; one test user inserted to satisfy `actor_user_id` FK.

import { NextRequest } from "next/server";
import { getSql } from "@/lib/db/client";

// Loose ctx typing — handler signatures vary (collection POST takes only
// req; by-id GET / replay / identical take a `Promise<{job_id|run_id}>` ctx).
// eslint-disable-next-line @typescript-eslint/no-explicit-any
type RouteHandler = (req: NextRequest, ctx?: any) => Promise<Response>;

const TEST_USER_ID = "11111111-1111-4111-8111-111111111111";

export async function resetRenderTables(): Promise<void> {
  const sql = getSql();
  // job_queue + render_run are independent of catalog_*; truncate keeps
  // tests deterministic. audit_log is truncated separately so other suites
  // sharing the DB do not get clobbered mid-run.
  await sql.unsafe("truncate render_run, job_queue restart identity cascade");
  await sql.unsafe(
    "delete from audit_log where action like 'render.run.%'",
  );
}

export async function seedTestUser(): Promise<string> {
  const sql = getSql();
  await sql`
    insert into users (id, email, display_name, role)
    values (${TEST_USER_ID}::uuid, 'render-tests@example.com', 'render tests', 'admin')
    on conflict (id) do nothing
  `;
  return TEST_USER_ID;
}

export async function invokeRender(
  handler: RouteHandler,
  method: "GET" | "POST",
  url: string,
  init?: { body?: unknown; headers?: Record<string, string>; params?: Record<string, string> },
): Promise<Response> {
  const reqInit: { method: string; body?: string; headers?: Record<string, string> } = {
    method,
  };
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

export const RENDER_TEST_USER_ID = TEST_USER_ID;
