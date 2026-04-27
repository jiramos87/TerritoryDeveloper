/**
 * Archetype slots schema loader (TECH-1887 / Stage 8.1).
 *
 * Panel archetypes (kind=archetype) declare a `slots_schema` blob on
 * `entity_version.params_json`. This helper fetches the published version's
 * `slots_schema` for a given archetype `catalog_entity.id`. Returns null when
 * the archetype has no published version (DEC-A22 lenient) or when the schema
 * key is absent.
 *
 * Schema shape (DEC-A27):
 *   {
 *     [slot_name: string]: {
 *       accepts_kind: string[];   // child_kind values allowed in this slot
 *       min?: number;             // default 0
 *       max?: number;             // default unbounded
 *     }
 *   }
 *
 * @see ia/projects/asset-pipeline (DB) — TECH-1887 §Plan Digest
 */
import type { Sql } from "postgres";
import type { CatalogPanelSlotSchemaEntry } from "@/types/api/catalog-api";

export type PanelSlotsSchema = Record<string, CatalogPanelSlotSchemaEntry>;

function isSchemaEntry(v: unknown): v is CatalogPanelSlotSchemaEntry {
  if (typeof v !== "object" || v === null) return false;
  const r = v as Record<string, unknown>;
  if (!Array.isArray(r.accepts_kind)) return false;
  if (!r.accepts_kind.every((k) => typeof k === "string")) return false;
  if (r.min !== undefined && typeof r.min !== "number") return false;
  if (r.max !== undefined && typeof r.max !== "number") return false;
  return true;
}

/** Coerce arbitrary jsonb into a validated PanelSlotsSchema or null. */
export function coerceSlotsSchema(raw: unknown): PanelSlotsSchema | null {
  if (typeof raw !== "object" || raw === null || Array.isArray(raw)) return null;
  const out: PanelSlotsSchema = {};
  let any = false;
  for (const [k, v] of Object.entries(raw as Record<string, unknown>)) {
    if (!isSchemaEntry(v)) continue;
    out[k] = {
      accepts_kind: v.accepts_kind,
      ...(v.min !== undefined ? { min: v.min } : {}),
      ...(v.max !== undefined ? { max: v.max } : {}),
    };
    any = true;
  }
  return any ? out : null;
}

/**
 * Fetch the `slots_schema` for the archetype's currently-published version.
 * Returns null when archetype has no published version, or when the schema
 * key is missing/malformed.
 */
export async function getArchetypeSlotsSchema(
  archetypeEntityId: number,
  sql: Sql,
): Promise<PanelSlotsSchema | null> {
  const rows = (await sql`
    select ev.params_json
      from catalog_entity e
      join entity_version ev on ev.id = e.current_published_version_id
     where e.id = ${archetypeEntityId} and e.kind = 'archetype'
     limit 1
  `) as unknown as Array<{ params_json: Record<string, unknown> }>;
  if (rows.length === 0) return null;
  const params = rows[0]!.params_json ?? {};
  const raw = (params as Record<string, unknown>).slots_schema;
  return coerceSlotsSchema(raw);
}
