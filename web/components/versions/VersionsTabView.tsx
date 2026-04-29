/**
 * Stateless view layer for VersionsTab (TECH-3223 / Stage 14.2).
 *
 * Pure render of timeline rows + empty / loading / error states. Container
 * (`VersionsTab`) owns fetch + cursor state and feeds props here. Splitting
 * keeps the static-markup test surface minimal (no jsdom / fetch mocking
 * required for view-level assertions).
 *
 * @see ia/projects/asset-pipeline/stage-14.2 — TECH-3223 §Plan Digest
 */
import type { CatalogKind } from "@/lib/refs/types";
import type { EntityVersionRow } from "@/lib/repos/history-repo";

export interface VersionsTabViewProps {
  rows: EntityVersionRow[];
  nextCursor: string | null;
  loading: boolean;
  error: string | null;
  kind: CatalogKind;
  entityId: string;
  onLoadMore?: () => void;
  onRetry?: () => void;
}

/**
 * Format an ISO-8601 timestamp as a short relative-time string.
 * Output suffix: `Xs/m/h/d/mo/y ago`.
 */
export function formatRelativeTime(iso: string, now: Date = new Date()): string {
  const t = new Date(iso).getTime();
  if (Number.isNaN(t)) return iso;
  const diffSec = Math.max(0, Math.floor((now.getTime() - t) / 1000));
  if (diffSec < 60) return `${diffSec}s ago`;
  const diffMin = Math.floor(diffSec / 60);
  if (diffMin < 60) return `${diffMin}m ago`;
  const diffHr = Math.floor(diffMin / 60);
  if (diffHr < 24) return `${diffHr}h ago`;
  const diffDay = Math.floor(diffHr / 24);
  if (diffDay < 30) return `${diffDay}d ago`;
  const diffMo = Math.floor(diffDay / 30);
  if (diffMo < 12) return `${diffMo}mo ago`;
  const diffYr = Math.floor(diffMo / 12);
  return `${diffYr}y ago`;
}

export function diffHref(
  kind: CatalogKind,
  entityId: string,
  versionId: string,
): string {
  return `/catalog/${kind}/${entityId}/diff/${versionId}`;
}

export default function VersionsTabView(props: VersionsTabViewProps) {
  const { rows, nextCursor, loading, error, kind, entityId, onLoadMore, onRetry } =
    props;

  if (loading && rows.length === 0) {
    return (
      <p data-testid="versions-loading" className="text-sm text-neutral-500">
        Loading…
      </p>
    );
  }

  if (error != null && rows.length === 0) {
    return (
      <div data-testid="versions-error" className="text-sm text-red-600">
        <p>Failed to load versions.</p>
        <button
          type="button"
          data-testid="versions-retry"
          onClick={onRetry}
          className="mt-2 underline"
        >
          Retry
        </button>
      </div>
    );
  }

  if (rows.length === 0) {
    return (
      <p data-testid="versions-empty" className="text-sm text-neutral-500">
        No versions yet.
      </p>
    );
  }

  return (
    <div data-testid="versions-tab">
      <ul data-testid="versions-list" className="space-y-2">
        {rows.map((r) => (
          <li
            key={r.id}
            data-testid="versions-row"
            data-version-id={r.id}
            className="flex items-center gap-3 rounded border border-neutral-200 px-3 py-2 text-sm"
          >
            <span data-testid="versions-row-number" className="font-mono">
              v{r.version_number}
            </span>
            <span
              data-testid="versions-row-status"
              data-status={r.status}
              className={
                r.status === "published"
                  ? "rounded bg-green-100 px-2 py-0.5 text-xs text-green-800"
                  : "rounded bg-neutral-100 px-2 py-0.5 text-xs text-neutral-700"
              }
            >
              {r.status}
            </span>
            <span
              data-testid="versions-row-time"
              className="text-xs text-neutral-500"
            >
              {formatRelativeTime(r.created_at)}
            </span>
            <a
              data-testid="versions-row-diff"
              href={diffHref(kind, entityId, r.id)}
              className="ml-auto text-xs text-blue-600 underline"
            >
              View diff
            </a>
          </li>
        ))}
      </ul>
      {nextCursor != null && (
        <button
          type="button"
          data-testid="versions-load-more"
          onClick={onLoadMore}
          disabled={loading}
          className="mt-3 rounded border border-neutral-300 px-3 py-1 text-sm hover:bg-neutral-50 disabled:opacity-50"
        >
          {loading ? "Loading…" : "Load more"}
        </button>
      )}
    </div>
  );
}
