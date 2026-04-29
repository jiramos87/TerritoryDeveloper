/**
 * Dashboard widget — last 10 lint failures (TECH-4183 / Stage 15.1).
 *
 * GET /api/catalog/dashboard/lint-failures
 *   -> 200 { items: LintFailureRow[] }
 *
 * Returns last 10 `publish_lint_finding` rows with status='fail',
 * ordered by created_at DESC. Joins catalog_entity for slug.
 */

import { NextResponse } from "next/server";

import { catalogJsonError, responseFromPostgresError } from "@/lib/catalog/catalog-api-errors";
import { getSql } from "@/lib/db/client";

export const dynamic = "force-dynamic";
export const routeMeta = {
  GET: { requires: "catalog.entity.read" },
} as const;

export type LintFailureRow = {
  id: string;
  entity_id: string;
  entity_slug: string | null;
  rule_id: string;
  severity: string;
  message: string;
  created_at: string;
};

export type LintFailuresResponse = { items: LintFailureRow[] };

export async function GET() {
  try {
    const sql = getSql();
    const rows = (await sql`
      select
        f.id::text as id,
        f.entity_id::text as entity_id,
        ce.slug as entity_slug,
        f.rule_id,
        f.severity,
        f.message,
        f.created_at
      from publish_lint_finding f
      left join catalog_entity ce on ce.id = f.entity_id
      where f.status = 'fail'
      order by f.created_at desc
      limit 10
    `) as Array<{
      id: string;
      entity_id: string;
      entity_slug: string | null;
      rule_id: string;
      severity: string;
      message: string;
      created_at: Date;
    }>;
    const items: LintFailureRow[] = rows.map((r) => ({
      id: r.id,
      entity_id: r.entity_id,
      entity_slug: r.entity_slug,
      rule_id: r.rule_id,
      severity: r.severity,
      message: r.message,
      created_at: r.created_at.toISOString(),
    }));
    const body: LintFailuresResponse = { items };
    return NextResponse.json({ ok: true, data: body }, { status: 200 });
  } catch (e) {
    if (e instanceof Error && e.message === "DATABASE_URL not set — required for DB access.") {
      return catalogJsonError(500, "internal", "Database not configured", { logContext: "dashboard-lint-failures" });
    }
    return responseFromPostgresError(e, "Lint failures query failed");
  }
}
