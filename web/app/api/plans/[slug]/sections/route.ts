/**
 * BFF GET /api/plans/[slug]/sections — read-only section-cluster view.
 *
 * Wraps `getPlanSectionsBundle()` (server-side direct pg) — same shape as
 * MCP `master_plan_sections({slug})` plus envelope fields the UI needs:
 *   - `claim_heartbeat_timeout_minutes` (from `carcass_config`)
 *   - `carcass_done` (true iff every carcass-role stage is `done`)
 *
 * Stage 2.2 / TECH-5243 of `parallel-carcass-rollout`.
 */

import { NextResponse, type NextRequest } from "next/server";
import { getPlanSectionsBundle } from "@/lib/ia/sections-data";
import {
  iaJsonError,
  isDbConfigError,
  postgresErrorResponse,
} from "@/lib/ia/api-errors";

export const dynamic = "force-dynamic";

type Ctx = { params: Promise<{ slug: string }> };

export async function GET(_req: NextRequest, ctx: Ctx) {
  const { slug } = await ctx.params;
  if (!slug) return iaJsonError(400, "bad_request", "Missing slug");
  try {
    const bundle = await getPlanSectionsBundle(slug);
    if (!bundle) return iaJsonError(404, "not_found", "Master plan not found");
    return NextResponse.json(bundle, { status: 200 });
  } catch (e) {
    if (isDbConfigError(e))
      return iaJsonError(500, "internal", "Database not configured");
    return postgresErrorResponse(e, "Get plan sections");
  }
}
