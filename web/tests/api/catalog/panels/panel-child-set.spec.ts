// TECH-1887 / Stage 8.1 — setPanelChildTree happy path + 3 reject cases + cycle.
//
// Calls the lib helper directly (no HTTP route) — covers DEC-A27 (slot
// composition + cycle), DEC-A38 (stale fingerprint), DEC-A48-style error
// envelopes, DEC-A22 (lenient publish: child_version_id NULL preserved when
// child has no published version).

import { afterEach, beforeEach, describe, expect, test } from "vitest";

import { setPanelChildTree } from "@/lib/catalog/panel-child-set";
import { getSql } from "@/lib/db/client";
import {
  PANEL_TEST_USER_ID,
  getPanelChildRows,
  getPanelUpdatedAt,
  resetPanelTables,
  seedArchetypeEntity,
  seedButtonChildEntity,
  seedPanelEntity,
  seedPanelTestUser,
  seedSpriteChildEntity,
} from "./_harness";

beforeEach(async () => {
  await seedPanelTestUser();
  await resetPanelTables();
}, 30000);

afterEach(async () => {
  await resetPanelTables();
}, 30000);

void PANEL_TEST_USER_ID;

describe("setPanelChildTree (TECH-1887)", () => {
  test("happy_round_trip: 2 button children inserted with order_idx 0,1", async () => {
    const archetypeId = await seedArchetypeEntity("arc_simple", "Simple", {
      body: { accepts_kind: ["button"], min: 0, max: 4 },
    });
    const panel = await seedPanelEntity("p_round_trip", "Round Trip", {
      archetypeEntityId: archetypeId,
    });
    const btnA = await seedButtonChildEntity("btn_a", "A");
    const btnB = await seedButtonChildEntity("btn_b", "B");
    const startedAt = await getPanelUpdatedAt(panel.id);

    const sql = getSql();
    const result = await sql.begin(async (tx) =>
      setPanelChildTree(
        panel.id,
        {
          updated_at: startedAt,
          slots: [
            {
              name: "body",
              children: [
                { child_entity_id: String(btnA), child_kind: "button", order_idx: 0 },
                { child_entity_id: String(btnB), child_kind: "button", order_idx: 1 },
              ],
            },
          ],
        },
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        tx as any,
      ),
    );
    expect(result.ok).toBe("ok");
    const rows = await getPanelChildRows(panel.id);
    expect(rows).toHaveLength(2);
    expect(rows[0]!.slot_name).toBe("body");
    expect(rows[0]!.order_idx).toBe(0);
    expect(rows[0]!.child_entity_id).toBe(String(btnA));
    expect(rows[1]!.order_idx).toBe(1);
    expect(rows[1]!.child_entity_id).toBe(String(btnB));
    // Draft path: child_version_id NULL.
    expect(rows[0]!.child_version_id).toBeNull();
  });

  test("kind_not_accepted: sprite into button-only slot rejected", async () => {
    const archetypeId = await seedArchetypeEntity("arc_btn_only", "Btn Only", {
      body: { accepts_kind: ["button"], min: 0, max: 2 },
    });
    const panel = await seedPanelEntity("p_kind_reject", "Kind Reject", {
      archetypeEntityId: archetypeId,
    });
    const sprite = await seedSpriteChildEntity("sp_x", "X");
    const startedAt = await getPanelUpdatedAt(panel.id);

    const sql = getSql();
    const result = await sql.begin(async (tx) =>
      setPanelChildTree(
        panel.id,
        {
          updated_at: startedAt,
          slots: [
            {
              name: "body",
              children: [{ child_entity_id: String(sprite), child_kind: "sprite", order_idx: 0 }],
            },
          ],
        },
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        tx as any,
      ),
    );
    expect(result.ok).toBe("validation");
    if (result.ok === "validation") {
      expect(result.error.code).toBe("kind_not_accepted");
      if (result.error.code === "kind_not_accepted") {
        expect(result.error.details.slot_name).toBe("body");
        expect(result.error.details.accepts).toContain("button");
      }
    }
    // Tree never touched.
    expect(await getPanelChildRows(panel.id)).toHaveLength(0);
  });

  test("slot_count_out_of_range: 0 children into min=1 slot rejected", async () => {
    const archetypeId = await seedArchetypeEntity("arc_min_one", "Min One", {
      body: { accepts_kind: ["button"], min: 1, max: 4 },
    });
    const panel = await seedPanelEntity("p_min_reject", "Min Reject", {
      archetypeEntityId: archetypeId,
    });
    const startedAt = await getPanelUpdatedAt(panel.id);

    const sql = getSql();
    const result = await sql.begin(async (tx) =>
      setPanelChildTree(
        panel.id,
        {
          updated_at: startedAt,
          slots: [{ name: "body", children: [] }],
        },
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        tx as any,
      ),
    );
    expect(result.ok).toBe("validation");
    if (result.ok === "validation") {
      expect(result.error.code).toBe("slot_count_out_of_range");
      if (result.error.code === "slot_count_out_of_range") {
        expect(result.error.details.count).toBe(0);
        expect(result.error.details.min).toBe(1);
      }
    }
  });

  test("panel_cycle_detected: A → B → A rejected", async () => {
    const archetypeId = await seedArchetypeEntity("arc_panels", "Panels", {
      body: { accepts_kind: ["panel", "button"], min: 0, max: 4 },
    });
    const panelA = await seedPanelEntity("p_a", "A", { archetypeEntityId: archetypeId });
    const panelB = await seedPanelEntity("p_b", "B", { archetypeEntityId: archetypeId });

    // Pre-seed: B has A as a child (existing edge B → A).
    const sql = getSql();
    await sql`
      insert into panel_child (panel_entity_id, slot_name, order_idx, child_kind, child_entity_id, params_json)
      values (${panelB.id}, 'body', 0, 'panel', ${panelA.id}, ${sql.json({ kind: "panel" })})
    `;

    // Now attempt A → B; cycle detector walks B → A, hits panelA = A.
    const startedAtA = await getPanelUpdatedAt(panelA.id);
    const result = await sql.begin(async (tx) =>
      setPanelChildTree(
        panelA.id,
        {
          updated_at: startedAtA,
          slots: [
            {
              name: "body",
              children: [{ child_entity_id: String(panelB.id), child_kind: "panel", order_idx: 0 }],
            },
          ],
        },
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        tx as any,
      ),
    );
    expect(result.ok).toBe("validation");
    if (result.ok === "validation") {
      expect(result.error.code).toBe("panel_cycle_detected");
      if (result.error.code === "panel_cycle_detected") {
        expect(result.error.details.cycle_path[0]).toBe(String(panelA.id));
        expect(result.error.details.cycle_path[result.error.details.cycle_path.length - 1]).toBe(
          String(panelA.id),
        );
      }
    }
  });

  test("stale_updated_at: rejected with current_updated_at", async () => {
    const archetypeId = await seedArchetypeEntity("arc_stale", "Stale", {
      body: { accepts_kind: ["button"], min: 0, max: 2 },
    });
    const panel = await seedPanelEntity("p_stale", "Stale", { archetypeEntityId: archetypeId });
    const sql = getSql();
    const result = await sql.begin(async (tx) =>
      setPanelChildTree(
        panel.id,
        {
          updated_at: "1970-01-01T00:00:00.000Z",
          slots: [{ name: "body", children: [] }],
        },
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        tx as any,
      ),
    );
    expect(result.ok).toBe("stale");
    if (result.ok === "stale") {
      expect(result.current_updated_at).toBeTruthy();
    }
  });

  test("publish_lenient_null: child_version_id NULL preserved when child has no published version", async () => {
    const archetypeId = await seedArchetypeEntity("arc_pub", "Pub", {
      body: { accepts_kind: ["button"], min: 0, max: 2 },
    });
    const panel = await seedPanelEntity("p_pub", "Pub", { archetypeEntityId: archetypeId });
    const btn = await seedButtonChildEntity("btn_unpub", "Unpublished");
    // Note: btn has no entity_version row → current_published_version_id stays NULL.
    const startedAt = await getPanelUpdatedAt(panel.id);

    const sql = getSql();
    const result = await sql.begin(async (tx) =>
      setPanelChildTree(
        panel.id,
        {
          updated_at: startedAt,
          publish: true,
          slots: [
            {
              name: "body",
              children: [{ child_entity_id: String(btn), child_kind: "button", order_idx: 0 }],
            },
          ],
        },
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        tx as any,
      ),
    );
    expect(result.ok).toBe("ok");
    const rows = await getPanelChildRows(panel.id);
    expect(rows).toHaveLength(1);
    expect(rows[0]!.child_version_id).toBeNull();
  });
});
