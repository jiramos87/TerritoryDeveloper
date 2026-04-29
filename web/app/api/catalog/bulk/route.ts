/**
 * Bulk entity mutation endpoint (TECH-4182 / Stage 15.1).
 *
 * POST /api/catalog/bulk
 *   Body: { action: "retire" | "restore" | "publish", entity_ids: string[] }
 *
 * Runs retire / restore / publish inside a single transaction (DEC-A35).
 * Per-entity audit rows emitted inside tx (DEC-A33 — one row per entity).
 * Per-action capability gate beyond proxy-level edit gate (DEC-A34).
 * Idempotency-Key header checked via audit_log (DEC-A48).
 *
 * @see web/lib/catalog/bulk-actions.ts — SQL helpers
 */
import { NextResponse, type NextRequest } from "next/server";
import { audit } from "@/lib/audit/emitter";
import { catalogJsonError, responseFromPostgresError } from "@/lib/catalog/catalog-api-errors";
import { runBulkPublish, runBulkRestore, runBulkRetire, type BulkAction } from "@/lib/catalog/bulk-actions";
import { loadCapabilitiesForRole } from "@/lib/auth/capabilities";
import { getSessionUser } from "@/lib/auth/get-session";
import { getSql } from "@/lib/db/client";

export const dynamic = "force-dynamic";

export const routeMeta = {
  POST: { requires: "catalog.entity.edit" },
} as const;

const VALID_ACTIONS = new Set<BulkAction>(["retire", "restore", "publish"]);

const ACTION_CAPABILITY: Record<BulkAction, string> = {
  retire: "catalog.entity.retire",
  restore: "catalog.entity.edit",
  publish: "catalog.entity.publish",
};

export async function POST(request: NextRequest) {
  let body: unknown;
  try {
    body = await request.json();
  } catch {
    return catalogJsonError(400, "bad_request", "Request body must be JSON");
  }

  const { action, entity_ids } = body as { action?: unknown; entity_ids?: unknown };

  if (!action || !VALID_ACTIONS.has(action as BulkAction)) {
    return catalogJsonError(400, "bad_request", "action must be one of: retire, restore, publish");
  }
  if (!Array.isArray(entity_ids) || entity_ids.length === 0 || entity_ids.length > 1000) {
    return catalogJsonError(400, "bad_request", "entity_ids must be a non-empty array of ≤1000 ids");
  }
  if (!entity_ids.every((id) => typeof id === "string")) {
    return catalogJsonError(400, "bad_request", "entity_ids must be strings");
  }

  const bulkAction = action as BulkAction;
  const entityIds = entity_ids as string[];

  // Per-action capability gate (beyond proxy-level HTTP gate).
  const user = await getSessionUser();
  if (user) {
    const caps = await loadCapabilitiesForRole(user.role);
    if (!caps.has(ACTION_CAPABILITY[bulkAction])) {
      return catalogJsonError(400, "bad_request", `Missing capability: ${ACTION_CAPABILITY[bulkAction]}`);
    }
  }

  // Idempotency-Key dedup (DEC-A48).
  const idempotencyKey = request.headers.get("Idempotency-Key");
  if (idempotencyKey) {
    const sql = getSql();
    const existing = await sql`
      SELECT id FROM audit_log
      WHERE payload->>'idempotency_key' = ${idempotencyKey}
        AND action LIKE 'catalog.entity.%_bulk'
      LIMIT 1
    `;
    if (existing.length > 0) {
      return NextResponse.json({ ok: true, data: { updated: 0, action: bulkAction, idempotent: true } }, { status: 200 });
    }
  }

  try {
    const sql = getSql();
    const result = await sql.begin(async (tx) => {
      let bulkResult;
      if (bulkAction === "retire") {
        bulkResult = await runBulkRetire(tx, entityIds);
      } else if (bulkAction === "restore") {
        bulkResult = await runBulkRestore(tx, entityIds);
      } else {
        bulkResult = await runBulkPublish(tx, entityIds);
      }

      // One audit row per entity (DEC-A33).
      for (const payload of bulkResult.audit_payloads) {
        await audit(tx, {
          actor_user_id: user?.id ?? null,
          action: payload.action,
          target_kind: "catalog_entity",
          target_id: payload.entity_id,
          payload: {
            ...payload.meta,
            ...(idempotencyKey ? { idempotency_key: idempotencyKey } : {}),
          },
        });
      }

      return bulkResult;
    });

    return NextResponse.json({ ok: true, data: { updated: result.updated, action: bulkAction } }, { status: 200 });
  } catch (e) {
    return responseFromPostgresError(e, "Bulk action failed");
  }
}
