/**
 * `ScalarFieldDiff` — primitive before/after pair (TECH-3302 / Stage 14.3).
 *
 * Renders two cells: `before` with red strikethrough, `after` with green
 * highlight. Tailwind palette mirrors `web/components/versions/VersionsTabView.tsx`
 * status badges. Handles `null` / `undefined` as `(none)` placeholder.
 *
 * Pure render component — no DB / fetch / hooks.
 *
 * @see ia/projects/asset-pipeline/stage-14.3 — TECH-3302 §Plan Digest
 */
"use client";

export interface ScalarFieldDiffProps {
  field: string;
  before: unknown;
  after: unknown;
}

function renderValue(v: unknown): string {
  if (v == null) return "(none)";
  if (typeof v === "string") return v;
  if (typeof v === "number" || typeof v === "boolean") return String(v);
  return JSON.stringify(v);
}

export default function ScalarFieldDiff({
  field,
  before,
  after,
}: ScalarFieldDiffProps) {
  return (
    <div
      data-testid="scalar-diff"
      data-field={field}
      className="flex items-baseline gap-2 text-sm"
    >
      <span className="font-mono text-neutral-500">{field}</span>
      <span
        data-testid="scalar-diff-before"
        className="rounded bg-red-50 px-1 text-red-700 line-through"
      >
        {renderValue(before)}
      </span>
      <span aria-hidden className="text-neutral-400">
        →
      </span>
      <span
        data-testid="scalar-diff-after"
        className="rounded bg-green-50 px-1 text-green-700"
      >
        {renderValue(after)}
      </span>
    </div>
  );
}
