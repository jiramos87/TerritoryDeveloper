/**
 * Single-version diff page (TECH-3304 / Stage 14.3).
 *
 * Server component: parses route params, validates `kind` ∈ CatalogKind,
 * fetches `/api/catalog/{kind}/{id}/diff/{versionId}` server-side via
 * relative URL + `headers()` host (Next 15 async API), then renders
 * `<EntityVersionDiff />` plus a from/to version-metadata header.
 *
 * Reachable via `diffHref` (Stage 14.2 — `web/components/versions/VersionsTabView.tsx:46`).
 *
 * @see ia/projects/asset-pipeline/stage-14.3 — TECH-3304 §Plan Digest
 */
import { headers } from "next/headers";
import { notFound } from "next/navigation";

import EntityVersionDiff from "@/components/diff/EntityVersionDiff";
import type { KindDiff } from "@/lib/diff/kind-schemas";
import type { CatalogKind } from "@/lib/refs/types";
import type { EntityVersionRow } from "@/lib/repos/history-repo";

export const dynamic = "force-dynamic";

const VALID_KINDS: ReadonlySet<string> = new Set([
  "sprite",
  "asset",
  "button",
  "panel",
  "pool",
  "token",
  "archetype",
  "audio",
]);

type Ctx = {
  params: Promise<{ kind: string; id: string; versionId: string }>;
};

interface DiffApiResponse {
  ok: boolean;
  data?: {
    from: EntityVersionRow | null;
    to: EntityVersionRow;
    diff: KindDiff;
  };
  error?: { code: string; message: string };
}

async function fetchDiffPayload(
  kind: string,
  id: string,
  versionId: string,
): Promise<DiffApiResponse["data"] | null> {
  const h = await headers();
  const host = h.get("host");
  const proto = h.get("x-forwarded-proto") ?? "http";
  if (!host) return null;
  const url = `${proto}://${host}/api/catalog/${kind}/${id}/diff/${versionId}`;
  const res = await fetch(url, { cache: "no-store" });
  if (res.status === 404) return null;
  if (!res.ok) return null;
  const body = (await res.json()) as DiffApiResponse;
  if (!body.ok || body.data == null) return null;
  return body.data;
}

function VersionHeader({
  from,
  to,
}: {
  from: EntityVersionRow | null;
  to: EntityVersionRow;
}) {
  return (
    <header
      data-testid="diff-page-header"
      className="mb-4 rounded border border-neutral-200 bg-neutral-50 p-3 text-sm"
    >
      <div className="flex items-center gap-2">
        <span data-testid="diff-from-version" className="font-mono">
          {from == null ? "(root)" : `v${from.version_number}`}
        </span>
        <span aria-hidden className="text-neutral-400">
          →
        </span>
        <span data-testid="diff-to-version" className="font-mono">
          v{to.version_number}
        </span>
      </div>
      <div className="mt-1 text-xs text-neutral-500">
        {from != null && (
          <span data-testid="diff-from-created-at">{from.created_at}</span>
        )}
        {from != null && <span aria-hidden> · </span>}
        <span data-testid="diff-to-created-at">{to.created_at}</span>
      </div>
    </header>
  );
}

export default async function EntityVersionDiffPage({ params }: Ctx) {
  const { kind, id, versionId } = await params;
  if (!VALID_KINDS.has(kind)) notFound();
  const payload = await fetchDiffPayload(kind, id, versionId);
  if (payload == null) notFound();
  return (
    <main data-testid="diff-page-root" className="p-4">
      <VersionHeader from={payload.from} to={payload.to} />
      <EntityVersionDiff
        kind={kind as CatalogKind}
        diff={payload.diff}
      />
    </main>
  );
}
