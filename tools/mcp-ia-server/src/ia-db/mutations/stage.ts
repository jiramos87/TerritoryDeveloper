/**
 * Stage-domain mutations: verification-flip, closeout-apply, insert,
 * update, body-write, decompose-apply.
 */

import { getIaDatabasePool } from "../pool.js";
import { tarjanScc } from "../tarjan-scc.js";
import {
  enqueueCacheBust,
  IaDbValidationError,
  poolOrThrow,
  PREFIX_SEQ,
  withTx,
} from "./shared.js";
import type { TaskBatchInsertItem } from "./task.js";

// ---------------------------------------------------------------------------
// stage_verification_flip
// ---------------------------------------------------------------------------

const STAGE_VERDICTS = ["pass", "fail", "partial"] as const;

export async function mutateStageVerificationFlip(
  slug: string,
  stage_id: string,
  verdict: (typeof STAGE_VERDICTS)[number],
  opts: { commit_sha?: string | null; notes?: string | null; actor?: string | null } = {},
): Promise<{ id: number; verified_at: string }> {
  if (!STAGE_VERDICTS.includes(verdict)) {
    throw new IaDbValidationError(`invalid verdict: ${verdict}`);
  }
  return withTx(async (c) => {
    const sr = await c.query(
      `SELECT 1 FROM ia_stages WHERE slug = $1 AND stage_id = $2`,
      [slug, stage_id],
    );
    if (sr.rowCount === 0) {
      throw new IaDbValidationError(`stage not found: ${slug}/${stage_id}`);
    }
    const res = await c.query<{ id: string; verified_at: string }>(
      `INSERT INTO ia_stage_verifications
         (slug, stage_id, verdict, commit_sha, notes, actor)
       VALUES ($1, $2, $3::stage_verdict, $4, $5, $6)
       RETURNING id::text AS id, verified_at`,
      [
        slug,
        stage_id,
        verdict,
        opts.commit_sha ?? null,
        opts.notes ?? null,
        opts.actor ?? null,
      ],
    );
    return {
      id: parseInt(res.rows[0]!.id, 10),
      verified_at: res.rows[0]!.verified_at,
    };
  });
}

// ---------------------------------------------------------------------------
// stage_closeout_apply (TECH-2975 — txn wrap + per-step audit trail)
// ---------------------------------------------------------------------------

const CLOSEOUT_AUDIT_PHASE = "stage_closeout";
const CLOSEOUT_AUDIT_KIND_PREFIX = "closeout_step.";

/**
 * Persist one audit row to `ia_ship_stage_journal` outside any transaction.
 *
 * Rationale (TECH-2975 §Implementer Latitude): the §Plan Digest §Acceptance
 * promises that `stage_closeout_diagnose` against a *failed* closeout shows
 * the step where the error occurred. If audit rows shared the closeout
 * txn, ROLLBACK would discard them along with state, defeating forensic
 * use. We trade per-step audit atomicity for visible failure trails —
 * state mutations remain atomic via the closeout `withTx`.
 *
 * `payload_kind = "closeout_step.<step_name>"` so the diagnose reader can
 * filter purely by prefix without touching payload jsonb.
 */
async function appendCloseoutAuditRow(
  slug: string,
  stage_id: string,
  step_name: string,
  ok: boolean,
  error?: string | null,
): Promise<void> {
  const pool = poolOrThrow();
  const session_id = `closeout-${slug}-${stage_id}`;
  await pool.query(
    `INSERT INTO ia_ship_stage_journal
       (session_id, slug, stage_id, phase, payload_kind, payload)
     VALUES ($1, $2, $3, $4, $5, $6::jsonb)`,
    [
      session_id,
      slug,
      stage_id,
      CLOSEOUT_AUDIT_PHASE,
      `${CLOSEOUT_AUDIT_KIND_PREFIX}${step_name}`,
      JSON.stringify({ step_name, ok, error: error ?? null }),
    ],
  );
}

