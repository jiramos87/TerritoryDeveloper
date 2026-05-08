/**
 * Task-domain mutations: insert, dep-register, raw-markdown-write,
 * status-flip, status-flip-batch, spec-section-write, commit-record,
 * batch-insert.
 */

import {
  canonHeadingText,
  sliceSection,
  type TaskRowDB,
} from "../queries.js";
import { tarjanScc } from "../tarjan-scc.js";
import {
  enqueueCacheBust,
  IaDbValidationError,
  poolOrThrow,
  PREFIX_SEQ,
  withTx,
} from "./shared.js";
import { getIaDatabasePool } from "../pool.js";

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

    const result = {
      task_id,
      prev_status: prev,
      new_status,
      updated_at: upd.rows[0]!.updated_at,
    };

    // Refresh ia_stage_facet_view (TECH-15907) after status flip.
    // REFRESH CONCURRENTLY cannot run inside a transaction — fire best-effort
    // outside the tx via a separate pool query. Ignore failures (MV optional).
    setImmediate(() => {
      const pool = getIaDatabasePool();
      if (pool) {
        pool
          .query("REFRESH MATERIALIZED VIEW CONCURRENTLY ia_stage_facet_view")
          .catch(() => {/* best-effort — view may not exist yet during migrations */});
      }
    });

    // Cache-bust: task status change invalidates any cached db_read_batch results
    // that may have included task state rows (TECH-18106).
    enqueueCacheBust("db_read_batch", "db_read_batch:%");

    return result;
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

    // Cache-bust post-commit: batch flip invalidates task state in db_read_batch cache (TECH-18106).
    if (flipped.length > 0) {
      enqueueCacheBust("db_read_batch", "db_read_batch:%");
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
