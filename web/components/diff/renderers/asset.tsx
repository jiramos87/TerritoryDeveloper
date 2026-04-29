/**
 * `AssetDiff` — asset-kind typed diff renderer (TECH-3303 / Stage 14.3).
 *
 * @see ia/projects/asset-pipeline/stage-14.3 — TECH-3303 §Plan Digest
 */
"use client";

import type { KindDiff } from "@/lib/diff/kind-schemas";

import AddedRemovedBlock from "./_added-removed";
import RouteByHint from "./_route-by-hint";

export interface AssetDiffProps {
  diff: KindDiff;
}

export default function AssetDiff({ diff }: AssetDiffProps) {
  return (
    <div data-testid="asset-renderer" className="space-y-3">
      <AddedRemovedBlock added={diff.added} removed={diff.removed} />
      <RouteByHint changes={diff.changed} />
    </div>
  );
}