export async function mutateStageCloseoutApply(
  slug: string,
  stage_id: string,
): Promise<{ slug: string; stage_id: string; archived_task_count: number; stage_status: "done" }> {
  // Audit log captured in-memory through the txn; flushed post-commit/rollback
  // so failed closeouts retain a forensic trail (rollback would otherwise
  // discard rows written via the same client `c`).
  const trail: Array<{ step_name: string; ok: boolean; error?: string | null }> = [];
  let lastStep = "init";
  try {
    const result = await withTx(async (c) => {
      lastStep = "stage_lock";
      const sr = await c.query(
        `SELECT 1 FROM ia_stages WHERE slug = $1 AND stage_id = $2 FOR UPDATE`,
        [slug, stage_id],
      );
      if (sr.rowCount === 0) {
        throw new IaDbValidationError(`stage not found: ${slug}/${stage_id}`);
      }
      trail.push({ step_name: "stage_lock", ok: true });

      lastStep = "non_terminal_check";
      const nonTerminal = await c.query<{ task_id: string; status: string }>(
        `SELECT task_id, status::text AS status
           FROM ia_tasks
          WHERE slug = $1 AND stage_id = $2
            AND status NOT IN ('done', 'archived')`,
        [slug, stage_id],
      );
      if (nonTerminal.rowCount! > 0) {
        const ids = nonTerminal.rows.map((r) => `${r.task_id}(${r.status})`).join(", ");
        throw new IaDbValidationError(
          `stage has non-terminal tasks: ${ids} — must be done or archived before closeout`,
        );
      }
      trail.push({ step_name: "non_terminal_check", ok: true });

      lastStep = "archive_done_tasks";
      const upd = await c.query<{ task_id: string }>(
        `UPDATE ia_tasks
            SET status = 'archived'::task_status,
                archived_at = COALESCE(archived_at, now()),
                updated_at = now()
          WHERE slug = $1 AND stage_id = $2 AND status = 'done'
         RETURNING task_id`,
        [slug, stage_id],
      );
      trail.push({ step_name: "archive_done_tasks", ok: true });

      lastStep = "stage_status_done";
      await c.query(
        `UPDATE ia_stages
            SET status = 'done'::stage_status, updated_at = now()
          WHERE slug = $1 AND stage_id = $2`,
        [slug, stage_id],
      );
      trail.push({ step_name: "stage_status_done", ok: true });

      return {
        slug,
        stage_id,
        archived_task_count: upd.rowCount ?? 0,
        stage_status: "done" as const,
      };
    });
    // Flush successful trail post-commit. Each row gets its own txn-less
    // INSERT; failures here surface as bare errors but do not roll back the
    // closeout itself (state is already committed).
    for (const row of trail) {
      await appendCloseoutAuditRow(slug, stage_id, row.step_name, row.ok, row.error ?? null);
    }
    // Cache-bust: stage closeout changes stage + task state — invalidate
    // cached db_read_batch results (TECH-18106).
    enqueueCacheBust("db_read_batch", "db_read_batch:%");
    return result;
  } catch (e) {
    // Flush partial trail + failure marker for the failed step. Best-effort —
    // audit-write failures are swallowed so the original closeout error is
    // surfaced unchanged to the caller.
    try {
      for (const row of trail) {
        await appendCloseoutAuditRow(slug, stage_id, row.step_name, row.ok, row.error ?? null);
      }
      const err = e instanceof Error ? e.message : String(e);
      await appendCloseoutAuditRow(slug, stage_id, lastStep, false, err);
    } catch {
      /* best-effort */
    }
    throw e;
  }
}

// ---------------------------------------------------------------------------
// master_plan_insert / stage_insert / stage_update
// (Wave 1.5 backfill — author master plans + stages DB-side post-refactor.)
// ---------------------------------------------------------------------------

export interface StageInsertInput {
  slug: string;
  stage_id: string;
  title?: string | null;
  objective?: string | null;
  exit_criteria?: string | null;
  body?: string | null;
  status?: "pending" | "in_progress" | "partial" | "done";
  /**
   * Optional list of `arch_surfaces.slug` values to link this Stage to in
   * the `stage_arch_surfaces` join table (DEC-A12 — link-table storage
   * shape, normalized + indexable). Each slug must already exist in
   * `arch_surfaces` (Invariant #12 — linker MUST NOT auto-create surfaces).
   * Empty / null = no surface links (cross-cutting tooling Stages).
   */
  arch_surfaces?: string[] | null;
  /** parallel-carcass D3 — 'carcass' | 'section' | null (legacy linear). */
  carcass_role?: "carcass" | "section" | null;
  /** parallel-carcass D19 — section cluster id; NULL for carcass + legacy. */
  section_id?: string | null;
}

export interface StageInsertResult {
  slug: string;
  stage_id: string;
  status: string;
  created_at: string;
  updated_at: string;
}

