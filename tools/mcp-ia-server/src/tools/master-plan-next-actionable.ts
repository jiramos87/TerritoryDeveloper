/**
 * MCP tool: master_plan_next_actionable — walk `ia_stages.depends_on[]`
 * edges (TECH-3225) via Kahn's algorithm + emit pending stages whose deps
 * are all `done`.
 *
 * db-lifecycle-extensions Stage 2 / TECH-3228.
 *
 * Output: `[{stage_id, slug, depends_on_resolved: [{stage_id, slug, status}]}]`
 * — pending stages whose deps are ALL `done`. Ordered topologically by depth
 * (shallowest first, deepest unblocked next).
 *
 * Cross-slug walk: `depends_on[]` items are `slug/stage_id` strings; refs
 * may target other plans (sibling-orchestrator pattern in preamble).
 *
 * Cycle safety: BEFORE INSERT/UPDATE trigger from TECH-3225 prevents cycles
 * at write time; this tool assumes the graph is acyclic.
 *
 * NOTE: After editing this descriptor, restart Claude Code (or run
 * `tsx tools/mcp-ia-server/src/index.ts` script) to refresh the in-memory
 * schema cache (N4).
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import { getIaDatabasePool } from "../ia-db/pool.js";
import {
  getStageDependsOnGraph,
  type StageDepNode,
} from "../ia-db/queries.js";

// ---------------------------------------------------------------------------
// Input schema
// ---------------------------------------------------------------------------

const inputShape = {
  slug: z
    .string()
    .describe(
      "Master-plan slug. Required. Walks `ia_stages.depends_on[]` for this " +
        "slug + cross-slug dep refs to emit pending stages whose deps are all `done`.",
    ),
  session_id: z
    .string()
    .optional()
    .describe(
      "Optional caller session id (parallel-carcass §6.2). When provided, " +
        "section stages are filtered through 2-tier claim awareness — a " +
        "stage is emitted only if its section is unclaimed OR claimed by " +
        "this session AND the stage itself is unclaimed OR claimed by " +
        "this session.",
    ),
};

// ---------------------------------------------------------------------------
// Output shape
// ---------------------------------------------------------------------------

export interface ResolvedDep {
  slug: string;
  stage_id: string;
  status: "pending" | "in_progress" | "done" | "unknown";
}

export interface NextActionableEntry {
  slug: string;
  stage_id: string;
  depends_on_resolved: ResolvedDep[];
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function jsonResult(payload: unknown) {
  return {
    content: [
      {
        type: "text" as const,
        text: JSON.stringify(payload, null, 2),
      },
    ],
  };
}

interface StageStatusRow {
  slug: string;
  stage_id: string;
  status: "pending" | "in_progress" | "done";
}

/**
 * Resolve cross-slug dep refs against `ia_stages` for status. Returns map
 * keyed by `slug/stage_id`; missing rows omitted (caller emits 'unknown').
 */
async function resolveCrossSlugStatuses(
  refs: Iterable<string>,
): Promise<Map<string, "pending" | "in_progress" | "done">> {
  const refSet = new Set<string>();
  for (const r of refs) refSet.add(r);
  if (refSet.size === 0) return new Map();

  const pool = getIaDatabasePool();
  if (!pool) {
    throw {
      code: "db_unavailable",
      message: "ia_db pool not initialized",
    };
  }

  // Build (slug, stage_id) tuples for the WHERE-IN clause.
  const slugs: string[] = [];
  const stageIds: string[] = [];
  for (const ref of refSet) {
    const idx = ref.indexOf("/");
    if (idx < 0) continue;
    slugs.push(ref.substring(0, idx));
    stageIds.push(ref.substring(idx + 1));
  }
  if (slugs.length === 0) return new Map();

  const res = await pool.query<StageStatusRow>(
    `SELECT slug, stage_id, status
       FROM ia_stages
      WHERE (slug, stage_id) IN (
        SELECT * FROM unnest($1::text[], $2::text[])
      )`,
    [slugs, stageIds],
  );

  const out = new Map<string, "pending" | "in_progress" | "done">();
  for (const r of res.rows) {
    out.set(`${r.slug}/${r.stage_id}`, r.status);
  }
  return out;
}

/**
 * Kahn topo sort over the same-slug subgraph. Cross-slug deps are pinned
 * (treated as external; not added to in-degree calc) — only same-slug deps
 * gate ordering. Cycle safety enforced by TECH-3225 trigger at write time.
 */
