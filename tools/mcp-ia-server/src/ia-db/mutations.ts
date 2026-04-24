/**
 * DB write mutations for the IA tables (Step 4 of ia-dev-db-refactor).
 *
 * All mutations are transactional (BEGIN/COMMIT/ROLLBACK). Row-level locks
 * via `SELECT ... FOR UPDATE` are scoped inside each tx. Id reservation
 * uses per-prefix sequences (`tech_id_seq`, `feat_id_seq`, …) from
 * migration 0015 — `nextval` is already atomic, no advisory lock needed.
 *
 * History writes (`ia_task_spec_history`) are tool-side explicit (F5=a) —
 * no triggers. Journal payload (`ia_ship_stage_journal.payload`) is
 * validated at the tool boundary (payload_kind present, payload is an
 * object) but body shape is trust-but-document (pass-through jsonb).
 *
 * Rollback policy: any exception inside a tx → ROLLBACK + re-throw. No
 * partial commits.
 *
 * Source of truth for decisions: docs/ia-dev-db-refactor-implementation.md
 * §Step 4.
 */

import type pg from "pg";
import { getIaDatabasePool } from "./pool.js";
import {
  IaDbUnavailableError,
  queryTaskBody,
  queryTaskState,
  sliceSection,
  type TaskRowDB,
  type TaskStateDB,
} from "./queries.js";

// ---------------------------------------------------------------------------
// Shared helpers.
// ---------------------------------------------------------------------------

function poolOrThrow(): pg.Pool {
  const pool = getIaDatabasePool();
  if (!pool) throw new IaDbUnavailableError();
  return pool;
}

const PREFIX_SEQ: Record<string, string> = {
  TECH: "tech_id_seq",
  FEAT: "feat_id_seq",
  BUG: "bug_id_seq",
  ART: "art_id_seq",
  AUDIO: "audio_id_seq",
};

export class IaDbValidationError extends Error {
  code = "invalid_input";
  constructor(message: string) {
    super(message);
  }
}

async function withTx<T>(fn: (c: pg.PoolClient) => Promise<T>): Promise<T> {
  const pool = poolOrThrow();
  const client = await pool.connect();
  try {
    await client.query("BEGIN");
    const out = await fn(client);
    await client.query("COMMIT");
    return out;
  } catch (e) {
    await client.query("ROLLBACK").catch(() => {});
    throw e;
  } finally {
    client.release();
  }
}

// ---------------------------------------------------------------------------
// task_insert
// ---------------------------------------------------------------------------

export interface TaskInsertInput {
  prefix: "TECH" | "FEAT" | "BUG" | "ART" | "AUDIO";
  slug?: string | null;
  stage_id?: string | null;
  title: string;
  body?: string;
  type?: string | null;
  priority?: string | null;
  notes?: string | null;
  depends_on?: string[];
  related?: string[];
  status?: TaskRowDB["status"];
}

export interface TaskInsertResult {
  task_id: string;
  created_at: string;
  depends_on: string[];
  related: string[];
}

