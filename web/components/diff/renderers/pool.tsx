/**
 * `PoolDiff` — pool-kind typed diff renderer (TECH-3304 / Stage 14.3).
 *
 * List-heavy: `members` field always rendered as `ListFieldDiff` (set diff
 * with + added / - removed markers per element) regardless of hint, since
 * pool membership changes are the dominant diff signal. Other fields route
 * via `RouteByHint` fallback.
 *
 * @see ia/projects/asset-pipeline/stage-14.3 — TECH-3304 §Plan Digest
 */
"use client";

import type { KindDiff, KindDiffChange } from "@/lib/diff/kind-schemas";

import AddedRemovedBlock from "./_added-removed";
import RouteByHint from "./_route-by-hint";
import ListFieldDiff from "./list";

const FORCE_LIST_FIELDS = new Set(["members", "member_asset_ids"]);

export interface PoolDiffProps {
  diff: KindDiff;
}

export default function PoolDiff({ diff }: PoolDiffProps) {
  const forced: KindDiffChange[] = [];
  const rest: KindDiffChange[] = [];
  for (const c of diff.changed) {
    if (FORCE_LIST_FIELDS.has(c.field)) forced.push(c);
    else rest.push(c);
  }
  return (
    <div data-testid="pool-renderer" className="space-y-3">
      <AddedRemovedBlock added={diff.added} removed={diff.removed} />
      {forced.length > 0 && (
        <div data-testid="pool-members-block" className="space-y-2">
          {forced.map((c) => (
            <ListFieldDiff
              key={c.field}
              field={c.field}
              before={c.before}
              after={c.after}
            />
          ))}
        </div>
      )}
      <RouteByHint changes={rest} />
    </div>
  );
}
