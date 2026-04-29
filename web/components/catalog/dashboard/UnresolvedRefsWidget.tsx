"use client";

import Link from "next/link";

import type { UnresolvedRefsResponse } from "@/app/api/catalog/dashboard/unresolved-refs/route";
import { useDashboardPoll } from "@/lib/hooks/useDashboardPoll";
import { DashboardCard } from "./DashboardCard";

export function UnresolvedRefsWidget() {
  const poll = useDashboardPoll<UnresolvedRefsResponse>(
    "/api/catalog/dashboard/unresolved-refs",
  );

  return (
    <DashboardCard
      title="Unresolved refs"
      loading={poll.status === "loading"}
      error={poll.status === "error" ? poll.error : null}
      testId="widget-unresolved-refs"
    >
      {poll.status === "ok" && (
        <div className="flex flex-col gap-[var(--ds-spacing-xs)]">
          <p
            data-testid="widget-unresolved-refs-count"
            className={
              poll.data.count > 0
                ? "text-4xl font-bold text-[var(--ds-text-accent-critical)]"
                : "text-4xl font-bold text-[var(--ds-text-accent-info)]"
            }
          >
            {poll.data.count}
          </p>
          <Link
            href="/catalog/refs?filter=unresolved"
            className="text-sm text-[var(--ds-text-accent-info)] hover:underline"
          >
            View unresolved refs →
          </Link>
        </div>
      )}
    </DashboardCard>
  );
}