export async function mutateTaskInsert(
  input: TaskInsertInput,
): Promise<TaskInsertResult> {
  const prefix = input.prefix;
  const seq = PREFIX_SEQ[prefix];
  if (!seq) throw new IaDbValidationError(`unknown prefix: ${prefix}`);
  const title = (input.title ?? "").trim();
  if (!title) throw new IaDbValidationError("title is required");
  const status = input.status ?? "pending";
  const body = input.body ?? "";
  const depends_on = (input.depends_on ?? []).map((s) => s.trim()).filter(Boolean);
  const related = (input.related ?? []).map((s) => s.trim()).filter(Boolean);

  return withTx(async (c) => {
    const nextRes = await c.query<{ n: string }>(`SELECT nextval($1)::text AS n`, [seq]);
    const task_id = `${prefix}-${nextRes.rows[0]!.n}`;

    if (input.slug && input.stage_id) {
      const sr = await c.query(
        `SELECT 1 FROM ia_stages WHERE slug = $1 AND stage_id = $2`,
        [input.slug, input.stage_id],
      );
      if (sr.rowCount === 0) {
        throw new IaDbValidationError(
          `no stage '${input.slug}/${input.stage_id}' in ia_stages`,
        );
      }
    }

    const ins = await c.query<{ created_at: string }>(
      `INSERT INTO ia_tasks (task_id, prefix, slug, stage_id, title, status,
                             priority, type, notes, body)
         VALUES ($1, $2, $3, $4, $5, $6::task_status, $7, $8, $9, $10)
       RETURNING created_at`,
      [
        task_id,
        prefix,
        input.slug ?? null,
        input.stage_id ?? null,
        title,
        status,
        input.priority ?? null,
        input.type ?? null,
        input.notes ?? null,
        body,
      ],
    );

    const depTargets = [...new Set([...depends_on, ...related])];
    if (depTargets.length > 0) {
      const check = await c.query<{ task_id: string }>(
        `SELECT task_id FROM ia_tasks WHERE task_id = ANY($1::text[])`,
        [depTargets],
      );
      const found = new Set(check.rows.map((r) => r.task_id));
      const missing = depTargets.filter((id) => !found.has(id));
      if (missing.length > 0) {
        throw new IaDbValidationError(
          `unknown dep targets: ${missing.join(", ")}`,
        );
      }
    }

    for (const d of depends_on) {
      await c.query(
        `INSERT INTO ia_task_deps (task_id, depends_on_id, kind)
           VALUES ($1, $2, 'depends_on')
         ON CONFLICT DO NOTHING`,
        [task_id, d],
      );
    }
    for (const r of related) {
      await c.query(
        `INSERT INTO ia_task_deps (task_id, depends_on_id, kind)
           VALUES ($1, $2, 'related')
         ON CONFLICT DO NOTHING`,
        [task_id, r],
      );
    }

    return {
      task_id,
      created_at: ins.rows[0]!.created_at,
      depends_on,
      related,
    };
  });
}

// ---------------------------------------------------------------------------
// task_status_flip
// ---------------------------------------------------------------------------

const VALID_STATUSES: TaskRowDB["status"][] = [
  "pending",
  "implemented",
  "verified",
  "done",
  "archived",
];

export async function mutateTaskStatusFlip(
  task_id: string,
  new_status: TaskRowDB["status"],
): Promise<{ task_id: string; prev_status: TaskRowDB["status"]; new_status: TaskRowDB["status"]; updated_at: string }> {
  if (!VALID_STATUSES.includes(new_status)) {
    throw new IaDbValidationError(`invalid status: ${new_status}`);
  }
  return withTx(async (c) => {
    const cur = await c.query<{ status: TaskRowDB["status"] }>(
      `SELECT status FROM ia_tasks WHERE task_id = $1 FOR UPDATE`,
      [task_id],
    );
    if (cur.rowCount === 0) {
      throw new IaDbValidationError(`task not found: ${task_id}`);
    }
    const prev = cur.rows[0]!.status;

    const completedClause = new_status === "done" ? ", completed_at = now()" : "";
    const archivedClause = new_status === "archived" ? ", archived_at = now()" : "";

    const upd = await c.query<{ updated_at: string }>(
      `UPDATE ia_tasks
          SET status = $2::task_status,
              updated_at = now()
              ${completedClause}
              ${archivedClause}
        WHERE task_id = $1
       RETURNING updated_at`,
      [task_id, new_status],
    );

    return {
      task_id,
      prev_status: prev,
      new_status,
      updated_at: upd.rows[0]!.updated_at,
    };
  });
}

// ---------------------------------------------------------------------------
// task_spec_section_write
// ---------------------------------------------------------------------------

/**
 * Replace (or append) one section of a task body, snapshotting the old
 * body into `ia_task_spec_history`. `content` must begin with a heading
 * line matching the section level; caller responsible for shape.
 *
 * Behavior: if the section exists, the lines from its heading through the
 * line before the next same-or-shallower heading are replaced by `content`.
 * If the section is missing, `content` is appended at end of body with a
 * blank separator line.
 */
