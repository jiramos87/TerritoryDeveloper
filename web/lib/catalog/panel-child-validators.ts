/**
 * Panel child save validators (TECH-1887 / Stage 8.1).
 *
 * Save-time gates per DEC-A27:
 *   - validateKindAccepts: child_kind must appear in slot.accepts_kind
 *   - validateSlotCount:   slot child count must be in [min, max] (defaults 0..∞)
 *   - validateNoPanelCycle: BFS through child_kind='panel' edges; if any
 *                           descendant entity_id == panel_entity_id, reject.
 *
 * Validators return discriminated union; first-error short-circuit at the
 * call site. Cycle BFS iteratively walks `panel_child` rows to avoid
 * recursion-stack risk; visited-set prevents cycles in shared subtrees from
 * blowing up.
 *
 * @see ia/projects/asset-pipeline (DB) — TECH-1887 §Plan Digest
 */
import type { Sql } from "postgres";
import type {
  CatalogPanelChildKind,
  CatalogPanelChildSetBody,
  CatalogPanelSlotSchemaEntry,
} from "@/types/api/catalog-api";
import type { PanelSlotsSchema } from "./panel-archetype-slots";

export type PanelValidationError =
  | {
      code: "kind_not_accepted";
      details: {
        slot_name: string;
        child_id: string | null;
        child_kind: CatalogPanelChildKind;
        accepts: string[];
      };
    }
  | {
      code: "slot_count_out_of_range";
      details: { slot_name: string; count: number; min: number; max: number | null };
    }
  | {
      code: "panel_cycle_detected";
      details: { cycle_path: string[] };
    }
  | {
      code: "unknown_slot";
      details: { slot_name: string; declared: string[] };
    };

export type PanelValidationResult =
  | { ok: true }
  | { ok: false; error: PanelValidationError };

/**
 * Resolve `child_kind` per child entry. When a slot child supplies
 * `child_entity_id`, the kind must equal `catalog_entity.kind` of that row;
 * `child_kind` (declared on the body) is the source of truth and gets
 * cross-checked against entity row when present.
 *
 * Returns a flat list of resolved children (one per body row) including the
 * resolved kind from DB lookup so the cycle detector can downstream-check
 * 'panel' edges without re-querying.
 */
export type ResolvedChild = {
  slot_name: string;
  order_idx: number;
  child_kind: CatalogPanelChildKind;
  child_entity_id: number | null;
};

/**
 * For each body row that supplies `child_entity_id`, fetch `kind` from
 * `catalog_entity` and assert it matches the declared `child_kind`. Throws
 * on the first mismatch (caller maps to validation error).
 */
export async function resolveAndCheckKinds(
  body: CatalogPanelChildSetBody,
  sql: Sql,
): Promise<{ ok: true; resolved: ResolvedChild[] } | { ok: false; error: PanelValidationError }> {
  const resolved: ResolvedChild[] = [];
  // Collect numeric child entity ids for one batch query.
  const numericIds: number[] = [];
  for (const slot of body.slots) {
    for (const c of slot.children) {
      if (c.child_entity_id != null && c.child_entity_id !== "" && /^\d+$/.test(c.child_entity_id)) {
        numericIds.push(Number.parseInt(c.child_entity_id, 10));
      }
    }
  }
  const kindMap = new Map<number, string>();
  if (numericIds.length > 0) {
    const rows = (await sql`
      select id, kind from catalog_entity where id = any(${numericIds}::bigint[])
    `) as unknown as Array<{ id: string; kind: string }>;
    for (const r of rows) kindMap.set(Number.parseInt(r.id as unknown as string, 10), r.kind);
  }
  for (const slot of body.slots) {
    for (const c of slot.children) {
      const idStr = c.child_entity_id ?? null;
      const idNum =
        idStr != null && idStr !== "" && /^\d+$/.test(idStr) ? Number.parseInt(idStr, 10) : null;
      // When entity_id present, declared child_kind must match DB row kind.
      // Two leaf kinds (spacer, label_inline) have no entity row — entity_id null OK.
      if (idNum != null) {
        const dbKind = kindMap.get(idNum);
        if (dbKind == null) {
          // Body referenced an entity_id that does not exist.
          return {
            ok: false,
            error: {
              code: "kind_not_accepted",
              details: {
                slot_name: slot.name,
                child_id: idStr,
                child_kind: c.child_kind,
                accepts: [],
              },
            },
          };
        }
        // Allow declared child_kind === dbKind; reject mismatch.
        if (dbKind !== c.child_kind) {
          return {
            ok: false,
            error: {
              code: "kind_not_accepted",
              details: {
                slot_name: slot.name,
                child_id: idStr,
                child_kind: c.child_kind,
                accepts: [dbKind],
              },
            },
          };
        }
      }
      resolved.push({
        slot_name: slot.name,
        order_idx: c.order_idx,
        child_kind: c.child_kind,
        child_entity_id: idNum,
      });
    }
  }
  return { ok: true, resolved };
}

