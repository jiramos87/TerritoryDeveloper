/**
 * MCP tools (architecture coherence) — Stage 1.3 of architecture-coherence-system.
 *
 * Read-side surfaces over the four arch tables (migration 0034 + 0036):
 *   - arch_decision_get / arch_decision_list (TECH-2443)
 *   - arch_surface_resolve (TECH-2444)
 *   - arch_drift_scan (TECH-2445)
 *   - arch_changelog_since (TECH-2446)
 *
 * Source-of-truth split per DEC-A10: humans edit ia/specs/architecture/*.md;
 * this DB indexes relations and serves agent queries.
 *
 * Per DEC-A16 composite-PK reshape (migration 0036), `stage_arch_surfaces`
 * joins on `surface_slug` only — numeric FK lookups forbidden.
 *
 * Run handlers (`runArchDecisionGet`, etc.) are exported separately from
 * registration so unit tests can drive them with a stub `pg.Pool`.
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import type { Pool } from "pg";
import { getIaDatabasePool } from "../ia-db/pool.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool, dbUnconfiguredError } from "../envelope.js";
import { resolveRepoRoot } from "../config.js";
import { spawnSync } from "node:child_process";

// ---------------------------------------------------------------------------
// Shared helpers.
// ---------------------------------------------------------------------------

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

// ---------------------------------------------------------------------------
// arch_decision_get
// ---------------------------------------------------------------------------

const archDecisionGetInputSchema = z.object({
  slug: z
    .string()
    .min(1)
    .describe("Decision slug (e.g. `DEC-A12`). Case-sensitive."),
});

interface ArchDecisionRow {
  slug: string;
  title: string;
  status: string;
  rationale: string;
  alternatives: string | null;
  superseded_by: string | null;
  surface_slug: string | null;
  created_at: string;
}

export async function runArchDecisionGet(
  pool: Pool,
  input: { slug?: string },
): Promise<ArchDecisionRow> {
  const slug = (input.slug ?? "").trim();
  if (!slug) {
    throw { code: "invalid_input" as const, message: "slug is required." };
  }
  const sql = `
    SELECT d.slug, d.title, d.status, d.rationale, d.alternatives,
           d.superseded_by, s.slug AS surface_slug,
           d.created_at::text AS created_at
      FROM arch_decisions d
 LEFT JOIN arch_surfaces s ON d.surface_id = s.id
     WHERE d.slug = $1
     LIMIT 1
  `;
  const { rows } = await pool.query(sql, [slug]);
  if (rows.length === 0) {
    throw {
      code: "decision_not_found" as const,
      message: `No decision '${slug}' in arch_decisions.`,
      details: { slug },
    };
  }
  const r = rows[0];
  return {
    slug: r.slug,
    title: r.title,
    status: r.status,
    rationale: r.rationale,
    alternatives: r.alternatives,
    superseded_by: r.superseded_by,
    surface_slug: r.surface_slug,
    created_at: r.created_at,
  };
}

export function registerArchDecisionGet(server: McpServer): void {
  server.registerTool(
    "arch_decision_get",
    {
      description:
        "DB-backed: fetch one **arch_decisions** row by `slug` with surface_slug joined via LEFT JOIN on **arch_surfaces.id**. Returns `decision_not_found` when slug is absent. Stage 1.3 / TECH-2443.",
      inputSchema: archDecisionGetInputSchema.shape,
    },
    async (args) =>
      runWithToolTiming("arch_decision_get", async () => {
        const envelope = await wrapTool(async (input: z.infer<typeof archDecisionGetInputSchema>) => {
          const pool = getIaDatabasePool();
          if (!pool) throw dbUnconfiguredError();
          return await runArchDecisionGet(pool, input);
        })(archDecisionGetInputSchema.parse(args ?? {}));
        return jsonResult(envelope);
      }),
  );
}

// ---------------------------------------------------------------------------
// arch_decision_list
// ---------------------------------------------------------------------------

const archDecisionListInputSchema = z.object({
  status: z
    .enum(["active", "superseded"])
    .optional()
    .describe("Filter by `status`. Omit to list all."),
  surface_slug: z
    .string()
    .optional()
    .describe("Filter by joined `arch_surfaces.slug`. Omit to skip filter."),
});

export async function runArchDecisionList(
  pool: Pool,
  input: { status?: string; surface_slug?: string },
): Promise<{ decisions: ArchDecisionRow[] }> {
  const params: unknown[] = [];
  const where: string[] = [];
  if (input.status) {
    params.push(input.status);
    where.push(`d.status = $${params.length}`);
  }
  if (input.surface_slug) {
    params.push(input.surface_slug);
    where.push(`s.slug = $${params.length}`);
  }
  const whereSql = where.length ? `WHERE ${where.join(" AND ")}` : "";
  const sql = `
    SELECT d.slug, d.title, d.status, d.rationale, d.alternatives,
           d.superseded_by, s.slug AS surface_slug,
           d.created_at::text AS created_at
      FROM arch_decisions d
 LEFT JOIN arch_surfaces s ON d.surface_id = s.id
     ${whereSql}
  ORDER BY d.slug ASC
  `;
  const { rows } = await pool.query(sql, params);
  return {
    decisions: rows.map((r: Record<string, unknown>) => ({
      slug: r.slug as string,
      title: r.title as string,
      status: r.status as string,
      rationale: r.rationale as string,
      alternatives: (r.alternatives as string | null) ?? null,
      superseded_by: (r.superseded_by as string | null) ?? null,
      surface_slug: (r.surface_slug as string | null) ?? null,
      created_at: r.created_at as string,
    })),
  };
}

export function registerArchDecisionList(server: McpServer): void {
  server.registerTool(
    "arch_decision_list",
    {
      description:
        "DB-backed: list **arch_decisions** rows (slug asc) with optional filters: `status` ∈ {active, superseded} and joined `surface_slug`. Empty result returns `{decisions: []}`. Stage 1.3 / TECH-2443.",
      inputSchema: archDecisionListInputSchema.shape,
    },
    async (args) =>
      runWithToolTiming("arch_decision_list", async () => {
        const envelope = await wrapTool(async (input: z.infer<typeof archDecisionListInputSchema>) => {
          const pool = getIaDatabasePool();
          if (!pool) throw dbUnconfiguredError();
          return await runArchDecisionList(pool, input);
        })(archDecisionListInputSchema.parse(args ?? {}));
        return jsonResult(envelope);
      }),
  );
}

// ---------------------------------------------------------------------------
// arch_surface_resolve
// ---------------------------------------------------------------------------

const archSurfaceResolveInputSchema = z
  .object({
    slug: z
      .string()
      .optional()
      .describe("Master-plan slug for `stage_id` lookup (required when `stage_id` set)."),
    stage_id: z
      .string()
      .optional()
      .describe("Stage id (e.g. `1.3`). Mutually exclusive with `task_id`."),
    task_id: z
      .string()
      .optional()
      .describe("Task id (e.g. `TECH-2444`). Resolves to its owning stage."),
  })
  .superRefine((val, ctx) => {
    const hasStage = Boolean(val.stage_id);
    const hasTask = Boolean(val.task_id);
    if (hasStage && hasTask) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: "stage_id and task_id are mutually exclusive",
      });
    }
    if (!hasStage && !hasTask) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: "exactly one of stage_id or task_id is required",
      });
    }
  });

interface ArchSurfaceRow {
  slug: string;
  kind: string;
  spec_path: string;
  spec_section: string | null;
}

export async function runArchSurfaceResolve(
  pool: Pool,
  input: { slug?: string; stage_id?: string; task_id?: string },
): Promise<{ surfaces: ArchSurfaceRow[] }> {
  const hasStage = Boolean(input.stage_id);
  const hasTask = Boolean(input.task_id);
  if (hasStage && hasTask) {
    throw {
      code: "invalid_input" as const,
      message: "stage_id and task_id are mutually exclusive",
    };
  }
  if (!hasStage && !hasTask) {
    throw {
      code: "invalid_input" as const,
      message: "exactly one of stage_id or task_id is required",
    };
  }

  let planSlug: string | null = input.slug?.trim() || null;
  let stageId: string | null = input.stage_id?.trim() || null;

  if (hasTask) {
    const taskId = input.task_id!.trim().toUpperCase();
    const tq = await pool.query(
      `SELECT slug, stage_id FROM ia_tasks WHERE task_id = $1 LIMIT 1`,
      [taskId],
    );
    if (tq.rows.length === 0) {
      throw {
        code: "task_not_found" as const,
        message: `No task '${taskId}' in ia_tasks.`,
        details: { task_id: taskId },
      };
    }
    planSlug = tq.rows[0].slug;
    stageId = tq.rows[0].stage_id;
  }

  // For stage_id-only path without slug, scan all plans for that stage_id
  // (DEC-A12 stage-level link via composite PK; absent slug → fan out).
  const params: unknown[] = [stageId];
  let where = `sas.stage_id = $1`;
  if (planSlug) {
    params.push(planSlug);
    where += ` AND sas.slug = $${params.length}`;
  }
  const sql = `
    SELECT s.slug, s.kind, s.spec_path, s.spec_section
      FROM stage_arch_surfaces sas
      JOIN arch_surfaces s ON s.slug = sas.surface_slug
     WHERE ${where}
  ORDER BY s.slug ASC
  `;
  const { rows } = await pool.query(sql, params);
  return {
    surfaces: rows.map((r: Record<string, unknown>) => ({
      slug: r.slug as string,
      kind: r.kind as string,
      spec_path: r.spec_path as string,
      spec_section: (r.spec_section as string | null) ?? null,
    })),
  };
}

export function registerArchSurfaceResolve(server: McpServer): void {
  server.registerTool(
    "arch_surface_resolve",
    {
      description:
        "DB-backed: resolve linked `arch_surfaces` for a Stage or Task. XOR input: `stage_id` (with optional `slug` to disambiguate) OR `task_id`. Joins **stage_arch_surfaces.surface_slug** → **arch_surfaces.slug** (DEC-A16 composite-PK shape; numeric FK forbidden). Stage 1.3 / TECH-2444.",
      inputSchema: {
        slug: z.string().optional(),
        stage_id: z.string().optional(),
        task_id: z.string().optional(),
      },
    },
    async (args) =>
      runWithToolTiming("arch_surface_resolve", async () => {
        const envelope = await wrapTool(async (input: { slug?: string; stage_id?: string; task_id?: string }) => {
          // Run schema refinement explicitly so wrapTool surfaces invalid_input.
          const parsed = archSurfaceResolveInputSchema.safeParse(input ?? {});
          if (!parsed.success) {
            throw {
              code: "invalid_input" as const,
              message: parsed.error.issues.map((i) => i.message).join("; "),
            };
          }
          const pool = getIaDatabasePool();
          if (!pool) throw dbUnconfiguredError();
          return await runArchSurfaceResolve(pool, parsed.data);
        })(args as { slug?: string; stage_id?: string; task_id?: string });
        return jsonResult(envelope);
      }),
  );
}

// ---------------------------------------------------------------------------
// arch_drift_scan
// ---------------------------------------------------------------------------

const archDriftScanInputSchema = z.object({
  plan_id: z
    .string()
    .optional()
    .describe("Master-plan slug (e.g. `architecture-coherence-system`). Omit to scan every open plan."),
});

interface DriftSurface {
  slug: string;
  changelog_kind: string;
  ts: string;
}

interface AffectedStage {
  slug: string;
  stage_id: string;
  drifted_surfaces: DriftSurface[];
  suggested_questions: string[];
}

function shapeQuestion(stageId: string, drift: DriftSurface, decisionSlug: string | null): string {
  if (drift.changelog_kind === "decide") {
    const dec = decisionSlug ?? "DEC-A?";
    return `Stage ${stageId} touches ${drift.slug}; new decision ${dec} — re-plan?`;
  }
  if (drift.changelog_kind === "supersede") {
    return `Stage ${stageId} touches retired surface ${drift.slug} — pivot?`;
  }
  // edit
  return `Stage ${stageId} touches ${drift.slug}; schema changed — re-validate?`;
}

export async function runArchDriftScan(
  pool: Pool,
  input: { plan_id?: string },
): Promise<{ affected_stages: AffectedStage[] }> {
  const params: unknown[] = [];
  let planFilter = "";
  if (input.plan_id) {
    params.push(input.plan_id.trim());
    planFilter = `WHERE mp.slug = $${params.length}`;
  } else {
    // scan every open plan: status NOT IN ('done', 'archived')
    planFilter = `WHERE mp.status NOT IN ('done', 'archived')`;
  }

  // Collect (slug, stage_id, plan_created_at, last_pending_flip_ts) per stage.
  const stageSql = `
    SELECT s.slug, s.stage_id, mp.created_at AS plan_created_at,
           (
             SELECT MAX(cl.ts)
               FROM ia_master_plan_change_log cl
              WHERE cl.slug = s.slug
                AND cl.kind = 'stage_status_flip'
                AND cl.body LIKE '%' || s.stage_id || '%'
                AND cl.body LIKE '%pending%'
           ) AS last_pending_flip_ts
      FROM ia_stages s
      JOIN ia_master_plans mp ON mp.slug = s.slug
      ${planFilter}
  `;
  const { rows: stages } = await pool.query(stageSql, params);

  const affected: AffectedStage[] = [];

  for (const st of stages as Array<{
    slug: string;
    stage_id: string;
    plan_created_at: Date;
    last_pending_flip_ts: Date | null;
  }>) {
    const cutoff = st.last_pending_flip_ts ?? st.plan_created_at;
    const driftSql = `
      SELECT cl.surface_slug, cl.decision_slug, cl.kind, cl.created_at::text AS ts
        FROM stage_arch_surfaces sas
        JOIN arch_changelog cl ON cl.surface_slug = sas.surface_slug
       WHERE sas.slug = $1
         AND sas.stage_id = $2
         AND cl.created_at > $3
    ORDER BY cl.created_at ASC
    `;
    const { rows: driftRows } = await pool.query(driftSql, [st.slug, st.stage_id, cutoff]);
    if (driftRows.length === 0) continue;
    const drifted_surfaces: DriftSurface[] = [];
    const suggested_questions: string[] = [];
    for (const dr of driftRows as Array<{
      surface_slug: string;
      decision_slug: string | null;
      kind: string;
      ts: string;
    }>) {
      const drift: DriftSurface = {
        slug: dr.surface_slug,
        changelog_kind: dr.kind,
        ts: dr.ts,
      };
      drifted_surfaces.push(drift);
      suggested_questions.push(shapeQuestion(st.stage_id, drift, dr.decision_slug));
    }
    affected.push({
      slug: st.slug,
      stage_id: st.stage_id,
      drifted_surfaces,
      suggested_questions,
    });
  }

  return { affected_stages: affected };
}

export function registerArchDriftScan(server: McpServer): void {
  server.registerTool(
    "arch_drift_scan",
    {
      description:
        "DB-backed: scan plan(s) for arch drift. Compares each Stage's linked arch_surfaces against arch_changelog entries newer than the Stage's last `_pending_` flip ts (fallback: plan `created_at`). Returns `{affected_stages: [{slug, stage_id, drifted_surfaces, suggested_questions}]}` — Stages with zero drift are excluded. Stage 1.3 / TECH-2445.",
      inputSchema: archDriftScanInputSchema.shape,
    },
    async (args) =>
      runWithToolTiming("arch_drift_scan", async () => {
        const envelope = await wrapTool(async (input: z.infer<typeof archDriftScanInputSchema>) => {
          const pool = getIaDatabasePool();
          if (!pool) throw dbUnconfiguredError();
          return await runArchDriftScan(pool, input);
        })(archDriftScanInputSchema.parse(args ?? {}));
        return jsonResult(envelope);
      }),
  );
}

// ---------------------------------------------------------------------------
// arch_changelog_since
// ---------------------------------------------------------------------------

const archChangelogSinceInputSchema = z
  .object({
    since_ts: z
      .string()
      .optional()
      .describe("ISO-8601 timestamp (e.g. `2026-04-01T00:00:00Z`). Mutually exclusive with `since_commit`."),
    since_commit: z
      .string()
      .optional()
      .describe("Git commit sha (short or full). Resolved to ts via `git log -1 --format=%cI`."),
  })
  .superRefine((val, ctx) => {
    const hasTs = Boolean(val.since_ts);
    const hasCommit = Boolean(val.since_commit);
    if (hasTs && hasCommit) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: "since_ts and since_commit are mutually exclusive",
      });
    }
    if (!hasTs && !hasCommit) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: "exactly one of since_ts or since_commit is required",
      });
    }
  });

interface ChangelogEntry {
  id: number;
  kind: string;
  surface_slug: string | null;
  decision_slug: string | null;
  commit_sha: string | null;
  body: string | null;
  created_at: string;
}

/**
 * Resolve a git commit sha to its ISO-8601 committer timestamp via
 * `git log -1 --format=%cI <sha>` (synchronous subprocess).
 *
 * Exposed as a parameter on `runArchChangelogSince` so unit tests can pass
 * a stub resolver without spawning git.
 */
