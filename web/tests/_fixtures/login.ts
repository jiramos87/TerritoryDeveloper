// TECH-8609 / Stage 19.1 — Programmatic login helper for Playwright
// critical-path. Uses the proxy's `NEXT_PUBLIC_AUTH_DEV_FALLBACK=1` path
// (cookie `dev_user_id`) — bypasses NextAuth JWT signing without forging a
// signed token. Test process must run with `NEXT_PUBLIC_AUTH_DEV_FALLBACK=1`
// in env so the proxy honors the dev cookie.

import type { BrowserContext } from "@playwright/test";

import { PLAYWRIGHT_ADMIN_USER_ID } from "./seed-admin";

const DEFAULT_BASE_URL =
  process.env.PLAYWRIGHT_BASE_URL ?? "http://localhost:4000";

export async function loginAsPlaywrightAdmin(
  context: BrowserContext,
  baseUrl: string = DEFAULT_BASE_URL,
): Promise<void> {
  const url = new URL(baseUrl);
  await context.addCookies([
    {
      name: "dev_user_id",
      value: PLAYWRIGHT_ADMIN_USER_ID,
      domain: url.hostname,
      path: "/",
      httpOnly: false,
      secure: false,
      sameSite: "Lax",
    },
  ]);
}
