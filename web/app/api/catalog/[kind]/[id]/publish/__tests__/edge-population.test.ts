/**
 * Golden test — `catalog_ref_edge` row counts after publish (TECH-3003 /
 * Stage 14.1).
 *
 * Direct-invokes the generic publish route for fixtures of all 8 catalog
 * kinds (sprite, asset, button, panel, pool, token, archetype, audio) and
 * asserts deterministic edge counts per kind. Lock contract:
 *
 *   sprite     → 0 edges (terminal walker)
 *   token      → 0 edges (terminal walker)
 *   audio      → 0 edges (terminal walker)
 *   pool       → 0 edges (stub walker — version-scoped pool refs deferred)
 *   archetype  → 0 edges (stub walker — params resolution deferred)
 *   panel      → 2 edges (panel_detail.{palette,frame_style} → token)
 *   button     → 6 edges (button_detail 6 sprite slots → sprite)
 *   asset      → 5 edges (asset_detail 5 sprite slots → sprite)
 *
 * Re-publish idempotency check: publishing the same panel twice produces
 * the same 2 rows (DELETE-then-INSERT contract from edge-builder).
 *
 * @see web/lib/refs/edge-builder.ts — TECH-3002 walker dispatch
 * @see db/migrations/0043_catalog_ref_edge.sql — table schema
 * @see ia/projects/asset-pipeline/stage-14.1 — TECH-3003 §Plan Digest
 */

import { afterEach, beforeEach, describe, expect, test, vi } from "vitest";

vi.mock("@/lib/auth/get-session", () => ({
  getSessionUser: vi.fn(),
}));

import { NextRequest } from "next/server";

import { getSessionUser } from "@/lib/auth/get-session";
import { getSql } from "@/lib/db/client";

const mockGetSession = getSessionUser as unknown as ReturnType<typeof vi.fn>;

const TEST_USER_ID = "44444444-4444-4444-8444-444444444444";

async function resetEdgeTables(): Promise<void> {
  const sql = getSql();
  await sql.unsafe(
    "truncate catalog_ref_edge, panel_detail, button_detail, asset_detail, audio_detail, pool_detail, token_detail, sprite_detail, entity_version, catalog_entity restart identity cascade",
  );
  await sql.unsafe("delete from audit_log where action like 'catalog.%.publish'");
}

async function seedTestUser(): Promise<void> {
  const sql = getSql();
  await sql`
    insert into users (id, email, display_name, role)
    values (${TEST_USER_ID}::uuid, 'edge-tests@example.com', 'edge tests', 'admin')
    on conflict (id) do nothing
  `;
}

interface SeededEntity {
  entity_id: number;
  draft_version_id: number;
  published_version_id: number;
}

/**
 * Insert one `catalog_entity` + 1 published version + 1 draft version, with
 * `catalog_entity.current_published_version_id` pointing at the published
 * row. Returns numeric ids.
 */
async function seedEntityWithVersions(
  kind: string,
  slug: string,
  displayName: string,
): Promise<SeededEntity> {
  const sql = getSql();
  const entityRows = (await sql`
    insert into catalog_entity (kind, slug, display_name)
    values (${kind}, ${slug}, ${displayName})
    returning id
  `) as Array<{ id: number }>;
  const entityId = entityRows[0]!.id;

  const pubRows = (await sql`
    insert into entity_version (entity_id, version_number, status)
    values (${entityId}, 1, 'published')
    returning id
  `) as Array<{ id: number }>;
  const pubVersionId = pubRows[0]!.id;

  await sql`
    update catalog_entity
    set current_published_version_id = ${pubVersionId}
    where id = ${entityId}
  `;

  const draftRows = (await sql`
    insert into entity_version (entity_id, version_number, status)
    values (${entityId}, 2, 'draft')
    returning id
  `) as Array<{ id: number }>;
  const draftVersionId = draftRows[0]!.id;

  return {
    entity_id: entityId,
    draft_version_id: draftVersionId,
    published_version_id: pubVersionId,
  };
}

