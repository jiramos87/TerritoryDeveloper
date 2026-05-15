/**
 * ia_plan_designs mutations + queries.
 *
 * DB-primary design seed storage (mig 0158). Owns slug + priority +
 * lifecycle status (draft / ready / consumed / archived). Written by
 * design-explore Phase 0 (insert draft) and Phase 4 (update to ready).
 * Read by ship-plan Phase A.0; auto-flipped to 'consumed' by
 * master_plan_bundle_apply (mig 0159) when bundle.plan.design_id is set.
 */

import { IaDbValidationError, poolOrThrow, withTx } from "./shared.js";

const PRIORITY_VALUES = ["P0", "P1", "P2", "P3"] as const;
type Priority = (typeof PRIORITY_VALUES)[number];

const STATUS_VALUES = ["draft", "ready", "consumed", "archived"] as const;
type Status = (typeof STATUS_VALUES)[number];

function assertPriority(p: string | undefined | null): Priority {
  if (p === undefined || p === null) return "P2";
  if (!(PRIORITY_VALUES as readonly string[]).includes(p)) {
    throw new IaDbValidationError(
      `priority must be one of P0/P1/P2/P3 (got: ${p})`,
    );
  }
  return p as Priority;
}

function assertStatus(s: string): Status {
  if (!(STATUS_VALUES as readonly string[]).includes(s)) {
    throw new IaDbValidationError(
      `status must be one of draft/ready/consumed/archived (got: ${s})`,
    );
  }
  return s as Status;
}

function assertKebabSlug(slug: string): string {
  const clean = (slug ?? "").trim();
  if (!clean) throw new IaDbValidationError("slug is required");
  if (!/^[a-z0-9][a-z0-9-]*$/.test(clean)) {
    throw new IaDbValidationError(
      `slug must be kebab-case [a-z0-9-]: ${clean}`,
    );
  }
  return clean;
}

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

export interface PlanDesignRow {
  id: number;
  slug: string;
  title: string;
  priority: Priority;
  status: Status;
  body_md: string | null;
  stages_yaml: unknown | null;
  parent_plan_slug: string | null;
  target_version: number;
  created_at: string;
  updated_at: string;
}

export interface PlanDesignInsertInput {
  slug: string;
  title: string;
  priority?: string;
  parent_plan_slug?: string | null;
  target_version?: number;
}

export interface PlanDesignInsertResult {
  id: number;
  slug: string;
  status: Status;
  priority: Priority;
  created_at: string;
}

export interface PlanDesignUpdateInput {
  slug: string;
  title?: string;
  priority?: string;
  status?: string;
  body_md?: string | null;
  stages_yaml?: unknown | null;
  parent_plan_slug?: string | null;
  target_version?: number;
}

export interface PlanDesignUpdateResult {
  slug: string;
  status: Status;
  priority: Priority;
  updated_at: string;
}

export interface PlanDesignListInput {
  status?: string;
  priority?: string;
  limit?: number;
}

export interface PlanDesignPromoteResult {
  slug: string;
  id: number;
  status: Status;
  master_plan_slug: string | null;
  already_consumed: boolean;
}

// ---------------------------------------------------------------------------
// Row helper
// ---------------------------------------------------------------------------

function mapRow(r: {
  id: string | number;
  slug: string;
  title: string;
  priority: string;
  status: string;
  body_md: string | null;
  stages_yaml: unknown | null;
  parent_plan_slug: string | null;
  target_version: number;
  created_at: string;
  updated_at: string;
}): PlanDesignRow {
  return {
    id: typeof r.id === "string" ? parseInt(r.id, 10) : r.id,
    slug: r.slug,
    title: r.title,
    priority: r.priority as Priority,
    status: r.status as Status,
    body_md: r.body_md,
    stages_yaml: r.stages_yaml,
    parent_plan_slug: r.parent_plan_slug,
    target_version: r.target_version,
    created_at: r.created_at,
    updated_at: r.updated_at,
  };
}

