// TECH-1470 — replay endpoint integration tests.
//
// Covers POST /api/render/runs/[run_id]/replay. Direct-invoke harness
// (no dev server); DB-backed (requires migrations 0026 + 0027 applied).
// Capability gate is enforced upstream by `proxy.ts` — these specs stub
// `getSessionUser` to a seeded test user and verify the handler-level
// behaviour (source lookup, retired-archetype 409, params override,
// idempotency replay, lineage propagation, audit row).

import { afterEach, beforeEach, describe, expect, test, vi } from "vitest";

vi.mock("@/lib/auth/get-session", () => ({
  getSessionUser: vi.fn(),
}));

import { getSessionUser } from "@/lib/auth/get-session";
import { getSql } from "@/lib/db/client";
import {
  RENDER_TEST_USER_ID,
  invokeRender,
  resetRenderTables,
  seedTestUser,
} from "./_harness";

const mockGetSession = getSessionUser as unknown as ReturnType<typeof vi.fn>;

const ARCHETYPE_ID = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const ARCHETYPE_VERSION_ID = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
const RETIRED_VERSION_ID = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";
const SOURCE_PARAMS = { wind: 0.4, age: 12 };

beforeEach(async () => {
  await seedTestUser();
  await resetRenderTables();
  mockGetSession.mockResolvedValue({
    id: RENDER_TEST_USER_ID,
    email: "render-tests@example.com",
    role: "admin",
  });
  delete process.env.RENDER_TEST_RETIRED_VERSIONS;
});

afterEach(async () => {
  await resetRenderTables();
  vi.clearAllMocks();
  delete process.env.RENDER_TEST_RETIRED_VERSIONS;
});

async function seedSourceRun(opts?: {
  archetype_version_id?: string;
  params_json?: Record<string, unknown>;
}): Promise<string> {
  const sql = getSql();
  const rows = (await sql`
    insert into render_run (archetype_id, archetype_version_id, params_json,
                            params_hash, output_uris, build_fingerprint,
                            duration_ms, triggered_by)
    values (${ARCHETYPE_ID}::uuid,
            ${(opts?.archetype_version_id ?? ARCHETYPE_VERSION_ID)}::uuid,
            ${sql.json((opts?.params_json ?? SOURCE_PARAMS) as never)}::jsonb,
            repeat('a', 64),
            array['gen://x/0']::text[],
            'fp:1',
            123,
            ${RENDER_TEST_USER_ID}::uuid)
    returning run_id::text as run_id
  `) as unknown as Array<{ run_id: string }>;
  return rows[0]!.run_id;
}

async function postReplay(
  run_id: string,
  body?: unknown,
  headers?: Record<string, string>,
): Promise<Response> {
  const { POST } = await import(
    "@/app/api/render/runs/[run_id]/replay/route"
  );
  return invokeRender(POST, "POST", `/api/render/runs/${run_id}/replay`, {
    body,
    headers,
    params: { run_id },
  });
}

