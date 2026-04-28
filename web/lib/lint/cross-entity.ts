/**
 * Layer 2 cross-entity lint runner (TECH-2569 / Stage 12.1).
 *
 * Resolves cross-entity refs per kind:
 *   - panel  → reads `panel_child` rows + verifies each `child_version_id`
 *              points at a non-retired `entity_version` (DEC-A23 retire model).
 *   - button → reads `button_detail` slot fields (icon sprite, label token,
 *              etc.) + verifies each resolves.
 *   - archetype → stub `[]` (param-ref walk lands when `params_schema`
 *                  introspection comes online).
 *   - sprite | asset | audio | token → orphan-only path (no inbound refs across
 *                  published versions → `warn` row).
 *   - pool → stub `[]` (cross-entity check deferred to Stage 14.1 alongside
 *            `catalog_ref_edge` materialization).
 *
 * `aggregateLintResults(layer1, layer2)` is a pure transform (no IO) consumed
 * by the publish API (TECH-2571) + `PublishDialog` (TECH-2570).
 *
 * @see ia/projects/asset-pipeline/stage-12.1 — TECH-2569 §Plan Digest
 * @see web/lib/lint/runner.ts — Layer 1 sibling runner
 */

import type { Sql } from "postgres";

import type { LintResult } from "@/lib/lint/types";

const ORPHAN_KINDS: ReadonlySet<string> = new Set([
  "sprite",
  "asset",
  "button",
  "panel",
  "token",
  "audio",
]);

/**
 * Verify that `entityId` points at a non-retired row in `catalog_entity`.
 * Retired entity (per DEC-A23) counts as unresolved — publish must fail.
 */
async function entityResolves(
  entityId: string | number,
  sql: Sql,
): Promise<boolean> {
  const rows = await sql<Array<{ id: number }>>`
    select id
    from catalog_entity
    where id = ${entityId}
      and retired_at is null
    limit 1
  `;
  return rows.length > 0;
}

/**
 * Panel ref resolver — walks `panel_child` rows for the panel + flags any
 * `child_entity_id` that fails to resolve. Returns `block` rows tagged
 * `panel.unresolved_ref`. NULL `child_entity_id` is allowed (spacer /
 * label_inline kinds per DEC-A27) — skip those.
 */
async function auditPanelRefs(
  panelEntityId: string,
  sql: Sql,
): Promise<LintResult[]> {
  type Row = {
    id: number;
    slot_name: string;
    order_idx: number;
    child_kind: string;
    child_entity_id: number | null;
  };
  const rows = await sql<Row[]>`
    select id, slot_name, order_idx, child_kind, child_entity_id
    from panel_child
    where panel_entity_id = ${panelEntityId}
  `;
  const out: LintResult[] = [];
  for (const row of rows) {
    if (row.child_entity_id === null) continue; // spacer / label_inline
    const ok = await entityResolves(row.child_entity_id, sql);
    if (!ok) {
      out.push({
        rule_id: "panel.unresolved_ref",
        severity: "block",
        message: `panel slot ${row.slot_name}[${row.order_idx}] (${row.child_kind}) → entity ${row.child_entity_id} unresolved or retired`,
      });
    }
  }
  return out;
}

/**
 * Button ref resolver — verifies each non-NULL slot id (6 sprite slots +
 * 4 token slots per DEC-A7) resolves to a non-retired entity. Each unresolved
 * slot emits one `block` row.
 */
async function auditButtonRefs(
  buttonEntityId: string,
  sql: Sql,
): Promise<LintResult[]> {
  type Row = {
    sprite_idle_entity_id: number | null;
    sprite_hover_entity_id: number | null;
    sprite_pressed_entity_id: number | null;
    sprite_disabled_entity_id: number | null;
    sprite_icon_entity_id: number | null;
    sprite_badge_entity_id: number | null;
    token_palette_entity_id: number | null;
    token_frame_style_entity_id: number | null;
    token_font_entity_id: number | null;
    token_illumination_entity_id: number | null;
  };
  const rows = await sql<Row[]>`
    select sprite_idle_entity_id, sprite_hover_entity_id,
           sprite_pressed_entity_id, sprite_disabled_entity_id,
           sprite_icon_entity_id, sprite_badge_entity_id,
           token_palette_entity_id, token_frame_style_entity_id,
           token_font_entity_id, token_illumination_entity_id
    from button_detail
    where entity_id = ${buttonEntityId}
    limit 1
  `;
  if (rows.length === 0) return [];
  const detail = rows[0];
  const slots: Array<[string, number | null]> = [
    ["sprite_idle", detail.sprite_idle_entity_id],
    ["sprite_hover", detail.sprite_hover_entity_id],
    ["sprite_pressed", detail.sprite_pressed_entity_id],
    ["sprite_disabled", detail.sprite_disabled_entity_id],
    ["sprite_icon", detail.sprite_icon_entity_id],
    ["sprite_badge", detail.sprite_badge_entity_id],
    ["token_palette", detail.token_palette_entity_id],
    ["token_frame_style", detail.token_frame_style_entity_id],
    ["token_font", detail.token_font_entity_id],
    ["token_illumination", detail.token_illumination_entity_id],
  ];
  const out: LintResult[] = [];
  for (const [slotName, refId] of slots) {
    if (refId === null) continue;
    const ok = await entityResolves(refId, sql);
    if (!ok) {
      out.push({
        rule_id: "button.unresolved_ref",
        severity: "block",
        message: `button slot ${slotName} → entity ${refId} unresolved or retired`,
      });
    }
  }
  return out;
}