/**
 * For each declared slot, check (a) the slot is declared in archetype schema,
 * (b) every child's child_kind ∈ slot.accepts_kind, (c) child count ∈ [min,max].
 *
 * When `slotsSchema` is null (no archetype bound), accepts/count checks are
 * skipped — all kinds + counts allowed (treat as free-form panel).
 */
export function validateAgainstSchema(
  body: CatalogPanelChildSetBody,
  slotsSchema: PanelSlotsSchema | null,
): PanelValidationResult {
  if (slotsSchema == null) return { ok: true };
  const declared = Object.keys(slotsSchema);
  for (const slot of body.slots) {
    const schema: CatalogPanelSlotSchemaEntry | undefined = slotsSchema[slot.name];
    if (schema == null) {
      return {
        ok: false,
        error: { code: "unknown_slot", details: { slot_name: slot.name, declared } },
      };
    }
    // Kind-accepts check.
    for (const c of slot.children) {
      if (!schema.accepts_kind.includes(c.child_kind)) {
        return {
          ok: false,
          error: {
            code: "kind_not_accepted",
            details: {
              slot_name: slot.name,
              child_id: c.child_entity_id ?? null,
              child_kind: c.child_kind,
              accepts: schema.accepts_kind,
            },
          },
        };
      }
    }
    // Count-in-range check.
    const count = slot.children.length;
    const min = typeof schema.min === "number" ? schema.min : 0;
    const max = typeof schema.max === "number" ? schema.max : null;
    if (count < min || (max != null && count > max)) {
      return {
        ok: false,
        error: {
          code: "slot_count_out_of_range",
          details: { slot_name: slot.name, count, min, max },
        },
      };
    }
  }
  return { ok: true };
}

/**
 * BFS through descendant panels: starting from the candidate-edges
 * (panel_entity_id → child panel ids in body), descend through existing
 * `panel_child` rows where `child_kind='panel'`. If any descendant equals
 * `panel_entity_id`, return the cycle path.
 *
 * `panel_entity_id` is the panel being saved; child panels declared in body
 * are the candidate first-hop edges (since the body has not been written yet).
 */
export async function validateNoPanelCycle(
  panelEntityId: number,
  bodyChildPanels: Array<{ child_entity_id: number; slot_name: string }>,
  sql: Sql,
): Promise<PanelValidationResult> {
  if (bodyChildPanels.length === 0) return { ok: true };
  // Pre-flight: child cannot be self.
  for (const c of bodyChildPanels) {
    if (c.child_entity_id === panelEntityId) {
      return {
        ok: false,
        error: {
          code: "panel_cycle_detected",
          details: { cycle_path: [String(panelEntityId), String(panelEntityId)] },
        },
      };
    }
  }

  const visited = new Set<number>([panelEntityId]);
  const queue: Array<{ id: number; path: number[] }> = bodyChildPanels.map((c) => ({
    id: c.child_entity_id,
    path: [panelEntityId, c.child_entity_id],
  }));

  while (queue.length > 0) {
    const cur = queue.shift()!;
    if (cur.id === panelEntityId) {
      return {
        ok: false,
        error: {
          code: "panel_cycle_detected",
          details: { cycle_path: cur.path.map(String) },
        },
      };
    }
    if (visited.has(cur.id)) continue;
    visited.add(cur.id);

    // Fetch this panel's own existing panel-kind children from DB.
    const rows = (await sql`
      select child_entity_id::int as child_id
        from panel_child
       where panel_entity_id = ${cur.id}
         and child_kind = 'panel'
         and child_entity_id is not null
    `) as unknown as Array<{ child_id: number }>;

    for (const r of rows) {
      if (r.child_id === panelEntityId) {
        return {
          ok: false,
          error: {
            code: "panel_cycle_detected",
            details: { cycle_path: [...cur.path, panelEntityId].map(String) },
          },
        };
      }
      if (!visited.has(r.child_id)) {
        queue.push({ id: r.child_id, path: [...cur.path, r.child_id] });
      }
    }
  }

  return { ok: true };
}
