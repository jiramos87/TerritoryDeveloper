import { NextResponse, type NextRequest } from 'next/server';
import { getToken } from 'next-auth/jwt';
import { getSql } from '@/lib/db/client';
import { loadCapabilitiesForRole } from '@/lib/auth/capabilities';
import { forbiddenEnvelope } from '@/lib/auth/route-meta';

/**
 * Next.js 16 Proxy (formerly `middleware.ts`). Logic mirrors §Plan Digest spec
 * verbatim — function renamed `middleware` → `proxy` per Next 16 deprecation.
 *
 * DEC-A33 capability matrix + DEC-A48 forbidden envelope contract.
 */

export const config = { matcher: ['/api/catalog/:path*', '/api/render/:path*'] };

type ResolvedUser = { id: string; role: string } | null;

async function devCookieFallback(req: NextRequest): Promise<ResolvedUser> {
  if (process.env.NEXT_PUBLIC_AUTH_DEV_FALLBACK !== '1') return null;
  const id = req.cookies.get('dev_user_id')?.value;
  if (!id) return null;
  const sql = getSql();
  const rows = await sql`select id, role, retired_at from users where id = ${id}`;
  const r = rows[0] as { id: string; role: string; retired_at: Date | null } | undefined;
  if (!r || r.retired_at != null) return null;
  return { id: r.id, role: r.role };
}

async function resolveSessionUser(req: NextRequest): Promise<ResolvedUser> {
  const token = await getToken({ req, secret: process.env.NEXTAUTH_SECRET });
  if (token?.uid && token?.role) {
    return { id: token.uid as string, role: token.role as string };
  }
  return devCookieFallback(req);
}

async function loadRouteRequires(pathname: string, method: string): Promise<string | null> {
  const map = (await import('./lib/auth/route-meta-map')).default as Record<
    string,
    Record<string, { requires: string }>
  >;
  // Direct match first.
  const direct = map[pathname]?.[method];
  if (direct) return direct.requires;
  // Pattern match — replace [id]-style segments with single-segment regex.
  for (const [pattern, methods] of Object.entries(map)) {
    if (!pattern.includes('[')) continue;
    const regex = new RegExp('^' + pattern.replace(/\[[^\]]+\]/g, '[^/]+') + '$');
    if (regex.test(pathname)) {
      const entry = methods[method];
      if (entry) return entry.requires;
    }
  }
  return null;
}

export async function proxy(req: NextRequest) {
  const required = await loadRouteRequires(req.nextUrl.pathname, req.method);
  if (!required) {
    return new NextResponse(JSON.stringify(forbiddenEnvelope('<unknown>', '<none>')), {
      status: 403,
      headers: { 'content-type': 'application/json' },
    });
  }
  const user = await resolveSessionUser(req);
  if (!user) {
    return new NextResponse(JSON.stringify(forbiddenEnvelope(required, '<none>')), {
      status: 401,
      headers: { 'content-type': 'application/json' },
    });
  }
  const caps = await loadCapabilitiesForRole(user.role);
  if (!caps.has(required)) {
    return new NextResponse(JSON.stringify(forbiddenEnvelope(required, user.role)), {
      status: 403,
      headers: { 'content-type': 'application/json' },
    });
  }
  return NextResponse.next();
}
