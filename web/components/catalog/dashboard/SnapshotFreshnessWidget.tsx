"use client";

import type { SnapshotFreshnessResponse } from "@/app/api/catalog/dashboard/snapshot-freshness/route";
import { useDashboardPoll } from "@/lib/hooks/useDashboardPoll";
import { DashboardCard } from "./DashboardCard";

export function SnapshotFreshnessWidget() {
  const poll = useDashboardPoll<SnapshotFreshnessResponse>(
    "/api/catalog/dashboard/snapshot-freshness",
  );

  return (
    <DashboardCard
      title="Snapshot freshness"
      loading={poll.status === "loading"}
      error={poll.status === "error" ? poll.error : null}
      testId="widget-snapshot-freshness"
    >
      {poll.status === "ok" && (
        poll.data.items.length === 0 ? (
          <p className="text-sm text-[var(--ds-text-muted)]">No snapshots yet.</p>
        ) : (
          <ul data-testid="widget-snapshot-freshness-list" className="flex flex-col gap-[var(--ds-spacing-xs)] text-sm">
            {poll.data.items.map((item) => (
              <li key={item.kind} className="flex items-center justify-between gap-[var(--ds-spacing-sm)]">
                <span className="font-mono text-[var(--ds-text-primary)]">{item.kind}</span>
                <div className="flex items-center gap-[var(--ds-spacing-xs)]">
                  {item.stale && (
                    <span
                      data-testid={`freshness-stale-${item.kind}`}
                      className="rounded bg-[var(--ds-text-accent-warn)] px-[var(--ds-spacing-xs)] text-xs text-[var(--ds-bg-canvas)]"
                    >
                      stale
                    </span>
                  )}
                  <span className="text-[var(--ds-text-muted)] text-xs">
                    {new Date(item.latest_at).toLocaleDateString()}
                  </span>
                </div>
              </li>
            ))}
          </ul>
        )
      )}
    </DashboardCard>
  );
}
