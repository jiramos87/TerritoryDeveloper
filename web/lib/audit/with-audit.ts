import { NextResponse, type NextRequest } from "next/server";
import type { Sql } from "postgres";
import { getSql } from "@/lib/db/client";
import { audit } from "./emitter";
import { getSessionUser } from "@/lib/auth/get-session";

type Emit = (
  action: string,
  target_kind: string,
  target_id: string,
  payload: Record<string, unknown>,
) => Promise<void>;

export type WithAuditCtx = { emit: Emit; sql: Sql };

export type WithAuditHandler<T> = (
  req: NextRequest,
  ctx: WithAuditCtx,
) => Promise<{ status: number; data: T }>;

/**
 * Decorator wrapping a mutating route handler in a single tx. Provides an
 * `emit()` helper that writes one `audit_log` row inside the same tx as the
 * mutation. Response envelope per DEC-A48: `{ ok: true, data, audit_id }`.
 *
 * If the handler throws, the tx rolls back (no partial mutation, no audit row).
 */
export function withAudit<T>(handler: WithAuditHandler<T>) {
  return async (req: NextRequest) => {
    const sql = getSql();
    const session = await getSessionUser();
    let auditId: string | null = null;
    const out = await sql.begin(async (tx) => {
      const emit: Emit = async (action, target_kind, target_id, payload) => {
        auditId = await audit(tx as unknown as Sql, {
          actor_user_id: session?.id ?? null,
          action,
          target_kind,
          target_id,
          payload,
        });
      };
      return handler(req, { emit, sql: tx as unknown as Sql });
    }) as { status: number; data: T };
    return NextResponse.json(
      { ok: true, data: out.data, audit_id: auditId },
      { status: out.status },
    );
  };
}
