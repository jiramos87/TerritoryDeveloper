/**
 * Catalog get-by-slug handler — kind-dispatched wrapper over per-kind spine repos.
 */

import type { CatalogKind } from "@/lib/refs/types";
import { getSpriteBySlug } from "@/lib/db/sprite-repo";
import { getAssetSpineBySlug } from "@/lib/catalog/asset-spine-repo";
import { getButtonSpineBySlug } from "@/lib/catalog/button-spine-repo";
import { getPanelSpineBySlug } from "@/lib/catalog/panel-spine-repo";
import { getAudioBySlug } from "@/lib/db/audio-repo";
import { getPoolSpineBySlug } from "@/lib/catalog/pool-spine-repo";
import { getTokenSpineBySlug } from "@/lib/catalog/token-spine-repo";
import { getArchetypeBySlug } from "@/lib/catalog/archetype-repo";

export async function getCatalogEntity(
  kind: CatalogKind,
  slug: string,
): Promise<unknown | null> {
  switch (kind) {
    case "sprite":
      return getSpriteBySlug(slug);
    case "asset":
      return getAssetSpineBySlug(slug);
    case "button":
      return getButtonSpineBySlug(slug);
    case "panel":
      return getPanelSpineBySlug(slug);
    case "audio":
      return getAudioBySlug(slug);
    case "pool":
      return getPoolSpineBySlug(slug);
    case "token":
      return getTokenSpineBySlug(slug);
    case "archetype":
      return getArchetypeBySlug(slug);
    default: {
      const _exhaustive: never = kind;
      void _exhaustive;
      return null;
    }
  }
}
