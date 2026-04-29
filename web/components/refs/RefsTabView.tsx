/**
 * Stateless view layer for RefsTab (TECH-3409 / Stage 14.4).
 *
 * Pure render of two-column refs grid (incoming / outgoing) + per-side empty /
 * loading / error / load-more states. Container (`RefsTab`) owns fetch + cursor
 * state and feeds props here. View has zero hooks / no data-loading calls.
 *
 * @see ia/projects/asset-pipeline/stage-14.4 — TECH-3409 §Plan Digest
 */
import type { CatalogRefEdgeRow } from "@/lib/repos/refs-repo";

import type { RefsSide } from "./RefsTab";

export interface RefsSideState {
  rows: CatalogRefEdgeRow[];
  nextCursor: string | null;
  loading: boolean;
  error: string | null;
}

export interface RefsTabViewProps {
  incoming: RefsSideState;
  outgoing: RefsSideState;
  onLoadMore?: (side: RefsSide) => void;
  onRetry?: (side: RefsSide) => void;
}

/**
 * Pure helper — anchor target for a refs row. Mirrors archetype/asset detail
 * route shape `/catalog/{kind}/{id}` so id-keyed (archetype) + slug-keyed
 * (sprite/asset/button/panel/audio/pool/token) detail pages both resolve.
 */
export function refsLinkHref(kind: string, id: string): string {
  return `/catalog/${kind}/${id}`;
}

function RefsColumn(props: {
  side: RefsSide;
  state: RefsSideState;
  header: string;
  emptyText: string;
  rowFormatter: (row: CatalogRefEdgeRow) => {
    text: string;
    href: string;
    otherKind: string;
    otherId: string;
  };
  onLoadMore?: (side: RefsSide) => void;
  onRetry?: (side: RefsSide) => void;
}) {
  const { side, state, header, emptyText, rowFormatter, onLoadMore, onRetry } =
    props;
  const { rows, nextCursor, loading, error } = state;

  return (
    <section
      data-testid={`refs-${side}-column`}
      className="rounded border border-neutral-200 p-3"
    >
      <h3 className="mb-2 text-sm font-semibold text-neutral-700">{header}</h3>
      {loading && rows.length === 0 ? (
        <p
          data-testid={`refs-${side}-loading`}
          className="text-sm text-neutral-500"
        >
          Loading…
        </p>
      ) : error != null && rows.length === 0 ? (
        <div
          data-testid={`refs-${side}-error`}
          className="text-sm text-red-600"
        >
          <p>Failed to load refs.</p>
          <button
            type="button"
            data-testid={`refs-${side}-retry`}
            onClick={() => onRetry?.(side)}
            className="mt-2 underline"
          >
            Retry
          </button>
        </div>
      ) : rows.length === 0 ? (
        <p
          data-testid={`refs-${side}-empty`}
          className="text-sm text-neutral-500"
        >
          {emptyText}
        </p>
      ) : (
        <>
          <ul
            data-testid={`refs-${side}-list`}
            className="space-y-1 text-sm"
          >
            {rows.map((r) => {
              const fmt = rowFormatter(r);
              const key = `${r.src_id}-${r.dst_id}-${r.created_at}`;
              return (
                <li
                  key={key}
                  data-testid={`refs-${side}-row`}
                  data-edge-role={r.edge_role}
                  className="flex items-center justify-between rounded border border-neutral-100 px-2 py-1"
                >
                  <a
                    href={fmt.href}
                    className="text-blue-600 underline"
                  >
                    {fmt.text}
                  </a>
                </li>
              );
            })}
          </ul>
          {nextCursor != null && (
            <button
              type="button"
              data-testid={`refs-${side}-load-more`}
              onClick={() => onLoadMore?.(side)}
              disabled={loading}
              className="mt-3 rounded border border-neutral-300 px-3 py-1 text-sm hover:bg-neutral-50 disabled:opacity-50"
            >
              {loading ? "Loading…" : "Load more"}
            </button>
          )}
        </>
      )}
    </section>
  );
}

export default function RefsTabView(props: RefsTabViewProps) {
  const { incoming, outgoing, onLoadMore, onRetry } = props;

  return (
    <div
      data-testid="refs-tab"
      className="grid grid-cols-1 gap-4 md:grid-cols-2"
    >
      <RefsColumn
        side="incoming"
        state={incoming}
        header="Incoming refs"
        emptyText="No incoming refs."
        rowFormatter={(r) => ({
          text: `${r.src_kind} #${r.src_id} \u2192 this`,
          href: refsLinkHref(r.src_kind, r.src_id),
          otherKind: r.src_kind,
          otherId: r.src_id,
        })}
        onLoadMore={onLoadMore}
        onRetry={onRetry}
      />
      <RefsColumn
        side="outgoing"
        state={outgoing}
        header="Outgoing refs"
        emptyText="No outgoing refs."
        rowFormatter={(r) => ({
          text: `this \u2192 ${r.dst_kind} #${r.dst_id}`,
          href: refsLinkHref(r.dst_kind, r.dst_id),
          otherKind: r.dst_kind,
          otherId: r.dst_id,
        })}
        onLoadMore={onLoadMore}
        onRetry={onRetry}
      />
    </div>
  );
}
