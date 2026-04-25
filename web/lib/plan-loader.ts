/**
 * plan-loader.ts — loadAllPlans() for the web workspace.
 *
 * Post `feature/ia-dev-db-refactor` Step 9.6: flat `ia/projects/*master-plan*.md`
 * files were deleted. DB (`ia_master_plans` / `ia_stages` / `ia_tasks`) is now
 * the sole source of truth.
 *
 * This module is a thin wrapper: dashboard RSC keeps calling `loadAllPlans()`,
 * but the underlying read goes through `loadDashboardData()` (DB-backed,
 * see `web/lib/ia/dashboard-data.ts`). Filesystem + GitHub-raw paths retired.
 *
 * @see `docs/web-dashboard-db-read-rewire.md`
 */

import { loadDashboardData } from "./ia/dashboard-data";
import type { PlanData } from "./plan-loader-types";

export async function loadAllPlans(): Promise<PlanData[]> {
  return loadDashboardData();
}