export async function mutateStageInsert(
  input: StageInsertInput,
): Promise<StageInsertResult> {
  const cleanSlug = (input.slug ?? "").trim();
  const cleanStageId = (input.stage_id ?? "").trim();
  if (!cleanSlug) throw new IaDbValidationError("slug is required");
  if (!cleanStageId) throw new IaDbValidationError("stage_id is required");
  if (!/^[0-9]+(\.[0-9]+)?$/.test(cleanStageId)) {
    throw new IaDbValidationError(
      `stage_id must match N or N.M (e.g. "5" or "5.4"): ${cleanStageId}`,
    );
  }
  const status = input.status ?? "pending";
  if (!["pending", "in_progress", "partial", "done"].includes(status)) {
    throw new IaDbValidationError(`invalid status: ${status}`);
  }
  return withTx(async (c) => {
    const planGuard = await c.query(
      `SELECT 1 FROM ia_master_plans WHERE slug = $1`,
      [cleanSlug],
    );
    if (planGuard.rowCount === 0) {
      throw new IaDbValidationError(`master plan not found: ${cleanSlug}`);
    }
    const exists = await c.query(
      `SELECT 1 FROM ia_stages WHERE slug = $1 AND stage_id = $2`,
      [cleanSlug, cleanStageId],
    );
    if ((exists.rowCount ?? 0) > 0) {
      throw new IaDbValidationError(
        `stage already exists: ${cleanSlug}/${cleanStageId}`,
      );
    }
    const carcassRole = input.carcass_role ?? null;
    if (carcassRole !== null && !["carcass", "section"].includes(carcassRole)) {
      throw new IaDbValidationError(
        `invalid carcass_role: ${carcassRole}; must be 'carcass', 'section', or null`,
      );
    }
    const ins = await c.query<{ created_at: string; updated_at: string }>(
      `INSERT INTO ia_stages (slug, stage_id, title, objective, exit_criteria, body, status, carcass_role, section_id)
       VALUES ($1, $2, $3, $4, $5, $6, $7::stage_status, $8, $9)
       RETURNING created_at, updated_at`,
      [
        cleanSlug,
        cleanStageId,
        input.title ?? null,
        input.objective ?? null,
        input.exit_criteria ?? null,
        input.body ?? "",
        status,
        carcassRole,
        input.section_id ?? null,
      ],
    );

    // ---- arch_surfaces link table (DEC-A12) ------------------------------
    // Normalize, dedupe, pre-validate against arch_surfaces table, then
    // bulk-insert into stage_arch_surfaces inside the same tx. Invariant
    // #12 — linker MUST NOT auto-create surfaces; unknown slugs reject.
    const archInput = input.arch_surfaces ?? null;
    if (archInput && archInput.length > 0) {
      const surfaces = [
        ...new Set(
          archInput.map((s) => (s ?? "").trim()).filter((s) => s.length > 0),
        ),
      ];
      if (surfaces.length > 0) {
        const existsRes = await c.query<{ slug: string }>(
          `SELECT slug FROM arch_surfaces WHERE slug = ANY($1::text[])`,
          [surfaces],
        );
        const found = new Set(existsRes.rows.map((r) => r.slug));
        const missing = surfaces.filter((s) => !found.has(s));
        if (missing.length > 0) {
          throw new IaDbValidationError(
            `unknown arch_surfaces slugs: ${missing.join(", ")} — Invariant #12 forbids auto-create`,
          );
        }
        await c.query(
          `INSERT INTO stage_arch_surfaces (slug, stage_id, surface_slug)
             SELECT $1, $2, unnest($3::text[])
           ON CONFLICT (slug, stage_id, surface_slug) DO NOTHING`,
          [cleanSlug, cleanStageId, surfaces],
        );
      }
    }

    return {
      slug: cleanSlug,
      stage_id: cleanStageId,
      status,
      created_at: ins.rows[0]!.created_at,
      updated_at: ins.rows[0]!.updated_at,
    };
  });
}

export interface StageUpdateInput {
  slug: string;
  stage_id: string;
  title?: string | null;
  objective?: string | null;
  exit_criteria?: string | null;
  /** parallel-carcass D3 — 'carcass' | 'section' | null (legacy linear). */
  carcass_role?: "carcass" | "section" | null;
  /** parallel-carcass D19 — section cluster id; NULL for carcass + legacy. */
  section_id?: string | null;
}

export interface StageUpdateResult {
  slug: string;
  stage_id: string;
  updated_fields: string[];
  updated_at: string;
}

