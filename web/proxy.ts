import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';

/** Stage 5.1 TECH-253 archived Decision Log lock — see orchestrator §Orchestrator Decision Log (2026-04-16). */
const SESSION_COOKIE_NAME = 'portal_session';

export function proxy(request: NextRequest): NextResponse {
  // Local-dev bypass — inlined at build from web/.env.local; unset on Vercel.
  if (process.env.DASHBOARD_AUTH_SKIP === '1') {
    return NextResponse.next();
  }
  const cookie = request.cookies.get(SESSION_COOKIE_NAME);
  if (!cookie || cookie.value === '') {
    return NextResponse.redirect(new URL('/auth/login', request.url));
  }
  return NextResponse.next();
}

// /dashboard/releases/:releaseId/rollout — reserved; no filesystem stub (404s via Next default).
export const config = { matcher: ['/dashboard', '/dashboard/:path*'] };
