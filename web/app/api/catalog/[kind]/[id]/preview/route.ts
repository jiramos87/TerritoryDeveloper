import { randomUUID } from "crypto";
import { type NextRequest, NextResponse } from "next/server";
import { getSql } from "@/lib/db/client";
import { catalogJsonError } from "@/lib/catalog/catalog-api-errors";

export const dynamic = "force-dynamic";
export const routeMeta = {
  POST: { requires: "catalog.entity.edit" },
} as const;

type Ctx = { params: Promise<{ kind: string; id: string }> };

const POLL_INTERVAL_MS = 500;
const POLL_MAX_ATTEMPTS = 60;

export async function POST(_req: NextRequest, ctx: Ctx) {
  const { id } = await ctx.params;
  if (!id) {
    return catalogJsonError(400, "bad_request", "id required");
  }

  const commandId = randomUUID();
  const requestEnvelope = {
    kind: "catalog_preview",
    command_id: commandId,
    requested_at_utc: new Date().toISOString(),
    params: {
      catalog_entry_id: id,
      include_screenshot: true,
    },
  };

  try {
    const sql = getSql();

    await sql`
      INSERT INTO agent_bridge_job (command_id, kind, status, request, agent_id)
      VALUES (
        ${commandId}::uuid,
        'catalog_preview',
        'pending',
        ${JSON.stringify(requestEnvelope)}::jsonb,
        'web-preview-api'
      )
    `;

    for (let attempt = 0; attempt < POLL_MAX_ATTEMPTS; attempt++) {
      await new Promise((r) => setTimeout(r, POLL_INTERVAL_MS));

      const rows = await sql<Array<{ status: string; response: unknown; error: string | null }>>`
        SELECT status, response, error
        FROM agent_bridge_job
        WHERE command_id = ${commandId}::uuid
      `;

      if (rows.length === 0) continue;
      const row = rows[0];

      if (row.status === "completed") {
        const resp = row.response as Record<string, unknown> | null;
        const artifactPaths = resp?.artifact_paths as string[] | undefined;
        const screenshotUrl = artifactPaths?.[0] ?? "";
        return NextResponse.json({ ok: true, screenshotUrl });
      }

      if (row.status === "failed") {
        return catalogJsonError(500, "internal", row.error ?? "Bridge job failed");
      }
    }

    return NextResponse.json({ ok: false, error: "timeout" }, { status: 504 });
  } catch (e) {
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured");
    }
    return catalogJsonError(500, "internal", e instanceof Error ? e.message : "Preview request failed");
  }
}
