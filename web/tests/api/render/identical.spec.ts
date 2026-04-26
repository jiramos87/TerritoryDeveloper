// TECH-1470 — identical re-render endpoint integration tests.
//
// Covers POST /api/render/runs/[run_id]/identical. Mirrors the replay
// harness — direct-invoke, DB-backed, mocked session. Identical re-
// renders MUST reject any body fields and propagate source archetype
// version + params byte-for-byte.

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
const SOURCE_PARAMS = { wind: 0.4, age: 12, nested: { a: 1, b: [1, 2, 3] } };

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

async function postIdentical(
  run_id: string,
  body?: unknown,
  headers?: Record<string, string>,
): Promise<Response> {
  const { POST } = await import(
    "@/app/api/render/runs/[run_id]/identical/route"
  );
  return invokeRender(POST, "POST", `/api/render/runs/${run_id}/identical`, {
    body,
    headers,
    params: { run_id },
  });
}

describe("POST /api/render/runs/[run_id]/identical (TECH-1470)", () => {
  test("identical_happy: byte-identical params + lineage + audit", async () => {
    const sourceId = await seedSourceRun();
    const res = await postIdentical(sourceId);
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
      archetype_id: string;
      params_json: Record<string, unknown>;
    };
    expect(payload.mode).toBe("identical");
    expect(payload.parent_run_id).toBe(sourceId);
    expect(payload.archetype_version_id).toBe(ARCHETYPE_VERSION_ID);
    expect(payload.archetype_id).toBe(ARCHETYPE_ID);
    expect(payload.params_json).toEqual(SOURCE_PARAMS);

    const audits = (await sql`
      select action, payload from audit_log where target_id = ${body.data.job_id}
    `) as unknown as Array<{ action: string; payload: Record<string, unknown> }>;
    expect(audits.length).toBe(1);
    expect(audits[0]!.action).toBe("render.run.identical_enqueued");
    expect(audits[0]!.payload.source_run_id).toBe(sourceId);
    expect(audits[0]!.payload.parent_run_id).toBe(sourceId);
  });

  test("identical_body_must_be_empty: any field returns 400", async () => {
    const sourceId = await seedSourceRun();
    const res = await postIdentical(sourceId, { params_json: {} });
    expect(res.status).toBe(400);
    const body = (await res.json()) as {
      ok: boolean;
      error: { code: string; details: string[] };
    };
    expect(body.ok).toBe(false);
    expect(body.error.code).toBe("validation");
    expect(body.error.details.some((d) => d.includes("unexpected field"))).toBe(true);
  });

  test("identical_archetype_retired: returns 409 archetype_retired", async () => {
    const sourceId = await seedSourceRun({
      archetype_version_id: RETIRED_VERSION_ID,
    });
    process.env.RENDER_TEST_RETIRED_VERSIONS = RETIRED_VERSION_ID;
    const res = await postIdentical(sourceId);
    expect(res.status).toBe(409);
    const body = (await res.json()) as {
      error: { code: string; message: string };
    };
    expect(body.error.code).toBe("conflict");
    expect(body.error.message).toBe("archetype_retired");
  });

  test("identical_source_missing: unknown run_id returns 404", async () => {
    const fake = "deadbeef-dead-4ead-8ead-deadbeefdead";
    const res = await postIdentical(fake);
    expect(res.status).toBe(404);
    const body = (await res.json()) as { error: { code: string } };
    expect(body.error.code).toBe("not_found");
  });

  test("identical_idempotency: same key returns prior job_id", async () => {
    const sourceId = await seedSourceRun();
    const headers = { "Idempotency-Key": "ident-key-001" };
    const r1 = await postIdentical(sourceId, undefined, headers);
    const b1 = (await r1.json()) as { data: { job_id: string } };
    const r2 = await postIdentical(sourceId, undefined, headers);
    const b2 = (await r2.json()) as { data: { job_id: string } };
    expect(b2.data.job_id).toBe(b1.data.job_id);
  });

  test("identical_backpressure: 50 queued returns 429", async () => {
    const sourceId = await seedSourceRun();
    const sql = getSql();
    for (let i = 0; i < 50; i++) {
      await sql`
        insert into job_queue (kind, status, payload_json, actor_user_id)
        values ('render_run', 'queued', ${sql.json({ i } as never)}::jsonb,
                ${RENDER_TEST_USER_ID}::uuid)
      `;
    }
    const res = await postIdentical(sourceId);
    expect(res.status).toBe(429);
    const body = (await res.json()) as {
      error: { code: string };
      retry_hint: { after_seconds: number };
    };
    expect(body.error.code).toBe("queue_full");
    expect(body.retry_hint.after_seconds).toBe(30);
  });

  test("identical_bad_run_id: malformed path uuid returns 400", async () => {
    const res = await postIdentical("not-a-uuid");
    expect(res.status).toBe(400);
    const body = (await res.json()) as { error: { code: string } };
    expect(body.error.code).toBe("validation");
  });
});
