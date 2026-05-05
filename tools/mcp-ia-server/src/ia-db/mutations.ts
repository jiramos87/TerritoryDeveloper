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
  canonHeadingText,
  IaDbUnavailableError,
  queryTaskBody,
  queryTaskState,
  sliceSection,
  type TaskRowDB,
  type TaskStateDB,
} from "./queries.js";
import { tarjanScc } from "./tarjan-scc.js";

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
  /**
   * Verbatim BACKLOG.md row block (checklist line + sub-bullets) for the
   * issue. Persisted to `ia_tasks.raw_markdown` (migration 0017) so the
   * Step 5 generator can emit byte-identical BACKLOG.md output without
   * reconstructing prose from structured fields. Pass omit/null to leave
   * the column untouched (generator falls back to structured fields).
   */
  raw_markdown?: string | null;
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
    // Idempotency guard — when (slug, stage_id, title) triple already exists,
    // return the existing row instead of advancing the sequence. Matches the
    // contract documented in `ia/skills/stage-file/SKILL.md` Phase 5.A.2 so
    // recipe re-runs on an already-filed Stage skip work cleanly.
    if (input.slug && input.stage_id) {
      const dup = await c.query<{
        task_id: string;
        created_at: string;
      }>(
        `SELECT task_id, created_at::text AS created_at
           FROM ia_tasks
          WHERE slug = $1 AND stage_id = $2 AND title = $3
          LIMIT 1`,
        [input.slug, input.stage_id, title],
      );
      if (dup.rowCount && dup.rowCount > 0) {
        const existing = dup.rows[0]!;
        const depRows = await c.query<{ depends_on_id: string; kind: string }>(
          `SELECT depends_on_id, kind FROM ia_task_deps WHERE task_id = $1`,
          [existing.task_id],
        );
        return {
          task_id: existing.task_id,
          created_at: existing.created_at,
          depends_on: depRows.rows.filter((r) => r.kind === "depends_on").map((r) => r.depends_on_id),
          related: depRows.rows.filter((r) => r.kind === "related").map((r) => r.depends_on_id),
        };
      }
    }

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
                             priority, type, notes, body, raw_markdown)
         VALUES ($1, $2, $3, $4, $5, $6::task_status, $7, $8, $9, $10, $11)
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
        input.raw_markdown ?? null,
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
// task_dep_register — TECH-2976 (db-lifecycle-extensions Stage 1)
// ---------------------------------------------------------------------------

export interface TaskDepRegisterResult {
  ok: true;
  task_id: string;
  edges_added: number;
}

export interface TaskDepRegisterCycleResult {
  ok: false;
  error: { code: "cycle_detected"; scc_members: string[] };
}

/**
 * Atomically insert `(task_id, dep_id, 'depends_on')` rows into `ia_task_deps`
 * with Tarjan SCC cycle detection inside the same PG transaction.
 *
 * Txn shape: BEGIN → existence checks → SELECT existing edges FOR UPDATE →
 * INSERT new edges (ON CONFLICT DO NOTHING) → load full edge graph → run
 * Tarjan over union → if any multi-node SCC OR self-loop ⇒ throw → tx
 * rollback discards the writes; else COMMIT. Cycle detection raises a
 * sentinel error caught at the boundary and reshaped into the structured
 * `{ok:false, error:{code, scc_members}}` non-throw response per §Plan
 * Digest §Pending Decisions (cycle error response shape).
 */
class CycleDetectedError extends Error {
  scc_members: string[];
  constructor(members: string[]) {
    super(`cycle_detected: ${members.join(", ")}`);
    this.scc_members = members;
  }
}