export function resolveCommitToTs(sha: string): string {
  const cwd = resolveRepoRoot();
  const out = spawnSync("git", ["log", "-1", "--format=%cI", sha], {
    cwd,
    encoding: "utf8",
    timeout: 5000,
  });
  if (out.status !== 0) {
    throw {
      code: "git_resolution_failed" as const,
      message: `git log failed for sha '${sha}': ${(out.stderr || "").trim()}`,
      details: { sha },
    };
  }
  const ts = (out.stdout || "").trim();
  if (!ts) {
    throw {
      code: "git_resolution_failed" as const,
      message: `git log returned empty ts for sha '${sha}'`,
      details: { sha },
    };
  }
  return ts;
}

export async function runArchChangelogSince(
  pool: Pool,
  input: { since_ts?: string; since_commit?: string },
  resolveSha: (sha: string) => string = resolveCommitToTs,
): Promise<{ entries: ChangelogEntry[] }> {
  const hasTs = Boolean(input.since_ts);
  const hasCommit = Boolean(input.since_commit);
  if (hasTs && hasCommit) {
    throw {
      code: "invalid_input" as const,
      message: "since_ts and since_commit are mutually exclusive",
    };
  }
  if (!hasTs && !hasCommit) {
    throw {
      code: "invalid_input" as const,
      message: "exactly one of since_ts or since_commit is required",
    };
  }

  const ts = hasTs ? input.since_ts!.trim() : resolveSha(input.since_commit!.trim());
  const sql = `
    SELECT id, kind, surface_slug, decision_slug, commit_sha, body,
           created_at::text AS created_at
      FROM arch_changelog
     WHERE created_at >= $1
  ORDER BY created_at ASC
  `;
  const { rows } = await pool.query(sql, [ts]);
  return {
    entries: rows.map((r: Record<string, unknown>) => ({
      id: Number(r.id),
      kind: r.kind as string,
      surface_slug: (r.surface_slug as string | null) ?? null,
      decision_slug: (r.decision_slug as string | null) ?? null,
      commit_sha: (r.commit_sha as string | null) ?? null,
      body: (r.body as string | null) ?? null,
      created_at: r.created_at as string,
    })),
  };
}

