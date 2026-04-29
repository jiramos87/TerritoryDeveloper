import type { TransactionSql } from "postgres";

import type { LintResult } from "@/lib/lint/types";

type Sql = TransactionSql | import("postgres").Sql;

const SEVERITY_TO_STATUS: Record<string, "fail" | "warn" | "pass"> = {
  block: "fail",
  warn: "warn",
  info: "pass",
};

/**
 * Persist lint findings into `publish_lint_finding` inside an open tx.
 * Records all results (block → fail, warn → warn, info → pass) so the
 * Dashboard LintFailuresWidget has a source (TECH-4183 / Stage 15.1).
 * No-op when results is empty.
 */
export async function recordFindings(
  tx: Sql,
  entityId: number,
  entityVersionId: number,
  results: LintResult[],
): Promise<void> {
  if (results.length === 0) return;
  for (const r of results) {
    const status = SEVERITY_TO_STATUS[r.severity] ?? "pass";
    await tx`
      insert into publish_lint_finding
        (entity_id, entity_version_id, rule_id, severity, status, message)
      values
        (${entityId}, ${entityVersionId}, ${r.rule_id}, ${r.severity}, ${status}, ${r.message})
    `;
  }
}
