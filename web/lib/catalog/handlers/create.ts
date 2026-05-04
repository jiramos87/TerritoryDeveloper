/**
 * Catalog create handler — kind-dispatched wrapper over per-kind spine repos.
 * Capability check + slug-regex gate delegated to each repo (DEC-A24).
 */

import type { Sql } from "postgres";
import type { CatalogKind } from "@/lib/refs/types";
import { createSprite, type CreateSpriteBody } from "@/lib/db/sprite-repo";
import { createAssetSpine, type CreateAssetSpineBody } from "@/lib/catalog/asset-spine-repo";
import { createButtonSpine } from "@/lib/catalog/button-spine-repo";
import { createPanelSpine } from "@/lib/catalog/panel-spine-repo";
import { createTokenSpine } from "@/lib/catalog/token-spine-repo";
import { createArchetype, type CreateArchetypeBody } from "@/lib/catalog/archetype-repo";
import { createPoolSpine } from "@/lib/catalog/pool-spine-repo";
import type {
  CatalogButtonCreateBody,
  CatalogPanelCreateBody,
  CatalogPoolCreateBody,
  CatalogTokenCreateBody,
} from "@/types/api/catalog-api";

export type CreateResult =
  | { ok: "ok"; data: { entity_id: string; slug: string } }
  | { ok: "conflict"; reason: string }
  | { ok: "validation"; reason: string }
  | { ok: "notfound" };

export async function createCatalogEntity(
  kind: CatalogKind,
  body: Record<string, unknown>,
  sql: Sql,
): Promise<CreateResult> {
  switch (kind) {
    case "sprite":
      return createSprite(body as CreateSpriteBody, sql);
    case "asset":
      return createAssetSpine(body as CreateAssetSpineBody, sql);
    case "button":
      return createButtonSpine(body as unknown as CatalogButtonCreateBody, sql);
    case "panel":
      return createPanelSpine(body as unknown as CatalogPanelCreateBody, sql);
    case "token":
      return createTokenSpine(body as unknown as CatalogTokenCreateBody, sql);
    case "archetype":
      return createArchetype(body as CreateArchetypeBody, sql);
    case "pool":
      return createPoolSpine(body as unknown as CatalogPoolCreateBody, sql);
    case "audio":
      return { ok: "validation", reason: "audio create not supported via generic handler; use /api/catalog/audio" };
    default: {
      const _exhaustive: never = kind;
      void _exhaustive;
      return { ok: "validation", reason: "unknown kind" };
    }
  }
}
