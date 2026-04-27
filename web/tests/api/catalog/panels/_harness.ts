// Panel catalog API integration harness (TECH-1887 + TECH-1888 / Stage 8.1).
// Mirrors button/sprite harness: direct-invoke route handlers; truncate spine
// tables between tests; one admin test user for actor_user_id FK.

import { NextRequest } from "next/server";
import { getSql } from "@/lib/db/client";

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type RouteHandler = (req: NextRequest, ctx?: any) => Promise<Response>;

export const PANEL_TEST_USER_ID = "44444444-4444-4444-8444-444444444444";

export async function resetPanelTables(): Promise<void> {
  const sql = getSql();
  // CASCADE handles all *_detail children of catalog_entity.
  await sql.unsafe(
    "truncate panel_child, panel_detail, button_detail, sprite_detail, entity_version, catalog_entity restart identity cascade",
  );
  await sql.unsafe("delete from audit_log where action like 'catalog.panel.%'");
}

export async function seedPanelTestUser(): Promise<string> {
  const sql = getSql();
  await sql`
    insert into users (id, email, display_name, role)
    values (${PANEL_TEST_USER_ID}::uuid, 'panel-tests@example.com', 'panel tests', 'admin')
    on conflict (id) do nothing
  `;
  return PANEL_TEST_USER_ID;
}

/** Insert a panel catalog_entity directly + matching panel_detail. */
export async function seedPanelEntity(
  slug: string,
  displayName: string,
  opts?: { archetypeEntityId?: number | null; layout?: "vstack" | "hstack" | "grid" | "free" },
): Promise<{ id: number; slug: string }> {
  const sql = getSql();
  const rows = (await sql`
    insert into catalog_entity (kind, slug, display_name)
    values ('panel', ${slug}, ${displayName})
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  const id = Number.parseInt(rows[0]!.id, 10);
  await sql`
    insert into panel_detail (entity_id, archetype_entity_id, layout_template)
    values (${id}, ${opts?.archetypeEntityId ?? null}, ${opts?.layout ?? "vstack"})
  `;
  return { id, slug };
}

/**
 * Insert an archetype catalog_entity + a published entity_version whose
 * `params_json.slots_schema` carries the supplied DEC-A27 schema. Returns the
 * archetype `catalog_entity.id`.
 */
export async function seedArchetypeEntity(
  slug: string,
  displayName: string,
  slotsSchema: Record<string, { accepts_kind: string[]; min?: number; max?: number }>,
): Promise<number> {
  const sql = getSql();
  const ent = (await sql`
    insert into catalog_entity (kind, slug, display_name)
    values ('archetype', ${slug}, ${displayName})
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  const archetypeId = Number.parseInt(ent[0]!.id, 10);
  const params = sql.json({ slots_schema: slotsSchema } as Parameters<typeof sql.json>[0]);
  const ver = (await sql`
    insert into entity_version (entity_id, version_number, status, params_json)
    values (${archetypeId}, 1, 'published', ${params})
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  const versionId = Number.parseInt(ver[0]!.id, 10);
  await sql`
    update catalog_entity set current_published_version_id = ${versionId}
    where id = ${archetypeId}
  `;
  return archetypeId;
}

/** Insert a button catalog_entity for use as a panel child. */
export async function seedButtonChildEntity(slug: string, displayName: string): Promise<number> {
  const sql = getSql();
  const rows = (await sql`
    insert into catalog_entity (kind, slug, display_name)
    values ('button', ${slug}, ${displayName})
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  return Number.parseInt(rows[0]!.id, 10);
}

/** Insert a sprite catalog_entity for child-kind mismatch tests. */
export async function seedSpriteChildEntity(slug: string, displayName: string): Promise<number> {
  const sql = getSql();
  const rows = (await sql`
    insert into catalog_entity (kind, slug, display_name)
    values ('sprite', ${slug}, ${displayName})
    returning id::text as id
  `) as unknown as Array<{ id: string }>;
  return Number.parseInt(rows[0]!.id, 10);
}

export async function getPanelUpdatedAt(panelEntityId: number): Promise<string> {
  const sql = getSql();
  const rows = (await sql`
    select updated_at from catalog_entity where id = ${panelEntityId}
  `) as unknown as Array<{ updated_at: string }>;
  return rows[0]!.updated_at;
}

export async function getPanelChildRows(panelEntityId: number) {
  const sql = getSql();
  return (await sql`
    select slot_name, order_idx, child_kind, child_entity_id::text as child_entity_id, child_version_id::text as child_version_id, params_json
    from panel_child
    where panel_entity_id = ${panelEntityId}
    order by slot_name asc, order_idx asc
  `) as unknown as Array<{
    slot_name: string;
    order_idx: number;
    child_kind: string;
    child_entity_id: string | null;
    child_version_id: string | null;
    params_json: Record<string, unknown>;
  }>;
}

export async function invokePanelRoute(
  handler: RouteHandler,
  method: "GET" | "POST" | "PATCH" | "DELETE",
  url: string,
  init?: { body?: unknown; headers?: Record<string, string>; params?: Record<string, string> },
): Promise<Response> {
  const reqInit: { method: string; body?: string; headers?: Record<string, string> } = { method };
  const headers: Record<string, string> = { ...(init?.headers ?? {}) };
  if (init?.body !== undefined) {
    reqInit.body = JSON.stringify(init.body);
    headers["content-type"] = "application/json";
  }
  if (Object.keys(headers).length > 0) reqInit.headers = headers;
  const req = new NextRequest(new URL(url, "http://localhost"), reqInit);
  const ctx = init?.params ? { params: Promise.resolve(init.params) } : undefined;
  return handler(req, ctx);
}