export async function mutateStageUpdate(
  input: StageUpdateInput,
): Promise<StageUpdateResult> {
  const cleanSlug = (input.slug ?? "").trim();
  const cleanStageId = (input.stage_id ?? "").trim();
  if (!cleanSlug) throw new IaDbValidationError("slug is required");
  if (!cleanStageId) throw new IaDbValidationError("stage_id is required");
  const fields: string[] = [];
  const values: unknown[] = [cleanSlug, cleanStageId];
  const sets: string[] = [];
  if (input.title !== undefined) {
    sets.push(`title = $${values.length + 1}`);
    values.push(input.title);
    fields.push("title");
  }
  if (input.objective !== undefined) {
    sets.push(`objective = $${values.length + 1}`);
    values.push(input.objective);
    fields.push("objective");
  }
  if (input.exit_criteria !== undefined) {
    sets.push(`exit_criteria = $${values.length + 1}`);
    values.push(input.exit_criteria);
    fields.push("exit_criteria");
  }
  if (input.carcass_role !== undefined) {
    if (input.carcass_role !== null && !["carcass", "section"].includes(input.carcass_role)) {
      throw new IaDbValidationError(
        `invalid carcass_role: ${input.carcass_role}; must be 'carcass', 'section', or null`,
      );
    }
    sets.push(`carcass_role = $${values.length + 1}`);
    values.push(input.carcass_role);
    fields.push("carcass_role");
  }
  if (input.section_id !== undefined) {
    sets.push(`section_id = $${values.length + 1}`);
    values.push(input.section_id);
    fields.push("section_id");
  }
  if (sets.length === 0) {
    throw new IaDbValidationError(
      "at least one of title/objective/exit_criteria/carcass_role/section_id must be provided",
    );
  }
  return withTx(async (c) => {
    const guard = await c.query(
      `SELECT 1 FROM ia_stages WHERE slug = $1 AND stage_id = $2 FOR UPDATE`,
      [cleanSlug, cleanStageId],
    );
    if (guard.rowCount === 0) {
      throw new IaDbValidationError(
        `stage not found: ${cleanSlug}/${cleanStageId}`,
      );
    }
    const upd = await c.query<{ updated_at: string }>(
      `UPDATE ia_stages
          SET ${sets.join(", ")}, updated_at = now()
        WHERE slug = $1 AND stage_id = $2
       RETURNING updated_at`,
      values,
    );
    return {
      slug: cleanSlug,
      stage_id: cleanStageId,
      updated_fields: fields,
      updated_at: upd.rows[0]!.updated_at,
    };
  });
}

// ---------------------------------------------------------------------------
// stage_delete
// (Hard-delete one ia_stages row + cascade-cleanup. Used by reshape paths
// where superseded carcass shells need physical removal — `stage_update`
// only relabels titles, leaving phantom pending rows in `master_plan_state`
// + `master_plan_health`. No status='archived' for stages (only for tasks),
// so DELETE is the only correct tool.)
//
// Cascade matrix (from FK declarations across migrations):
//   ia_tasks                   → ON DELETE RESTRICT (blocks unless stage empty)
//   ia_stage_verifications     → ON DELETE CASCADE
//   stage_arch_surfaces        → ON DELETE CASCADE
//   ia_red_stage_proofs        → ON DELETE CASCADE
//   stage_carcass_signals      → ON DELETE CASCADE
//   ia_stage_claims            → ON DELETE CASCADE
//   ia_master_plan_change_log  → text col only, no FK (audit trail preserved)
//
// Default = strict — refuse if ANY task rows exist on the stage. Caller
// must flip tasks to terminal status (`archived`) + opt in to
// `cascade_archived_tasks=true` to also delete those rows. Non-archived
// task rows ALWAYS block deletion regardless of flag — protects against
// accidental work loss.
// ---------------------------------------------------------------------------

export interface StageDeleteInput {
  slug: string;
  stage_id: string;
  /**
   * When true, also DELETE child `ia_tasks` rows whose status='archived'
   * before deleting the stage. Non-archived task rows always block
   * deletion regardless of this flag — caller must first flip them via
   * `task_status_flip` / `stage_closeout_apply`.
   */
  cascade_archived_tasks?: boolean;
  /** Audit body for `ia_master_plan_change_log` (kind='stage-delete'). */
  audit_note?: string | null;
  /** Audit actor for `ia_master_plan_change_log.actor`. */
  actor?: string | null;
}