export async function mutateTaskSpecSectionWrite(
  task_id: string,
  section: string,
  content: string,
  meta: { actor?: string; git_sha?: string; change_reason?: string } = {},
): Promise<{ task_id: string; history_id: number; updated_at: string }> {
  return withTx(async (c) => {
    const cur = await c.query<{ body: string }>(
      `SELECT body FROM ia_tasks WHERE task_id = $1 FOR UPDATE`,
      [task_id],
    );
    if (cur.rowCount === 0) {
      throw new IaDbValidationError(`task not found: ${task_id}`);
    }
    const oldBody = cur.rows[0]!.body;

    const histRes = await c.query<{ id: string }>(
      `INSERT INTO ia_task_spec_history (task_id, body, actor, git_sha, change_reason)
         VALUES ($1, $2, $3, $4, $5)
       RETURNING id::text AS id`,
      [
        task_id,
        oldBody,
        meta.actor ?? null,
        meta.git_sha ?? null,
        meta.change_reason ?? null,
      ],
    );

    let newBody: string;
    const slice = sliceSection(oldBody, section);
    if (slice) {
      const lines = oldBody.split(/\r?\n/);
      const beforeIdx = findHeadingLine(lines, section);
      const endIdx = findSectionEnd(lines, beforeIdx, slice.level);
      const before = lines.slice(0, beforeIdx).join("\n");
      const after = lines.slice(endIdx).join("\n");
      newBody = [before, content, after].filter((s) => s.length > 0).join("\n");
    } else {
      newBody = oldBody.endsWith("\n")
        ? `${oldBody}\n${content}`
        : `${oldBody}\n\n${content}`;
    }

    const upd = await c.query<{ updated_at: string }>(
      `UPDATE ia_tasks
          SET body = $2, updated_at = now()
        WHERE task_id = $1
       RETURNING updated_at`,
      [task_id, newBody],
    );

    return {
      task_id,
      history_id: parseInt(histRes.rows[0]!.id, 10),
      updated_at: upd.rows[0]!.updated_at,
    };
  });
}

