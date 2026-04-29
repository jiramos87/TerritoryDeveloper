/**
 * `PanelDiff` — panel-kind typed diff renderer (TECH-3304 / Stage 14.3).
 *
 * Routes changed fields via shared `RouteByHint` helper using hints from
 * `kind-schemas.ts` (`background_token` / `border_token` -> token, list
 * fields -> ListFieldDiff). No kind-specific override — token swatch path
 * is owned by `token.tsx` (T14.3.4); panel renders token refs as scalar.
 *
 * @see ia/projects/asset-pipeline/stage-14.3 — TECH-3304 §Plan Digest
 */
"use client";

import type { KindDiff } from "@/lib/diff/kind-schemas";

import AddedRemovedBlock from "./_added-removed";
import RouteByHint from "./_route-by-hint";

export interface PanelDiffProps {
  diff: KindDiff;
}

export default function PanelDiff({ diff }: PanelDiffProps) {
  return (
    <div data-testid="panel-renderer" className="space-y-3">
      <AddedRemovedBlock added={diff.added} removed={diff.removed} />
      <RouteByHint changes={diff.changed} />
    </div>
  );
}
