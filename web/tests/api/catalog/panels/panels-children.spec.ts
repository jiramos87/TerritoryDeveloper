// TECH-1888 / Stage 8.1 — POST /api/catalog/panels/[slug]/children round-trip.
//
// Direct-invoke (no dev server); DB-backed; capability gate stubbed via
// session mock. setPanelChildTree unit-tested separately in
// panel-child-set.spec.ts; this spec covers route plumbing only — happy +
// validation envelope + cycle reject + 409 stale + 404 unknown slug + audit.

import { afterEach, beforeEach, describe, expect, test, vi } from "vitest";

vi.mock("@/lib/auth/get-session", () => ({
  getSessionUser: vi.fn(),
}));

import { getSessionUser } from "@/lib/auth/get-session";
import { getSql } from "@/lib/db/client";
import {
  PANEL_TEST_USER_ID,
  getPanelChildRows,
  getPanelUpdatedAt,
  invokePanelRoute,
  resetPanelTables,
  seedArchetypeEntity,
  seedButtonChildEntity,
  seedPanelEntity,
  seedPanelTestUser,
  seedSpriteChildEntity,
} from "./_harness";

const mockGetSession = getSessionUser as unknown as ReturnType<typeof vi.fn>;

beforeEach(async () => {
  await seedPanelTestUser();
  await resetPanelTables();
  mockGetSession.mockResolvedValue({
    id: PANEL_TEST_USER_ID,
    email: "panel-tests@example.com",
    role: "admin",
  });
}, 30000);

afterEach(async () => {
  await resetPanelTables();
  vi.clearAllMocks();
}, 30000);

async function postChildren(slug: string, body: unknown): Promise<Response> {
  const { POST } = await import("@/app/api/catalog/panels/[slug]/children/route");
  return invokePanelRoute(POST, "POST", `/api/catalog/panels/${slug}/children`, {
    body,
    params: { slug },
  });
}

describe("POST /api/catalog/panels/[slug]/children (TECH-1888)", () => {
  test("happy_round_trip: 2 button children written + audit emitted", async () => {
    const archetypeId = await seedArchetypeEntity("arc_basic", "Basic", {
      body: { accepts_kind: ["button"], min: 0, max: 4 },
    });
    const panel = await seedPanelEntity("p_round_trip", "Round Trip", {
      archetypeEntityId: archetypeId,
    });
    const btnA = await seedButtonChildEntity("btn_a", "A");
    const btnB = await seedButtonChildEntity("btn_b", "B");
    const startedAt = await getPanelUpdatedAt(panel.id);

    const res = await postChildren("p_round_trip", {
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
    });
    expect(res.status).toBe(200);
    const body = (await res.json()) as {
      ok: boolean;
      data: { entity_id: string; slug: string; rows_written: number; updated_at: string };
      audit_id: string | null;
    };
    expect(body.ok).toBe(true);
    expect(body.data.rows_written).toBe(2);
    expect(body.audit_id).toMatch(/^\d+$/);

    const rows = await getPanelChildRows(panel.id);
    expect(rows).toHaveLength(2);

    const sql = getSql();
    const audit = (await sql`
      select action, target_kind, target_id, payload
      from audit_log
      where action = 'catalog.panel.children_set'
      order by id desc limit 1
    `) as unknown as Array<{
      action: string;
      target_kind: string;
      target_id: string;
      payload: { slug: string; rows_written: number };
    }>;
    expect(audit).toHaveLength(1);
    expect(audit[0]!.target_kind).toBe("catalog_entity");
    expect(audit[0]!.target_id).toBe(String(panel.id));
    expect(audit[0]!.payload.slug).toBe("p_round_trip");
    expect(audit[0]!.payload.rows_written).toBe(2);
  });

  test("validation_kind_not_accepted: sprite into button-only slot -> 400 with details", async () => {
    const archetypeId = await seedArchetypeEntity("arc_btn_only", "Btn Only", {
      body: { accepts_kind: ["button"], min: 0, max: 2 },
    });
    const panel = await seedPanelEntity("p_kind_reject", "Kind Reject", {
      archetypeEntityId: archetypeId,
    });
    const sprite = await seedSpriteChildEntity("sp_x", "X");
    const startedAt = await getPanelUpdatedAt(panel.id);

    const res = await postChildren("p_kind_reject", {
      updated_at: startedAt,
      slots: [
        {
          name: "body",
          children: [{ child_entity_id: String(sprite), child_kind: "sprite", order_idx: 0 }],
        },
      ],
    });
    expect(res.status).toBe(400);
    const body = (await res.json()) as {
      error: string;
      code: string;
      details?: { code: string; details: { slot_name: string; accepts: string[] } };
    };
    expect(body.code).toBe("bad_request");
    expect(body.details?.code).toBe("kind_not_accepted");
    expect(body.details?.details.slot_name).toBe("body");
  });

  test("validation_panel_cycle_detected: A -> B -> A -> 400 with cycle_path", async () => {
    const archetypeId = await seedArchetypeEntity("arc_panels", "Panels", {
      body: { accepts_kind: ["panel", "button"], min: 0, max: 4 },
    });
    const panelA = await seedPanelEntity("p_a", "A", { archetypeEntityId: archetypeId });
    const panelB = await seedPanelEntity("p_b", "B", { archetypeEntityId: archetypeId });

    const sql = getSql();
    await sql`
      insert into panel_child (panel_entity_id, slot_name, order_idx, child_kind, child_entity_id)
      values (${panelB.id}, 'body', 0, 'panel', ${panelA.id})
    `;

    const startedAtA = await getPanelUpdatedAt(panelA.id);
    const res = await postChildren("p_a", {
      updated_at: startedAtA,
      slots: [
        {
          name: "body",
          children: [{ child_entity_id: String(panelB.id), child_kind: "panel", order_idx: 0 }],
        },
      ],
    });
    expect(res.status).toBe(400);
    const body = (await res.json()) as {
      code: string;
      details?: { code: string; details: { cycle_path: string[] } };
    };
    expect(body.details?.code).toBe("panel_cycle_detected");
    expect(body.details?.details.cycle_path[0]).toBe(String(panelA.id));
  });

  test("conflict_stale_updated_at: returns 409 with current_updated_at", async () => {
    const archetypeId = await seedArchetypeEntity("arc_stale", "Stale", {
      body: { accepts_kind: ["button"], min: 0, max: 2 },
    });
    await seedPanelEntity("p_stale", "Stale", { archetypeEntityId: archetypeId });

    const res = await postChildren("p_stale", {
      updated_at: "1970-01-01T00:00:00.000Z",
      slots: [{ name: "body", children: [] }],
    });
    expect(res.status).toBe(409);
    const body = (await res.json()) as { code: string; current?: unknown };
    expect(body.code).toBe("conflict");
    expect(body.current).toBeDefined();
  });

  test("not_found_unknown_slug: 404 when panel slug missing", async () => {
    const res = await postChildren("ghost_panel", {
      updated_at: "1970-01-01T00:00:00.000Z",
      slots: [],
    });
    expect(res.status).toBe(404);
    const body = (await res.json()) as { code: string };
    expect(body.code).toBe("not_found");
  });
});
