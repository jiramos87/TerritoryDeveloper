/**
 * `SpriteDiff` — sprite-kind typed diff renderer (TECH-3303 / Stage 14.3).
 *
 * Iterates `diff.added` / `diff.removed` / `diff.changed`, routes each
 * changed field via `hintFor("sprite", field)` (already resolved on each
 * `KindDiffChange.hint`) to the matching fallback renderer.
 *
 * Sprite has no kind-specific rendering — fully delegated to fallbacks.
 *
 * @see ia/projects/asset-pipeline/stage-14.3 — TECH-3303 §Plan Digest
 */
"use client";

import type { KindDiff } from "@/lib/diff/kind-schemas";

import AddedRemovedBlock from "./_added-removed";
import RouteByHint from "./_route-by-hint";

export interface SpriteDiffProps {
  diff: KindDiff;
}

export default function SpriteDiff({ diff }: SpriteDiffProps) {
  return (
    <div data-testid="sprite-renderer" className="space-y-3">
      <AddedRemovedBlock added={diff.added} removed={diff.removed} />
      <RouteByHint changes={diff.changed} />
    </div>
  );
}