export async function mutateTaskDepRegister(
  task_id: string,
  depends_on: string[],
): Promise<TaskDepRegisterResult | TaskDepRegisterCycleResult> {
  const id = (task_id ?? "").trim();
  if (!id) throw new IaDbValidationError("task_id is required");
  const targets = Array.from(
    new Set(depends_on.map((s) => (s ?? "").trim()).filter((s) => s.length)),
  );

  // Self-reference short-circuit (avoids Tarjan run on trivial cycle).
  if (targets.includes(id)) {
    return { ok: false, error: { code: "cycle_detected", scc_members: [id] } };
  }

  try {
    return await withTx(async (c): Promise<TaskDepRegisterResult> => {
      // Existence guards.
      const checkIds = [id, ...targets];
      const ex = await c.query<{ task_id: string }>(
        `SELECT task_id FROM ia_tasks WHERE task_id = ANY($1::text[]) FOR UPDATE`,
        [checkIds],
      );
      const found = new Set(ex.rows.map((r) => r.task_id));
      if (!found.has(id)) {
        throw new IaDbValidationError(`task not found: ${id}`);
      }
      const missing = targets.filter((t) => !found.has(t));
      if (missing.length > 0) {
        throw new IaDbValidationError(
          `unknown dep targets: ${missing.join(", ")}`,
        );
      }

      // Insert new edges (idempotent re-register no-op).
      let edges_added = 0;
      for (const dep of targets) {
        const ins = await c.query(
          `INSERT INTO ia_task_deps (task_id, depends_on_id, kind)
             VALUES ($1, $2, 'depends_on')
           ON CONFLICT DO NOTHING`,
          [id, dep],
        );
        edges_added += ins.rowCount ?? 0;
      }

      // Load full edge graph (depends_on edges only).
      const all = await c.query<{ task_id: string; depends_on_id: string }>(
        `SELECT task_id, depends_on_id
           FROM ia_task_deps
          WHERE kind = 'depends_on'`,
      );
      const adj = new Map<string, string[]>();
      const radj = new Map<string, string[]>();
      const nodeSet = new Set<string>();
      for (const row of all.rows) {
        nodeSet.add(row.task_id);
        nodeSet.add(row.depends_on_id);
        const fwd = adj.get(row.task_id) ?? [];
        fwd.push(row.depends_on_id);
        adj.set(row.task_id, fwd);
        const back = radj.get(row.depends_on_id) ?? [];
        back.push(row.task_id);
        radj.set(row.depends_on_id, back);
      }

      // Scope Tarjan to reachability closure of new edge endpoints
      // (forward + backward BFS from {id} ∪ targets). Pre-existing cycles
      // disjoint from new edges are not flagged — only newly-introduced
      // cycles trip rejection. §Implementer Latitude: spec §Acceptance
      // says "no new cycles introduced"; full-graph Tarjan would re-flag
      // every prior write on contaminated DBs. Closure approach honours
      // intent without falsely rejecting clean inserts.
      const seeds = new Set<string>([id, ...targets]);
      const closure = new Set<string>();
      const queue: string[] = [];
      for (const s of seeds) {
        if (nodeSet.has(s) && !closure.has(s)) {
          closure.add(s);
          queue.push(s);
        }
      }
      while (queue.length > 0) {
        const u = queue.shift()!;
        for (const v of adj.get(u) ?? []) {
          if (!closure.has(v)) {
            closure.add(v);
            queue.push(v);
          }
        }
        for (const v of radj.get(u) ?? []) {
          if (!closure.has(v)) {
            closure.add(v);
            queue.push(v);
          }
        }
      }

      // Sub-adjacency restricted to closure nodes.
      const subAdj = new Map<string, string[]>();
      for (const u of closure) {
        const outs = (adj.get(u) ?? []).filter((v) => closure.has(v));
        subAdj.set(u, outs);
      }
      const result = tarjanScc(Array.from(closure), subAdj);
      if (result.multiNodeSccs.length > 0 || result.selfLoops.length > 0) {
        const offending =
          result.multiNodeSccs.length > 0
            ? result.multiNodeSccs[0]!
            : [result.selfLoops[0]!];
        throw new CycleDetectedError(offending);
      }

      return { ok: true, task_id: id, edges_added };
    });
  } catch (e) {
    if (e instanceof CycleDetectedError) {
      return {
        ok: false,
        error: { code: "cycle_detected", scc_members: e.scc_members },
      };
    }
    throw e;
  }
}

// ---------------------------------------------------------------------------
// task_raw_markdown_write — TECH-2973 (db-lifecycle-extensions Stage 1)
// ---------------------------------------------------------------------------

export interface TaskRawMarkdownWriteResult {
  task_id: string;
  bytes_written: number;
  updated_at: string;
}

/**
 * Persist verbatim BACKLOG.md row block into `ia_tasks.raw_markdown`.
 *
 * Removes the Pass-A-null + Pass-B-backfill workaround in `/stage-file`:
 * callers can now author the row text directly once `task_insert` returns
 * the reserved `ISSUE_ID`, without falling back to direct SQL UPDATE.
 *
 * Single-row UPDATE, no surrounding txn — idempotent write, no cascading
 * state. Caller may wrap in an outer txn if it needs atomic compound
 * semantics with other writes.
 *
 * Empty string is accepted; `null` is reserved for "never written" sentinel
 * so this entrypoint normalises explicit empty input to "" rather than
 * NULL.
 */