function findHeadingLine(lines: string[], section: string): number {
  const needle = section.trim().toLowerCase();
  for (let i = 0; i < lines.length; i++) {
    const m = lines[i]!.match(/^(#{1,6})\s+(.+?)\s*$/);
    if (m && m[2]!.trim().toLowerCase() === needle) return i;
  }
  return -1;
}

function findSectionEnd(lines: string[], start: number, level: number): number {
  for (let i = start + 1; i < lines.length; i++) {
    const m = lines[i]!.match(/^(#{1,6})\s+/);
    if (m && m[1]!.length <= level) return i;
  }
  return lines.length;
}

// ---------------------------------------------------------------------------
// task_commit_record
// ---------------------------------------------------------------------------

const COMMIT_KINDS = ["feat", "fix", "chore", "docs", "refactor", "test"] as const;

export async function mutateTaskCommitRecord(
  task_id: string,
  commit_sha: string,
  commit_kind: (typeof COMMIT_KINDS)[number],
  message?: string | null,
): Promise<{ id: number; recorded_at: string }> {
  if (!COMMIT_KINDS.includes(commit_kind)) {
    throw new IaDbValidationError(`invalid commit_kind: ${commit_kind}`);
  }
  const sha = (commit_sha ?? "").trim();
  if (!sha) throw new IaDbValidationError("commit_sha is required");

  return withTx(async (c) => {
    const tr = await c.query(
      `SELECT 1 FROM ia_tasks WHERE task_id = $1`,
      [task_id],
    );
    if (tr.rowCount === 0) {
      throw new IaDbValidationError(`task not found: ${task_id}`);
    }
    const res = await c.query<{ id: string; recorded_at: string }>(
      `INSERT INTO ia_task_commits (task_id, commit_sha, commit_kind, message)
         VALUES ($1, $2, $3, $4)
       ON CONFLICT (task_id, commit_sha) DO UPDATE
         SET commit_kind = EXCLUDED.commit_kind,
             message     = EXCLUDED.message
       RETURNING id::text AS id, recorded_at`,
      [task_id, sha, commit_kind, message ?? null],
    );
    return {
      id: parseInt(res.rows[0]!.id, 10),
      recorded_at: res.rows[0]!.recorded_at,
    };
  });
}

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
// stage_closeout_apply
// ---------------------------------------------------------------------------

export async function mutateStageCloseoutApply(
  slug: string,
  stage_id: string,
): Promise<{ slug: string; stage_id: string; archived_task_count: number; stage_status: "done" }> {
  return withTx(async (c) => {
    const sr = await c.query(
      `SELECT 1 FROM ia_stages WHERE slug = $1 AND stage_id = $2 FOR UPDATE`,
      [slug, stage_id],
    );
    if (sr.rowCount === 0) {
      throw new IaDbValidationError(`stage not found: ${slug}/${stage_id}`);
    }

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

    const upd = await c.query<{ task_id: string }>(
      `UPDATE ia_tasks
          SET status = 'archived'::task_status,
              archived_at = COALESCE(archived_at, now()),
              updated_at = now()
        WHERE slug = $1 AND stage_id = $2 AND status = 'done'
       RETURNING task_id`,
      [slug, stage_id],
    );

    await c.query(
      `UPDATE ia_stages
          SET status = 'done'::stage_status, updated_at = now()
        WHERE slug = $1 AND stage_id = $2`,
      [slug, stage_id],
    );

    return {
      slug,
      stage_id,
      archived_task_count: upd.rowCount ?? 0,
      stage_status: "done",
    };
  });
}

// ---------------------------------------------------------------------------
// journal_append
// ---------------------------------------------------------------------------

export interface JournalAppendInput {
  session_id: string;
  task_id?: string | null;
  slug?: string | null;
  stage_id?: string | null;
  phase: string;
  payload_kind: string;
  payload: Record<string, unknown>;
}

export async function mutateJournalAppend(
  input: JournalAppendInput,
): Promise<{ id: number; recorded_at: string }> {
  const session_id = (input.session_id ?? "").trim();
  if (!session_id) throw new IaDbValidationError("session_id is required");
  const phase = (input.phase ?? "").trim();
  if (!phase) throw new IaDbValidationError("phase is required");
  const payload_kind = (input.payload_kind ?? "").trim();
  if (!payload_kind) throw new IaDbValidationError("payload_kind is required");
  if (input.payload === null || typeof input.payload !== "object") {
    throw new IaDbValidationError("payload must be an object");
  }

  const pool = poolOrThrow();
  const res = await pool.query<{ id: string; recorded_at: string }>(
    `INSERT INTO ia_ship_stage_journal
       (session_id, task_id, slug, stage_id, phase, payload_kind, payload)
     VALUES ($1, $2, $3, $4, $5, $6, $7::jsonb)
     RETURNING id::text AS id, recorded_at`,
    [
      session_id,
      input.task_id ?? null,
      input.slug ?? null,
      input.stage_id ?? null,
      phase,
      payload_kind,
      JSON.stringify(input.payload),
    ],
  );
  return {
    id: parseInt(res.rows[0]!.id, 10),
    recorded_at: res.rows[0]!.recorded_at,
  };
}

// ---------------------------------------------------------------------------
// fix_plan_write / fix_plan_consume
// ---------------------------------------------------------------------------

export async function mutateFixPlanWrite(
  task_id: string,
  round: number,
  tuples: Array<Record<string, unknown>>,
): Promise<{ task_id: string; round: number; written: number }> {
  if (!Number.isInteger(round) || round < 0) {
    throw new IaDbValidationError("round must be a non-negative integer");
  }
  if (!Array.isArray(tuples) || tuples.length === 0) {
    throw new IaDbValidationError("tuples must be a non-empty array");
  }
  return withTx(async (c) => {
    const tr = await c.query(
      `SELECT 1 FROM ia_tasks WHERE task_id = $1`,
      [task_id],
    );
    if (tr.rowCount === 0) {
      throw new IaDbValidationError(`task not found: ${task_id}`);
    }

    await c.query(
      `DELETE FROM ia_fix_plan_tuples
        WHERE task_id = $1 AND round = $2 AND applied_at IS NULL`,
      [task_id, round],
    );

    for (let i = 0; i < tuples.length; i++) {
      await c.query(
        `INSERT INTO ia_fix_plan_tuples (task_id, round, tuple_index, tuple)
           VALUES ($1, $2, $3, $4::jsonb)`,
        [task_id, round, i, JSON.stringify(tuples[i])],
      );
    }
    return { task_id, round, written: tuples.length };
  });
}

export async function mutateFixPlanConsume(
  task_id: string,
  round: number,
): Promise<{ task_id: string; round: number; consumed: number }> {
  if (!Number.isInteger(round) || round < 0) {
    throw new IaDbValidationError("round must be a non-negative integer");
  }
  return withTx(async (c) => {
    const res = await c.query(
      `UPDATE ia_fix_plan_tuples
          SET applied_at = now()
        WHERE task_id = $1 AND round = $2 AND applied_at IS NULL`,
      [task_id, round],
    );
    return { task_id, round, consumed: res.rowCount ?? 0 };
  });
}

// Re-export read helper so tools can round-trip without two imports.
export { queryTaskBody, queryTaskState };
export type { TaskStateDB };