export interface StageDeleteResult {
  slug: string;
  stage_id: string;
  deleted_archived_tasks: number;
  cascade_counts: {
    stage_verifications: number;
    arch_surfaces: number;
    red_stage_proofs: number;
    carcass_signals: number;
    stage_claims: number;
  };
  change_log_entry_id: number | null;
}

export async function mutateStageDelete(
  input: StageDeleteInput,
): Promise<StageDeleteResult> {
  const cleanSlug = (input.slug ?? "").trim();
  const cleanStageId = (input.stage_id ?? "").trim();
  if (!cleanSlug) throw new IaDbValidationError("slug is required");
  if (!cleanStageId) throw new IaDbValidationError("stage_id is required");
  const cascadeArchived = input.cascade_archived_tasks === true;

  return withTx(async (c) => {
    // Stage row guard + lock — block concurrent writes during delete.
    const sg = await c.query(
      `SELECT 1 FROM ia_stages WHERE slug = $1 AND stage_id = $2 FOR UPDATE`,
      [cleanSlug, cleanStageId],
    );
    if (sg.rowCount === 0) {
      throw new IaDbValidationError(
        `stage not found: ${cleanSlug}/${cleanStageId}`,
      );
    }

    // Non-terminal task guard — `pending` / `in_progress` / `verified` /
    // `implemented` / `done` rows always block. Only `archived` is OK to
    // sweep via cascade flag.
    const nonArchived = await c.query<{ task_id: string; status: string }>(
      `SELECT task_id, status::text AS status
         FROM ia_tasks
        WHERE slug = $1 AND stage_id = $2
          AND status <> 'archived'
        ORDER BY task_id`,
      [cleanSlug, cleanStageId],
    );
    if ((nonArchived.rowCount ?? 0) > 0) {
      const ids = nonArchived.rows
        .map((r) => `${r.task_id}(${r.status})`)
        .join(", ");
      throw new IaDbValidationError(
        `stage has non-archived tasks: ${ids} — flip to archived (task_status_flip / stage_closeout_apply) before delete`,
      );
    }

    // Archived tasks — count + optionally cascade-delete. Without the flag,
    // archived task rows themselves block delete (FK is RESTRICT — DB would
    // reject anyway; we surface a clearer error here).
    const archivedCount = await c.query<{ n: string }>(
      `SELECT count(*)::text AS n FROM ia_tasks
        WHERE slug = $1 AND stage_id = $2 AND status = 'archived'`,
      [cleanSlug, cleanStageId],
    );
    const archivedN = parseInt(archivedCount.rows[0]!.n, 10);
    let deletedArchivedTasks = 0;
    if (archivedN > 0) {
      if (!cascadeArchived) {
        throw new IaDbValidationError(
          `stage has ${archivedN} archived task(s) — pass cascade_archived_tasks=true to delete them along with the stage`,
        );
      }
      const del = await c.query(
        `DELETE FROM ia_tasks WHERE slug = $1 AND stage_id = $2 AND status = 'archived'`,
        [cleanSlug, cleanStageId],
      );
      deletedArchivedTasks = del.rowCount ?? 0;
    }

    // Pre-count cascade tables for return shape (post-delete counts come
    // from RETURNING but Postgres doesn't return cascade row counts on the
    // parent DELETE — query before).
    const verRes = await c.query<{ n: string }>(
      `SELECT count(*)::text AS n FROM ia_stage_verifications
        WHERE slug = $1 AND stage_id = $2`,
      [cleanSlug, cleanStageId],
    );
    const surfRes = await c.query<{ n: string }>(
      `SELECT count(*)::text AS n FROM stage_arch_surfaces
        WHERE slug = $1 AND stage_id = $2`,
      [cleanSlug, cleanStageId],
    );
    const proofRes = await c.query<{ n: string }>(
      `SELECT count(*)::text AS n FROM ia_red_stage_proofs
        WHERE slug = $1 AND stage_id = $2`,
      [cleanSlug, cleanStageId],
    );
    const sigRes = await c.query<{ n: string }>(
      `SELECT count(*)::text AS n FROM stage_carcass_signals
        WHERE slug = $1 AND stage_id = $2`,
      [cleanSlug, cleanStageId],
    );
    const claimRes = await c.query<{ n: string }>(
      `SELECT count(*)::text AS n FROM ia_stage_claims
        WHERE slug = $1 AND stage_id = $2`,
      [cleanSlug, cleanStageId],
    );

    // Audit row — written BEFORE delete so a successful txn includes the
    // trail row, and a rollback discards both together. Plan-scoped FK
    // (slug → ia_master_plans) on change_log keeps the row intact even
    // though the stage row vanishes; `stage_id` is plain text.
    const auditBody =
      input.audit_note ??
      `stage_delete: ${cleanSlug}/${cleanStageId} (${deletedArchivedTasks} archived task(s) cascade-deleted)`;
    let changeLogEntryId: number | null = null;
    try {
      const audit = await c.query<{ entry_id: string }>(
        `INSERT INTO ia_master_plan_change_log (slug, stage_id, kind, body, actor)
         VALUES ($1, $2, 'stage-delete', $3, $4)
         RETURNING entry_id::text AS entry_id`,
        [cleanSlug, cleanStageId, auditBody, input.actor ?? "stage_delete"],
      );
      changeLogEntryId = parseInt(audit.rows[0]!.entry_id, 10);
    } catch (e) {
      // ia_master_plan_change_log may have UNIQUE(slug, stage_id, kind, commit_sha)
      // — duplicate stage-delete entries (e.g. retry) collapse to no-op so
      // the actual stage row deletion still proceeds.
      const msg = e instanceof Error ? e.message : String(e);
      if (!/duplicate key|unique/i.test(msg)) throw e;
    }

    // The actual delete — cascades clean up child rows in CASCADE tables.
    await c.query(
      `DELETE FROM ia_stages WHERE slug = $1 AND stage_id = $2`,
      [cleanSlug, cleanStageId],
    );

    // Cache-bust: stage delete invalidates plan state / render reads.
    enqueueCacheBust("db_read_batch", "db_read_batch:%");

    return {
      slug: cleanSlug,
      stage_id: cleanStageId,
      deleted_archived_tasks: deletedArchivedTasks,
      cascade_counts: {
        stage_verifications: parseInt(verRes.rows[0]!.n, 10),
        arch_surfaces: parseInt(surfRes.rows[0]!.n, 10),
        red_stage_proofs: parseInt(proofRes.rows[0]!.n, 10),
        carcass_signals: parseInt(sigRes.rows[0]!.n, 10),
        stage_claims: parseInt(claimRes.rows[0]!.n, 10),
      },
      change_log_entry_id: changeLogEntryId,
    };
  });
}

