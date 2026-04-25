import { NextResponse, type NextRequest } from "next/server";
import { searchTaskSpecs } from "@/lib/ia/queries";
import { iaJsonError, isDbConfigError, postgresErrorResponse } from "@/lib/ia/api-errors";

export const dynamic = "force-dynamic";

export async function GET(req: NextRequest) {
  const url = new URL(req.url);
  const q = url.searchParams.get("q") ?? "";
  const limitRaw = url.searchParams.get("limit");
  const limit = limitRaw ? Number(limitRaw) : 25;
  if (!q.trim()) return iaJsonError(400, "bad_request", "Missing query parameter `q`");
  if (Number.isNaN(limit) || limit < 1 || limit > 100) {
    return iaJsonError(400, "bad_request", "`limit` must be 1..100");
  }
  try {
    const hits = await searchTaskSpecs(q, limit);
    return NextResponse.json({ q, limit, hits }, { status: 200 });
  } catch (e) {
    if (isDbConfigError(e)) return iaJsonError(500, "internal", "Database not configured");
    return postgresErrorResponse(e, "Search task specs");
  }
}
