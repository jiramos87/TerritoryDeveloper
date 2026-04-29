/**
 * Internal helper — render `KindDiff.added` and `KindDiff.removed` blocks
 * as plain field-name lists (TECH-3303 / Stage 14.3).
 *
 * `added` and `removed` show only the field name (no value) per Plan Digest
 * §Test Blueprint shape: KindDiff treats added/removed as field-presence
 * indicators; renderer surfaces them as labeled chips.
 *
 * @see ia/projects/asset-pipeline/stage-14.3 — TECH-3303 §Plan Digest
 */
"use client";

export interface AddedRemovedBlockProps {
  added: string[];
  removed: string[];
}

export default function AddedRemovedBlock({ added, removed }: AddedRemovedBlockProps) {
  if (added.length === 0 && removed.length === 0) return null;
  return (
    <div data-testid="added-removed-block" className="space-y-1 text-sm">
      {added.length > 0 && (
        <div data-testid="added-block">
          <span className="text-xs text-neutral-500">added:</span>
          <ul className="mt-1 ml-4">
            {added.map((f) => (
              <li
                key={f}
                data-testid="added-field"
                className="rounded bg-green-50 px-1 font-mono text-xs text-green-700"
              >
                + {f}
              </li>
            ))}
          </ul>
        </div>
      )}
      {removed.length > 0 && (
        <div data-testid="removed-block">
          <span className="text-xs text-neutral-500">removed:</span>
          <ul className="mt-1 ml-4">
            {removed.map((f) => (
              <li
                key={f}
                data-testid="removed-field"
                className="rounded bg-red-50 px-1 font-mono text-xs text-red-700 line-through"
              >
                - {f}
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}
