/**
 * `AudioDiff` — audio-kind typed diff renderer (TECH-3304 / Stage 14.3).
 *
 * Routes changed fields via shared `RouteByHint` helper using hints from
 * `kind-schemas.ts` (`audio_path` / `waveform_path` -> blob, `tags` /
 * `variants` -> list). No kind-specific override.
 *
 * @see ia/projects/asset-pipeline/stage-14.3 — TECH-3304 §Plan Digest
 */
"use client";

import type { KindDiff } from "@/lib/diff/kind-schemas";

import AddedRemovedBlock from "./_added-removed";
import RouteByHint from "./_route-by-hint";

export interface AudioDiffProps {
  diff: KindDiff;
}

export default function AudioDiff({ diff }: AudioDiffProps) {
  return (
    <div data-testid="audio-renderer" className="space-y-3">
      <AddedRemovedBlock added={diff.added} removed={diff.removed} />
      <RouteByHint changes={diff.changed} />
    </div>
  );
}