// ---------------------------------------------------------------------------
// mutatePlanDesignInsert
// ---------------------------------------------------------------------------

export async function mutatePlanDesignInsert(
  input: PlanDesignInsertInput,
): Promise<PlanDesignInsertResult> {
  const slug = assertKebabSlug(input.slug);
  const title = (input.title ?? "").trim();
  if (!title) throw new IaDbValidationError("title is required");
  const priority = assertPriority(input.priority);
  const parent = input.parent_plan_slug ?? null;
  const targetVersion = input.target_version ?? 1;

  return withTx(async (c) => {
    const exists = await c.query(
      `SELECT 1 FROM ia_plan_designs WHERE slug = $1`,
      [slug],
    );
    if ((exists.rowCount ?? 0) > 0) {
      const e = new IaDbValidationError(
        `plan_design_slug_collision: ${slug}`,
      );
      e.code = "slug_collision";
      throw e;
    }
    const ins = await c.query<{
      id: string;
      created_at: string;
    }>(
      `INSERT INTO ia_plan_designs
         (slug, title, priority, parent_plan_slug, target_version, status)
       VALUES ($1, $2, $3, $4, $5, 'draft')
       RETURNING id::text AS id, created_at::text AS created_at`,
      [slug, title, priority, parent, targetVersion],
    );
    return {
      id: parseInt(ins.rows[0]!.id, 10),
      slug,
      status: "draft",
      priority,
      created_at: ins.rows[0]!.created_at,
    };
  });
}

// ---------------------------------------------------------------------------
// mutatePlanDesignUpdate
// ---------------------------------------------------------------------------

export async function mutatePlanDesignUpdate(
  input: PlanDesignUpdateInput,
): Promise<PlanDesignUpdateResult> {
  const slug = assertKebabSlug(input.slug);

  const updates: string[] = [];
  const params: unknown[] = [slug];
  let idx = 2;

  if (input.title !== undefined) {
    const t = (input.title ?? "").trim();
    if (!t) throw new IaDbValidationError("title cannot be empty");
    updates.push(`title = $${idx++}`);
    params.push(t);
  }
  if (input.priority !== undefined) {
    const p = assertPriority(input.priority);
    updates.push(`priority = $${idx++}`);
    params.push(p);
  }
  if (input.status !== undefined) {
    const s = assertStatus(input.status);
    updates.push(`status = $${idx++}`);
    params.push(s);
  }
  if (input.body_md !== undefined) {
    updates.push(`body_md = $${idx++}`);
    params.push(input.body_md);
  }
  if (input.stages_yaml !== undefined) {
    updates.push(`stages_yaml = $${idx++}::jsonb`);
    params.push(
      input.stages_yaml === null ? null : JSON.stringify(input.stages_yaml),
    );
  }
  if (input.parent_plan_slug !== undefined) {
    updates.push(`parent_plan_slug = $${idx++}`);
    params.push(input.parent_plan_slug);
  }
  if (input.target_version !== undefined) {
    updates.push(`target_version = $${idx++}`);
    params.push(input.target_version);
  }

  if (updates.length === 0) {
    throw new IaDbValidationError("at least one field must be provided");
  }

  updates.push(`updated_at = now()`);

  return withTx(async (c) => {
    const guard = await c.query(
      `SELECT 1 FROM ia_plan_designs WHERE slug = $1 FOR UPDATE`,
      [slug],
    );
    if (guard.rowCount === 0) {
      throw new IaDbValidationError(`plan_design not found: ${slug}`);
    }
    const upd = await c.query<{
      status: string;
      priority: string;
      updated_at: string;
    }>(
      `UPDATE ia_plan_designs
          SET ${updates.join(", ")}
        WHERE slug = $1
       RETURNING status, priority, updated_at::text AS updated_at`,
      params,
    );
    return {
      slug,
      status: upd.rows[0]!.status as Status,
      priority: upd.rows[0]!.priority as Priority,
      updated_at: upd.rows[0]!.updated_at,
    };
  });
}

// ---------------------------------------------------------------------------
// queryPlanDesignGet
// ---------------------------------------------------------------------------