export async function mutateTaskRawMarkdownWrite(
  task_id: string,
  body: string,
): Promise<TaskRawMarkdownWriteResult> {
  const cleanId = (task_id ?? "").trim().toUpperCase();
  if (!cleanId) {
    throw new IaDbValidationError("task_id is required");
  }
  if (typeof body !== "string") {
    throw new IaDbValidationError("body must be a string");
  }
  const pool = poolOrThrow();
  const res = await pool.query<{ updated_at: string }>(
    `UPDATE ia_tasks
        SET raw_markdown = $2, updated_at = now()
      WHERE task_id = $1
     RETURNING updated_at`,
    [cleanId, body],
  );
  if (res.rowCount === 0) {
    throw new IaDbValidationError(`task not found: ${cleanId}`);
  }
  return {
    task_id: cleanId,
    bytes_written: Buffer.byteLength(body, "utf8"),
    updated_at: res.rows[0]!.updated_at,
  };
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
// task_status_flip_batch (ship-protocol Stage 3 / TECH-12642)
// ---------------------------------------------------------------------------

export interface TaskStatusFlipBatchInput {
  slug: string;
  stage_id: string;
  new_status: TaskRowDB["status"];
  task_ids?: string[];
}

export interface TaskStatusFlipBatchResult {
  flipped: Array<{
    task_id: string;
    prev_status: TaskRowDB["status"];
    new_status: TaskRowDB["status"];
  }>;
  skipped: Array<{
    task_id: string;
    reason: "not_found" | "already_target" | "not_in_stage";
  }>;
}

/**
 * Single-tx batch flip of N tasks belonging to one (slug, stage_id) Stage.
 * Used by ship-cycle (Sonnet stage-atomic batch) to flip all stage tasks
 * `pending → implemented` (or `implemented → done`) in one DB roundtrip.
 *
 * Behavior:
 *   - When `task_ids` omitted → operate on ALL non-terminal tasks of stage.
 *   - When `task_ids` provided → filter to that subset; ids not in stage land
 *     in `skipped[].reason='not_in_stage'`; missing ids → `not_found`.
 *   - Already-target ids → `skipped[].reason='already_target'`.
 *   - Single Postgres tx wraps SELECT FOR UPDATE + UPDATE (row-level locks).
 *   - `completed_at` / `archived_at` stamps applied uniformly per row.
 */
export async function mutateTaskStatusFlipBatch(
  input: TaskStatusFlipBatchInput,
): Promise<TaskStatusFlipBatchResult> {
  if (!VALID_STATUSES.includes(input.new_status)) {
    throw new IaDbValidationError(`invalid status: ${input.new_status}`);
  }
  const slug = input.slug.trim();
  const stage_id = input.stage_id.trim();
  if (!slug || !stage_id) {
    throw new IaDbValidationError("slug and stage_id are required");
  }
  const requestedIds = (input.task_ids ?? []).map((s) =>
    s.trim().toUpperCase(),
  );

  return withTx(async (c) => {
    const stageRows = await c.query<{ task_id: string; status: string }>(
      `SELECT task_id, status::text AS status
         FROM ia_tasks
        WHERE slug = $1 AND stage_id = $2
        FOR UPDATE`,
      [slug, stage_id],
    );
    const stageMap = new Map<string, string>();
    for (const r of stageRows.rows) {
      stageMap.set(r.task_id, r.status);
    }

    const flipped: TaskStatusFlipBatchResult["flipped"] = [];
    const skipped: TaskStatusFlipBatchResult["skipped"] = [];

    let workingIds: string[];
    if (requestedIds.length > 0) {
      workingIds = [];
      for (const id of requestedIds) {
        if (!stageMap.has(id)) {
          // Distinguish never-existing vs in-other-stage by probing whole table.
          const probe = await c.query<{ task_id: string }>(
            `SELECT task_id FROM ia_tasks WHERE task_id = $1`,
            [id],
          );
          skipped.push({
            task_id: id,
            reason: probe.rowCount === 0 ? "not_found" : "not_in_stage",
          });
          continue;
        }
        workingIds.push(id);
      }
    } else {
      // No filter → all non-terminal stage tasks.
      workingIds = [];
      for (const [id, status] of stageMap.entries()) {
        if (status !== "done" && status !== "archived") {
          workingIds.push(id);
        }
      }
    }

    for (const id of workingIds) {
      const prev = stageMap.get(id) as TaskRowDB["status"];
      if (prev === input.new_status) {
        skipped.push({ task_id: id, reason: "already_target" });
        continue;
      }
      const completedClause =
        input.new_status === "done" ? ", completed_at = now()" : "";
      const archivedClause =
        input.new_status === "archived" ? ", archived_at = now()" : "";
      await c.query(
        `UPDATE ia_tasks
            SET status = $2::task_status,
                updated_at = now()
                ${completedClause}
                ${archivedClause}
          WHERE task_id = $1`,
        [id, input.new_status],
      );
      flipped.push({
        task_id: id,
        prev_status: prev,
        new_status: input.new_status,
      });
    }

    return { flipped, skipped };
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
 *
 * Defensive heading normalization: the `section` parameter is treated as the
 * canonical form of the section name. The first heading line of `content` is
 * normalized to match: when caller passes `§Plan Digest` but content opens
 * with bare `## Plan Digest`, the heading is rewritten to `## §Plan Digest`
 * (and vice versa). Prevents Opus authoring drift where the literal § marker
 * is dropped during composition. Lookup of an existing section is also
 * §-tolerant via `canonHeadingText` in `sliceSection` / `findHeadingLine`.
 */
export async function mutateTaskSpecSectionWrite(
  task_id: string,
  section: string,
  content: string,
  meta: { actor?: string; git_sha?: string; change_reason?: string } = {},
): Promise<{
  task_id: string;
  history_id: number;
  updated_at: string;
  heading_normalized: boolean;
}> {
  const { content: normalizedContent, normalized: heading_normalized } =
    normalizeContentHeading(section, content);
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
      newBody = [before, normalizedContent, after]
        .filter((s) => s.length > 0)
        .join("\n");
    } else {
      newBody = oldBody.endsWith("\n")
        ? `${oldBody}\n${normalizedContent}`
        : `${oldBody}\n\n${normalizedContent}`;
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
      heading_normalized,
    };
  });
}

function findHeadingLine(lines: string[], section: string): number {
  const needle = canonHeadingText(section);
  for (let i = 0; i < lines.length; i++) {
    const m = lines[i]!.match(/^(#{1,6})\s+(.+?)\s*$/);
    if (m && canonHeadingText(m[2]!) === needle) return i;
  }
  return -1;
}

/**
 * Align the first heading line of `content` with the canonical `section`
 * name. Asymmetric rule (section arg is authoritative for §, never
 * destructively):
 *
 *   - section has `§` + content heading lacks `§` (canon-match holds)
 *     → prepend `§` to the heading text. `normalized = true`.
 *   - section lacks `§` + content heading has `§`              → leave alone
 *     (content's § is preserved; we never force-strip a marker the author
 *     deliberately wrote). `normalized = false`.
 *   - both match exactly OR no canon match OR no leading heading
 *     → leave alone. `normalized = false`.
 *
 * The boolean surfaces in the tool result so callers (stage-authoring,
 * spec-implementer) can detect drift in their hand-off counters.
 */
function normalizeContentHeading(
  section: string,
  content: string,
): { content: string; normalized: boolean } {
  const lines = content.split(/\r?\n/);
  let firstHeadingIdx = -1;
  for (let i = 0; i < lines.length; i++) {
    if (lines[i]!.trim() === "") continue;
    if (/^(#{1,6})\s+(.+?)\s*$/.test(lines[i]!)) {
      firstHeadingIdx = i;
    }
    break;
  }
  if (firstHeadingIdx < 0) return { content, normalized: false };

  const m = lines[firstHeadingIdx]!.match(/^(#{1,6})\s+(.+?)\s*$/);
  if (!m) return { content, normalized: false };

  const headingText = m[2]!.trim();
  const sectionText = section.trim();
  if (headingText === sectionText) return { content, normalized: false };

  if (canonHeadingText(headingText) !== canonHeadingText(sectionText)) {
    return { content, normalized: false };
  }

  const sectionHasMarker = sectionText.startsWith("\u00a7");
  const headingHasMarker = headingText.startsWith("\u00a7");
  if (!(sectionHasMarker && !headingHasMarker)) {
    return { content, normalized: false };
  }

  const newLine = `${m[1]} ${sectionText}`;
  const newLines = [...lines];
  newLines[firstHeadingIdx] = newLine;
  return { content: newLines.join("\n"), normalized: true };
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
// master_plan_insert / stage_insert / stage_update
// (Wave 1.5 backfill — author master plans + stages DB-side post-refactor.)
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
// task_batch_insert — db-lifecycle-extensions Stage 3 / TECH-3404
// ---------------------------------------------------------------------------

export interface TaskBatchInsertItem {
  label: string;
  prefix?: "TECH" | "FEAT" | "BUG" | "ART" | "AUDIO";
  title: string;
  body?: string;
  type?: string | null;
  priority?: string | null;
  notes?: string | null;
  depends_on_labels?: string[];
  status?: TaskRowDB["status"];
}

export interface TaskBatchInsertInput {
  slug: string;
  stage_id: string;
  tasks: TaskBatchInsertItem[];
}

export type TaskBatchInsertResult =
  | {
      ok: true;
      ids: string[];
      id_map: Record<string, string>;
    }
  | {
      ok: false;
      error:
        | "label_collision"
        | "unknown_label"
        | "cycle_detected"
        | "label_missing"
        | "title_missing";
      offending_labels?: string[];
      missing?: string[];
      scc_members?: string[];
      message?: string;
    };

/**
 * Atomically insert N tasks under one stage with intra-batch label-based
 * dep resolve in a single PG transaction. Pre-flight collision check
 * BEFORE any DB write. Rollback on any failure leaves zero residual
 * `ia_tasks` rows.
 *
 * Cycle detection uses Tarjan SCC over the intra-batch label graph
 * (resolved post-id-reservation). Pre-existing cycles in the DB graph are
 * not flagged here — only newly-introduced cycles.
 */
export async function mutateTaskBatchInsert(
  input: TaskBatchInsertInput,
): Promise<TaskBatchInsertResult> {
  const slug = (input.slug ?? "").trim();
  const stage_id = (input.stage_id ?? "").trim();
  if (!slug) throw new IaDbValidationError("slug is required");
  if (!stage_id) throw new IaDbValidationError("stage_id is required");
  if (!Array.isArray(input.tasks) || input.tasks.length === 0) {
    throw new IaDbValidationError("tasks array is required (non-empty)");
  }

  // Pre-flight: label collision + missing label + missing title.
  const labelSeen = new Map<string, number>();
  const collisions: string[] = [];
  for (const t of input.tasks) {
    const lbl = (t.label ?? "").trim();
    if (!lbl) {
      return {
        ok: false,
        error: "label_missing",
        message: "every task item requires a non-empty `label`",
      };
    }
    const title = (t.title ?? "").trim();
    if (!title) {
      return {
        ok: false,
        error: "title_missing",
        message: `task ${lbl}: title is required`,
      };
    }
    const prev = labelSeen.get(lbl) ?? 0;
    labelSeen.set(lbl, prev + 1);
    if (prev === 1) collisions.push(lbl);
  }
  if (collisions.length > 0) {
    return {
      ok: false,
      error: "label_collision",
      offending_labels: collisions,
    };
  }

  // Intra-batch label set for dep resolve.
  const labels = new Set(input.tasks.map((t) => t.label.trim()));
  // Pre-flight: unknown label refs in depends_on_labels.
  const missingLabels = new Set<string>();
  for (const t of input.tasks) {
    const deps = (t.depends_on_labels ?? []).map((s) => s.trim()).filter(Boolean);
    for (const d of deps) {
      if (!labels.has(d)) missingLabels.add(d);
    }
  }
  if (missingLabels.size > 0) {
    return {
      ok: false,
      error: "unknown_label",
      missing: Array.from(missingLabels),
    };
  }

  // Cycle detection on intra-batch label graph (pre-id-reservation).
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
    return {
      ok: false,
      error: "cycle_detected",
      scc_members: offending,
    };
  }

  return withTx(async (c) => {
    // Stage existence guard.
    const sg = await c.query(
      `SELECT 1 FROM ia_stages WHERE slug = $1 AND stage_id = $2`,
      [slug, stage_id],
    );
    if (sg.rowCount === 0) {
      throw new IaDbValidationError(
        `no stage '${slug}/${stage_id}' in ia_stages`,
      );
    }

    // Reserve ids per task (per-prefix DB sequence; Invariant 13 honored —
    // reserve-id.sh shell wrapper retired in favor of the DB-MCP successor
    // path established by Step 4 of ia-dev-db-refactor).
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

    // Insert task rows.
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

    // Resolve label-based deps to ids and register edges.
    for (const t of input.tasks) {
      const task_id = id_map[t.label.trim()]!;
      const deps = (t.depends_on_labels ?? []).map((s) => s.trim()).filter(Boolean);
      for (const depLabel of deps) {
        const depId = id_map[depLabel];
        if (!depId) {
          throw new IaDbValidationError(
            `internal: unresolved label ${depLabel} (post-flight)`,
          );
        }
        await c.query(
          `INSERT INTO ia_task_deps (task_id, depends_on_id, kind)
             VALUES ($1, $2, 'depends_on')
           ON CONFLICT DO NOTHING`,
          [task_id, depId],
        );
      }
    }

    return { ok: true, ids, id_map };
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

// Re-export read helper so tools can round-trip without two imports.
export { queryTaskBody, queryTaskState };
export type { TaskStateDB };
