/**
 * Snapshots authoring console (TECH-2674 §Acceptance #7).
 *
 * Server component that fetches the initial snapshot history page directly
 * from the database (same `select` shape as `GET /api/catalog/snapshot`)
 * and hands it to the client `SnapshotHistoryTable`. Going through the DB
 * avoids HTTP self-fetch + capability re-check; the client-side "Load more"
 * + "Export" + "Retire" actions still hit the API routes (gated upstream
 * by `proxy.ts` capability checks).
 *
 * @see ia/projects/asset-pipeline/stage-13.1 — TECH-2674 §Plan Digest
 */

import { getSql } from "@/lib/db/client";
import SnapshotHistoryTable, {
  type SnapshotHistoryRow,
} from "@/components/snapshot/SnapshotHistoryTable";

export const dynamic = "force-dynamic";

const PAGE_SIZE = 20;

type SnapshotDbRow = {
  id: string;
  hash: string;
  manifest_path: string;
  schema_version: number;
  status: "active" | "retired";
  entity_counts_json: Record<string, number>;
  created_at: Date;
  retired_at: Date | null;
  created_by: string | null;
};

async function loadInitialPage(): Promise<{
  items: SnapshotHistoryRow[];
  nextCursor: string | null;
}> {
  const sql = getSql();
  const rows = (await sql`
    select
      id::text as id,
      hash,
      manifest_path,
      schema_version,
      status::text as status,
      entity_counts_json,
      created_at,
      retired_at,
      created_by::text as created_by
    from catalog_snapshot
    order by created_at desc, id desc
    limit ${PAGE_SIZE + 1}
  `) as unknown as SnapshotDbRow[];

  let nextCursor: string | null = null;
  let pageRows = rows;
  if (rows.length > PAGE_SIZE) {
    pageRows = rows.slice(0, PAGE_SIZE);
    const tail = pageRows[pageRows.length - 1]!;
    nextCursor = tail.created_at.toISOString();
  }

  const items: SnapshotHistoryRow[] = pageRows.map((r) => ({
    id: r.id,
    hash: r.hash,
    manifest_path: r.manifest_path,
    schema_version: r.schema_version,
    status: r.status,
    entity_counts_json: r.entity_counts_json,
    created_at: r.created_at.toISOString(),
    retired_at: r.retired_at === null ? null : r.retired_at.toISOString(),
    created_by: r.created_by,
  }));

  return { items, nextCursor };
}

export default async function SnapshotsAuthoringPage() {
  const { items, nextCursor } = await loadInitialPage();
  return (
    <SnapshotHistoryTable
      initialItems={items}
      initialNextCursor={nextCursor}
    />
  );
}
