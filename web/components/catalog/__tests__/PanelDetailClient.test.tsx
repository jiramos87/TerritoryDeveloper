import { describe, it, expect } from "vitest";

import { buildPanelChildSetBody } from "@/app/catalog/panels/[slug]/PanelDetailClient";
import type { PanelChildRowState } from "@/components/catalog/PanelChildRow";
import type { PanelDetailSlotState } from "@/app/catalog/panels/[slug]/PanelDetailClient";
import type { CatalogPanelDto } from "@/types/api/catalog-api";

/**
 * <PanelDetailClient /> body-shape coverage (TECH-1886 / Stage 8.1).
 *
 * Plan Digest §Test Blueprint mandates assertion that the save payload matches
 * the DEC-A43 atomic-replace shape `{ updated_at, slots: [{ name, children:
 * [{ child_entity_id, child_kind, order_idx, params_json }] }] }`. We assert
 * via the exported `buildPanelChildSetBody` helper that the orchestrator
 * delegates to, exercising mixed-state slots (filled, empty, optional).
 */

const PANEL: CatalogPanelDto = {
  entity_id: "00000000-0000-0000-0000-000000000010",
  slug: "panel_demo",
  display_name: "Demo panel",
  tags: [],
  retired_at: null,
  current_published_version_id: null,
  updated_at: "2026-04-26T10:00:00.000Z",
  panel_detail: {
    archetype_entity_id: "00000000-0000-0000-0000-000000000020",
    background_sprite_entity_id: null,
    palette_entity_id: null,
    frame_style_entity_id: null,
    layout_template: "vstack",
    modal: false,
    slots_schema: {
      header: { accepts_kind: ["label"], min: 0, max: 1 },
      body: { accepts_kind: ["button", "panel"] },
      footer: { accepts_kind: ["button"], min: 0, max: 2 },
    },
  },
  slots: [],
  archetype_resolution: null,
};

const HEADER_CHILD: PanelChildRowState = {
  child_entity_id: "00000000-0000-0000-0000-000000000030",
  child_kind: "label",
  order_idx: 0,
  params_json: { text: "Title" },
  resolved: { slug: "label_title", display_name: "Title label" },
};

const BODY_CHILD_A: PanelChildRowState = {
  child_entity_id: "00000000-0000-0000-0000-000000000031",
  child_kind: "button",
  order_idx: 0,
  params_json: {},
  resolved: { slug: "button_save", display_name: "Save" },
};

const BODY_CHILD_B: PanelChildRowState = {
  child_entity_id: "00000000-0000-0000-0000-000000000032",
  child_kind: "button",
  order_idx: 1,
  params_json: { variant: "danger" },
  resolved: { slug: "button_cancel", display_name: "Cancel" },
};

describe("buildPanelChildSetBody (PanelDetailClient save shape)", () => {
  it("emits DEC-A43 envelope with updated_at + per-slot children list", () => {
    const slots: PanelDetailSlotState[] = [
      {
        name: "header",
        schema: PANEL.panel_detail!.slots_schema!.header!,
        children: [HEADER_CHILD],
      },
      {
        name: "body",
        schema: PANEL.panel_detail!.slots_schema!.body!,
        children: [BODY_CHILD_A, BODY_CHILD_B],
      },
      {
        name: "footer",
        schema: PANEL.panel_detail!.slots_schema!.footer!,
        children: [],
      },
    ];

    const body = buildPanelChildSetBody(PANEL, slots);

    expect(body.updated_at).toBe("2026-04-26T10:00:00.000Z");
    expect(body.slots).toHaveLength(3);
    expect(body.slots.map((s) => s.name)).toEqual(["header", "body", "footer"]);

    expect(body.slots[0]!.children).toEqual([
      {
        child_entity_id: "00000000-0000-0000-0000-000000000030",
        child_kind: "label",
        order_idx: 0,
        params_json: { text: "Title" },
      },
    ]);
    expect(body.slots[1]!.children).toEqual([
      {
        child_entity_id: "00000000-0000-0000-0000-000000000031",
        child_kind: "button",
        order_idx: 0,
        params_json: {},
      },
      {
        child_entity_id: "00000000-0000-0000-0000-000000000032",
        child_kind: "button",
        order_idx: 1,
        params_json: { variant: "danger" },
      },
    ]);
    expect(body.slots[2]!.children).toEqual([]);
  });

  it("renumbers order_idx contiguously even when state has stale indices", () => {
    const slots: PanelDetailSlotState[] = [
      {
        name: "body",
        schema: PANEL.panel_detail!.slots_schema!.body!,
        children: [
          { ...BODY_CHILD_A, order_idx: 5 },
          { ...BODY_CHILD_B, order_idx: 9 },
        ],
      },
    ];

    const body = buildPanelChildSetBody(PANEL, slots);

    expect(body.slots[0]!.children.map((c) => c.order_idx)).toEqual([0, 1]);
  });

  it("preserves null child_entity_id (spacer / label_inline support)", () => {
    const slots: PanelDetailSlotState[] = [
      {
        name: "body",
        schema: { accepts_kind: ["spacer"] },
        children: [
          {
            child_entity_id: null,
            child_kind: "spacer",
            order_idx: 0,
            params_json: { width: 8 },
            resolved: null,
          },
        ],
      },
    ];

    const body = buildPanelChildSetBody(PANEL, slots);

    expect(body.slots[0]!.children[0]).toEqual({
      child_entity_id: null,
      child_kind: "spacer",
      order_idx: 0,
      params_json: { width: 8 },
    });
  });
});
