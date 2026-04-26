import type { NextAuthOptions } from 'next-auth';
import EmailProvider from 'next-auth/providers/email';
import { getSql } from '@/lib/db/client';

export const authOptions: NextAuthOptions = {
  providers: [
    EmailProvider({
      server: process.env.EMAIL_SERVER ?? '',
      from: 'noreply@asset-pipeline.local',
    }),
  ],
  session: { strategy: 'jwt' },
  callbacks: {
    async signIn({ user }) {
      if (!user.email) return false;
      const sql = getSql();
      const rows = await sql`
        insert into users (email, display_name, last_login_at)
        values (${user.email}, ${user.name ?? user.email}, now())
        on conflict (email) do update set last_login_at = now()
        returning id, role, retired_at
      `;
      const row = rows[0] as { retired_at: Date | null } | undefined;
      return row != null && row.retired_at == null;
    },
    async jwt({ token, user }) {
      if (user?.email) {
        const sql = getSql();
        const rows = await sql`select id, role from users where email = ${user.email}`;
        const r = rows[0] as { id: string; role: string } | undefined;
        if (r) {
          token.uid = r.id;
          token.role = r.role;
        }
      }
      return token;
    },
    async session({ session, token }) {
      if (session.user) {
        (session.user as { id?: string }).id = token.uid as string | undefined;
        (session.user as { role?: string }).role = token.role as string | undefined;
      }
      return session;
    },
  },
};
