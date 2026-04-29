/**
 * `ArchetypeDiff` — archetype-kind typed diff renderer (TECH-3304 / Stage 14.3).
 *
 * Archetype payloads embed sub-payloads keyed by nested-kind. When a changed
 * field's hint = `"asset"` / `"sprite"` / `"audio"` / `"token"`, this renderer
 * recursively dispatches to that kind's renderer against a synthetic sub-`KindDiff`
 * (single-field changed entry promoted to a sub-diff scalar dispatch).
 *
 * Direct kind-renderer import avoids `<EntityVersionDiff />` self-call recursion;
 * archetype sub-payloads are not arbitrarily deep (one level of nesting per
 * Stage 14.3 scope).
 *
 * Other fields (lists / tags / scalars) route via shared `RouteByHint`.
 *
 * @see ia/projects/asset-pipeline/stage-14.3 — TECH-3304 §Plan Digest
 */
"use client";

import type {
  FieldHint,
  KindDiff,
  KindDiffChange,
} from "@/lib/diff/kind-schemas";

import AddedRemovedBlock from "./_added-removed";
import RouteByHint from "./_route-by-hint";
import AssetDiff from "./asset";
import AudioDiff from "./audio";
import SpriteDiff from "./sprite";
import TokenDiff from "./token";

const NESTED_KIND_HINTS: ReadonlySet<FieldHint> = new Set([
  "asset",
  "sprite",
  "audio",
  "token",
]);

export interface ArchetypeDiffProps {
  diff: KindDiff;
}

function NestedKindBlock({ change }: { change: KindDiffChange }) {
  const subDiff: KindDiff = {
    added: [],
    removed: [],
    changed: [{ ...change, hint: "scalar" }],
  };
  return (
    <div
      data-testid="archetype-nested-block"
      data-field={change.field}
      data-nested-kind={change.hint}
      className="rounded border border-neutral-200 p-2"
    >
      <span className="font-mono text-xs text-neutral-500">
        {change.field} (nested {change.hint})
      </span>
      <div className="mt-1">
        {change.hint === "asset" ? (
          <AssetDiff diff={subDiff} />
        ) : change.hint === "sprite" ? (
          <SpriteDiff diff={subDiff} />
        ) : change.hint === "audio" ? (
          <AudioDiff diff={subDiff} />
        ) : change.hint === "token" ? (
          <TokenDiff diff={subDiff} />
        ) : null}
      </div>
    </div>
  );
}

export default function ArchetypeDiff({ diff }: ArchetypeDiffProps) {
  const nested: KindDiffChange[] = [];
  const rest: KindDiffChange[] = [];
  for (const c of diff.changed) {
    if (NESTED_KIND_HINTS.has(c.hint)) nested.push(c);
    else rest.push(c);
  }
  return (
    <div data-testid="archetype-renderer" className="space-y-3">
      <AddedRemovedBlock added={diff.added} removed={diff.removed} />
      {nested.length > 0 && (
        <div data-testid="archetype-nested-block-list" className="space-y-2">
          {nested.map((c) => (
            <NestedKindBlock key={c.field} change={c} />
          ))}
        </div>
      )}
      <RouteByHint changes={rest} />
    </div>
  );
}