// ---------------------------------------------------------------------------
// stage_body_write
// (Mirror of mutateMasterPlanPreambleWrite for ia_stages.body.)
// ---------------------------------------------------------------------------

export interface StageBodyWriteResult {
  slug: string;
  stage_id: string;
  bytes: number;
  updated_at: string;
}

export async function mutateStageBodyWrite(
  slug: string,
  stage_id: string,
  body: string,
): Promise<StageBodyWriteResult> {
  const cleanSlug = (slug ?? "").trim();
  const cleanStageId = (stage_id ?? "").trim();
  if (!cleanSlug) throw new IaDbValidationError("slug is required");
  if (!cleanStageId) throw new IaDbValidationError("stage_id is required");
  if (typeof body !== "string") {
    throw new IaDbValidationError("body must be a string");
  }
  return withTx(async (c) => {
    const guard = await c.query(
      `SELECT 1 FROM ia_stages WHERE slug = $1 AND stage_id = $2 FOR UPDATE`,
      [cleanSlug, cleanStageId],
    );
    if (guard.rowCount === 0) {
      throw new IaDbValidationError(
        `stage not found: ${cleanSlug}/${cleanStageId}`,
      );
    }
    const upd = await c.query<{ updated_at: string }>(
      `UPDATE ia_stages
          SET body = $3, updated_at = now()
        WHERE slug = $1 AND stage_id = $2
       RETURNING updated_at`,
      [cleanSlug, cleanStageId, body],
    );
    return {
      slug: cleanSlug,
      stage_id: cleanStageId,
      bytes: Buffer.byteLength(body, "utf8"),
      updated_at: upd.rows[0]!.updated_at,
    };
  });
}

// ---------------------------------------------------------------------------
// stage_decompose_apply — db-lifecycle-extensions Stage 3 / TECH-3405
// ---------------------------------------------------------------------------

export interface StageDecomposeApplyInput {
  slug: string;
  stage_id: string;
  prose_block?: {
    title?: string | null;
    objective?: string | null;
    exit_criteria?: string | null;
    body?: string;
  };
  tasks: TaskBatchInsertItem[];
  commit_sha?: string;
}

