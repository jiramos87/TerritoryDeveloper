/**
 * Dashboard widget components — render with mocked fetch (TECH-4183 §Test Blueprint).
 * Uses renderToStaticMarkup for static-markup checks (no @testing-library).
 */
import { describe, expect, it, vi } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

import { DashboardCard } from "@/components/catalog/dashboard/DashboardCard";

// Minimal fetch mock — widgets call useDashboardPoll which calls fetch; but
// renderToStaticMarkup renders only the initial state (loading skeleton).
vi.stubGlobal("fetch", vi.fn().mockResolvedValue({
  ok: true,
  json: () => Promise.resolve({ ok: true, data: {} }),
}));

// ---------- DashboardCard ----------

describe("<DashboardCard />", () => {
  it("renders title", () => {
    const html = renderToStaticMarkup(<DashboardCard title="Test widget" />);
    expect(html).toContain("Test widget");
  });

  it("renders error banner on error", () => {
    const html = renderToStaticMarkup(
      <DashboardCard title="Widget" error="DB down" />,
    );
    expect(html).toContain("DB down");
    expect(html).toContain('role="alert"');
  });

  it("renders skeleton when loading", () => {
    const html = renderToStaticMarkup(
      <DashboardCard title="Widget" loading />,
    );
    expect(html).toContain("animate-pulse");
  });

  it("renders children when not loading and no error", () => {
    const html = renderToStaticMarkup(
      <DashboardCard title="Widget"><span id="body-content">hello</span></DashboardCard>,
    );
    expect(html).toContain("body-content");
  });

  it("error takes precedence over loading + children", () => {
    const html = renderToStaticMarkup(
      <DashboardCard title="Widget" loading error="oops"><span>ignored</span></DashboardCard>,
    );
    expect(html).toContain("oops");
    expect(html).not.toContain("animate-pulse");
    expect(html).not.toContain("ignored");
  });
});

// ---------- UnresolvedRefsWidget render smoke ----------

import { UnresolvedRefsWidget } from "@/components/catalog/dashboard/UnresolvedRefsWidget";

describe("<UnresolvedRefsWidget />", () => {
  it("renders without crashing (initial skeleton state)", () => {
    const html = renderToStaticMarkup(<UnresolvedRefsWidget />);
    expect(html).toContain("Unresolved refs");
  });
});

// ---------- LintFailuresWidget render smoke ----------

import { LintFailuresWidget } from "@/components/catalog/dashboard/LintFailuresWidget";

describe("<LintFailuresWidget />", () => {
  it("renders without crashing", () => {
    const html = renderToStaticMarkup(<LintFailuresWidget />);
    expect(html).toContain("Lint failures");
  });
});

// ---------- PublishQueueWidget render smoke ----------

import { PublishQueueWidget } from "@/components/catalog/dashboard/PublishQueueWidget";

describe("<PublishQueueWidget />", () => {
  it("renders without crashing", () => {
    const html = renderToStaticMarkup(<PublishQueueWidget />);
    expect(html).toContain("Publish queue");
  });
});

// ---------- SnapshotFreshnessWidget render smoke ----------

import { SnapshotFreshnessWidget } from "@/components/catalog/dashboard/SnapshotFreshnessWidget";

describe("<SnapshotFreshnessWidget />", () => {
  it("renders without crashing", () => {
    const html = renderToStaticMarkup(<SnapshotFreshnessWidget />);
    expect(html).toContain("Snapshot freshness");
  });
});
