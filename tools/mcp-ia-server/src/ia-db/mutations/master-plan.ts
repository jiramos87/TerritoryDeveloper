/**
 * Master-plan-domain mutations: preamble-write, description-write,
 * change-log-append, insert, close, version-create.
 */

import { IaDbValidationError, withTx } from "./shared.js";

// ---------------------------------------------------------------------------
// master_plan_preamble_write / master_plan_change_log_append
// (Step 9.6.8 — Option A DB pivot, retires ia/projects/{slug}/index.md edits.)
// ---------------------------------------------------------------------------

export interface MasterPlanPreambleWriteResult {
  slug: string;
  bytes: number;
  updated_at: string;
  change_log_entry_id: number | null;
}

export async function mutateMasterPlanPreambleWrite(
  slug: string,
  preamble: string,
  changeLog?: {
    kind: string;
    body: string;
    actor?: string | null;
    commit_sha?: string | null;
  } | null,
): Promise<MasterPlanPreambleWriteResult> {
  const cleanSlug = (slug ?? "").trim();
  if (!cleanSlug) throw new IaDbValidationError("slug is required");
  if (typeof preamble !== "string") {
    throw new IaDbValidationError("preamble must be a string");
  }
  return withTx(async (c) => {
    const guard = await c.query(
      `SELECT 1 FROM ia_master_plans WHERE slug = $1 FOR UPDATE`,
      [cleanSlug],
    );
    if (guard.rowCount === 0) {
      throw new IaDbValidationError(`master plan not found: ${cleanSlug}`);
    }
    const upd = await c.query<{ updated_at: string }>(
      `UPDATE ia_master_plans
          SET preamble = $2, updated_at = now()
        WHERE slug = $1
       RETURNING updated_at`,
      [cleanSlug, preamble],
    );
    let entryId: number | null = null;
    if (changeLog && changeLog.kind && changeLog.body) {
      const ins = await c.query<{ entry_id: string }>(
        `INSERT INTO ia_master_plan_change_log
           (slug, kind, body, actor, commit_sha)
         VALUES ($1, $2, $3, $4, $5)
         RETURNING entry_id::text AS entry_id`,
        [
          cleanSlug,
          changeLog.kind,
          changeLog.body,
          changeLog.actor ?? null,
          changeLog.commit_sha ?? null,
        ],
      );
      entryId = parseInt(ins.rows[0]!.entry_id, 10);
    }
    return {
      slug: cleanSlug,
      bytes: Buffer.byteLength(preamble, "utf8"),
      updated_at: upd.rows[0]!.updated_at,
      change_log_entry_id: entryId,
    };
  });
}

export interface MasterPlanDescriptionWriteResult {
  slug: string;
  bytes: number;
  updated_at: string;
  change_log_entry_id: number | null;
}

/**
 * Replace the short product-overview blurb on `ia_master_plans.description`.
 *
 * Soft 200-char target — advisory only, not enforced. Mirror of
 * mutateMasterPlanPreambleWrite. Authored by master-plan-new from the
 * preamble; replaces the verbose preamble as the primary dashboard subtitle.
 */
export async function mutateMasterPlanDescriptionWrite(
  slug: string,
  description: string,
  changeLog?: {
    kind: string;
    body: string;
    actor?: string | null;
    commit_sha?: string | null;
  } | null,
): Promise<MasterPlanDescriptionWriteResult> {
  const cleanSlug = (slug ?? "").trim();
  if (!cleanSlug) throw new IaDbValidationError("slug is required");
  if (typeof description !== "string") {
    throw new IaDbValidationError("description must be a string");
  }
  return withTx(async (c) => {
    const guard = await c.query(
      `SELECT 1 FROM ia_master_plans WHERE slug = $1 FOR UPDATE`,
      [cleanSlug],
    );
    if (guard.rowCount === 0) {
      throw new IaDbValidationError(`master plan not found: ${cleanSlug}`);
    }
    const upd = await c.query<{ updated_at: string }>(
      `UPDATE ia_master_plans
          SET description = $2, updated_at = now()
        WHERE slug = $1
       RETURNING updated_at`,
      [cleanSlug, description],
    );
    let entryId: number | null = null;
    if (changeLog && changeLog.kind && changeLog.body) {
      const ins = await c.query<{ entry_id: string }>(
        `INSERT INTO ia_master_plan_change_log
           (slug, kind, body, actor, commit_sha)
         VALUES ($1, $2, $3, $4, $5)
         RETURNING entry_id::text AS entry_id`,
        [
          cleanSlug,
          changeLog.kind,
          changeLog.body,
          changeLog.actor ?? null,
          changeLog.commit_sha ?? null,
        ],
      );
      entryId = parseInt(ins.rows[0]!.entry_id, 10);
    }
    return {
      slug: cleanSlug,
      bytes: Buffer.byteLength(description, "utf8"),
      updated_at: upd.rows[0]!.updated_at,
      change_log_entry_id: entryId,
    };
  });
}

