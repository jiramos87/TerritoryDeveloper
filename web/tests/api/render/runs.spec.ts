// TECH-1469 — render queue API integration tests.
//
// Covers POST /api/render/runs and GET /api/render/runs/[job_id].
// Direct-invoke harness (no dev server); DB-backed (requires migrations
// 0026 + 0027 applied). Capability gate is enforced upstream by `proxy.ts`
// — these specs stub `getSessionUser` to a seeded test user and verify the
// handler-level behaviour (idempotency, backpressure, audit, queue position,
// run_id linkage, failed retry hint).

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

const VALID_BODY = {
  archetype_id: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
  archetype_version_id: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
  params_json: { wind: 0.4, age: 12 },
};

beforeEach(async () => {
  await seedTestUser();
  await resetRenderTables();
  mockGetSession.mockResolvedValue({
    id: RENDER_TEST_USER_ID,
    email: "render-tests@example.com",
    role: "admin",
  });
});

afterEach(async () => {
  await resetRenderTables();
  vi.clearAllMocks();
});

async function postRun(
  body: unknown,
  headers?: Record<string, string>,
): Promise<Response> {
  const { POST } = await import("@/app/api/render/runs/route");
  return invokeRender(POST, "POST", "/api/render/runs", { body, headers });
}

async function getRun(job_id: string): Promise<Response> {
  const { GET } = await import("@/app/api/render/runs/[job_id]/route");
  return invokeRender(GET, "GET", `/api/render/runs/${job_id}`, {
    params: { job_id },
  });
}

describe("POST /api/render/runs (TECH-1469)", () => {
  test("post_enqueue_happy: inserts job_queue row + emits audit", async () => {
    const res = await postRun(VALID_BODY);
    expect(res.status).toBe(200);
    const body = (await res.json()) as {
      ok: boolean;
      data: { job_id: string };
      audit_id: string | null;
    };
    expect(body.ok).toBe(true);
    expect(body.data.job_id).toMatch(
      /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/,
    );
    expect(body.audit_id).toMatch(/^\d+$/);

    const sql = getSql();
    const rows = (await sql`
      select status, kind, actor_user_id::text as actor_user_id
        from job_queue
       where job_id = ${body.data.job_id}::uuid
    `) as unknown as Array<{ status: string; kind: string; actor_user_id: string }>;
    expect(rows.length).toBe(1);
    expect(rows[0]!.status).toBe("queued");
    expect(rows[0]!.kind).toBe("render_run");
    expect(rows[0]!.actor_user_id).toBe(RENDER_TEST_USER_ID);

    const audits = (await sql`
      select action, payload from audit_log where target_id = ${body.data.job_id}
    `) as unknown as Array<{ action: string; payload: Record<string, unknown> }>;
    expect(audits.length).toBe(1);
    expect(audits[0]!.action).toBe("render.run.enqueued");
    expect(audits[0]!.payload.archetype_id).toBe(VALID_BODY.archetype_id);
    expect(typeof audits[0]!.payload.params_hash).toBe("string");
  });

  test("post_idempotency_replay: same Idempotency-Key returns prior job_id", async () => {
    const headers = { "Idempotency-Key": "test-key-001" };
    const r1 = await postRun(VALID_BODY, headers);
    const b1 = (await r1.json()) as { data: { job_id: string } };

    const r2 = await postRun(VALID_BODY, headers);
    const b2 = (await r2.json()) as { data: { job_id: string } };

    expect(b2.data.job_id).toBe(b1.data.job_id);

    const sql = getSql();
    const rows = (await sql`
      select count(*)::int as n from job_queue where idempotency_key = 'test-key-001'
    `) as unknown as Array<{ n: number }>;
    expect(rows[0]!.n).toBe(1);
  });

  test("post_backpressure: 50 queued render_runs return 429 with retry_hint", async () => {
    const sql = getSql();
    // Pre-seed 50 queued render_run rows belonging to the test user.
    for (let i = 0; i < 50; i++) {
      await sql`
        insert into job_queue (kind, status, payload_json, actor_user_id)
        values ('render_run', 'queued', ${sql.json({ i } as never)}::jsonb,
                ${RENDER_TEST_USER_ID}::uuid)
      `;
    }
    const res = await postRun(VALID_BODY);
    expect(res.status).toBe(429);
    const body = (await res.json()) as {
      ok: boolean;
      error: { code: string; details: { active_count: number } };
      retry_hint: { after_seconds: number };
    };
    expect(body.ok).toBe(false);
    expect(body.error.code).toBe("queue_full");
    expect(body.error.details.active_count).toBe(50);
    expect(body.retry_hint.after_seconds).toBe(30);
  });

  test("post_validation: malformed body returns 400 with details array", async () => {
    const res = await postRun({ archetype_id: "not-a-uuid", params_json: 5 });
    expect(res.status).toBe(400);
    const body = (await res.json()) as {
      ok: boolean;
      error: { code: string; details: string[] };
    };
    expect(body.ok).toBe(false);
    expect(body.error.code).toBe("validation");
    expect(Array.isArray(body.error.details)).toBe(true);
    expect(body.error.details.length).toBeGreaterThan(0);
  });
});

