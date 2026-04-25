import { NextResponse, type NextRequest } from "next/server";
import { loadDashboardData } from "@/lib/ia/dashboard-data";
import { iaJsonError, isDbConfigError, postgresErrorResponse } from "@/lib/ia/api-errors";

export const dynamic = "force-dynamic";

export async function GET(_req: NextRequest) {
  try {
    const plans = await loadDashboardData();
    return NextResponse.json({ plans }, { status: 200 });
  } catch (e) {
    if (isDbConfigError(e)) return iaJsonError(500, "internal", "Database not configured");
    return postgresErrorResponse(e, "Load dashboard data");
  }
}