/**
 * Append one change-log row.
 *
 * UNIQUE `(slug, stage_id, kind, commit_sha)` constraint (migration 0042)
 * lets repeat closeout chains rely on idempotent appends — `ON CONFLICT DO
 * NOTHING` returns `deduped: true` + `entry_id: null` instead of raising
 * `23505`. Callers that need to distinguish first-write vs dedup inspect
 * the returned `deduped` flag.
 *
 * NULL columns are distinct under PG default UNIQUE semantics, so legacy
 * plan-scope rows (no `stage_id` / no `commit_sha`) never collide.
 */
export async function mutateMasterPlanChangeLogAppend(
  slug: string,
  kind: string,
  body: string,
  opts: {
    actor?: string | null;
    commit_sha?: string | null;
    stage_id?: string | null;
  } = {},
): Promise<{ entry_id: number | null; ts: string | null; deduped: boolean }> {
  const cleanSlug = (slug ?? "").trim();
  const cleanKind = (kind ?? "").trim();
  const cleanBody = body ?? "";
  if (!cleanSlug) throw new IaDbValidationError("slug is required");
  if (!cleanKind) throw new IaDbValidationError("kind is required");
  if (!cleanBody) throw new IaDbValidationError("body is required");
  return withTx(async (c) => {
    const guard = await c.query(
      `SELECT 1 FROM ia_master_plans WHERE slug = $1`,
      [cleanSlug],
    );
    if (guard.rowCount === 0) {
      throw new IaDbValidationError(`master plan not found: ${cleanSlug}`);
    }
    const res = await c.query<{ entry_id: string; ts: string }>(
      `INSERT INTO ia_master_plan_change_log
         (slug, stage_id, kind, body, actor, commit_sha)
       VALUES ($1, $2, $3, $4, $5, $6)
       ON CONFLICT ON CONSTRAINT ia_master_plan_change_log_unique
       DO NOTHING
       RETURNING entry_id::text AS entry_id, ts`,
      [
        cleanSlug,
        opts.stage_id ?? null,
        cleanKind,
        cleanBody,
        opts.actor ?? null,
        opts.commit_sha ?? null,
      ],
    );
    if (res.rowCount === 0) {
      return { entry_id: null, ts: null, deduped: true };
    }
    return {
      entry_id: parseInt(res.rows[0]!.entry_id, 10),
      ts: res.rows[0]!.ts,
      deduped: false,
    };
  });
}

// ---------------------------------------------------------------------------
// master_plan_insert
// (Wave 1.5 backfill — author master plans DB-side post-refactor.)
// ---------------------------------------------------------------------------

export interface MasterPlanInsertResult {
  slug: string;
  title: string;
  created_at: string;
  updated_at: string;
}

export async function mutateMasterPlanInsert(
  slug: string,
  title: string,
  preamble: string | null = null,
  description: string | null = null,
): Promise<MasterPlanInsertResult> {
  const cleanSlug = (slug ?? "").trim();
  const cleanTitle = (title ?? "").trim();
  if (!cleanSlug) throw new IaDbValidationError("slug is required");
  if (!cleanTitle) throw new IaDbValidationError("title is required");
  if (!/^[a-z0-9][a-z0-9-]*$/.test(cleanSlug)) {
    throw new IaDbValidationError(
      `slug must be kebab-case [a-z0-9-]: ${cleanSlug}`,
    );
  }
  return withTx(async (c) => {
    const exists = await c.query(
      `SELECT 1 FROM ia_master_plans WHERE slug = $1`,
      [cleanSlug],
    );
    if ((exists.rowCount ?? 0) > 0) {
      throw new IaDbValidationError(`master plan already exists: ${cleanSlug}`);
    }
    const ins = await c.query<{ created_at: string; updated_at: string }>(
      `INSERT INTO ia_master_plans (slug, title, preamble, description)
       VALUES ($1, $2, $3, $4)
       RETURNING created_at, updated_at`,
      [cleanSlug, cleanTitle, preamble, description],
    );
    return {
      slug: cleanSlug,
      title: cleanTitle,
      created_at: ins.rows[0]!.created_at,
      updated_at: ins.rows[0]!.updated_at,
    };
  });
}

