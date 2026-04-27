/**
 * Panel slots schema helpers (TECH-1886 / Stage 8.1).
 *
 * The archetype's pinned `entity_version.params_json.slots_schema` is shipped
 * to the client embedded in `CatalogPanelDto.panel_detail.slots_schema`. UI
 * code parses + validates that shape via this module before rendering slot
 * columns.
 *
 * @see ia/projects/asset-pipeline (DB) — TECH-1886 §Plan Digest
 */
import type { CatalogPanelSlotSchemaEntry } from "@/types/api/catalog-api";

export type PanelSlotsSchema = Record<string, CatalogPanelSlotSchemaEntry>;

/**
 * Validate raw jsonb shape into a typed `PanelSlotsSchema`. Returns null when
 * input is missing / wrong shape (lenient — caller treats null as "free-form
 * panel" and renders only slots that actually have children).
 */
export function parseSlotsSchema(raw: unknown): PanelSlotsSchema | null {
  if (raw == null || typeof raw !== "object" || Array.isArray(raw)) return null;
  const out: PanelSlotsSchema = {};
  for (const [name, entry] of Object.entries(raw as Record<string, unknown>)) {
    if (entry == null || typeof entry !== "object" || Array.isArray(entry)) return null;
    const e = entry as Record<string, unknown>;
    if (!Array.isArray(e.accepts_kind)) return null;
    const accepts = e.accepts_kind.filter((s): s is string => typeof s === "string");
    if (accepts.length === 0) return null;
    const min = typeof e.min === "number" ? e.min : undefined;
    const max = typeof e.max === "number" ? e.max : undefined;
    out[name] = { accepts_kind: accepts, min, max };
  }
  return out;
}

/**
 * Walk slot order: archetype-declared slot names first, then any extra slot
 * names present in DTO children that are not in the schema.
 */
export function slotOrder(
  schema: PanelSlotsSchema | null,
  childSlotNames: string[],
): string[] {
  const out: string[] = [];
  const seen = new Set<string>();
  if (schema != null) {
    for (const name of Object.keys(schema)) {
      if (!seen.has(name)) {
        out.push(name);
        seen.add(name);
      }
    }
  }
  const extras = [...new Set(childSlotNames)].sort();
  for (const name of extras) {
    if (!seen.has(name)) {
      out.push(name);
      seen.add(name);
    }
  }
  return out;
}
