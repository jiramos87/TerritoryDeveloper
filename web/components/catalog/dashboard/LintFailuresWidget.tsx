"use client";

import type { LintFailuresResponse } from "@/app/api/catalog/dashboard/lint-failures/route";
import { useDashboardPoll } from "@/lib/hooks/useDashboardPoll";
import { DashboardCard } from "./DashboardCard";

function relativeTime(iso: string): string {
  const diff = Date.now() - new Date(iso).getTime();
  const mins = Math.floor(diff / 60_000);
  if (mins < 60) return `${mins}m ago`;
  const hrs = Math.floor(mins / 60);
  if (hrs < 24) return `${hrs}h ago`;
  return `${Math.floor(hrs / 24)}d ago`;
}

export function LintFailuresWidget() {
  const poll = useDashboardPoll<LintFailuresResponse>(
    "/api/catalog/dashboard/lint-failures",
  );

  return (
    <DashboardCard
      title="Lint failures"
      loading={poll.status === "loading"}
      error={poll.status === "error" ? poll.error : null}
      testId="widget-lint-failures"
    >
      {poll.status === "ok" && (
        poll.data.items.length === 0 ? (
          <p className="text-sm text-[var(--ds-text-muted)]">No failures.</p>
        ) : (
          <ul data-testid="widget-lint-failures-list" className="flex flex-col gap-[var(--ds-spacing-xs)] text-sm">
            {poll.data.items.map((item) => (
              <li key={item.id} className="flex flex-col gap-[1px] border-b border-[var(--ds-border-subtle)] pb-[var(--ds-spacing-xs)]">
                <div className="flex items-center justify-between gap-[var(--ds-spacing-xs)]">
                  <span className="font-mono text-[var(--ds-text-primary)]">
                    {item.entity_slug ?? `#${item.entity_id}`}
                  </span>
                  <span className="text-[var(--ds-text-muted)] text-xs">{relativeTime(item.created_at)}</span>
                </div>
                <div className="flex items-center gap-[var(--ds-spacing-xs)]">
                  <span className="font-mono text-xs text-[var(--ds-text-muted)]">{item.rule_id}</span>
                </div>
                <p className="text-[var(--ds-text-muted)] text-xs truncate" title={item.message}>
                  {item.message}
                </p>
              </li>
            ))}
          </ul>
        )
      )}
    </DashboardCard>
  );
}