function kahnTopoSort(nodes: StageDepNode[]): StageDepNode[] {
  const byKey = new Map<string, StageDepNode>();
  for (const n of nodes) byKey.set(`${n.slug}/${n.stage_id}`, n);

  const inDegree = new Map<string, number>();
  const reverseEdges = new Map<string, string[]>(); // dep -> [dependents]

  for (const n of nodes) {
    const key = `${n.slug}/${n.stage_id}`;
    inDegree.set(key, 0);
    reverseEdges.set(key, []);
  }

  for (const n of nodes) {
    const key = `${n.slug}/${n.stage_id}`;
    for (const dep of n.depends_on) {
      // Only count same-slug deps in topo ordering.
      if (!byKey.has(dep)) continue;
      inDegree.set(key, (inDegree.get(key) ?? 0) + 1);
      const list = reverseEdges.get(dep) ?? [];
      list.push(key);
      reverseEdges.set(dep, list);
    }
  }

  // Stable order: queue by stage_id ASC for determinism.
  const queue: string[] = [];
  for (const [k, d] of inDegree) {
    if (d === 0) queue.push(k);
  }
  queue.sort();

  const sorted: StageDepNode[] = [];
  while (queue.length > 0) {
    const k = queue.shift()!;
    const node = byKey.get(k);
    if (node) sorted.push(node);
    for (const child of reverseEdges.get(k) ?? []) {
      const newDeg = (inDegree.get(child) ?? 0) - 1;
      inDegree.set(child, newDeg);
      if (newDeg === 0) {
        queue.push(child);
        queue.sort();
      }
    }
  }

  return sorted;
}

/**
 * Pure core: given graph + cross-slug status map, emit pending stages whose
 * deps are ALL `done`.
 */
export function computeNextActionable(
  graph: StageDepNode[],
  crossSlugStatuses: Map<string, "pending" | "in_progress" | "done">,
): NextActionableEntry[] {
  const ownByKey = new Map<string, StageDepNode>();
  for (const n of graph) ownByKey.set(`${n.slug}/${n.stage_id}`, n);

  const sorted = kahnTopoSort(graph);
  const out: NextActionableEntry[] = [];

  for (const node of sorted) {
    if (node.status !== "pending") continue;

    const resolved: ResolvedDep[] = [];
    let allDone = true;
    for (const depRef of node.depends_on) {
      const idx = depRef.indexOf("/");
      const depSlug = idx < 0 ? depRef : depRef.substring(0, idx);
      const depStageId = idx < 0 ? "" : depRef.substring(idx + 1);

      // Same-slug → use ownByKey (canonical). Cross-slug → cross-status map.
      let status: ResolvedDep["status"];
      const own = ownByKey.get(depRef);
      if (own) {
        status = own.status;
      } else {
        status = crossSlugStatuses.get(depRef) ?? "unknown";
      }
      if (status !== "done") allDone = false;
      resolved.push({ slug: depSlug, stage_id: depStageId, status });
    }

    if (!allDone) continue;
    out.push({
      slug: node.slug,
      stage_id: node.stage_id,
      depends_on_resolved: resolved,
    });
  }

  return out;
}

// ---------------------------------------------------------------------------
// Registration
// ---------------------------------------------------------------------------

type NextActionableArgs = { slug?: string; session_id?: string };

interface StageCarcassRow {
  stage_id: string;
  carcass_role: string | null;
  section_id: string | null;
  status: "pending" | "in_progress" | "done";
}
interface ClaimSnapshot {
  section_session: Map<string, string>; // section_id → session_id (active)
  stage_session: Map<string, string>; // stage_id → session_id (active)
}

async function readCarcassMeta(slug: string): Promise<StageCarcassRow[]> {
  const pool = getIaDatabasePool();
  if (!pool) {
    throw { code: "db_unavailable", message: "ia_db pool not initialized" };
  }
  const res = await pool.query<StageCarcassRow>(
    `SELECT stage_id, carcass_role, section_id, status
       FROM ia_stages
      WHERE slug = $1`,
    [slug],
  );
  return res.rows;
}

async function readClaims(slug: string): Promise<ClaimSnapshot> {
  const pool = getIaDatabasePool();
  if (!pool) {
    throw { code: "db_unavailable", message: "ia_db pool not initialized" };
  }
  const sec = await pool.query<{ section_id: string; session_id: string }>(
    `SELECT section_id, session_id FROM ia_section_claims
      WHERE slug = $1 AND released_at IS NULL`,
    [slug],
  );
  const stg = await pool.query<{ stage_id: string; session_id: string }>(
    `SELECT stage_id, session_id FROM ia_stage_claims
      WHERE slug = $1 AND released_at IS NULL`,
    [slug],
  );
  return {
    section_session: new Map(sec.rows.map((r) => [r.section_id, r.session_id])),
    stage_session: new Map(stg.rows.map((r) => [r.stage_id, r.session_id])),
  };
}

