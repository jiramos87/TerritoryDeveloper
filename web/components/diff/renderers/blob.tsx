/**
 * `BlobFieldDiff` — image / audio path side-by-side stub (TECH-3302 / Stage 14.3).
 *
 * Renders both blob refs as text placeholders. Actual image-diff / audio-diff
 * stays out of Stage 14.3 scope per objective ("blobs as image-diff stub").
 *
 * @see ia/projects/asset-pipeline/stage-14.3 — TECH-3302 §Plan Digest
 */
"use client";

export interface BlobFieldDiffProps {
  field: string;
  before: unknown;
  after: unknown;
}

function renderRef(v: unknown): string {
  if (v == null) return "(none)";
  if (typeof v === "string") return v;
  return JSON.stringify(v);
}

export default function BlobFieldDiff({ field, before, after }: BlobFieldDiffProps) {
  return (
    <div data-testid="blob-diff" data-field={field} className="text-sm">
      <span className="font-mono text-neutral-500">{field}</span>
      <div className="mt-1 grid grid-cols-2 gap-2">
        <div
          data-testid="blob-diff-before"
          className="rounded border border-red-200 bg-red-50 p-2 font-mono text-xs text-red-700"
        >
          before: {renderRef(before)}
        </div>
        <div
          data-testid="blob-diff-after"
          className="rounded border border-green-200 bg-green-50 p-2 font-mono text-xs text-green-700"
        >
          after: {renderRef(after)}
        </div>
      </div>
    </div>
  );
}
