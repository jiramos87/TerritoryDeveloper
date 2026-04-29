/**
 * `EntityVersionDiff` — kind-dispatched typed diff view (TECH-3302 → TECH-3304 / Stage 14.3).
 *
 * Renders one of 8 per-kind sub-renderers based on `kind` prop. All branches
 * wired (T14.3.4 + T14.3.5). Default branch = TypeScript exhaustiveness check
 * (`never`) so future kind additions surface as compile errors.
 *
 * Pure render component — props-driven, no DB / fetch / hooks.
 *
 * @see ia/projects/asset-pipeline/stage-14.3 — TECH-3304 §Plan Digest
 */
"use client";

import type { CatalogKind } from "@/lib/refs/types";
import type { KindDiff } from "@/lib/diff/kind-schemas";

import ArchetypeDiff from "./renderers/archetype";
import AssetDiff from "./renderers/asset";
import AudioDiff from "./renderers/audio";
import ButtonDiff from "./renderers/button";
import PanelDiff from "./renderers/panel";
import PoolDiff from "./renderers/pool";
import SpriteDiff from "./renderers/sprite";
import TokenDiff from "./renderers/token";

export interface EntityVersionDiffProps {
  kind: CatalogKind;
  diff: KindDiff;
}

export default function EntityVersionDiff({ kind, diff }: EntityVersionDiffProps) {
  switch (kind) {
    case "sprite":
      return <SpriteDiff diff={diff} />;
    case "asset":
      return <AssetDiff diff={diff} />;
    case "button":
      return <ButtonDiff diff={diff} />;
    case "token":
      return <TokenDiff diff={diff} />;
    case "panel":
      return <PanelDiff diff={diff} />;
    case "pool":
      return <PoolDiff diff={diff} />;
    case "archetype":
      return <ArchetypeDiff diff={diff} />;
    case "audio":
      return <AudioDiff diff={diff} />;
    default: {
      const _exhaustive: never = kind;
      throw new Error(`EntityVersionDiff: unhandled kind ${String(_exhaustive)}`);
    }
  }
}
