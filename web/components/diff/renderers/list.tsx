/**
 * `ListFieldDiff` — line-level added/removed markers (TECH-3302 / Stage 14.3).
 *
 * Computes set-diff between `before` and `after` arrays (cast to strings),
 * renders `+` lines for added and `-` lines for removed. No LCS — `params_json`
 * list fields hold short identifiers, not text (Pending Decision: list_diff_strategy).
 *
 * Falls back to scalar-pair rendering when either input is not an array.
 *
 * @see ia/projects/asset-pipeline/stage-14.3 — TECH-3302 §Plan Digest
 */
"use client";

import ScalarFieldDiff from "./scalar";

export interface ListFieldDiffProps {
  field: string;
  before: unknown;
  after: unknown;
}

function renderItem(v: unknown): string {
  if (v == null) return "(none)";
  if (typeof v === "string") return v;
  if (typeof v === "number" || typeof v === "boolean") return String(v);
  return JSON.stringify(v);
}

export default function ListFieldDiff({ field, before, after }: ListFieldDiffProps) {
  if (!Array.isArray(before) || !Array.isArray(after)) {
    return <ScalarFieldDiff field={field} before={before} after={after} />;
  }
  const beforeStrs = before.map(renderItem);
  const afterStrs = after.map(renderItem);
  const beforeSet = new Set(beforeStrs);
  const afterSet = new Set(afterStrs);
  const added = afterStrs.filter((s) => !beforeSet.has(s));
  const removed = beforeStrs.filter((s) => !afterSet.has(s));
  return (
    <div data-testid="list-diff" data-field={field} className="text-sm">
      <span className="font-mono text-neutral-500">{field}</span>
      <ul className="mt-1 ml-4 space-y-0.5">
        {added.map((item, idx) => (
          <li
            key={`+${idx}-${item}`}
            data-testid="list-diff-added"
            className="rounded bg-green-50 px-1 font-mono text-xs text-green-700"
          >
            + {item}
          </li>
        ))}
        {removed.map((item, idx) => (
          <li
            key={`-${idx}-${item}`}
            data-testid="list-diff-removed"
            className="rounded bg-red-50 px-1 font-mono text-xs text-red-700 line-through"
          >
            - {item}
          </li>
        ))}
      </ul>
    </div>
  );
}