/**
 * Apply parallel-carcass filters to candidate next-actionable entries:
 *  - carcass gate: when ≥1 carcass stage exists and not all are 'done',
 *    drop non-carcass entries.
 *  - 2-tier claim awareness: when a stage has section_id, emit only if
 *    (section unclaimed OR claimed by session_id) AND (stage unclaimed
 *    OR claimed by session_id).
 */
export function applyCarcassFilters(
  entries: NextActionableEntry[],
  meta: StageCarcassRow[],
  claims: ClaimSnapshot,
  session_id: string | undefined,
): NextActionableEntry[] {
  const byStage = new Map<string, StageCarcassRow>();
  for (const m of meta) byStage.set(m.stage_id, m);

  const carcass = meta.filter((m) => m.carcass_role === "carcass");
  const carcassExists = carcass.length > 0;
  const carcassDone =
    carcassExists && carcass.every((c) => c.status === "done");

  const out: NextActionableEntry[] = [];
  for (const e of entries) {
    const m = byStage.get(e.stage_id);
    if (carcassExists && !carcassDone) {
      if (!m || m.carcass_role !== "carcass") continue;
    }
    if (m && m.section_id) {
      const secOwner = claims.section_session.get(m.section_id);
      if (secOwner !== undefined && secOwner !== session_id) continue;
      const stgOwner = claims.stage_session.get(m.stage_id);
      if (stgOwner !== undefined && stgOwner !== session_id) continue;
    }
    out.push(e);
  }
  return out;
}

export function registerMasterPlanNextActionable(server: McpServer): void {
  server.registerTool(
    "master_plan_next_actionable",
    {
      description:
        "DB-backed: walk `ia_stages.depends_on[]` edges (TECH-3225) via " +
        "Kahn topological sort + emit pending stages whose deps are ALL `done`. " +
        "Replaces hand-written sibling-unblock chain in master-plan preamble. " +
        "Input: `{slug: string}` (required). " +
        "Output: `[{slug, stage_id, depends_on_resolved: [{slug, stage_id, status}]}]` " +
        "ordered topologically (shallowest first). " +
        "Cross-slug deps resolved against `ia_stages` for the referenced slug; " +
        "missing rows emit `status: 'unknown'`. " +
        "Errors: `slug_not_found` (no `ia_master_plans` row). " +
        "Parallel-carcass §6.2: when plan has carcass stages and any are " +
        "not 'done', non-carcass entries are filtered out. When `session_id` " +
        "is provided, section stages are 2-tier-claim filtered (section " +
        "+ stage). Legacy plans (NULL carcass_role + zero claim rows) " +
        "pass through unchanged. " +
        "Schema-cache restart required after adding this tool (N4): " +
        "restart Claude Code or run `tsx tools/mcp-ia-server/src/index.ts`.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("master_plan_next_actionable", async () => {
        const envelope = await wrapTool(
          async (
            input: NextActionableArgs | undefined,
          ): Promise<NextActionableEntry[]> => {
            const slug = (input?.slug ?? "").trim();
            if (!slug) {
              throw { code: "invalid_input", message: "slug is required." };
            }

            // Plan existence guard.
            const pool = getIaDatabasePool();
            if (!pool) {
              throw {
                code: "db_unavailable",
                message: "ia_db pool not initialized",
              };
            }
            const planRes = await pool.query<{ slug: string }>(
              `SELECT slug FROM ia_master_plans WHERE slug = $1`,
              [slug],
            );
            if (planRes.rows.length === 0) {
              throw {
                code: "slug_not_found",
                message: `Master plan '${slug}' not found in ia_master_plans.`,
              };
            }

            // Build same-slug graph + cross-slug status map.
            const graph = await getStageDependsOnGraph(slug);
            const crossSlugRefs: string[] = [];
            const ownKeys = new Set(
              graph.map((n) => `${n.slug}/${n.stage_id}`),
            );
            for (const n of graph) {
              for (const dep of n.depends_on) {
                if (!ownKeys.has(dep)) crossSlugRefs.push(dep);
              }
            }
            const crossSlugStatuses =
              await resolveCrossSlugStatuses(crossSlugRefs);

            const raw = computeNextActionable(graph, crossSlugStatuses);

            // Parallel-carcass filters (additive; legacy plans w/ NULL
            // carcass_role + zero claim rows pass through unchanged).
            const meta = await readCarcassMeta(slug);
            const claims = await readClaims(slug);
            return applyCarcassFilters(raw, meta, claims, input?.session_id);
          },
        )(args as NextActionableArgs | undefined);

        return jsonResult(envelope);
      }),
  );
}