export async function queryPlanDesignGet(
  slug: string,
): Promise<PlanDesignRow | null> {
  const clean = assertKebabSlug(slug);
  const pool = poolOrThrow();
  const res = await pool.query(
    `SELECT id::text AS id, slug, title, priority, status,
            body_md, stages_yaml,
            parent_plan_slug, target_version,
            created_at::text AS created_at,
            updated_at::text AS updated_at
       FROM ia_plan_designs
      WHERE slug = $1`,
    [clean],
  );
  if (res.rowCount === 0) return null;
  return mapRow(res.rows[0]!);
}

// ---------------------------------------------------------------------------
// queryPlanDesignList
// ---------------------------------------------------------------------------

export async function queryPlanDesignList(
  input: PlanDesignListInput,
): Promise<PlanDesignRow[]> {
  const where: string[] = [];
  const params: unknown[] = [];
  let idx = 1;
  if (input.status !== undefined) {
    where.push(`status = $${idx++}`);
    params.push(assertStatus(input.status));
  }
  if (input.priority !== undefined) {
    where.push(`priority = $${idx++}`);
    params.push(assertPriority(input.priority));
  }
  const limit = Math.max(1, Math.min(input.limit ?? 100, 500));
  params.push(limit);

  const pool = poolOrThrow();
  const res = await pool.query(
    `SELECT id::text AS id, slug, title, priority, status,
            body_md, stages_yaml,
            parent_plan_slug, target_version,
            created_at::text AS created_at,
            updated_at::text AS updated_at
       FROM ia_plan_designs
      ${where.length > 0 ? `WHERE ${where.join(" AND ")}` : ""}
      ORDER BY
        CASE priority WHEN 'P0' THEN 0 WHEN 'P1' THEN 1 WHEN 'P2' THEN 2 ELSE 3 END,
        updated_at DESC
      LIMIT $${idx}`,
    params,
  );
  return res.rows.map(mapRow);
}

// ---------------------------------------------------------------------------
// mutatePlanDesignPromote — manual flip to consumed + optional FK link.
// (Normally master_plan_bundle_apply does this inline; promote is the
//  out-of-band escape hatch for retries or backfills.)
// ---------------------------------------------------------------------------

export async function mutatePlanDesignPromote(
  slug: string,
  master_plan_slug: string | null = null,
): Promise<PlanDesignPromoteResult> {
  const cleanSlug = assertKebabSlug(slug);

  return withTx(async (c) => {
    const cur = await c.query<{ id: string; status: string }>(
      `SELECT id::text AS id, status
         FROM ia_plan_designs
        WHERE slug = $1
        FOR UPDATE`,
      [cleanSlug],
    );
    if (cur.rowCount === 0) {
      throw new IaDbValidationError(`plan_design not found: ${cleanSlug}`);
    }
    const row = cur.rows[0]!;
    const designId = parseInt(row.id, 10);
    if (row.status === "consumed") {
      return {
        slug: cleanSlug,
        id: designId,
        status: "consumed",
        master_plan_slug,
        already_consumed: true,
      };
    }
    await c.query(
      `UPDATE ia_plan_designs
          SET status = 'consumed', updated_at = now()
        WHERE id = $1`,
      [designId],
    );

    if (master_plan_slug) {
      const planSlug = assertKebabSlug(master_plan_slug);
      const planGuard = await c.query(
        `SELECT 1 FROM ia_master_plans WHERE slug = $1 FOR UPDATE`,
        [planSlug],
      );
      if (planGuard.rowCount === 0) {
        throw new IaDbValidationError(`master_plan not found: ${planSlug}`);
      }
      await c.query(
        `UPDATE ia_master_plans
            SET design_id = $1, updated_at = now()
          WHERE slug = $2 AND (design_id IS NULL OR design_id = $1)`,
        [designId, planSlug],
      );
    }

    return {
      slug: cleanSlug,
      id: designId,
      status: "consumed",
      master_plan_slug,
      already_consumed: false,
    };
  });
}