// ---------------------------------------------------------------------------
// master_plan_close — flip ia_master_plans.closed_at = now()
// (ship-protocol Stage 4 / TECH-12644 — sibling to master_plan_version_create.)
// ---------------------------------------------------------------------------

export interface MasterPlanCloseResult {
  slug: string;
  version: number;
  closed_at: string;
  /** True when closed_at was already set on entry — idempotent no-op. */
  already_closed: boolean;
}

export async function mutateMasterPlanClose(
  slug: string,
): Promise<MasterPlanCloseResult> {
  const cleanSlug = (slug ?? "").trim();
  if (!cleanSlug) throw new IaDbValidationError("slug is required");

  return withTx(async (c) => {
    const cur = await c.query<{ version: number; closed_at: string | null }>(
      `SELECT version, closed_at::text AS closed_at
         FROM ia_master_plans
        WHERE slug = $1
        FOR UPDATE`,
      [cleanSlug],
    );
    if (cur.rowCount === 0) {
      throw new IaDbValidationError(`master plan not found: ${cleanSlug}`);
    }
    const row = cur.rows[0]!;
    if (row.closed_at !== null) {
      return {
        slug: cleanSlug,
        version: row.version,
        closed_at: row.closed_at,
        already_closed: true,
      };
    }
    const upd = await c.query<{ closed_at: string }>(
      `UPDATE ia_master_plans
          SET closed_at = now(), updated_at = now()
        WHERE slug = $1
        RETURNING closed_at::text AS closed_at`,
      [cleanSlug],
    );
    return {
      slug: cleanSlug,
      version: row.version,
      closed_at: upd.rows[0]!.closed_at,
      already_closed: false,
    };
  });
}

// ---------------------------------------------------------------------------
// master_plan_version_create — emit v(N+1) row chained to a closed parent
// (ship-protocol Stage 4 / TECH-12644.)
// ---------------------------------------------------------------------------

export interface MasterPlanVersionCreateInput {
  parent_slug: string;
  /** Optional override slug for the new version row. Defaults to `{parent}-v{N+1}`. */
  child_slug?: string;
  /** Optional override title for the new row. Defaults to parent title. */
  title?: string;
}

export interface MasterPlanVersionCreateResult {
  parent_slug: string;
  parent_version: number;
  child_slug: string;
  child_version: number;
  child_created_at: string;
}

export async function mutateMasterPlanVersionCreate(
  input: MasterPlanVersionCreateInput,
): Promise<MasterPlanVersionCreateResult> {
  const parentSlug = (input.parent_slug ?? "").trim();
  if (!parentSlug) throw new IaDbValidationError("parent_slug is required");

  return withTx(async (c) => {
    const parent = await c.query<{
      version: number;
      closed_at: string | null;
      title: string;
    }>(
      `SELECT version, closed_at::text AS closed_at, title
         FROM ia_master_plans
        WHERE slug = $1
        FOR UPDATE`,
      [parentSlug],
    );
    if (parent.rowCount === 0) {
      throw new IaDbValidationError(`parent plan not found: ${parentSlug}`);
    }
    const p = parent.rows[0]!;
    if (p.closed_at === null) {
      // Closure must precede new-version creation — guards version-chain audit.
      const e = new IaDbValidationError(
        `parent_not_closed: ${parentSlug} has closed_at=NULL — run ship-final first`,
      );
      e.code = "parent_not_closed";
      throw e;
    }

    const childVersion = p.version + 1;
    const childSlug =
      (input.child_slug ?? "").trim() || `${parentSlug}-v${childVersion}`;
    if (!/^[a-z0-9][a-z0-9-]*$/.test(childSlug)) {
      throw new IaDbValidationError(
        `child_slug must be kebab-case [a-z0-9-]: ${childSlug}`,
      );
    }

    const collide = await c.query(
      `SELECT 1 FROM ia_master_plans WHERE slug = $1`,
      [childSlug],
    );
    if ((collide.rowCount ?? 0) > 0) {
      const e = new IaDbValidationError(
        `slug_collision: ${childSlug} already exists`,
      );
      e.code = "slug_collision";
      throw e;
    }

    const childTitle = (input.title ?? "").trim() || p.title;

    const ins = await c.query<{ created_at: string }>(
      `INSERT INTO ia_master_plans
         (slug, title, parent_plan_slug, version, closed_at)
       VALUES ($1, $2, $3, $4, NULL)
       RETURNING created_at::text AS created_at`,
      [childSlug, childTitle, parentSlug, childVersion],
    );

    return {
      parent_slug: parentSlug,
      parent_version: p.version,
      child_slug: childSlug,
      child_version: childVersion,
      child_created_at: ins.rows[0]!.created_at,
    };
  });
}
