import { NextResponse, type NextRequest } from "next/server";
import { getTask } from "@/lib/ia/queries";
import { iaJsonError, isDbConfigError, postgresErrorResponse } from "@/lib/ia/api-errors";

export const dynamic = "force-dynamic";

type Ctx = { params: Promise<{ id: string }> };

export async function GET(_req: NextRequest, ctx: Ctx) {
  const { id } = await ctx.params;
  if (!id) return iaJsonError(400, "bad_request", "Missing task id");
  try {
    const out = await getTask(id);
    if (!out) return iaJsonError(404, "not_found", "Task not found");
    return NextResponse.json(out, { status: 200 });
  } catch (e) {
    if (isDbConfigError(e)) return iaJsonError(500, "internal", "Database not configured");
    return postgresErrorResponse(e, "Get task");
  }
}