describe("GET /api/render/runs/[job_id] (TECH-1469)", () => {
  test("get_unknown_404: returns not_found envelope", async () => {
    const fake = "deadbeef-dead-4ead-8ead-deadbeefdead";
    const res = await getRun(fake);
    expect(res.status).toBe(404);
    const body = (await res.json()) as { ok: boolean; error: { code: string } };
    expect(body.ok).toBe(false);
    expect(body.error.code).toBe("not_found");
  });

  test("get_queue_position: 1-based position among earlier queued rows", async () => {
    const sql = getSql();
    // Insert three queued rows at distinct enqueued_at timestamps.
    const r1 = (await sql`
      insert into job_queue (kind, status, payload_json, actor_user_id, enqueued_at)
      values ('render_run', 'queued', '{}'::jsonb, ${RENDER_TEST_USER_ID}::uuid,
              now() - interval '3 minutes')
      returning job_id::text as job_id
    `) as unknown as Array<{ job_id: string }>;
    const r2 = (await sql`
      insert into job_queue (kind, status, payload_json, actor_user_id, enqueued_at)
      values ('render_run', 'queued', '{}'::jsonb, ${RENDER_TEST_USER_ID}::uuid,
              now() - interval '2 minutes')
      returning job_id::text as job_id
    `) as unknown as Array<{ job_id: string }>;
    const r3 = (await sql`
      insert into job_queue (kind, status, payload_json, actor_user_id, enqueued_at)
      values ('render_run', 'queued', '{}'::jsonb, ${RENDER_TEST_USER_ID}::uuid,
              now() - interval '1 minutes')
      returning job_id::text as job_id
    `) as unknown as Array<{ job_id: string }>;

    const res2 = await getRun(r2[0]!.job_id);
    const b2 = (await res2.json()) as {
      data: { status: string; queue_position?: number };
    };
    expect(b2.data.status).toBe("queued");
    expect(b2.data.queue_position).toBe(2);

    // Mark first as done; r3's queue_position should now be 2 (one ahead: r2).
    await sql`
      update job_queue set status='done', finished_at=now()
       where job_id = ${r1[0]!.job_id}::uuid
    `;
    const res3 = await getRun(r3[0]!.job_id);
    const b3 = (await res3.json()) as { data: { queue_position?: number } };
    expect(b3.data.queue_position).toBe(2);
  });

  test("get_done_includes_run_id: links render_run row when status=done", async () => {
    const sql = getSql();
    const inserted = (await sql`
      insert into job_queue (kind, status, payload_json, actor_user_id, finished_at)
      values ('render_run', 'done', '{}'::jsonb, ${RENDER_TEST_USER_ID}::uuid, now())
      returning job_id::text as job_id
    `) as unknown as Array<{ job_id: string }>;
    const job_id = inserted[0]!.job_id;
    await sql`
      insert into render_run (run_id, archetype_id, archetype_version_id, params_json,
                              params_hash, output_uris, build_fingerprint, duration_ms,
                              triggered_by)
      values (${job_id}::uuid,
              ${VALID_BODY.archetype_id}::uuid,
              ${VALID_BODY.archetype_version_id}::uuid,
              ${sql.json(VALID_BODY.params_json as never)}::jsonb,
              repeat('a', 64),
              array['gen://x/0']::text[],
              'fp:1',
              123,
              ${RENDER_TEST_USER_ID}::uuid)
    `;

    const res = await getRun(job_id);
    const body = (await res.json()) as {
      data: { status: string; run_id?: string };
    };
    expect(body.data.status).toBe("done");
    expect(body.data.run_id).toBe(job_id);
  });

  test("get_failed_includes_error: failed row carries error + retry_hint=0", async () => {
    const sql = getSql();
    const inserted = (await sql`
      insert into job_queue (kind, status, payload_json, actor_user_id, error,
                             finished_at)
      values ('render_run', 'failed', '{}'::jsonb, ${RENDER_TEST_USER_ID}::uuid,
              'sprite-gen unreachable', now())
      returning job_id::text as job_id
    `) as unknown as Array<{ job_id: string }>;
    const res = await getRun(inserted[0]!.job_id);
    const body = (await res.json()) as {
      data: { status: string; error?: string; retry_hint?: { after_seconds: number } };
    };
    expect(body.data.status).toBe("failed");
    expect(body.data.error).toBe("sprite-gen unreachable");
    expect(body.data.retry_hint?.after_seconds).toBe(0);
  });
});
