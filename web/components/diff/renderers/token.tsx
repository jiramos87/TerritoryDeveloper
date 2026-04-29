/**
 * `TokenDiff` — token-kind typed diff renderer (TECH-3303 / Stage 14.3).
 *
 * Token-hinted color fields render as visual swatch chips when the value
 * parses as a CSS color (`CSS.supports("color", value)`); falls back to
 * scalar rendering otherwise. Non-token fields delegate to fallback renderers.
 *
 * Inline `TokenSwatchChip` is server-render-safe — `CSS.supports` is guarded
 * via `typeof CSS !== "undefined"` so SSR / `renderToStaticMarkup` paths fall
 * back to scalar (matches T14.3.3 fallback decision tree).
 *
 * @see ia/projects/asset-pipeline/stage-14.3 — TECH-3303 §Plan Digest
 */
"use client";

import type { KindDiff, KindDiffChange } from "@/lib/diff/kind-schemas";

import AddedRemovedBlock from "./_added-removed";
import RouteByHint from "./_route-by-hint";
import ScalarFieldDiff from "./scalar";

export interface TokenDiffProps {
  diff: KindDiff;
}

function isCssColor(value: unknown): value is string {
  if (typeof value !== "string") return false;
  if (value.length === 0) return false;
  if (typeof CSS === "undefined" || typeof CSS.supports !== "function") {
    return false;
  }
  try {
    return CSS.supports("color", value);
  } catch {
    return false;
  }
}

function TokenSwatchChip({ value, label }: { value: string; label: string }) {
  return (
    <span
      data-testid="token-swatch-chip"
      data-label={label}
      className="inline-flex items-center gap-1 align-middle"
    >
      <span
        data-testid="token-swatch-color"
        aria-hidden
        className="inline-block h-6 w-6 rounded border border-neutral-300"
        style={{ background: value }}
      />
      <code className="font-mono text-xs text-neutral-600">{value}</code>
    </span>
  );
}

function TokenSwatchPair({ change }: { change: KindDiffChange }) {
  const { field, before, after } = change;
  const beforeColor = isCssColor(before);
  const afterColor = isCssColor(after);
  if (!beforeColor && !afterColor) {
    return <ScalarFieldDiff field={field} before={before} after={after} />;
  }
  return (
    <div
      data-testid="token-swatch-diff"
      data-field={field}
      className="flex items-center gap-2 text-sm"
    >
      <span className="font-mono text-neutral-500">{field}</span>
      <span data-testid="token-swatch-before" className="rounded bg-red-50 px-1">
        {beforeColor ? (
          <TokenSwatchChip value={String(before)} label="before" />
        ) : (
          <code className="text-xs text-neutral-600">
            {before == null ? "(none)" : String(before)}
          </code>
        )}
      </span>
      <span aria-hidden className="text-neutral-400">
        →
      </span>
      <span data-testid="token-swatch-after" className="rounded bg-green-50 px-1">
        {afterColor ? (
          <TokenSwatchChip value={String(after)} label="after" />
        ) : (
          <code className="text-xs text-neutral-600">
            {after == null ? "(none)" : String(after)}
          </code>
        )}
      </span>
    </div>
  );
}

export default function TokenDiff({ diff }: TokenDiffProps) {
  return (
    <div data-testid="token-renderer" className="space-y-3">
      <AddedRemovedBlock added={diff.added} removed={diff.removed} />
      <RouteByHint
        changes={diff.changed}
        overrides={{
          token: (c) => <TokenSwatchPair change={c} />,
        }}
      />
    </div>
  );
}
