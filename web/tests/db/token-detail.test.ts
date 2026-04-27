// TECH-2092 / Stage 10.1 — token_detail schema-level CHECK + FK assertions.
//
// Direct DB inserts (no API): each kind happy path; semantic_target XOR
// CHECK rejects non-semantic with target + semantic without target.

import { afterEach, beforeEach, describe, expect, test } from "vitest";

import { getSql } from "@/lib/db/client";

async function resetCatalog(): Promise<void> {
  const sql = getSql();
  await sql.unsafe(
    "truncate token_detail, button_detail, sprite_detail, entity_version, catalog_entity restart identity cascade",
  );
}

async function insertTokenEntity(slug: string): Promise<number> {
  const sql = getSql();
  const rows = (await sql`
    insert into catalog_entity (kind, slug, display_name)
    values ('token', ${slug}, ${slug})
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  return Number.parseInt(rows[0]!.id, 10);
}

beforeEach(async () => {
  await resetCatalog();
}, 30000);

afterEach(async () => {
  await resetCatalog();
}, 30000);

describe("token_detail schema (TECH-2092)", () => {
  test("color insert: hex value persists", async () => {
    const sql = getSql();
    const id = await insertTokenEntity("color_a");
    await sql`
      insert into token_detail (entity_id, token_kind, value_json)
      values (${id}, 'color', ${sql.json({ hex: "#112233" })})
    `;
    const rows = (await sql`
      select token_kind, value_json from token_detail where entity_id = ${id}
    `) as unknown as Array<{ token_kind: string; value_json: Record<string, unknown> }>;
    expect(rows).toHaveLength(1);
    expect(rows[0]!.token_kind).toBe("color");
    expect(rows[0]!.value_json).toEqual({ hex: "#112233" });
  });

  test("type-scale insert: persists font_family/size_px/line_height", async () => {
    const sql = getSql();
    const id = await insertTokenEntity("type_a");
    const value = { font_family: "Inter", size_px: 14, line_height: 1.4 };
    await sql`
      insert into token_detail (entity_id, token_kind, value_json)
      values (${id}, 'type-scale', ${sql.json(value)})
    `;
    const rows = (await sql`
      select value_json from token_detail where entity_id = ${id}
    `) as unknown as Array<{ value_json: Record<string, unknown> }>;
    expect(rows[0]!.value_json).toEqual(value);
  });

  test("motion insert: persists curve + duration_ms", async () => {
    const sql = getSql();
    const id = await insertTokenEntity("motion_a");
    const value = { curve: "ease-in-out", duration_ms: 200 };
    await sql`
      insert into token_detail (entity_id, token_kind, value_json)
      values (${id}, 'motion', ${sql.json(value)})
    `;
    const rows = (await sql`
      select value_json from token_detail where entity_id = ${id}
    `) as unknown as Array<{ value_json: Record<string, unknown> }>;
    expect(rows[0]!.value_json).toEqual(value);
  });

  test("spacing insert: persists px", async () => {
    const sql = getSql();
    const id = await insertTokenEntity("spacing_a");
    await sql`
      insert into token_detail (entity_id, token_kind, value_json)
      values (${id}, 'spacing', ${sql.json({ px: 8 })})
    `;
    const rows = (await sql`
      select value_json from token_detail where entity_id = ${id}
    `) as unknown as Array<{ value_json: Record<string, unknown> }>;
    expect(rows[0]!.value_json).toEqual({ px: 8 });
  });

  test("semantic insert: persists target + value_json.token_role", async () => {
    const sql = getSql();
    const targetId = await insertTokenEntity("color_target");
    await sql`
      insert into token_detail (entity_id, token_kind, value_json)
      values (${targetId}, 'color', ${sql.json({ hex: "#aabbcc" })})
    `;
    const semId = await insertTokenEntity("semantic_a");
    await sql`
      insert into token_detail (
        entity_id, token_kind, value_json, semantic_target_entity_id
      ) values (
        ${semId}, 'semantic', ${sql.json({ token_role: "primary" })}, ${targetId}
      )
    `;
    const rows = (await sql`
      select token_kind, semantic_target_entity_id::text as target
        from token_detail where entity_id = ${semId}
    `) as unknown as Array<{ token_kind: string; target: string }>;
    expect(rows[0]!.token_kind).toBe("semantic");
    expect(Number.parseInt(rows[0]!.target, 10)).toBe(targetId);
  });

  test("CHECK reject: non-semantic kind with target", async () => {
    const sql = getSql();
    const targetId = await insertTokenEntity("color_target_b");
    await sql`
      insert into token_detail (entity_id, token_kind, value_json)
      values (${targetId}, 'color', ${sql.json({ hex: "#000000" })})
    `;
    const id = await insertTokenEntity("bad_color");
    await expect(
      sql`
        insert into token_detail (
          entity_id, token_kind, value_json, semantic_target_entity_id
        ) values (
          ${id}, 'color', ${sql.json({ hex: "#111111" })}, ${targetId}
        )
      `,
    ).rejects.toThrow(/token_detail_semantic_target_xor/);
  });

  test("CHECK reject: semantic kind without target", async () => {
    const sql = getSql();
    const id = await insertTokenEntity("bad_semantic");
    await expect(
      sql`
        insert into token_detail (entity_id, token_kind, value_json)
        values (${id}, 'semantic', ${sql.json({ token_role: "x" })})
      `,
    ).rejects.toThrow(/token_detail_semantic_target_xor/);
  });
});
