// TECH-8609 / Stage 19.1 — Idempotent admin user seed for Playwright
// critical-path. Mirrors the per-kind harness pattern; UUID extends the
// existing 5/6/7/8-prefix sequence with `aaaaaaaa…`.

import { getSql } from "@/lib/db/client";

export const PLAYWRIGHT_ADMIN_USER_ID = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
export const PLAYWRIGHT_ADMIN_EMAIL = "playwright-admin@example.com";

export async function seedPlaywrightAdmin(): Promise<string> {
  const sql = getSql();
  await sql`
    insert into users (id, email, display_name, role)
    values (
      ${PLAYWRIGHT_ADMIN_USER_ID}::uuid,
      ${PLAYWRIGHT_ADMIN_EMAIL},
      'playwright admin',
      'admin'
    )
    on conflict (id) do nothing
  `;
  return PLAYWRIGHT_ADMIN_USER_ID;
}