async function invokePublish(
  kind: string,
  entityId: number,
  versionId: number,
  justification?: string,
): Promise<Response> {
  const { POST } = await import("@/app/api/catalog/[kind]/[id]/publish/route");
  const body: Record<string, unknown> = { versionId: String(versionId) };
  if (justification !== undefined) body.justification = justification;
  const req = new NextRequest(
    new URL(`/api/catalog/${kind}/${entityId}/publish`, "http://localhost"),
    {
      method: "POST",
      body: JSON.stringify(body),
      headers: { "content-type": "application/json" },
    },
  );
  const ctx = {
    params: Promise.resolve({ kind, id: String(entityId) }),
  };
  return POST(req, ctx);
}

async function countEdges(
  kind: string,
  entityId: number,
  versionId: number,
): Promise<number> {
  const sql = getSql();
  const rows = (await sql`
    select count(*)::int as n
    from catalog_ref_edge
    where src_kind = ${kind}
      and src_id = ${entityId}
      and src_version_id = ${versionId}
  `) as Array<{ n: number }>;
  return rows[0]!.n;
}

beforeEach(async () => {
  await seedTestUser();
  await resetEdgeTables();
  mockGetSession.mockResolvedValue({
    id: TEST_USER_ID,
    email: "edge-tests@example.com",
    role: "admin",
  });
}, 30000);

afterEach(async () => {
  await resetEdgeTables();
  vi.clearAllMocks();
}, 30000);

