/**
 * Internal helper — route each `KindDiff.changed[]` entry to the right
 * fallback renderer based on its `hint` (TECH-3303 / Stage 14.3).
 *
 * Per-kind renderers (`sprite.tsx` / `asset.tsx` / etc.) compose this helper
 * to render the `changed` block; `added` / `removed` blocks are rendered
 * separately as inline lists.
 *
 * Token-hinted fields fall back to scalar rendering here — kind renderers
 * (e.g. `token.tsx`) override locally to inject the swatch chip when the
 * hint = `"token"` AND `CSS.supports("color", value)`.
 *
 * @see ia/projects/asset-pipeline/stage-14.3 — TECH-3303 §Plan Digest
 */
"use client";

import type { KindDiffChange } from "@/lib/diff/kind-schemas";

import BlobFieldDiff from "./blob";
import ListFieldDiff from "./list";
import ScalarFieldDiff from "./scalar";

export interface RouteByHintProps {
  changes: KindDiffChange[];
  /** Optional override map keyed by hint — kind renderers swap behavior per hint. */
  overrides?: Partial<
    Record<
      KindDiffChange["hint"],
      (change: KindDiffChange) => React.ReactNode
    >
  >;
}

export default function RouteByHint({ changes, overrides }: RouteByHintProps) {
  return (
    <div data-testid="route-by-hint" className="space-y-2">
      {changes.map((c) => {
        const override = overrides?.[c.hint];
        if (override) {
          return <div key={c.field}>{override(c)}</div>;
        }
        switch (c.hint) {
          case "list":
            return (
              <ListFieldDiff
                key={c.field}
                field={c.field}
                before={c.before}
                after={c.after}
              />
            );
          case "blob":
            return (
              <BlobFieldDiff
                key={c.field}
                field={c.field}
                before={c.before}
                after={c.after}
              />
            );
          case "scalar":
          case "token":
          case "asset":
          case "sprite":
          case "audio":
          default:
            return (
              <ScalarFieldDiff
                key={c.field}
                field={c.field}
                before={c.before}
                after={c.after}
              />
            );
        }
      })}
    </div>
  );
}