export interface StageDecomposeApplyResult {
  ok: true;
  ids: string[];
  id_map: Record<string, string>;
  stage_id_resolved: string;
  deduped?: boolean;
}

/**
 * Atomic compose of:
 *   1. Stage prose write (title/objective/exit_criteria/body) on
 *      `ia_stages` row (must exist).
 *   2. Batch task insert via shared mutation path (label-based dep
 *      resolve).
 *
 * Idempotency: dedup keyed on `(slug, stage_id, commit_sha)` against
 * `ia_master_plan_change_log` (UNIQUE constraint shape from Stage 1
 * T1.2). If a matching row exists with the supplied commit_sha, returns
 * `{ok:true, deduped:true}` with existing ids fetched from the stage's
 * task rows. Caller passes `commit_sha` from outer git env; missing
 * `commit_sha` disables dedup.
 *
 * Rollback semantics: any sub-step failure rolls back ALL writes (prose
 * update revert + zero task rows committed). All-or-nothing.
 */
export async function mutateStageDecomposeApply(
  input: StageDecomposeApplyInput,
): Promise<StageDecomposeApplyResult> {
  const slug = (input.slug ?? "").trim();
  const stage_id = (input.stage_id ?? "").trim();
  if (!slug) throw new IaDbValidationError("slug is required");
  if (!stage_id) throw new IaDbValidationError("stage_id is required");
  if (!Array.isArray(input.tasks)) {
    throw new IaDbValidationError("tasks array is required");
  }

  return withTx(async (c) => {
    // Stage row guard (existence required — `/stage-decompose` runs on
    // skeleton stage from `master_plan_extend`, never creates stages).
    const sg = await c.query(
      `SELECT 1 FROM ia_stages WHERE slug = $1 AND stage_id = $2 FOR UPDATE`,
      [slug, stage_id],
    );
    if (sg.rowCount === 0) {
      throw new IaDbValidationError(
        `no stage '${slug}/${stage_id}' in ia_stages`,
      );
    }

    // Idempotency check via `ia_master_plan_change_log` UNIQUE on
    // (slug, stage_id, kind, commit_sha). Mirrors Stage 1 T1.2 shape.
    if (input.commit_sha) {
      const dedup = await c.query<{ entry_id: number }>(
        `SELECT entry_id
           FROM ia_master_plan_change_log
          WHERE slug = $1
            AND stage_id = $2
            AND kind = 'stage-decompose-apply'
            AND commit_sha = $3
          LIMIT 1`,
        [slug, stage_id, input.commit_sha],
      );
      if ((dedup.rowCount ?? 0) > 0) {
        const existing = await c.query<{ task_id: string }>(
          `SELECT task_id FROM ia_tasks WHERE slug = $1 AND stage_id = $2 ORDER BY created_at`,
          [slug, stage_id],
        );
        const ids = existing.rows.map((r) => r.task_id);
        return {
          ok: true as const,
          ids,
          id_map: {},
          stage_id_resolved: stage_id,
          deduped: true,
        };
      }
    }

    // Prose write — ALL-or-NOTHING with task inserts.
    const prose = input.prose_block ?? {};
    const sets: string[] = [];
    const values: unknown[] = [slug, stage_id];
    if (prose.title !== undefined) {
      sets.push(`title = $${values.length + 1}`);
      values.push(prose.title);
    }
    if (prose.objective !== undefined) {
      sets.push(`objective = $${values.length + 1}`);
      values.push(prose.objective);
    }
    if (prose.exit_criteria !== undefined) {
      sets.push(`exit_criteria = $${values.length + 1}`);
      values.push(prose.exit_criteria);
    }
    if (prose.body !== undefined) {
      sets.push(`body = $${values.length + 1}`);
      values.push(prose.body);
    }
    if (sets.length > 0) {
      await c.query(
        `UPDATE ia_stages
            SET ${sets.join(", ")}, updated_at = now()
          WHERE slug = $1 AND stage_id = $2`,
        values,
      );
    }

    // Inline batch insert — duplicates `mutateTaskBatchInsert` core logic
    // inside SAME tx (cannot nest withTx). Pre-flight already runs in
    // mutateTaskBatchInsert; here we re-execute the validation +
    // reservation + insert sequence on the supplied client.
    const labelSeen = new Map<string, number>();
    const collisions: string[] = [];
    for (const t of input.tasks) {
      const lbl = (t.label ?? "").trim();
      if (!lbl) {
        throw new IaDbValidationError("every task item requires a non-empty `label`");
      }
      const title = (t.title ?? "").trim();
      if (!title) {
        throw new IaDbValidationError(`task ${lbl}: title is required`);
      }
      const prev = labelSeen.get(lbl) ?? 0;
      labelSeen.set(lbl, prev + 1);
      if (prev === 1) collisions.push(lbl);
    }
    if (collisions.length > 0) {
      throw new IaDbValidationError(
        `label_collision: ${collisions.join(", ")}`,
      );
    }
    const labels = new Set(input.tasks.map((t) => t.label.trim()));
    const missingLabels = new Set<string>();
    for (const t of input.tasks) {
      const deps = (t.depends_on_labels ?? []).map((s) => s.trim()).filter(Boolean);
      for (const d of deps) {
        if (!labels.has(d)) missingLabels.add(d);
      }
    }
    if (missingLabels.size > 0) {
      throw new IaDbValidationError(
        `unknown_label: ${Array.from(missingLabels).join(", ")}`,
      );
    }
    const labelAdj = new Map<string, string[]>();
    for (const t of input.tasks) {
      labelAdj.set(t.label.trim(), (t.depends_on_labels ?? []).map((s) => s.trim()).filter(Boolean));
    }
    const cycleResult = tarjanScc(Array.from(labels), labelAdj);
    if (cycleResult.multiNodeSccs.length > 0 || cycleResult.selfLoops.length > 0) {
      const offending =
        cycleResult.multiNodeSccs.length > 0
          ? cycleResult.multiNodeSccs[0]!
          : [cycleResult.selfLoops[0]!];
      throw new IaDbValidationError(
        `cycle_detected: ${offending.join(", ")}`,
      );
    }

    const id_map: Record<string, string> = {};
    const ids: string[] = [];
    for (const t of input.tasks) {
      const prefix = t.prefix ?? "TECH";
      const seq = PREFIX_SEQ[prefix];
      if (!seq) throw new IaDbValidationError(`unknown prefix: ${prefix}`);
      const nextRes = await c.query<{ n: string }>(`SELECT nextval($1)::text AS n`, [seq]);
      const task_id = `${prefix}-${nextRes.rows[0]!.n}`;
      id_map[t.label.trim()] = task_id;
      ids.push(task_id);
    }
    for (const t of input.tasks) {
      const task_id = id_map[t.label.trim()]!;
      const status = t.status ?? "pending";
      await c.query(
        `INSERT INTO ia_tasks (task_id, prefix, slug, stage_id, title, status,
                               priority, type, notes, body)
           VALUES ($1, $2, $3, $4, $5, $6::task_status, $7, $8, $9, $10)`,
        [
          task_id,
          t.prefix ?? "TECH",
          slug,
          stage_id,
          t.title.trim(),
          status,
          t.priority ?? null,
          t.type ?? null,
          t.notes ?? null,
          t.body ?? "",
        ],
      );
    }
    for (const t of input.tasks) {
      const task_id = id_map[t.label.trim()]!;
      const deps = (t.depends_on_labels ?? []).map((s) => s.trim()).filter(Boolean);
      for (const depLabel of deps) {
        const depId = id_map[depLabel]!;
        await c.query(
          `INSERT INTO ia_task_deps (task_id, depends_on_id, kind)
             VALUES ($1, $2, 'depends_on')
           ON CONFLICT DO NOTHING`,
          [task_id, depId],
        );
      }
    }

    // Audit row in ia_master_plan_change_log so dedup is feasible on
    // re-call. UNIQUE(slug, stage_id, kind, commit_sha) — duplicate
    // inserts no-op via ON CONFLICT (already-handled-by-pre-check above).
    if (input.commit_sha) {
      await c.query(
        `INSERT INTO ia_master_plan_change_log
            (slug, stage_id, kind, commit_sha, body, actor)
          VALUES ($1, $2, 'stage-decompose-apply', $3, $4, $5)
          ON CONFLICT DO NOTHING`,
        [
          slug,
          stage_id,
          input.commit_sha,
          `Decomposed stage with ${ids.length} tasks: ${ids.join(", ")}`,
          "stage-decompose-apply",
        ],
      );
    }

    return {
      ok: true as const,
      ids,
      id_map,
      stage_id_resolved: stage_id,
    };
  });
}
