/**
 * Snapshot retire route — POST integration tests (TECH-2674 §Test Blueprint).
 *
 *   1. First POST flips `status='retired'` + sets `retired_at`.
 *   2. Second POST returns 200 with `unchanged: true` (idempotent).
 *   3. Missing id returns 404.
 *
 * @see ia/projects/asset-pipeline/stage-13.1 — TECH-2674 §Plan Digest
 */

import { beforeEach, describe, expect, test, vi } from "vitest";

import { POST } from "@/app/api/catalog/snapshot/[id]/retire/route";
import { getSql } from "@/lib/db/client";

const TEST_USER_ID = "33333333-3333-4333-8333-333333333333";

vi.mock("@/lib/auth/get-session", () => ({
  getSessionUser: async () => ({
    id: TEST_USER_ID,
    email: "snapshot-retire@example.com",
    role: "admin",
  }),
}));

async function reset(): Promise<void> {
  const sql = getSql();
  await sql`delete from catalog_snapshot`;
  await sql`delete from audit_log where action like 'catalog.snapshot.%'`;
}

async function seedUser(): Promise<void> {
  const sql = getSql();
  await sql`
    insert into users (id, email, display_name, role)
    values (${TEST_USER_ID}::uuid, 'snapshot-retire@example.com', 'snapshot retire', 'admin')
    on conflict (id) do nothing
  `;
}

async function seedActiveRow(hash: string): Promise<string> {
  const sql = getSql();
  const rows = (await sql`
    insert into catalog_snapshot (hash, manifest_path, entity_counts_json, schema_version, status, created_by)
    values (
      ${hash},
      'Assets/StreamingAssets/catalog/manifest.json',
      '{}'::jsonb,
      2,
      'active',
      ${TEST_USER_ID}::uuid
    )
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  return rows[0]!.id;
}

beforeEach(async () => {
  await seedUser();
  await reset();
}, 30000);

describe("POST /api/catalog/snapshot/[id]/retire (TECH-2674)", () => {
  test("first call flips active → retired + emits audit row", async () => {
    const id = await seedActiveRow("hash_first");
    const req = new Request(
      `http://localhost/api/catalog/snapshot/${id}/retire`,
      { method: "POST" },
    );
    const ctx = { params: Promise.resolve({ id }) };
    const res = await POST(req as unknown as Parameters<typeof POST>[0], ctx);
    expect(res.status).toBe(200);

    const body = (await res.json()) as {
      ok: true;
      data: {
        id: string;
        status: "retired";
        retired_at: string;
        unchanged: boolean;
      };
    };
    expect(body.data.status).toBe("retired");
    expect(body.data.unchanged).toBe(false);
    expect(body.data.retired_at).toMatch(/^2\d{3}-\d{2}-\d{2}T/);

    const sql = getSql();
    const rows = (await sql`
      select status::text as status, retired_at
      from catalog_snapshot
      where id = ${id}::uuid
    `) as unknown as Array<{ status: string; retired_at: Date | null }>;
    expect(rows[0]!.status).toBe("retired");
    expect(rows[0]!.retired_at).not.toBeNull();

    const auditRows = (await sql`
      select count(*)::int as n
      from audit_log
      where action = 'catalog.snapshot.retire' and target_id = ${id}
    `) as unknown as Array<{ n: number }>;
    expect(auditRows[0]!.n).toBe(1);
  });

  test("second call on already-retired row returns unchanged: true (no audit row)", async () => {
    const id = await seedActiveRow("hash_idempotent");
    const ctx = { params: Promise.resolve({ id }) };
    // Flip once.
    await POST(
      new Request(`http://localhost/api/catalog/snapshot/${id}/retire`, {
        method: "POST",
      }) as unknown as Parameters<typeof POST>[0],
      ctx,
    );
    // Flip twice.
    const res2 = await POST(
      new Request(`http://localhost/api/catalog/snapshot/${id}/retire`, {
        method: "POST",
      }) as unknown as Parameters<typeof POST>[0],
      ctx,
    );
    expect(res2.status).toBe(200);
    const body2 = (await res2.json()) as {
      ok: true;
      data: { unchanged: boolean; status: "retired" };
    };
    expect(body2.data.unchanged).toBe(true);
    expect(body2.data.status).toBe("retired");

    // Only one audit row across the two calls.
    const sql = getSql();
    const auditRows = (await sql`
      select count(*)::int as n
      from audit_log
      where action = 'catalog.snapshot.retire' and target_id = ${id}
    `) as unknown as Array<{ n: number }>;
    expect(auditRows[0]!.n).toBe(1);
  });

  test("unknown id returns 404", async () => {
    const fakeId = "00000000-0000-4000-8000-000000000000";
    const ctx = { params: Promise.resolve({ id: fakeId }) };
    const res = await POST(
      new Request(
        `http://localhost/api/catalog/snapshot/${fakeId}/retire`,
        { method: "POST" },
      ) as unknown as Parameters<typeof POST>[0],
      ctx,
    );
    expect(res.status).toBe(404);
  });
});
