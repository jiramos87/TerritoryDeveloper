/**
 * Catalog update handler — kind-dispatched wrapper over per-kind patch repos.
 * Optimistic concurrency via updated_at fingerprint (DEC-A38).
 */

import type { Sql } from "postgres";
import type { CatalogKind } from "@/lib/refs/types";
import { patchSprite, type PatchSpriteBody } from "@/lib/db/sprite-repo";
import { patchAssetSpine } from "@/lib/catalog/asset-spine-repo";
import { patchButtonSpine } from "@/lib/catalog/button-spine-repo";
import { patchPanelSpine, type CatalogPanelPatchBody } from "@/lib/catalog/panel-spine-repo";
import { patchTokenSpine } from "@/lib/catalog/token-spine-repo";
import { patchArchetype, type PatchArchetypeBody } from "@/lib/catalog/archetype-repo";
import { patchPoolSpine } from "@/lib/catalog/pool-spine-repo";
import type {
  CatalogAssetSpinePatchBody,
  CatalogButtonPatchBody,
  CatalogPoolPatchBody,
  CatalogTokenPatchBody,
} from "@/types/api/catalog-api";

export type UpdateResult<T = unknown> =
  | { ok: "ok"; data: T }
  | { ok: "notfound" }
  | { ok: "conflict"; reason: string; current?: T }
  | { ok: "validation"; reason: string };

export async function updateCatalogEntity(
  kind: CatalogKind,
  slug: string,
  body: Record<string, unknown>,
  sql: Sql,
): Promise<UpdateResult> {
  switch (kind) {
    case "sprite":
      return patchSprite(slug, body as PatchSpriteBody, sql) as Promise<UpdateResult>;
    case "asset":
      return patchAssetSpine(slug, body as CatalogAssetSpinePatchBody, sql) as Promise<UpdateResult>;
    case "button":
      return patchButtonSpine(slug, body as CatalogButtonPatchBody, sql) as Promise<UpdateResult>;
    case "panel":
      return patchPanelSpine(slug, body as CatalogPanelPatchBody, sql) as Promise<UpdateResult>;
    case "token":
      return patchTokenSpine(slug, body as CatalogTokenPatchBody, sql) as Promise<UpdateResult>;
    case "archetype":
      return patchArchetype(slug, body as PatchArchetypeBody, sql) as Promise<UpdateResult>;
    case "pool":
      return patchPoolSpine(slug, body as CatalogPoolPatchBody, sql) as Promise<UpdateResult>;
    case "audio":
      return { ok: "validation", reason: "audio update not supported via generic handler; use /api/catalog/audio/[slug]" };
    default: {
      const _exhaustive: never = kind;
      void _exhaustive;
      return { ok: "validation", reason: "unknown kind" };
    }
  }
}
