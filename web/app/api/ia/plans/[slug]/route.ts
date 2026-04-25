import { NextResponse, type NextRequest } from "next/server";
import { getMasterPlan } from "@/lib/ia/queries";
import { iaJsonError, isDbConfigError, postgresErrorResponse } from "@/lib/ia/api-errors";

export const dynamic = "force-dynamic";

type Ctx = { params: Promise<{ slug: string }> };

export async function GET(_req: NextRequest, ctx: Ctx) {
  const { slug } = await ctx.params;
  if (!slug) return iaJsonError(400, "bad_request", "Missing slug");
  try {
    const plan = await getMasterPlan(slug);
    if (!plan) return iaJsonError(404, "not_found", "Master plan not found");
    return NextResponse.json(plan, { status: 200 });
  } catch (e) {
    if (isDbConfigError(e)) return iaJsonError(500, "internal", "Database not configured");
    return postgresErrorResponse(e, "Get master plan");
  }
}
