// TECH-1675 — sprite promote.
//
// Covers POST /api/catalog/sprites/[slug]/promote — happy-path blob copy +
// detail column write, missing-source 404, validation errors. Uses an
// isolated tmp blob root via BLOB_ROOT env override so the test does not
// touch the repo-shipped var/blobs/.

import * as fs from "node:fs/promises";
import * as os from "node:os";
import * as path from "node:path";
import { afterEach, beforeAll, beforeEach, describe, expect, test, vi } from "vitest";

vi.mock("@/lib/auth/get-session", () => ({
  getSessionUser: vi.fn(),
}));

import { getSessionUser } from "@/lib/auth/get-session";
import { getSql } from "@/lib/db/client";
import {
  SPRITE_TEST_USER_ID,
  invokeSpriteRoute,
  resetSpriteTables,
  seedSpriteTestUser,
} from "./_harness";

const mockGetSession = getSessionUser as unknown as ReturnType<typeof vi.fn>;

const TEST_RUN_ID = "ccccccc1-cccc-4ccc-8ccc-cccccccccccc";
const VARIANT_IDX = 0;

let TMP_BLOB_ROOT: string;

beforeAll(async () => {
  TMP_BLOB_ROOT = await fs.mkdtemp(path.join(os.tmpdir(), "sprite-promote-"));
  process.env.BLOB_ROOT = TMP_BLOB_ROOT;
});

beforeEach(async () => {
  await seedSpriteTestUser();
  await resetSpriteTables();
  mockGetSession.mockResolvedValue({
    id: SPRITE_TEST_USER_ID,
    email: "sprite-tests@example.com",
    role: "admin",
  });

  // Seed source blob: {TMP_BLOB_ROOT}/{run_id}/0.png
  const runDir = path.join(TMP_BLOB_ROOT, TEST_RUN_ID);
  await fs.mkdir(runDir, { recursive: true });
  await fs.writeFile(path.join(runDir, "0.png"), Buffer.from([0x89, 0x50, 0x4e, 0x47]));
}, 30000);

afterEach(async () => {
  await resetSpriteTables();
  // Clean Generated PNGs we wrote during the test.
  const gen = path.resolve(__dirname, "..", "..", "..", "..", "..", "Assets", "Sprites", "Generated");
  try {
    const entries = await fs.readdir(gen);
    for (const e of entries) {
      if (e !== ".gitkeep") await fs.unlink(path.join(gen, e)).catch(() => {});
    }
  } catch {
    // dir may not exist yet on first run; ignore.
  }
  vi.clearAllMocks();
}, 30000);

async function postSprite(body: unknown): Promise<Response> {
  const { POST } = await import("@/app/api/catalog/sprites/route");
  return invokeSpriteRoute(POST, "POST", "/api/catalog/sprites", { body });
}

async function postPromote(slug: string, body: unknown): Promise<Response> {
  const { POST } = await import("@/app/api/catalog/sprites/[slug]/promote/route");
  return invokeSpriteRoute(POST, "POST", `/api/catalog/sprites/${slug}/promote`, {
    body,
    params: { slug },
  });
}

describe("POST /api/catalog/sprites/[slug]/promote (TECH-1675)", () => {
  test("promote_happy: copies blob + writes detail columns", async () => {
    await postSprite({ slug: "promote_one", display_name: "P" });
    const res = await postPromote("promote_one", { run_id: TEST_RUN_ID, variant_idx: VARIANT_IDX });
    expect(res.status).toBe(200);
    const body = (await res.json()) as {
      ok: boolean;
      data: { slug: string; assets_path: string };
      audit_id: string | null;
    };
    expect(body.ok).toBe(true);
    expect(body.data.assets_path).toBe("Assets/Sprites/Generated/promote_one.png");
    expect(body.audit_id).toMatch(/^\d+$/);

    // Asset file exists.
    const repoRoot = path.resolve(__dirname, "..", "..", "..", "..", "..");
    const dest = path.join(repoRoot, "Assets", "Sprites", "Generated", "promote_one.png");
    const stat = await fs.stat(dest);
    expect(stat.size).toBeGreaterThan(0);

    // sprite_detail columns updated.
    const sql = getSql();
    const det = (await sql`
      select assets_path, source_run_id::text as source_run_id, source_variant_idx, provenance
        from sprite_detail d
        join catalog_entity e on e.id = d.entity_id
       where e.slug = 'promote_one'
    `) as unknown as Array<{
      assets_path: string;
      source_run_id: string;
      source_variant_idx: number;
      provenance: string;
    }>;
    expect(det[0]!.assets_path).toBe("Assets/Sprites/Generated/promote_one.png");
    expect(det[0]!.source_run_id).toBe(TEST_RUN_ID);
    expect(det[0]!.source_variant_idx).toBe(VARIANT_IDX);
    expect(det[0]!.provenance).toBe("generator");
  });

  test("promote_bad_run_id: 400 when run_id is not a UUID", async () => {
    await postSprite({ slug: "bad_run", display_name: "B" });
    const res = await postPromote("bad_run", { run_id: "not-uuid", variant_idx: 0 });
    expect(res.status).toBe(400);
  });

  test("promote_missing_blob: 404 when source blob absent", async () => {
    await postSprite({ slug: "no_blob", display_name: "N" });
    const fakeRun = "deadbeef-dead-4ead-8ead-deadbeefdead";
    const res = await postPromote("no_blob", { run_id: fakeRun, variant_idx: 0 });
    expect(res.status).toBe(404);
    const body = (await res.json()) as { code: string };
    expect(body.code).toBe("not_found");
  });

  test("promote_unknown_sprite: 404", async () => {
    const res = await postPromote("missing_sprite", { run_id: TEST_RUN_ID, variant_idx: 0 });
    expect(res.status).toBe(404);
  });
});