describe("POST /api/render/runs/[run_id]/replay (TECH-1470)", () => {
  test("replay_happy_no_override: inherits source params verbatim", async () => {
    const sourceId = await seedSourceRun();
    const res = await postReplay(sourceId);
    expect(res.status).toBe(200);
    const body = (await res.json()) as {
      ok: boolean;
      data: { job_id: string };
      audit_id: string | null;
    };
    expect(body.ok).toBe(true);
    expect(body.audit_id).toMatch(/^\d+$/);

    const sql = getSql();
    const rows = (await sql`
      select payload_json from job_queue
       where job_id = ${body.data.job_id}::uuid
    `) as unknown as Array<{ payload_json: Record<string, unknown> }>;
    expect(rows.length).toBe(1);
    const payload = rows[0]!.payload_json as {
      mode: string;
      parent_run_id: string;
      archetype_version_id: string;
      params_json: Record<string, unknown>;
    };
    expect(payload.mode).toBe("replay");
    expect(payload.parent_run_id).toBe(sourceId);
    expect(payload.archetype_version_id).toBe(ARCHETYPE_VERSION_ID);
    expect(payload.params_json).toEqual(SOURCE_PARAMS);

    const audits = (await sql`
      select action, payload from audit_log where target_id = ${body.data.job_id}
    `) as unknown as Array<{ action: string; payload: Record<string, unknown> }>;
    expect(audits.length).toBe(1);
    expect(audits[0]!.action).toBe("render.run.replay_enqueued");
    expect(audits[0]!.payload.params_overridden).toBe(false);
    expect(audits[0]!.payload.source_run_id).toBe(sourceId);
    expect(audits[0]!.payload.parent_run_id).toBe(sourceId);
  });

  test("replay_with_override: applies override + audit flag set", async () => {
    const sourceId = await seedSourceRun();
    const override = { wind: 0.9, age: 30 };
    const res = await postReplay(sourceId, { params_json: override });
    expect(res.status).toBe(200);
    const body = (await res.json()) as { data: { job_id: string } };

    const sql = getSql();
    const rows = (await sql`
      select payload_json from job_queue
       where job_id = ${body.data.job_id}::uuid
    `) as unknown as Array<{ payload_json: Record<string, unknown> }>;
    const payload = rows[0]!.payload_json as {
      params_json: Record<string, unknown>;
    };
    expect(payload.params_json).toEqual(override);

    const audits = (await sql`
      select payload from audit_log where target_id = ${body.data.job_id}
    `) as unknown as Array<{ payload: Record<string, unknown> }>;
    expect(audits[0]!.payload.params_overridden).toBe(true);
  });

  test("replay_invalid_override: rejects unexpected fields with 400", async () => {
    const sourceId = await seedSourceRun();
    const res = await postReplay(sourceId, {
      params_json: { ok: 1 },
      junk: "extra",
    });
    expect(res.status).toBe(400);
    const body = (await res.json()) as {
      ok: boolean;
      error: { code: string; details: string[] };
    };
    expect(body.ok).toBe(false);
    expect(body.error.code).toBe("validation");
    expect(Array.isArray(body.error.details)).toBe(true);
  });

  test("replay_archetype_retired: returns 409 archetype_retired", async () => {
    const sourceId = await seedSourceRun({
      archetype_version_id: RETIRED_VERSION_ID,
    });
    process.env.RENDER_TEST_RETIRED_VERSIONS = RETIRED_VERSION_ID;
    const res = await postReplay(sourceId);
    expect(res.status).toBe(409);
    const body = (await res.json()) as {
      ok: boolean;
      error: { code: string; message: string };
    };
    expect(body.ok).toBe(false);
    expect(body.error.code).toBe("conflict");
    expect(body.error.message).toBe("archetype_retired");

    // No job_queue row inserted on rejection.
    const sql = getSql();
    const counts = (await sql`
      select count(*)::int as n from job_queue
    `) as unknown as Array<{ n: number }>;
    expect(counts[0]!.n).toBe(0);
  });

  test("replay_source_missing: unknown run_id returns 404", async () => {
    const fake = "deadbeef-dead-4ead-8ead-deadbeefdead";
    const res = await postReplay(fake);
    expect(res.status).toBe(404);
    const body = (await res.json()) as { ok: boolean; error: { code: string } };
    expect(body.ok).toBe(false);
    expect(body.error.code).toBe("not_found");
  });

  test("replay_idempotency: same Idempotency-Key returns prior job_id", async () => {
    const sourceId = await seedSourceRun();
    const headers = { "Idempotency-Key": "replay-key-001" };
    const r1 = await postReplay(sourceId, undefined, headers);
    const b1 = (await r1.json()) as { data: { job_id: string } };
    const r2 = await postReplay(sourceId, undefined, headers);
    const b2 = (await r2.json()) as { data: { job_id: string } };
    expect(b2.data.job_id).toBe(b1.data.job_id);

    const sql = getSql();
    const rows = (await sql`
      select count(*)::int as n from job_queue where idempotency_key = 'replay-key-001'
    `) as unknown as Array<{ n: number }>;
    expect(rows[0]!.n).toBe(1);
  });

  test("replay_backpressure: 50 queued render_runs returns 429 + retry_hint", async () => {
    const sourceId = await seedSourceRun();
    const sql = getSql();
    for (let i = 0; i < 50; i++) {
      await sql`
        insert into job_queue (kind, status, payload_json, actor_user_id)
        values ('render_run', 'queued', ${sql.json({ i } as never)}::jsonb,
                ${RENDER_TEST_USER_ID}::uuid)
      `;
    }
    const res = await postReplay(sourceId);
    expect(res.status).toBe(429);
    const body = (await res.json()) as {
      error: { code: string };
      retry_hint: { after_seconds: number };
    };
    expect(body.error.code).toBe("queue_full");
    expect(body.retry_hint.after_seconds).toBe(30);
  });

  test("replay_bad_run_id: malformed path uuid returns 400", async () => {
    const res = await postReplay("not-a-uuid");
    expect(res.status).toBe(400);
    const body = (await res.json()) as { error: { code: string } };
    expect(body.error.code).toBe("validation");
  });
});
