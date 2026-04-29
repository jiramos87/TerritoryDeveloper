"use client";

import type { QueueDepthResponse } from "@/app/api/catalog/dashboard/queue-depth/route";
import { useDashboardPoll } from "@/lib/hooks/useDashboardPoll";
import { DashboardCard } from "./DashboardCard";

export function PublishQueueWidget() {
  const poll = useDashboardPoll<QueueDepthResponse>(
    "/api/catalog/dashboard/queue-depth",
  );

  return (
    <DashboardCard
      title="Publish queue"
      loading={poll.status === "loading"}
      error={poll.status === "error" ? poll.error : null}
      testId="widget-publish-queue"
    >
      {poll.status === "ok" && (
        <div data-testid="widget-publish-queue-data" className="flex flex-col gap-[var(--ds-spacing-xs)]">
          <p className="text-4xl font-bold text-[var(--ds-text-primary)]">
            {poll.data.total}
          </p>
          <dl className="flex gap-[var(--ds-spacing-md)] text-sm">
            <div className="flex gap-[var(--ds-spacing-xs)]">
              <dt className="text-[var(--ds-text-muted)]">Queued</dt>
              <dd className="font-semibold text-[var(--ds-text-primary)]">{poll.data.queued}</dd>
            </div>
            <div className="flex gap-[var(--ds-spacing-xs)]">
              <dt className="text-[var(--ds-text-muted)]">Running</dt>
              <dd className="font-semibold text-[var(--ds-text-accent-warn)]">{poll.data.running}</dd>
            </div>
          </dl>
        </div>
      )}
    </DashboardCard>
  );
}