describe("publish hook → catalog_ref_edge population (TECH-3003)", () => {
  test("sprite publish emits 0 edges (terminal walker)", async () => {
    const sprite = await seedEntityWithVersions("sprite", "sp_solo", "Solo Sprite");
    const sql = getSql();
    await sql`
      insert into sprite_detail (entity_id, pixels_per_unit, pivot_x, pivot_y, provenance)
      values (${sprite.entity_id}, 100, 0.5, 0.5, 'hand')
    `;
    const res = await invokePublish(
      "sprite",
      sprite.entity_id,
      sprite.draft_version_id,
      "test fixture — orphan warn ack",
    );
    expect(res.status).toBe(200);
    expect(
      await countEdges("sprite", sprite.entity_id, sprite.draft_version_id),
    ).toBe(0);
  });

  test("token publish emits 0 edges (terminal walker)", async () => {
    const token = await seedEntityWithVersions("token", "tok_solo", "Solo Token");
    const sql = getSql();
    await sql`
      insert into token_detail (entity_id, token_kind, value_json)
      values (${token.entity_id}, 'color', '{"hex":"#fff"}'::jsonb)
    `;
    const res = await invokePublish(
      "token",
      token.entity_id,
      token.draft_version_id,
      "test fixture — orphan warn ack",
    );
    expect(res.status).toBe(200);
    expect(
      await countEdges("token", token.entity_id, token.draft_version_id),
    ).toBe(0);
  });

  test("audio publish emits 0 edges (terminal walker)", async () => {
    const audio = await seedEntityWithVersions("audio", "aud_solo", "Solo Audio");
    const sql = getSql();
    await sql`
      insert into audio_detail (entity_id, source_uri, duration_ms, sample_rate, channels, fingerprint, loudness_lufs, peak_db)
      values (${audio.entity_id}, 's3://x', 1000, 48000, 2, 'fp1', -16, -3)
    `;
    const res = await invokePublish(
      "audio",
      audio.entity_id,
      audio.draft_version_id,
      "test fixture — orphan warn ack",
    );
    expect(res.status).toBe(200);
    expect(
      await countEdges("audio", audio.entity_id, audio.draft_version_id),
    ).toBe(0);
  });

  test("pool publish emits 0 edges (stub walker — deferred)", async () => {
    const pool = await seedEntityWithVersions("pool", "pl_solo", "Solo Pool");
    const sql = getSql();
    await sql`
      insert into pool_detail (entity_id, primary_subtype)
      values (${pool.entity_id}, 'tree')
    `;
    const res = await invokePublish("pool", pool.entity_id, pool.draft_version_id);
    expect(res.status).toBe(200);
    expect(
      await countEdges("pool", pool.entity_id, pool.draft_version_id),
    ).toBe(0);
  });

  test("archetype publish emits 0 edges (stub walker — deferred)", async () => {
    const arch = await seedEntityWithVersions("archetype", "arc_solo", "Solo Arch");
    const res = await invokePublish("archetype", arch.entity_id, arch.draft_version_id);
    expect(res.status).toBe(200);
    expect(
      await countEdges("archetype", arch.entity_id, arch.draft_version_id),
    ).toBe(0);
  });

  test("panel publish emits 2 panel.token edges (palette + frame_style)", async () => {
    const palette = await seedEntityWithVersions("token", "tok_palette", "Palette");
    const frame = await seedEntityWithVersions("token", "tok_frame", "Frame Style");
    const panel = await seedEntityWithVersions("panel", "pn_solo", "Solo Panel");
    const sql = getSql();
    await sql`
      insert into token_detail (entity_id, token_kind, value_json)
      values (${palette.entity_id}, 'color', '{"hex":"#fff"}'::jsonb),
             (${frame.entity_id}, 'spacing', '{"px":8}'::jsonb)
    `;
    await sql`
      insert into panel_detail (entity_id, palette_entity_id, frame_style_entity_id)
      values (${panel.entity_id}, ${palette.entity_id}, ${frame.entity_id})
    `;
    const res = await invokePublish(
      "panel",
      panel.entity_id,
      panel.draft_version_id,
      "test fixture — orphan warn ack",
    );
    expect(res.status).toBe(200);
    expect(
      await countEdges("panel", panel.entity_id, panel.draft_version_id),
    ).toBe(2);
    const rows = (await sql`
      select dst_kind, edge_role
      from catalog_ref_edge
      where src_kind = 'panel' and src_id = ${panel.entity_id}
        and src_version_id = ${panel.draft_version_id}
    `) as Array<{ dst_kind: string; edge_role: string }>;
    expect(rows.every((r) => r.dst_kind === "token" && r.edge_role === "panel.token")).toBe(true);
  });

  test("button publish emits 6 button.sprite edges (token slots dropped)", async () => {
    const sprites: SeededEntity[] = [];
    for (const slug of [
      "sp_idle",
      "sp_hover",
      "sp_pressed",
      "sp_disabled",
      "sp_icon",
      "sp_badge",
    ]) {
      const s = await seedEntityWithVersions("sprite", slug, slug);
      sprites.push(s);
    }
    const sql = getSql();
    for (const s of sprites) {
      await sql`
        insert into sprite_detail (entity_id, pixels_per_unit, pivot_x, pivot_y, provenance)
        values (${s.entity_id}, 100, 0.5, 0.5, 'hand')
      `;
    }
    const tokenPalette = await seedEntityWithVersions("token", "tok_p2", "PaletteB");
    await sql`
      insert into token_detail (entity_id, token_kind, value_json)
      values (${tokenPalette.entity_id}, 'color', '{"hex":"#000"}'::jsonb)
    `;
    const button = await seedEntityWithVersions("button", "btn_solo", "Solo Button");
    await sql`
      insert into button_detail (
        entity_id,
        sprite_idle_entity_id, sprite_hover_entity_id, sprite_pressed_entity_id,
        sprite_disabled_entity_id, sprite_icon_entity_id, sprite_badge_entity_id,
        token_palette_entity_id
      ) values (
        ${button.entity_id},
        ${sprites[0].entity_id}, ${sprites[1].entity_id}, ${sprites[2].entity_id},
        ${sprites[3].entity_id}, ${sprites[4].entity_id}, ${sprites[5].entity_id},
        ${tokenPalette.entity_id}
      )
    `;
    const res = await invokePublish(
      "button",
      button.entity_id,
      button.draft_version_id,
      "test fixture — orphan warn ack",
    );
    expect(res.status).toBe(200);
    expect(
      await countEdges("button", button.entity_id, button.draft_version_id),
    ).toBe(6);
    const rows = (await sql`
      select dst_kind, edge_role
      from catalog_ref_edge
      where src_kind = 'button' and src_id = ${button.entity_id}
        and src_version_id = ${button.draft_version_id}
    `) as Array<{ dst_kind: string; edge_role: string }>;
    expect(rows.every((r) => r.dst_kind === "sprite" && r.edge_role === "button.sprite")).toBe(true);
  });

  test("asset publish emits 5 asset.sprite edges (5 sprite slots)", async () => {
    const sprites: SeededEntity[] = [];
    for (const slug of [
      "sp_world",
      "sp_btn_target",
      "sp_btn_pressed",
      "sp_btn_disabled",
      "sp_btn_hover",
    ]) {
      const s = await seedEntityWithVersions("sprite", slug, slug);
      sprites.push(s);
    }
    const sql = getSql();
    for (const s of sprites) {
      await sql`
        insert into sprite_detail (entity_id, pixels_per_unit, pivot_x, pivot_y, provenance)
        values (${s.entity_id}, 100, 0.5, 0.5, 'hand')
      `;
    }
    const asset = await seedEntityWithVersions("asset", "as_solo", "Solo Asset");
    await sql`
      insert into asset_detail (
        entity_id, category, footprint_w, footprint_h, has_button,
        world_sprite_entity_id, button_target_sprite_entity_id,
        button_pressed_sprite_entity_id, button_disabled_sprite_entity_id,
        button_hover_sprite_entity_id
      ) values (
        ${asset.entity_id}, 'building', 1, 1, true,
        ${sprites[0].entity_id}, ${sprites[1].entity_id},
        ${sprites[2].entity_id}, ${sprites[3].entity_id},
        ${sprites[4].entity_id}
      )
    `;
    const res = await invokePublish(
      "asset",
      asset.entity_id,
      asset.draft_version_id,
      "test fixture — orphan warn ack",
    );
    expect(res.status).toBe(200);
    expect(
      await countEdges("asset", asset.entity_id, asset.draft_version_id),
    ).toBe(5);
    const rows = (await sql`
      select dst_kind, edge_role
      from catalog_ref_edge
      where src_kind = 'asset' and src_id = ${asset.entity_id}
        and src_version_id = ${asset.draft_version_id}
    `) as Array<{ dst_kind: string; edge_role: string }>;
    expect(rows.every((r) => r.dst_kind === "sprite" && r.edge_role === "asset.sprite")).toBe(true);
  });

  test("re-publish idempotency: panel publish twice → still 2 rows", async () => {
    const palette = await seedEntityWithVersions("token", "tok_idem_pal", "Pal");
    const frame = await seedEntityWithVersions("token", "tok_idem_frm", "Frm");
    const panel = await seedEntityWithVersions("panel", "pn_idem", "Idem Panel");
    const sql = getSql();
    await sql`
      insert into token_detail (entity_id, token_kind, value_json)
      values (${palette.entity_id}, 'color', '{"hex":"#fff"}'::jsonb),
             (${frame.entity_id}, 'spacing', '{"px":8}'::jsonb)
    `;
    await sql`
      insert into panel_detail (entity_id, palette_entity_id, frame_style_entity_id)
      values (${panel.entity_id}, ${palette.entity_id}, ${frame.entity_id})
    `;
    const r1 = await invokePublish(
      "panel",
      panel.entity_id,
      panel.draft_version_id,
      "test fixture — orphan warn ack",
    );
    expect(r1.status).toBe(200);
    // Re-publish — route detects already-published and short-circuits;
    // edges from first run remain. Re-running edge builder directly on the
    // same triple would also produce 2 rows (DELETE-then-INSERT contract).
    const r2 = await invokePublish(
      "panel",
      panel.entity_id,
      panel.draft_version_id,
      "test fixture — orphan warn ack",
    );
    expect(r2.status).toBe(200);
    expect(
      await countEdges("panel", panel.entity_id, panel.draft_version_id),
    ).toBe(2);
  });
});
