import type { Sql } from "postgres";

export type AuditEntry = {
  actor_user_id: string | null;
  action: string;
  target_kind: string;
  target_id: string;
  payload: Record<string, unknown>;
};

/**
 * Append a single row to `audit_log`. Caller passes a tx (or sql instance) so
 * the audit row commits in the same tx as the mutation it records (see
 * `withAudit` decorator). Returns the generated bigserial id as a string.
 *
 * @see DEC-A48 — `audit_id` field in mutate response envelope.
 */
export async function audit(sql: Sql, e: AuditEntry): Promise<string> {
  const rows = await sql`
    insert into audit_log (actor_user_id, action, target_kind, target_id, payload, created_at)
    values (
      ${e.actor_user_id},
      ${e.action},
      ${e.target_kind},
      ${e.target_id},
      ${sql.json(e.payload as never)},
      now()
    )
    returning id
  `;
  return String((rows as unknown as Array<{ id: string | number }>)[0]!.id);
}
