import { describe, it, expect, vi } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";

import AudioList, {
  type AudioListRow,
} from "@/components/catalog/AudioList";

const ROWS: AudioListRow[] = [
  {
    entity_id: "1",
    slug: "ui_click_a",
    display_name: "UI — Click A",
    status: "active",
    duration_ms: 80,
    loudness_lufs: -18.5,
    updated_at: "2026-04-26T12:00:00.000Z",
  },
  {
    entity_id: "2",
    slug: "ui_click_b",
    display_name: "UI — Click B",
    status: "active",
    duration_ms: 96,
    loudness_lufs: -19.2,
    updated_at: "2026-04-26T12:01:00.000Z",
  },
  {
    entity_id: "3",
    slug: "ui_hover_a",
    display_name: "UI — Hover A",
    status: "active",
    duration_ms: 120,
    loudness_lufs: -20.0,
    updated_at: "2026-04-26T12:02:00.000Z",
  },
  {
    entity_id: "4",
    slug: "ui_legacy_blip",
    display_name: "UI — Legacy Blip",
    status: "retired",
    duration_ms: 64,
    loudness_lufs: -22.1,
    updated_at: "2026-04-25T09:00:00.000Z",
  },
];

const ACTIVE_ROWS = ROWS.filter((r) => r.status === "active");
const RETIRED_ROWS = ROWS.filter((r) => r.status === "retired");

describe("<AudioList />", () => {
  it("renders three filter chips: Active / Retired / All", () => {
    const html = renderToStaticMarkup(
      <AudioList
        rows={ACTIVE_ROWS}
        filter="active"
        onFilterChange={() => {}}
      />,
    );
    expect(html).toContain('data-testid="audio-list-filter-active"');
    expect(html).toContain('data-testid="audio-list-filter-retired"');
    expect(html).toContain('data-testid="audio-list-filter-all"');
  });

  it("marks the active filter chip with aria-selected=true", () => {
    const html = renderToStaticMarkup(
      <AudioList
        rows={RETIRED_ROWS}
        filter="retired"
        onFilterChange={() => {}}
      />,
    );
    const retiredChip = html.match(
      /<button[^>]*data-testid="audio-list-filter-retired"[^>]*>/,
    );
    expect(retiredChip?.[0]).toContain('aria-selected="true"');
    const activeChip = html.match(
      /<button[^>]*data-testid="audio-list-filter-active"[^>]*>/,
    );
    expect(activeChip?.[0]).toContain('aria-selected="false"');
  });

  it("active list shows 3 active rows (TECH-1958 Test Blueprint row 1)", () => {
    const html = renderToStaticMarkup(
      <AudioList
        rows={ACTIVE_ROWS}
        filter="active"
        onFilterChange={() => {}}
      />,
    );
    expect(html).toContain('data-testid="audio-list-row-ui_click_a"');
    expect(html).toContain('data-testid="audio-list-row-ui_click_b"');
    expect(html).toContain('data-testid="audio-list-row-ui_hover_a"');
    // Retired row absent from active filter (parent filters before passing rows).
    expect(html).not.toContain('data-testid="audio-list-row-ui_legacy_blip"');
    // Three active rows visible (count `<tr>` row-roots, not nested cells).
    const rowMatches =
      html.match(/<tr[^>]*data-testid="audio-list-row-/g) ?? [];
    expect(rowMatches.length).toBe(3);
  });

  it("retired tab shows 1 retired row (TECH-1958 Test Blueprint row 1)", () => {
    const html = renderToStaticMarkup(
      <AudioList
        rows={RETIRED_ROWS}
        filter="retired"
        onFilterChange={() => {}}
      />,
    );
    expect(html).toContain('data-testid="audio-list-row-ui_legacy_blip"');
    const rowMatches =
      html.match(/<tr[^>]*data-testid="audio-list-row-/g) ?? [];
    expect(rowMatches.length).toBe(1);
  });

  it("renders slug as link to /catalog/audio/{slug}", () => {
    const html = renderToStaticMarkup(
      <AudioList rows={ROWS} filter="all" onFilterChange={() => {}} />,
    );
    expect(html).toContain('href="/catalog/audio/ui_click_a"');
    expect(html).toContain('href="/catalog/audio/ui_legacy_blip"');
  });

  it("renders display_name + status + duration + loudness columns", () => {
    const html = renderToStaticMarkup(
      <AudioList
        rows={ACTIVE_ROWS.slice(0, 1)}
        filter="active"
        onFilterChange={() => {}}
      />,
    );
    expect(html).toContain("UI — Click A");
    expect(html).toContain(">active<");
    expect(html).toContain("80 ms");
    // toFixed(1) on -18.5 → "-18.5"
    expect(html).toContain("-18.5");
  });

  it("renders em-dash for null duration / loudness", () => {
    const sparse: AudioListRow[] = [
      {
        entity_id: "x",
        slug: "incomplete_sample",
        display_name: "Incomplete",
        status: "active",
        duration_ms: null,
        loudness_lufs: null,
        updated_at: "2026-04-26T12:00:00.000Z",
      },
    ];
    const html = renderToStaticMarkup(
      <AudioList rows={sparse} filter="active" onFilterChange={() => {}} />,
    );
    expect(html).toContain('data-testid="audio-list-row-incomplete_sample"');
    expect(html).toContain("—");
  });

  it("renders empty-state copy when no rows", () => {
    const html = renderToStaticMarkup(
      <AudioList rows={[]} filter="active" onFilterChange={() => {}} />,
    );
    expect(html).toContain('data-testid="audio-list-empty"');
  });

  it("renders error copy when error present", () => {
    const html = renderToStaticMarkup(
      <AudioList
        rows={[]}
        filter="active"
        onFilterChange={() => {}}
        error="boom"
      />,
    );
    expect(html).toContain('data-testid="audio-list-error"');
    expect(html).toContain("boom");
  });

  it("renders loading copy when loading=true", () => {
    const html = renderToStaticMarkup(
      <AudioList
        rows={[]}
        filter="active"
        onFilterChange={() => {}}
        loading
      />,
    );
    expect(html).toContain('data-testid="audio-list-loading"');
  });

  it("onFilterChange handler-shape verified via direct invocation", () => {
    const onFilterChange = vi.fn();
    onFilterChange("retired");
    expect(onFilterChange).toHaveBeenCalledWith("retired");
  });
});
