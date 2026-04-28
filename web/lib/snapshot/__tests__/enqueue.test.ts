/**
 * `enqueueSnapshotRebuild` payload + row-shape tests (TECH-2674 §Test Blueprint).
 *
 * Covers:
 *   1. Publish-trigger payload round-trips through `payload_json` jsonb.
 *   2. Manual-trigger payload round-trips with no `source_*` fields.
 *   3. `kind='snapshot_rebuild'` + `status='queued'` defaults persist.
 *
 * @see ia/projects/asset-pipeline/stage-13.1 — TECH-2674 §Plan Digest
 */

import { beforeEach, describe, expect, test } from "vitest";

import { getSql } from "@/lib/db/client";
import {
  enqueueSnapshotRebuild,
  SNAPSHOT_REBUILD_KIND,
} from "@/lib/snapshot/enqueue";

async function reset(): Promise<void> {
  const sql = getSql();
  await sql`delete from job_queue where kind = ${SNAPSHOT_REBUILD_KIND}`;
}

beforeEach(async () => {
  await reset();
}, 30000);

describe("enqueueSnapshotRebuild — payload + row shape (TECH-2674)", () => {
  test("publish trigger persists source_* fields in payload_json", async () => {
    const sql = getSql();
    const out = await enqueueSnapshotRebuild(sql, {
      trigger: "publish",
      source_kind: "sprite",
      source_entity_id: "1234",
      source_version_id: "5678",
    });

    expect(out.jobId).toMatch(
      /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/,
    );

    const rows = (await sql`
      select kind, status, payload_json
      from job_queue
      where job_id = ${out.jobId}::uuid
    `) as unknown as Array<{
      kind: string;
      status: string;
      payload_json: Record<string, unknown>;
    }>;

    expect(rows.length).toBe(1);
    expect(rows[0]!.kind).toBe(SNAPSHOT_REBUILD_KIND);
    expect(rows[0]!.status).toBe("queued");
    expect(rows[0]!.payload_json).toEqual({
      trigger: "publish",
      source_kind: "sprite",
      source_entity_id: "1234",
      source_version_id: "5678",
    });
  });

  test("manual trigger persists with no source_* fields", async () => {
    const sql = getSql();
    const out = await enqueueSnapshotRebuild(sql, { trigger: "manual" });

    const rows = (await sql`
      select payload_json
      from job_queue
      where job_id = ${out.jobId}::uuid
    `) as unknown as Array<{ payload_json: Record<string, unknown> }>;

    expect(rows.length).toBe(1);
    expect(rows[0]!.payload_json).toEqual({ trigger: "manual" });
    expect("source_kind" in rows[0]!.payload_json).toBe(false);
    expect("source_entity_id" in rows[0]!.payload_json).toBe(false);
    expect("source_version_id" in rows[0]!.payload_json).toBe(false);
  });

  test("multiple enqueues each insert a fresh row", async () => {
    const sql = getSql();
    await enqueueSnapshotRebuild(sql, { trigger: "manual" });
    await enqueueSnapshotRebuild(sql, { trigger: "manual" });
    await enqueueSnapshotRebuild(sql, {
      trigger: "publish",
      source_kind: "asset",
      source_entity_id: "1",
      source_version_id: "1",
    });

    const rows = (await sql`
      select count(*)::int as n
      from job_queue
      where kind = ${SNAPSHOT_REBUILD_KIND}
    `) as unknown as Array<{ n: number }>;
    expect(rows[0]!.n).toBe(3);
  });
});
