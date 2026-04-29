"use client";

import { LintFailuresWidget } from "@/components/catalog/dashboard/LintFailuresWidget";
import { PublishQueueWidget } from "@/components/catalog/dashboard/PublishQueueWidget";
import { SnapshotFreshnessWidget } from "@/components/catalog/dashboard/SnapshotFreshnessWidget";
import { UnresolvedRefsWidget } from "@/components/catalog/dashboard/UnresolvedRefsWidget";

/**
 * Catalog dashboard — 4-widget 2×2 grid (TECH-4183 / Stage 15.1).
 * Polls each widget endpoint every 30s; pauses on tab hidden.
 */
export default function CatalogDashboardPage() {
  return (
    <div data-testid="catalog-dashboard" className="flex flex-col gap-[var(--ds-spacing-lg)]">
      <header>
        <h1 className="text-[length:var(--ds-font-size-h2)] font-semibold">Authoring console</h1>
        <p className="text-[var(--ds-text-muted)]">
          Health overview across content, configuration, and operations.
        </p>
      </header>
      <div className="grid grid-cols-1 gap-[var(--ds-spacing-md)] md:grid-cols-2">
        <UnresolvedRefsWidget />
        <LintFailuresWidget />
        <PublishQueueWidget />
        <SnapshotFreshnessWidget />
      </div>
    </div>
  );
}
