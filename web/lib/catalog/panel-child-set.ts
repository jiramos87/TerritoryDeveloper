/**
 * `setPanelChildTree` — atomic replace-tree handler (TECH-1887 / Stage 8.1).
 *
 * Per DEC-A43: replace-tree pattern (delete-all-then-insert) inside a single
 * SERIALIZABLE tx. Validators run before the delete; on validation failure
 * caller returns DEC-A48 error envelope.
 *
 * Concurrency: caller holds the panel `catalog_entity` row via `for update`
 * lock (caller passes the locked sql tx). Optimistic-lock fingerprint
 * (`expected_updated_at`) compared against panel `catalog_entity.updated_at`.
 *
 * Publish path (DEC-A22): when `body.publish === true`, snapshot
 * `child_version_id` from each child entity's `current_published_version_id`
 * (NULL is preserved when child has no published version — lenient).
 *
 * @see ia/projects/asset-pipeline (DB) — TECH-1887 §Plan Digest
 */
import type { Sql } from "postgres";
import type { CatalogPanelChildSetBody } from "@/types/api/catalog-api";
import {
  resolveAndCheckKinds,
  validateAgainstSchema,
  validateNoPanelCycle,
  type PanelValidationError,
} from "./panel-child-validators";
import { getArchetypeSlotsSchema } from "./panel-archetype-slots";

export type SetPanelChildTreeResult =
  | { ok: "ok"; rows_written: number; updated_at: string }
  | { ok: "validation"; error: PanelValidationError }
  | { ok: "stale"; current_updated_at: string }
  | { ok: "notfound" };

export async function setPanelChildTree(
  panelEntityId: number,
  body: CatalogPanelChildSetBody,
  sql: Sql,
): Promise<SetPanelChildTreeResult> {
  // Lock panel row + verify fingerprint. Postgres cannot apply FOR UPDATE to
  // the nullable side of an outer join, so lock `catalog_entity` first then
  // read `panel_detail` separately.
  const lockRows = (await sql`
    select id, updated_at
      from catalog_entity
     where id = ${panelEntityId} and kind = 'panel'
     for update
  `) as unknown as Array<{ id: string; updated_at: string }>;
  if (lockRows.length === 0) return { ok: "notfound" };
  const cur = lockRows[0]!;
  if (
    new Date(cur.updated_at).toISOString() !== new Date(body.updated_at).toISOString()
  ) {
    return { ok: "stale", current_updated_at: cur.updated_at };
  }
  const detailRows = (await sql`
    select archetype_entity_id::text as archetype_entity_id
      from panel_detail where entity_id = ${panelEntityId}
  `) as unknown as Array<{ archetype_entity_id: string | null }>;
  const archetypeIdStr = detailRows[0]?.archetype_entity_id ?? null;

  // Resolve archetype slots schema (null when no archetype bound or no published version).
  const archetypeId = archetypeIdStr != null ? Number.parseInt(archetypeIdStr, 10) : null;
  const slotsSchema = archetypeId != null ? await getArchetypeSlotsSchema(archetypeId, sql) : null;

  // Validator chain: kind-accepts → count → cycle.
  const schemaCheck = validateAgainstSchema(body, slotsSchema);
  if (!schemaCheck.ok) return { ok: "validation", error: schemaCheck.error };

  const resolved = await resolveAndCheckKinds(body, sql);
  if (!resolved.ok) return { ok: "validation", error: resolved.error };

  const childPanels: Array<{ child_entity_id: number; slot_name: string }> = [];
  for (const r of resolved.resolved) {
    if (r.child_kind === "panel" && r.child_entity_id != null) {
      childPanels.push({ child_entity_id: r.child_entity_id, slot_name: r.slot_name });
    }
  }
  const cycleCheck = await validateNoPanelCycle(panelEntityId, childPanels, sql);
  if (!cycleCheck.ok) return { ok: "validation", error: cycleCheck.error };

  // Optional publish path: pre-fetch each child's current_published_version_id.
  let publishedMap: Map<number, string | null> = new Map();
  if (body.publish === true) {
    const ids = Array.from(
      new Set(
        resolved.resolved
          .map((r) => r.child_entity_id)
          .filter((v): v is number => typeof v === "number"),
      ),
    );
    if (ids.length > 0) {
      const rows = (await sql`
        select id, current_published_version_id::text as cpv
          from catalog_entity
         where id = any(${ids}::bigint[])
      `) as unknown as Array<{ id: string; cpv: string | null }>;
      publishedMap = new Map(
        rows.map((r) => [Number.parseInt(r.id as unknown as string, 10), r.cpv]),
      );
    }
  }

  // Atomic replace: delete then insert.
  await sql`delete from panel_child where panel_entity_id = ${panelEntityId}`;

  let rowsWritten = 0;
  for (const slot of body.slots) {
    for (const c of slot.children) {
      const childIdStr = c.child_entity_id ?? null;
      const childIdNum =
        childIdStr != null && childIdStr !== "" && /^\d+$/.test(childIdStr)
          ? Number.parseInt(childIdStr, 10)
          : null;
      const callerParams = (c.params_json ?? {}) as Record<string, unknown>;
      // Trigger `panel_child_params_json_lint` (mig 0063) requires `kind`
      // discriminator. Inject from `child_kind` (canonical — overrides caller).
      const params = { ...callerParams, kind: c.child_kind } as Parameters<typeof sql.json>[0];
      const childVersionId =
        body.publish === true && childIdNum != null
          ? publishedMap.get(childIdNum) ?? null
          : null;
      const childVersionIdNum =
        childVersionId != null && /^\d+$/.test(childVersionId)
          ? Number.parseInt(childVersionId, 10)
          : null;
      await sql`
        insert into panel_child (
          panel_entity_id, panel_version_id, slot_name, order_idx,
          child_kind, child_entity_id, child_version_id, params_json
        ) values (
          ${panelEntityId}, ${null}, ${slot.name}, ${c.order_idx},
          ${c.child_kind}, ${childIdNum}, ${childVersionIdNum}, ${sql.json(params)}
        )
      `;
      rowsWritten++;
    }
  }

  // Touch panel `catalog_entity.updated_at` so subsequent reads see fresh fingerprint.
  const touched = (await sql`
    update catalog_entity set updated_at = now() where id = ${panelEntityId}
    returning updated_at
  `) as unknown as Array<{ updated_at: string }>;
  return { ok: "ok", rows_written: rowsWritten, updated_at: touched[0]!.updated_at };
}