/**
 * Orphan candidate detection — leaf-kind entity (sprite / asset / button /
 * panel / token / audio) with zero inbound refs across published rows.
 * Emits 1 `warn` row tagged `{kind}.orphan_candidate`.
 *
 * Inbound-ref scan covers `panel_child.child_entity_id` + the 10 button slot
 * columns. `entity_version.params_json` ref-walking is deferred (archetype
 * params shape requires `params_schema` introspection).
 */
async function auditOrphanCandidate(
  kind: string,
  entityId: string,
  sql: Sql,
): Promise<LintResult[]> {
  if (!ORPHAN_KINDS.has(kind)) return [];

  // Inbound from panel_child.
  const panelInbound = await sql<Array<{ id: number }>>`
    select id from panel_child where child_entity_id = ${entityId} limit 1
  `;
  if (panelInbound.length > 0) return [];

  // Inbound from button_detail (10 slot columns OR'd together).
  const buttonInbound = await sql<Array<{ entity_id: number }>>`
    select entity_id from button_detail
    where sprite_idle_entity_id      = ${entityId}
       or sprite_hover_entity_id     = ${entityId}
       or sprite_pressed_entity_id   = ${entityId}
       or sprite_disabled_entity_id  = ${entityId}
       or sprite_icon_entity_id      = ${entityId}
       or sprite_badge_entity_id     = ${entityId}
       or token_palette_entity_id    = ${entityId}
       or token_frame_style_entity_id = ${entityId}
       or token_font_entity_id       = ${entityId}
       or token_illumination_entity_id = ${entityId}
    limit 1
  `;
  if (buttonInbound.length > 0) return [];

  return [
    {
      rule_id: `${kind}.orphan_candidate`,
      severity: "warn",
      message: `entity ${entityId} (${kind}) has no inbound refs across published consumers`,
    },
  ];
}

/**
 * Run Layer 2 (cross-entity) lint rules for `kind`. Routes per-kind to
 * appropriate resolver / orphan check; archetype + pool stubs return `[]`
 * until later stages.
 */
export async function runLayer2(
  kind: string,
  entityId: string,
  versionId: string,
  sql: Sql,
): Promise<LintResult[]> {
  // versionId reserved — Stage 14.1 will read from per-version ref edges; for
  // now ref resolution reads detail tables keyed on entity_id.
  void versionId;

  const out: LintResult[] = [];

  if (kind === "panel") {
    out.push(...(await auditPanelRefs(entityId, sql)));
  } else if (kind === "button") {
    out.push(...(await auditButtonRefs(entityId, sql)));
  } else if (kind === "archetype" || kind === "pool") {
    // Stub — see §Pending Decisions (Stage 14.1 ref materialization).
    return [];
  }

  // Orphan check runs for all leaf kinds (panel + button covered above for
  // outbound refs; orphan is independent — leaf kind with zero inbound).
  out.push(...(await auditOrphanCandidate(kind, entityId, sql)));

  return out;
}

/**
 * Pure aggregator — bucket Layer 1 + Layer 2 results by severity. Consumed
 * by `PublishDialog` (TECH-2570) + the publish API (TECH-2571).
 */
export function aggregateLintResults(
  layer1: LintResult[],
  layer2: LintResult[],
): {
  block: LintResult[];
  warn: LintResult[];
  info: LintResult[];
} {
  const block: LintResult[] = [];
  const warn: LintResult[] = [];
  const info: LintResult[] = [];
  for (const row of [...layer1, ...layer2]) {
    if (row.severity === "block") block.push(row);
    else if (row.severity === "warn") warn.push(row);
    else info.push(row);
  }
  return { block, warn, info };
}
