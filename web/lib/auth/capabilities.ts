import { getSql } from '@/lib/db/client';

const cache = new Map<string, Promise<Set<string>>>();

export function loadCapabilitiesForRole(role: string): Promise<Set<string>> {
  let p = cache.get(role);
  if (p) return p;
  p = (async () => {
    const sql = getSql();
    const rows = await sql`select capability_id from role_capability where role = ${role}`;
    return new Set((rows as unknown as { capability_id: string }[]).map((r) => r.capability_id));
  })();
  cache.set(role, p);
  return p;
}

export function clearCapabilityCache(): void {
  cache.clear();
}