export function registerArchChangelogSince(server: McpServer): void {
  server.registerTool(
    "arch_changelog_since",
    {
      description:
        "DB-backed: list **arch_changelog** entries newer than a timestamp or commit-sha (XOR). `since_commit` resolves to ISO ts via `git log -1 --format=%cI`. Returns ordered (asc) entries. Stage 1.3 / TECH-2446.",
      inputSchema: {
        since_ts: z.string().optional(),
        since_commit: z.string().optional(),
      },
    },
    async (args) =>
      runWithToolTiming("arch_changelog_since", async () => {
        const envelope = await wrapTool(async (input: { since_ts?: string; since_commit?: string }) => {
          const parsed = archChangelogSinceInputSchema.safeParse(input ?? {});
          if (!parsed.success) {
            throw {
              code: "invalid_input" as const,
              message: parsed.error.issues.map((i) => i.message).join("; "),
            };
          }
          const pool = getIaDatabasePool();
          if (!pool) throw dbUnconfiguredError();
          return await runArchChangelogSince(pool, parsed.data);
        })(args as { since_ts?: string; since_commit?: string });
        return jsonResult(envelope);
      }),
  );
}

// ---------------------------------------------------------------------------
// Aggregate registration helper.
// ---------------------------------------------------------------------------

/**
 * Register all five Stage 1.3 architecture-coherence MCP tools on the given
 * server: arch_decision_get, arch_decision_list, arch_surface_resolve,
 * arch_drift_scan, arch_changelog_since.
 */
export function registerArchTools(server: McpServer): void {
  registerArchDecisionGet(server);
  registerArchDecisionList(server);
  registerArchSurfaceResolve(server);
  registerArchDriftScan(server);
  registerArchChangelogSince(server);
}
