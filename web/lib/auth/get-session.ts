import { getServerSession } from 'next-auth';
import { authOptions } from './config';
import { getSql } from '@/lib/db/client';

export type SessionUser = { id: string; email: string; role: string };

export async function getSessionUser(): Promise<SessionUser | null> {
  // Direct-invoke harness (vitest) calls route handlers without a Next request
  // scope; `getServerSession` then throws inside `headers()`. Treat that as
  // "no session" rather than crash the route — capability gating still happens
  // upstream in `proxy.ts` where the request scope exists.
  type Sess = { user?: { id?: string; email?: string; role?: string } } | null;
  let session: Sess;
  try {
    session = (await getServerSession(authOptions)) as Sess;
  } catch {
    return null;
  }
  const u = session?.user;
  if (!u?.id || !u?.email || !u?.role) return null;
  const sql = getSql();
  const rows = await sql`select retired_at from users where id = ${u.id}`;
  const row = rows[0] as { retired_at: Date | null } | undefined;
  if (row?.retired_at != null) return null;
  return { id: u.id, email: u.email, role: u.role };
}
