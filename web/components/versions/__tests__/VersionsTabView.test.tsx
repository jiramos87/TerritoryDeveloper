/**
 * VersionsTabView static-render tests (TECH-3223 / Stage 14.2).
 *
 * No jsdom — uses `react-dom/server` `renderToStaticMarkup` per existing
 * component test pattern (`web/components/entity/__tests__/...`).
 * Covers row render, diff href shape, status badge, empty state, error state,
 * loading state, load-more button visibility, drift guard against author
 * column re-introduction, and `formatRelativeTime` helper.
 *
 * @see web/components/versions/VersionsTabView.tsx
 */
import { describe, expect, it } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

import VersionsTabView, {
  diffHref,
  formatRelativeTime,
} from "@/components/versions/VersionsTabView";
import type { EntityVersionRow } from "@/lib/repos/history-repo";

function row(overrides: Partial<EntityVersionRow> = {}): EntityVersionRow {
  return {
    id: "1",
    entity_id: "10",
    version_number: 1,
    status: "draft",
    created_at: "2026-04-29T00:00:00.000Z",
    parent_version_id: null,
    archetype_version_id: null,
    ...overrides,
  };
}

describe("formatRelativeTime", () => {
  const now = new Date("2026-04-29T12:00:00.000Z");

  it("seconds bucket", () => {
    expect(formatRelativeTime("2026-04-29T11:59:30.000Z", now)).toBe("30s ago");
  });
  it("minutes bucket", () => {
    expect(formatRelativeTime("2026-04-29T11:30:00.000Z", now)).toBe("30m ago");
  });
  it("hours bucket", () => {
    expect(formatRelativeTime("2026-04-29T10:00:00.000Z", now)).toBe("2h ago");
  });
  it("days bucket", () => {
    expect(formatRelativeTime("2026-04-26T12:00:00.000Z", now)).toBe("3d ago");
  });
  it("months bucket", () => {
    expect(formatRelativeTime("2026-01-29T12:00:00.000Z", now)).toBe("3mo ago");
  });
  it("years bucket", () => {
    expect(formatRelativeTime("2024-04-29T12:00:00.000Z", now)).toBe("2y ago");
  });
  it("invalid iso passes through", () => {
    expect(formatRelativeTime("not-a-date", now)).toBe("not-a-date");
  });
});

describe("diffHref", () => {
  it("renders /catalog/{kind}/{entityId}/diff/{versionId}", () => {
    expect(diffHref("sprite", "42", "100")).toBe("/catalog/sprite/42/diff/100");
    expect(diffHref("archetype", "9", "55")).toBe(
      "/catalog/archetype/9/diff/55",
    );
  });
});

describe("<VersionsTabView />", () => {
  it("renders rows with version_number, status badge, time, diff link", () => {
    const html = renderToStaticMarkup(
      <VersionsTabView
        rows={[
          row({ id: "200", version_number: 2, status: "published" }),
          row({ id: "100", version_number: 1, status: "draft" }),
        ]}
        nextCursor={null}
        loading={false}
        error={null}
        kind="sprite"
        entityId="42"
      />,
    );
    expect(html).toContain('data-testid="versions-tab"');
    expect(html).toContain('data-testid="versions-list"');
    expect(html).toContain('data-version-id="200"');
    expect(html).toContain('data-version-id="100"');
    expect(html).toContain("v2");
    expect(html).toContain("v1");
    expect(html).toContain('data-status="published"');
    expect(html).toContain('data-status="draft"');
    expect(html).toContain('href="/catalog/sprite/42/diff/200"');
    expect(html).toContain('href="/catalog/sprite/42/diff/100"');
  });

  it("renders empty-state when rows is empty + not loading + no error", () => {
    const html = renderToStaticMarkup(
      <VersionsTabView
        rows={[]}
        nextCursor={null}
        loading={false}
        error={null}
        kind="sprite"
        entityId="42"
      />,
    );
    expect(html).toContain('data-testid="versions-empty"');
    expect(html).not.toContain('data-testid="versions-list"');
  });

  it("renders loading state when rows empty + loading=true", () => {
    const html = renderToStaticMarkup(
      <VersionsTabView
        rows={[]}
        nextCursor={null}
        loading={true}
        error={null}
        kind="sprite"
        entityId="42"
      />,
    );
    expect(html).toContain('data-testid="versions-loading"');
  });

  it("renders error state with retry button", () => {
    const html = renderToStaticMarkup(
      <VersionsTabView
        rows={[]}
        nextCursor={null}
        loading={false}
        error="boom"
        kind="sprite"
        entityId="42"
      />,
    );
    expect(html).toContain('data-testid="versions-error"');
    expect(html).toContain('data-testid="versions-retry"');
  });

  it("shows Load more button when nextCursor non-null", () => {
    const html = renderToStaticMarkup(
      <VersionsTabView
        rows={[row({ id: "1", version_number: 1 })]}
        nextCursor="some-cursor"
        loading={false}
        error={null}
        kind="sprite"
        entityId="42"
      />,
    );
    expect(html).toContain('data-testid="versions-load-more"');
  });

  it("hides Load more button when nextCursor null", () => {
    const html = renderToStaticMarkup(
      <VersionsTabView
        rows={[row({ id: "1", version_number: 1 })]}
        nextCursor={null}
        loading={false}
        error={null}
        kind="sprite"
        entityId="42"
      />,
    );
    expect(html).not.toContain('data-testid="versions-load-more"');
  });

  it("drift guard — never renders the word 'author' (column dropped from schema)", () => {
    const html = renderToStaticMarkup(
      <VersionsTabView
        rows={[row({ id: "1", version_number: 1 })]}
        nextCursor={null}
        loading={false}
        error={null}
        kind="sprite"
        entityId="42"
      />,
    );
    expect(html.toLowerCase()).not.toContain("author");
  });

  it("works for archetype kind (id-keyed wire)", () => {
    const html = renderToStaticMarkup(
      <VersionsTabView
        rows={[row({ id: "300", version_number: 5, status: "draft" })]}
        nextCursor={null}
        loading={false}
        error={null}
        kind="archetype"
        entityId="9"
      />,
    );
    expect(html).toContain('href="/catalog/archetype/9/diff/300"');
  });
});
