/**
 * Catalog list handler — kind-dispatched wrapper over per-kind spine repos.
 * REST routes and agent callers use this instead of importing repos directly.
 */

import type { CatalogKind } from "@/lib/refs/types";
import { listSprites, type SpriteListFilter } from "@/lib/db/sprite-repo";
import { listAssetsSpine } from "@/lib/catalog/asset-spine-repo";
import { listButtonsSpine } from "@/lib/catalog/button-spine-repo";
import { listPanelsSpine } from "@/lib/catalog/panel-spine-repo";
import { listAudioEntities } from "@/lib/db/audio-repo";
import { listPoolsSpine } from "@/lib/catalog/pool-spine-repo";
import { listTokensSpine } from "@/lib/catalog/token-spine-repo";
import { listArchetypes } from "@/lib/catalog/archetype-repo";

export type CatalogListFilter = "active" | "retired" | "all";

export interface CatalogListInput {
  filter?: CatalogListFilter;
  limit?: number;
  cursor?: string | null;
}

export interface CatalogListOutput {
  items: unknown[];
  next_cursor: string | null;
}

export async function listCatalogEntities(
  kind: CatalogKind,
  input: CatalogListInput,
): Promise<CatalogListOutput> {
  const filter = (input.filter ?? "active") as SpriteListFilter;
  const limit = input.limit ?? 50;
  const cursor = input.cursor ?? null;
  const opts = { filter, limit, cursor };

  switch (kind) {
    case "sprite":
      return listSprites(opts);
    case "asset":
      return listAssetsSpine(opts);
    case "button":
      return listButtonsSpine(opts);
    case "panel":
      return listPanelsSpine(opts);
    case "audio":
      return listAudioEntities(opts);
    case "pool":
      return listPoolsSpine(opts);
    case "token":
      return listTokensSpine(opts);
    case "archetype":
      return listArchetypes(opts);
    default: {
      const _exhaustive: never = kind;
      void _exhaustive;
      return { items: [], next_cursor: null };
    }
  }
}
