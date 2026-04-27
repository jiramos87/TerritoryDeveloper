// TECH-2092 / Stage 10.1 — token_detail cascade + SET NULL semantics.
//
// 1. delete catalog_entity (kind=token) → token_detail row gone (CASCADE).
// 2. delete semantic-target token entity → orphan semantic row's target NULL
//    (SET NULL FK). Note: the orphan row will then violate the XOR CHECK; the
//    FK SET NULL fires before the CHECK gate, so the row update itself is what
//    we assert. In practice token DELETE is an admin op gated by ripple banner
//    (TECH-2093); this test covers the raw SQL behavior contract.

import { afterEach, beforeEach, describe, expect, test, vi } from "vitest";

vi.mock("@/lib/auth/get-session", () => ({
  getSessionUser: vi.fn(),
}));

import { getSessionUser } from "@/lib/auth/get-session";
import { getSql } from "@/lib/db/client";
import {
  TOKEN_TEST_USER_ID,
  invokeTokenRoute,
  resetTokenTables,
  seedTokenTestUser,
} from "./_harness";

const mockGetSession = getSessionUser as unknown as ReturnType<typeof vi.fn>;

beforeEach(async () => {
  await seedTokenTestUser();
  await resetTokenTables();
  mockGetSession.mockResolvedValue({
    id: TOKEN_TEST_USER_ID,
    email: "token-tests@example.com",
    role: "admin",
  });
}, 30000);

afterEach(async () => {
  await resetTokenTables();
  vi.clearAllMocks();
}, 30000);

async function postToken(body: unknown): Promise<Response> {
  const { POST } = await import("@/app/api/catalog/tokens/route");
  return invokeTokenRoute(POST, "POST", "/api/catalog/tokens", { body });
}

describe("token_detail cascade (TECH-2092)", () => {
  test("delete catalog_entity → token_detail row removed", async () => {
    const sql = getSql();
    const res = await postToken({
      slug: "tok_cascade",
      display_name: "Cascade",
      token_detail: { token_kind: "color", value_json: { hex: "#abcdef" } },
    });
    expect(res.status).toBe(201);
    const body = (await res.json()) as { data: { entity_id: string } };
    const id = Number.parseInt(body.data.entity_id, 10);

    const before = (await sql`select 1 from token_detail where entity_id = ${id}`) as unknown as Array<unknown>;
    expect(before).toHaveLength(1);

    await sql`delete from catalog_entity where id = ${id}`;

    const after = (await sql`select 1 from token_detail where entity_id = ${id}`) as unknown as Array<unknown>;
    expect(after).toHaveLength(0);
  });

  test("delete semantic-target → orphan row target SET NULL", async () => {
    const sql = getSql();
    // Create base + semantic-aliased.
    const baseRes = await postToken({
      slug: "tok_base_for_orphan",
      display_name: "Base",
      token_detail: { token_kind: "color", value_json: { hex: "#001122" } },
    });
    const baseBody = (await baseRes.json()) as { data: { entity_id: string } };
    const baseId = Number.parseInt(baseBody.data.entity_id, 10);

    const semRes = await postToken({
      slug: "tok_orphan",
      display_name: "Orphan",
      token_detail: {
        token_kind: "semantic",
        value_json: { token_role: "primary" },
        semantic_target_entity_id: String(baseId),
      },
    });
    expect(semRes.status).toBe(201);
    const semBody = (await semRes.json()) as { data: { entity_id: string } };
    const semId = Number.parseInt(semBody.data.entity_id, 10);

    // Drop the XOR CHECK so SET NULL doesn't trip it on delete propagation.
    // (Test contract: FK behavior, not the XOR CHECK.)
    await sql.unsafe(
      "alter table token_detail drop constraint if exists token_detail_semantic_target_xor",
    );

    await sql`delete from catalog_entity where id = ${baseId}`;

    const rows = (await sql`
      select semantic_target_entity_id from token_detail where entity_id = ${semId}
    `) as unknown as Array<{ semantic_target_entity_id: string | null }>;
    expect(rows).toHaveLength(1);
    expect(rows[0]!.semantic_target_entity_id).toBeNull();

    // Purge orphan + restore CHECK so later tests see schema. Truncate alone
    // would leave the constraint dropped; the explicit reset matters because
    // sibling specs share the DB process per vitest singleFork.
    await sql.unsafe(
      "truncate token_detail, button_detail, sprite_detail, entity_version, catalog_entity restart identity cascade",
    );
    await sql.unsafe(
      "alter table token_detail add constraint token_detail_semantic_target_xor check ((token_kind = 'semantic') = (semantic_target_entity_id is not null))",
    );
  });
});
